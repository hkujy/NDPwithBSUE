// I do not want to touch this: 7-Jan-2019
using System;
using System.Collections.Generic;
using ILOG.Concert;
using ILOG.CPLEX;
using IOPT;

namespace SolveLp
{
    public partial class Lp
    {
        //v_LHS_l: variable, left hand side, l  ( Notations follow Liu and Wang global optimization paper)
        //v_LHS_l_Lb: variable for the left hand side lower bound value
        //v_RHS_Y: variable, right hand side, y
        // u_var_k. the dimension of which is xi
        protected internal void LinearLn(Cplex cplex, INumVar v_LHS_l, INumVar v_LHS_l_Lb, INumVar v_RHS_y,
                                IIntVar[] v_big_A, INumVar[] v_u_k, IIntVar v_J,
                                BBmain.LpInput LpData, double Lower,
                               double Upper)
        {
            // Linearization the log function with input of right hand side variable and left hand side variables
            // and the upper and lower bounds values for the right side variable

            //Lower = Math.Max(Lower, PARA.ZERO/1000);
            /// linear relax of the sue
            List<double> BreakPoints = new List<double>();
            INumExpr expr = cplex.NumExpr();

            /******************* Equation (34)******************************************************/
            //Console.WriteLine("34");

            for (int i = 1; i <= PARA.DesignPara.NumOfBreakPoints; i++)
            {
                //BreakPoints.Add(PARA.ZERO + (i - 1) * 1.0 / (PARA.DesignPara.NumOfBreakPoints - 1));
                //i, Lower + (i - 1) * (Upper - Lower) / (PARA.DesignPara.NumOfBreakPoints - 1));
                BreakPoints.Add(Lower + (i - 1) * (Upper - Lower) / (PARA.DesignPara.NumOfBreakPoints - 1));
                BreakPoints[i-1] = Math.Max(BreakPoints[i-1], 0.0000000000001);
            }

            /******************** Equation (35) *************************************************/
            //Console.WriteLine("35");
            //Console.WriteLine("lower value = {0}", Lower);
            // tang upper bound
            for (int b = 0; b < PARA.DesignPara.NumOfBreakPoints; b++)
            {
                //Console.WriteLine("b={0},value={1}", b, BreakPoints[b]);
                cplex.AddLe(v_LHS_l, cplex.Sum(Math.Log(BreakPoints[b]) - 1.0, cplex.Prod(1.0 / BreakPoints[b], v_RHS_y)));
                //    for (int path = 0; path < LpData.NumOfPath; path++)
                //    {
                //        //cplex.AddLe(v_Chi[path], cplex.Sum(Math.Log(BreakPoints[b]) - 1.0, cplex.Prod(1.0 / BreakPoints[b], v_PathProb[path])));
                //        cplex.AddLe(v_Chi[path], cplex.Sum(Math.Log(BreakPoints[b]) - 1.0, cplex.Prod(1.0 / BreakPoints[b], v_PathProb[path])));
                //    }
            }

            /******************* Equation (36)******************************************************/
            // Equation (36)
            //Console.WriteLine("36");

            cplex.AddGe(v_LHS_l, v_LHS_l_Lb);

            //for (int p = 0; p < LpData.NumOfPath; p++)
            //{
            //    cplex.AddGe(v_Chi[p], v_ChiLb[p]);
            //}

            /******************* Equation (37)******************************************************/
            //Console.WriteLine("37");

            // range the prob value
            for (int i = 0; i < PARA.DesignPara.NumOfBreakPoints - 1; i++)
            {
                cplex.AddGe(v_RHS_y, cplex.Sum(BreakPoints[i], cplex.Prod(-1 * PARA.DesignPara.BigM, v_big_A[i])));
                cplex.AddLe(v_RHS_y, cplex.Sum(BreakPoints[i + 1], cplex.Prod(PARA.DesignPara.BigM, v_big_A[i])));
                //for (int path = 0; path < LpData.NumOfPath; path++)
                //{
                //    cplex.AddGe(v_PathProb[path], cplex.Sum(BreakPoints[i], cplex.Prod(-1 * PARA.DesignPara.BigM, v_BigA[path][i])));
                //    cplex.AddLe(v_PathProb[path], cplex.Sum(BreakPoints[i + 1], cplex.Prod(PARA.DesignPara.BigM, v_BigA[path][i])));
                //}
            }

            /******************* Equation (38)******************************************************/
            //Console.WriteLine("38");

            double Mulitplier;
            for (int i = 0; i < PARA.DesignPara.NumOfBreakPoints - 1; i++)
            {

                //for (int path = 0; path < LpData.NumOfPath; path++)
                //{
                Mulitplier = (Math.Log(BreakPoints[i + 1]) - Math.Log(BreakPoints[i])) / (BreakPoints[i + 1] - BreakPoints[i]);

                //expr = cplex.Sum(Math.Log(BreakPoints[i]), cplex.Prod(Mulitplier, cplex.Diff(v_PathProb[path], BreakPoints[i])));
                expr = cplex.Sum(Math.Log(BreakPoints[i]), cplex.Prod(Mulitplier, cplex.Diff(v_RHS_y, BreakPoints[i])));

                //expr = cplex.Diff(expr, cplex.Prod(PARA.DesignPara.BigM, v_BigA[path][i]));
                expr = cplex.Diff(expr, cplex.Prod(PARA.DesignPara.BigM, v_big_A[i]));
                //cplex.AddGe(v_ChiLb[path], expr);
                cplex.AddGe(v_LHS_l_Lb, expr);

                //expr = cplex.Sum(Math.Log(BreakPoints[i]), cplex.Prod(Mulitplier, cplex.Diff(v_PathProb[path], BreakPoints[i])));
                expr = cplex.Sum(Math.Log(BreakPoints[i]), cplex.Prod(Mulitplier, cplex.Diff(v_RHS_y, BreakPoints[i])));
                //expr = cplex.Sum(expr, cplex.Prod(PARA.DesignPara.BigM, v_BigA[path][i]));
                expr = cplex.Sum(expr, cplex.Prod(PARA.DesignPara.BigM, v_big_A[i]));
                //cplex.AddLe(v_ChiLb[path], expr);
                cplex.AddLe(v_LHS_l_Lb, expr);
                //}
            }

            /******************* Equation (39)******************************************************/
            //Console.WriteLine("39");

            // expression of A
            //for (int p = 0; p < LpData.NumOfPath; p++)
            //{
            for (int b = 1; b <= PARA.DesignPara.NumOfBreakPoints - 1; b++)
            {
                double Part1 = 0;
                double Part2Coe = 0;
                INumExpr Part2Exp = cplex.NumExpr();
                for (int k = 1; k <= xi; k++)
                {
                    Part1 += 1 - Math.Pow(-1, Math.Floor((b - 1) / (Math.Pow(2, k - 1))));
                    Part2Coe = Math.Pow(-1, Math.Floor((b - 1) / Math.Pow(2, k - 1)));
                    //Part2Exp = cplex.Sum(Part2Exp, cplex.Prod(Part2Coe, uVar[p][k - 1]));
                    Part2Exp = cplex.Sum(Part2Exp, cplex.Prod(Part2Coe, v_u_k[k - 1]));
                }
                //cplex.AddEq(v_BigA[p][b - 1], cplex.Sum(0.5 * Part1, Part2Exp));
                cplex.AddEq(v_big_A[b - 1], cplex.Sum(0.5 * Part1, Part2Exp));
            }
            //}
            /******************* Equations (40) and (41) ******************************************************/
            //Console.WriteLine("40/1");

            /// expression of J/psi 
            //for (int p = 0; p < LpData.NumOfPath; p++)
            //{
            INumExpr psiexp = cplex.NumExpr();
            for (int b = 0; b < xi; b++)
            {
                //psiexp = cplex.Sum(psiexp, cplex.Prod(Math.Pow(2, b), uVar[p][b]));
                psiexp = cplex.Sum(psiexp, cplex.Prod(Math.Pow(2, b), v_u_k[b]));
            }
            psiexp = cplex.Sum(psiexp, 1);
            //cplex.AddEq(PsiVarJ[p], psiexp);
            cplex.AddEq(v_J, psiexp);
            //cplex.AddGe(PsiVarJ[p], 1);
            cplex.AddGe(v_J, 1);
            //cplex.AddLe(PsiVarJ[p], PARA.DesignPara.NumOfBreakPoints - 1);

            //}

            /*******************************************************************************************/


        }

