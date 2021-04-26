// checked 7-Jan-2019
using System;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace IOPT
{

    public partial class BBmain
    {
        public partial class SolClass
        {
            protected internal void PrintInMainIter(LpInput LpData,Stopwatch IterTime,int NumOfPathAdd, bool isOnScreen)
            {
                using (StreamWriter file = new StreamWriter(MyFileNames.OutPutFolder + "GlobalIter.txt",true))
                {

                    if (isOnScreen)
                        Console.WriteLine("{0},{1},{2},{3},{4},{5}",
                            Global.NumOfIter, LpData.PathSet.Count, CplexObj, TotalOpCost + TotalPasCostCompute, IterTime.ElapsedMilliseconds, NumOfPathAdd);
                    else
                        file.WriteLine("{0},{1},{2},{3},{4},{5}", Global.NumOfIter, LpData.PathSet.Count, CplexObj,
                         TotalOpCost + TotalPasCostCompute, IterTime.ElapsedMilliseconds, NumOfPathAdd);
                }

                using (StreamWriter bf = new StreamWriter(MyFileNames.OutPutFolder + "BB_Best_SolNum.txt", true))
                {
                    if (isOnScreen)
                        Console.WriteLine("{0},{1},{2},{3},{4},{5}", Global.NumOfIter, SolNumID, TotalCostCompute,
                        TotalPasCostCompute, TotalOpCost, CplexObj);
                    else
                    {
                        if (Global.NumOfIter == 0) bf.WriteLine("Iter,BestSolNum,TotalCost,PasCost,OpCost,BestCplObj");
                        bf.WriteLine("{0},{1},{2},{3},{4},{5}", Global.NumOfIter, SolNumID, TotalCostCompute,
                             TotalPasCostCompute, TotalOpCost, CplexObj);
                    }
                }
            }
            protected internal void PrintSol(LpInput LpData)
            {
                PrintSolSummary(LpData);
                PrintPath(LpData,  MyFileNames.OutPutFolder + "BB_LP_Path.txt");
                PrintFreLine(LpData);
                PrintSchLine(LpData);
                PrintPasPath(LpData, MyFileNames.OutPutFolder + "BB_LP_PasPath.txt");
            }
            //public void CplexOutputPath(Cplex cplex, INumVar[] PathPie, INumVar[] PathProb, INumVar[] Chi)
            protected internal void PrintPath(LpInput LpData, string FileName)
            {
                //string FileName = MyFileNames.OutPutFolder+ "BB_LP_Path.txt";

                using (StreamWriter file =
                         new StreamWriter(FileName, true))
                {
                    if (Global.NumOfIter == 0 && Global.BBSolNum == 0) file.WriteLine("Iter,SolNum,OD,PathId,Pie,Prob_Cplex,Prob_Compute,chi");
                    for (int p = 0; p < v_PathPie.Count(); p++)
                    {
                        file.WriteLine("{0},{1},{2},{3},{4:#.0000},{5:#.0000},{6:#.0000},{7:#.0000}",
                            Global.NumOfIter,//0
                            SolNumID,
                            LpData.PathSet[p].Trip.ID,
                            p,
                            v_PathPie[p],
                            v_PathProb[p],
                            Prob_Compute[p],
                            v_LnProb[p]);
                    }
                }
            }

            //public static void CplexOutputFreLine(Cplex cplex, INumVar[] Fre, INumVar[] Headway, LpInput LpData)
            protected internal void PrintFreLine(LpInput LpData)
            {
                string FileName = MyFileNames.OutPutFolder+ "BB_LP_Fre.txt";
                using (StreamWriter file = new System.IO.StreamWriter(FileName, true))
                {
                    if (Global.NumOfIter == 0 && Global.BBSolNum == 0) file.WriteLine("Iter,SolNum,LineId,Fre,Headway,Prod");
                    for (int l = 0; l < LpData.NumFreLines; l++)
                    {
                        file.WriteLine("{0},{1},{2},{3:#.0000},{4:#.0000},{5:#.0000}", Global.NumOfIter, SolNumID,
                            LpData.FreLineSet[l].ID, v_Fre[l], v_Headway[l], v_Fre[l] * v_Headway[l]);
                    }
                }
            }

            //public static void CplexOutputSchLine(Cplex cplex, INumVar[] TrainDep, LpInput LpData, Dictionary<int, int> m_SchLine2Var)
            protected internal void PrintSchLine(LpInput LpData)
            {
                string FileName = MyFileNames.OutPutFolder+ "BB_LP_Sch.txt";
                using (StreamWriter file =
                        new StreamWriter(FileName, true))
                {
                    if (Global.NumOfIter == 0 && Global.BBSolNum == 0)
                        file.WriteLine("Iter,SolNum,LineId,Stop,Train,Arr,Dwell,Dep");
                    for (int l = 0; l < LpData.SchLineSet.Count(); l++)
                    {
                        int index = LpData.m_SchLineId_TrainTerminalDepVarPos[LpData.SchLineSet[l].ID];
                        for (int q = 0; q < LpData.SchLineSet[l].NumOfTrains; q++)
                        {
                            double TerminalDep = v_TrainTerminalDep[index + q];
                            for (int s = 0; s < LpData.SchLineSet[l].Stops.Count; s++)
                            {
                                #region firstVersion
                                //// first version
                                //double TimeDif = LpData.SchLineSet[l].m_Stop_TimeDif[LpData.SchLineSet[l].ID][LpData.SchLineSet[l].Stops[s].ID];
                                //double arrTime;
                                //if (s == 0) arrTime = TerminalDep + TimeDif;
                                //else arrTime = TerminalDep + TimeDif - PARA.DesignPara.MinDwellTime;
                                //double depTime = TerminalDep + TimeDif;
                                //if (arrTime < 0) arrTime = 0; // adjust the arrival time if it is not defined for the first train 
                                #endregion
                                int LineID = LpData.SchLineSet[l].ID;
                                //Console.WriteLine("Lid = {0}, stop = {1}, maxstop = {2}", LineID, s, LpData.SchLineSet[l].Stops.Count);
                                double arrTime = LpData.SchLineSet[l].Stops[s].getArrTime(LineID, q);
                                double depTime = LpData.SchLineSet[l].Stops[s].getDepTime(LineID, q);
                                if (depTime < arrTime)
                                {
                                    Console.WriteLine("WTF: Deptime < Arrtime");
                                    Console.WriteLine("line = {0},q={1}", LineID, q);
                                    Console.WriteLine("Deptime = {0}, ArrTime = {1}", LineID, q);
                                    Console.ReadLine();
                                }
                                double dweTime = LpData.SchLineSet[l].Stops[s].getDwellTime(LineID, q);
                                file.WriteLine("{0},{1},{2},{3},{4},{5:#.00},{6:#.00},{7:#.00}", Global.NumOfIter, SolNumID,
                                    LpData.SchLineSet[l].ID, LpData.SchLineSet[l].Stops[s].ID, q, arrTime, dweTime, depTime);
#if DEBUG
                                Console.WriteLine("{0},{1},{2},{3},{4},{5:#.00},{6:#.00},{7:#.00}", Global.NumOfIter, SolNumID,
                                    LpData.SchLineSet[l].ID, LpData.SchLineSet[l].Stops[s].ID, q, arrTime, dweTime, depTime);
#endif
                            }
                        }
                    }
                }

                FileName = MyFileNames.OutPutFolder+ "BB_LP_SchAtTerminal.txt";
                using (StreamWriter file =
                        new StreamWriter(FileName, true))
                {
                    if (Global.NumOfIter == 0 && Global.BBSolNum == 0) file.WriteLine("Iter,LineId,Train,Dep");

                    for (int l = 0; l < LpData.SchLineSet.Count(); l++)
                    {
                        //int index = LpData.m_SchLineId_TrainTerminalDepVarPos[LpData.SchLineSet[l].ID];
                        int LineID = LpData.SchLineSet[l].ID;
                        for (int q = 0; q < LpData.SchLineSet[l].NumOfTrains; q++)
                        {
                            double TerminalDepNewVer = LpData.SchLineSet[l].Stops[0].getDepTime(LineID, q);
                            //double TerminalDep = v_TrainTerminalDep[index + q];
                            //file.WriteLine("{0},{1},{2},{3:#.0}", Global.NumOfIter, LpData.SchLineSet[l].ID, q, TerminalDep);
                            file.WriteLine("{0},{1},{2},{3:#.0}", Global.NumOfIter, LpData.SchLineSet[l].ID, q, TerminalDepNewVer);
                            //Console.WriteLine("{0},{1},{2},{3:#.0},{4:#.0}", Global.NumOfIter,
                            //       LpData.SchLineSet[l].ID, q, TerminalDep,TerminalDepNewVer);
                        }
                    }
                }
                //Console.WriteLine("Wtf: Check the print schedule arrTime and depTime");
                //Console.ReadLine();
            }
            protected internal void PrintSolSummary(LpInput LpData)
            {
                using (StreamWriter SolFile = new StreamWriter(MyFileNames.OutPutFolder+ "BB_Sol.txt", true))
                {
                    if (Global.NumOfIter == 0 && Global.BBSolNum == 0)
                    {
                        SolFile.WriteLine("SolNum,TotalPasCostCplex,TotalPasCostCompute,TotalOpCost,TotalCostCplex,TotalCostCompute, CplexObj,Cpu,CplexGap");
                    }
                    SolFile.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", Global.BBSolNum, TotalPasCostCplex, TotalPasCostCompute, TotalOpCost, CplexObj, TotalPasCostCompute,
                        CplexObj, CpuTime, CplexMipGap);
                }
            }

            //public static void CplexOutPutPasPas(Cplex cplex, LpInput LpData, INumVar[] PasPathWaitVar, INumVar[] DeltaVar)
            protected internal void PrintPasPath(LpInput LpData, string FileName)
            {
                //  output the passenger path
                //string FileName = MyFileNames.OutPutFolder+ "BB_LP_PasPath.txt";
                using (StreamWriter file = new StreamWriter(FileName, true))
                {
                    for (int p = 0; p < LpData.NumOfPath; p++)
                    {

                        file.Write("I={0},Sol={1},P={2},", Global.NumOfIter, SolNumID, p);
                        for (int i = 0; i < LpData.PathSet[p].VisitNodes.Count; i++)
                        {
                            int nodeID = LpData.PathSet[p].VisitNodes[i];
                            file.Write("Node={0},", LpData.PathSet[p].VisitNodes[i]);
                            if (i != LpData.PathSet[p].VisitNodes.Count - 1)
                            {
                                int BoardLineID = LpData.PathSet[p].m_NodeID_NextLine[nodeID].ID;
                                int DeltaSeat = SolveLp.Lp.FindDeltaSeatPos(LpData, p, nodeID, BoardLineID);
                                int waitpos = -1;
                                if (LpData.PathSet[p].TranferNodes.Contains(nodeID))
                                {
                                    waitpos = LpData.PathSet[p].m_NodeId_WaitVarPos[nodeID];
                                    file.Write("Wait={0:f4},", v_PasPathWait[waitpos]);
                                }
                                else
                                    file.Write("on board,");
                                file.Write("seat={0},", v_Delta_Seat[DeltaSeat]);

                                if (LpData.PathSet[p].m_NodeID_NextLine[nodeID].ServiceType.Equals(TransitServiceType.Schedule))
                                {
                                    int CapVarPos = LpData.PathSet[p].m_NodeId_SchCapCostVarPos[nodeID];
                                    if (LpData.PathSet[p].TranferNodes.Contains(nodeID))
                                    {
                                        file.Write("BoardCapCost={0:#.00},", v_PathSchCapCost[CapVarPos]);
                                    }
                                    else
                                    {
                                        file.Write("OnBoardCapCost={0:#.00},", v_PathSchCapCost[CapVarPos]);
                                    }
                                }

                                if (LpData.PathSet[p].m_NodeID_NextLine[nodeID].ServiceType.Equals(TransitServiceType.Frequency))
                                {
                                    int CapVarPos = LpData.PathSet[p].m_NodeId_FreCapCostVarPos[nodeID];
                                    if (LpData.PathSet[p].TranferNodes.Contains(nodeID))
                                    {
                                        file.Write("BoardCapCost={0},", v_PathFreCapCost[CapVarPos]);
                                    }
                                    else
                                    {
                                        file.Write("OnBoardCapCost={0},", v_PathFreCapCost[CapVarPos]);
                                    }
                                }

                                //file.Write("Board={0},", LpData.PathSet[p].m_NodeId_BoardLineId[node]);
                                int indexline = LpData.SchLineSet.FindIndex(x => x.ID == BoardLineID);
                                //if (indexline >= 0) file.Write("Board={0},", LpData.SchLineSet[indexline].Name);
                                if (indexline >= 0) file.Write("Board={0},", LpData.SchLineSet[indexline].ID);
                                indexline = LpData.FreLineSet.FindIndex(x => x.ID == BoardLineID);
                                //if (indexline >= 0) file.Write("Board={0},", LpData.FreLineSet[indexline].Name);
                                if (indexline >= 0) file.Write("Board={0},", LpData.FreLineSet[indexline].ID);

                                if (LpData.PathSet[p].m_NodeID_NextLine[nodeID].ServiceType.Equals(TransitServiceType.Schedule))
                                {
                                    int TrainIndex = -1;
                                    int delaPos = LpData.PathSet[p].m_NodeId_DeltaTrainBoard[nodeID];
                                    int LineIndex = LpData.SchLineSet.FindIndex(x => x.ID == BoardLineID);
                                    for (int q = 0; q < LpData.SchLineSet[LineIndex].NumOfTrains; q++)
                                    {
                                        if (v_Delta_Board_Veh[delaPos + q].Equals(1))
                                        {
                                            file.Write("VehNo={0},", q);
                                            TrainIndex = q;
                                        }
                                    }
                                    Debug.Assert(TrainIndex != -1, "VehNo index is not solved");
                                }
                            }
                        }
                        file.Write(Environment.NewLine);
                    }
                }
                ///<remarks>
                ///for the destination node, the values are default set to be -999
                ///</remarks>
                FileName = MyFileNames.OutPutFolder+ "BB_Lp_PasPath_Data.txt";
                using (StreamWriter file = new StreamWriter(FileName, true))
                {
                    if (Global.NumOfIter == 0 && SolNumID == 0)
                    {
                        file.WriteLine("Iter,SolNum,OD,Path,Node,Wait,Line,Veh,Seat,Cap,PathCost,PathProb,PathFlow,SegTime,ArrTime,DepTime");
                    }
                    for (int p = 0; p < LpData.NumOfPath; p++)
                    {
                        double NowDepTime = LpData.PathSet[p].Trip.TargetDepTime;
                        double NowArrTime = LpData.PathSet[p].Trip.TargetDepTime;
                        for (int i = 0; i < LpData.PathSet[p].VisitNodes.Count; i++)
                        {
                            int NodeID = LpData.PathSet[p].VisitNodes[i];
                            int BoardLineId = -999;
                            double WaitingTime = -999;
                            int SeatIndex = -999;
                            double CapCost = -999;
                            int Veh = -999;

                            double SegTimeVal = -999;
                            if (i != LpData.PathSet[p].VisitNodes.Count - 1)
                            {
                                BoardLineId = LpData.PathSet[p].m_NodeID_NextLine[NodeID].ID;
                                int DeltaSeat = SolveLp.Lp.FindDeltaSeatPos(LpData, p, NodeID, BoardLineId);
                                if (LpData.PathSet[p].TranferNodes.Contains(NodeID))
                                {
                                    int WaitPos = LpData.PathSet[p].m_NodeId_WaitVarPos[NodeID];
                                    WaitingTime = v_PasPathWait[WaitPos];
                                }
                                else
                                {
                                    WaitingTime = 0;
                                }
                                if (LpData.PathSet[p].TranferNodes.Contains(NodeID) && i != 0)
                                {
                                    // first version
                                    if (LpData.PathSet[p].m_NodeID_NextLine[NodeID].ServiceType == TransitServiceType.Frequency)
                                    {
                                        NowDepTime = WaitingTime + NowArrTime + PARA.PathPara.MinTransferTime;
                                    }
                                    else
                                    {
                                        int TrainIndex = -1;
                                        int delaPos = LpData.PathSet[p].m_NodeId_DeltaTrainBoard[NodeID];
                                        int LineIndex = LpData.SchLineSet.FindIndex(x => x.ID == BoardLineId);
                                        for (int q = 0; q < LpData.SchLineSet[LineIndex].NumOfTrains; q++)
                                        {
                                            if (v_Delta_Board_Veh[delaPos + q].Equals(1))
                                            {
                                                TrainIndex = q;
                                            }
                                        }

                                        #region FirstVersion
                                        // the following four lines are from the first version
                                        //int SchLinePos = LpData.m_SchLineId_TrainTerminalDepVarPos[BoardLineId];
                                        //double TrainDepTime = v_TrainTerminalDep[SchLinePos + TrainIndex];

                                        //double TimeDif = LpData.SchLineSet[LineIndex].m_Stop_TimeDif[LpData.SchLineSet[LineIndex].ID][NodeID];
                                        //NowDepTime = TrainDepTime + TimeDif;
                                        // end of the first version 
                                        // revised version
                                        #endregion

                                        int lIndex = LpData.SchLineSet.FindIndex(x => x.ID == BoardLineId);
                                        int sIndex = LpData.SchLineSet[lIndex].Stops.FindIndex(x => x.ID == NodeID);
                                        double DepTime = LpData.SchLineSet[lIndex].Stops[sIndex].getDepTime(BoardLineId, TrainIndex);

                                        NowDepTime = DepTime;
#if DEBUG
                                        Console.WriteLine("check print PrintPasPath: l = {0}, s = {1}, dep = {2}", BoardLineId, sIndex, DepTime);
#endif
                                        //Console.ReadLine();
                                        ////
                                    }

                                }
                                else
                                {
                                    if (LpData.PathSet[p].m_NodeID_NextLine[NodeID].ServiceType == TransitServiceType.Frequency)
                                    {
                                        NowDepTime = WaitingTime + NowArrTime;
                                    }
                                    else
                                    {
                                        int TrainIndex = -1;
                                        int delaPos = LpData.PathSet[p].m_NodeId_DeltaTrainBoard[NodeID];
                                        int LineIndex = LpData.SchLineSet.FindIndex(x => x.ID == BoardLineId);
                                        for (int q = 0; q < LpData.SchLineSet[LineIndex].NumOfTrains; q++)
                                        {
                                            if (v_Delta_Board_Veh[delaPos + q].Equals(1))
                                            {
                                                TrainIndex = q;
                                            }
                                        }
#region FirstVersion
                                        //int SchLinePos = LpData.m_SchLineId_TrainTerminalDepVarPos[BoardLineId];
                                        //double TrainDepTime = v_TrainTerminalDep[SchLinePos + TrainIndex];
                                        //double TimeDif = LpData.SchLineSet[LineIndex].m_Stop_TimeDif[LpData.SchLineSet[LineIndex].ID][NodeID];
                                        //NowDepTime = TrainDepTime + TimeDif;
#endregion
                                        int lIndex = LpData.SchLineSet.FindIndex(x => x.ID == BoardLineId);
                                        int sIndex = LpData.SchLineSet[lIndex].Stops.FindIndex(x => x.ID == NodeID);
                                        double DepTime = LpData.SchLineSet[lIndex].Stops[sIndex].getDepTime(BoardLineId, TrainIndex);

                                        NowDepTime = DepTime;
                                        //Console.WriteLine("check print PrintPasPath: l = {0}, s = {1}, dep = {2}", BoardLineId, sIndex, DepTime);
                                        //Console.ReadLine();

                                    }
                                }
                                     

                                SeatIndex = v_Delta_Seat[DeltaSeat];
                                if (LpData.PathSet[p].m_NodeID_NextLine[NodeID].ServiceType.Equals(TransitServiceType.Schedule))
                                {
                                    int CapVarPos = LpData.PathSet[p].m_NodeId_SchCapCostVarPos[NodeID];
                                    CapCost = v_PathSchCapCost[CapVarPos];
                                }
                                else if (LpData.PathSet[p].m_NodeID_NextLine[NodeID].ServiceType.Equals(TransitServiceType.Frequency))
                                {
                                    int CapVarPos = LpData.PathSet[p].m_NodeId_FreCapCostVarPos[NodeID];
                                    CapCost = v_PathFreCapCost[CapVarPos];
                                }

                                // find train index:
                                if (LpData.PathSet[p].m_NodeID_NextLine[NodeID].ServiceType.Equals(TransitServiceType.Schedule))
                                {
                                    int delaPos = LpData.PathSet[p].m_NodeId_DeltaTrainBoard[NodeID];
                                    int LineIndex = LpData.SchLineSet.FindIndex(x => x.ID == BoardLineId);
                                    for (int q = 0; q < LpData.SchLineSet[LineIndex].NumOfTrains; q++)
                                    {
                                        if (v_Delta_Board_Veh[delaPos + q].Equals(1))
                                        {
                                            Veh = q;
                                        }
                                    }
                                }
                                else
                                    Veh = -1;


                                int SegCout = 0;
                                int NowNode = LpData.PathSet[p].VisitNodes[i];
                                int NextNode = LpData.PathSet[p].VisitNodes[i + 1];
                                for (int s = 0; s < LpData.PathSet[p].m_NodeID_NextLine[NodeID].Stops.Count - 1; s++)
                                {
                                    if (LpData.PathSet[p].m_NodeID_NextLine[NodeID].Stops[s].ID == NowNode
                                        && LpData.PathSet[p].m_NodeID_NextLine[NodeID].Stops[s + 1].ID == NextNode)
                                    {
                                        break;
                                    }
                                    else
                                        SegCout++;
                                }
                                SegTimeVal = LpData.PathSet[p].m_NodeID_NextLine[NodeID].SegTravelTimes[SegCout];

                                // find board line id 
                                int indexline = LpData.SchLineSet.FindIndex(x => x.ID == BoardLineId);
                                if (indexline >= 0)
                                {
                                    BoardLineId = LpData.SchLineSet[indexline].ID;
                                }
                                indexline = LpData.FreLineSet.FindIndex(x => x.ID == BoardLineId);

                                if (indexline >= 0)
                                {
                                    BoardLineId = LpData.FreLineSet[indexline].ID;
                                }
                            }
                            /// <remarks>
                            /// The output cost does not include transfer cost, 
                            /// so the cost is larger than expected cost
                            /// if it is the destination node, then the arrival time is useful
                            /// </remarks>
                            file.WriteLine("{0},{1},{2},{3},{4},{5:#.00},{6},{7},{8},{9:#.00},{10},{11},{12},{13:#.0},{14:#.00},{15:#.00}",
                                Global.NumOfIter,//0
                                SolNumID,
                                LpData.PathSet[p].Trip.ID,
                                p,
                                LpData.PathSet[p].VisitNodes[i],
                                WaitingTime,
                                BoardLineId,
                                Veh,
                                SeatIndex,
                                CapCost,
                                v_PathPie[p],
                                v_PathProb[p],
                                v_PathProb[p] * LpData.PathSet[p].Trip.Demand,
                                SegTimeVal,
                                NowArrTime,
                                NowDepTime
                                );
    //                        Console.WriteLine("{0},{1},{2},{3},{4},{5:#.00},{6},{7},{8},{9:#.00},{10},{11},{12},{13:#.0},{14:#.00},{15:#.00}",
    //Global.NumOfIter,//0
    //SolNumID,
    //LpData.PathSet[p].Trip.ID,
    //p,
    //LpData.PathSet[p].VisitNodes[i],
    //WaitingTime,
    //BoardLineId,
    //Veh,
    //SeatIndex,
    //CapCost,
    //v_PathPie[p],
    //v_PathProb[p],
    //v_PathProb[p] * LpData.PathSet[p].Trip.Demand,
    //SegTimeVal,
    //NowArrTime,
    //NowDepTime
    //);

                            NowArrTime = NowDepTime + SegTimeVal;
                        }
                    }
                }
            }
        }
    }
}

