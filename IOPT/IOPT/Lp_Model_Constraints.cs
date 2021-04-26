using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILOG.Concert;
using ILOG.CPLEX;
using IOPT;
using System.Diagnostics;


// if capacity is added. the capacity flow need another constraint to be correlated to the arrival and departure time at a node
namespace SolveLp
{
    public partial class Lp
    {

        public static int FindDeltaSeatPos(BBmain.LpInput LpData, int PathId, int NodeId, int LineId)
        {

            int line_index = LpData.PathSet[PathId].m_LineNodeID_DeltaSeatPos.FindIndex(x => x.LineID == LineId);

            int SeatIndex = LpData.PathSet[PathId].m_LineNodeID_DeltaSeatPos[line_index].m_BoardNodeId_DeltaPos[NodeId];

            return LpData.PathSet[PathId].m_LineNodeID_DeltaSeatPos[line_index].m_BoardNodeId_DeltaPos[NodeId];

        }
        protected static int FindDeltaCongestionPos(BBmain.LpInput LpData, int NodeId, int LineId)
        {

            int l_index = LpData.m_Link_Delta_Congest.FindIndex(x => x.LineId == LineId);
            int s_index = LpData.m_Link_Delta_Congest[l_index].TailList.FindIndex(x=>x==NodeId);

            return LpData.m_Link_Delta_Congest[l_index].PosList[s_index];

        }

        protected internal void BilinearFreConstraintUsingRange(
            BBmain.LpInput LpData, double[] FreUb, double[] FreLb,
            List<double> HeadwayUb, List<double> HeadwayLb,
            IList<IRange> RangeFre)
        {
            // bilinear frequency constraint
            INumExpr expr = cplex.NumExpr();
            for (int l = 0; l < LpData.FreLineSet.Count; l++)
            {
                RangeFre.Add(cplex.AddLe(v_Fre[l], FreUb[l]));
                RangeFre.Add(cplex.AddGe(v_Fre[l], FreLb[l]));

                RangeFre.Add(cplex.AddLe(v_Headway[l], HeadwayUb[l]));
                RangeFre.Add(cplex.AddGe(v_Headway[l], HeadwayLb[l]));

                expr = cplex.Diff(cplex.Sum(cplex.Prod(HeadwayLb[l], v_Fre[l]), cplex.Prod(v_Headway[l], FreLb[l])),
                                FreLb[l] * HeadwayLb[l]);

                RangeFre.Add(cplex.AddLe(cplex.Diff(expr, v_Bilinear_w[l]), 0));

                expr = cplex.Diff(cplex.Sum(cplex.Prod(HeadwayUb[l], v_Fre[l]), cplex.Prod(v_Headway[l], FreUb[l])),
                            FreUb[l] * HeadwayUb[l]);

                RangeFre.Add(cplex.AddLe(cplex.Diff(expr, v_Bilinear_w[l]), 0));


                expr = cplex.Diff(cplex.Sum(cplex.Prod(HeadwayUb[l], v_Fre[l]), cplex.Prod(v_Headway[l], FreLb[l])),
                                HeadwayUb[l] * FreLb[l]);

                RangeFre.Add(cplex.AddGe(cplex.Diff(expr, v_Bilinear_w[l]), 0));

                expr = cplex.Diff(cplex.Sum(cplex.Prod(HeadwayLb[l], v_Fre[l]),
                              cplex.Prod(v_Headway[l], FreUb[l])), HeadwayLb[l] * FreUb[l]);

                RangeFre.Add(cplex.AddGe(cplex.Diff(expr, v_Bilinear_w[l]), 0));

                RangeFre.Add(cplex.AddEq(v_Bilinear_w[l], 1.0));

            }
        }
        protected internal void DepTimeAndHeadwayConstraint(BBmain.LpInput LpData)

