// checked 2021-May
using System;
using System.Collections.Generic;
using System.Linq;
using SolveLp;
namespace IOPT
{
    public partial class BBmain
    {
        public class LineFlowClass
        {
            public int LineID { get; set; }
            public int NodeId { get; set; }
            public int VarPos { get; set; }
            public List<double> BoardFlow { get; set; }
            public List<double> OnBoardFlow { get; set; }
            public LineFlowClass()
            {
                LineID = -1; NodeId = -1;
                BoardFlow = new List<double>();
                OnBoardFlow = new List<double>();
                VarPos = -1;
            }
        }
        public partial class SolClass
        {
            public int SolNumID { get; set; }
            public int BBLevel { get; set; }  // level for the branch and bound algorithm
            //protected internal List<double> v_DepatureDelta { get; set; }
            protected internal double CplexObj { get; set; }
            protected internal double CplexMipGap { get; set; }
            protected internal double TotalCostCompute { get; set; }  // operation cost + total passenger cost based on computed value
            protected internal double TotalOpCost { get; set; } // total operation cost
            protected internal double TotalPasCostCplex { get; set; } // total passenger cost obtained from cplex
            protected internal double TotalPasCostCompute { get; set; } // total passengers cost computed use the computed probability 
            protected internal double CpuTime { get; set; }
            protected internal SOLSTA SolSatus { get; set; }
            protected internal SOLSOVTYPE SolSovleType { get; set; }
            protected internal List<double> Prob_Compute { get; set; }
            protected internal List<double> v_Fre { get; set; }
            protected internal List<double> v_Headway { get; set; }
            protected internal List<double> v_RelaxObj { get; set; }
            protected internal List<double> v_PathPie { get; set; }
            protected internal List<int> v_Delta_Board_Veh { get; set; }
            protected internal List<int> v_Delta_FreDep_t { get; set; }
            protected internal List<int> v_Delta_FreArr_t { get; set; }
            protected internal List<double> v_PasPathDep { get; set; }
            protected internal List<double> v_PasPathArr { get; set; }
            protected internal List<double> v_Y { get; set; }
            protected internal List<double> v_PathProb { get; set; }
            protected internal List<double> v_PathSchCapCost { get; set; }
            protected internal List<double> v_PathFreCapCost { get; set; }
            protected internal List<double> v_LnProb { get; set; }
            protected internal List<double> v_Bilinear_w { get; set; }
            protected internal List<double> v_PasPathWait { get; set; }
            protected internal List<double> v_TrainTerminalDep { get; set; }
            protected internal List<double> v_LnProb_Lb { get; set; }
            protected internal List<double> v_LnProb_J { get; set; }
            protected internal List<double> PathCostExprVal { get; set; }
            protected internal List<double> PathFlowExprVal { get; set; }
            protected internal Dictionary<int, double> m_FixedLineHeadway { get; set; } // map line id to its headway
            protected internal List<double> HeadwayUp { get; set; }
            protected internal List<double> HeadwayLp { get; set; }
            protected internal List<List<double>> v_LnProb_BigA { get; set; }
            protected internal List<List<double>> v_LnProb_u { get; set; }
            protected internal List<int> v_Delta_Seat { get; set; }
            protected internal List<int> v_Delta_Congest { get; set; }
            protected internal List<int> Remove_PathSet { get; set; }
            protected internal List<double> TrainDepTimeAtEachStop { get; set; }
            protected internal List<double> SchLineDwellTime { get; set; }
            protected internal List<double> FreLineDwellTime { get; set; }

