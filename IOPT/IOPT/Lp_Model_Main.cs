/// checked 2021-May
using System;
using System.Collections.Generic;
using System.Linq;
using ILOG.Concert;
using ILOG.CPLEX;
using IOPT;
using System.Diagnostics;

namespace SolveLp
{
    public partial class Lp
    {
        protected internal Cplex cplex { get; set; }
        protected internal INumVar[] v_Fre { get; set; }
        protected internal INumVar[] v_RelaxObj { get; set; }
        protected internal INumVar[] v_PathPie { get; set; }
        protected internal IIntVar[] v_Delta_Board_Veh { get; set; }
        protected internal IIntVar[] v_Delta_FreDep_t { get; set; }
        protected internal IIntVar[] v_Delta_FreArr_t { get; set; }
        protected internal INumVar[] v_Y { get; set; }
        protected internal INumVar[] v_Ybar_Dep { get; set; }
        protected internal INumVar[] v_Ybar_Arr { get; set; }
        protected internal INumVar[] v_PathProb { get; set; }
        protected internal INumVar[] v_PathSchCapCost { get; set; }
        protected internal INumVar[] v_PathFreCapCost { get; set; }
        protected internal INumVar[] v_LnProb { get; set; }
        protected internal INumVar[] v_Headway { get; set; }
        protected internal INumVar[] v_Bilinear_w { get; set; }
        protected internal INumVar[] v_PasPathDep { get; set; }
        protected internal INumVar[] v_PasPathArr { get; set; }
        protected internal INumVar[] v_PasPathWait { get; set; }
        protected internal INumVar[] v_TrainTerminalDep { get; set; }
        protected internal INumVar[] v_LnProb_Lb { get; set; }
        protected internal IIntVar[][] v_LnProb_BigA { get; set; }
        protected internal INumVar[][] v_LnProb_u { get; set; }
        protected internal IIntVar[] v_LnProb_J { get; set; }
        protected internal List<INumExpr> PathCostExpr { get; set; }
        protected internal List<INumExpr> DwellTimeExpr_Sch { get; set; }
        protected internal List<INumExpr> DwellTimeExpr_Fre { get; set; }
        protected internal List<INumExpr> TrainDepTimeExpr { get; set; }
        protected internal INumVar[] v_FreDwellTime { get; set; }  // dwell time associated with each line stop
        protected internal INumVar[] v_PathDwell { get; set; }
        protected internal List<INumExpr> PathFlowExpr { get; set; }
        protected internal List<IRange> BilinearRange { get; set; }
        protected internal INumVar[] v_Bcm_m { get; set; }
        protected internal INumVar[] v_Bcm_m_Lb { get; set; }
        protected internal INumVar[] v_Bcm_z { get; set; }
        protected internal INumVar[] v_Bcm_z_Lb { get; set; }
        protected internal INumVar[] v_Bcm_y { get; set; }
        protected internal INumVar[] v_Bcm_one_plus_y { get; set; }
        protected internal IIntVar[][] v_Bcm_LnZ_BigA { get; set; }
        protected internal IIntVar[][] v_Bcm_LnZ_u { get; set; } //{0,1}
        protected internal IIntVar[][] v_Bcm_LnM_BigA { get; set; }
        protected internal INumVar[][] v_Bcm_LnM_u { get; set; } // {0,1}
        protected internal IIntVar[] v_Bcm_LnZ_J { get; set; }
        protected internal IIntVar[] v_Bcm_LnM_J { get; set; }
        protected internal IIntVar[] v_Delta_Seat { get; set; }
        protected internal IIntVar[] v_Delta_Congest { get; set; }  // a binary indicator to indicate whether a section is congested or not
        protected internal int xi { get; set; } // defined associated with number of break points int xi = (int)Math.Ceiling(Math.Log(LpData.NumOfBreakPoints - 1, 2));