        {
            // constraints for the first and last departure 
            int VarPos = -1;
            INumExpr expr = cplex.NumExpr();
            for (int l = 0; l < LpData.SchLineSet.Count; l++)
            {
                VarPos = LpData.m_SchLineId_TrainTerminalDepVarPos[LpData.SchLineSet[l].ID];
                // add first train time is less than the headway
                cplex.AddLe(v_TrainTerminalDep[VarPos], PARA.DesignPara.MaxHeadway, "First_Dep_" + l.ToString());
                // add the last train is less the maximum departure time
                cplex.AddLe(v_TrainTerminalDep[VarPos + LpData.SchLineSet[l].NumOfTrains - 1], PARA.DesignPara.MaxLineOperTime, "Last_Dep_" + l.ToString());
            }

            // add headway between the two trains, only applicable to the two stops ??
            for (int l = 0; l < LpData.SchLineSet.Count; l++)
            {
                for (int q = 0; q < LpData.SchLineSet[l].NumOfTrains - 1; q++)
                {
                    VarPos = LpData.m_SchLineId_TrainTerminalDepVarPos[LpData.SchLineSet[l].ID] + q;
                    expr = cplex.Diff(v_TrainTerminalDep[VarPos + 1], v_TrainTerminalDep[VarPos]);
                    cplex.AddLe(expr, PARA.DesignPara.MaxHeadway,"HeadwayRangeMax_"+l.ToString());
                    cplex.AddGe(expr, PARA.DesignPara.MinHeadway,"HeadwayRangMin_"+l.ToString());
                }
            }
            ///<future>
            ///can be a future extension consider variable headway and determine hold time
            ///</future>
            // the following code 
            // the departure time of the last train  + travel time to the last stop  <  the maximum modelling horizon
            // remark: I think the following constraint is not binding and essential in the experiments
            for (int l = 0; l < LpData.SchLineSet.Count; l++)
            {
                int q = LpData.SchLineSet[l].NumOfTrains - 1;  // the last train
                VarPos = LpData.m_SchLineId_TrainTerminalDepVarPos[LpData.SchLineSet[l].ID] + q;

                #region FirstVersion
                //int LastStop = LpData.SchLineSet[l].Stops[LpData.SchLineSet[l].Stops.Count - 1].ID;
                //double addTime = LpData.SchLineSet[l].m_Stop_TimeDif[LpData.SchLineSet[l].ID][LastStop];
                // add additional time to take into account the minimum dwell time 
                #endregion

                // revised version
                // add time from the first stop to the last stop 
                // also add addition time to take into account the dwell time
                double addTime = LpData.SchLineSet[l].getTravelTimeBetweenStop(LpData.SchLineSet[l].Stops[0].ID,
                LpData.SchLineSet[l].Stops[LpData.SchLineSet[l].Stops.Count - 1].ID);
                addTime += PARA.DesignPara.MinDwellTime * LpData.SchLineSet[l].Stops.Count;
                // end of revised version

                ///<remarks>The following is the first version</remarks>
                //cplex.AddLe(cplex.Sum(v_TrainTerminalDep[VarPos], addTime), PARA.DesignPara.MaxTimeHorizon,
                //"LastTrainLessThanTimeHorizion_"+l.ToString());
                ///-----------------------------------------------------------------
                //Console.WriteLine("Wtf: Add Time = {0}", addTime); 
                cplex.AddLe(cplex.Sum(v_TrainTerminalDep[VarPos], addTime), PARA.DesignPara.MaxLineOperTime,
                "LastTrainLessThanTimeHorizion_" + l.ToString());
            }

        }
   
