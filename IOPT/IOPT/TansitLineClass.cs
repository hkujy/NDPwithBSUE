
/// updated 24-June-2018
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Data;
using System.Collections.Specialized;

namespace IOPT
{
    public class TransitLineClass
    {
        protected internal int ID { get; set; }
        protected internal int NumOfTrains { get; set; }
        // add number of doors 
        // revised 2021 Feb for computing the proper dwell time
        protected internal int NumOfDoors { get; set; }    
        //------------------------------------------------
        protected internal int NumOfSegs { get; set; }
        protected internal double FreCap { get; set; }
        protected internal double Headway { get; set; }
        protected internal string Name { get; set; }
        protected internal double StartTime { get; set; }   // start and end time for the operations
        protected internal double EndTime { get; set; }
        protected internal double TravelLength { get; set; } // travel distance use in computing frequency
        protected internal bool isInvolvedInDecsionVar { get; set; } // indicate whether a line is involved in the decision and to be solved 
        protected internal TransitServiceType ServiceType;
        protected internal TransitVehicleType VehicleType;
        protected internal List<double> TrainCap { get; set; }
        protected internal List<double> IniDepTime { get; set; }
        protected internal List<double> DepTimes;
        protected internal List<double> SegTravelTimes;
        protected internal List<SegClass> MapSegs;
        protected internal List<NodeClass> Stops;
        //public Dictionary<int, Dictionary<int, double>> m_Stop_TimeDif;// with respect to the departure time,. difference between departure times
        private void ini()
        {
            ID = PARA.NULLINT;
            NumOfTrains = PARA.NULLINT;
            isInvolvedInDecsionVar = false;
            TrainCap = new List<double>();
            NumOfSegs = PARA.NULLINT;
            Headway = PARA.NULLDOUBLE;
            Name = "NullName";
            ServiceType = TransitServiceType.IsNull;
            VehicleType = TransitVehicleType.IsNull;
            StartTime = PARA.NULLDOUBLE;
            EndTime = PARA.NULLDOUBLE;
            TravelLength = PARA.NULLDOUBLE;
            Stops = new List<NodeClass>();
            DepTimes = new List<double>();
            SegTravelTimes = new List<double>();
            MapSegs = new List<SegClass>();
            //m_Stop_TimeDif = new Dictionary<int, Dictionary<int, double>>();
            IniDepTime = new List<double>();
        }
        public TransitLineClass()
        {
            ini();
        }
        protected internal TransitLineClass(int SetID, string SetName, double SetStartTime,
            double SetEndTime, double SetHeadWay, TransitServiceType SetSerType, TransitVehicleType SetVehType)
        {
            ini();
            ID = SetID;
            Name = SetName;
            StartTime = SetStartTime;
            EndTime = SetEndTime;
            Headway = SetHeadWay;
            ServiceType = SetSerType;
            VehicleType = SetVehType;
        }
        /// <summary>
        /// add time different with respect to the first stop
        /// </summary>
        /// <returns></returns>
        //public void getStopTimeDifMap()
        //{
        //    Dictionary<int, double> temp = new Dictionary<int, double>();
        //    double CumTime = 0;
        //    // add departure node to be zero
        //    temp.Add(Stops[0].ID, CumTime);
        //    for (int s = 1; s < Stops.Count(); s++)
        //    {
        //        CumTime += SegTravelTimes[s - 1] + PARA.DesignPara.MinDwellTime;
        //        temp.Add(Stops[s].ID, CumTime);
        //    }
        //    m_Stop_TimeDif.Add(ID, temp);
        //}

        /// <summary>
        /// given the stop id of two nodes, 
        /// return the travel time between the two nodes
        /// I think it can also address the case when the two stops are not continuous stops
        /// </summary>
        /// <param name="sTailID"></param>
        /// <param name="sHeadID"></param>
        /// <returns></returns>
        public double getTravelTimeBetweenStop(int sTailID, int sHeadID)
        {
            if (sTailID == sHeadID) return 0;
            double val = 0;
                
            for (int st=0;st<Stops.Count-1;st++)
            {
                if (Stops[st].ID != sTailID) continue;
                for (int sh = st + 1; sh < Stops.Count; sh++)
                {
                    int tail = Stops[sh - 1].ID;
                    int head = Stops[sh].ID;
                    int sIndex = MapSegs.FindIndex(x => x.Tail.ID == tail && x.Head.ID == head);
                    val += MapSegs[sIndex].TravelTime;
                    if (head == sHeadID)
                    {
                        return val;
                    }
                }
            }

            Console.WriteLine("Warning: GetTravelTimeBetweenStop Function return 0");
            return val;

        }