        /// <summary>
        /// Initialize variables with LpData
        /// </summary>
        protected internal Lp(BBmain.LpInput LpData)
        {
            // step 0: define decision variable names
            #region define and ini variable name strings
            string[] PathPieVarName = new string[LpData.NumOfPath];
            string[] PathCapName = new string[LpData.NumOfPath];
            string[] PathProbVarName = new string[LpData.NumOfPath];
            string[] RelaxObjName = new string[LpData.NumOfPath];
            string[] TrainDepName = new string[LpData.TotalNumOfTrains];
            string[] DeltaVarName = new string[LpData.DeltaPathTrainPos];
            string[] PasPathDepName = new string[LpData.DepVarPos];
            string[] PasPathArrName = new string[LpData.ArrVarPos];
            string[] PasPathWaitName = new string[LpData.WaitVarPos];
            string[] FreVarName = new string[LpData.NumFreLines];
            string[] HeadwayVarName = new string[LpData.NumFreLines];
            string[] SchCapName = new string[LpData.Node_SchCapPos];
            string[] FreCapName = new string[LpData.Node_FreCapPos];
            string[] v_Delta_SeatName = new string[LpData.LineSec_Delta_SeatPos];
            string[] v_Delta_CongestName = new string[LpData.CongStausPos];
            for (int i = 0; i < v_Delta_SeatName.Length; ++i) v_Delta_SeatName[i] = "deltaSeat" + i.ToString();
            for (int i = 0; i < v_Delta_CongestName.Length; ++i) v_Delta_CongestName[i] = "deltaCongest" + i.ToString();
            for (int i = 0; i < LpData.Node_SchCapPos; i++) SchCapName[i] = "schcap" + i.ToString();
            for (int i = 0; i < LpData.DepVarPos; i++) PasPathDepName[i] = "pd" + i.ToString();
            for (int i = 0; i < LpData.ArrVarPos; i++) PasPathArrName[i] = "pa" + i.ToString();
            for (int i = 0; i < LpData.WaitVarPos; i++) PasPathWaitName[i] = "pw" + i.ToString();
            for (int p = 0; p < LpData.DeltaPathTrainPos; p++) DeltaVarName[p] = "dta" + p.ToString();
            for (int p = 0; p < LpData.TotalNumOfTrains; p++) TrainDepName[p] = "td" + p.ToString();
            for (int i = 0; i < LpData.Node_FreCapPos; i++) FreCapName[i] = "frecap" + i.ToString();


            for (int i = 0; i < LpData.NumOfPath; i++)
            {
                RelaxObjName[i] = "z" + i.ToString(); PathPieVarName[i] = "pie" + i.ToString();
                PathProbVarName[i] = "prob" + i.ToString(); PathCapName[i] = "cap" + i.ToString();
            }
            for (int l = 0; l < LpData.NumFreLines; l++)
            {
                FreVarName[l] = "fl_" + l.ToString();
                HeadwayVarName[l] = "hl_" + l.ToString();
            }
            #endregion
            cplex = new Cplex();
            double[] RelaxObjUpperBound = new double[LpData.NumOfPath];
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                RelaxObjUpperBound[p] = LpData.PathSet[p].Trip.MaxPie * LpData.PathSet[p].Trip.Demand;
            }
            v_RelaxObj = cplex.NumVarArray(LpData.NumOfPath, 0, float.MaxValue, NumVarType.Float, RelaxObjName);

            for (int p = 0; p < LpData.NumOfPath; p++) cplex.AddLe(v_RelaxObj[p], RelaxObjUpperBound[p]);

            v_PathPie = cplex.NumVarArray(LpData.NumOfPath, 0, 100, NumVarType.Float, PathPieVarName);
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                cplex.AddLe(v_PathPie[p], LpData.getMaxPieOfPath(p));
                cplex.AddGe(v_PathPie[p], LpData.getMinPieOfPath(p));
            }

