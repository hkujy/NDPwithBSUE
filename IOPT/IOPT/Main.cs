using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
///<remarks>
/// Author: Yu Jiang (姜宇）
/// Email:  yujiang@dtu.dk
/// Homepage: www.dryujiang.com
/// Finalized Date: 2021 May
///</remarks>


namespace IOPT
{
    class Program
    {
        static void Main(string[] args)
        {
            int MaxPathPerIter = 1;//
            double CurrentBestObj = double.MaxValue;
            bool Terminate = false;
            List<int> AddPathSet = new List<int>();
            Stopwatch IterTime = new Stopwatch();
            Global.NumOfIter = 0;
            SetFile.Init(); // ini files 
            SetPara(); // read and set parameters 
            // Step 2: set network input
            NetworkClass Network = new NetworkClass();
            string GlobalIterFile = MyFileNames.OutPutFolder + "GlobalIter.txt";
            using (StreamWriter file = new StreamWriter(GlobalIterFile, true)) file.WriteLine("Iter,NumCol,BestCplexObj,BestComputeObj,CpuTime,NumColNewAdd");
            /// Step 3: Create Solution Class 
            BBmain.LpInput NewLpData = new BBmain.LpInput();
            BBmain BBmain = new BBmain();
            BBmain.SolClass BestSol = new BBmain.SolClass();
            PARA.PrintEventLog = new StreamWriter(MyFileNames.OutPutFolder + "GenEvtLog.txt", true);
            Stopwatch stp = new Stopwatch();
            stp.Start();
            Network.InitFlow();    
            if (PARA.DesignPara.AssignMent.Equals(AssignMethod.SUE)) PARA.PathPara.BoundNonDomEventLower = 100;
            if (PARA.DesignPara.AssignMent.Equals(AssignMethod.BCM)) PARA.PathPara.BoundNonDomEventLower = PARA.DesignPara.Slack_ini;
            Network.GenPathSet();
            if (PARA.DesignPara.AssignMent.Equals(AssignMethod.BCM)) PARA.PathPara.BoundNonDomEventLower = PARA.DesignPara.Slack_update;
            PARA.DesignPara.CapType = CapCostType.StepWise;  // consider the difference between seat and stand
            switch (PARA.DesignPara.AssignMent)
            {
                #region SUE
                ///<remarks>
                /// In the formal test, the SUE case only used in one test. 2021-Apr-17
                ///</remarks>
                case AssignMethod.SUE: 
                    IterTime.Start();
                    BBmain.LpData.SetValue(Network);
                    BestSol = BBmain.MainSolve();
                    Network.Loading(BBmain.LpData, BestSol); 
                    IterTime.Stop();
                    using (StreamWriter bf = new StreamWriter(MyFileNames.OutPutFolder + "BB_Best_SolNum.txt", true))
                    {
                        bf.WriteLine("Iter,BestSolNum,TotalCost,PasCost,OpCost,BestCplexObj");
                        bf.WriteLine("{0},{1},{2},{3},{4},{5}", Global.NumOfIter, BestSol.SolNumID,
                            BestSol.TotalCostCompute, BestSol.TotalPasCostCompute, BestSol.TotalOpCost, BestSol.CplexObj);
                    }
                    using (StreamWriter file = new StreamWriter(GlobalIterFile, true))
                    {
                        Console.WriteLine("Iter = {0},NewPath = {1}, BestCplex = {2},BestTotalCompute = {3},CpuTime = {4}", Global.NumOfIter, 0, BestSol.CplexObj,
                            BestSol.TotalPasCostCompute + BestSol.TotalPasCostCompute,
                            IterTime.ElapsedMilliseconds);
                    }
                    Global.NumOfIter++;
                    break;
                #endregion
                #region BCM 
                case AssignMethod.BCM:
                    do
                    {
                        IterTime.Start();
                        List<int> RemoveID = new List<int>();
                        int NumOfPathAdd = 0;
                        BBmain.LpData.SetValue(Network);
                        List<double> MinPie = new List<double>(); List<double> MaxPie = new List<double>();
                        BBmain.LpData.MaxEventPie.Clear(); BBmain.LpData.MinEventPie.Clear();
                        #region settingForTheFirstIteration
                        if (Global.NumOfIter == 0)
                        {
                            for (int t = 0; t < Network.Trips.Count(); t++)
                            {
                                MinPie.Add(double.MaxValue); BBmain.LpData.MaxEventPie.Add(0.0); BBmain.LpData.MinEventPie.Add(double.MaxValue);
                            }
                            for (int t = 0; t < BBmain.LpData.TripPathSet.Count; t++)
                            {
                                for (int p = 0; p < BBmain.LpData.TripPathSet[t].Count; p++)
                                {
                                    MinPie[t] = Math.Min(MinPie[t], BBmain.LpData.PathSet[BBmain.LpData.TripPathSet[t][p]].EventPie);
                                    BBmain.LpData.MinEventPie[t] = Math.Min(MinPie[t], BBmain.LpData.PathSet[BBmain.LpData.TripPathSet[t][p]].EventPie);
                                    BBmain.LpData.MaxEventPie[t] = BBmain.LpData.MinEventPie[t]
                                        + PARA.DesignPara.GetBcmValue()
                                        + Global.MaxGapBetweenMinMaxCost;
                                }
                            }
                      
                            for (int p = 0; p < BBmain.LpData.PathSet.Count; p++)
                            {
                                ///<remarks>
                                /// In the following, different procedures are applied to different test examples
                                /// remove different paths in different case studies
                                /// 2021-Apr-17
                                ///</remarks>
                                if (Network.Lines.Count <= 4)   
                                {
                                    // remark: small network could consider more paths in the first iteration
                                    if (BBmain.LpData.PathSet[p].EventPie > MinPie[BBmain.LpData.PathSet[p].Trip.ID] + PARA.DesignPara.GetBcmValue())
                                    {
                                        RemoveID.Add(p);
                                    }
                                }
                                else
                                {
                                    // remark: for the CPH network, only consider the shortest path in the first iteration
                                    if (BBmain.LpData.PathSet[p].EventPie > MinPie[BBmain.LpData.PathSet[p].Trip.ID])
                                    {
                                        RemoveID.Add(p);
                                    }
                                }
                            }
                        }
                        #endregion
                        // remove path and set the new path ID index
                        if (RemoveID.Count > 0)
                        {
                            BBmain.LpData.PathSet.RemoveAll(x => RemoveID.Contains(x.ID));
                            for (int p = 0; p < BBmain.LpData.PathSet.Count; p++)
                            {
                                BBmain.LpData.PathSet[p].ID = p;
                            }
                            BBmain.LpData.SetValue(Network); 
                        }
                        BestSol = BBmain.MainSolve();
                        CurrentBestObj = Math.Min(CurrentBestObj, BestSol.CplexObj);
                        if (BestSol.SolSatus.Equals(BBmain.SOLSTA.EpsFeasible))
                        {
                            Network.Loading(BBmain.LpData, BestSol);
                            BestSol.UpdateLines_SchAndHeadway(ref Network.Lines, ref Network.Nodes, BBmain.LpData); 
                            Network.CleanEventArry();
                            Network.GenPathSet(); 
                            NewLpData.Clear(clearPathSet: true);
                            NewLpData.SetValue(Network); 
                            // next is to compare the generated set and current set of pat
                            if (NewLpData.LpPathSetIsEqual(BBmain.LpData, ref AddPathSet))
                            {
                                Terminate = true;
                            }
                            else
                            {
                                // add the minimum path cost
                                MinPie.Clear(); MaxPie.Clear(); BBmain.LpData.MaxEventPie.Clear(); BBmain.LpData.MinEventPie.Clear();
                                for (int t = 0; t < Network.Trips.Count(); t++)
                                {
                                    MaxPie.Add(0); MinPie.Add(double.MaxValue); BBmain.LpData.MaxEventPie.Add(0.0); BBmain.LpData.MinEventPie.Add(double.MaxValue);
                                }
                                // get minimum cost for the path associated with the current solution
                                for (int t = 0; t < BBmain.LpData.TripPathSet.Count; t++)
                                {
                                    for (int p = 0; p < BBmain.LpData.TripPathSet[t].Count; p++)
                                    {
                                        MinPie[t] = Math.Min(MinPie[t], BestSol.v_PathPie[BBmain.LpData.TripPathSet[t][p]]);
                                        MaxPie[t] = Math.Max(MaxPie[t], BestSol.v_PathPie[BBmain.LpData.TripPathSet[t][p]]);
                                        BBmain.LpData.MinEventPie[t] = Math.Min(MinPie[t], BBmain.LpData.PathSet[BBmain.LpData.TripPathSet[t][p]].EventPie);
                                        BBmain.LpData.MaxEventPie[t] = BBmain.LpData.MinEventPie[t]
                                             + PARA.DesignPara.GetBcmValue()
                                             + Global.MaxGapBetweenMinMaxCost;
                                    }
                                }

                                // find and add the path with the minimum cost
                                double min_add_path_cost = double.MaxValue; int min_add_path_id = -1;
                                double max_add_path_cost = double.MinValue; int max_add_path_id = -1;
                                List<double> GapWithCurrent = new List<double>();
                                for (int t = 0; t < NewLpData.TripPathSet.Count(); t++) GapWithCurrent.Add(0.0);
                                int[] MinCostOdPathToAdd = new int[NewLpData.TripPathSet.Count];
                                int[] NumPathForEachOD = new int[NewLpData.TripPathSet.Count];
                                for (int i = 0; i < MinCostOdPathToAdd.Count(); i++) MinCostOdPathToAdd[i] = -1;
                                for (int t = 0; t < NewLpData.TripPathSet.Count; t++)
                                {
                                    double MinCost = double.MaxValue;
                                    for (int p = 0; p < NewLpData.TripPathSet[t].Count; p++)
                                    {
                                        int pathid = NewLpData.TripPathSet[t][p];
                                        if (AddPathSet.Contains(pathid))
                                        {
                                            if (NewLpData.PathSet[pathid].EventPie < MinCost)
                                            {
                                                MinCostOdPathToAdd[t] = pathid;
                                                MinCost = NewLpData.PathSet[pathid].EventPie;
                                                if (NewLpData.PathSet[pathid].EventPie < min_add_path_cost)
                                                {
                                                    min_add_path_cost = NewLpData.PathSet[pathid].EventPie;
                                                    min_add_path_id = pathid;
                                                }
                                                if (NewLpData.PathSet[pathid].EventPie > max_add_path_cost)
                                                {
                                                    max_add_path_cost = NewLpData.PathSet[pathid].EventPie;
                                                    max_add_path_id = pathid;
                                                }
                                                GapWithCurrent[t] = MinCost - MinPie[t];
                                                NumPathForEachOD[t]++;
                                            }
                                        }
                                    }
                                }
                                
                                // the following check whether to terminate 
                                Terminate = true;
                                for (int i = 0; i < AddPathSet.Count; i++)
                                {
                                    // if the added path cost is less than the minimum value + bcm then the algorithm continuous  
                                    if (NewLpData.PathSet[AddPathSet[i]].EventPie
                                        <= MinPie[NewLpData.PathSet[AddPathSet[i]].Trip.ID]
                                           + PARA.DesignPara.GetBcmValue())
                                    {
                                        if (NumOfPathAdd >= MaxPathPerIter) continue;
                                        if (AddPathSet[i] != MinCostOdPathToAdd[NewLpData.PathSet[AddPathSet[i]].Trip.ID])
                                            continue;
                                        // try add one path that has the maximum cost gap
                                        if (NewLpData.PathSet[AddPathSet[i]].Trip.ID != GapWithCurrent.IndexOf(GapWithCurrent.Min())) continue;
                                        NumOfPathAdd++;
                                        BBmain.LpData.PathSet.Add(NewLpData.PathSet[AddPathSet[i]]);
                                        BBmain.LpData.PathSet[BBmain.LpData.PathSet.Count - 1].ID = BBmain.LpData.PathSet.Count - 1;
                                        Terminate = false;
                                    }
                                }
                                GapWithCurrent.Clear();
                            }
                        }
                        IterTime.Stop();
                        // output solution for each iteration
                        BestSol.PrintInMainIter(BBmain.LpData, IterTime, NumOfPathAdd, isOnScreen: false);
                        Global.NumOfIter++;
                        if (BestSol.SolSatus.Equals(BBmain.SOLSTA.InFeasible)) Terminate = true;
                    } while (!Terminate);
                    break;
                #endregion
            }

            stp.Stop();
            using (StreamWriter file = new StreamWriter(MyFileNames.OutPutFolder + "CpuTime.txt"))
            {
                file.WriteLine("Total Computation time: {0} milliseconds)", stp.ElapsedMilliseconds);
            }
            PARA.WriteFile();
            Console.WriteLine("Complete one test");

            return;
        }
        /// <summary>
        /// Ini and read parameters 
        /// </summary>
        /// <returns></returns>
        internal static void SetPara()
        {
            PARA.BBPara = new BBParaClass();
            PARA.PathPara = new PathParaClass();
            PARA.PathPara.ReadFromFile();
            PARA.DesignPara = new DesignParaClass();
            PARA.DesignPara.ReadFromFile();
            PARA.ReadAdjustablePara();
            PARA.SetIntervalSet();
        }
    }
}

