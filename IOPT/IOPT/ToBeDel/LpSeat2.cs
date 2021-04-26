using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILOG.Concert;
using ILOG.CPLEX;
using System.Diagnostics;
using IOPT;

// for modeling the seat capacity constraint

namespace SolveLp
{
    public partial class Lp
    {

        public void SeatCapSatusCon(Cplex cplex, INumVar[] v_PathSchCapCost, INumVar[] v_PathFreCapCost,
                    INumVar[] v_CongestionStatus, BBmain.LpInput LpData)
        {
            // define the status of link congestion
            //public Dictionary<Dictionary<int, Dictionary<int, int>>, int> m_UsedLink2CapStatus { get; set; }
            List<Dictionary<int, Dictionary<int, int>>> Keys = LpData.m_UsedLink2CapStatus.Keys.ToList();

            INumExpr SumExpr = cplex.NumExpr();
            INumExpr rhs = cplex.NumExpr();
            INumExpr lhs = cplex.NumExpr();
            SumExpr = cplex.IntExpr();

            //List <Dictionary<int, Dictionary<int, int>> Keys = LpData.m_UsedLink2CapStatus.ToList();
            for (int i = 0; i < Keys.Count; i++)
            {

                int StatusPos = LpData.m_UsedLink2CapStatus[Keys[i]];
                List<int> LineKeys = Keys[i].Keys.ToList();
                for (int l = 0; l < LineKeys.Count; l++)
                {
                    int LineId = LineKeys[l];
                    List<int> stopKey = Keys[LineId].Keys.ToList();
                    if (stopKey.Count > 1)
                    {
                        Console.WriteLine("error in the stop key location");
                    }
                    int BoardNode = Keys[i][LineId][stopKey[0]];
                    Console.WriteLine("LineId = {0}, BoardNode = {1}", LineId, BoardNode);
                    ///
                    if (LpData.FreLineSet.Exists(x => x.ID == LineId))
                    {
                        // add constraint for the frequency based line
                        for (int p = 0; p < LpData.PathSet.Count; p++)
                        {
                            if (LpData.PathSet[p].m_BoardNode2FreCapVar.ContainsKey(BoardNode))
                            {
                                int boardPos = LpData.PathSet[p].m_BoardNode2FreCapVar[BoardNode];
                                SumExpr = cplex.Sum(SumExpr, v_PathFreCapCost[boardPos]);
                                //for (int tau = 0; tau < PARA.IntervalSets.Count; tau++)
                                //{
                                //    SumExpr = cplex.Sum(SumExpr, v_PathFreCapCost[boardPos + tau]);
                                //}
                            }
                        }
                    }
                    else
                    {
                        // add for the schedule based one
                        for (int p = 0; p < LpData.PathSet.Count; p++)
                        {
                            if (LpData.PathSet[p].m_BoardNode2SchCapVar.ContainsKey(BoardNode))
                            {
                                int boardpos = LpData.PathSet[p].m_BoardNode2SchCapVar[BoardNode];
                                SumExpr = cplex.Sum(SumExpr, v_PathSchCapCost[boardpos]);
                            }

                        }
                    }


                    ///

                }

                cplex.AddLe(SumExpr, cplex.Prod(PARA.DesignPara.BigM, cplex.Diff(1, v_CongestionStatus[StatusPos])));
                cplex.AddGe(SumExpr, cplex.Prod(-1.0 * PARA.DesignPara.BigM, v_CongestionStatus[StatusPos]));
            }
            // definitional constraints for the seat delta variables
        }


        public void SeatCapConnect(Cplex cplex, INumVar[] v_CongestionStatus, INumVar[] v_SeatLamada, BBmain.LpInput LpData)
        {
            // the purpose of this to set the constraints that connects the Lamada variables and congestion status variables
            for (int p = 0; p < LpData.PathSet.Count; p++)
            {

                List<int> KeyLines = LpData.PathSet[p].LineSec.Keys.ToList();
                for (int l = 0; l < KeyLines.Count; l++)
                {
                    int LineId = KeyLines[l];
                    for (int s = 0; s < LpData.PathSet[p].LineSec[LineId].Count - 1; s = +2)
                    {
                        int TailNode = LpData.PathSet[p].LineSec[LineId][s];
                        int HeadNode = LpData.PathSet[p].LineSec[LineId][s + 1];

                        Dictionary<int, int> lamdakey = new Dictionary<int, int>();
                        lamdakey.Add(LineId, TailNode);
                        Dictionary<int, int> pairnode = new Dictionary<int, int>();
                        pairnode.Add(TailNode, HeadNode);
                        Dictionary<int, Dictionary<int, int>> statusKey = new Dictionary<int, Dictionary<int, int>>();
                        statusKey.Add(LineId, pairnode);

                        int lamdaPos = LpData.PathSet[p].m_LineSec2Lamda[lamdakey];
                        int statuPos = LpData.m_UsedLink2CapStatus[statusKey];


                        if (s == 0) // boarding stop
                        {
                            cplex.AddEq(v_CongestionStatus[statuPos], v_SeatLamada[lamdaPos]);
                        }
                        else
                        {
                            cplex.AddLe(v_CongestionStatus[statuPos], v_SeatLamada[lamdaPos]);

                            int PreStop = LpData.PathSet[p].LineSec[LineId][s - 1];
                            Dictionary<int, int> Prelamdakey = new Dictionary<int, int>();
                            Prelamdakey.Add(LineId, PreStop);
                            int PreLamdaPos = LpData.PathSet[p].m_LineSec2Lamda[Prelamdakey];

                            cplex.AddGe(cplex.Sum(v_CongestionStatus[statuPos], v_SeatLamada[PreLamdaPos]), v_SeatLamada[lamdaPos]);
                        }
                    }
                }


            }
        }

        public void SeatCon(Cplex cplex, INumVar[] v_PathSchCapCost, INumVar[] v_PathFreCapCost,
                    INumVar[] v_CongestionStatus, INumVar[] v_SeatLamada, BBmain.LpInput LpData)
        {

            DefSeatLamda(cplex, v_SeatLamada, LpData);
            SeatCapSatusCon(cplex, v_PathSchCapCost, v_PathFreCapCost, v_CongestionStatus, LpData);
            SeatCapConnect(cplex, v_CongestionStatus, v_SeatLamada, LpData);

            // step 1: use line sequence to create the first constraint 
            //   SeatSecCon(Cplex cplex,  BBmain.LpInput LpData, INumVar[] v_SeatLamada)
            // step 2: create constraint for the congestion indicator 
            // step 3: create constraints that connect the congestion indicator

        }

    }
}
