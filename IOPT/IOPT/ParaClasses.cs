/// checked 14-June-2018

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
/// <summary>
///  This file contains all the parameters classes
/// </summary>
namespace IOPT
{
    #region enum  variables
    public enum TransitServiceType { Frequency, Schedule, IsNull }
    public enum NodeType { Zone, Stop, IsNull }
    public enum SegType { Travel, Walk, IsNull }
    public enum EventType { Seg, Node, IsNull }
    public enum HeapPosSet { Head, Middle, End, IsNull }
    public enum StopType { LanuchTerminal, Intermediate, EndTerminal, IsNull }
    public enum TransitVehicleType { Bus, Metro, Train, S_Train, IsNull }
    public enum AssignMethod { SUE, BCM, RSUE, IsNull }
    public enum CapCostType
    {
        StepWise,  ///seat, separate seat and stand cost function
        StandOnly,// only stand 
        IsNull
    }
    #endregion

    public class MyFileNames
    {
        // the get file path is used for the linux system 
        public static string GetRelativePath(int level, string subfolder)
        {
            string str = "";
            for (int i = 0; i < level; i++) str = str + ".." + Path.DirectorySeparatorChar;
            return str + subfolder + Path.DirectorySeparatorChar;
        }
        public static string InputFolder =  AppDomain.CurrentDomain.BaseDirectory + GetRelativePath(4, "InPut"); //             "..\\..\\..\\..\\Input\\";
        public static string OutPutFolder = AppDomain.CurrentDomain.BaseDirectory + GetRelativePath(4, "OutPut");
        public static string LogFolder = AppDomain.CurrentDomain.BaseDirectory + GetRelativePath(4, "Logs");
        public static string AdjustPara = AppDomain.CurrentDomain.BaseDirectory + GetRelativePath(4, "PyScript") + "AdjustPara.txt";
    }
    public class Global
    {
        public static double MaxGapBetweenMinMaxCost = 10; // the buffer(gap) time between the min and max cost  
        public static bool UseWarmStartUp = false;  // record whether the BB use warm up solution from the cplex
        public static int EventNumCount = 0;
        public static int BBSolNum = -1;
        public static int NumOfIter = 0;
        public static int MaxBBLevel = 50;
        public static double DemandScale = 1;
        public static string TestCase = "Null";
        public static string TestCaseIndex = "Null";
        /// <summary>
        /// Convert string variables to double. 
        /// some problem occurs in the default, so I have to write it myself
        /// </summary>
        /// <param name="svalue"></param>
        /// <returns></returns>
        public static double MyString2Double(string svalue)
        {
            // convert read string data to double value 
            if (svalue.Equals("-1")) return -1;
            if (svalue.Equals("0")) return 0;

            double val = 0;
            char[] l = svalue.ToCharArray();
            int NumOfInt = 0;
            List<char> intc = new List<char>();
            List<char> dec = new List<char>();
            for (int i = 0; i < l.Length; i++)
            {
                if (l[i].ToString().Equals(".")) break;
                else
                {
                    intc.Add(l[i]);
                    NumOfInt++;
                }

            }
            for (int i = NumOfInt + 1; i < l.Length; i++)
            {
                dec.Add(l[i]);
            }
            for (int i = 0; i < intc.Count; i++)
            {
                val += Convert.ToInt32(intc[i].ToString()) * Math.Pow(10, intc.Count - i - 1);
            }
            for (int i = 0; i < dec.Count; i++)
            {
                val += Convert.ToInt32(dec[i].ToString()) / Math.Pow(10, i + 1);
            }
            return val;
        }

    }
    public class PathParaClass
    {
        // Parameters to be set in the para file
        protected internal double WalkW { get; set; }

        protected internal double WaitW { get; set; }
        protected internal double ConW { get; set; }
        protected internal double ZoneWaitW { get; set; }
        protected internal double TransferPenalty { get; set; }
        protected internal double InVTrainW { get; set; }
        protected internal double InVSTrainW { get; set; }
        protected internal double InVBusW { get; set; }
        protected internal double InVMetroW { get; set; }
        protected internal double SeatBeta { get; set; }
        protected internal double StandBeta { get; set; }
        protected internal double StandConstant { get; set; } // a constant for the jump between seat and no seat
        //protected internal double BoardAlpha { get; set; }
        //protected internal double Slack { get; set; }
        protected internal double MinTransferTime { get; set; }
        /// <remark>
        /// remark on upper and lower bound in the non-dominated setting 
        /// in an early version it is assumed that the bound set has both upper and lower bound
        /// </remark>
        protected internal double BoundNonDomEventLower { get; set; }  // bounded lower = to the slack variable
        protected internal double BoundNonDomEventUpper { get; set; }

