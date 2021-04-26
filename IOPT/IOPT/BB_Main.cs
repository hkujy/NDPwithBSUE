// checked 2021-May
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
            MyModel.SetCplexModel(LpData, RootNode.HeadwayUp, RootNode.HeadwayLp, EmptySet, RootNode.RelaxedSol,
                                    isScratchModel: true);
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
                        MyLog.Instance.Info("BB_Solve: Current Branch objective can be not solved");
                    } // if cplexIsSolved
                    if (BranchQ.Count > 0) Terminate = false;
                    else Terminate = true;
                    if (CurrentNode.Level > Global.MaxBBLevel)
                    {
                        MyLog.Instance.Info("BB_Solve_Info: Maximum BB level integration has been reached");
                        Terminate = true;
                    }
                    if (BestSol.SolSatus.Equals(SOLSTA.EpsFeasible))
                    {
                        if (Math.Abs(BestSol.CplexObj / LBSol.CplexObj - 1) <= PARA.BBPara.EpsObj) Terminate = true;
                    }
                } while (!Terminate);
            }  // with file and write 

            if (PARA.DesignPara.AssignMent == AssignMethod.BCM)
            {
                getRemovePathSet(BestSol);
            }
            return BestSol;
        }

        /// <summary>
        /// Remove the path if the path violated the bcm
        /// </summary>
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
                            i, p, TripPathSetPie[p], TripPathSetPie.Min(), gapvalue, PARA.DesignPara.GetBcmValue());

                        if (gapvalue > PARA.DesignPara.GetBcmValue())
                        {
                            BestSol.Remove_PathSet.Add(p);
                        }
                    }
                }
            }
        }
    }
}
