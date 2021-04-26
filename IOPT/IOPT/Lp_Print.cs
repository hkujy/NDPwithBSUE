// checked 2021-May
using System;
using System.Linq;
using System.Diagnostics;
using IOPT;
namespace SolveLp
{
    public partial class Lp
    {
        protected internal void PrintScreen(BBmain.LpInput LpData)
        {
            for (int i = 0; i < v_PasPathWait.Count(); i++)
            {
                Console.WriteLine("PasPathWait_{0} = {1}", i, cplex.GetValue(v_PasPathWait[i]));
            }

            for (int p = 0; p < LpData.PathSet.Count; p++)
            {
                Console.WriteLine("PathFlow ={0}", cplex.GetValue(PathFlowExpr[p]));
            }

            for (int i = 0; i < PathCostExpr.Count; i++)
            {
                Console.WriteLine(cplex.GetValue(PathCostExpr[i]));
            }

            Console.WriteLine("-******-------revise and check the sch dwell time---******---");
            //for (int i=0;i<DwellTimeExpr_Sch.Count;i++)
            //{
            //    Console.WriteLine(cplex.GetValue(DwellTimeExpr_Sch[i]));
            //}

            Console.WriteLine("--------complete the check------");

            Console.WriteLine(cplex.GetObjValue());
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                Console.WriteLine("pathpie_{0} = {1}", p, cplex.GetValue(v_PathPie[p]));
                Console.WriteLine("pathprob_{0} = {1}", p, cplex.GetValue(v_PathProb[p]));
                LpData.PathSet[p].PathPie = cplex.GetValue(v_PathPie[p]);
                LpData.PathSet[p].PathProb = cplex.GetValue(v_PathProb[p]);
            }
            for (int l = 0; l < LpData.NumFreLines; l++)
            {
                Console.WriteLine("LineId={0}, h={1}, f={2}, prod={3}", l, cplex.GetValue(v_Headway[l]), cplex.GetValue(v_Fre[l]),
                    cplex.GetValue(v_Headway[l]) * cplex.GetValue(v_Fre[l]));
            }

