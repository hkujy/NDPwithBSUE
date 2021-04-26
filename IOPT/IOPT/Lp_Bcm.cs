// I do not want to touch this part 7-Jan-2019
using System;
using System.Collections.Generic;
using ILOG.Concert;
using IOPT;
using System.Diagnostics;

// if capacity is added. the capacity flow need another constraint to be correlated to the arrival and departure time at a node
namespace SolveLp
{
    public partial class Lp
    {
        // a basic version of the path probability choice equality comparison
        // Conclusion: the reduction in the number of constraints do not help the improvement of computation speed
        protected internal void BcmPathProbDifEq(BBmain.LpInput LpData)
        {
            for (int w = 0; w < LpData.TripPathSet.Count; w++)
            {
                for (int p0 = 0; p0 < LpData.TripPathSet[w].Count - 1; p0++)
                {
                    for (int p1 = p0 + 1; p1 < LpData.TripPathSet[w].Count; p1++)
                    {
                        int PathID_0 = LpData.TripPathSet[w][p0];
                        int PathID_1 = LpData.TripPathSet[w][p1];
                        cplex.AddEq(cplex.Diff(v_LnProb[PathID_0], v_LnProb[PathID_1]), cplex.Diff(v_Bcm_z[PathID_0], v_Bcm_z[PathID_1]),
                           "PathDif_" + p0.ToString() + "_" + p1.ToString());
                    }
                }
            }
        }

        protected internal void BcmPathProbDifEq_Reduction(BBmain.LpInput LpData)
        {
            ///<remarks>
            ///I have tried to compare the reduction, but it does not improve the computational performance 
            ///</remarks>
            ///
            // reduce the number of constraints added 
            for (int w = 0; w < LpData.TripPathSet.Count; w++)
            {
                for (int p0 = 0; p0 < LpData.TripPathSet[w].Count - 1; p0++)
                {
                    int p1 = p0 + 1;
                    int PathID_0 = LpData.TripPathSet[w][p0];
                    int PathID_1 = LpData.TripPathSet[w][p1];
                    cplex.AddEq(cplex.Diff(v_LnProb[PathID_0], v_LnProb[PathID_1]), cplex.Diff(v_Bcm_z[PathID_0], v_Bcm_z[PathID_1]),
                       "PathDif_" + p0.ToString() + "_" + p1.ToString());
                }
            }
        }

        // the following two estimate the upper and lower bound of the y value based on the formulation
        protected internal double Bcm_y_expr_val(double theta, double dif, double bound)
        {
            double val = double.MaxValue;

            //val = 1.0 - Math.Exp(-1.0 * theta * dif - bound);
            val =  Math.Exp(-1.0 * theta *( dif - bound))-1;

            return val;
        }

        protected internal void BCM(BBmain.LpInput LpData)
        {

            LnRelax_Prob(LpData);
            ProbSumEq1(LpData);
            // find min cost expression 
            List<INumExpr> OdMinCostExpr = new List<INumExpr>();
            for (int w = 0; w < LpData.TripPathSet.Count; w++)
            {
                OdMinCostExpr.Add(cplex.NumExpr());
                OdMinCostExpr[OdMinCostExpr.Count - 1] = cplex.Sum(cplex.IntExpr(), PARA.DesignPara.Infi);
            }

            List<INumExpr> ThetaExpr = new List<INumExpr>();
            for (int p = 0; p < LpData.NumOfPath; p++)
            { ThetaExpr.Add(cplex.NumExpr()); }

            for (int w = 0; w < LpData.TripPathSet.Count; w++)
            {
                for (int p = 0; p < LpData.TripPathSet[w].Count; p++)
                {
                    int PathId = LpData.TripPathSet[w][p];
                    OdMinCostExpr[w] = cplex.Min(PathCostExpr[PathId], OdMinCostExpr[w]);
                }
                for (int p = 0; p < LpData.TripPathSet[w].Count; p++)
                {
                    int PathId = LpData.TripPathSet[w][p];
                    ThetaExpr[PathId] = cplex.Prod(-1.0 * PARA.DesignPara.Theta,
                        cplex.Diff(cplex.Diff(PathCostExpr[PathId], OdMinCostExpr[w]), PARA.DesignPara.GetBcmValue()));
                }
            }
            //if (PARA.DesignPara.BcmMaxCostDif < 0) PARA.DesignPara.BcmMaxCostDif = PARA.DesignPara.BcmBound;
            // Ln relax Z and
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                if (PARA.DesignPara.BcmMaxCostDif < 0)
                    PARA.DesignPara.BcmMaxCostDif = PARA.DesignPara.GetBcmValue();
                double y_lower = Bcm_y_expr_val(PARA.DesignPara.Theta, PARA.DesignPara.BcmMaxCostDif, PARA.DesignPara.GetBcmValue()); 
                double y_upper = Bcm_y_expr_val(PARA.DesignPara.Theta, 0, PARA.DesignPara.GetBcmValue());
                y_lower = Math.Max(y_lower, PARA.ZERO / 1000);

                Debug.Assert(y_lower <= y_upper, "y lower bound is greater than upper bound");
                Debug.Assert(y_lower != 0, "y lower is equal to 0");
                Debug.Assert(y_upper != 0, "y upper is equal to 0");

                //Ln z
                LinearLn(cplex, v_Bcm_z[p], v_Bcm_z_Lb[p], v_Bcm_y[p], v_Bcm_LnZ_BigA[p], v_Bcm_LnZ_u[p], v_Bcm_LnZ_J[p], LpData, y_lower, y_upper);
                // Ln m
                LinearLn(cplex, v_Bcm_m[p], v_Bcm_m_Lb[p], v_Bcm_one_plus_y[p], v_Bcm_LnM_BigA[p], v_Bcm_LnM_u[p], v_Bcm_LnM_J[p], LpData, y_lower+1, y_upper+1);

            }


            // add equality constraints
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                cplex.AddEq(v_Bcm_one_plus_y[p], cplex.Sum(1, v_Bcm_y[p]));
                // m = theta expression
                cplex.AddEq(v_Bcm_m[p], ThetaExpr[p]);
                cplex.AddGe(v_Bcm_m[p], v_Bcm_m_Lb[p]);
                cplex.AddGe(v_Bcm_z[p], v_Bcm_z_Lb[p]);

            }

            BcmPathProbDifEq(LpData);

        }

    }

}

