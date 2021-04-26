using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace IOPT
{
    public partial class EventPath
    {
        public static int FindInterval(double _time)
        {
            int interval = -1;
            Debug.Assert(_time < PARA.IntervalSets[PARA.IntervalSets.Count - 1][PARA.IntervalSets[PARA.IntervalSets.Count-1].Count-1], "time should be less than the maximum time interval value");
            interval = PARA.IntervalSets.FindIndex(x => x[0] <= _time && x[x.Count - 1]+1> _time);
            return interval;
        }

        // Compute capacity congestion cost and compare it with the solution output

        public static void UpdateNodeFlow(ref SegClass Seg, double Alight, double Board, int LineId, int q, int t)
        {
            Debug.Assert(q >= 0 || t >= 0, "update node flow time interval is not identified");
            if (q >= 0)
            {
                Seg.Tail.m_LineID_Flow[LineId][q].Board += Board;
                Seg.Head.m_LineID_Flow[LineId][q].Alight += Alight;
            }
            if (t >= 0)
            {
                Seg.Tail.m_LineID_Flow[LineId][t].Board += Board;
                Seg.Head.m_LineID_Flow[LineId][t].Alight += Alight;
            }
        }


        /// <summary>
        /// update board and alight node flow 
        /// </summary>
        /// <param name="BoardNode"></param>
        /// <param name="AlightNode"></param>
        /// <param name="AlightFlow"></param>
        /// <param name="BoardFlow"></param>
        /// <param name="LineId"></param>
        /// <param name="TrainIndex"></param>
        /// <param name="BoardInterval"></param>
        /// <param name="AlightInterval"></param>
        /// <returns></returns>
        public static void UpdateNodeFlow(NodeClass BoardNode, NodeClass AlightNode, double AlightFlow, double BoardFlow, int LineId, int TrainIndex,
                                          int BoardInterval, int AlightInterval)
        {
            if (TrainIndex>=0)
            {
                Debug.Assert(BoardInterval < 0 && AlightInterval < 0);
                BoardNode.m_LineID_Flow[LineId][TrainIndex].Board+= BoardFlow;
                AlightNode.m_LineID_Flow[LineId][TrainIndex].Alight+= AlightFlow;
            }
            if (BoardInterval>=0&&AlightInterval>=0)
            {
                Debug.Assert(TrainIndex < 0);
                BoardNode.m_LineID_Flow[LineId][BoardInterval].Board += BoardFlow;
                AlightNode.m_LineID_Flow[LineId][AlightInterval].Alight += AlightFlow;
            }
        }

        /// <summary>
        /// 1. load flow from cplex solution 
        /// 2. update capacity congestion cost, which will be used for the next iteration of event path finding algorithm
        /// </summary>
        /// <param name="LpData"></param>
        /// <param name="_events"></param>
        /// <param name="_segs"></param>
        /// <param name="_nodes"></param>
        /// <param name="_lines"></param>
        /// <param name="_sol"></param>
        /// <returns></returns>
        public static void Loading(BBmain.LpInput LpData, EventClass[] _events, ref List<SegClass> _segs, ref List<NodeClass> _nodes,
                                    ref List<TransitLineClass> _lines, BBmain.SolClass _sol)
        {
            // initialize the flow and capacity cost values
            foreach (NodeClass n in _nodes)
            {
                foreach(var key in n.m_LineID_Flow.Keys)
                {
                    for (int f = 0; f < n.m_LineID_Flow[key].Count; f++) n.m_LineID_Flow[key][f].Ini();
                }
            }
            foreach (SegClass s in _segs)
            {
                for (int i = 0; i < s.OnBoardFlow.Count; i++)
                {
                    s.CapCost[i] = 0.0;
                    s.OnBoardFlow[i] = 0;
                }
            }
            double Alight = 0;
            double Board = 0;
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                double Flow = _sol.PathFlow[p];
#if (Debug)
                Console.WriteLine("Path Flow = {0}", Flow);
#endif
                double DepTime = LpData.PathSet[p].TargetDepTime;
                for (int i = 0; i < LpData.PathSet[p].VisitNodes.Count - 1; i++)
                {
                    Alight = Flow;
                    Board = Flow;
                    if (i == 0) { Alight = 0; Board = Flow; }
                    double WaitingTime = 0;
                    int BoardNodeID = LpData.PathSet[p].VisitNodes[i];
                    int AlightNodeID = LpData.PathSet[p].VisitNodes[i + 1];
#if (Debug)
                    Console.WriteLine("Board Node={0},Alight Node = {1}", BoardNodeID, AlightNodeID);
#endif                
                    //int node = LpData.PathSet[p].VisitNodes[i];
                    int BoardLineID = LpData.PathSet[p].m_NodeId_BoardLineId[BoardNodeID];
                    SegClass VisitSeg = _segs.Find(x => x.MapLine[0].ID == BoardLineID && x.Tail.ID == BoardNodeID);
                    int waitpos = LpData.PathSet[p].m_NodeId2WaitVar[BoardNodeID];
                    WaitingTime = _sol.v_PasPathWait[waitpos];
                    DepTime += WaitingTime;
#if (Debug)
                    Console.WriteLine("Wait = {0:f4}, BoardLine = {1},", WaitingTime, LpData.PathSet[p].m_NodeId_BoardLineId[BoardNodeID]);
#endif                
                    int TrainIndex = -1;
                    int DepInterval = -1;
                    int ArrInterval = -1;
                    int LineIndex = -1;

                    switch (VisitSeg.MapLine[0].ServiceType)
                    {
                        case TransitServiceType.Schedule:
                            LineIndex = LpData.SchLineSet.FindIndex(x => x.ID == BoardLineID);  // line index the schedule based lines
                            int delaPos = LpData.PathSet[p].m_NodeId2Delta[BoardNodeID];
                            for (int q = 0; q < LpData.SchLineSet[LineIndex].NumOfTrains; q++)
                            {
                                if (_sol.v_DeltaPathTrain[delaPos + q].Equals(1))
                                {
                                    TrainIndex = q;
                                    break;
                                }
                            }
                            DepTime += VisitSeg.TravelTime + PARA.PathPara.MinPasArrDepGap;
                            break;
                        case TransitServiceType.Frequency:
                            DepInterval = FindInterval(DepTime);
                            LineIndex= LpData.FreLineSet.FindIndex(x => x.ID == BoardLineID);
                            double TimeDif = LpData.FreLineSet[LineIndex].m_Stop_TimeDif[BoardLineID][AlightNodeID]
                                   - LpData.FreLineSet[LineIndex].m_Stop_TimeDif[BoardLineID][BoardNodeID];
                            ArrInterval = FindInterval(DepTime + TimeDif);
                            DepTime += VisitSeg.TravelTime;

                            break;
                    }

                    UpdateNodeFlow(_nodes[BoardNodeID], _nodes[AlightNodeID], Alight, Board, BoardLineID, TrainIndex,
                                 DepInterval, ArrInterval);
#if (Debug)

                    Console.WriteLine("Arrival at next node time = {0}", DepTime);
#endif

                }
            }


            // update segment flow following the line order
            foreach(TransitLineClass l in _lines)
            {
                
                int Dim = -1;
                if (l.ServiceType == TransitServiceType.Frequency) Dim = PARA.IntervalSets.Count;
                else Dim = l.NumOfTrains;
                for (int q = 0; q < Dim; q++)
                {
                    double OnBoard = 0;
                    for (int i = 0; i < l.MapSegs.Count; i++)
                    {
                        l.MapSegs[i].OnBoardFlow[q] = OnBoard +
                            l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Board -
                            l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Alight;
                        OnBoard = l.MapSegs[i].OnBoardFlow[q];
                    }
                }

            }

            foreach(SegClass s in _segs)
            {

                int LineIndex = -1;
                //capacity for the frequency 
                switch (s.MapLine[0].ServiceType)
                {
                    case TransitServiceType.Frequency:
                        LineIndex = LpData.FreLineSet.FindIndex(x => x.ID == s.MapLine[0].ID);
                        for (int t=0;t<PARA.IntervalSets.Count;t++)
                        {
                            s.CapCost[t] = PARA.PathPara.SeatValue * Math.Max(s.OnBoardFlow[t] - 
                                _sol.v_Fre[LineIndex] * PARA.DesignPara.FreBusCap*(PARA.IntervalSets[t].Count), 0);
                        }
                        //cap[tau] = cplex.Prod(PARA.SeatValue, cplex.Max(cplex.Diff(P_onboard[tau], cplex.Prod(v_Fre[l], PARA.FreCap)), 0));

                        break;
                    case TransitServiceType.Schedule:
                        LineIndex = LpData.SchLineSet.FindIndex(x => x.ID == s.MapLine[0].ID);
                        for (int q=0;q<s.MapLine[0].NumOfTrains;q++)
                        {
                            s.CapCost[q] = PARA.PathPara.SeatValue * Math.Max(s.OnBoardFlow[q] - LpData.SchLineSet[LineIndex].TrainCap[q], 0);
                        }
                        //cap = cplex.Prod(PARA.SeatValue, cplex.Max(cplex.Diff(P_onboard, LpData.SchLineSet[l].TrainCap[q]), 0));
                        break;
                }
                for (int i = 0; i < s.OnBoardFlow.Count; i++)
                {
                    if (s.MapLine[0].ServiceType==TransitServiceType.Schedule)
                    {
                        Console.WriteLine("Seg={0},Line={1},q={2},onboard={3}, CapCost={4}",
                            s.ID, s.MapLine[0].ID, i, s.OnBoardFlow[i], s.CapCost[i]);
                    }
                    else
                    {
                        Console.WriteLine("Seg={0},Line={1},Interval={2},onboard={3}, CapCost={4}",
                                s.ID, s.MapLine[0].ID, i, s.OnBoardFlow[i], s.CapCost[i]);
                    }

                }

            }

            // todo update capacity cost 


        }
    }







}