        internal void ProbSumEq1(BBmain.LpInput LpData)
        {

            // probability conversation 
            INumExpr ProbSum = cplex.NumExpr();
            for (int td = 0; td < LpData.TripPathSet.Count; td++)
            {
                ProbSum = cplex.NumExpr();
                INumExpr[] PathSetPieExpArr = new INumExpr[LpData.TripPathSet[td].Count];

                for (int p = 0; p < LpData.TripPathSet[td].Count; p++)
                {
                    PathSetPieExpArr[p] = PathCostExpr[LpData.TripPathSet[td][p]];
                    //PathSetPieExp.Add(PathCostExpr[LpData.TripPathSet[td][p]]);
                }

                for (int p = 0; p < LpData.TripPathSet[td].Count; p++)
                {
                    ProbSum = cplex.Sum(ProbSum, v_PathProb[LpData.TripPathSet[td][p]]);

                    cplex.AddLe(v_PathProb[LpData.TripPathSet[td][p]], 1 + PARA.ZERO);
                }
                cplex.AddEq(ProbSum, 1);

                //if (PARA.DesignPara.AssignMent.Equals(AssignMethod.BCM))
                //{
                //    for (int p = 0; p < LpData.TripPathSet[td].Count; p++)
                //    {
                //        //cplex.AddGe(v_PathPie[LpData.TripPathSet[td][p]], cplex.Min(PathSetPieExpArr));
                //        cplex.AddLe(PathCostExpr[LpData.TripPathSet[td][p]], cplex.Sum(cplex.Min(PathSetPieExpArr), PARA.DesignPara.BcmBound));
                //    }
                //}

            }
        }

