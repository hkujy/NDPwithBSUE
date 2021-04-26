///checked 2021-May
using System;
using System.Collections.Generic;
using System.Linq;
using ILOG.Concert;
using ILOG.CPLEX;
using IOPT;

namespace SolveLp
{
    public partial class Lp
    {
        public static int getPathAlightIndex_Sch(int p, int maxNumOfTrain,int s)
        {
            // the index should start from 0
            return p * maxNumOfTrain + s;
        }
        /// <summary>
        /// add capacity constraint cost for the schedule based lines 
        /// </summary>
        public static void SchCapCon(Cplex cplex,
                                    BBmain.LpInput LpData,
                                    List<INumExpr> PathFlow,
                                    List<INumExpr> PathCostExpr,
                                    List<INumExpr> DwellTimeExpr_Sch,
                                    IIntVar[] v_Delta_board_veh,
                                    INumVar[] v_Y,
                                    INumVar[] v_SchLinkCap,
                                    IIntVar[] v_Delta_Congest,
                                    IIntVar[] v_Delta_Seat,
                                    //IIntVar[] v_Delta_Dif,
                                    bool SolveModel)
        {
            #region Ini
            double RhsVal = -999, LhsVal = -999;
            double P_boardVal = -999, P_alightVal = -999, P_onboardVal = -999, P_arrVal = -999;
            double CapCostStandVal = -999,  CapCostStepWiseBoardVal = -999, StandingFlowVal = -999;
            double CapCostSeatVal = -999;
            //double CapCostBoardVal = -999,
            int Delta_CongPos = -1;
            INumExpr rhs = cplex.NumExpr();
            INumExpr lhs = cplex.NumExpr();
            INumExpr P_board = cplex.NumExpr();
            INumExpr P_alight = cplex.NumExpr();
            INumExpr P_onboard = cplex.NumExpr();
            INumExpr P_arr = cplex.NumExpr();
            INumExpr TempExpr = cplex.NumExpr();
            INumExpr CapCostStand = cplex.NumExpr();
            INumExpr CapCostSeat = cplex.NumExpr();
            //INumExpr CapCostBoard = cplex.NumExpr();
            INumExpr CapCostStepWise = cplex.NumExpr(); // cost including boarding cost
            INumExpr StandingFlow = cplex.NumExpr();
            List<int> ActiveBoardPathSet = new List<int>(); // contain the set of path to add capacity constraint
            List<int> ContinousPathSet = new List<int>();
            List<double> DwellTime_schExpVal = new List<double>();
            int maxTrainNum = 0; 
            List<INumExpr> PathAlightAndTransfer = new List<INumExpr>();
            foreach (TransitLineClass l in LpData.SchLineSet)
            {
                if (l.NumOfTrains >= maxTrainNum)
                {
                    maxTrainNum = l.NumOfTrains;
                }
            }
            double[] PathAlightValAndTransfer = new double[maxTrainNum * LpData.PathSet.Count];
            for (int p=0;p<LpData.PathSet.Count;p++)
            {
                for (int q = 0;q<maxTrainNum;q++)
                {
                    PathAlightAndTransfer.Add(cplex.NumExpr());
                }
            }

            for (int i = 0; i < DwellTimeExpr_Sch.Count; i++) DwellTime_schExpVal.Add(0.0);
            #endregion

            for (int l = 0; l < LpData.SchLineSet.Count(); l++)
            {
                int lineID = LpData.SchLineSet[l].ID;
                for (int q = 0; q < LpData.SchLineSet[l].NumOfTrains; q++)
                {
                    for (int k = 0; k < LpData.SchLineSet[l].Stops.Count(); k++)
                    {
                        int skId = LpData.SchLineSet[l].Stops[k].ID;
                        // arrival == on board
                        if (SolveModel)
                        {
                            if (k > 0) P_arr = P_onboard;
                            if (k == 0) P_arr = cplex.IntExpr();
                        }
                        else
                        {
                            if (k > 0) P_arrVal = P_onboardVal;
                            if (k == 0) P_arrVal = 0;
                        }

                        P_board = cplex.IntExpr(); P_boardVal = 0;
                        P_alight = cplex.IntExpr(); P_alightVal = 0;
                        P_onboardVal = 0;

                        ActiveBoardPathSet.Clear();
                        ContinousPathSet.Clear();
                        // compute boarding passengers 
                        for (int i = 0; i < PathAlightAndTransfer.Count; i++) PathAlightAndTransfer[i] = cplex.NumExpr();
                        for (int i = 0; i < PathAlightValAndTransfer.Length; i++) PathAlightValAndTransfer[i] = 0.0;
                        // step 1. compute alighting values 
                        for (int p = 0; p < LpData.PathSet.Count; p++)
                        {
                            if (!LpData.PathSet[p].m_NodeID_TransferLine.ContainsKey(skId)) continue;
                            if (LpData.PathSet[p].m_NodeID_TransferLine[skId].Alight.ID == lineID)
                            {   // if this path alight at this node
                                int SkPos = LpData.PathSet[p].VisitNodes.IndexOf(skId);
                                for (int ps = 0; ps < SkPos; ps++)  // previous stop
                                {
                                    int nodeid = LpData.PathSet[p].VisitNodes[ps];
                                    if (LpData.PathSet[p].m_NodeID_TransferLine.ContainsKey(nodeid))
                                    {
                                        if (LpData.PathSet[p].m_NodeID_TransferLine[nodeid].Board.ID == lineID  //board this line
                                        && LpData.PathSet[p].m_NodeID_TransferLine[skId].Alight.ID == lineID) //alight line l
                                        { // board line l at previous stops and alight 
                                            int pos = LpData.PathSet[p].m_NodeId_DeltaTrainBoard[nodeid] + q;
                                            int ptindex = getPathAlightIndex_Sch(p,maxTrainNum, q);
                                            if (SolveModel)
                                            {
                                                P_alight = cplex.Sum(P_alight, v_Y[pos]);
                                                PathAlightAndTransfer[ptindex] = cplex.Sum(PathAlightAndTransfer[ptindex], v_Y[pos]);
                                            }
                                            else
                                            {
                                                P_alightVal += cplex.GetValue(v_Y[pos]);
                                                PathAlightValAndTransfer[ptindex] += cplex.GetValue(v_Y[pos]);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        for (int p = 0; p < LpData.PathSet.Count; p++)
                        {
                            if (LpData.PathSet[p].VisitNodes.Contains(skId) ) // must visit the node and next line = line id
                            {
                                // a continuous line 
                                if (!LpData.PathSet[p].TranferNodes.Contains(skId)   // not a transfer node
                                    &&LpData.PathSet[p].m_NodeID_NextLine[skId].ID == lineID)    // next line is the same as the line of interests
                                {
                                    ContinousPathSet.Add(p);
                                }
                                if (LpData.PathSet[p].m_NodeID_TransferLine.ContainsKey(skId)) // transfer line
                                {
                                    if (LpData.PathSet[p].m_NodeID_TransferLine[skId].Board.ID == lineID)
                                    {
                                        ActiveBoardPathSet.Add(p);  //board line l and subject to line capacity constraint 
                                                                    // if path contains use this node and board this lines
                                        int pos = LpData.PathSet[p].m_NodeId_DeltaTrainBoard[skId] + q;
                                        int ptindex = getPathAlightIndex_Sch(p,maxTrainNum, q);
                                        if (SolveModel)
                                        {
                                            lhs = cplex.Diff(v_Y[pos], PathFlow[p]);
                                            rhs = cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(v_Delta_board_veh[pos], 1));

                                            cplex.AddGe(lhs, rhs);
                                            rhs = cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(1, v_Delta_board_veh[pos]));
                                            cplex.AddLe(lhs, rhs);

                                            cplex.AddLe(v_Y[pos], cplex.Prod(PARA.DesignPara.BigM, v_Delta_board_veh[pos]));

                                            P_board = cplex.Sum(P_board, v_Y[pos]);
                                        }
                                        else
                                        {
                                            LhsVal = cplex.GetValue(v_Y[pos]) - cplex.GetValue(PathFlow[p]);
                                            RhsVal = cplex.GetValue(v_Delta_board_veh[pos]);
                                            if (RhsVal.Equals(1))
                                            {
                                                if ((!LhsVal.Equals(0)) && Math.Abs(LhsVal) > PARA.ZERO) Console.WriteLine("PathFlow equality is violated");
                                            }
                                            P_boardVal += cplex.GetValue(v_Y[pos]);
                                        }
                                    }
                                }
                            }


                            #region addDwellTimeToPathCost
                            int cnode = skId;
                            int pdindex = LpData.GetPathDwellIndex(p, cnode);
                            if (!LpData.PathSet[p].TranferNodes.Contains(cnode)) continue;
                            if (LpData.PathSet[p].m_NodeID_TransferLine[cnode].Board.ID < 0) continue;   // destation node, there is no board
                            if (cnode == LpData.PathSet[p].m_NodeID_TransferLine[cnode].Board.Stops[0].ID) continue;
                            if (cnode == LpData.PathSet[p].Trip.OriginID) continue;
                            int loc = getPathAlightIndex_Sch(p,maxTrainNum,q);
#if DEBUG
                               
                            Console.WriteLine("Check boarding or alighting for add dwell time to sch lines : p = {0}, cnode = {1}, boardline = {2}, loc = {3}", p, cnode, lineID,loc);
                            Console.WriteLine("p={0},node={1},inde={2}", p, cnode, pdindex);
#endif
                            PathCostExpr[p] = cplex.Sum(PathCostExpr[p], cplex.Prod(DwellTimeExpr_Sch[loc], PARA.PathPara.WaitW));
                            if (pdindex < 0)
                            {
                                MyLog.Instance.Debug("Warning: the path dwell index is negative");
                            }
                            #endregion
                        }// for path set
                        if (SolveModel)
                        {
                            P_onboard = cplex.Diff(cplex.Sum(P_arr, P_board), P_alight);
                            int loc = LpData.GetSchDwellExpIndex(lineID, k, q);
                            if (loc >= 0)
                            {
                                DwellTimeExpr_Sch[loc] =
                                   cplex.Max(cplex.Prod(cplex.Max(P_board, P_alight), PARA.DesignPara.BoardAlightTimePerPas/LpData.SchLineSet[l].NumOfDoors),
                                    PARA.DesignPara.MinDwellTime);
                                cplex.AddGe(DwellTimeExpr_Sch[loc], 0);
                            }
                        }
                        else
                        {
                            P_onboardVal = P_arrVal + P_boardVal - P_alightVal;

                            int loc = LpData.GetSchDwellExpIndex(lineID, k, q);
                            if (loc>=0) DwellTime_schExpVal[loc] = Math.Max(P_alightVal,P_boardVal) * PARA.DesignPara.BoardAlightTimePerPas/LpData.SchLineSet[l].NumOfDoors;
#if DEBUG
                            if (loc>=0)
                            {
                                Console.WriteLine("Line={0}, s={1}, q ={2},loc={3}, No.Board={4},No.Alight={5},Time={6}",
                                    lineID, k, q, loc,P_boardVal,P_alightVal, DwellTime_schExpVal[loc]);
                            }
#endif
                        }

                        if (SolveModel)
                        {
                            CapCostStand = cplex.IntExpr();
                            CapCostSeat = cplex.IntExpr();
                            CapCostStepWise = cplex.IntExpr();
                            StandingFlow = cplex.Max(cplex.Diff(P_onboard, LpData.SchLineSet[l].TrainCap[q]), 0);
                        }
                        else
                        {
                            StandingFlowVal = Math.Max(0, P_onboardVal - LpData.SchLineSet[l].TrainCap[q]);
                        }

                        if (PARA.DesignPara.isConsiderSeatSequence)
                        {
                            if (skId != LpData.SchLineSet[l].Stops[LpData.SchLineSet[l].Stops.Count - 1].ID)
                            {
                                // if it is not the last node of a link
                                Delta_CongPos = FindDeltaCongestionPos(LpData, skId, lineID);
                                Delta_CongPos = Delta_CongPos + q;
                                if (SolveModel)
                                {
                                    cplex.AddLe(cplex.Diff(StandingFlow, PARA.ZERO), cplex.Prod(cplex.Diff(1, v_Delta_Congest[Delta_CongPos]), 0.9999/PARA.ZERO));
                                    cplex.AddLe(cplex.Diff(1, v_Delta_Congest[Delta_CongPos]), cplex.Prod(0.9999/PARA.ZERO, cplex.Sum(StandingFlow, PARA.ZERO)));
                                }
                                else
                                {
                                    if (cplex.GetValue(v_Delta_Congest[Delta_CongPos]).Equals(1))
                                    {
                                        if (StandingFlowVal > PARA.ZERO )
                                        {
                                            Console.WriteLine("Warning_SchCap: v_Delta_Congest = {0}, standing flow ={1}", cplex.GetValue(v_Delta_Congest[Delta_CongPos]), StandingFlowVal);
                                        }
                                    }
                                }
                            }
                        }
                        if (SolveModel)
                        {
                            if (skId != LpData.SchLineSet[l].Stops[LpData.SchLineSet[l].Stops.Count - 1].ID)
                            {

                                CapCostSeat = cplex.Prod(P_onboard, PARA.PathPara.SeatBeta);
                                CapCostStand = cplex.Prod(PARA.PathPara.StandBeta, StandingFlow);

                                CapCostStepWise =  cplex.Sum(CapCostSeat, 
                                    cplex.Sum(CapCostStand,
                                    cplex.Prod(PARA.PathPara.StandConstant, cplex.Diff(1, v_Delta_Congest[Delta_CongPos])))); 
                            }
                        }
                        else
                        {
                            if (skId != LpData.SchLineSet[l].Stops[LpData.SchLineSet[l].Stops.Count - 1].ID)
                            {
                                CapCostSeatVal = PARA.PathPara.SeatBeta * P_onboardVal;
                                CapCostStandVal = PARA.PathPara.StandBeta * StandingFlowVal;

                                if (StandingFlowVal >= PARA.GeZero)
                                {
                                    CapCostStandVal += PARA.PathPara.StandConstant;
                                }
                                CapCostStepWiseBoardVal = CapCostSeatVal + CapCostStandVal;
                            }
                        }

                        for (int pppID = 0; pppID < LpData.PathSet.Count; pppID++)
                        {
                            if (ActiveBoardPathSet.Contains(pppID))
                            {
                                int pathid = pppID;
                                int boardpos = LpData.PathSet[pathid].m_NodeId_SchCapCostVarPos[skId];
                                int deltapos = LpData.PathSet[pathid].m_NodeId_DeltaTrainBoard[skId] + q;
                                int deltcongest = FindDeltaCongestionPos(LpData, skId, lineID) + q;

                                switch (PARA.DesignPara.CapType)
                                {
                                    case CapCostType.StepWise:
                                        if (SolveModel)
                                        {
                                            cplex.AddLe(v_SchLinkCap[boardpos], cplex.Sum(CapCostStepWise, cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(1, v_Delta_board_veh[deltapos]))));
                                            cplex.AddGe(v_SchLinkCap[boardpos], cplex.Sum(CapCostStepWise, cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(v_Delta_board_veh[deltapos], 1))));
                                        }
                                        else
                                        {
#if DEBUG
                                            if (cplex.GetValue(v_Delta_board_veh[deltapos]).Equals(1)
                                                && Math.Abs(CapCostStepWiseBoardVal-cplex.GetValue(v_SchLinkCap[boardpos]))>PARA.ZERO)
                                            {
                                                Console.WriteLine("Warning_SchCap: BoardVehStatus = {0}, CapCost = {1} does not equal to CapCostStepWiseBoardVal, which is {2}", cplex.GetValue(v_Delta_board_veh[deltapos]), cplex.GetValue(v_SchLinkCap[boardpos]),
                                                    CapCostStepWiseBoardVal);
                                            }

#endif 
                                        }
                                        break;
                                    case CapCostType.IsNull:
                                        Console.WriteLine("Para.Design.CapCostPara is not set properly");
                                        Console.ReadLine();
                                        break;
                                }
                            }


                            if (ContinousPathSet.Contains(pppID))
                            {
                                int pathid = pppID;
                                int deltapos = LpData.PathSet[pathid].m_NodeId_DeltaTrainBoard[skId] + q;
                                int CapVarPos = LpData.PathSet[pathid].m_NodeId_SchCapCostVarPos[skId];
                                int DeltaSeatPos = FindDeltaSeatPos(LpData, pppID, skId, LpData.PathSet[pppID].m_NodeID_NextLine[skId].ID);
                                Delta_CongPos = FindDeltaCongestionPos(LpData, skId, lineID);
                                if (SolveModel)
                                {
                                    TempExpr = cplex.Diff(v_Delta_board_veh[deltapos], cplex.Sum(v_Delta_Congest[Delta_CongPos], v_Delta_Seat[DeltaSeatPos]));


                                    cplex.Add(cplex.IfThen(cplex.Eq(cplex.Sum(v_Delta_board_veh[deltapos], v_Delta_Seat[DeltaSeatPos]), 2),
                                                                 cplex.Eq(v_SchLinkCap[CapVarPos], CapCostStepWise)));

                                    // else it equals the standing cost
                                    cplex.AddLe(v_SchLinkCap[CapVarPos], cplex.Sum(CapCostStepWise, cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(1, TempExpr))));
                                    cplex.AddGe(v_SchLinkCap[CapVarPos], cplex.Sum(CapCostStepWise, cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(TempExpr, 1))));

                                }
                                else
                                {
                                    double TempVal = cplex.GetValue(v_Delta_board_veh[deltapos]) - (cplex.GetValue(v_Delta_Congest[Delta_CongPos]) + cplex.GetValue(v_Delta_Seat[DeltaSeatPos]));
#if DEBUG                             
                                    if (cplex.GetValue(v_Delta_Seat[DeltaSeatPos]).Equals(1) &&
                                        Math.Abs(cplex.GetValue(v_SchLinkCap[CapVarPos])-CapCostSeatVal)>PARA.ZERO)
                                    {
                                        Console.WriteLine("Warning_SchCap: Board={0}, CongestStatus={1}, SeatStatus={2}, DeltaDif={3}, V_SchCap={4}, SeatCost = {5}",
                                             cplex.GetValue(v_Delta_board_veh[deltapos]), cplex.GetValue(v_Delta_Congest[Delta_CongPos]),
                                        cplex.GetValue(v_Delta_Seat[DeltaSeatPos]), TempVal, cplex.GetValue(v_SchLinkCap[CapVarPos]), CapCostSeatVal);
                                    }
#endif
                                }
                            }
                            if (ContinousPathSet.Contains(pppID) && ActiveBoardPathSet.Contains(pppID))
                            {
                                MyLog.Instance.Debug("The path set classification generation is wrong");
                            }
                        }

                    }// for each train
                }// for each stop
            } // for each line


            for (int p = 0; p < LpData.PathSet.Count; p++)
            {
                List<int> myKeys = LpData.PathSet[p].m_NodeId_SchCapCostVarPos.Keys.ToList();
                for (int i = 0; i < myKeys.Count; i++)
                {
                    if (SolveModel)
                    {
                        int boardPos = LpData.PathSet[p].m_NodeId_SchCapCostVarPos[myKeys[i]];
                        PathCostExpr[p] = cplex.Sum(PathCostExpr[p], cplex.Prod(v_SchLinkCap[boardPos], PARA.PathPara.ConW));
                    }
                }
            }
        }


