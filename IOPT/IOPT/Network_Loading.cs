using ILOG.CPLEX;
using System;
using System.Data;
using System.Diagnostics;

namespace IOPT
{
    public partial class NetworkClass
    {
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
        protected internal static void UpdateNodeFlow(NodeClass BoardNode, NodeClass AlightNode, double AlightFlow, double BoardFlow, int BoardLineID, int TrainIndex,
                                          int BoardInterval, int AlightInterval, int AlightLineId, bool isTransfer, double ArrivalFlow, int ContinousLineID)
        {
            if (TrainIndex >= 0)
            {
                Debug.Assert(BoardInterval < 0 && AlightInterval < 0);
                BoardNode.m_LineID_Flow[BoardLineID][TrainIndex].Board += BoardFlow;
                //AlightNode.m_LineID_Flow[BoardLineID][TrainIndex].Alight += AlightFlow;

                if (isTransfer)
                {
                    AlightNode.m_LineID_Flow[AlightLineId][TrainIndex].Alight += AlightFlow;
                }
                else
                {
                    AlightNode.m_LineID_Flow[ContinousLineID][TrainIndex].Arrival += ArrivalFlow;

                }

            }
            if (BoardInterval >= 0 && AlightInterval >= 0)
            {
                Debug.Assert(TrainIndex < 0);
                BoardNode.m_LineID_Flow[BoardLineID][BoardInterval].Board += BoardFlow;

                if (isTransfer)
                {
                    AlightNode.m_LineID_Flow[AlightLineId][AlightInterval].Alight += AlightFlow;
                }
                else
                {
                    AlightNode.m_LineID_Flow[ContinousLineID][AlightInterval].Arrival += ArrivalFlow;
                }
            }
        }
        /// <summary>
        /// 1. load flow from cplex solution 
        /// 2. update capacity congestion cost, which will be used for the next iteration of event path finding algorithm
        /// </summary>
        /// <param name="LpData"></param>
        /// <param name="_sol"></param>
        /// <returns></returns>
        protected internal void Loading(BBmain.LpInput _LpData, BBmain.SolClass _sol)
        {
            // initialize the flow and capacity cost values
            #region IniNodeAndSegFlow
            foreach (NodeClass n in Nodes)
            {
                foreach (var key in n.m_LineID_Flow.Keys)
                {
                    for (int f = 0; f < n.m_LineID_Flow[key].Count; f++)
                        n.m_LineID_Flow[key][f].Ini();
                }
            }
            foreach (SegClass s in Segs)
            {
                for (int i = 0; i < s.OnBoardFlow.Count; i++)
                {
                    s.CapCost[i] = 0.0;
                    s.OnBoardFlow[i] = 0;
                    s.BoardingFlow[i] = 0;
                    s.DwellCost[i] = 0.0;
                }
            }
            #endregion
            double AlightFlow = 0;
            double BoardFlow = 0;
            double ArrivalFlow = 0; // use arrival flow to compute the onboard flow
            //bool isTranfer = false;  // if not transfer, then the passengers are still on board.

            for (int p = 0; p < _LpData.NumOfPath; p++)
            {
                double Flow = _sol.PathFlowExprVal[p];
#if DEBUG
                Console.WriteLine("Info_NetworkLoad: Flow ={0}, Path = {1}", Flow, p);
#endif
                double DepTime = _LpData.PathSet[p].TargetDepTime;// time to the next node?
                for (int i = 0; i < _LpData.PathSet[p].VisitNodes.Count - 1; i++)
                {
                    AlightFlow = Flow;
                    BoardFlow = Flow;
                    bool isTransfer = true;
                    double WaitingTime = 0;
                    int BoardNodeID = _LpData.PathSet[p].VisitNodes[i];
                    int AlightNodeID = _LpData.PathSet[p].VisitNodes[i + 1];
#if DEBUG
                    Console.WriteLine("Info_NetworkLaoding: BoardNode={0}, Alight Node = {1}", BoardNodeID, AlightNodeID);
#endif                
                    //int node = LpData.PathSet[p].VisitNodes[i];
                    int BoardLineID = _LpData.PathSet[p].m_NodeID_NextLine[BoardNodeID].ID;
                    int AlightLineID = -1;
                    int ContinousLineID = -1;

                    SegClass VisitSeg = Segs.Find(x => x.MapLine[0].ID == BoardLineID && x.Tail.ID == BoardNodeID);
                    if (_LpData.PathSet[p].TranferNodes.Contains(BoardNodeID))
                    {
                        int WaitPos = _LpData.PathSet[p].m_NodeId_WaitVarPos[BoardNodeID];
                        WaitingTime = _sol.v_PasPathWait[WaitPos];
                        AlightLineID = BoardLineID;
                        if (AlightNodeID != _LpData.PathSet[p].VisitNodes[_LpData.PathSet[p].VisitNodes.Count - 1])
                        {
                            //if (BoardLineID == LpData.PathSet[p].m_NodeID_NextLine[AlightNodeID].ID)
                            if (_LpData.PathSet[p].m_NodeID_NextLine[AlightNodeID].ID == BoardLineID)
                            {
                                isTransfer = false;
                                AlightFlow = 0;
                                ArrivalFlow = Flow;
                                ContinousLineID = BoardLineID;
                            }
                        }
                    }
                    else
                    {
                        // if the path is not transfer nodes
                        MyLog.Instance.Info("Check Network Loading: the path does not contain transfer node");
                        WaitingTime = 0;
                        AlightLineID = -1;
                        ContinousLineID = BoardLineID;
                        AlightFlow = 0;
                        BoardFlow = 0;
                        ArrivalFlow = Flow;
                        isTransfer = false;
                    }

                    DepTime += WaitingTime;
#if DEBUG
                    Console.WriteLine("Info_NetworkLoad: Wait = {0:f4}, BoardLine = {1},", WaitingTime, _LpData.PathSet[p].m_NodeID_NextLine[BoardNodeID].ID);
#endif                
                    int TrainIndex = -1;
                    int DepInterval = -1;
                    int ArrInterval = -1;
                    int LineIndex = -1;

                    switch (VisitSeg.MapLine[0].ServiceType)
                    {
                        case TransitServiceType.Schedule:
                            LineIndex = _LpData.SchLineSet.FindIndex(x => x.ID == BoardLineID);  // line index the schedule based lines
                            int delaPos = _LpData.PathSet[p].m_NodeId_DeltaTrainBoard[BoardNodeID];
                            for (int q = 0; q < _LpData.SchLineSet[LineIndex].NumOfTrains; q++)
                            {
                                if (_sol.v_Delta_Board_Veh[delaPos + q].Equals(1))
                                {
                                    TrainIndex = q;
                                    break;
                                }
                            }
                            // firstVersion
                            //DepTime += VisitSeg.TravelTime + PARA.PathPara.MinTransferTime;
                            //DepTime += VisitSeg.TravelTime + dep;
                            // end of first version
                            double TrainDep = Nodes.Find(x => x.ID == BoardNodeID).getDepTime(BoardLineID, TrainIndex);
                            // the following compute the possible depart time at the destination
                            if (isTransfer)
                                DepTime = VisitSeg.TravelTime + TrainDep + PARA.PathPara.MinTransferTime; // 
                            else
                                DepTime = VisitSeg.TravelTime + TrainDep; // 
                            //Console.WriteLine("get deptime = {0}, dep = {1}", TrainDep, DepTime);
                            //Console.ReadLine();
                            break;
                        case TransitServiceType.Frequency:
                            DepInterval = PARA.FindInterval(DepTime);
                            LineIndex = _LpData.FreLineSet.FindIndex(x => x.ID == BoardLineID);
                            //double TimeDif = _LpData.FreLineSet[LineIndex].m_Stop_TimeDif[BoardLineID][AlightNodeID] - _LpData.FreLineSet[LineIndex].m_Stop_TimeDif[BoardLineID][BoardNodeID];
                            double TimeDifNew = _LpData.FreLineSet[LineIndex].getTravelTimeBetweenStop(BoardNodeID, AlightNodeID);
                            //Console.WriteLine("Wtf:check timedif {0}, {1}", TimeDif, TimeDifNew);
                            //Console.WriteLine("Wtf:need to address the dwell time difference");
                            //Console.ReadLine();
                            //.m_Stop_TimeDif[BoardLineID][AlightNodeID] - _LpData.FreLineSet[LineIndex].m_Stop_TimeDif[BoardLineID][BoardNodeID];
                            //ArrInterval = PARA.FindInterval(DepTime + TimeDif);
                            ArrInterval = PARA.FindInterval(DepTime + TimeDifNew);
                            if (isTransfer)
                                DepTime += VisitSeg.TravelTime + PARA.PathPara.MinTransferTime;
                            else
                                DepTime += VisitSeg.TravelTime;
                            break;
                    }

                    UpdateNodeFlow(Nodes[BoardNodeID], Nodes[AlightNodeID],
                                   AlightFlow, BoardFlow, BoardLineID, TrainIndex,
                                   DepInterval, ArrInterval, AlightLineID, isTransfer, ArrivalFlow, ContinousLineID);

                    //if (VisitSeg.MapLine[0].ServiceType.Equals(TransitServiceType.Frequency))
                    //{
                    //    Nodes[AlightNodeID].m_LineID_Flow[BoardLineID][ArrInterval].Arrival += Alight;
                    //}
#if DEBUG
                    Console.WriteLine("Info_NetworkLoading: Arrival at next node time = {0}", DepTime);
#endif
                }
            }


            // update segment flow following the line order
            foreach (TransitLineClass l in Lines)
            {
                int Dim = -1;  // Dim stands for dimension for the frequency and schedule based lines
                if (l.ServiceType == TransitServiceType.Frequency)
                {
                    Dim = PARA.IntervalSets.Count;
                }
                else Dim = l.NumOfTrains;
                for (int q = 0; q < Dim; q++)
                {
                    //double OnBoard = 0;
                    for (int i = 0; i < l.MapSegs.Count; i++)
                    {
                        // avoid track on board: change to use arrival flow
                        //l.MapSegs[i].OnBoardFlow[q] = OnBoard +
                        //    l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Board -
                        //    l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Alight;
                        //if (q == 0 && i == 1 && l.ID == 0)
                        //    Console.WriteLine("Check onboard");

                        //l.MapSegs[i].OnBoardFlow[q] = l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Arrival +
                        //    l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Board -
                        //    l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Alight;
                        //l.MapSegs[i].OnBoardFlow[q] +=  + l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Board - l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Alight;
                        // arrival = without transfer flow + new onboard flow

                        //if (_sol.SolNumID == 3 && l.MapSegs[i].ID == 4)
                        //    Console.WriteLine("Check Fre Loading");


                        l.MapSegs[i].OnBoardFlow[q] = l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Arrival + l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Board;

                        //Console.WriteLine("Seg={0}, tail={1}, head={2}, MapLine={3},OnBoard={4},Board={5},Alight={6},Arrival={7} ",
                        //    l.MapSegs[i].ID, l.MapSegs[i].Tail.ID, l.MapSegs[i].Head.ID, l.ID, l.MapSegs[i].OnBoardFlow[q], 
                        //    l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Board, l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Alight, 
                        //    l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Arrival);

                        //if (i+1<l.MapSegs.Count)   // update flow for the next arrival
                        //{
                        //    if (l.ServiceType== TransitServiceType.Schedule)
                        //        l.MapSegs[i + 1].OnBoardFlow[q] = l.MapSegs[i].OnBoardFlow[q];
                        //    else
                        //    {

                        //    }
                        //}

                        //l.MapSegs[i].OnBoardFlow[q] = Math.Max(l.MapSegs[i].OnBoardFlow[q], 0);
                        //OnBoard = l.MapSegs[i].OnBoardFlow[q];
                        // Compute boarding flow, in case the board flow has a separated cost
                        l.MapSegs[i].BoardingFlow[q] += l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Board;


                        if (l.MapSegs[i].OnBoardFlow[q] < 0) MyLog.Instance.Warn("Loading flow function has negetive onboard flow");
                    }
                }
            }

            double BoardingFlow = 0;
            double Capacity = 0;
            double OnBoardFlow = 0;

            string FileName = MyFileNames.OutPutFolder + "Bcm_Seg.txt";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(FileName, true))
            {
                if (Global.NumOfIter == 0)
                {
                    file.WriteLine("Iter,SolIdD,SegID,Tail,Head,SegTravelTime,Line,Train/Interval_Id,OnBoardF,BoardF,CapCost");
                }

            }
            foreach (SegClass s in Segs)
            {
                int LineIndex = -1;
                //capacity for the frequency 
                double DivideCapValue;
                switch (s.MapLine[0].ServiceType)
                {
                    case TransitServiceType.Frequency:

                        LineIndex = _LpData.FreLineSet.FindIndex(x => x.ID == s.MapLine[0].ID);
                        DivideCapValue = 1.0 / SolveLp.Lp.getLineCapforCongest(_LpData.FreLineSet[LineIndex]);
                        for (int t = 0; t < PARA.IntervalSets.Count; t++)
                        {
                            OnBoardFlow = s.OnBoardFlow[t];
                            BoardingFlow = s.BoardingFlow[t];
                            Capacity = _sol.v_Fre[LineIndex] * _LpData.FreLineSet[LineIndex].FreCap * (PARA.IntervalSets[t].Count);
                            s.CapCost[t] = getCapCost(Capacity, BoardingFlow, OnBoardFlow, isBoardingStop: true, HaveSeat: false, DivideCapValue: DivideCapValue);
                        }
                        break;
                    case TransitServiceType.Schedule:
                        LineIndex = _LpData.SchLineSet.FindIndex(x => x.ID == s.MapLine[0].ID);
                        DivideCapValue = 1.0 / SolveLp.Lp.getLineCapforCongest(_LpData.SchLineSet[LineIndex]);

                        for (int q = 0; q < s.MapLine[0].NumOfTrains; q++)
                        {
                            OnBoardFlow = s.OnBoardFlow[q];
                            BoardingFlow = s.BoardingFlow[q];
                            Capacity = _LpData.SchLineSet[LineIndex].TrainCap[q];
                            s.CapCost[q] = getCapCost(Capacity, BoardingFlow, OnBoardFlow, isBoardingStop: true, HaveSeat: false, DivideCapValue: DivideCapValue);
                        }
                        break;
                }

                #region ReviseAddDwellTime
                // Revise add in 2021Feb: add s dwell cost

                switch (s.MapLine[0].ServiceType)
                {
                    case TransitServiceType.Frequency:
                        //Console.WriteLine("To be completed of calculation dwell time");
                        //Console.ReadLine();
                        LineIndex = _LpData.FreLineSet.FindIndex(x => x.ID == s.MapLine[0].ID);
                        for (int t = 0; t < PARA.IntervalSets.Count; t++)
                        {
                            //s.DwellCost[t] = PARA.PathPara.WaitW * s.Tail.getDwellTime(s.MapLine[0].ID, t);
                            s.DwellCost[t] =  s.Tail.getDwellTime(s.MapLine[0].ID, t);
                        }
                        break;
                    case TransitServiceType.Schedule:
                        LineIndex = _LpData.SchLineSet.FindIndex(x => x.ID == s.MapLine[0].ID);
                        for (int q = 0; q < s.MapLine[0].NumOfTrains; q++)
                        {
                            //s.DwellCost[q] = PARA.PathPara.WaitW * s.Tail.getDwellTime(s.MapLine[0].ID, q);
                            s.DwellCost[q] = s.Tail.getDwellTime(s.MapLine[0].ID, q);
                            //Console.WriteLine("Check sch dwell cost: n={0},l={1},q={2},dwc={3}", s.Tail.ID, LineIndex, q, s.DwellCost[q]);
                            //Console.ReadLine();
                            //s.DwellCost[q]
                        }
                        break;
                }
                //
                #endregion 

                // print segment cost over global iterations
                s.PrintSeg(_sol.SolNumID);
            }
        }
        protected internal double getCapCost(double Cap, double Board, double OnBoard,
                                bool isBoardingStop, bool HaveSeat, double DivideCapValue)
        {
            double Cost = -1;

            if (isBoardingStop)
            {
                switch (PARA.DesignPara.CapType)
                {
                    case CapCostType.StepWise:

                        //Cost = PARA.PathPara.BoardAlpha * Board * DivideCapValue +
                        //        PARA.PathPara.SeatBeta * OnBoard * DivideCapValue +
                        //        PARA.PathPara.StandBeta * Math.Max(OnBoard - Cap, 0) * DivideCapValue;
                        Cost = PARA.PathPara.SeatBeta * OnBoard +
                            PARA.PathPara.StandBeta * Math.Max(OnBoard - Cap, 0);
                        // add the stand constant val when the flow is greater than capacity more than GeZero
                        if (OnBoard - Cap >= PARA.GeZero)
                        {
                            Cost += PARA.PathPara.StandConstant;
                        }

                        break;
                    #region not used cases
                    case CapCostType.StandOnly:
                        Console.WriteLine("Warning_NetworkLoading: Para.Design.CapCostPara is not set properly");
                        Cost = PARA.PathPara.StandBeta * Math.Max(OnBoard - Cap, 0);
                        break;

                    case CapCostType.IsNull:
                        Console.WriteLine("Warning_NetworkLoading: Para.Design.CapCostPara is not set properly");
                        break;
                        #endregion
                }
            }
            else
            {
                ///<remarks>
                ///it is hard to determine whether it is a board or continuous stop when evaluating the cost associated with each link 
                ///So in this version it is not considered 
                ///</remarks>

            }
            return Cost;

        }



    }


}