        internal void LnRelax_Prob(BBmain.LpInput LpData)
        {
            // set the ln relaxation constraints
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                LinearLn(cplex, v_LnProb[p], v_LnProb_Lb[p], v_PathProb[p], v_LnProb_BigA[p], v_LnProb_u[p], v_LnProb_J[p], LpData, PARA.ZERO, 1);
                cplex.AddGe(v_LnProb[p], v_LnProb_Lb[p]);
            }
        }


        protected internal void RelaxRSUE_v2(BBmain.LpInput LpData)
        {
            LnRelax_Prob(LpData);
            ProbSumEq1(LpData);
            PathProbDifEq(cplex, LpData);
        }


        protected internal void PathProbDifEq(Cplex cplex, BBmain.LpInput LpData)
        {
            // equality constraints between two paths
            // it is not necessary to compare each pai
            for (int w = 0; w < LpData.TripPathSet.Count; w++)
            {
                for (int p0 = 0; p0 < LpData.TripPathSet[w].Count - 1; p0++)
                {
                    for (int p1 = p0 + 1; p1 < LpData.TripPathSet[w].Count; p1++)
                    {
                        int PathID_0 = LpData.TripPathSet[w][p0];
                        int PathID_1 = LpData.TripPathSet[w][p1];
                        cplex.AddEq(cplex.Diff(v_LnProb[PathID_0], v_LnProb[PathID_1]),
                           cplex.Prod(PARA.DesignPara.Theta, cplex.Diff(v_PathPie[PathID_1], v_PathPie[PathID_0])), "PathDif_" + p0.ToString() + "_" + p1.ToString());
                    }
                }
            }

        }

    }

}

