using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;

/// <remarks>
/// The following remarks are made in the first version 
///1. Make sure the line/node number index starts from 0
///2. If there not solution from Cplex consider to increase the maximum pie value and check the bounds
///3. Transfer penalty is considered in the function GetAddCpCost
///4. Current Version Ignore Walking links and zone nodes
///5. Current fre-based waiting time is assumed to be half headway
///6. Maximum operation time for frequency based lines is fixed
///9. set min arr dep transferTime,0.0 otherwise the it is not consistent between LP and event path
///10. Each segment only map one bus line
///TODO: Future add transfer line or walking links lines, may not be necessary
///</remarks>

///<remarks>
/// code revised in 2021 Feb
///</remarks>


namespace IOPT
{
    class Program
    {
        /// <summary>
        /// main program for the exact solution method
        /// the following main program can be generalize to the a general test case
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>

        static void Main(string[] args)
        {
            int MaxPathPerIter = 1;//
            double CurrentBestObj = double.MaxValue;
            bool Terminate = false;
            List<int> AddPathSet = new List<int>();
            Stopwatch IterTime = new Stopwatch();
            Global.NumOfIter = 0;
            SetFile.Init();
            // read and set parameters 
            SetPara();
            // Step 2: set network input
            NetworkClass Network = new NetworkClass();

#if DEBUG
            #region code2ToCheckLineTT
            // Code to debug the transit travel time between stops 
            // check the travel time input
            foreach (TransitLineClass l in Network.Lines)
            {
                for (int s1 = 0; s1 < l.Stops.Count - 1; s1++)
                {
                    for (int s2 = s1 + 1; s2 < l.Stops.Count; s2++)
                    {
                        int tail = l.Stops[s1].ID;
                        int head = l.Stops[s2].ID;
                        Console.WriteLine("l={0},tail={1},head={2},time={3}", l.ID, tail, head, l.getTravelTimeBetweenStop(tail, head));
                    }
                }
            }
            #endregion
#endif
            string GlobalIterFile = MyFileNames.OutPutFolder + "GlobalIter.txt";
            using (StreamWriter file = new StreamWriter(GlobalIterFile, true))
            {
                file.WriteLine("Iter,NumCol,BestCplexObj,BestComputeObj,CpuTime,NumColNewAdd");
            }

            /// Step 3: Create Solution Class 
            BBmain.LpInput NewLpData = new BBmain.LpInput();
            BBmain BBmain = new BBmain();
            BBmain.SolClass BestSol = new BBmain.SolClass();
            PARA.PrintEventLog = new StreamWriter(MyFileNames.OutPutFolder + "GenEvtLog.txt", true);
            Stopwatch stp = new Stopwatch();
            stp.Start();
            // Network Flow Initialization
            Network.InitFlow();
            #region setting for SUE, used in the three link example
            ///<remarks>
            ///Adaptive slack time is not considered, but can be incorporated in the future
            ///double AdaptSlack = PARA.PathPara.BoundNonDomEventLower;
            ///Console.WriteLine("Adaptive SlackTime is set to be = {0}", AdaptSlack);
            ///</remarks>
            if (PARA.DesignPara.AssignMent.Equals(AssignMethod.SUE)) PARA.PathPara.BoundNonDomEventLower = 100;
            #endregion
            ///<remarks>
            /// I think the slack time is not used this version
            /// meaning while, generating initial paths uses different slack time as the coloumn generation
            ///</remarks>
            if (PARA.DesignPara.AssignMent.Equals(AssignMethod.BCM)) PARA.PathPara.BoundNonDomEventLower = PARA.DesignPara.Slack_ini;
            Network.GenPathSet();
            // relax the value for the column generations
            if (PARA.DesignPara.AssignMent.Equals(AssignMethod.BCM))
            {
                PARA.PathPara.BoundNonDomEventLower = PARA.DesignPara.Slack_update;
            }
            PARA.DesignPara.CapType = CapCostType.StepWise;  // consider the difference between seat and stand
#if DEBUG
            Console.WriteLine("Slack TIme = {0}", PARA.PathPara.BoundNonDomEventLower);
#endif
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
                #region RSUE
                case AssignMethod.RSUE: // just follow the same procedure as SUE using different value
                    do
                    {
                        IterTime.Start();
                        MyLog.Instance.Info("RSUE:Start Iter = " + Global.NumOfIter.ToString());
                        BBmain.LpData.SetValue(Network);
                        MyLog.Instance.Info("RSUE:Complete Set LpData Value");
                        BestSol = BBmain.MainSolve();
                        MyLog.Instance.Info("RSUE:Complete Main Solve");
                        Network.Loading(BBmain.LpData, BestSol);
                        MyLog.Instance.Info("RSUE:Complete Loading");
                        BestSol.UpdateLines_SchAndHeadway(ref Network.Lines, ref Network.Nodes, BBmain.LpData);
                        MyLog.Instance.Info("RSUE:Complete update line schedule");
                        Network.GenPathSet();
                        NewLpData.Clear(clearPathSet: true);
                        NewLpData.SetValue(Network);
                        MyLog.Instance.Info("RSUE:Complete Generate New PathSet");


                        // compare the generated set and current set of pat
                        if (NewLpData.LpPathSetIsEqual(BBmain.LpData, ref AddPathSet))
                        {
                            Terminate = true;
                        }
                        else
                        {
                            Terminate = false;
                            for (int i = 0; i < AddPathSet.Count; i++)
                            {
                                BBmain.LpData.PathSet.Add(NewLpData.PathSet[AddPathSet[i]]);
                                BBmain.LpData.PathSet[BBmain.LpData.PathSet.Count - 1].ID = BBmain.LpData.PathSet.Count - 1;
                            }

                            ///<remarks> if use the following well order path set
                            ///then the solution may not be the same due to the existence of multiple solutions
                            ///<code>
                            ///List<BBmain.LpInput.LpPath> temp = new List<BBmain.LpInput.LpPath>();
                            ///for (int i = 0; i < BBmain.LpData.PathSet.Count; i++) temp.Add(BBmain.LpData.PathSet[i]);
                            ///BBmain.LpData.PathSet.Clear();
                            ///for (int i = 0; i < Trips.Count; i++)
                            ///{
                            ///    for (int j = 0; j < temp.Count; j++)
                            ///    {
                            ///        if (temp[j].Trip.ID == i) BBmain.LpData.PathSet.Add(temp[j]);
                            ///    }
                            ///}
                            ///temp.Clear();
                            ///</code>
                            ///</remarks>
                        }
                        IterTime.Stop();

                        using (StreamWriter file = new StreamWriter(GlobalIterFile, true))
                        {
                            file.WriteLine("{0},{1},{2},{3},{4}", Global.NumOfIter, NewLpData.PathSet.Count, BestSol.CplexObj,
                             BestSol.TotalOpCost + BestSol.TotalPasCostCompute, IterTime.ElapsedMilliseconds);
                        }

                        Console.WriteLine("Iter = {0},NewPath = {1}, BestCplex = {2},BestTotal = {3},CpuTime = {4}", Global.NumOfIter, NewLpData.PathSet.Count, BestSol.CplexObj,
                            BestSol.TotalPasCostCompute + BestSol.TotalPasCostCompute, IterTime.ElapsedMilliseconds);
                        Global.NumOfIter++;

                    } while (!Terminate);
                    break;
                #endregion
                case AssignMethod.BCM:
                    do
                    {
                        IterTime.Start();
                        ///<remarks>
                        ///The event dominate algorithm still possible to generate path
                        ///whose travel cost is larger than the minimum value + the boundary
                        ///Therefore, it is necessary to remove these path in case
                        ///</remarks>
                        List<int> RemoveID = new List<int>();
                        int NumOfPathAdd = 0;
                        MyLog.Instance.Info("BCM:Start BCM Iter = " + Global.NumOfIter.ToString());
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
                                        + PARA.DesignPara.GetBcmValue(BBmain.LpData.PathSet[p].Trip.BcmRatioValue)
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
                                    if (BBmain.LpData.PathSet[p].EventPie > MinPie[BBmain.LpData.PathSet[p].Trip.ID] + PARA.DesignPara.GetBcmValue(
                                                                         BBmain.LpData.PathSet[p].Trip.BcmRatioValue))
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
                            BBmain.LpData.SetValue(Network); //revise 2021: only reset the lp value if paths are removed
                        }
                        BBmain.LpData.PrintPasPathSet(MyFileNames.OutPutFolder + "BB_ActivePathSet.txt");
                        BestSol = BBmain.MainSolve(); MyLog.Instance.Info("BCM:Complete main solve");
                        CurrentBestObj = Math.Min(CurrentBestObj, BestSol.CplexObj);
                        if (BestSol.SolSatus.Equals(BBmain.SOLSTA.EpsFeasible))
                        {
#if DEBUG
                            Console.WriteLine("Print PathCost After Main Solve");
                            for (int p=0;p<BestSol.PathCostExprVal.Count;p++)
                            {
                                Console.WriteLine("P = {0}, val = {1}", p, BestSol.PathCostExprVal[p]);
                            }
                            //Console.ReadLine();
#endif
                            Network.Loading(BBmain.LpData, BestSol); MyLog.Instance.Info("BCM:Complete Loading");
                            BestSol.UpdateLines_SchAndHeadway(ref Network.Lines, ref Network.Nodes, BBmain.LpData); MyLog.Instance.Info("BCM:Complete UpdateingSchandHeadway");
                            Network.CleanEventArry();
                            Network.GenPathSet(); MyLog.Instance.Info("BCM:Complete Generate New PathSet");
                            NewLpData.Clear(clearPathSet: true);
                            NewLpData.SetValue(Network); MyLog.Instance.Info("BCM:Complete set network value for new path set");

#if DEBUG
                            ///<remarks>
                            /// there could be some gap between the Gen path find and the path cost from the solutions
                            /// this may because the schedule value in the cplex has decimal value, making it difficult
                            /// I think the error is minor and will not affect the following computation
                            /// this is because the algorithm does not rely the consistence between different cost 
                            /// but depends on whether there exist new paths to be generated
                            ///</remarks>
                            Console.WriteLine("Print path pie after loading and set value");
                            for(int p = 0;p<NewLpData.PathSet.Count;p++)
                            {
                                Console.WriteLine("p = {0}, val = {1}", p, NewLpData.PathSet[p].EventPie);
                            }
                            //Console.ReadLine();
#endif
                            // next is to compare the generated set and current set of pat
                            if (NewLpData.LpPathSetIsEqual(BBmain.LpData, ref AddPathSet))
                            {
                                // if the two path sets are the same then stop the algorithm
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
                                        ///<remark>
                                        /// remark: add MaxGapBetweenMinMaxCost
                                        /// this is a buffer time 
                                        /// because the current bound does not consider assignment, 
                                        /// so after the assignment and consider congestion cost, the actual minimum cost could be larger 
                                        ///</remark> 
                                        BBmain.LpData.MaxEventPie[t] = BBmain.LpData.MinEventPie[t]
                                             + PARA.DesignPara.GetBcmValue(BBmain.LpData.PathSet[p].Trip.BcmRatioValue)
                                             + Global.MaxGapBetweenMinMaxCost;
                                    }
                                }

                                // find and add the path with the minimum cost
                                double min_add_path_cost = double.MaxValue;
                                int min_add_path_id = -1;
                                double max_add_path_cost = double.MinValue;
                                int max_add_path_id = -1;
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

                                Terminate = true;

                                for (int i = 0; i < AddPathSet.Count; i++)
                                {
                                    // if the added path cost is less than the minimum value + bcm then the algorithm continuous  
                                    if (NewLpData.PathSet[AddPathSet[i]].EventPie
                                        <= MinPie[NewLpData.PathSet[AddPathSet[i]].Trip.ID]
                                           + PARA.DesignPara.GetBcmValue(NewLpData.PathSet[AddPathSet[i]].Trip.BcmRatioValue))
                                    {
                                        if (NumOfPathAdd >= MaxPathPerIter) continue;
                                        if (AddPathSet[i] != MinCostOdPathToAdd[NewLpData.PathSet[AddPathSet[i]].Trip.ID])
                                            continue;
                                        // try add one path that has the maximum cost gap
                                        if (NewLpData.PathSet[AddPathSet[i]].Trip.ID != GapWithCurrent.IndexOf(GapWithCurrent.Min())) continue;

                                        NumOfPathAdd++;
#if DEBUG
                                        Console.WriteLine("OD = {0}, min cost={1}, add path cost = {2}, bcm= {3}, maxcost = {4}", NewLpData.PathSet[AddPathSet[i]].Trip.ID,
                                         MinPie[NewLpData.PathSet[AddPathSet[i]].Trip.ID], NewLpData.PathSet[AddPathSet[i]].EventPie,
                                         PARA.DesignPara.GetBcmValue(NewLpData.PathSet[AddPathSet[i]].Trip.BcmRatioValue),
                                         MaxPie[NewLpData.PathSet[AddPathSet[i]].Trip.ID]);
#endif

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
#if DEBUG
                        BestSol.PrintInMainIter(BBmain.LpData, IterTime, NumOfPathAdd, isOnScreen: true);
#endif
                        Global.NumOfIter++;
                        if (BestSol.SolSatus.Equals(BBmain.SOLSTA.InFeasible)) Terminate = true;
                    } while (!Terminate);
                    break;
            }

            stp.Stop();
            using (StreamWriter file = new StreamWriter(MyFileNames.OutPutFolder + "CpuTime.txt"))
            {
                file.WriteLine("Total Computation time: {0} milliseconds)", stp.ElapsedMilliseconds);
            }
            PARA.WriteFile();
            //#region GenerateAllSuePath
            //// the following part generate SUE all path set
            //if (PARA.DesignPara.AssignMent.Equals(AssignMethod.SUE))
            //{
            //    Console.WriteLine("I think I do not need to generate the SUE set in the revised version");
            //    Console.ReadLine();
            //    PARA.PathPara.BoundNonDomEventLower = 1000;
            //    Global.NumOfIter++;
            //    Network.GenPathSet();
            //}
            //#endregion

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
            MyLog.Instance.Info("Set para function is completed");
        }
    }
}



