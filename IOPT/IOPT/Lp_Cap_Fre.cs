
using System;
using System.Collections.Generic;
using System.Linq;
using ILOG.Concert;
using ILOG.CPLEX;
using System.Diagnostics;
using IOPT;
using System.IO;

namespace SolveLp
{
    //Capacity constraint for the frequency based line
    //public class LpCap
    public partial class Lp
    {
        /// <summary>
        /// p is path index 0,1,2,3...
        /// l is line order index in the freset of lines
        /// s is tau
        /// </summary>
        /// <param name="p"></param>
        /// <param name="l"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int getPathAlightIndex_Fre(int p, int s)
        {
            // the index should start from 0
            return p * PARA.IntervalSets.Count + s;
        }
        public static double getLineCapforCongest(TransitLineClass l)
        {
            //return 100;
            //double value = 100;
            if (l.ServiceType.Equals(TransitServiceType.Schedule))
                return l.TrainCap[0];
            else if (l.ServiceType.Equals(TransitServiceType.Frequency))
                return l.FreCap;
            else
            {
                Console.WriteLine("Warning: Wrong input for getLineCap");
                Console.ReadLine();
                return 100;
            }    
            ////value = l.TrainCap[0];
            //else if (l.ServiceType.Equals(TransitServiceType.Frequency))
            //{
            //    //// find a number between the maximum and min frequency 
            //    //double AveFre = (60 / PARA.DesignPara.MaxHeadway + 60 / PARA.DesignPara.MinHeadway) / 2;
            //    //double AveCap = AveFre * l.FreCap;  // average capacity for each hour 
            //    ////value = AveCap / (60 / PARA.DesignPara.DurationOfEachInterval);
            //    value = l.FreCap;
            //}
            //return value;
        }
        /// <summary>
        /// add capacity constraint for the frequency based lines
        /// </summary>
        /// <param name="cplex"></param>
        /// <param name="LpData"></param>
        /// <param name="PathFlowExpr"></param>
        /// <param name="PathCostExpr"></param>
        /// <param name="v_Delta_FreDep_t"></param>
        /// <param name="v_Delta_Arr_t"></param>
        /// <param name="v_Ybar_Dep"></param>
        /// <param name="v_Ybar_Arr"></param>
        /// <param name="v_FreCapCost"></param>
        /// <param name="v_PasPathDep"></param>
        /// <param name="v_Fre"></param>
        /// <param name="v_CongestionStatus"></param>
        /// <param name="v_Delta_Seat"></param>
        /// <param name="SolveModel"></param>
        /// <returns></returns>
        public static void FreCapCon(Cplex cplex,
                                    BBmain.LpInput LpData,
                                    List<INumExpr> PathFlowExpr,
                                    List<INumExpr> PathCostExpr,
                                    List<INumExpr> DwellTimeExpr_Fre,
                                    IIntVar[] v_Delta_FreDep_t,
                                    IIntVar[] v_Delta_Arr_t,
                                    INumVar[] v_Ybar_Dep,
                                    INumVar[] v_Ybar_Arr,
                                    INumVar[] v_FreCapCost,
                                    INumVar[] v_PasPathDep,
                                    INumVar[] v_Fre,
                                    INumVar[] v_Delta_Congest,
                                    IIntVar[] v_Delta_Seat,
                                    INumVar[] v_FreDwellTime,
                                    bool SolveModel)

