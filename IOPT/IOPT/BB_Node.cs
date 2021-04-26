// checked 2021-May
using SolveLp;

namespace IOPT
{
    public partial class BBmain
    {
        public enum SOLSTA  // type for the solution stations: Eps means epsilon feasible
        { InFeasible, EpsFeasible, IsNull }
        public enum NODESTA  // Node status in the branch and bound tree
        { Branch, Stop, IsNull }
        public enum SOLSOVTYPE   // SOLution SOlV type:
        { Fixed, Relaxed, IsNull } // status of the how the node is solved
        // fixed means the frequency and headways are fixed and then the problem is solved 
        // relaxed: means it is solved via relaxation method

        public enum BBCompareType         /*which value to compare*/
        {
            CplexObj,
            TotalCostCompute
        }
        protected internal LpInput LpData { set; get; }
        protected internal BBmain() { LpData = new LpInput(); }

        protected internal class BBNode
        {
            public int Level { get; set; }
            public SOLSTA SolSatus { get; set; }
            public NODESTA NodeStatus;
            public SolClass FixedSol; // solution with fixed objective values
            public SolClass RelaxedSol; // solution with out fixed value
            public int BranchLine { get; set; }
            public double[] HeadwayUp { get; set; }  //upper bound and lower bound of the headway
            public double[] HeadwayLp { get; set; }
            public BBNode[] Children; // two child nodes: 0 is left, 1 is right one
            public BBNode(LpInput LpData) // initialization function
            {
                Level = 0;
                SolSatus = SOLSTA.IsNull;
                HeadwayUp = new double[LpData.NumFreLines]; HeadwayLp = new double[LpData.NumFreLines];
                Children = new BBNode[2];
                NodeStatus = NODESTA.IsNull;
                BranchLine = PARA.NULLINT;
                FixedSol = new SolClass(LpData);
                RelaxedSol = new SolClass(LpData);
            }
            /// <summary>
            ///  use the middle point to generate children node
            /// </summary>
            protected internal void GenerateChildren()
            {
                for (int l = 0; l < HeadwayUp.Length; l++)
                {
                    Children[0].HeadwayUp[l] = HeadwayUp[l]; Children[0].HeadwayLp[l] = HeadwayLp[l];
                    Children[1].HeadwayUp[l] = HeadwayUp[l]; Children[1].HeadwayLp[l] = HeadwayLp[l];
                    Children[0].Level = Level + 1;
                    Children[1].Level = Level + 1;
                }
                Children[0].HeadwayUp[BranchLine] = 0.5 * (HeadwayUp[BranchLine] + HeadwayLp[BranchLine]);
                Children[1].HeadwayLp[BranchLine] = 0.5 * (HeadwayLp[BranchLine] + HeadwayUp[BranchLine]);
            }

            /// <summary>
            /// Fixed the line headway for the lines that violate the constraints 
            /// This is supposed to provide an upper bound
            /// </summary>
            public bool FixedLineHeadwayReSolve(Lp MyModel, LpInput LpData)
            {
                Global.UseWarmStartUp = true;
                if (Level > 0) RelaxedSol.CopySolFromCplex(MyModel, LpData);
                RelaxedSol.getFixedLineMap(); // fixed the lines
                if (RelaxedSol.m_FixedLineHeadway.Count == 0)
                {
                    FixedSol.CopyFromSol(RelaxedSol);
                    Global.UseWarmStartUp = false;
                    return true;
                }
                MyModel.SetCplexModel(LpData, HeadwayUp, HeadwayLp, RelaxedSol.m_FixedLineHeadway,
                    RelaxedSol, isScratchModel: false); // set model with fixed lines
                bool isCplexSolved = MyModel.SolveModel(LpData);
                if (isCplexSolved)
                {
                    FixedSol.CopySolFromCplex(MyModel, LpData);
                    Global.UseWarmStartUp = false;
                    return true;
                }
                else
                {
                    MyLog.Instance.Info("FixedLineHeadwayReSolve_Warning: Cplex is not solved in the fixed line procedure");
                    return false;
                }
            }

            /// <summary>
            ///  Check and compare the upper and lower bound value
            /// </summary>
            protected internal void CheckLBValue(SolClass LB, SolClass UB, BBCompareType CompareVal)
            {
                double LBVal = double.MaxValue;
                double UBVal = double.MaxValue;
                switch (CompareVal)
                {
                    case BBCompareType.CplexObj:
                        LBVal = LB.CplexObj; UBVal = UB.CplexObj;
                        break;
                    case BBCompareType.TotalCostCompute:
                        LBVal = LB.TotalCostCompute; UBVal = UB.TotalCostCompute;
                        break;
                }

                if (LBVal > UBVal)
                {
                    LB.CopyFromSol(UB);
                }
            }

            /// <summary>
            /// compare the solution to determine whether to replace the solution 
            /// Meanwhile, we can also determine the upper and lower bound 
            /// </summary>
            protected internal void CompareSolu(SolClass UB, SolClass LB, SolClass Best)
            {
                bool Replace = false;
                FixedSol.CompareUpperBound(UB, out Replace, out NodeStatus, CompareVal: BBCompareType.CplexObj);
                if (Replace) UB.CopyFromSol(FixedSol);
                RelaxedSol.CompareLowerBound(LB, out Replace, out NodeStatus, CompareVal: BBCompareType.CplexObj);
                if (Replace) LB.CopyFromSol(RelaxedSol);
                FixedSol.CompareBest(Best, out Replace, CompareVal: BBCompareType.CplexObj);
                if (Replace) Best.CopyFromSol(FixedSol);
                RelaxedSol.CompareBest(Best, out Replace, CompareVal: BBCompareType.CplexObj);
                if (Replace) Best.CopyFromSol(RelaxedSol);
                CheckLBValue(LB, UB, CompareVal: BBCompareType.CplexObj);
            }
        }
    }
}