        /// <summary>
        /// find the next arrival bus at a stop node, the next bus arrival time equal the departure time at the node
        /// </summary>
        /// <param name="DepTime"></param>
        /// <param name="Node"></param>
        /// <param name="LineID"></param>
        /// <param name="Lines"></param>
        /// <param name="TrainIndex"></param>
        /// <param name="isBoard">
        /// if on board is false, means only compute the time associated with the alight node and it is not necessary to compute the next arrival time
        /// </param>
        /// <param name="ConsiderMinGap"></param>
        /// <returns></returns>
        ///<remarks>
        ///current version does not include dwell time as a decision
        ///</remarks>
        protected internal static double NextArrival(double DepTime, NodeClass Node, int LineID, List<TransitLineClass> Lines, out int TrainIndex,
                                        bool isToBoardaNewLine, bool ConsiderMinTransferGap)
        {
            double NextDepTime = -999d;
            bool IsFindNextTime = false;
            double ActualDepTime = DepTime;
            TrainIndex = -1;
            switch (Lines[LineID].ServiceType)
            {
                case TransitServiceType.Frequency:
                    if (isToBoardaNewLine) NextDepTime = ActualDepTime + PARA.AverageWaitPara * Lines[LineID].Headway;
                    else if(!isToBoardaNewLine) NextDepTime = ActualDepTime;
                    if (NextDepTime>=0) IsFindNextTime = true;
                    break;

                case TransitServiceType.Schedule:
                    if (!isToBoardaNewLine)
                    {
                        // alight at one node
                        NextDepTime = ActualDepTime;
                        if (NextDepTime >= 0) IsFindNextTime = true;
                        //break;
                    }
                    if (ConsiderMinTransferGap && isToBoardaNewLine) ActualDepTime = DepTime + PARA.PathPara.MinTransferTime;

                    ///<remarks>
                    ///the minimum value between the arrival and departure
                    ///only applicable between the transfer from frequency to schedule
                    ///because if the transfer is from schedule to frequency,
                    ///then the expected waiting time should be greater than the gap value, if set the minimum headway correctly
                    ///</remarks>

                    for (int i = 0; i < Node.LineTimes.Count; i++)
                    {
                        if (Node.LineTimes[i].LineID == LineID)
                        {
                            if (ActualDepTime <= Node.LineTimes[i].ArrTimes[0])
                            {
                                // equal to the first train is a special case
                                NextDepTime = Node.LineTimes[i].ArrTimes[0];
                                if (NextDepTime >= 0) IsFindNextTime = true;
                                TrainIndex = 0;
                                return NextDepTime;
                            }
                            else
                            {
                                for (int j = 0; j < Node.LineTimes[i].ArrTimes.Count - 1; j++)
                                {
                                    if (ActualDepTime > Node.LineTimes[i].ArrTimes[j]
                                        && ActualDepTime <= Node.LineTimes[i].ArrTimes[j + 1])
                                    {
                                        NextDepTime = Node.LineTimes[i].ArrTimes[j + 1];
                                        if (NextDepTime >= 0) IsFindNextTime = true;
                                        TrainIndex = j + 1;
                                        return NextDepTime;
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
            if (!IsFindNextTime)
            {
                NextDepTime = PARA.DesignPara.MaxTimeHorizon + 100d;
            }

            Debug.Assert(NextDepTime >= 0, "Warning_TransitLineClass: Next Time is less than 0");

            return NextDepTime;
        }

        protected internal void IniSchedule(ref List<NodeClass> Nodes)
        {
            Dictionary<int, int> map_nodeId2DepartArrayIndex = new Dictionary<int, int>();
            List<double> DwellTime = new List<double>();
            int mapIndex = 0;

            //revised 2021Feb : add dwell time to the frequency-based lines
            if (ServiceType == TransitServiceType.Frequency)
            {
                for (int s = 0; s < Stops.Count; s++)
                {
                    map_nodeId2DepartArrayIndex.Add(Stops[s].ID, mapIndex);
                    for (int q = 0; q < PARA.IntervalSets.Count; q++)
                    {
                        if (s == 0)
                        {
                            DwellTime.Add(0.0);
                        }
                        else
                        {
                            DwellTime.Add(PARA.DesignPara.MinDwellTime);
                        }
                        mapIndex++;
                    }
                }
                CreateFreLineDwell(ref Nodes, DwellTime,map_nodeId2DepartArrayIndex);

                return;
            }
            #region firstVersionWithFixedDwellTime
            // first version, in which the dwell time is set to be fixed values
            //double[] IniDep = new double[NumOfTrains];

            //if (IniDepTime.Count>0)
            //{
            //    for (int i = 0; i < NumOfTrains; i++) IniDep[i] = IniDepTime[i];
            //}
            //else
            //{
            //    MyLog.Instance.Debug("This part should not be called, since the departure time is preset and read from file");
            //    for (int i = 0; i < NumOfTrains; i++) IniDep[i] = StartTime + i * Headway;
            //}
            //CreateScheduleTable(ref Nodes, IniDep,isIniDepTime:true);
            #endregion
            
            /// revise in 2021Feb
            List<double> depval = new List<double>();

            for (int s = 0; s < Stops.Count - 1; s++)
            {
                map_nodeId2DepartArrayIndex.Add(Stops[s].ID, mapIndex);
                for (int q = 0; q < NumOfTrains; q++)
                {
                    /// the following dwellTimeTobeADD is created for the revised version
                    // if s is at the first stop, then it is added
                    double DwellTimeToBeAdd = PARA.DesignPara.MinDwellTime * s;  
                    int FirstStop = Stops[0].ID;
                    int ThisStop = Stops[s].ID;
                    double TimeBetween = getTravelTimeBetweenStop(FirstStop, ThisStop);
                    depval.Add(TimeBetween + DwellTimeToBeAdd+ IniDepTime[q]);         
                    if (s == 0)
                    {
                        DwellTime.Add(0.0);
                    }
                    else
                    {
                        DwellTime.Add(PARA.DesignPara.MinDwellTime);
                    }
                    mapIndex++;
                }
            }
            CreateScheduleTable(ref Nodes, depval,DwellTime, map_nodeId2DepartArrayIndex);


            #region CreateIniDwell
            // revise 2021Feb
            // create ini dwell cost for each seg
            
            for (int s=0;s<Stops.Count-1;s++)
            {
                switch (ServiceType)
                {
                    case TransitServiceType.Frequency:
                        
                        for (int t = 0; t < PARA.IntervalSets.Count; t++)
                        {
                            double dwc = PARA.PathPara.WaitW * Stops[s].getDwellTime(ID, t);
                            MapSegs[s].DwellCost[t] = dwc;
                        }
                        break;
                    case TransitServiceType.Schedule:
                        for (int q = 0; q < NumOfTrains; q++)
                        {
                           double dwc = PARA.PathPara.WaitW * Stops[s].getDwellTime(ID, q);
                            MapSegs[s].DwellCost[q] = dwc;
                        }
                    break;
                }
            }
            #endregion

        }


        /// <summary>
        /// Revised create timetable
        /// with input of dwell time and schedule
        /// </summary>
        /// <param name="Nodes"></param>
        /// <param name="DepTimes"></param>
        /// <param name="DwellTime"></param>
        /// <param name=""></param>
        /// <param name="m_NodeId2Index"></param>
        /// <returns></returns>
        protected internal void CreateScheduleTable(ref List<NodeClass> Nodes,List<double> DepTimes, List<double> DwellTime, Dictionary<int,int> m_NodeId2Index)
        {
            // the objective is create table run for the schedule based runs
            List<double> SetArrTimes = new List<double>();
            List<double> SetDepTimes = new List<double>();
            List<double> SetDwellTimes = new List<double>();
            int NodeId;
            StopType SetStopType;
            for (int s = 0; s < Stops.Count; s++)
            {
                NodeId = Stops[s].ID;
                if (s == 0)
                {
                    SetStopType = StopType.LanuchTerminal;
                    // for the launch terminal the departure time equals the arrival time
                    int index = m_NodeId2Index[NodeId];
                    for(int q=0;q<NumOfTrains;q++)
                    {
                        SetArrTimes.Add(DepTimes[index+q]);
                        SetDepTimes.Add(DepTimes[index+q]);
                        SetDwellTimes.Add(0.0);
                    }
                }
                else if (s == Stops.Count - 1)
                {
                    SetStopType = StopType.EndTerminal;
                    int preStop = Stops[s - 1].ID;
                    int index = m_NodeId2Index[preStop];
                    double TimeBetween = getTravelTimeBetweenStop(Stops[s - 1].ID, Stops[s].ID);
                    for (int q=0;q<NumOfTrains;q++)
                    {
                        SetArrTimes.Add(DepTimes[index + q]+TimeBetween);
                        SetDepTimes.Add(DepTimes[index+q]+TimeBetween);
                        SetDwellTimes.Add(DwellTime[index + q]);
                    }
                }
                else
                {
                    SetStopType = StopType.Intermediate;
                    int index = m_NodeId2Index[NodeId];
                    for(int q=0;q<NumOfTrains;q++)
                    {
                        SetArrTimes.Add(DepTimes[index + q] - DwellTime[index+q]);
                        SetDepTimes.Add(DepTimes[index + q]);
                        SetDwellTimes.Add(DwellTime[index + q]);
                    }
                }
                DepArrTimeClass SetTime = new DepArrTimeClass(ID, SetArrTimes, SetDepTimes, SetDwellTimes, SetStopType);

                if (Nodes[NodeId].LineTimes.Exists(x => x.LineID.Equals(ID)))
                {
                    int index = Nodes[NodeId].LineTimes.FindIndex(x => x.LineID.Equals(ID));
                    Nodes[NodeId].LineTimes.RemoveAt(index);
                    Nodes[NodeId].LineTimes.Add(SetTime);
                }
                else
                {
                    Nodes[NodeId].LineTimes.Add(SetTime);
                }
                SetDepTimes.Clear();
                SetArrTimes.Clear();
                SetDwellTimes.Clear();
            }

#if DEBUG
            /// check and print timetable 

            for (int q=0;q<NumOfTrains;q++)
            {
                for (int s =0;s<Stops.Count;s++)
                {
                    int nindex = Nodes.FindIndex(x => x.ID == Stops[s].ID);
                    int lindex = Nodes[nindex].LineTimes.FindIndex(x => x.LineID == ID);
                    Console.WriteLine("l={0},q={1},node={2},arr={3},dep={4},dwell={5}",
                        ID, q, Nodes[nindex].ID, Nodes[nindex].LineTimes[lindex].ArrTimes[q], Nodes[nindex].LineTimes[lindex].DepTimes[q],
                        Nodes[nindex].LineTimes[lindex].DwellTimes[q]
                        ); 
                }
            }

            Console.WriteLine("wtf: complete check the sch timetable of line = {0}",ID);
            //Console.ReadLine();
#endif    
        }




        /// <summary>
        /// create dwell time for the frequency based lines
        /// </summary>
        /// <param name="Nodes"></param>
        /// <param name="DwellTime"></param>
        /// <param name=""></param>
        /// <param name="m_NodeId2Index"></param>
        /// <returns></returns>
        protected internal void CreateFreLineDwell(ref List<NodeClass> Nodes, List<double> DwellTime, Dictionary<int, int> m_NodeId2Index)
        {
            // similar to the schedule timetable, but this created for schedule based line
            List<double> SetDwellTimes = new List<double>();
            int NodeId;
            StopType SetStopType;
            for (int s = 0; s < Stops.Count; s++)
            {
                NodeId = Stops[s].ID;
                if (s == 0)
                {
                    SetStopType = StopType.LanuchTerminal;
                    // for the launch terminal 
                    // the dwell time is 0.0
                    int index = m_NodeId2Index[NodeId];
                    for (int q = 0; q < PARA.IntervalSets.Count; q++)
                    {
                        SetDwellTimes.Add(0.0);
                    }
                }
                else if (s == Stops.Count - 1)
                {
                    SetStopType = StopType.EndTerminal;
                    // the dwell time is zero at the destination of the bus line
                    int index = m_NodeId2Index[NodeId];
                    for (int q = 0; q < PARA.IntervalSets.Count; q++)
                    {
                        SetDwellTimes.Add(DwellTime[index+q]);
                    }
                }
                else
                {
                    SetStopType = StopType.Intermediate;
                    int index = m_NodeId2Index[NodeId];
                    for (int q = 0; q < PARA.IntervalSets.Count; q++)
                    {
                        SetDwellTimes.Add(DwellTime[index+q]);
                    }
                }
                DepArrTimeClass SetTime = new DepArrTimeClass(ID, SetDwellTimes, SetStopType); 
                //DepArrTimeClass SetTime = new DepArrTimeClass(ID, SetArrTimes, SetDepTimes, SetStopType);

                if (Nodes[NodeId].LineTimes.Exists(x => x.LineID.Equals(ID)))
                {
                    int index = Nodes[NodeId].LineTimes.FindIndex(x => x.LineID.Equals(ID));
                    Nodes[NodeId].LineTimes.RemoveAt(index);
                    Nodes[NodeId].LineTimes.Add(SetTime);
                }
                else
                {
                    Nodes[NodeId].LineTimes.Add(SetTime);
                }
                SetDwellTimes.Clear();
                SetDwellTimes.Clear();
            }

#if DEBUG
            /// check and print timetable 
            for (int q = 0; q < NumOfTrains; q++)
            {
                for (int s = 0; s < Stops.Count; s++)
                {
                    int nindex = Nodes.FindIndex(x => x.ID == Stops[s].ID);
                    int lindex = Nodes[nindex].LineTimes.FindIndex(x => x.LineID == ID);
                    Console.WriteLine("l={0},q={1},node={2},arr={3},dep={4}",
                        ID, q, Nodes[nindex].ID, Nodes[nindex].LineTimes[lindex].ArrTimes[q], Nodes[nindex].LineTimes[lindex].DepTimes[q]);
                }
            }

            Console.WriteLine("wtf: complete check the sch timetable of line = {0}", ID);
            //Console.ReadLine();
#endif
        }



    }//
}



#region createSchInTheFirstVersion
/// <summary>
/// create schedule for each stop given the departure time at the first stop
/// </summary>
/// <param name="Nodes"></param>
/// <param name="DepTimes"></param>
/// <returns></returns>
//protected internal void CreateScheduleTable(ref List<NodeClass> Nodes, double[] DepTimes, bool isIniDepTime)
//{
//    if (DepTimes.Length != NumOfTrains)
//        Debug.Assert(DepTimes.Length == NumOfTrains, "Dep time length not equals the number of trains");
//    // the objective is create table run for the schedule based runs
//    List<double> SetArrTimes = new List<double>();
//    List<double> SetDepTimes = new List<double>();
//    int NodeId;
//    StopType SetStopType;
//    for (int s = 0; s < Stops.Count; s++)
//    {
//        NodeId = Stops[s].ID;
//        if (s == 0)
//        {
//            SetStopType = StopType.LanuchTerminal;
//            // for the launch terminal the departure time equals the arrival time
//            foreach(double dep in DepTimes)
//            {
//                SetArrTimes.Add(dep);
//                SetDepTimes.Add(dep);
//            }
//        }
//        else if (s == Stops.Count - 1)
//        {
//            SetStopType = StopType.EndTerminal;
//            double addTime = m_Stop_TimeDif[ID][Stops[s].ID];
//            double TimeBetween = getTravelTimeBetweenStop(Stops[s-1].ID, Stops[s].ID);
//            Console.WriteLine("wtf: Between time = {0}", TimeBetween);
//            foreach (double dep in DepTimes)
//            {
//                SetArrTimes.Add(dep + addTime - PARA.DesignPara.MinDwellTime);
//                SetDepTimes.Add(dep + addTime);
//            }
//        }
//        else
//        {
//            SetStopType = StopType.Intermediate;
//            double addTime = m_Stop_TimeDif[ID][Stops[s].ID];
//            double TimeBetween = getTravelTimeBetweenStop(Stops[s-1].ID, Stops[s].ID);
//            Console.WriteLine("wtf: Between time = {0}", TimeBetween);
//            foreach (double dep in DepTimes)
//            {
//                SetArrTimes.Add(dep + addTime-PARA.DesignPara.MinDwellTime);
//                SetDepTimes.Add(dep + addTime );
//            }
//        }
//        DepArrTimeClass SetTime = new DepArrTimeClass(ID, SetArrTimes, SetDepTimes, SetStopType);

//        if (Nodes[NodeId].LineTimes.Exists(x=>x.LineID.Equals(ID)))
//        {
//            int index = Nodes[NodeId].LineTimes.FindIndex(x => x.LineID.Equals(ID));
//            Nodes[NodeId].LineTimes.RemoveAt(index);
//            Nodes[NodeId].LineTimes.Add(SetTime);
//        }
//        else
//        {
//            Nodes[NodeId].LineTimes.Add(SetTime);
//        }
//        SetDepTimes.Clear();
//        SetArrTimes.Clear();
//    }
//}
#endregion