        {
            ///step 1: Initialization
            #region Initialize
            double SumVal = -1;
            int CongStatsPos = -1;
            double TempExprVal = 0;
            int Arr_t_pos = -1;
            double[] StandingFlowVal = new double[PARA.IntervalSets.Count];
            double[] P_boardVal = new double[PARA.IntervalSets.Count];
            double[] P_alightVal = new double[PARA.IntervalSets.Count];
            double[] P_onboardVal = new double[PARA.IntervalSets.Count];
            double[] P_arrVal = new double[PARA.IntervalSets.Count];
            double[] CapCostStandVal = new double[PARA.IntervalSets.Count];
            //double[] CapCostBoardVal = new double[PARA.IntervalSets.Count];
            double[] CapCostSeatVal = new double[PARA.IntervalSets.Count];
            double[] CapCostStepWiseVal = new double[PARA.IntervalSets.Count];
            double[] PathCostVal = new double[PathFlowExpr.Count];
            double[] PathAlightAndTransferVal = new double[PARA.IntervalSets.Count * LpData.PathSet.Count];
            for (int i = 0; i < PARA.IntervalSets.Count; i++)
            {
                P_boardVal[i] = 0;
                P_alightVal[i] = 0;
                P_onboardVal[i] = 0;
                P_arrVal[i] = 0;
                CapCostStandVal[i] = 0;
                //CapCostBoardVal[i] = 0;
                CapCostSeatVal[i] = 0;
                CapCostStepWiseVal[i] = 0;
                StandingFlowVal[i] = 0;
            }

            for (int i = 0; i < PathCostExpr.Count; i++) { PathCostVal[i] = 0; }

            INumExpr rhs = cplex.NumExpr();
            INumExpr lhs = cplex.NumExpr();
            INumExpr sum = cplex.NumExpr();
            INumExpr TempExpr = cplex.NumExpr();


            List<INumExpr> StandingFlow = new List<INumExpr>();
            List<INumExpr> P_board = new List<INumExpr>();
            List<INumExpr> P_alight = new List<INumExpr>();
            List<INumExpr> P_onboard = new List<INumExpr>();
            List<INumExpr> P_arr = new List<INumExpr>();
            List<INumExpr> CapCostStand = new List<INumExpr>();
            //List<INumExpr> CapCostBoard = new List<INumExpr>();
            List<INumExpr> CapCostSeat = new List<INumExpr>();
            List<INumExpr> CapCostStepWise = new List<INumExpr>();

            List<INumExpr> PathAlightAndTransfer = new List<INumExpr>();

            // revised version
            for (int p = 0; p < LpData.PathSet.Count; p++)
            {
                for (int l = 0; l < LpData.FreLineSet.Count; l++)
                {
                    for (int s = 0; s < PARA.IntervalSets.Count; s++)
                        PathAlightAndTransfer.Add(cplex.NumExpr());
                }
            }
            // --------------

            List<int> ActiveBoardPathSet = new List<int>(); // contain the set of path to add capacity constraint
            List<int> ContinousPathSet = new List<int>();  // set of continous path

            for (int s = 0; s < PARA.IntervalSets.Count; s++)
            {
                P_board.Add(cplex.NumExpr());
                P_alight.Add(cplex.NumExpr());
                P_onboard.Add(cplex.NumExpr());
                P_arr.Add(cplex.NumExpr());
                CapCostStand.Add(cplex.NumExpr());
                //CapCostBoard.Add(cplex.NumExpr());
                CapCostSeat.Add(cplex.NumExpr());
                CapCostStepWise.Add(cplex.NumExpr());
                StandingFlow.Add(cplex.NumExpr());
            }

            // The following constraint ensures that each v_depdelta has a value, 
            // otherwise, its value can not be obtained
            // I guess this the due to the definition of the dimension 
            if (SolveModel)
            {
                for (int i = 0; i < v_Ybar_Arr.Length; i++) cplex.AddGe(v_Ybar_Arr[i], 0);
            }
            #endregion
            List<int> ActivePathAlightIndex = new List<int>();
            List<double> v_Ybar_Dep_value = new List<double>();
            List<double> v_Ybar_Arr_value = new List<double>();
            List<double> v_PasPathDep_value = new List<double>();
            List<double> v_Delta_FreDep_t_value = new List<double>();
            List<double> v_Delta_Arr_t_value = new List<double>();
            List<double> v_Delta_Congest_value = new List<double>();
            if (!SolveModel)
            {
                v_Ybar_Dep_value = cplex.GetValues(v_Ybar_Dep).ToList();
                v_Ybar_Arr_value = cplex.GetValues(v_Ybar_Arr).ToList();
                v_PasPathDep_value = cplex.GetValues(v_PasPathDep).ToList();
                v_Delta_FreDep_t_value = cplex.GetValues(v_Delta_FreDep_t).ToList();
                v_Delta_Arr_t_value = cplex.GetValues(v_Delta_Arr_t).ToList();
                v_Delta_Congest_value = cplex.GetValues(v_Delta_Congest).ToList();
            }

            #region DefineDelatAndArrVar_loopPath
            // step 1 define delta arrival variables 
            for (int p = 0; p < LpData.PathSet.Count; p++)
            {
                for (int i = 0; i < LpData.PathSet[p].VisitNodes.Count; i++)
                {
                    int node = LpData.PathSet[p].VisitNodes[i];
                    // step 1: add departure node conservation
                    #region if delta = 1 then y_dep = flow
                    if (LpData.PathSet[p].m_NodeId_DepVarPos.ContainsKey(node))
                    {
                        int DepPos = LpData.PathSet[p].m_NodeId_DepVarPos[node];
                        if (LpData.PathSet[p].m_Delta_FreDep_t_pos.ContainsKey(node))
                        {
                            sum = cplex.IntExpr(); SumVal = 0.0;
                            int Dep_t_pos = LpData.PathSet[p].m_Delta_FreDep_t_pos[node];
                            if (SolveModel)
                            {
                                for (int t = 0; t < (int)PARA.DesignPara.MaxTimeHorizon; t++)
                                {
                                    // it is a bit strange that the "ifthen" syntax in cplex does not work sometimes
                                    lhs = cplex.Diff(v_Ybar_Dep[Dep_t_pos + t], PathFlowExpr[p]);
                                    rhs = cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(v_Delta_FreDep_t[Dep_t_pos + t], 1));
                                    cplex.AddGe(lhs, rhs);
                                    rhs = cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(1, v_Delta_FreDep_t[Dep_t_pos + t]));
                                    cplex.AddLe(lhs, rhs);
                                    cplex.AddLe(v_Ybar_Dep[Dep_t_pos + t], cplex.Prod(PARA.DesignPara.BigM, v_Delta_FreDep_t[Dep_t_pos + t]));
                                }
                            }
                            else
                            {
                                for (int t = 0; t < (int)PARA.DesignPara.MaxTimeHorizon; t++)
                                {
                                    if (cplex.GetValue(v_Delta_FreDep_t[Dep_t_pos + t]).Equals(1))
                                    {
                                        Console.WriteLine("time = {0}, boardflow = {1}",t, cplex.GetValue(v_Ybar_Dep[Dep_t_pos + t]));
                                        if (cplex.GetValue(v_PasPathDep[DepPos]) - t < -PARA.ZERO || cplex.GetValue(v_PasPathDep[DepPos]) - t - 1 > PARA.ZERO)
                                        {
                                            Console.WriteLine("Warning_FreCap: The two number should be less than zero {0}, {1}", cplex.GetValue(v_PasPathDep[DepPos]) - t, t + 1 - cplex.GetValue(v_PasPathDep[DepPos]));
                                        }
                                    }
                                    if (cplex.GetValue(v_Delta_FreDep_t[Dep_t_pos + t]).Equals(1))
                                    {
                                        if (Math.Abs(cplex.GetValue(v_Ybar_Dep[Dep_t_pos + t]) - cplex.GetValue(PathFlowExpr[p])) > PARA.ZERO)
                                            Console.WriteLine("Warning_FreCap: Y_bar_flow != pathflow: Y_bar= {0}, PathFlow = {1}", cplex.GetValue(v_Ybar_Dep[Dep_t_pos + t]), cplex.GetValue(PathFlowExpr[p]));
                                    }
                                    if (cplex.GetValue(v_Delta_FreDep_t[Dep_t_pos + t]).Equals(0))
                                    {
                                        if (cplex.GetValue(v_Ybar_Dep[Dep_t_pos + t]) > PARA.ZERO)
                                        {
                                            Console.WriteLine("Warning_FreCap: v_Delata_Fre_Dep_t = {0}, v_Ybar should be 0, but it now equals to {1}", cplex.GetValue(v_Delta_FreDep_t[Dep_t_pos + t]),
                                                cplex.GetValue(v_Ybar_Dep[Dep_t_pos + t]));
                                        }

                                    }
                                }
                            }
                        }
                    }
                    #endregion
                    // step 2: add arrive definition
                    #region if delta = 1, then y_arr = flow
                    if (node != LpData.PathSet[p].VisitNodes[0])
                    // if it is not the first node
                    {
                        if (LpData.PathSet[p].m_Delta_Arr_t_pos.ContainsKey(node))
                        {
                            Arr_t_pos = LpData.PathSet[p].m_Delta_Arr_t_pos[node];
                            for (int t = 0; t < (int)PARA.DesignPara.MaxTimeHorizon; t++)
                            {
                                if (SolveModel)
                                {
                                    //cplex.Add(cplex.IfThen(cplex.Eq(v_Delta_Arr_t[Arr_t_pos + t], 1),
                                    //    cplex.Eq(v_Ybar_Arr[Arr_t_pos + t], PathFlowExpr[p])));
                                    lhs = cplex.Diff(v_Ybar_Arr[Arr_t_pos + t], PathFlowExpr[p]);
                                    rhs = cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(v_Delta_Arr_t[Arr_t_pos + t], 1));
                                    cplex.AddGe(lhs, rhs);
                                    rhs = cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(1, v_Delta_Arr_t[Arr_t_pos + t]));
                                    cplex.AddLe(lhs, rhs);
                                    cplex.AddLe(v_Ybar_Arr[Arr_t_pos + t], cplex.Prod(PARA.DesignPara.BigM, v_Delta_Arr_t[Arr_t_pos + t]));
                                }
                                else
                                {
                                    if (cplex.GetValue(v_Delta_Arr_t[Arr_t_pos + t]).Equals(0))
                                    {
                                        if (cplex.GetValue(v_Ybar_Arr[Arr_t_pos + t]) > PARA.ZERO)
                                        {
                                            Console.WriteLine("Warning_FreCap: " +
                                                "ArrDelta = {0}, ArrFlow = {1}, if detal = 0, then arrflow should also = 0  ", cplex.GetValue(v_Delta_Arr_t[Arr_t_pos + t]), v_Ybar_Arr[Arr_t_pos + t]);
                                        }
                                    }
                                    if (cplex.GetValue(v_Delta_Arr_t[Arr_t_pos + t]).Equals(1))
                                    {
                                        if (Math.Abs(cplex.GetValue(v_Ybar_Arr[Arr_t_pos + t]) - cplex.GetValue(PathFlowExpr[p])) > PARA.ZERO)
                                        {
                                            //Console.WriteLine("Info_FreCap: p = {0}, node = {1}, time = {2}, flow = {3}", p, node, t, cplex.GetValue(v_Ybar_Arr[Arr_t_pos + t]));
                                            Console.WriteLine("Warnining_FreCap: " + " Delta = 1, but arr flow != path flow" +
                                                "p = {0}, node = {1}, time = {2}, flow = {3}", p, node, t, cplex.GetValue(v_Ybar_Arr[Arr_t_pos + t]));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }
            }
            #endregion

            // Step 2. compute boarding, arrival alighting 

            for (int l = 0; l < LpData.FreLineSet.Count; l++)
            {
                int LineID = LpData.FreLineSet[l].ID;
                for (int k = 0; k < LpData.FreLineSet[l].Stops.Count; k++)
                {
                    int StopKID = LpData.FreLineSet[l].Stops[k].ID;
                    ActiveBoardPathSet.Clear();
                    ContinousPathSet.Clear();

                    #region IniParrBoard
                    // step 2.1 Ini and set value to be zero
                    if (SolveModel)
                    {
                        for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                        {
                            P_arr[tau] = cplex.IntExpr();
                            P_board[tau] = cplex.IntExpr();
                            P_alight[tau] = cplex.IntExpr();
                        }
                    }
                    else
                    {
                        for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                        {
                            P_arrVal[tau] = 0;
                            P_boardVal[tau] = 0;
                            P_alightVal[tau] = 0;
                        }
                    }
                    #endregion
                    // step 2.2. compute arrival and alight loop all paths
                    for (int p = 0; p < LpData.PathSet.Count; p++)
                    {
                        // loop to compute arrival and alight 
                        if (LpData.PathSet[p].VisitNodes.Contains(StopKID))
                        {
                            if (LpData.PathSet[p].m_NodeID_NextLine.ContainsKey(StopKID))
                            {
                                if (!LpData.PathSet[p].TranferNodes.Contains(StopKID) &&
                                    LpData.PathSet[p].m_NodeID_NextLine[StopKID].ID == LineID)
                                {
                                    // this set is created for check seating continuous 
                                    ContinousPathSet.Add(p);
                                }
                            }
                            #region Compute arrival flow  
                            if (LpData.PathSet[p].VisitNodes.Contains(StopKID))
                            {
                                bool isGetArr = false;
                                int FromStop = -1;
                                for (int fs = 0; fs < LpData.PathSet[p].VisitNodes.Count() - 1; fs++)
                                {
                                    if (LpData.PathSet[p].VisitNodes[fs + 1] == StopKID)
                                    {
                                        FromStop = LpData.PathSet[p].VisitNodes[fs];
                                    }
                                }
                                if (FromStop == -1) 
                                    isGetArr = false;
                                else
                                {
                                    if (LpData.PathSet[p].m_NodeID_NextLine[FromStop].ID == LineID)
                                        isGetArr = true;
                                }
                                if (isGetArr)
                                {
                                    Arr_t_pos = LpData.PathSet[p].m_Delta_Arr_t_pos[StopKID];
                                    for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                                    {
                                        if (SolveModel)
                                        {
                                            for (int t = 0; t < PARA.IntervalSets[tau].Count; t++)
                                                P_arr[tau] = cplex.Sum(P_arr[tau], v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
                                        }
                                        else
                                        {
                                            for (int t = 0; t < PARA.IntervalSets[tau].Count; t++)
                                                P_arrVal[tau] = P_arrVal[tau] + cplex.GetValue(v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
                                        }
                                    }
                                }
                                else
                                {
                                    // todo write board pass /// first boarding stop
                                    if (StopKID == LpData.PathSet[p].VisitNodes[0])
                                    {
                                        if (LpData.PathSet[p].m_NodeID_NextLine[StopKID].ID == LineID)
                                        {
                                            ActiveBoardPathSet.Add(p);
                                            int pos;
                                            if (LpData.PathSet[p].m_Delta_FreDep_t_pos.ContainsKey(StopKID))
                                            {
                                                pos = LpData.PathSet[p].m_Delta_FreDep_t_pos[StopKID];
                                            }
                                            else
                                            {
                                                Console.WriteLine(" Lp cap fre issue: does not contain key: p={0}, stop={1}", p, StopKID);
                                                Console.ReadLine();
                                            }
                                            pos = LpData.PathSet[p].m_Delta_FreDep_t_pos[StopKID];
                                            for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                                            {
                                                for (int t = 0; t < PARA.IntervalSets[tau].Count; t++)
                                                {
                                                    if (SolveModel)
                                                    {
                                                        P_board[tau] = cplex.Sum(P_board[tau], v_Ybar_Dep[pos + PARA.IntervalSets[tau][t]]);
                                                    }
                                                    else
                                                    {
                                                        P_boardVal[tau] = P_boardVal[tau] +
                                                            cplex.GetValue(v_Ybar_Dep[pos + PARA.IntervalSets[tau][t]]);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion
                            // obtain alight passengers 
                            if (LpData.PathSet[p].m_NodeID_TransferLine.ContainsKey(StopKID))
                            {
                                #region Compute Alight flow
                                if (LpData.PathSet[p].m_NodeID_TransferLine[StopKID].Alight.ID == LineID)
                                {

                                    // at first node, the alight is zero 
                                    // at last node, the alight does not matter 
                                    if (StopKID != LpData.PathSet[p].VisitNodes[0])
                                    {
                                        Arr_t_pos = LpData.PathSet[p].m_Delta_Arr_t_pos[StopKID];
                                        //Console.WriteLine("p={0}, node = {1}, Arr_t_pos = {2}, line={3}",p, skId, Arr_t_pos,l);
                                        // first express the flow in each intervals
                                        for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                                        {
                                            int plindex = getPathAlightIndex_Fre(p,  tau);
                                            for (int t = 0; t < PARA.IntervalSets[tau].Count; t++)
                                            {
                                                if (SolveModel)
                                                {
                                                    //PathAlight[plindex] = cplex.Sum(PathAlight[plindex], v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
                                                    P_alight[tau] = cplex.Sum(P_alight[tau], v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
                                                }
                                                else
                                                {
                                                    P_alightVal[tau] = P_alightVal[tau] + cplex.GetValue(v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
                                                    //PathAlightVal[plindex] = PathAlightVal[plindex] + cplex.GetValue(v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion
                            }
                        }
                    }

                    // step 2.3. compute passengers alight from other line and transfer to this line
                    for (int i = 0; i < PathAlightAndTransfer.Count; i++) PathAlightAndTransfer[i] = cplex.NumExpr();
                    for (int i = 0; i < PathAlightAndTransferVal.Length; i++) PathAlightAndTransferVal[i] = 0.0;
                    ActivePathAlightIndex.Clear();
                    for (int p = 0; p < LpData.PathSet.Count; p++)
                    {
                        if (LpData.PathSet[p].m_NodeID_TransferLine.ContainsKey(StopKID))
                        {
                            if (LpData.PathSet[p].m_NodeID_TransferLine[StopKID].Board.ID == LineID)
                            {
                                if (StopKID == LpData.PathSet[p].VisitNodes[0]) continue;  // the boarding time at the origin stop dose not count
                                ActiveBoardPathSet.Add(p); // set of path need to add capacity cost
                                Arr_t_pos = LpData.PathSet[p].m_Delta_Arr_t_pos[StopKID];
                                for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                                {
                                    int plindex = getPathAlightIndex_Fre(p, tau);
                                    ActivePathAlightIndex.Add(plindex);
#if DEBUG
                                    Console.WriteLine("build plindex = {0}", plindex);
#endif
                                    if (SolveModel)
                                    {
                                        for (int t = 0; t < PARA.IntervalSets[tau].Count; t++)
                                        {
                                            PathAlightAndTransfer[plindex] = cplex.Sum(PathAlightAndTransfer[plindex], v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
                                            //P_alight[tau] = cplex.Sum(P_alight[tau], v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
                                        }
                                    }
                                    else
                                    {
                                        for (int t = 0; t < PARA.IntervalSets[tau].Count; t++)
                                        {
                                            //P_alightVal[tau] = P_alightVal[tau] + cplex.GetValue(v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
                                            PathAlightAndTransferVal[plindex] = PathAlightAndTransferVal[plindex] + cplex.GetValue(v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // step 2.4 add board flow to P board
                    for (int p = 0; p < LpData.PathSet.Count; p++)
                    {
                        if (SolveModel)
                        {
                            for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                            {
                                int plindex = getPathAlightIndex_Fre(p,  tau);
                                if (ActivePathAlightIndex.Contains(plindex))
                                {
                                    //Console.WriteLine("active plindex = {0}", plindex);
                                    P_board[tau] = cplex.Sum(P_board[tau], PathAlightAndTransfer[plindex]);
                                }
                            }
                        }
                        else
                        {
                            for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                            {
                                int plindex = getPathAlightIndex_Fre(p, tau);
                                if (ActivePathAlightIndex.Contains(plindex))
                                {
                                    P_boardVal[tau] = P_boardVal[tau] + PathAlightAndTransferVal[plindex];
                                }
                            }
                        }
                    }
                    //step 2.5  Define and compute on board flow
                    for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                    {
                        if (SolveModel)
                        {
                            P_onboard[tau] = cplex.Diff(cplex.Sum(P_arr[tau], P_board[tau]), P_alight[tau]);
                            StandingFlow[tau] = cplex.Max(cplex.Diff(P_onboard[tau],
                                cplex.Prod(v_Fre[l], LpData.FreLineSet[l].FreCap * (PARA.IntervalSets[tau].Count))), 0);
#region reviseAddDewllTimeCompute
                            // revised in 2021 Feb
                            int loc = LpData.GetFreDwellExpIndex(LineID, k, tau);
                            if (loc >= 0)
                            {
                                //Console.WriteLine("Line = {0}, s = {1}, loc = {2}", LineID, k, LpData.GetFreDwellExpIndex(LineID, k, tau));
                                DwellTimeExpr_Fre[loc] = cplex.Prod(cplex.Max(P_board[tau], P_alight[tau]),
                                    PARA.DesignPara.BoardAlightTimePerPas / LpData.FreLineSet[l].NumOfDoors);
                                cplex.AddLe(v_FreDwellTime[loc], DwellTimeExpr_Fre[loc]);
                                cplex.AddGe(v_FreDwellTime[loc], DwellTimeExpr_Fre[loc]);
                            }
#endregion
                        }
                        else
                        {
                            P_onboardVal[tau] = P_arrVal[tau] + P_boardVal[tau] - P_alightVal[tau];
                            StandingFlowVal[tau] = Math.Max(P_onboardVal[tau] - cplex.GetValue(v_Fre[l]) * LpData.FreLineSet[l].FreCap * PARA.IntervalSets[tau].Count, 0);

                            int loc = LpData.GetFreDwellExpIndex(LineID, k, tau);
                            if (loc >= 0)
                            {
#if DEBUG
                                Console.WriteLine("Check DwellTime: Line = {0}, s = {1}, loc = {2}, dwell = {3},board={4},alight={5}", LineID, k, LpData.GetFreDwellExpIndex(LineID, k, tau),
                                    Math.Max(P_boardVal[tau], P_alightVal[tau]) * PARA.DesignPara.BoardAlightTimePerPas/LpData.FreLineSet[l].NumOfDoors,
                                   P_boardVal[tau], P_alightVal[tau]);
                                //Console.ReadLine();
#endif
                            }
                        }

                        // define congestion indicator
                        if (PARA.DesignPara.isConsiderSeatSequence)
                        {
                            if (StopKID != LpData.FreLineSet[l].Stops[LpData.FreLineSet[l].Stops.Count - 1].ID)
                            {
                                CongStatsPos = FindDeltaCongestionPos(LpData, StopKID, LineID);
                                CongStatsPos = CongStatsPos + tau;
                                if (SolveModel)
                                {
                                    cplex.AddLe(StandingFlow[tau], cplex.Prod(cplex.Diff(1, v_Delta_Congest[CongStatsPos]), 1000));
                                    cplex.AddLe(cplex.Diff(1, v_Delta_Congest[CongStatsPos]), cplex.Prod(1000, cplex.Sum(StandingFlow[tau], 0.5 * PARA.ZERO)));
                                    //cplex.Add(cplex.IfThen(cplex.Ge(StandingFlow[tau], PARA.GeZero), cplex.Eq(v_Delta_Congest[CongStatsPos], 0)));
                                    //cplex.Add(cplex.IfThen(cplex.Le(StandingFlow[tau], PARA.LeZero), cplex.Eq(v_Delta_Congest[CongStatsPos], 1)));
                                }
                                else
                                {
                                    if (StandingFlowVal[tau] > PARA.ZERO && cplex.GetValue(v_Delta_Congest[CongStatsPos]).Equals(1))
                                        Console.WriteLine("Warining_FreCap: Standingflow = {0}, But CongestionStatus = {1}.", StandingFlowVal[tau], cplex.GetValue(v_Delta_Congest[CongStatsPos]));
                                }

                            }
                        }
                    }

                    if (SolveModel)
                    {
                        if (StopKID != LpData.FreLineSet[l].Stops[LpData.FreLineSet[l].Stops.Count - 1].ID)
                        {
                            int baseCongStatsPos = FindDeltaCongestionPos(LpData, StopKID, LineID);
                            //double DivideCap = 1.0 / getLineCapforCongest(LpData.FreLineSet[l]);
                            for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                            {
                                CongStatsPos = baseCongStatsPos + tau;
                                CapCostSeat[tau] = cplex.Prod(P_onboard[tau], PARA.PathPara.SeatBeta);
                                CapCostStand[tau] = cplex.Prod(PARA.PathPara.StandBeta, StandingFlow[tau]);
                                CapCostStepWise[tau] = cplex.Sum(CapCostSeat[tau], 
                                    cplex.Sum(CapCostStand[tau],
                                    cplex.Prod(PARA.PathPara.StandConstant, cplex.Diff(1, v_Delta_Congest[CongStatsPos]))));

                                //CapCostSeat[tau] = cplex.Prod(CapCostSeat[tau], DivideCap);

                                //CapCostBoard[tau] = cplex.Prod(PARA.PathPara.BoardAlpha, P_board[tau]);
                                //CapCostBoard[tau] = cplex.Prod(CapCostBoard[tau], DivideCap);

                                //CapCostStand[tau] = cplex.Sum(CapCostSeat[tau],
                                //    cplex.Sum(cplex.Prod(PARA.PathPara.StandBeta, cplex.Prod(StandingFlow[tau], DivideCap)),
                                //              cplex.Prod(PARA.PathPara.StandConstant, cplex.Diff(1, v_Delta_Congest[CongStatsPos]))));

                                //CapCostStand[tau] = cplex.Sum(CapCostSeat[tau],
                                //     cplex.Sum(cplex.Prod(PARA.PathPara.StandBeta, cplex.Prod(StandingFlow[tau], DivideCap)),
                                //       cplex.Prod(PARA.PathPara.StandConstant, cplex.Diff(1, v_Delta_Congest[CongStatsPos]))));

                                //CapCostStepWiseBoard[tau] = cplex.Sum(CapCostBoard[tau], CapCostStand[tau]);
                            }
                        }
                    }
                    else
                    {
                        if (StopKID != LpData.FreLineSet[l].Stops[LpData.FreLineSet[l].Stops.Count - 1].ID)
                        {
                            int baseCongStatsPos = FindDeltaCongestionPos(LpData, StopKID, LineID);
                            //double DivideCap = 1.0 / getLineCapforCongest(LpData.FreLineSet[l]);
                            for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                            {
                                CongStatsPos = baseCongStatsPos + tau;
                                CapCostSeatVal[tau] = P_onboardVal[tau] * PARA.PathPara.SeatBeta;
                                CapCostStandVal[tau] =  PARA.PathPara.StandBeta * StandingFlowVal[tau] ;
                                //CapCostSeatVal[tau] = P_onboardVal[tau] * PARA.PathPara.SeatBeta * DivideCap;
                                //CapCostBoardVal[tau] = PARA.PathPara.BoardAlpha * P_boardVal[tau] * DivideCap;
                                //CapCostStandVal[tau] = CapCostSeatVal[tau] + PARA.PathPara.StandBeta * StandingFlowVal[tau] * DivideCap;
                                if (StandingFlowVal[tau] >= PARA.GeZero)
                                {
                                    CapCostStandVal[tau] += PARA.PathPara.StandConstant;
                                }
                                CapCostStepWiseVal[tau] = CapCostSeatVal[tau] + CapCostStandVal[tau];

                                //CapCostStepWiseBoardVal[tau] = CapCostBoardVal[tau] + CapCostStandVal[tau];
                                //Console.WriteLine("Info_FreCap:Line = {0}, BoardNode = {1}, Tau = {2}, Onboard = {3}, Stand = {4}, SeatCost = {5}, StandCost = {6}",
                                //    LineID, StopKID, tau,
                                //    P_onboardVal[tau], StandingFlowVal[tau], CapCostSeatVal[tau], CapCostStandVal[tau]);
                            }
                        }
                    }

                    // step 2.6 compute capacity cost for each path 
                    for (int pppID = 0; pppID < LpData.PathSet.Count; pppID++)
                    {
                        if (ActiveBoardPathSet.Contains(pppID))
                        {
                            int PathId = pppID;
                            int DeltaPos = LpData.PathSet[PathId].m_Delta_FreDep_t_pos[StopKID];
                            int CapPos = LpData.PathSet[PathId].m_NodeId_FreCapCostVarPos[StopKID];
                            for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                            {
                                if (SolveModel)
                                {
                                    sum = cplex.IntExpr();
                                    for (int tt = 0; tt < PARA.IntervalSets[tau].Count; tt++)
                                    {
                                        sum = cplex.Sum(sum, v_Delta_FreDep_t[DeltaPos + PARA.IntervalSets[tau][tt]]);
                                    }

                                    switch (PARA.DesignPara.CapType)
                                    {
                                        case CapCostType.StepWise:
                                            cplex.Add(cplex.IfThen(cplex.Eq(sum, 1),
                                                cplex.Eq(v_FreCapCost[CapPos], CapCostStepWise[tau])));

                                            cplex.AddLe(v_FreCapCost[CapPos], cplex.Sum(CapCostStepWise[tau], cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(1, sum))));
                                            cplex.AddGe(v_FreCapCost[CapPos], cplex.Sum(CapCostStepWise[tau], cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(sum, 1))));

                                            break;
                                        case CapCostType.IsNull:
                                            Console.WriteLine("Warning_FreCap: Should not use CapCostType.StandOnly");
                                            Console.WriteLine("Para.Design.CapCostPara is not set properly");
                                            Console.ReadLine();
                                            break;
                                    }
                                }
                                else
                                {
                                    SumVal = 0;
                                    for (int tt = 0; tt < PARA.IntervalSets[tau].Count; tt++)
                                        SumVal += cplex.GetValue(v_Delta_FreDep_t[DeltaPos + PARA.IntervalSets[tau][tt]]);
                                    switch (PARA.DesignPara.CapType)
                                    {
                                        case CapCostType.StepWise:
                                            if (SumVal == 1 &&
                                               Math.Abs(cplex.GetValue(v_FreCapCost[CapPos]) - CapCostStepWiseVal[tau]) > PARA.ZERO)
                                            {
                                                Console.WriteLine("Warning_FreCap if {0} = 1, then {1} = {2}, path = {3}, node = {4}, File: Lp_Cap_Fre.cpp", SumVal,
                                                    cplex.GetValue(v_FreCapCost[CapPos]), CapCostStepWiseVal[tau], PathId, StopKID);
                                                Console.WriteLine("Seating flow = {0}, Standing flow = {1}", P_onboardVal[tau], StandingFlowVal[tau]);
                                                Console.WriteLine("congestion status ={0}, congest pos ={1}", cplex.GetValue(v_Delta_Congest[CongStatsPos]), CongStatsPos);
                                                Console.ReadLine();
                                            }

                                            break;

                                        case CapCostType.IsNull:
                                            Console.WriteLine("Warning: Para.Design.CapCostPara is not set properly");
                                            break;
                                    }
                                }
                            }
                        }
                        if (ContinousPathSet.Contains(pppID))
                        {
                            int PathId = pppID;
                            int DeltaPos = LpData.PathSet[PathId].m_Delta_FreDep_t_pos[StopKID];
                            int CapPos = LpData.PathSet[PathId].m_NodeId_FreCapCostVarPos[StopKID];
                            int SeatLamdaPos = FindDeltaSeatPos(LpData, pppID, StopKID, LpData.PathSet[pppID].m_NodeID_NextLine[StopKID].ID);
                            CongStatsPos = FindDeltaCongestionPos(LpData, StopKID, LineID);
                            Debug.Assert(LpData.PathSet[pppID].m_NodeID_NextLine[StopKID].ID == LineID);

                            if (SolveModel)
                            {
                                for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                                {
                                    sum = cplex.IntExpr();
                                    for (int tt = 0; tt < PARA.IntervalSets[tau].Count; tt++)
                                    {
                                        sum = cplex.Sum(sum, v_Delta_FreDep_t[DeltaPos + PARA.IntervalSets[tau][tt]]);
                                    }
                                    TempExpr = cplex.IntExpr();
                                    TempExpr = cplex.Diff(sum, v_Delta_Congest[CongStatsPos]);
                                    TempExpr = cplex.Diff(TempExpr, v_Delta_Seat[SeatLamdaPos]);


                                    cplex.Add(cplex.IfThen(cplex.Eq(cplex.Sum(sum, v_Delta_Seat[SeatLamdaPos]), 2), 
                                        cplex.Eq(v_FreCapCost[CapPos], CapCostStepWise[tau])));

                                    cplex.AddLe(v_FreCapCost[CapPos], cplex.Sum(CapCostStepWise[tau], cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(1, TempExpr))));
                                    cplex.AddGe(v_FreCapCost[CapPos], cplex.Sum(CapCostStepWise[tau], cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(TempExpr, 1))));


                                    //cplex.Add(cplex.IfThen(cplex.Eq(cplex.Sum(sum, v_Delta_Seat[SeatLamdaPos]), 2),
                                    //    cplex.Eq(v_FreCapCost[CapPos], CapCostSeat[tau])));

                                    //cplex.AddLe(v_FreCapCost[CapPos], cplex.Sum(CapCostStand[tau], cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(1, TempExpr))));
                                    //cplex.AddGe(v_FreCapCost[CapPos], cplex.Sum(CapCostStand[tau], cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(TempExpr, 1))));

                                    //cplex.AddGe(v_FreCapCost[CapPos], 0);
                                    //cplex.Add(cplex.IfThen(cplex.Eq(TempExpr, 1), cplex.Eq(v_FreCapCost[CapPos], CapCostStand[tau])));
                                    //cplex.Add(cplex.IfThen(cplex.Eq(TempExpr, 0), cplex.Le(v_FreCapCost[CapPos], 0)));
                                    //cplex.Add(cplex.IfThen(cplex.Eq(TempExpr, -1), cplex.Le(v_FreCapCost[CapPos], 0)));
                                    //cplex.Add(cplex.IfThen(cplex.Eq(TempExpr, -2), cplex.Le(v_FreCapCost[CapPos], 0)));
                                }
                            }
                            else
                            {
                                for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                                {
                                    SumVal = 0;
                                    for (int tt = 0; tt < PARA.IntervalSets[tau].Count; tt++)
                                    {
                                        SumVal += cplex.GetValue(v_Delta_FreDep_t[DeltaPos + PARA.IntervalSets[tau][tt]]);
                                    }

                                    TempExprVal = SumVal - cplex.GetValue(v_Delta_Congest[CongStatsPos]) - cplex.GetValue(v_Delta_Seat[SeatLamdaPos]);

                                    if (TempExprVal.Equals(1))
                                    {
                                        if (Math.Abs(cplex.GetValue(v_FreCapCost[CapPos]) - CapCostStandVal[tau]) > PARA.ZERO)
                                        {
                                            Console.WriteLine("Warning_FreCap: FreCapCost = {0} should equal CapCost = {1}", cplex.GetValue(v_FreCapCost[CapPos]), CapCostStand[tau]);
                                        }
                                    }

                                }
                            }
                        }
                        if (ActiveBoardPathSet.Contains(pppID) &&
                            ContinousPathSet.Contains(pppID))
                        {
                            Console.WriteLine("Warning_FreCap: the path set classification generation is wrong");
                        }
                    }
                }
            }

            for (int p = 0; p < LpData.PathSet.Count; p++)
            {
                List<int> myKeys = LpData.PathSet[p].m_NodeId_FreCapCostVarPos.Keys.ToList();
                for (int i = 0; i < myKeys.Count; i++)
                {
                    int NowLamdaPos = FindDeltaSeatPos(LpData, p, myKeys[i], LpData.PathSet[p].m_NodeID_NextLine[myKeys[i]].ID);
                    int boardPos = LpData.PathSet[p].m_NodeId_FreCapCostVarPos[myKeys[i]];

                    if (SolveModel)
                    {
                        //PathCostExpr[p] = cplex.Sum(PathCostExpr[p], v_FreCapCost[boardPos]);
                        PathCostExpr[p] = cplex.Sum(PathCostExpr[p], cplex.Prod(v_FreCapCost[boardPos], PARA.PathPara.ConW));
                    }
                    else
                    {
                        PathCostVal[p] += cplex.GetValue(v_FreCapCost[boardPos]);
                    }
                }
            }
        }
    }
} //namespace SolveLp




//    for (int p = 0; p < LpData.PathSet.Count; p++)
//{
//    // block to compute the board
//    if (LpData.PathSet[p].m_NodeID_TransferLine.ContainsKey(StopKID))
//    {
//        if (LpData.PathSet[p].m_NodeID_TransferLine[StopKID].Board.ID == LineID)
//        {
//            ActiveBoardPathSet.Add(p); // set of path need to add capacity cost
//            for (int fl =0;fl<LpData.FreLineSet.Count;fl++ ) // from line
//            {
//                if (fl == l) continue;
//                if (StopKID == LpData.PathSet[p].VisitNodes[0]) continue;
//                Arr_t_pos = LpData.PathSet[p].m_Delta_Arr_t_pos[StopKID];
//                for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
//                {
//                    int plindex = getPathAlightIndex(p, l, tau, LpData.FreLineSet.Count);
//                    for (int t = 0; t < PARA.IntervalSets[tau].Count; t++)
//                    {
//                        if (SolveModel)
//                        {
//                            PathAlight[plindex] = cplex.Sum(PathAlight[plindex], v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
//                            //P_alight[tau] = cplex.Sum(P_alight[tau], v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
//                        }
//                        else
//                        {
//                            P_alightVal[tau] = P_alightVal[tau] + cplex.GetValue(v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
//                            //PathAlightVal[plindex] = PathAlightVal[plindex] + cplex.GetValue(v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
//                        }
//                    }
//                }
//            }

//if (SolveModel)
//            {
//                for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
//                {
//                    int plindex = getPathAlightIndex(p, l, tau,LpData.FreLineSet.Count);
//                    P_board[tau] = cplex.Sum(P_board[tau], PathAlight[plindex]);
//                        //P_board[tau] = cplex.Sum(P_board[tau], v_Ybar_Dep[pos + PARA.IntervalSets[tau][t]]);
//                }
//            }
//            else
//            {
//                for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
//                {
//                    int plindex = getPathAlightIndex(p, l, tau, LpData.FreLineSet.Count);
//                    P_boardVal[tau] = P_boardVal[tau] + PathAlightVal[plindex];
//                }
//            }

//        }
//    }


//#region compute board flow
//if (LpData.PathSet[p].m_NodeID_TransferLine[StopKID].Board.ID == LineID)
//{
//    ActiveBoardPathSet.Add(p); // set of path need to add capacity cost
//    int pos = LpData.PathSet[p].m_Delta_FreDep_t_pos[StopKID];
//    // first express the flow in each intervals
//    if (SolveModel)
//    {
//        for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
//        {
//            for (int t = 0; t < PARA.IntervalSets[tau].Count; t++)
//            {
//                P_board[tau] = cplex.Sum(P_board[tau], v_Ybar_Dep[pos + PARA.IntervalSets[tau][t]]);
//            }
//        }
//    }
//    else
//    {
//        for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
//        {
//            for (int t = 0; t < PARA.IntervalSets[tau].Count; t++)
//            {
//                P_boardVal[tau] = P_boardVal[tau] +
//                    cplex.GetValue(v_Ybar_Dep[pos + PARA.IntervalSets[tau][t]]);
//            }
//        }
//    }
//}
//#endregion
//}


//for (int fl = 0; fl < LpData.FreLineSet.Count; fl++) // from line
//{
//    if (fl == l) continue;
//    for (int p = 0; p < LpData.PathSet.Count; p++)
//    {
//        if (LpData.PathSet[p].m_NodeID_TransferLine.ContainsKey(StopKID))
//        {
//            if (LpData.PathSet[p].m_NodeID_TransferLine[StopKID].Board.ID == LineID)
//            {
//                if (StopKID == LpData.PathSet[p].VisitNodes[0]) continue;  // the boarding time at the origin stop dose not count
//                ActiveBoardPathSet.Add(p); // set of path need to add capacity cost
//                Arr_t_pos = LpData.PathSet[p].m_Delta_Arr_t_pos[StopKID];
//                for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
//                {
//                    int plindex = getPathAlightIndex(p,  tau);
//                    ActivePathAlightIndex.Add(plindex);


//                    Console.WriteLine("build plindex = {0}", plindex);
//                    if (SolveModel)
//                    {
//                        for (int t = 0; t < PARA.IntervalSets[tau].Count; t++)
//                        {
//                            PathAlightAndTransfer[plindex] = cplex.Sum(PathAlightAndTransfer[plindex], v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
//                            //P_alight[tau] = cplex.Sum(P_alight[tau], v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
//                        }

//                    }
//                    else
//                    {
//                        for (int t = 0; t < PARA.IntervalSets[tau].Count; t++)
//                        {
//                            //P_alightVal[tau] = P_alightVal[tau] + cplex.GetValue(v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
//                            PathAlightValAndTransfer[plindex] = PathAlightValAndTransfer[plindex] + cplex.GetValue(v_Ybar_Arr[Arr_t_pos + PARA.IntervalSets[tau][t]]);
//                        }

//                    }

//                }
//            }
//        }
//    }
//}
