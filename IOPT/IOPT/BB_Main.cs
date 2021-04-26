using System;
using System.Collections.Generic;
using SolveLp;
using System.IO;
using ILOG.Concert;
using System.Linq;

namespace IOPT
{
    public partial class BBmain
    {

        /// <summary>
        /// Main for the Branch and Bound
        /// </summary>
        /// <returns></returns>
        public SolClass MainSolve()
        {
            SolClass BestSol = BBSolve();
            return BestSol;
        }
        /// <summary>
        /// main procedure for the branch and bound algorithm
        /// </summary>
        /// <returns></returns>
        public SolClass BBSolve()
        {
            #region Ini
            Dictionary<int, double> EmptySet = new Dictionary<int, double>();
            SolClass LBSol = new SolClass(LpData);  // lower bound solution
            SolClass UBSol = new SolClass(LpData); // upper bound solution 
            SolClass BestSol = new SolClass(LpData); // best solution
            BBNode RootNode = new BBNode(LpData);  // root node for the branch and bound
            BBNode CurrentNode = new BBNode(LpData); // current node for the branch and bound
            LBSol.CplexObj = double.MinValue;
            bool CplexIsSolved = false;
            bool Terminate = false;
            #endregion

            // set root node
            for (int l = 0; l < LpData.NumFreLines; l++)
            {
                // set initial value for root node
                RootNode.HeadwayLp[l] = PARA.DesignPara.MinHeadway;
                RootNode.HeadwayUp[l] = PARA.DesignPara.MaxHeadway;
                RootNode.Level = 0;
            }
            Queue<BBNode> BranchQ = new Queue<BBNode>();
            Lp MyModel = new Lp(LpData);

            BranchQ.Enqueue(RootNode);
#if DEBUG
            Console.WriteLine("BB_Solve_Info: Start to Set ScratchModel Lp");
#endif
            MyModel.SetCplexModel(LpData, RootNode.HeadwayUp, RootNode.HeadwayLp, EmptySet, RootNode.RelaxedSol,
                                    isScratchModel: true);
#if DEBUG
            Console.WriteLine("BB_Solve_Info: Complete Set ScratchModel Lp");
#endif
            MyLog.Instance.Info("BB_Solve: Complete set scratch model");
            IObjective Obj = MyModel.cplex.GetObjective();
            string IterFile = MyFileNames.OutPutFolder + "BB_Iter.txt";
            using (StreamWriter file = new StreamWriter(IterFile, true))
            {
                if (Global.NumOfIter == 0)
                {
                    file.WriteLine("Iter,Level,RelaxObj,Status,FixedObj,Status,UbObj,Status,LbObj,Status,BestObj,Status,RelaxSolId,FixSolId");
                }
                do
                {
                    /// loop  all the nodes in the branch and bound queue
                    CurrentNode = BranchQ.Dequeue();
                    MyModel.SetCplexModel(LpData, CurrentNode.HeadwayUp, CurrentNode.HeadwayLp, EmptySet, CurrentNode.RelaxedSol, isScratchModel: false);
                    CplexIsSolved = MyModel.SolveModel(LpData);
                    #region check the tightest of the relaxed obj            
#if DEBUG
                    //// print and check objective value on the screen
                    Console.WriteLine("******Check and compare tightest of the relaxed objective*********");
                    for (int p = 0; p < MyModel.v_RelaxObj.Length; p++)
                    {
                        if (!CplexIsSolved) continue;
                        Console.Write("BB_Solve_Info: Path={0}, Relaxed Obj = {1} >= ", p, MyModel.cplex.GetValue(MyModel.v_RelaxObj[p]));
                        Console.WriteLine(MyModel.cplex.GetValue(MyModel.v_PathPie[p]) * LpData.ProbLb[p]
                                                    + MyModel.cplex.GetValue(MyModel.v_PathProb[p]) * LpData.PieLb[p] - LpData.ProbLb[p] * LpData.PieLb[p]);

                        Console.Write("BB_Solve_Info: Path={0}, Relaxed Obj = {1} >= ", p, MyModel.cplex.GetValue(MyModel.v_RelaxObj[p]));
                        Console.WriteLine(MyModel.cplex.GetValue(MyModel.v_PathPie[p]) * LpData.ProbUb[p]
                                                    + MyModel.cplex.GetValue(MyModel.v_PathProb[p]) * LpData.PieUb[p] - LpData.ProbUb[p] * LpData.PieUb[p]);

                        Console.Write("BB_Solve_Info: Path ={0}, Relaxed Obj = {1} <= ", p, MyModel.cplex.GetValue(MyModel.v_RelaxObj[p]));
                        Console.WriteLine(MyModel.cplex.GetValue(MyModel.v_PathPie[p]) * LpData.ProbUb[p]
                                                    + MyModel.cplex.GetValue(MyModel.v_PathProb[p]) * LpData.PieLb[p] - LpData.ProbUb[p] * LpData.PieLb[p]);

                        Console.Write("BB_Solve_Info: Path ={0}, Relaxed Obj = {1} <= ", p, MyModel.cplex.GetValue(MyModel.v_RelaxObj[p]));
                        Console.WriteLine(MyModel.cplex.GetValue(MyModel.v_PathPie[p]) * LpData.ProbLb[p]
                                                    + MyModel.cplex.GetValue(MyModel.v_PathProb[p]) * LpData.PieUb[p] - LpData.ProbLb[p] * LpData.PieUb[p]);
                    }
                    Console.WriteLine("******Complete*********************************");

#endif
                    #endregion
                    if (CplexIsSolved)
                    {
                        if (CurrentNode.Level == 0 || LpData.NumFreLines == 0)  // if this is the root node
                        {
                            CurrentNode.RelaxedSol.CopySolFromCplex(MyModel, LpData);
                            LBSol.CopyFromSol(CurrentNode.RelaxedSol);
                            RootNode.RelaxedSol.CopyFromSol(CurrentNode.RelaxedSol);
                            RootNode.RelaxedSol.BBLevel = 0;
                            RootNode.FixedSol.BBLevel = 0;
                            if (RootNode.RelaxedSol.SolSatus == SOLSTA.EpsFeasible) Terminate = true;
                        }

                        bool isFixedLineSolved = CurrentNode.FixedLineHeadwayReSolve(MyModel, LpData);
                        if (!isFixedLineSolved)
                        {
                            Console.WriteLine("BB_Main: Warning: fixed line head way can not be solved");
                            MyLog.Instance.Debug("BB_Main: Warning: fixed line head way can not be solved");
                        }
                        CurrentNode.RelaxedSol.BBLevel = CurrentNode.Level;
                        CurrentNode.FixedSol.BBLevel = CurrentNode.Level;
                        CurrentNode.CompareSolu(UBSol, LBSol, BestSol);

                        if (CurrentNode.NodeStatus == NODESTA.Branch)
                        {
                            CurrentNode.BranchLine = CurrentNode.RelaxedSol.getBranchLine();
                            CurrentNode.Children[0] = new BBNode(LpData);
                            CurrentNode.Children[1] = new BBNode(LpData);
                            CurrentNode.GenerateChildren();
                            BranchQ.Enqueue(CurrentNode.Children[0]);
                            BranchQ.Enqueue(CurrentNode.Children[1]);
                        }

                        file.Write("{0},", Global.NumOfIter);
                        file.Write("{0},", CurrentNode.Level);
                        file.Write("{0},{1},", CurrentNode.RelaxedSol.CplexObj, CurrentNode.RelaxedSol.SolSatus);
                        file.Write("{0},{1},", CurrentNode.FixedSol.CplexObj, CurrentNode.FixedSol.SolSatus);
                        file.Write("{0},{1},", UBSol.CplexObj, UBSol.SolSatus);
                        file.Write("{0},{1},", LBSol.CplexObj, LBSol.SolSatus);
                        file.Write("{0},{1},", BestSol.CplexObj, BestSol.SolSatus);
                        file.Write("{0},{1}", CurrentNode.RelaxedSol.SolNumID, CurrentNode.FixedSol.SolNumID);
                        file.Write(Environment.NewLine);
                    }
                    else
                    {
#if DEBUG
                        Console.WriteLine("BB_Solve_Warning: The Cplex Problem can not be solved, to be determined");
#endif
                        MyLog.Instance.Info("BB_Solve: Current Branch objective can be not solved");
                    } // if cplexIsSolved

                    // if there is no more nodes in the branch and bound queue, then the algorithm stops
                    if (BranchQ.Count > 0) Terminate = false;
                    else Terminate = true;

                    if (CurrentNode.Level > Global.MaxBBLevel)
                    {
#if DEBUG
                        Console.WriteLine("BB_Solve_Info: Maximum BB level integration has been reached");
#endif
                        MyLog.Instance.Info("BB_Solve_Info: Maximum BB level integration has been reached");
                        Terminate = true;
                    }

                    if (BestSol.SolSatus.Equals(SOLSTA.EpsFeasible))
                    {
                        if (Math.Abs(BestSol.CplexObj / LBSol.CplexObj - 1) <= PARA.BBPara.EpsObj) Terminate = true;
                    }
                } while (!Terminate);
#if DEBUG
                Console.WriteLine("Info_BB_Main: Best Sol Cplex Cost = {0}, CplexObj = {1}", BestSol.CplexObj, BestSol.CplexObj);
                Console.WriteLine("Branch and bound completes");
                /// Check and print the schedule time
#endif
            }  // with file and write 

            if (PARA.DesignPara.AssignMent == AssignMethod.BCM)
            {
                // only remove the path set if the solution method is bcm
                getRemovePathSet(BestSol);
            }
            return BestSol;
        }