        /// <summary>
        /// add train depart time express
        /// </summary>
        public static void setTrainDepExpr(Cplex cplex, 
                                           BBmain.LpInput LpData, 
                                           INumVar[] v_TrainTerminalDep,
                                           List<INumExpr> DwellTimeExpr_Sch,
                                           List<INumExpr> TrainDepTimeExpr)
                                      
        {
            INumExpr CumDwellTime = cplex.NumExpr(); // cumulative dwell time
            for (int l = 0;l<LpData.SchLineSet.Count;l++)
            {
                int LineID = LpData.SchLineSet[l].ID;
                for (int q=0;q<LpData.SchLineSet[l].NumOfTrains;q++)
                {
                    int SchLinePos = LpData.m_SchLineId_TrainTerminalDepVarPos[LineID];
                    for (int s=0;s<LpData.SchLineSet[l].Stops.Count-1;s++)
                    {
                        int cnode = LpData.SchLineSet[l].Stops[s].ID;
                        int trainDepIndex = LpData.GetTrainDepIndex(l, s, q);
                        if (trainDepIndex < 0) continue;
                        CumDwellTime = cplex.NumExpr();
                        int ps = 1;  // start to count from the first node
                        while (ps <= s) // while it is at or before the current stop
                        {
                            int psloc = LpData.GetSchDwellExpIndex(LineID, ps, q);
                            CumDwellTime = cplex.Sum(CumDwellTime, DwellTimeExpr_Sch[psloc]);
                            ps++;
                        }
                        double getTimeDif = LpData.SchLineSet[l].getTravelTimeBetweenStop(LpData.SchLineSet[l].Stops[0].ID, cnode);
                        TrainDepTimeExpr[trainDepIndex] = cplex.Sum(v_TrainTerminalDep[SchLinePos + q], cplex.Sum(getTimeDif, CumDwellTime));
                    }
                }
            }
        }
    }
}