        public PathParaClass()
        {
            WalkW = 1; WaitW = 1; ConW = 1; ZoneWaitW = 1; TransferPenalty = 1; InVTrainW = 1; InVSTrainW = 1;
            InVBusW = 1; InVMetroW = 1;
            SeatBeta = 1; 
            //BoardAlpha = 0.05;
            BoundNonDomEventLower = 0;
            BoundNonDomEventUpper = 0;
            StandConstant = 0;
        }

        /// <summary>
        /// read path parameter file
        /// The is not futher adjusted from the python code.
        /// </summary>
        /// <returns></returns>
        public void ReadFromFile()
        {
            Console.WriteLine("input folder = {0}", MyFileNames.InputFolder);
            string ParaFileName = MyFileNames.InputFolder + "PathPara.csv";
            Trace.Assert(File.Exists(ParaFileName), "PathPara does not exist");

            char[] delimiters = new char[] { ',' };
            using (StreamReader reader = new StreamReader(ParaFileName))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    string[] parts = line.Split(delimiters);
                    if (parts[0].Equals("WalkW")) WalkW = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("WaitW")) WaitW = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("ConW")) ConW = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("ZoneWaitW")) ZoneWaitW = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("TransferP")) TransferPenalty = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("InVTrainW")) InVTrainW = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("InVSTrainW")) InVSTrainW = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("InVBusW")) InVBusW = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("InVMetroW")) InVMetroW = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("BoundNonDomEventLower")) BoundNonDomEventLower = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("BoundNonDomEventUpper")) BoundNonDomEventUpper = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("TransferTime")) MinTransferTime = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("SeatBeta")) SeatBeta = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("StandBeta")) StandBeta = Global.MyString2Double(parts[1]);
                    //if (parts[0].Equals("BoardAlpha")) BoardAlpha = Global.MyString2Double(parts[1]);
                }
            }
            Trace.Assert(BoundNonDomEventLower <= BoundNonDomEventUpper, 
                "Input BoundNonDomEventUpper should be less than BoundNonDomEventLower ");
        }
        public void WriteFile()
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(MyFileNames.OutPutFolder+ "PathPara.csv"))
            {
                file.WriteLine("WalkW,{0}", WalkW); file.WriteLine("WaitW,{0}", WaitW); file.WriteLine("ConW,{0}", ConW); file.WriteLine("ZoneWaitW,{0}", ZoneWaitW); file.WriteLine("TransferP,{0}", TransferPenalty);
                file.WriteLine("InVTrainW,{0}", InVTrainW); file.WriteLine("InVSTrainW,{0}", InVSTrainW); file.WriteLine("InVBusW,{0}", InVBusW); file.WriteLine("InVMetroW,{0}", InVMetroW);
                file.WriteLine("SeatValue,{0}", SeatBeta); file.WriteLine("Slack,{0}", BoundNonDomEventLower);
                file.WriteLine("MinPasArrDepGap,{0}", MinTransferTime);
            }
        }
    }
    public class BBParaClass
    {
        public double EpsConstraint { get; set;} // eps for the branch and bound constraint setting
        public double EpsObj { get; set; } // eps for the branch and bound objective, measured by the percentage
        public BBParaClass()
        {
            // large network may set the eps constraints to be 0.1
            // this is adjusted and read from the input file
            // the large network case and small case using different eps values
            EpsConstraint = 0.1; EpsObj = 0.1;
        }
        public void WriteFile()
        {
            using (StreamWriter file = new StreamWriter(MyFileNames.OutPutFolder+ "BBPara.Csv"))
            {
                file.WriteLine("EpsConstraint,{0}", EpsConstraint); file.WriteLine("EpsObj,{0}", EpsObj);
            }
        }
    }

    public class DesignParaClass
    {
        protected internal AssignMethod AssignMent;
        protected internal CapCostType CapType;
        protected internal double Slack_ini { get; set; }
        protected internal double Slack_update { get; set; }
        protected internal bool UseBcmRatio { get; set; }  // based on the percentage of the bounded value
        protected internal bool isConsiderSeatSequence { get; set; }// whether cap cost depends on seating sequence
        protected internal int DurationOfEachInterval { get; set; }
        protected internal double BcmRatio { get; set; }
        protected internal double BcmValConst { get; set; }
        protected internal double BcmMaxCostDif { get; set; } // the maximum difference value of Cr-min(Cs) // used in the in determing the upper and lower bound of ln value
        protected internal double MaxHeadway { get; set; }
        protected internal double MinHeadway { get; set; }
        protected internal double MinDwellTime { get; set; }
        protected internal double FleetSize { get; set; }
        protected internal double BigM { get; set; }
        protected internal double Eps { get; set; }
        protected internal double Theta { get; set; }
        protected internal int NumOfBreakPoints { get; set; }
        protected internal bool ConstantPathCostBound { get; set; }
        //protected internal double MaxPieVal { get; set; }
        //protected internal double MinPieVal { get; set; }
        protected internal double MinProb { get; set; }
        protected internal double MaxProb { get; set; }
        protected internal double MIPRelGap { get; set; }
        protected internal double CplexTimLim { get; set; }
        protected internal double MaxTimeHorizon { get; set; }
        protected internal double MaxLineOperTime { get; set; }  // this may be slight longer than the maximum time horizon
        protected internal double Infi { get; set; }
        protected internal double FreOperationCost { get; set; }

        // -----------------------------------------------------------
        // add boarding and alighting time per passenger 
        protected internal double BoardAlightTimePerPas { get; set; }
        // -----------------------------------------------------------
        public double GetBcmValue(double BcmValRatio) // select between ratio and constant value
        {
            if (UseBcmRatio) return BcmValRatio;
            else return BcmValConst;
        }
        /// <summary>
        /// ini the design parameters
        /// Most of these parameters will be read via the "adjust_para function
        /// </summary>
        /// <returns></returns>
        protected internal DesignParaClass()
        {
            // most of these parameters will be read via the adjust para function
            Slack_ini = double.MinValue;
            Slack_update = double.MinValue;
            MaxHeadway = -1; MinHeadway = -1; MinDwellTime = 1; FleetSize = 50;
            BigM = 500; Theta = 0.15f; NumOfBreakPoints = 4;
            //MaxPieVal = 100;
            //MinPieVal = 20;
            MIPRelGap = 0.1; CplexTimLim = 3600;
            MinProb = 0.001; MaxProb = 1.001;
            MaxTimeHorizon = 90;
            MaxLineOperTime = 90;
            DurationOfEachInterval = 15;
            AssignMent = AssignMethod.IsNull;
            BcmRatio = 0;
            Infi = 1.0e+10;
            BcmMaxCostDif = -1;
            CapType = CapCostType.IsNull;
            isConsiderSeatSequence = false;
            FreOperationCost = 10000;
            ConstantPathCostBound = true; // if false then the upper and lower bound the cost varies 
            BoardAlightTimePerPas = 0.0;
        }
        /// <summary>
        /// read design parameters from file
        /// </summary>
        /// <returns></returns>
        protected internal void ReadFromFile()
        {
            string DataFile = MyFileNames.InputFolder + "DesignPara.csv";
            char[] delimiters = new char[] { ',' };
            using (StreamReader reader = new StreamReader(DataFile))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    string[] parts = line.Split(delimiters);
                    if (parts[0].Equals("MaxHeadway")) MaxHeadway = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("MinHeadway")) MinHeadway = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("MinDwellTime")) MinDwellTime = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("FleetSize")) FleetSize = Convert.ToInt32(parts[1]);
                    if (parts[0].Equals("BigM")) BigM = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("Theta")) Theta = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("NumOfBreakPoints")) NumOfBreakPoints = Convert.ToInt32(parts[1]);
                    //if (parts[0].Equals("MaxPieVal")) MaxPieVal = Global.MyString2Double(parts[1]);
                    //if (parts[0].Equals("MinPieVal")) MinPieVal = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("MIPRelGap")) MIPRelGap = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("CplexTimLim")) CplexTimLim = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("MaxTimeHorizon")) MaxTimeHorizon = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("MaxLineOperTime")) MaxLineOperTime = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("EachInterVal")) DurationOfEachInterval = Convert.ToInt32(parts[1]);
                    if (parts[0].Equals("BcmBound"))
                    {
                        BcmValConst = Global.MyString2Double(parts[1]);
                    }
                    if (parts[0].Equals("BcmMaxCostDif")) BcmMaxCostDif = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("FreOperationCost")) FreOperationCost = Global.MyString2Double(parts[1]);
                    
                }
            }
        }

        protected internal void WriteFile()
        {
            using (StreamWriter file = new StreamWriter(MyFileNames.OutPutFolder+ "DesignPara.csv"))
            {
                file.WriteLine("MaxHeadway,{0}", MaxHeadway);
                file.WriteLine("MinHeadway,{0}", MinHeadway);
                file.WriteLine("MinDwellTime,{0}", MinDwellTime);
                file.WriteLine("FleetSize,{0}", FleetSize);
                file.WriteLine("BigM,{0}", BigM);
                file.WriteLine("Theta,{0}", Theta);
                file.WriteLine("NumOfBreakPoints,{0}", NumOfBreakPoints);
                //file.WriteLine("MaxPieVal,{0}", MaxPieVal);
                //file.WriteLine("MinPieVal,{0}", MinPieVal);
                file.WriteLine("MIPRelGap,{0}", MIPRelGap);
                file.WriteLine("CplexTimLim,{0}", CplexTimLim);
                file.WriteLine("MaxTimeHorizon,{0}", MaxTimeHorizon);
                file.WriteLine("MaxFreLineOperTime,{0}", MaxLineOperTime);
                file.WriteLine("EachInterVal,{0}", DurationOfEachInterval);
                file.WriteLine("BcmBound,{0}", BcmValConst);
                file.WriteLine("BcmRatio,{0}", BcmRatio);
                file.WriteLine("BcmMaxCostDif,{0}", BcmMaxCostDif);
            }
        }
    }

    public class PARA
    {
        protected internal static bool PrintEventPathOnScreen = false;
        protected internal const double AverageWaitPara = 0.5;  // half of the headway
        protected internal const int NULLINT = -1;
        protected internal const double NULLDOUBLE = -9999d;
        protected internal const double MAXLABEL = 99999d;   // label in the shortest algorithm
        protected internal const int MaxNumOfEvents = 1000;
        protected internal const int MaxNumNonDomEvent = 10;
        protected internal const double ZERO = 1.0E-4d;
        protected internal const double GeZero = 1.0E-3d;
        protected internal const double LeZero = 0.9999 * GeZero;
        protected internal static DesignParaClass DesignPara;
        protected internal static PathParaClass PathPara;
        protected internal static BBParaClass BBPara;
        //protected internal static int NumOfIter = 0; // Num of iteration 
        protected internal static List<List<int>> IntervalSets; // interval sets for the capacity constraint frequency lines
        protected internal static StreamWriter PrintEventLog; // the log file for print event generation 

        /// <summary>
        /// write output parameters 
        /// </summary>
        protected internal static void WriteFile()
        {
            PathPara.WriteFile();
            BBPara.WriteFile();
            DesignPara.WriteFile();
        }

        protected internal static void ReadAdjustablePara()
        {
            char[] delimiters = new char[] { ',' };
            bool isWriteCase = true;
            using (StreamReader reader = new StreamReader(MyFileNames.AdjustPara))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    string[] parts = line.Split(delimiters);
                    if (parts[0].Equals("Theta")) { DesignPara.Theta = Global.MyString2Double(parts[1]); }
                    if (parts[0].Equals("NumOfBreakPoints")) DesignPara.NumOfBreakPoints = Convert.ToInt32(parts[1]);
                    //if (parts[0].Equals("MaxPieVal")) DesignPara.MaxPieVal = Global.MyString2Double(parts[1]);
                    //if (parts[0].Equals("MinPieVal")) DesignPara.MinPieVal = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("MinProb")) DesignPara.MinProb = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("MaxProb")) DesignPara.MaxProb = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("Assign"))
                    {
                        if (parts[1].Equals("SUE")) DesignPara.AssignMent = AssignMethod.SUE;
                        if (parts[1].Equals("RSUE")) DesignPara.AssignMent = AssignMethod.RSUE;
                        if (parts[1].Equals("BCM")) DesignPara.AssignMent = AssignMethod.BCM;
                    }
                    if (parts[0].Equals("isSeatSeq"))
                    {
                        if (parts[1].Equals("true")) DesignPara.isConsiderSeatSequence = true;
                        else DesignPara.isConsiderSeatSequence = false;
                    }
                    if (parts[0].Equals("Case"))
                    {
                        if (parts[1].Equals(Global.TestCase)) isWriteCase = false;
                    }

                    if (parts[0].Equals("SeatBeta")) PathPara.SeatBeta = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("StandBeta")) PathPara.StandBeta = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("StandConst")) PathPara.StandConstant = Global.MyString2Double(parts[1]);
                    //if (parts[0].Equals("BoardAlpha")) PathPara.BoardAlpha = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("FreOperationCost")) DesignPara.FreOperationCost = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("MIPRelGap")) DesignPara.MIPRelGap = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("BcmBound")) DesignPara.BcmValConst = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("BcmRatio")) DesignPara.BcmRatio = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("BoundNonDomEventLower")) PathPara.BoundNonDomEventLower = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("UseBcmRatio"))
                    {
                        if (parts[1].Equals("true")) DesignPara.UseBcmRatio = true;
                        else if (parts[1].Equals("false")) DesignPara.UseBcmRatio = false;
                        else
                        {
                            Console.WriteLine("ReadDesignPare: UseBcmRatio is not set correctly");
                            Console.ReadLine();
                        }
                    }

                    if (parts[0].Equals("Slack_Ini")) DesignPara.Slack_ini = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("Slack_Update")) DesignPara.Slack_update = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("CplexTimLim")) DesignPara.CplexTimLim = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("EachInterVal")) DesignPara.DurationOfEachInterval = Convert.ToInt32(parts[1]);
                    if (parts[0].Equals("BufferCost")) Global.MaxGapBetweenMinMaxCost = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("EpsConstraint")) BBPara.EpsConstraint = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("EpsObj")) BBPara.EpsObj = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("MaxGapBetweenMinMaxCost")) Global.MaxGapBetweenMinMaxCost = Global.MyString2Double(parts[1]);
                    if (parts[0].Equals("MaxBBLevel")) Global.MaxBBLevel = Convert.ToInt32(parts[1]);
                    if (parts[0].Equals("BoardAlightTimePerPas")) DesignPara.BoardAlightTimePerPas = Global.MyString2Double(parts[1]);
                }
            }
            using (StreamWriter f = new StreamWriter(MyFileNames.AdjustPara, true))
            {
                if (isWriteCase) f.WriteLine("Case,{0}", Global.TestCase);
            }


            //Console.WriteLine("board alight time per pas = {0}", DesignPara.BoardAlightTimePerPas);
            //Console.ReadLine();
        }

        /// <summary>
        /// Given time return which interval the time belongs to
        /// </summary>
        /// <param name="_time"></param>
        /// <returns></returns>
        protected internal static int FindInterval(double _time)
        {
            int interval = -1;
            Debug.Assert(_time < IntervalSets[IntervalSets.Count - 1][IntervalSets[IntervalSets.Count - 1].Count - 1], "time should be less than the maximum time interval value");
            //interval = IntervalSets.FindIndex(x => x[0] <= _time && x[x.Count - 1] + 1 > _time);
            interval = IntervalSets.FindIndex(x => x[0]-1 < _time &&  _time<= x[x.Count - 1]);
            return interval;
        }
        /// <summary>
        /// Checked 2021Feb
        /// in the current version, 
        /// the duration of each time interval is read from file
        /// create the set of intervals
        /// </summary>
        /// <returns></returns>
        protected internal static void SetIntervalSet()
        {
            IntervalSets = new List<List<int>>();
            int DurationOfEachInterval = DesignPara.DurationOfEachInterval;
            int n = 1;
            do
            {
                IntervalSets.Add(new List<int>());
                for (int i = DurationOfEachInterval * (n - 1); i < DurationOfEachInterval * n; i++)
                    IntervalSets[n - 1].Add(i);
                n++;
            } while (DurationOfEachInterval * n < DesignPara.MaxTimeHorizon);
            IntervalSets.Add(new List<int>());
            for (int i = DurationOfEachInterval * (n - 1); i < DesignPara.MaxTimeHorizon; i++)
                IntervalSets[n - 1].Add(i);
        }

    }
}