            v_Delta_Board_Veh = cplex.IntVarArray(LpData.DeltaPathTrainPos, 0, 1);
            v_Delta_FreDep_t = cplex.IntVarArray(LpData.Delta_FreDep_t_Pos, 0, 1);
            v_Delta_FreArr_t = cplex.IntVarArray(LpData.Delta_FreArr_t_Pos, 0, 1);
            v_PathDwell = cplex.NumVarArray(LpData.GetPathDwellDemension(), 0.0, 5.0, NumVarType.Float);
            v_FreDwellTime = cplex.NumVarArray(LpData.GetFreDwellDimension(), 0.0, 5.0, NumVarType.Float);
            v_Y = cplex.NumVarArray(LpData.DeltaPathTrainPos, 0, LpData.MaxDemand, NumVarType.Float);
            v_Y = cplex.NumVarArray(LpData.DeltaPathTrainPos, 0, LpData.MaxDemand, NumVarType.Float);
            v_Ybar_Dep = cplex.NumVarArray(LpData.Delta_FreDep_t_Pos, 0, LpData.MaxDemand, NumVarType.Float);
            v_Ybar_Arr = cplex.NumVarArray(LpData.Delta_FreArr_t_Pos, 0, LpData.MaxDemand, NumVarType.Float);
            v_PathProb = cplex.NumVarArray(LpData.NumOfPath, PARA.ZERO, 1, NumVarType.Float, PathProbVarName);
            v_PathSchCapCost = cplex.NumVarArray(LpData.Node_SchCapPos, 0, 10.0, NumVarType.Float, SchCapName);
            v_PathFreCapCost = cplex.NumVarArray(LpData.Node_FreCapPos, 0, 10.0, NumVarType.Float, FreCapName);
            v_Fre = cplex.NumVarArray(LpData.NumFreLines, 1 / (PARA.DesignPara.MaxHeadway + 0.01), 1 / (PARA.DesignPara.MinHeadway - 0.01), FreVarName);
            v_Headway = cplex.NumVarArray(LpData.NumFreLines, PARA.DesignPara.MinHeadway - 0.01, PARA.DesignPara.MaxHeadway + 0.01, HeadwayVarName);
            v_Bilinear_w = cplex.NumVarArray(LpData.NumFreLines, -1000, 1000, HeadwayVarName);
            v_PasPathDep = cplex.NumVarArray(LpData.DepVarPos, 0, PARA.DesignPara.MaxTimeHorizon, PasPathDepName);
            v_PasPathArr = cplex.NumVarArray(LpData.ArrVarPos, 0, PARA.DesignPara.MaxTimeHorizon, PasPathArrName);
            v_PasPathWait = cplex.NumVarArray(LpData.WaitVarPos, 0, PARA.DesignPara.MaxHeadway, PasPathWaitName);
            v_Delta_Seat = cplex.BoolVarArray(LpData.LineSec_Delta_SeatPos, v_Delta_SeatName);
            v_Delta_Congest = cplex.BoolVarArray(LpData.CongStausPos, v_Delta_CongestName);
            v_TrainTerminalDep = cplex.NumVarArray(LpData.TotalNumOfTrains, 0, PARA.DesignPara.MaxTimeHorizon, NumVarType.Float, TrainDepName);
            v_LnProb = cplex.NumVarArray(LpData.NumOfPath, float.MinValue, 0, NumVarType.Float);
            v_LnProb_Lb = cplex.NumVarArray(LpData.NumOfPath, float.MinValue, 0);
            v_LnProb_BigA = new IIntVar[LpData.NumOfPath][];
            v_LnProb_u = new IIntVar[LpData.NumOfPath][];
            v_LnProb_J = cplex.IntVarArray(LpData.NumOfPath, 0, PARA.DesignPara.NumOfBreakPoints - 1);
            v_Bcm_LnM_BigA = new IIntVar[LpData.NumOfPath][];
            v_Bcm_LnM_u = new IIntVar[LpData.NumOfPath][];
            v_Bcm_LnM_J = cplex.IntVarArray(LpData.NumOfPath, 0, PARA.DesignPara.NumOfBreakPoints - 1);
            v_Bcm_LnZ_BigA = new IIntVar[LpData.NumOfPath][];
            v_Bcm_LnZ_u = new IIntVar[LpData.NumOfPath][];
            v_Bcm_LnZ_J = cplex.IntVarArray(LpData.NumOfPath, 0, PARA.DesignPara.NumOfBreakPoints - 1);
            DwellTimeExpr_Sch = new List<INumExpr>();
            DwellTimeExpr_Fre = new List<INumExpr>();
            TrainDepTimeExpr = new List<INumExpr>();