        /// <summary>
        /// Remove the path if the path violated the bcm
        /// </summary>
        /// <param name="BestSol"></param>
        /// <returns></returns>
        protected internal void getRemovePathSet(SolClass BestSol)
        {
            BestSol.Remove_PathSet.Clear();
            for (int i = 0; i < LpData.TripPathSet.Count; i++)
            {
                using (StreamWriter file = new System.IO.StreamWriter(MyFileNames.OutPutFolder+ "Bcm_PathIter.txt", true))
                {
                    if (Global.NumOfIter == 0 && i == 0) file.WriteLine("Iter,OD,PathId,Pie,MinPie,Gap,GivenBound");
                    double[] expval = new double[LpData.TripPathSet[i].Count];
                    List<double> TripPathSetPie = new List<double>(LpData.TripPathSet[i].Count);
                    for (int p = 0; p < LpData.TripPathSet[i].Count; p++)
                    {
                        TripPathSetPie.Add(LpData.PathSet[LpData.TripPathSet[i][p]].PathPie);
                    }
                    double gapvalue = 0;
                    for (int p = 0; p < LpData.TripPathSet[i].Count; p++)
                    {
                        gapvalue = TripPathSetPie[p] - TripPathSetPie.Min();
                        file.WriteLine("{0},{1},{2},{3},{4},{5},{6}", Global.NumOfIter,
                            i, p, TripPathSetPie[p], TripPathSetPie.Min(), gapvalue, PARA.DesignPara.GetBcmValue(LpData.PathSet[p].Trip.BcmRatioValue));

                        if (gapvalue > PARA.DesignPara.GetBcmValue(LpData.PathSet[p].Trip.BcmRatioValue))
                        {
                            BestSol.Remove_PathSet.Add(p);
                        }
                    }
                }
            }
        }
    }
}