///<remarks>
////The following code practice relative path separator
////Console.WriteLine("Path.AltDirectorySeparatorChar={0}",
////Path.AltDirectorySeparatorChar);
////Console.WriteLine("Path.DirectorySeparatorChar={0}",
////    Path.DirectorySeparatorChar);
////Console.WriteLine("Path.PathSeparator={0}",
////    Path.PathSeparator);
////Console.WriteLine("Path.VolumeSeparatorChar={0}",
////    Path.VolumeSeparatorChar);
////Console.Write("Path.GetInvalidPathChars()=");
////foreach (char c in Path.GetInvalidPathChars())
////    Console.Write(c);
////Console.WriteLine();
////Console.WriteLine("Check path separator");
////Console.ReadLine();
/// ///</remarks>
#region RemoveProcedure
///<remarks>
///I think it is unnecessary to use the remove procedures 
///because the Solve will not generate solution that has a pie cost that is larger than the bound value
///</remarks>

//if (BestSol.Remove_PathSet.Count == 0)
//{
//    if (NewLpData.LpPathSetIsEqual_BCM(BBmain.LpData, ref AddPathSet))
//    {
//        Terminate = true;
//    }
//    else
//    {
//        Terminate = false;
//        for (int i = 0; i < AddPathSet.Count; i++)
//        {
//            BBmain.LpData.PathSet.Add(NewLpData.PathSet[AddPathSet[i]]);
//            BBmain.LpData.PathSet[BBmain.LpData.PathSet.Count - 1].ID = BBmain.LpData.PathSet.Count - 1;
//        }