            for (int i = 0; i < LpData.GetSchDewllDimension(); i++) { DwellTimeExpr_Sch.Add(cplex.NumExpr()); }
            for (int i = 0; i < LpData.GetFreDwellDimension(); i++) { DwellTimeExpr_Fre.Add(cplex.NumExpr()); }
            for (int i = 0; i < LpData.GetTrainDepDemsion(); i++) { TrainDepTimeExpr.Add(cplex.NumExpr()); }
            PathCostExpr = new List<INumExpr>();
            PathFlowExpr = new List<INumExpr>();
            BilinearRange = new List<IRange>();
            xi = (int)Math.Ceiling(Math.Log(PARA.DesignPara.NumOfBreakPoints - 1, 2));
            string[] m_name = new string[LpData.NumOfPath];
            string[] m_Lb_name = new string[LpData.NumOfPath];
            string[] z_name = new string[LpData.NumOfPath];
            string[] z_Lb_name = new string[LpData.NumOfPath];
            string[] y_name = new string[LpData.NumOfPath];
            string[] y_p_name = new string[LpData.NumOfPath];
            string[] y_Lb_name = new string[LpData.NumOfPath];

            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                m_name[p] = "m_" + p.ToString();
                m_Lb_name[p] = "m_Lb" + p.ToString();
                z_name[p] = "z_" + p.ToString();
                z_Lb_name[p] = "z_Lb" + p.ToString();
                y_name[p] = "y_" + p.ToString();
                y_p_name[p] = "(y+1)_" + p.ToString();
                y_Lb_name[p] = "(y+1)_Lb_" + p.ToString();
            }
            v_Bcm_m = cplex.NumVarArray(LpData.NumOfPath, -100, 100, NumVarType.Float, m_name);
            v_Bcm_m_Lb = cplex.NumVarArray(LpData.NumOfPath, -100, 100, NumVarType.Float, m_Lb_name);
            v_Bcm_z = cplex.NumVarArray(LpData.NumOfPath, -100, 100, NumVarType.Float, z_name);
            v_Bcm_z_Lb = cplex.NumVarArray(LpData.NumOfPath, -100, 100, NumVarType.Float, z_Lb_name);
            v_Bcm_y = cplex.NumVarArray(LpData.NumOfPath, -100, 100, NumVarType.Float, y_name);
            v_Bcm_one_plus_y = cplex.NumVarArray(LpData.NumOfPath, -100, 100, NumVarType.Float, y_p_name);
        }

        /// <summary>
        /// formulate path cost with out adding capacity constraints
        /// </summary>
        protected internal void GetPathCostExpr_withoutCapCost(BBmain.LpInput LpData,
                                                List<INumExpr> DwellTimeExpr_Sch,
                                                List<INumExpr> DwellTimeExpr_Fre)
        {
            INumExpr Rhs = cplex.NumExpr(), Lhs = cplex.NumExpr();
            for (int i = 0; i < v_FreDwellTime.Length; i++) cplex.Add(cplex.Ge(v_FreDwellTime[i], 0.0));
            for (int i = 0; i < v_PathDwell.Length; i++) cplex.Add(cplex.Ge(v_PathDwell[i], 0.0)); // define dwell time
            IIntExpr _sum = cplex.IntExpr();
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                // add in vehicle travel cost 
                PathCostExpr[p] = cplex.Sum(PathCostExpr[p], LpData.PathSet[p].m_VehType_InVehTime[TransitVehicleType.Bus] * PARA.PathPara.InVBusW);
                PathCostExpr[p] = cplex.Sum(PathCostExpr[p], LpData.PathSet[p].m_VehType_InVehTime[TransitVehicleType.Metro] * PARA.PathPara.InVMetroW);
                PathCostExpr[p] = cplex.Sum(PathCostExpr[p], LpData.PathSet[p].m_VehType_InVehTime[TransitVehicleType.S_Train] * PARA.PathPara.InVSTrainW);
                PathCostExpr[p] = cplex.Sum(PathCostExpr[p], LpData.PathSet[p].m_VehType_InVehTime[TransitVehicleType.Train] * PARA.PathPara.InVTrainW);
                /// add transfer cost
                /// The list "tranfer_nodes" include the boarding and terminal stops
                int NumOfTranfer = LpData.PathSet[p].TranferNodes.Count - 2;
                PathCostExpr[p] = cplex.Sum(PathCostExpr[p], NumOfTranfer * PARA.PathPara.TransferPenalty);
                /// add waiting cost expression
                for (int n = 0; n < LpData.PathSet[p].VisitNodes.Count() - 1; n++)
                {
                    int CurrentNode = LpData.PathSet[p].VisitNodes[n];
                    ///<remarks>
                    ///waiting cost only applicable to the transfer nodes
                    ///include the time at the first stop
                    ///</remarks>
                    if (LpData.PathSet[p].TranferNodes.Contains(CurrentNode))
                    {
                        int WaitPos = LpData.PathSet[p].m_NodeId_WaitVarPos[CurrentNode];
                        int DepPos = LpData.PathSet[p].m_NodeId_DepVarPos[CurrentNode];
                        int ArrPos = LpData.PathSet[p].m_NodeId_ArrVarPos[CurrentNode];
                        if (n == 0)
                        {
                            // At origin node, path arrival time equal the demand departure time
                            cplex.AddEq(v_PasPathArr[ArrPos], LpData.PathSet[p].TargetDepTime, "ParrArrTime = DemandTime_" + p.ToString());
                        }
                        cplex.AddEq(v_PasPathWait[WaitPos], cplex.Diff(v_PasPathDep[DepPos], v_PasPathArr[ArrPos]), "PasWait_" + p.ToString());
                        PathCostExpr[p] = cplex.Sum(cplex.Prod(v_PasPathWait[WaitPos], PARA.PathPara.WaitW), PathCostExpr[p]);
                    }
                }

                // add departure and arrival conservation
                for (int n = 1; n < LpData.PathSet[p].VisitNodes.Count(); n++)
                {
                    int CurrentNode = LpData.PathSet[p].VisitNodes[n];
                    int PreNode = LpData.PathSet[p].VisitNodes[n - 1];
                    int PreDepPos = LpData.PathSet[p].m_NodeId_DepVarPos[PreNode];
                    int NowArrPos = LpData.PathSet[p].m_NodeId_ArrVarPos[CurrentNode];
                    // arr time =  dep(i-1) + t(i-1)
                    if (CurrentNode != LpData.PathSet[p].Trip.DestID)
                    {
                        if (LpData.PathSet[p].TranferNodes.Contains(CurrentNode))  // only add minimum transfer time if it is a transfer node
                        {
                            cplex.AddEq(v_PasPathArr[NowArrPos], cplex.Sum(v_PasPathDep[PreDepPos],
                                LpData.PathSet[p].TimeBetweenNodes[n - 1] + PARA.PathPara.MinTransferTime),
                                "Path" + p.ToString() + "ArriveTimeAt" + CurrentNode.ToString());
                        }
                        else
                        {
                            cplex.AddEq(v_PasPathArr[NowArrPos],
                                cplex.Sum(v_PasPathDep[PreDepPos], LpData.PathSet[p].TimeBetweenNodes[n - 1]),
                                "Path" + p.ToString() + "ArriveTimeAtContinous" + CurrentNode.ToString());
                        }
                    }
                    else
                    {
                        // if it is the destination node, then dep time is not considered
                        cplex.AddEq(v_PasPathArr[NowArrPos], cplex.Sum(v_PasPathDep[PreDepPos], LpData.PathSet[p].TimeBetweenNodes[n - 1]),
                            "Path" + p.ToString() + "ArriveAtDest" + CurrentNode.ToString());
                    }
                }

                // add departure time 
                for (int n = 0; n < LpData.PathSet[p].VisitNodes.Count() - 1; n++)
                {
                    int DelatStartPos = PARA.NULLINT;
                    int CurrentNode = LpData.PathSet[p].VisitNodes[n];
                    int DepPos = LpData.PathSet[p].m_NodeId_DepVarPos[CurrentNode];
                    int ArrPos = LpData.PathSet[p].m_NodeId_ArrVarPos[CurrentNode];
                    int BoardLineID = LpData.PathSet[p].m_NodeID_NextLine[CurrentNode].ID;
                    TransitServiceType Type = LpData.PathSet[p].m_NodeID_NextLine[CurrentNode].ServiceType;
                    int FreLinePos = PARA.NULLINT; int SchLinePos = PARA.NULLINT;
                    int PathDwellIndex = LpData.GetPathDwellIndex(p, CurrentNode);
                    int LocIndex = -1;

                    if (Type == TransitServiceType.Frequency)
                    {
                        if (LpData.PathSet[p].TranferNodes.Contains(CurrentNode))
                        {
                            FreLinePos = LpData.m_FreLineId_HeadwayVarPos[BoardLineID];
                            if (PathDwellIndex < 0)
                            {
                                /// If it does not include a dwell time var
                                /// Then add half headway time
                                cplex.AddEq(v_PasPathDep[DepPos],
                                        cplex.Sum(v_PasPathArr[ArrPos], cplex.Prod(0.5, v_Headway[FreLinePos])),
                                             "Path" + p.ToString() + "Node" + CurrentNode.ToString() + "0.5*fre");
                            }
                            else
                            {
#region AddDwellCost
                                int _sid = LpData.PathSet[p].m_NodeID_NextLine[CurrentNode].Stops.FindIndex(x => x.ID == CurrentNode);
                                if (LpData.PathSet[p].m_Delta_Arr_t_pos.ContainsKey(CurrentNode))
                                {
                                    if (CurrentNode == LpData.PathSet[p].Trip.DestID) continue;
                                    int pos = LpData.PathSet[p].m_Delta_Arr_t_pos[CurrentNode];
                                    for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                                    {
                                        LocIndex = LpData.GetFreDwellExpIndex(BoardLineID, _sid, tau);
                                        if (LocIndex < 0) continue;
                                        _sum = cplex.IntExpr();
                                        for (int t = 0; t < PARA.IntervalSets[tau].Count; t++)
                                        {
                                            _sum = cplex.Sum(_sum, v_Delta_FreArr_t[pos + PARA.IntervalSets[tau][t]]);
                                        }
                                        cplex.Add(cplex.IfThen(cplex.Eq(_sum, 1), cplex.Eq(v_PathDwell[PathDwellIndex], v_FreDwellTime[LocIndex])));
                                    }
                                }
#endregion
                                PathCostExpr[p] = cplex.Sum(PathCostExpr[p], cplex.Prod(PARA.PathPara.WaitW, v_PathDwell[PathDwellIndex]));

                                cplex.AddEq(v_PasPathDep[DepPos],
                                            cplex.Sum(v_PathDwell[PathDwellIndex],
                                            cplex.Sum(v_PasPathArr[ArrPos], cplex.Prod(0.5, v_Headway[FreLinePos]))),
                                                 "Path" + p.ToString() + "Node" + CurrentNode.ToString() + "0.5*fre");
                            }
                        }
                        else
                        {
#region reviseAddDwell  
                            if (PathDwellIndex < 0) continue;
                            int _sid = LpData.PathSet[p].m_NodeID_NextLine[CurrentNode].Stops.FindIndex(x => x.ID == CurrentNode);
                            for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                            {
                                if (CurrentNode == LpData.PathSet[p].Trip.DestID) continue;
                                int pos = LpData.PathSet[p].m_Delta_Arr_t_pos[CurrentNode];
                                LocIndex = LpData.GetFreDwellExpIndex(BoardLineID, _sid, tau);
                                if (LocIndex < 0) continue;
                                _sum = cplex.IntExpr();
                                for (int t = 0; t < PARA.IntervalSets[tau].Count; t++)
                                {
                                    _sum = cplex.Sum(_sum, v_Delta_FreArr_t[pos + PARA.IntervalSets[tau][t]]);
                                }
                                cplex.Add(cplex.IfThen(cplex.Eq(_sum, 1),
                                    cplex.Eq(v_PathDwell[PathDwellIndex], v_FreDwellTime[LocIndex])));
                            }
#endregion
                            PathCostExpr[p] = cplex.Sum(PathCostExpr[p], cplex.Prod(PARA.PathPara.WaitW, v_PathDwell[PathDwellIndex]));

                            cplex.AddEq(v_PasPathDep[DepPos],
                                cplex.Sum(v_PasPathArr[ArrPos], v_PathDwell[PathDwellIndex]),
                                "Path" + p.ToString() + "AtNode" + CurrentNode.ToString() + "Dep=Arr");
                        }
                    }
                    else if (Type == TransitServiceType.Schedule)
                    {
                        int LineIndex = LpData.SchLineSet.FindIndex(x => x.ID == BoardLineID);
                        SchLinePos = LpData.m_SchLineId_TrainTerminalDepVarPos[BoardLineID];
                        INumExpr TrainDep = cplex.NumExpr();
                        INumExpr PreTrainDep = cplex.NumExpr();
                        INumExpr DeltaSum = cplex.NumExpr();
                        DelatStartPos = LpData.PathSet[p].m_NodeId_DeltaTrainBoard[CurrentNode];
                        for (int q = 0; q < LpData.SchLineSet[LineIndex].NumOfTrains; q++)
                        {
                            DeltaSum = cplex.Sum(DeltaSum, v_Delta_Board_Veh[DelatStartPos + q]);
                        }
                        cplex.AddEq(DeltaSum, 1, "use_one_train");
                        if (LpData.PathSet[p].TranferNodes.Contains(CurrentNode))
                        {
                            for (int q = 0; q < LpData.SchLineSet[LineIndex].NumOfTrains; q++)
                            {
                                double addInvehileTime = LpData.SchLineSet[LineIndex].getTravelTimeBetweenStop(LpData.SchLineSet[LineIndex].Stops[0].ID, CurrentNode);
                                int WaitPos = LpData.PathSet[p].m_NodeId_WaitVarPos[CurrentNode];
                                int Lid = BoardLineID;
                                int sid = LpData.SchLineSet[LineIndex].Stops.FindIndex(x => x.ID == CurrentNode);
                                int DwellTimeLoc = LpData.GetSchDwellExpIndex(BoardLineID, sid, q);
                                if (DwellTimeLoc >= 0)
                                {
                                    TrainDep = cplex.Sum(v_TrainTerminalDep[SchLinePos + q], addInvehileTime);
                                    int ps = 1;  // previous stop
                                    while (ps <= sid) // while it is at or before the current stop
                                    {
                                        int psloc = LpData.GetSchDwellExpIndex(BoardLineID, ps, q);
                                        TrainDep = cplex.Sum(TrainDep, DwellTimeExpr_Sch[psloc]);
                                        ps++;
                                    }
                                }
                                else
                                { // train departing time without accounting the boarding/alighting time
                                    TrainDep = cplex.Sum(v_TrainTerminalDep[SchLinePos + q], addInvehileTime);
                                }
                                Lhs = cplex.Diff(TrainDep, cplex.Sum(v_PasPathArr[ArrPos], v_PasPathWait[WaitPos]));
                                Rhs = cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(v_Delta_Board_Veh[DelatStartPos + q], 1));
                                cplex.AddGe(Lhs, Rhs);
                            }
                        }

                        for (int q = 0; q < LpData.SchLineSet[LineIndex].NumOfTrains; q++)
                        {
                            double addInvehileTime = LpData.SchLineSet[LineIndex].getTravelTimeBetweenStop(LpData.SchLineSet[LineIndex].Stops[0].ID, CurrentNode);
                            TrainDep = cplex.Sum(v_TrainTerminalDep[SchLinePos + q], addInvehileTime);
                            int Lid = BoardLineID;
                            int sid = LpData.SchLineSet[LineIndex].Stops.FindIndex(x => x.ID == CurrentNode);
                            int DwellTimeLoc = LpData.GetSchDwellExpIndex(BoardLineID, sid, q);
                            if (DwellTimeLoc >= 0)
                            {
                                int ps = 1;
                                while (ps <= sid) // while it is at or before the current stop
                                {
                                    int psloc = LpData.GetSchDwellExpIndex(BoardLineID, ps, q);
                                    TrainDep = cplex.Sum(TrainDep, DwellTimeExpr_Sch[psloc]);
                                    ps++;
                                }
                            }
                            if (q >= 1)
                            {
                                PreTrainDep = cplex.Sum(v_TrainTerminalDep[SchLinePos + q - 1], addInvehileTime);
                                Lid = BoardLineID;
                                sid = LpData.SchLineSet[LineIndex].Stops.FindIndex(x => x.ID == CurrentNode);
                                DwellTimeLoc = LpData.GetSchDwellExpIndex(BoardLineID, sid, q - 1);
                                if (DwellTimeLoc >= 0)
                                {
                                    int ps = 1;
                                    while (ps <= sid) // while it is at or before the current stop
                                    {
                                        int psloc = LpData.GetSchDwellExpIndex(BoardLineID, ps, q - 1);
                                        PreTrainDep = cplex.Sum(PreTrainDep, DwellTimeExpr_Sch[psloc]);
                                        ps++;
                                    }
                                }
                                Lhs = cplex.Diff(PreTrainDep, v_PasPathArr[ArrPos]);
                                Rhs = cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(1, v_Delta_Board_Veh[DelatStartPos + q]));
                                cplex.AddLe(Lhs, Rhs);
                            }

                            Rhs = cplex.Sum(TrainDep, cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(v_Delta_Board_Veh[DelatStartPos + q], 1)));
                            cplex.AddGe(v_PasPathDep[DepPos], Rhs);

                            Rhs = cplex.Sum(TrainDep, cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(1, v_Delta_Board_Veh[DelatStartPos + q])));
                            cplex.AddLe(v_PasPathDep[DepPos], Rhs);

                            cplex.Add(cplex.IfThen(cplex.Eq(v_Delta_Board_Veh[DelatStartPos + q], 1),
                                cplex.Le(v_PasPathArr[ArrPos], TrainDep, "PasArrBeforeTrain")));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set cplex model from data
        /// </summary>
        protected internal void SetCplexModel(BBmain.LpInput LpData, double[] HeadwayUp, double[] HeadwayLp,
                                              Dictionary<int, double> m_FixedLineHeadway,
                                             BBmain.SolClass WarmSol, bool isScratchModel)
        {
            double[] FreUb = new double[LpData.NumFreLines];
            double[] FreLb = new double[LpData.NumFreLines];
            for (int i = 0; i < LpData.NumFreLines; i++)
            {
                FreUb[i] = 1.0 / HeadwayLp[i]; FreLb[i] = 1.0 / HeadwayUp[i];
            }
            // only build the model if it starts from scratch, otherwise only change the headway constraints
            if (!isScratchModel) goto ModifyFre;

            INumExpr Rhs = cplex.NumExpr(), Lhs = cplex.NumExpr();
            INumExpr expr = cplex.NumExpr();
            for (int q = 0; q < LpData.NumOfPath; q++) PathCostExpr.Add(cplex.NumExpr());
            if (PathCostExpr.Count > LpData.NumOfPath) Console.WriteLine("Wtf: Warning, the path cost expr dimension is not correct");
            
            BilinearFreConstraintUsingRange(LpData, FreUb, FreLb, LpData.HeadwayUb, LpData.HeadwayLb, BilinearRange);
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                PathFlowExpr.Add(cplex.NumExpr());
                PathFlowExpr[p] = cplex.Prod(Global.DemandScale * LpData.PathSet[p].Trip.Demand, v_PathProb[p]);
            }

            // add capacity constraints
            SchCapCon(cplex, LpData, PathFlowExpr, PathCostExpr, DwellTimeExpr_Sch, v_Delta_Board_Veh, v_Y, v_PathSchCapCost, v_Delta_Congest, v_Delta_Seat, SolveModel: true);
            FreCapCon(cplex, LpData, PathFlowExpr, PathCostExpr, DwellTimeExpr_Fre, v_Delta_FreDep_t, v_Delta_FreArr_t,
                v_Ybar_Dep, v_Ybar_Arr, v_PathFreCapCost, v_PasPathDep, v_Fre, v_Delta_Congest, v_Delta_Seat, v_FreDwellTime,
                SolveModel: true);
            if (PARA.DesignPara.isConsiderSeatSequence)
            {
                SeatCon(cplex, v_PathFreCapCost, v_Delta_Congest, v_Delta_Seat, v_Delta_Board_Veh,
                    v_Delta_FreDep_t, LpData, SolveModel: true);
            }
            DepTimeAndHeadwayConstraint(LpData); // add first and last departure time constraints
            DefDeltaDepArr(cplex, DwellTimeExpr_Fre, v_Delta_FreDep_t, v_Delta_FreArr_t, v_PasPathDep, v_PasPathArr, LpData, SolveModel: true);
            GetPathCostExpr_withoutCapCost(LpData, DwellTimeExpr_Sch, DwellTimeExpr_Fre); // add path cost expression
            setTrainDepExpr(cplex, LpData, v_TrainTerminalDep, DwellTimeExpr_Sch, TrainDepTimeExpr);

            // the upper bound of the v_lnProb parameters, I think can be derived analytically 
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                v_LnProb_BigA[p] = cplex.IntVarArray(PARA.DesignPara.NumOfBreakPoints - 1, 0, 10);
                v_LnProb_u[p] = cplex.BoolVarArray(xi);
            }
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                v_Bcm_LnZ_BigA[p] = cplex.IntVarArray(PARA.DesignPara.NumOfBreakPoints - 1, 0, 10);
                v_Bcm_LnZ_u[p] = cplex.BoolVarArray(xi);
                v_Bcm_LnM_BigA[p] = cplex.IntVarArray(PARA.DesignPara.NumOfBreakPoints - 1, 0, 10);
                v_Bcm_LnM_u[p] = cplex.BoolVarArray(xi);
            }
            if (PARA.DesignPara.AssignMent.Equals(AssignMethod.SUE)) RelaxRSUE_v2(LpData);
            if (PARA.DesignPara.AssignMent.Equals(AssignMethod.BCM)) BCM(LpData);
            RelaxObj(LpData, LpData.ProbLb, LpData.ProbUb, LpData.PieLb, LpData.PieUb);

            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                cplex.AddEq(v_PathPie[p], PathCostExpr[p], "PieExp");
            }

            cplex.SetParam(Cplex.Param.MIP.Tolerances.MIPGap, PARA.DesignPara.MIPRelGap);
            cplex.SetParam(Cplex.Param.TimeLimit, PARA.DesignPara.CplexTimLim);