            protected internal SOLSTA getStatus()
            {
                SOLSTA sls = SOLSTA.EpsFeasible;
                for (int l = 0; l < v_Fre.Count; l++)
                {
                    if (Math.Abs(v_Headway[l] * v_Fre[l] - 1) > PARA.BBPara.EpsConstraint)
                    {
                        sls = SOLSTA.InFeasible;
                    }
                }
                return sls;
            }
            /// <summary>
            /// Init solution
            /// </summary>
            private void CreatNew()
            {
                Remove_PathSet = new List<int>();
                CplexObj = double.MaxValue;
                CplexMipGap = -1;
                SolNumID = -1;
                BBLevel = 0;
                TotalOpCost = -1;
                TotalCostCompute = -1;
                TotalPasCostCplex = double.MaxValue;
                TotalPasCostCompute = -1;
                SolSatus = SOLSTA.InFeasible;
                SolSovleType = SOLSOVTYPE.IsNull;
                v_Fre = new List<double>();
                v_Headway = new List<double>();
                Prob_Compute = new List<double>();
                v_RelaxObj = new List<double>();
                v_PathPie = new List<double>();
                v_Delta_Board_Veh = new List<int>();
                v_Y = new List<double>();
                v_PathProb = new List<double>();
                v_PathSchCapCost = new List<double>();
                v_PathFreCapCost = new List<double>();
                v_LnProb = new List<double>();
                v_Bilinear_w = new List<double>();
                v_PasPathWait = new List<double>();
                v_TrainTerminalDep = new List<double>();
                v_LnProb_Lb = new List<double>();
                v_LnProb_BigA = new List<List<double>>();
                v_LnProb_u = new List<List<double>>();
                v_LnProb_J = new List<double>();
                PathCostExprVal = new List<double>();
                PathFlowExprVal = new List<double>();
                HeadwayUp = new List<double>();
                HeadwayLp = new List<double>();
                CpuTime = -999;
                m_FixedLineHeadway = new Dictionary<int, double>();
                v_Delta_Seat = new List<int>();
                v_Delta_Congest = new List<int>();
                v_Delta_FreDep_t = new List<int>();
                v_Delta_FreArr_t = new List<int>();
                v_PasPathDep = new List<double>();
                v_PasPathArr = new List<double>();
                ///<remarks>
                /// revised 2021 Feb
                /// add sch dwell time and train dep time at each stop
                ///</remarks> revised 2021 Feb
                TrainDepTimeAtEachStop = new List<double>();
                SchLineDwellTime = new List<double>();
                FreLineDwellTime = new List<double>();
            }
            protected internal SolClass() { CreatNew(); }
            /// <summary>
            /// construct a new solution based on the LpData
            /// when some parameters are specified
            /// </summary>
            public SolClass(LpInput LpData)  // construct a solution
            {
                CreatNew();
                for (int p = 0; p < LpData.NumOfPath; p++)
                {
                    v_RelaxObj.Add(0.0);
                    v_PathPie.Add(0.0);
                    v_PathProb.Add(0.0);
                    Prob_Compute.Add(0.0);
                    v_LnProb.Add(0.0);
                    v_LnProb_Lb.Add(0.0);
                    v_LnProb_J.Add(0.0);
                    PathCostExprVal.Add(0.0);
                    PathFlowExprVal.Add(0.0);
                }
                for (int l = 0; l < LpData.NumFreLines; l++)
                {
                    v_Fre.Add(0.0); v_Headway.Add(0.0); v_Bilinear_w.Add(0.0);
                    HeadwayUp.Add(0.0); HeadwayLp.Add(0.0);
                }
                for (int i = 0; i < LpData.DeltaPathTrainPos; i++) v_Delta_Board_Veh.Add(0);
                for (int i = 0; i < LpData.DeltaPathTrainPos; i++) v_Y.Add(0.0);
                for (int i = 0; i < LpData.Node_SchCapPos; i++) v_PathSchCapCost.Add(0.0);
                for (int i = 0; i < LpData.Node_FreCapPos; i++) v_PathFreCapCost.Add(0.0);
                for (int i = 0; i < LpData.WaitVarPos; i++) v_PasPathWait.Add(0.0);
                for (int i = 0; i < LpData.TotalNumOfTrains; i++) v_TrainTerminalDep.Add(0);
                for (int i = 0; i < LpData.DepVarPos; i++) v_PasPathDep.Add(0);
                for (int i = 0; i < LpData.ArrVarPos; i++) v_PasPathArr.Add(0);
                for (int i = 0; i < LpData.Delta_FreDep_t_Pos; i++) v_Delta_FreDep_t.Add(0);
                for (int i = 0; i < LpData.Delta_FreArr_t_Pos; i++) v_Delta_FreArr_t.Add(0);
                for (int i = 0; i < LpData.LineSec_Delta_SeatPos; i++) v_Delta_Seat.Add(0);
                for (int i = 0; i < LpData.CongStausPos; i++) v_Delta_Congest.Add(0);
                for (int i = 0; i < LpData.NumOfPath; i++)
                {
                    v_LnProb_BigA.Add(new List<double>());
                    for (int j = 0; j < PARA.DesignPara.NumOfBreakPoints - 1; j++) v_LnProb_BigA[i].Add(0.0);

                    v_LnProb_u.Add(new List<double>());
                    for (int j = 0; j < (int)Math.Ceiling(Math.Log(PARA.DesignPara.NumOfBreakPoints - 1, 2)); j++) v_LnProb_u[i].Add(0.0);
                }
                for (int i = 0; i < LpData.GetTrainDepDemsion(); i++) { TrainDepTimeAtEachStop.Add(0.0); }
                for (int i = 0; i < LpData.GetSchDewllDimension(); i++) { SchLineDwellTime.Add(0.0); }
                for (int i = 0; i < LpData.GetFreDwellDimension(); i++) { FreLineDwellTime.Add(0.0); }
            }
            /// <summary>
            /// create a map from line id to its headway solution
            /// these lines are considered to eps feasible lines
            /// </summary>
            protected internal void getFixedLineMap()
            {
                m_FixedLineHeadway.Clear();
                for (int l = 0; l < v_Fre.Count(); l++)
                {
                    if (v_Headway[l] * v_Fre[l] < 1.0 - PARA.BBPara.EpsConstraint
                        || v_Headway[l] * v_Fre[l] > 1.0 + PARA.BBPara.EpsConstraint)
                    {
                        m_FixedLineHeadway.Add(l, v_Headway[l]);
                    }
                }
            }
            /// <summary>
            /// find the line to branch based on the violation of the constraints
            /// only branch one line with the maximum gap values
            /// </summary>
            protected internal int getBranchLine()
            {
                double MaxGapVal = double.MinValue;
                int BranchLineIndex = PARA.NULLINT;
                List<double> Product = new List<double>();
                for (int l = 0; l < v_Fre.Count(); l++) Product.Add(v_Fre[l] * v_Headway[l]);
                for (int l = 0; l < Product.Count; l++)
                {
                    if (Math.Abs(Product[l] - 1) > MaxGapVal
                        && Math.Abs(Product[l] - 1) > PARA.BBPara.EpsConstraint) // only consider the value greater than eps
                    {
                        MaxGapVal = Math.Abs(Product[l] - 1);
                        BranchLineIndex = l;
                    }
                }
                return BranchLineIndex;
            }
            /// <summary>
            /// get the probability using compute method
            /// </summary>
            protected internal void getProb_Compute(LpInput LpData)
            {
                for (int p = 0; p < v_PathPie.Count; p++) LpData.PathSet[p].PathPie = v_PathPie[p];
                if (PARA.DesignPara.AssignMent.Equals(AssignMethod.SUE))
                {
                    for (int i = 0; i < LpData.TripPathSet.Count; i++)
                    {
                        double sum = 0;
                        for (int p = 0; p < LpData.TripPathSet[i].Count; p++)
                        {
                            sum += Math.Exp(-1.0 * PARA.DesignPara.Theta * LpData.PathSet[LpData.TripPathSet[i][p]].PathPie);
                        }
                        for (int p = 0; p < LpData.TripPathSet[i].Count; p++)
                        {
                            LpData.PathSet[LpData.TripPathSet[i][p]].PathProb_Compute =
                                Math.Exp(-1.0 * PARA.DesignPara.Theta * LpData.PathSet[LpData.TripPathSet[i][p]].PathPie) / sum;
                            Prob_Compute[LpData.TripPathSet[i][p]] = LpData.PathSet[LpData.TripPathSet[i][p]].PathProb_Compute;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < LpData.TripPathSet.Count; i++)
                    {
                        double[] expval = new double[LpData.TripPathSet[i].Count()];
                        double sum = 0;
                        List<double> TripPathSetPie = new List<double>(LpData.TripPathSet[i].Count);
                        for (int p = 0; p < LpData.TripPathSet[i].Count(); p++) TripPathSetPie.Add(LpData.PathSet[LpData.TripPathSet[i][p]].PathPie);
                        for (int p = 0; p < LpData.TripPathSet[i].Count; p++)
                        {
                            expval[p] = Math.Exp(-1.0 * PARA.DesignPara.Theta * (TripPathSetPie[p] - TripPathSetPie.Min() -
                                PARA.DesignPara.GetBcmValue()));
                            sum += expval[p] - 1;
                        }
                        for (int p = 0; p < LpData.TripPathSet[i].Count; p++)
                        {
                            LpData.PathSet[LpData.TripPathSet[i][p]].PathProb_Compute =
                                (expval[p] - 1) / sum;
                            Prob_Compute[LpData.TripPathSet[i][p]] = LpData.PathSet[LpData.TripPathSet[i][p]].PathProb_Compute;
                        }
                    }
                }
            }
            protected internal double getTotalPasCostCplex(LpInput LpData)
            {
                double TPC = 0;
                for (int p = 0; p < v_PathPie.Count; p++)
                {
                    TPC += v_PathProb[p] * v_PathPie[p] * LpData.PathSet[p].Trip.Demand;
                }
                return TPC;
            }
            protected internal double getTotalPasCostViaComputeProb(LpInput LpData)
            {
                double TPCompute = 0;
                for (int p = 0; p < v_PathPie.Count; p++)
                {
                    TPCompute += v_PathPie[p] * Prob_Compute[p] * LpData.PathSet[p].Trip.Demand;
                }
                return TPCompute;
            }

            /// <summary>
            /// Compute Operation Cost
            /// </summary>
            protected internal double getTotalOpCost()
            {
                double ORCost = 0;
                for (int l = 0; l < v_Fre.Count; l++)
                {
                    ORCost += v_Fre[l] * PARA.DesignPara.FreOperationCost;
                }
                return ORCost;
            }
            protected internal void PrintOnScreenCheck(Lp model, LpInput LpData)
            {
                for (int i = 0; i < model.v_Bcm_LnZ_u.Count(); i++)
                {
                    for (int j = 0; j < model.v_Bcm_LnZ_u[i].Count(); j++)
                    {
                        Console.WriteLine("i={0},j={1},v_Bcm_LnZ_u={2}", i, j, model.cplex.GetValue(model.v_Bcm_LnZ_u[i][j]));

                    }
                }
            }
            /// <summary>
            /// Copy all the solution values from the cplex
            /// </summary>
            public void CopySolFromCplex(Lp model, LpInput LpData)
            {
                List<double> v_FreDwellVal = new List<double>();
                List<double> v_PathDwellTimeVal = new List<double>();
                v_FreDwellVal = model.cplex.GetValues(model.v_FreDwellTime).ToList();
                v_PathDwellTimeVal = model.cplex.GetValues(model.v_PathDwell).ToList();
                for (int i = 0; i < v_FreDwellVal.Count; i++) v_FreDwellVal[i] = Math.Max(v_FreDwellVal[i], 0.0);
                for (int i = 0; i < v_PathDwellTimeVal.Count; i++) v_PathDwellTimeVal[i] = Math.Max(v_PathDwellTimeVal[i], 0.0);
                SolNumID = Global.BBSolNum;
                CplexObj = model.cplex.GetObjValue();
                v_RelaxObj = model.cplex.GetValues(model.v_RelaxObj).ToList();
                CplexMipGap = model.cplex.GetMIPRelativeGap();
                CpuTime = model.cplex.GetCplexTime();
                v_Fre = model.cplex.GetValues(model.v_Fre).ToList();
                v_Headway = model.cplex.GetValues(model.v_Headway).ToList();
                SolSatus = getStatus();
                v_PathPie = model.cplex.GetValues(model.v_PathPie).ToList();
                v_PathProb = model.cplex.GetValues(model.v_PathProb).ToList();
                v_PasPathWait = model.cplex.GetValues(model.v_PasPathWait).ToList();
                v_PathSchCapCost = model.cplex.GetValues(model.v_PathSchCapCost).ToList();
                v_PathFreCapCost = model.cplex.GetValues(model.v_PathFreCapCost).ToList();
                v_LnProb_Lb = model.cplex.GetValues(model.v_LnProb_Lb).ToList();
                v_LnProb_J = model.cplex.GetValues(model.v_LnProb_J).ToList();
                v_LnProb = model.cplex.GetValues(model.v_LnProb).ToList();
                getProb_Compute(LpData);
                for (int p = 0; p < LpData.NumOfPath; p++)
                {
                    PathCostExprVal[p] = model.cplex.GetValue(model.PathCostExpr[p]);
                    PathFlowExprVal[p] = model.cplex.GetValue(model.PathFlowExpr[p]);
                    LpData.PathSet[p].PathPie = v_PathPie[p];
                    LpData.PathSet[p].PathProb = v_PathProb[p];
                    LpData.PathSet[p].PathProb_Compute = Prob_Compute[p];
                }
                v_Delta_Board_Veh = Array.ConvertAll(model.cplex.GetValues(model.v_Delta_Board_Veh).ToArray(), Convert.ToInt32).ToList();
                v_Delta_Seat = Array.ConvertAll(model.cplex.GetValues(model.v_Delta_Seat).ToArray(), Convert.ToInt32).ToList();
                v_Delta_Congest = Array.ConvertAll(model.cplex.GetValues(model.v_Delta_Congest).ToArray(), Convert.ToInt32).ToList();
                v_Delta_FreArr_t = Array.ConvertAll(model.cplex.GetValues(model.v_Delta_FreArr_t).ToArray(), Convert.ToInt32).ToList();
                v_Delta_FreDep_t = Array.ConvertAll(model.cplex.GetValues(model.v_Delta_FreDep_t).ToArray(), Convert.ToInt32).ToList();
                v_PasPathDep = model.cplex.GetValues(model.v_PasPathDep).ToArray().ToList();
                v_PasPathArr = model.cplex.GetValues(model.v_PasPathArr).ToArray().ToList();
                v_TrainTerminalDep = model.cplex.GetValues(model.v_TrainTerminalDep).ToList();
                for (int i = 0; i < v_TrainTerminalDep.Count; i++)
                {
                    v_TrainTerminalDep[i] = Math.Max(v_TrainTerminalDep[i], 0.0);
                }

                v_Bilinear_w = model.cplex.GetValues(model.v_Bilinear_w).ToList();

                for (int i = 0; i < LpData.NumOfPath; i++)
                {
                    for (int j = 0; j < PARA.DesignPara.NumOfBreakPoints - 1; j++)
                        v_LnProb_BigA[i][j] = model.cplex.GetValue(model.v_LnProb_BigA[i][j]);
                }
                for (int i = 0; i < LpData.NumOfPath; i++)
                {
                    for (int j = 0; j < (int)Math.Ceiling(Math.Log(PARA.DesignPara.NumOfBreakPoints - 1, 2)); j++)
                        v_LnProb_u[i][j] = model.cplex.GetValue(model.v_LnProb_u[i][j]);
                }
                TotalPasCostCplex = getTotalPasCostCplex(LpData);
                TotalPasCostCompute = getTotalPasCostViaComputeProb(LpData);
                TotalOpCost = getTotalOpCost();
                CplexObj = TotalPasCostCplex + TotalOpCost;

                TotalCostCompute = TotalOpCost + TotalPasCostCompute;
                if (model.TrainDepTimeExpr.Count != TrainDepTimeAtEachStop.Count)
                {
                    Console.WriteLine("Warning on the train departure time dimension");
                }
                else
                {
                    for (int i = 0; i < TrainDepTimeAtEachStop.Count; i++)
                    {
                        TrainDepTimeAtEachStop[i] = Math.Max(model.cplex.GetValue(model.TrainDepTimeExpr[i]), 0.0);
                    }
                }
                for (int l = 0; l < LpData.SchLineSet.Count; l++)
                {
                    int LineID = LpData.SchLineSet[l].ID;
                    for (int q = 0; q < LpData.SchLineSet[l].NumOfTrains; q++)
                    {
                        for (int s = 0; s < LpData.SchLineSet[l].Stops.Count - 1; s++)
                        {
                            double val = getTrainDepTimeAtStop(LineID, LpData.SchLineSet[l].Stops[s].ID, q, LpData);
                            LpData.SchLineSet[l].Stops[s].LineTimes.Find(x => x.LineID == LineID).DepTimes[q] = val;
                        }
                    }
                }
                for (int i = 0; i < model.DwellTimeExpr_Sch.Count; i++)
                {
                    SchLineDwellTime[i] = Math.Max(model.cplex.GetValue(model.DwellTimeExpr_Sch[i]), 0.0);
                }

                for (int l = 0; l < LpData.SchLineSet.Count; l++)
                {
                    for (int s = 0; s < LpData.SchLineSet[l].Stops.Count - 1; s++)
                    {
                        for (int q = 0; q < LpData.SchLineSet[l].NumOfTrains; q++)
                        {
                            int index = LpData.GetSchDwellExpIndex(LpData.SchLineSet[l].ID, s, q);

                            double val = 0;
                            if (index != -1)
                            {
                                val = model.cplex.GetValue(model.DwellTimeExpr_Sch[index]);
                            }
                            else
                            {
                                if (s == 0)  // first stop dwell time =0
                                {
                                    val = 0;
                                }
                                else
                                {
                                    val = PARA.DesignPara.MinDwellTime;
                                }
                            }
                            LpData.SchLineSet[l].Stops[s].LineTimes.Find(x => x.LineID == LpData.SchLineSet[l].ID).DwellTimes[q] = val;
                            LpData.SchLineSet[l].Stops[s].LineTimes.Find(x => x.LineID == LpData.SchLineSet[l].ID).ArrTimes[q] =
                                LpData.SchLineSet[l].Stops[s].LineTimes.Find(x => x.LineID == LpData.SchLineSet[l].ID).DepTimes[q] - val;
                        }
                    }
                }
                for (int i = 0; i < model.DwellTimeExpr_Fre.Count; i++)
                {
                    FreLineDwellTime[i] = Math.Max(model.cplex.GetValue(model.DwellTimeExpr_Fre[i]), 0.0);
                }

                for (int l = 0; l < LpData.FreLineSet.Count; l++)
                {
                    for (int s = 0; s < LpData.FreLineSet[l].Stops.Count; s++)
                    {
                        for (int q = 0; q < PARA.IntervalSets.Count; q++)
                        {
                            int index = LpData.GetFreDwellExpIndex(LpData.FreLineSet[l].ID, s, q);

                            if (index >= 0)
                            {
                                LpData.FreLineSet[l].Stops[s].LineTimes.Find(x => x.LineID == LpData.FreLineSet[l].ID).DwellTimes[q] = FreLineDwellTime[index];
                            }
                        }
                    }
                }
                PrintSol(LpData);
            }

            /// <summary>
            /// Copy solution from another solution
            /// </summary>
            public void CopyFromSol(SolClass FromSol)
            {
                for (int p = 0; p < v_PathPie.Count; p++)
                {
                    v_RelaxObj[p] = FromSol.v_RelaxObj[p];
                    v_PathPie[p] = FromSol.v_PathPie[p];
                    Prob_Compute[p] = FromSol.Prob_Compute[p];
                    v_PathProb[p] = FromSol.v_PathProb[p];
                    v_LnProb_Lb[p] = FromSol.v_LnProb_Lb[p];
                    v_LnProb[p] = FromSol.v_LnProb_Lb[p];
                    v_LnProb_J[p] = FromSol.v_LnProb_J[p];
                    for (int j = 0; j < v_LnProb_BigA[p].Count; j++) v_LnProb_BigA[p][j] = FromSol.v_LnProb_BigA[p][j];
                    for (int j = 0; j < v_LnProb_u[p].Count; j++) v_LnProb_u[p][j] = FromSol.v_LnProb_u[p][j];
                    PathCostExprVal[p] = FromSol.PathCostExprVal[p];
                    PathFlowExprVal[p] = FromSol.PathFlowExprVal[p];
                }
                for (int i = 0; i < v_Delta_FreArr_t.Count; i++) v_Delta_FreArr_t[i] = FromSol.v_Delta_FreArr_t[i];
                for (int i = 0; i < v_Delta_FreDep_t.Count; i++) v_Delta_FreDep_t[i] = FromSol.v_Delta_FreDep_t[i];
                for (int i = 0; i < v_Delta_Seat.Count; i++) v_Delta_Seat[i] = FromSol.v_Delta_Seat[i];
                for (int i = 0; i < v_Delta_Congest.Count; i++) v_Delta_Congest[i] = FromSol.v_Delta_Congest[i];
                for (int i = 0; i < v_PasPathDep.Count; i++) v_PasPathDep[i] = FromSol.v_PasPathDep[i];
                for (int i = 0; i < v_PasPathArr.Count; i++) v_PasPathArr[i] = FromSol.v_PasPathArr[i];
                for (int i = 0; i < TrainDepTimeAtEachStop.Count; i++) TrainDepTimeAtEachStop[i] = FromSol.TrainDepTimeAtEachStop[i];
                for (int i = 0; i < SchLineDwellTime.Count; i++) SchLineDwellTime[i] = FromSol.SchLineDwellTime[i];
                CpuTime = FromSol.CpuTime;
                CplexObj = FromSol.CplexObj;
                CplexMipGap = FromSol.CplexMipGap;
                SolNumID = FromSol.SolNumID;
                BBLevel = FromSol.BBLevel;
                TotalPasCostCompute = FromSol.TotalPasCostCompute;
                TotalCostCompute = FromSol.TotalCostCompute;
                TotalOpCost = FromSol.TotalOpCost;
                TotalPasCostCplex = FromSol.TotalPasCostCplex;
                SolSatus = FromSol.SolSatus;
                SolSovleType = FromSol.SolSovleType;
                for (int l = 0; l < v_Fre.Count; l++) { v_Fre[l] = FromSol.v_Fre[l]; v_Headway[l] = FromSol.v_Headway[l]; }
                for (int i = 0; i < v_Delta_Board_Veh.Count; i++) v_Delta_Board_Veh[i] = FromSol.v_Delta_Board_Veh[i];
                for (int i = 0; i < v_Y.Count; i++) v_Y[i] = FromSol.v_Y[i];
                for (int i = 0; i < v_Bilinear_w.Count; i++) v_Bilinear_w[i] = FromSol.v_Bilinear_w[i];
                for (int i = 0; i < v_PasPathWait.Count; i++) v_PasPathWait[i] = FromSol.v_PasPathWait[i];
                for (int i = 0; i < v_PathSchCapCost.Count; i++) v_PathSchCapCost[i] = FromSol.v_PathSchCapCost[i];
                for (int i = 0; i < v_PathFreCapCost.Count; i++) v_PathFreCapCost[i] = FromSol.v_PathFreCapCost[i];
                for (int i = 0; i < v_TrainTerminalDep.Count; i++) v_TrainTerminalDep[i] = FromSol.v_TrainTerminalDep[i];
                for (int l = 0; l < HeadwayLp.Count; l++)
                {
                    HeadwayLp[l] = FromSol.HeadwayLp[l];
                    HeadwayUp[l] = FromSol.HeadwayUp[l];
                }
                m_FixedLineHeadway.Clear();
                foreach (int key in FromSol.m_FixedLineHeadway.Keys) m_FixedLineHeadway.Add(key, FromSol.m_FixedLineHeadway[key]);
            }

            public void CompareUpperBound(SolClass UB, out bool Replace, out NODESTA NodeStatus, BBCompareType CompareVal)
            {
                double UBVal = double.MaxValue;
                double ThisVal = double.MaxValue;

                switch (CompareVal)
                {
                    case BBCompareType.CplexObj:
                        UBVal = UB.CplexObj; ThisVal = CplexObj;
                        break;
                    case BBCompareType.TotalCostCompute:
                        UBVal = UB.TotalCostCompute; ThisVal = TotalCostCompute;
                        break;
                }
                if (UBVal.Equals(double.MaxValue))
                {
                    NodeStatus = NODESTA.Branch;
                    Replace = true;
                    if (SolSatus == SOLSTA.EpsFeasible) NodeStatus = NODESTA.Stop;
                    return;
                }
                Replace = false;
                NodeStatus = NODESTA.IsNull;
                switch (SolSatus)
                {
                    case SOLSTA.EpsFeasible:
                        NodeStatus = NODESTA.Stop;
                        if (UB.SolSatus == SOLSTA.EpsFeasible)
                        {
                            if (ThisVal < UBVal) { Replace = true; }
                        }
                        if (UB.SolSatus == SOLSTA.InFeasible)
                        {
                            Replace = true; // only keep the feasible solution values
                        }
                        break;
                    case SOLSTA.InFeasible:
                        if (UB.SolSatus == SOLSTA.EpsFeasible)
                        {
                            if (ThisVal < UBVal) NodeStatus = NODESTA.Branch;
                            else NodeStatus = NODESTA.Stop;
                        }
                        if (UB.SolSatus == SOLSTA.InFeasible)
                        {
                            NodeStatus = NODESTA.Branch;
                        }
                        break;
                    case SOLSTA.IsNull:
                        Console.WriteLine("err: Node_Node Status is Null");
                        MyLog.Instance.Error("Node status is Null");
                        break;
                }
            }

            public void CompareLowerBound(SolClass LB, out bool Replace, out NODESTA NodeStatus, BBCompareType CompareVal)
            {
                double LbVal = double.MaxValue;
                double ThisVal = double.MaxValue;
                switch (CompareVal)
                {
                    case BBCompareType.CplexObj:
                        LbVal = LB.CplexObj; ThisVal = CplexObj;
                        break;
                    case BBCompareType.TotalCostCompute:
                        LbVal = LB.TotalCostCompute; ThisVal = TotalCostCompute;
                        break;
                }
                if (LbVal.Equals(double.MaxValue))
                {
                    Replace = true;
                    NodeStatus = NODESTA.Branch;
                    if (SolSatus == SOLSTA.EpsFeasible) NodeStatus = NODESTA.Stop;
                    return;
                }
                Replace = false;
                NodeStatus = NODESTA.IsNull;
                switch (SolSatus)
                {
                    case SOLSTA.EpsFeasible:
                        NodeStatus = NODESTA.Stop;
                        if (LB.SolSatus == SOLSTA.EpsFeasible)
                        {
                            if (ThisVal < LbVal) { Replace = true; }
                        }

                        if (LB.SolSatus == SOLSTA.InFeasible)
                        {
                            Replace = true; // ensure the boundary values are feasible
                        }
                        break;
                    case SOLSTA.InFeasible:
                        if (LB.SolSatus == SOLSTA.EpsFeasible)
                        {
                            if (ThisVal < LbVal) NodeStatus = NODESTA.Branch;
                            else NodeStatus = NODESTA.Stop;
                        }
                        if (LB.SolSatus == SOLSTA.InFeasible)
                        {
                            NodeStatus = NODESTA.Branch;
                        }
                        break;
                    case SOLSTA.IsNull:
                        break;
                }
            }
            public void CompareBest(SolClass Best, out bool Replace, BBCompareType CompareVal)
            {
                double ThisVal = double.MaxValue;
                double BestVal = double.MaxValue;
                switch (CompareVal)
                {
                    case BBCompareType.CplexObj:
                        ThisVal = CplexObj; BestVal = Best.CplexObj;
                        break;
                    case BBCompareType.TotalCostCompute:
                        ThisVal = TotalCostCompute; BestVal = Best.TotalCostCompute;
                        break;
                }
                Replace = false;
                switch (SolSatus)
                {
                    case SOLSTA.EpsFeasible:
                        if (Best.SolSatus == SOLSTA.EpsFeasible)
                        {
                            if (ThisVal < BestVal) Replace = true;
                        }
                        if (Best.SolSatus == SOLSTA.InFeasible)
                            Replace = true;  // using feasible solution to replace infeasible solution 
                        break;
                    case SOLSTA.InFeasible:
                        Replace = false;
                        break;
                    case SOLSTA.IsNull:
                        break;
                }

            }
            /// <summary>
            /// set frequency and headway 
            /// </summary>
            public void UpdateLines_SchAndHeadway(ref List<TransitLineClass> Lines, ref List<NodeClass> Nodes, LpInput LpData)
            {
                for (int l = 0; l < LpData.FreLineSet.Count; l++)
                {
                    int LineIndex = Lines.FindIndex(x => x.ID == LpData.FreLineSet[l].ID);
                    if (Lines[LineIndex].isInvolvedInDecsionVar)
                    {
                        Lines[LineIndex].Headway = v_Headway[l];
                    }
                    Dictionary<int, int> map_nodeId2DepartArrayIndex = new Dictionary<int, int>();
                    List<double> dwell = new List<double>();
                    int mapIndex = 0;

                    for (int s = 0; s < LpData.FreLineSet[l].Stops.Count; s++)
                    {
                        map_nodeId2DepartArrayIndex.Add(LpData.FreLineSet[l].Stops[s].ID, mapIndex);
                        for (int q = 0; q < PARA.IntervalSets.Count; q++)
                        {
                            int index = LpData.GetFreDwellExpIndex(LpData.FreLineSet[l].ID, s, q);
                            if (index >= 0)
                            {
                                dwell.Add(FreLineDwellTime[index]);
                            }
                            else
                            {
                                dwell.Add(PARA.DesignPara.MinDwellTime);
                            }
                            mapIndex++;
                        }
                    }
                    Lines[LineIndex].CreateFreLineDwell(ref Nodes, dwell, map_nodeId2DepartArrayIndex);
                }
                for (int l = 0; l < LpData.SchLineSet.Count; l++)
                {
                    int LineIndex = Lines.FindIndex(x => x.ID == LpData.SchLineSet[l].ID);
                    Dictionary<int, int> map_nodeId2DepartArrayIndex = new Dictionary<int, int>();
                    List<double> dep = new List<double>();
                    List<double> dwell = new List<double>();
                    int mapIndex = 0;
                    if (Lines[LineIndex].isInvolvedInDecsionVar)
                    {
                        foreach (NodeClass n in Nodes) n.ClearLineTimes(LineIndex);
                        for (int s = 0; s < Lines[LineIndex].Stops.Count - 1; s++)
                        {
                            map_nodeId2DepartArrayIndex.Add(Lines[LineIndex].Stops[s].ID, mapIndex);
                            for (int q = 0; q < Lines[LineIndex].NumOfTrains; q++)
                            {
                                double dt = getTrainDepTimeAtStop(Lines[LineIndex].ID, Lines[LineIndex].Stops[s].ID, q, LpData);
                                double dw = getTrainDwellTimeAtStop(Lines[LineIndex].ID, Lines[LineIndex].Stops[s].ID, q, LpData);
                                dep.Add(dt);
                                dwell.Add(dw);
                                mapIndex++;
                            }
                        }
                        Lines[LineIndex].CreateScheduleTable(ref Nodes, dep, dwell, map_nodeId2DepartArrayIndex);
                    }
                    map_nodeId2DepartArrayIndex.Clear();
                    dep.Clear();
                    dwell.Clear();
                }
            }

            /// <summary>
            /// lineID, stopID are the "true" ID number
            /// </summary>
            protected internal double getTrainDepTimeAtStop(int lineID, int StopID, int qIndex, LpInput LpData)
            {
                double val = -1;

                if (LpData.SchLineSet.FindIndex(x => x.ID == lineID) < 0)
                {
                    Console.WriteLine("Warning: the line to find the schedule is not a schedule based line");
                    Console.ReadLine();
                }

                for (int l = 0; l < LpData.SchLineSet.Count; l++)
                {
                    if (LpData.SchLineSet[l].ID != lineID) continue;
                    if (StopID == LpData.SchLineSet[l].Stops.Last().ID)
                        Console.WriteLine("Warning: The input stop is the last stop, which does not have a depature time");

                    for (int s = 0; s < LpData.SchLineSet[l].Stops.Count - 1; s++)
                    {
                        if (StopID != LpData.SchLineSet[l].Stops[s].ID) continue;
                        int trainDepIndex = LpData.GetTrainDepIndex(l, s, qIndex);
                        return TrainDepTimeAtEachStop[trainDepIndex];
                    }
                }
                return val;
            }
            protected internal double getTrainDwellTimeAtStop(int LineID, int StopId, int q, LpInput LpData)
            {
                double val = 0;
                for (int l = 0; l < LpData.SchLineSet.Count; l++)
                {
                    if (LpData.SchLineSet[l].ID != LineID) continue;
                    for (int s = 0; s < LpData.SchLineSet[l].Stops.Count - 1; s++)
                    {
                        if (LpData.SchLineSet[l].Stops[s].ID != StopId) continue;
                        int index = LpData.GetSchDwellExpIndex(LineID, s, q);
                        if (index == -1) continue;
                        return SchLineDwellTime[index];
                    }
                }
                return val;
            }
        }
    }
}

