using System;
using System.Collections.Generic;
using System.Linq;
using ILOG.Concert;
using ILOG.CPLEX;
using IOPT;

// for modeling the seat capacity constraint

namespace SolveLp
{
    public partial class Lp
    {

        /// <summary>
        /// definitional constraints for seat variable along a route
        /// </summary>
        /// <param name="cplex"></param>
        /// <param name="v_Delta_Seat"></param>
        /// <param name="LpData"></param>
        /// <param name="SolveModel"></param>
        /// <returns></returns>
        protected internal void DefDeltaSeat(Cplex cplex, INumVar[] v_Delta_Seat, BBmain.LpInput LpData,
                                bool SolveModel)
        {
            // constraint for the sequence of the seat
            for (int p = 0; p < LpData.PathSet.Count; p++)
            {
                List<int> KeyLines = LpData.PathSet[p].m_LineId_VistNodeOrder.Keys.ToList();
                for (int l = 0; l < KeyLines.Count; l++)
                {
                    if (LpData.PathSet[p].m_LineId_VistNodeOrder[KeyLines[l]].Count > 2) // if there only two nodes, then the last node would be the end the of this line
                    {
                        for (int s = 0; s < LpData.PathSet[p].m_LineId_VistNodeOrder[KeyLines[l]].Count - 2; s++)
                        {
                            // stop i lama<= stop i+1
                            int nowStop = LpData.PathSet[p].m_LineId_VistNodeOrder[KeyLines[l]][s];
                            int nextStop = LpData.PathSet[p].m_LineId_VistNodeOrder[KeyLines[l]][s + 1];
                            int nowStopDeltaSeatPos = FindDeltaSeatPos(LpData, p, nowStop, KeyLines[l]);
                            int nextStopDeltaSeatPos = FindDeltaSeatPos(LpData, p, nextStop, KeyLines[l]); 
                            if (SolveModel)
                            {
                                cplex.AddLe(v_Delta_Seat[nowStopDeltaSeatPos], v_Delta_Seat[nextStopDeltaSeatPos]);
                            }
                            else
                            {
                                if (cplex.GetValue(v_Delta_Seat[nowStopDeltaSeatPos])> cplex.GetValue(v_Delta_Seat[nextStopDeltaSeatPos]))
                                    Console.WriteLine("Lp_Seat_Warning: SeatLmanda Constraint violate: {0}<={1}", cplex.GetValue(v_Delta_Seat[nowStopDeltaSeatPos]), cplex.GetValue(v_Delta_Seat[nextStopDeltaSeatPos]));
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// defines the connection between the two variables, congestion and seat
        /// </summary>
        /// <param name="cplex"></param>
        /// <param name="v_PathFreCapCost"></param>
        /// <param name="v_Delta_Congest"></param>
        /// <param name="v_Delta_Board_Veh"></param>
        /// <param name="v_Delta_FreDep_t"></param>
        /// <param name="v_Delta_Seat"></param>
        /// <param name="LpData"></param>
        /// <param name="SolveModel"></param>
        /// <returns></returns>
        protected internal void DefConnection(Cplex cplex, INumVar[] v_PathFreCapCost,
                     INumVar[] v_Delta_Congest,
                     INumVar[] v_Delta_Board_Veh, // indicator for the train used
                     INumVar[] v_Delta_FreDep_t,   // departure time index for the frequency
                     INumVar[] v_Delta_Seat,
                     BBmain.LpInput LpData,
                     bool SolveModel)
        {
            for (int p = 0; p < LpData.PathSet.Count; p++)
            {
                int NowLamdaPos = 0;
                int CongStatusPos = 0;
                int NumOfq = 0;
                int TrainDeltaPos = -1;
                double rhsVal = 0.0, lhsVal = 0.0, SumVal = 0.0;

                INumExpr rhs = cplex.NumExpr(),lhs = cplex.NumExpr(),SumExp = cplex.NumExpr();

                TransitServiceType LineType = TransitServiceType.IsNull;
                List<int> KeyLines = LpData.PathSet[p].m_LineId_VistNodeOrder.Keys.ToList();

                for (int l = 0; l < KeyLines.Count; l++)
                {
                    int LineId = KeyLines[l];
                    // check the type of the line
                    if (LpData.SchLineSet.Exists(x => x.ID == LineId))
                    {
                        LineType = TransitServiceType.Schedule;
                        NumOfq = LpData.SchLineSet[LpData.SchLineSet.FindIndex(x => x.ID == LineId)].NumOfTrains;
                    }
                    if (LpData.FreLineSet.Exists(x => x.ID == LineId))
                    {
                        LineType = TransitServiceType.Frequency;
                    }

                    for (int s = 0; s < LpData.PathSet[p].m_LineId_VistNodeOrder[LineId].Count - 1; s++)
                    {
                        int NowStop = LpData.PathSet[p].m_LineId_VistNodeOrder[LineId][s];
                        NowLamdaPos = FindDeltaSeatPos(LpData, p, NowStop, KeyLines[l]);

                        if (s == 0)// the node is the boarding node for the line
                        {
                            if (LineType.Equals(TransitServiceType.Schedule))
                            {
                                for (int q = 0; q < NumOfq; q++)
                                {
                                    TrainDeltaPos = LpData.PathSet[p].m_NodeId_DeltaTrainBoard[NowStop] + q;
                                    CongStatusPos = FindDeltaCongestionPos(LpData, NowStop, KeyLines[l]) + q;

                                    if (SolveModel)
                                    {
                                        cplex.Add(cplex.IfThen(cplex.Eq(v_Delta_Board_Veh[TrainDeltaPos], 1), cplex.Eq(v_Delta_Seat[NowLamdaPos], v_Delta_Congest[CongStatusPos])));
                                    }
                                    else
                                    {
                                        if (cplex.GetValue(v_Delta_Board_Veh[TrainDeltaPos]).Equals(1))
                                        {
                                            if (!(cplex.GetValue(v_Delta_Seat[NowLamdaPos]).Equals(cplex.GetValue(v_Delta_Congest[CongStatusPos]))))
                                            Console.WriteLine("BoardStop Congestion=SeatLamda: {0} == {1}", cplex.GetValue(v_Delta_Seat[NowLamdaPos]), cplex.GetValue(v_Delta_Congest[CongStatusPos]));
                                        }
                                    }
                                }
                            }
                            else if (LineType.Equals(TransitServiceType.Frequency))
                            {
                                int deltapos = LpData.PathSet[p].m_Delta_FreDep_t_pos[NowStop];

                                for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                                {
                                    SumExp = cplex.IntExpr(); SumVal = 0;

                                    for (int tt = 0; tt < PARA.IntervalSets[tau].Count; tt++)
                                    {
                                        if (SolveModel) SumExp = cplex.Sum(SumExp, v_Delta_FreDep_t[deltapos + PARA.IntervalSets[tau][tt]]);
                                        else SumVal += cplex.GetValue(v_Delta_FreDep_t[deltapos + PARA.IntervalSets[tau][tt]]);
                                    }

                                    if (SolveModel)
                                    {
                                        cplex.Add(cplex.IfThen(cplex.Eq(SumExp, 1), cplex.Eq(v_Delta_Congest[CongStatusPos], v_Delta_Seat[NowLamdaPos])));
                                    }
                                    else
                                    {
                                        if (SumVal.Equals(1))
                                        {
                                            if (!(cplex.GetValue(v_Delta_Congest[CongStatusPos]).Equals(cplex.GetValue(v_Delta_Seat[NowLamdaPos]))))
                                            {
                                                Console.WriteLine("Lp_Seat_Warning: BoardStop Congestion=SeatLamda: {0} == {1}", cplex.GetValue(v_Delta_Congest[CongStatusPos]), cplex.GetValue(v_Delta_Seat[NowLamdaPos]));
                                            }
                                        }
                                    }
                                }
                            }
                            // boarding node 
                        }
                        else
                        {
                            int PreStop = LpData.PathSet[p].m_LineId_VistNodeOrder[LineId][s - 1];
                            int PreLamdaPos = FindDeltaSeatPos(LpData, p, PreStop, LineId);
                            if (LineType.Equals(TransitServiceType.Schedule))
                            {

                                for (int q = 0; q < NumOfq; q++)
                                {
                                    TrainDeltaPos = LpData.PathSet[p].m_NodeId_DeltaTrainBoard[NowStop] + q;
                                    CongStatusPos = FindDeltaCongestionPos(LpData, NowStop, LineId) + q;

                                    if (SolveModel)
                                    {
                                        cplex.Add(cplex.IfThen(cplex.Eq(v_Delta_Board_Veh[TrainDeltaPos], 1),
                                           cplex.Ge(v_Delta_Seat[NowLamdaPos], v_Delta_Congest[CongStatusPos])));

                                        lhs = cplex.Sum(v_Delta_Congest[CongStatusPos], v_Delta_Seat[PreLamdaPos]);
                                        rhs = v_Delta_Seat[NowLamdaPos];
                                        cplex.Add(cplex.IfThen(cplex.Eq(v_Delta_Board_Veh[TrainDeltaPos], 1), cplex.Ge(lhs, rhs)));

                                    }
                                    else
                                    {
                                        if (cplex.GetValue(v_Delta_Board_Veh[TrainDeltaPos]).Equals(1))
                                        {

                                            lhsVal = cplex.GetValue(v_Delta_Congest[CongStatusPos]) + cplex.GetValue(v_Delta_Seat[PreLamdaPos]);
                                            rhsVal = cplex.GetValue(v_Delta_Seat[NowLamdaPos]);

                                            if (cplex.GetValue(v_Delta_Congest[CongStatusPos])> cplex.GetValue(v_Delta_Seat[NowLamdaPos]) || rhsVal>lhsVal)

                                            Console.WriteLine("Nonboarding: Congestion<=Seat:{0}<={1} and {2}<={3}", cplex.GetValue(v_Delta_Congest[CongStatusPos]),
                                                cplex.GetValue(v_Delta_Seat[NowLamdaPos]), rhsVal, lhsVal);

                                        }
                                    }
                                }
                            }
                            else if (LineType.Equals(TransitServiceType.Frequency))
                            {

                                int deltapos = LpData.PathSet[p].m_Delta_FreDep_t_pos[NowStop];

                                for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                                {
                                    // first equation is the same as the case of the first stop
                                    SumExp = cplex.IntExpr(); SumVal = 0.0;

                                    for (int tt = 0; tt < PARA.IntervalSets[tau].Count; tt++)
                                    {
                                        if (SolveModel) SumExp = cplex.Sum(SumExp, v_Delta_FreDep_t[deltapos + PARA.IntervalSets[tau][tt]]);
                                        else SumVal += cplex.GetValue(v_Delta_FreDep_t[deltapos + PARA.IntervalSets[tau][tt]]);
                                    }

                                    if (SolveModel)
                                    {
                                        cplex.Add(cplex.IfThen(cplex.Eq(SumExp, 1), cplex.Ge(v_Delta_Seat[NowLamdaPos], v_Delta_Congest[CongStatusPos])));
                                        lhs = cplex.Sum(v_Delta_Congest[CongStatusPos], v_Delta_Seat[PreLamdaPos]);
                                        rhs = v_Delta_Seat[NowLamdaPos];
                                        cplex.Add(cplex.IfThen(cplex.Eq(SumExp, 1), cplex.Ge(lhs, rhs)));
                                     
                                    }
                                    else
                                    {
                                        if (SumVal.Equals(1))
                                        {
                                            lhsVal = cplex.GetValue(v_Delta_Congest[CongStatusPos]) + cplex.GetValue(v_Delta_Seat[PreLamdaPos]);
                                            rhsVal = cplex.GetValue(v_Delta_Seat[NowLamdaPos]);
                                            if (cplex.GetValue(v_Delta_Congest[CongStatusPos])> cplex.GetValue(v_Delta_Seat[NowLamdaPos]) ||rhsVal>lhsVal)
                                            Console.WriteLine("Nonboarding: Congestion<=Seat:{0}<={1} and {2}<={3}", cplex.GetValue(v_Delta_Congest[CongStatusPos]),
                                            cplex.GetValue(v_Delta_Seat[NowLamdaPos]), rhsVal, lhsVal);
                                        }
                                    }

                                }

                            }
                        }
                    }
                }
            }
        }

        protected internal void SeatCon(Cplex cplex, INumVar[] v_PathFreCapCost,
                    INumVar[] v_Delta_Congest, INumVar[] v_Delta_Seat,
                    INumVar[] v_Delta_Board_Veh, // indicator for the train used
                    INumVar[] v_Delta_FreDep_t,   // departure time index for the frequency
                    BBmain.LpInput LpData,
                    bool SolveModel)
        {
            DefDeltaSeat(cplex, v_Delta_Seat, LpData, SolveModel);
            DefConnection(cplex,  v_PathFreCapCost, v_Delta_Congest, v_Delta_Board_Veh, v_Delta_FreDep_t, v_Delta_Seat, LpData, SolveModel);
        }

    }
}
