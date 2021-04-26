// checked 2021-May
using System;
using System.Collections.Generic;
using ILOG.Concert;
using IOPT;

namespace SolveLp
{
    public partial class Lp
    {
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

        // the following two estimate the upper and lower bound of the y value based on the formulation
        protected internal double Bcm_y_expr_val(double theta, double dif, double bound)
        {
            return Math.Exp(-1.0 * theta * (dif - bound)) - 1; ;
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
            // Ln relax Z and
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                if (PARA.DesignPara.BcmMaxCostDif < 0)
                    PARA.DesignPara.BcmMaxCostDif = PARA.DesignPara.GetBcmValue();
                double y_lower = Bcm_y_expr_val(PARA.DesignPara.Theta, PARA.DesignPara.BcmMaxCostDif, PARA.DesignPara.GetBcmValue()); 
                double y_upper = Bcm_y_expr_val(PARA.DesignPara.Theta, 0, PARA.DesignPara.GetBcmValue());
                y_lower = Math.Max(y_lower, PARA.ZERO / 1000);
                //Ln z
                LinearLn(cplex, v_Bcm_z[p], v_Bcm_z_Lb[p], v_Bcm_y[p], v_Bcm_LnZ_BigA[p], v_Bcm_LnZ_u[p], v_Bcm_LnZ_J[p], LpData, y_lower, y_upper);
                // Ln m
                LinearLn(cplex, v_Bcm_m[p], v_Bcm_m_Lb[p], v_Bcm_one_plus_y[p], v_Bcm_LnM_BigA[p], v_Bcm_LnM_u[p], v_Bcm_LnM_J[p], LpData, y_lower+1, y_upper+1);
            }

            // add equality constraints
            for (int p = 0; p < LpData.NumOfPath; p++)
            {
                cplex.AddEq(v_Bcm_one_plus_y[p], cplex.Sum(1, v_Bcm_y[p]));
                cplex.AddEq(v_Bcm_m[p], ThetaExpr[p]);
                cplex.AddGe(v_Bcm_m[p], v_Bcm_m_Lb[p]);
                cplex.AddGe(v_Bcm_z[p], v_Bcm_z_Lb[p]);
            }
            BcmPathProbDifEq(LpData);
        }
    }
}

