
using System;
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
                BoardNode.m_LineID_Flow[BoardLineID][TrainIndex].Board += BoardFlow;
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

            for (int p = 0; p < _LpData.NumOfPath; p++)
            {
                double Flow = _sol.PathFlowExprVal[p];
                double DepTime = _LpData.PathSet[p].TargetDepTime;// time to the next node?
                for (int i = 0; i < _LpData.PathSet[p].VisitNodes.Count - 1; i++)
                {
                    AlightFlow = Flow;
                    BoardFlow = Flow;
                    bool isTransfer = true;
                    double WaitingTime = 0;
                    int BoardNodeID = _LpData.PathSet[p].VisitNodes[i];
                    int AlightNodeID = _LpData.PathSet[p].VisitNodes[i + 1];
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
                        WaitingTime = 0;
                        AlightLineID = -1;
                        ContinousLineID = BoardLineID;
                        AlightFlow = 0;
                        BoardFlow = 0;
                        ArrivalFlow = Flow;
                        isTransfer = false;
                    }

                    DepTime += WaitingTime;
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
                            double TrainDep = Nodes.Find(x => x.ID == BoardNodeID).getDepTime(BoardLineID, TrainIndex);
                            if (isTransfer)
                                DepTime = VisitSeg.TravelTime + TrainDep + PARA.PathPara.MinTransferTime; // 
                            else
                                DepTime = VisitSeg.TravelTime + TrainDep; // 
                            break;
                        case TransitServiceType.Frequency:
                            DepInterval = PARA.FindInterval(DepTime);
                            LineIndex = _LpData.FreLineSet.FindIndex(x => x.ID == BoardLineID);
                            double TimeDifNew = _LpData.FreLineSet[LineIndex].getTravelTimeBetweenStop(BoardNodeID, AlightNodeID);
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
                    for (int i = 0; i < l.MapSegs.Count; i++)
                    {
                        l.MapSegs[i].OnBoardFlow[q] = l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Arrival + l.MapSegs[i].Tail.m_LineID_Flow[l.ID][q].Board;
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
                switch (s.MapLine[0].ServiceType)
                {
                    case TransitServiceType.Frequency:
                        LineIndex = _LpData.FreLineSet.FindIndex(x => x.ID == s.MapLine[0].ID);
                        for (int t = 0; t < PARA.IntervalSets.Count; t++)
                        {
                            s.DwellCost[t] =  s.Tail.getDwellTime(s.MapLine[0].ID, t);
                        }
                        break;
                    case TransitServiceType.Schedule:
                        LineIndex = _LpData.SchLineSet.FindIndex(x => x.ID == s.MapLine[0].ID);
                        for (int q = 0; q < s.MapLine[0].NumOfTrains; q++)
                        {
                            s.DwellCost[q] = s.Tail.getDwellTime(s.MapLine[0].ID, q);
                        }
                        break;
                }
                #endregion 
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
                        Cost = PARA.PathPara.SeatBeta * OnBoard +
                            PARA.PathPara.StandBeta * Math.Max(OnBoard - Cap, 0);
                        if (OnBoard - Cap >= PARA.GeZero) { Cost += PARA.PathPara.StandConstant; }
                        break;
                    case CapCostType.IsNull:
                        Console.WriteLine("Warning_NetworkLoading: Para.Design.CapCostPara is not set properly");
                        break;
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