//#if !DEBUG
//            cplex.SetOut(null);
//#endif
          return;

        ModifyFre:
            foreach (var i in BilinearRange) cplex.Remove(i);
            // the following only update the range constraint for the frequency constraints
            BilinearFreConstraintUsingRange(LpData, FreUb, FreLb, LpData.HeadwayUb, LpData.HeadwayLb, BilinearRange);
            double[] WarmFre = WarmSol.v_Fre.ToArray();
            double[] WarmHead = WarmSol.v_Headway.ToArray();
            for (int l = 0; l < LpData.NumFreLines; l++)
            {
                if (m_FixedLineHeadway.ContainsKey(l))
                {
                    Debug.Assert(m_FixedLineHeadway[l] <= PARA.DesignPara.MaxHeadway);
                    BilinearRange.Add(cplex.AddEq(v_Headway[l], Math.Max(m_FixedLineHeadway[l], PARA.DesignPara.MinHeadway)));
                    BilinearRange.Add(cplex.AddEq(v_Fre[l], 1.0 / Math.Max(m_FixedLineHeadway[l], PARA.DesignPara.MinHeadway)));
                    WarmHead[l] = Math.Max(m_FixedLineHeadway[l], PARA.DesignPara.MinHeadway);
                    WarmFre[l] = 1.0 / Math.Max(m_FixedLineHeadway[l], PARA.DesignPara.MinHeadway);
               }
            }
            if (!Global.UseWarmStartUp) return;   // if not using warm up solution then return