/// not used 
/// 
/// <summary>
/// Check the convergence condition based on the objective value
/// ** I think I only compare the cplex object in the final version, which makes sense, since the algoirhtm is based on obj
/// </summary>
/// <param name="UB"></param>
/// <param name="LB"></param>
/// <param name="Best"></param>
/// <param name="CompareVal"></param>
/// <returns></returns>
//protected internal bool IsTerminate(SolClass UB, SolClass LB, SolClass Best, BBCompareType CompareVal)
//{
//    if (!(Best.SolSatus == SOLSTA.EpsFeasible)) return false;
//    switch (CompareVal)
//    {
//        case BBCompareType.CplexObj:
//            if (Math.Abs(UB.CplexObj / LB.CplexObj - 1) <= PARA.BBPara.EpsObj) return true;
//            if (Math.Abs(Best.CplexObj / LB.CplexObj - 1) <= PARA.BBPara.EpsObj) return true;
//            break;
//        //case BBCompareType.TotalCostCplex:
//        //    if (Math.Abs(UB.TotalCost_Cplex / LB.TotalCost_Cplex - 1) <= PARA.BBPara.EpsObj) return true;
//        //    if (Math.Abs(Best.TotalCost_Cplex / LB.TotalCost_Cplex - 1) <= PARA.BBPara.EpsObj) return true;
//        //    break;
//        case BBCompareType.TotalCostCompute:
//            if (Math.Abs(UB.TotalCostCompute / LB.TotalCostCompute - 1) <= PARA.BBPara.EpsObj) return true;
//            if (Math.Abs(Best.TotalCostCompute / LB.TotalCostCompute - 1) <= PARA.BBPara.EpsObj) return true;
//            break;
//    }
//    return false;
//}