            for (int i = 0; i < v_PathSchCapCost.Count(); i++)
            {
                Console.WriteLine("CapCost_{0} = {1}", i, cplex.GetValue(v_PathSchCapCost[i]));
            }
            Console.WriteLine("Output schedule");
            for (int p = 0; p < LpData.TotalNumOfTrains; p++)
            {
                Console.WriteLine("q={0}, time={1}", p, cplex.GetValue(v_TrainTerminalDep[p]));
            }
            Console.WriteLine("train dep");
            for (int p = 0; p < LpData.TotalNumOfTrains; p++)
            {
                Console.WriteLine("q={0}, dep ={1}", p, (cplex.GetValue(v_TrainTerminalDep[p])));
            }
            Console.WriteLine("train delta");
            for (int p = 0; p < LpData.DeltaPathTrainPos; p++)
            {
                Console.WriteLine("q={0},delta={1}", p, cplex.GetValue(v_Delta_Board_Veh[p]));
            }
            Console.WriteLine("wait time");
            for (int i = 0; i < v_PasPathWait.Count(); i++)
            {
                Console.WriteLine(cplex.GetValue(v_PasPathWait[i]));
            }
            Console.WriteLine("ChiVar");
            for (int i = 0; i < v_LnProb.Count(); i++)
            {
                Console.WriteLine(cplex.GetValue(v_LnProb[i]));
            }
            Console.WriteLine("ChiLbVar");
            for (int i = 0; i < v_LnProb.Count(); i++)
            {
                Console.WriteLine(cplex.GetValue(v_LnProb_Lb[i]));
            }
            Console.WriteLine("Relax Obj");
            for (int i = 0; i < v_RelaxObj.Count(); i++)
            {
                Console.WriteLine(cplex.GetValue(v_RelaxObj[i]));
            }
            Console.WriteLine("big A");
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                for (int b = 0; b < PARA.DesignPara.NumOfBreakPoints - 1; b++)
                    Console.WriteLine("path_{0},point_{1} = {2}", p, b, cplex.GetValue(v_LnProb_BigA[p][b]));
            }
            Console.WriteLine("u var");
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                for (int b = 0; b < xi; b++)
                {
                    Console.WriteLine("path_{0},xi_{1} = {2}", p, b, cplex.GetValue(v_LnProb_u[p][b]));
                }
            }
            Console.WriteLine("Psi");
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                Console.WriteLine("path_{0},psi = {1}", p, cplex.GetValue(v_LnProb_J[p]));
            }

        }

        protected internal void CplexOutputFreLine(BBmain.LpInput LpData)
        {
            string FileName = MyFileNames.OutPutFolder+ "LP_FreSol.txt";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(FileName))
            {
                file.WriteLine("id,fre,headway,prod");
                for (int l = 0; l < LpData.NumFreLines; l++)
                {
                    file.WriteLine("{0},{1},{2},{3}", LpData.FreLineSet[l].ID, cplex.GetValue(v_Fre[l]), cplex.GetValue(v_Headway[l]), cplex.GetValue(v_Fre[l]) * cplex.GetValue(v_Headway[l]));
                }
            }
        }

        protected internal void CplexCheckSol(BBmain.LpInput LpData)
        {
            // Check cap constraints
            Console.WriteLine("---------Start to check the solution---------------");
            SchCapCon(cplex, LpData, PathFlowExpr, PathCostExpr, DwellTimeExpr_Sch,
                v_Delta_Board_Veh, v_Y, v_PathSchCapCost, v_Delta_Congest, v_Delta_Seat, SolveModel: false);
            FreCapCon(cplex, LpData, PathFlowExpr, PathCostExpr, DwellTimeExpr_Fre, v_Delta_FreDep_t, v_Delta_FreArr_t, v_Ybar_Dep, v_Ybar_Arr, v_PathFreCapCost,
                v_PasPathDep, v_Fre, v_Delta_Congest, v_Delta_Seat, v_FreDwellTime,
                SolveModel: false);
            DefDeltaDepArr(cplex, DwellTimeExpr_Fre, v_Delta_FreDep_t, v_Delta_FreArr_t, v_PasPathDep, v_PasPathArr, LpData, SolveModel: false);
            SeatCon(cplex,  v_PathFreCapCost,
                    v_Delta_Congest, v_Delta_Seat, v_Delta_Board_Veh, v_Delta_FreDep_t, LpData, SolveModel: false);
            Console.WriteLine("---------Complete the check cplex solution---------------");

        }
        protected internal void CplexOutPutPasPas(BBmain.LpInput LpData)
        {
            //  output the passenger path
            string FileName = MyFileNames.OutPutFolder+ "LP_PasPath.txt";
            using (System.IO.StreamWriter file =
               new System.IO.StreamWriter(FileName))
            {
                for (int p = 0; p < LpData.NumOfPath; p++)
                {
                    file.Write("Path={0},", p);
                    for (int i = 0; i < LpData.PathSet[p].VisitNodes.Count; i++)
                    {
                        int nodeID = LpData.PathSet[p].VisitNodes[i];
                        file.Write("Node={0},", LpData.PathSet[p].VisitNodes[i]);
                        if (i != LpData.PathSet[p].VisitNodes.Count - 1)
                        {
                            int BoardLineID = LpData.PathSet[p].m_NodeID_NextLine[nodeID].ID;
                            int waitpos = LpData.PathSet[p].m_NodeId_WaitVarPos[nodeID];
                            file.Write("Wait={0:f4},", cplex.GetValue(v_PasPathWait[waitpos]));
                            int indexline = LpData.SchLineSet.FindIndex(x => x.ID == BoardLineID);
                            file.Write("Board={0},", LpData.SchLineSet[indexline].Name);
                            Console.WriteLine("Board={0},", LpData.SchLineSet[indexline].Name);
                            if (LpData.PathSet[p].m_NodeID_NextLine[nodeID].ServiceType.Equals(TransitServiceType.Schedule))
                            {
                                int TrainIndex = -1;
                                int delaPos = LpData.PathSet[p].m_NodeId_DeltaTrainBoard[nodeID];
                                int LineIndex = LpData.SchLineSet.FindIndex(x => x.ID == BoardLineID);
                                for (int q = 0; q < LpData.SchLineSet[LineIndex].NumOfTrains; q++)
                                {
                                    if (cplex.GetValue(v_Delta_Board_Veh[delaPos + q]).Equals(1))
                                    {
                                        file.Write("TrainNo={0},", q);
                                        TrainIndex = q;
                                    }
                                }
                                Debug.Assert(TrainIndex != -1, "TrainIndex is not solved");
                            }
                        }
                    }
                    file.Write(Environment.NewLine);
                }
            }
        }

    }
}