//    }
//}
//if (BestSol.Remove_PathSet.Count > 0)
//{
//    //BBmain.LpData.PathSet.RemoveAt(BestSol.Remove_PathSet[0]);
//    if (!NewLpData.LpPathSetIsEqual_BCM(BBmain.LpData, ref AddPathSet))
//    {
//        for (int i = 0; i < AddPathSet.Count; i++)
//        {
//            BBmain.LpData.PathSet.Add(NewLpData.PathSet[AddPathSet[i]]);
//            BBmain.LpData.PathSet[BBmain.LpData.PathSet.Count - 1].ID = BBmain.LpData.PathSet.Count - 1;
//        }
//    }
//    MyLog.Instance.Info("BCM: Remove {" + BestSol.Remove_PathSet.Count.ToString() + "} number routes");

//    BBmain.LpData.PathSet.RemoveAll(x => BestSol.Remove_PathSet.Contains(x.ID));
//    for (int p = 0; p < BBmain.LpData.PathSet.Count; p++)
//        BBmain.LpData.PathSet[p].ID = p;
//}
//if (BestSol.CplexObj >= CurrentBestObj)
//{
//    using (StreamWriter bf = new StreamWriter(MyFileNames.OutPutFolder+ "BB_Best_SolNum.txt", true))
//    {
//        bf.WriteLine("{0},{1},{2},{3},{4},{5}", Global.NumOfIter, BestSol.SolNumID,
//            BestSol.TotalCostCompute, BestSol.TotalPasCostCompute, BestSol.TotalOpCost, BestSol.CplexObj);
//    }
//    ///<remarks>
//    ///It can be shown that the increment in the objective function can not be used as the termination condition
//    ///</remarks>
//    //Terminate = true;
//}
//else
//{
//    CurrentBestObj = BestSol.CplexObj;
//}
#endregion