#region warmstart

            if (WarmSol.v_TrainTerminalDep.Count > 0)
            {
                cplex.AddMIPStart(v_TrainTerminalDep, WarmSol.v_TrainTerminalDep.ToArray());
                cplex.AddMIPStart(v_Delta_Board_Veh, Array.ConvertAll(WarmSol.v_Delta_Board_Veh.ToArray(), Convert.ToDouble));
                cplex.AddMIPStart(v_PathSchCapCost, WarmSol.v_PathSchCapCost.ToArray());
            }
            if (v_Delta_Seat.Count() == WarmSol.v_Delta_Seat.Count())
                cplex.AddMIPStart(v_Delta_Seat, Array.ConvertAll(WarmSol.v_Delta_Seat.ToArray(), Convert.ToDouble));

            if (v_Delta_FreArr_t.Count() == WarmSol.v_Delta_FreArr_t.Count())
                cplex.AddMIPStart(v_Delta_FreArr_t, Array.ConvertAll(WarmSol.v_Delta_FreArr_t.ToArray(), Convert.ToDouble));
            if (v_Delta_Congest.Count() == WarmSol.v_Delta_Congest.Count())
                cplex.AddMIPStart(v_Delta_Congest, Array.ConvertAll(WarmSol.v_Delta_Congest.ToArray(), Convert.ToDouble));
            if (WarmSol.v_Fre.Count > 0)
            {
                cplex.AddMIPStart(v_Headway, WarmHead);
                cplex.AddMIPStart(v_Fre, WarmFre);
                if (v_Delta_FreDep_t.Count() == WarmSol.v_Delta_FreDep_t.Count())
                    cplex.AddMIPStart(v_Delta_FreDep_t, Array.ConvertAll(WarmSol.v_Delta_FreDep_t.ToArray(), Convert.ToDouble));
                if (v_PathFreCapCost.Count() == WarmSol.v_PathFreCapCost.Count())
                    cplex.AddMIPStart(v_PathFreCapCost, WarmSol.v_PathFreCapCost.ToArray());
            }
            if (v_PasPathWait.Count() == WarmSol.v_PasPathWait.Count())
                cplex.AddMIPStart(v_PasPathWait, WarmSol.v_PasPathWait.ToArray());

            if (v_LnProb_Lb.Count() == WarmSol.v_LnProb_Lb.Count())
                cplex.AddMIPStart(v_LnProb_Lb, WarmSol.v_LnProb_Lb.ToArray());
            if (v_Bilinear_w.Count() == WarmSol.v_Bilinear_w.Count())
                cplex.AddMIPStart(v_Bilinear_w, WarmSol.v_Bilinear_w.ToArray());
            if (v_PathPie.Count() == WarmSol.v_PathPie.Count())
                cplex.AddMIPStart(v_PathPie, WarmSol.v_PathPie.ToArray());
#endregion
            return;
        }
        /// <summary>
        /// solve model main procedure
        /// </summary>
        protected internal bool SolveModel(BBmain.LpInput LpData)
        {
            bool status = cplex.Solve();
            if (status) { Global.BBSolNum++;}
            return status;
        }
    }
}