        /// <summary>
        /// the pass only arrive at a stop with in one interval of the time horizon
        /// </summary>
        /// <param name="cplex"></param>
        /// <param name="v_Delta_FreDep_t"></param>
        /// <param name="v_Delta_Arr_t"></param>
        /// <param name="v_PasPathDep"></param>
        /// <param name="v_PasPathArr"></param>
        /// <param name="LpData"></param>
        /// <param name="SolveModel"></param>
        /// <returns></returns>
        protected internal void DefDeltaDepArr(Cplex cplex,
                     List<INumExpr> DwellTimeExpr_Fre,
                     IIntVar[] v_Delta_FreDep_t,
                     IIntVar[] v_Delta_FreArr_t,
                     INumVar[] v_PasPathDep,
                     INumVar[] v_PasPathArr,
                     BBmain.LpInput LpData,
                     bool SolveModel)
        {

            INumExpr rhs = cplex.NumExpr();
            INumExpr lhs = cplex.NumExpr();
            INumExpr sum = cplex.NumExpr();
            double sumVal = 0;
            string Name;

            for (int p = 0; p < LpData.PathSet.Count; p++)
            {
                for (int i = 0; i < LpData.PathSet[p].VisitNodes.Count; i++)
                {
                    int node = LpData.PathSet[p].VisitNodes[i];

                    if (LpData.PathSet[p].m_NodeId_ArrVarPos.ContainsKey(node))
                    {
                        int ArrPos = LpData.PathSet[p].m_NodeId_ArrVarPos[node];
                        if (LpData.PathSet[p].m_Delta_Arr_t_pos.ContainsKey(node))
                        {
                            if (node == LpData.PathSet[p].VisitNodes[0] )
                            {
                                continue;
                            }
                            sum = cplex.IntExpr(); sumVal = 0.0;
                            int pos = LpData.PathSet[p].m_Delta_Arr_t_pos[node];
                            for (int t = 0; t < (int)PARA.DesignPara.MaxTimeHorizon; t++)
                            {
                                if (SolveModel) sum = cplex.Sum(sum, v_Delta_FreArr_t[pos + t]);
                                else sumVal += cplex.GetValue(v_Delta_FreArr_t[pos + t]);
                            }
                            Name = "DepArrConstraint:P=" + p + " node=" + node;
                            if (SolveModel)
                            {
                                cplex.AddEq(sum, 1, Name);

                            }
                            else
                            {
                                if (sumVal != 1)
                                    Console.WriteLine("Warning:SumDelat=1: {0}, sumDeltaArrVal = {1}", Name, sumVal);
                            }
                            for (int t = 0; t < (int)PARA.DesignPara.MaxTimeHorizon; t++)
                            {
                                if (SolveModel)
                                {
                                    lhs = cplex.Diff(v_PasPathArr[ArrPos], t);
                                    rhs = cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(v_Delta_FreArr_t[pos + t], 1));
                                    cplex.AddGe(lhs, rhs);
                                    //lhs = cplex.Diff(t + 1 - PARA.ZERO, v_PasPathArr[ArrPos]);
                                    lhs = cplex.Diff(t + 1, v_PasPathArr[ArrPos]);
                                    cplex.AddGe(lhs, rhs);
                                }
                                else
                                {
                                    if (cplex.GetValue(v_Delta_FreArr_t[pos + t]) == 1)
                                    {
                                        if (cplex.GetValue(v_PasPathArr[ArrPos]) - t < -PARA.ZERO || cplex.GetValue(v_PasPathArr[ArrPos]) - t - 1 > PARA.ZERO)
                                        {
                                            Console.WriteLine("t={0,1},PasArrTime = {2}, DeltaARR={3}", t, t + 1, cplex.GetValue(v_PasPathArr[ArrPos]), cplex.GetValue(v_Delta_FreArr_t[pos + t]));
                                        }
                                    }
                                }

                            }
                        }
                    }

                    if (LpData.PathSet[p].m_NodeId_DepVarPos.ContainsKey(node))
                    {
                        int DepTimeVarPos = LpData.PathSet[p].m_NodeId_DepVarPos[node];
                        if (LpData.PathSet[p].m_Delta_FreDep_t_pos.ContainsKey(node))
                        {
                            sum = cplex.IntExpr(); sumVal = 0.0;
                            int DeltaPos = LpData.PathSet[p].m_Delta_FreDep_t_pos[node];
                            for (int t = 0; t < (int)PARA.DesignPara.MaxTimeHorizon; t++)
                            {
                                if (SolveModel) sum = cplex.Sum(sum, v_Delta_FreDep_t[DeltaPos + t]);
                                else sumVal += cplex.GetValue(v_Delta_FreDep_t[DeltaPos + t]);
                            }
                            // must dep from the node at some time interval
                            Name = "DepFromNode:P=" + p + " node=" + node;
                            if (SolveModel)
                            {
                                cplex.AddEq(sum, 1, Name);
                            }
                            else
                            {
                                if (sumVal != 1)
                                    Console.WriteLine("Warning: SumDelat=1: {0}, sumDeltDepaVal = {1}", Name, sumVal);
                            }
                            for (int t = 0; t < (int)PARA.DesignPara.MaxTimeHorizon; t++)
                            {
                                if (SolveModel)
                                {
                                    lhs = cplex.Diff(v_PasPathDep[DepTimeVarPos], t);
                                    rhs = cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(v_Delta_FreDep_t[DeltaPos + t], 1));
                                    cplex.AddGe(lhs, rhs);
                                    lhs = cplex.Diff(t + 1 - PARA.ZERO, v_PasPathDep[DepTimeVarPos]);
                                    cplex.AddGe(lhs, rhs);
                                }
                                else
                                {
                                    if (cplex.GetValue(v_Delta_FreDep_t[DeltaPos + t]) == 1)
                                    {
                                        if (cplex.GetValue(v_PasPathDep[DepTimeVarPos]) - t < -PARA.ZERO || cplex.GetValue(v_PasPathDep[DepTimeVarPos]) - t - 1 > PARA.ZERO)
                                        {
                                            Console.WriteLine("Lp_Model_Constraint_Warning: DefDeltaDepArr: TimeInterval = {0,1}, PasDepTime = {2}, DeltaDep = {3}", t, t + 1, cplex.GetValue(v_PasPathDep[DepTimeVarPos]), cplex.GetValue(v_Delta_FreDep_t[DeltaPos + t]));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

