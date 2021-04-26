// I do not want to touch this 7-Jan-2019
using System.Collections.Generic;
using System.Linq;
using ILOG.Concert;
using IOPT;
using System.Diagnostics;

// if capacity is added. the capacity flow need another constraint to be correlated to the arrival and departure time at a node
namespace SolveLp
{
    public partial class Lp
    {
        protected internal void OperationCost(BBmain.LpInput LpData, INumExpr ObjExpr)
        {
            for (int l = 0; l < LpData.FreLineSet.Count; l++)
            {
                ObjExpr = cplex.Sum(ObjExpr, cplex.Prod(v_Fre[l], PARA.DesignPara.FreOperationCost));
            }
        }

        protected internal void RelaxObj(BBmain.LpInput LpData,
                            List<double> ProbLb, List<double> ProbUb, List<double> PieLb, List<double> PieUb)
        {

            INumExpr expr = cplex.NumExpr();
            INumExpr objexpr = cplex.NumExpr();

            for (int p = 0; p < v_PathPie.Count(); p++)
            {
                Debug.Assert(LpData.PathSet[p].Trip.Demand > 0, "path trip demand is 0");
                expr = cplex.Diff(cplex.Sum(cplex.Prod(ProbLb[p], v_PathPie[p]), cplex.Prod(v_PathProb[p], PieLb[p])), ProbLb[p] * PieLb[p]);
                cplex.AddGe(v_RelaxObj[p], expr, "ReObj_1_" + p.ToString());

                expr = cplex.Diff(cplex.Sum(cplex.Prod(ProbUb[p], v_PathPie[p]), cplex.Prod(v_PathProb[p], PieUb[p])), ProbUb[p] * PieUb[p]);
                cplex.AddGe(v_RelaxObj[p], expr, "ReObj_2_" + p.ToString());

                expr = cplex.Diff(cplex.Sum(cplex.Prod(ProbUb[p], v_PathPie[p]), cplex.Prod(v_PathProb[p], PieLb[p])), ProbUb[p] * PieLb[p]);
                cplex.AddLe(v_RelaxObj[p], expr, "ReObj_3_" + p.ToString());

                expr = cplex.Diff(cplex.Sum(cplex.Prod(ProbLb[p], v_PathPie[p]), cplex.Prod(v_PathProb[p], PieUb[p])), ProbLb[p] * PieUb[p]);
                cplex.AddLe(v_RelaxObj[p], expr, "ReObj_4_" + p.ToString());

                objexpr = cplex.Sum(objexpr, cplex.Prod(v_RelaxObj[p], LpData.PathSet[p].Trip.Demand));
                cplex.AddGe(v_RelaxObj[p], 0);
            }
            //OperationCost(LpData, objexpr);
            for (int l = 0; l < LpData.FreLineSet.Count; l++)
            {
                objexpr = cplex.Sum(objexpr, cplex.Prod(v_Fre[l], PARA.DesignPara.FreOperationCost));
            }
            cplex.AddMinimize(objexpr, "RelaxObjSum");

        }
    }
}
