/// updated 2021-May
using System.Collections.Generic;
namespace IOPT
{
    public class TransitLineClass
    {
        protected internal int ID { get; set; }
        protected internal int NumOfTrains { get; set; }
        protected internal int NumOfDoors { get; set; }    
        protected internal int NumOfSegs { get; set; }
        protected internal double FreCap { get; set; }
        protected internal double Headway { get; set; }
        protected internal string Name { get; set; }
        protected internal double StartOperationTime { get; set; } // start and end time for the operations
        protected internal double EndOperationTime { get; set; }
        protected internal double Length { get; set; } // travel distance use in computing frequency
        protected internal bool isInvolvedInDecsionVar { get; set; } // indicate whether a line is involved in the decision and to be solved 
        protected internal TransitServiceType ServiceType;
        protected internal TransitVehicleType VehicleType;
        protected internal List<double> TrainCap { get; set; }
        protected internal List<double> IniDepTime { get; set; }
        protected internal List<double> DepTimes;
        protected internal List<double> SegTravelTimes;
        protected internal List<SegClass> MapSegs;
        protected internal List<NodeClass> Stops;
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
            StartOperationTime = PARA.NULLDOUBLE;
            EndOperationTime = PARA.NULLDOUBLE;
            Length = PARA.NULLDOUBLE;
            Stops = new List<NodeClass>();
            DepTimes = new List<double>();
            SegTravelTimes = new List<double>();
            MapSegs = new List<SegClass>();
            IniDepTime = new List<double>();
        }
        public TransitLineClass() { ini(); }
        protected internal TransitLineClass(int SetID, string SetName, double SetStartTime,
            double SetEndTime, double SetHeadWay, TransitServiceType SetSerType, TransitVehicleType SetVehType)
        {
            ini();
            ID = SetID;
            Name = SetName;
            StartOperationTime = SetStartTime;
            EndOperationTime = SetEndTime;
            Headway = SetHeadWay;
            ServiceType = SetSerType;
            VehicleType = SetVehType;
        }
        /// <summary>
        /// given the stop id of two nodes, 
        /// return the travel time between the two nodes
        /// </summary>
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
            return val;
        }

        /// <summary>
        /// find the next arrival bus at a stop node, the next bus arrival time equal the departure time at the node
        /// </summary>
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
            return NextDepTime;
        }

        protected internal void IniSchedule(ref List<NodeClass> Nodes)
        {
            Dictionary<int, int> map_nodeId2DepartArrayIndex = new Dictionary<int, int>();
            List<double> DwellTime = new List<double>();
            int mapIndex = 0;
            #region FreType
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
            #endregion
            #region SchType
            List<double> depval = new List<double>();
            for (int s = 0; s < Stops.Count - 1; s++)
            {
                map_nodeId2DepartArrayIndex.Add(Stops[s].ID, mapIndex);
                for (int q = 0; q < NumOfTrains; q++)
                {
                    double DwellTimeToBeAdd = PARA.DesignPara.MinDwellTime * s;  
                    int FirstStop = Stops[0].ID; int ThisStop = Stops[s].ID;
                    double TimeBetween = getTravelTimeBetweenStop(FirstStop, ThisStop);
                    depval.Add(TimeBetween + DwellTimeToBeAdd+ IniDepTime[q]);         
                    if (s == 0) { DwellTime.Add(0.0); }
                    else { DwellTime.Add(PARA.DesignPara.MinDwellTime); }
                    mapIndex++;
                }
            }
            CreateScheduleTable(ref Nodes, depval,DwellTime, map_nodeId2DepartArrayIndex);
            // the following initialized the dwell times
            for (int s=0;s<Stops.Count-1;s++)
            {
                for (int q = 0; q < NumOfTrains; q++)
                {
                    double dwc = PARA.PathPara.WaitW * Stops[s].getDwellTime(ID, q);
                    MapSegs[s].DwellCost[q] = dwc;
                }
            }
            #endregion
        }
        /// <summary>
        /// Revised create timetable
        /// with input of dwell time and schedule
        /// </summary>
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
        }
        /// <summary>
        /// create dwell time for the frequency based lines
        /// </summary>
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
                    // for the launch terminal, the dwell time is 0.0
                    int index = m_NodeId2Index[NodeId];
                    for (int q = 0; q < PARA.IntervalSets.Count; q++)
                    {
                        SetDwellTimes.Add(0.0);
                    }
                }
                else if (s == Stops.Count - 1)
                {
                    SetStopType = StopType.EndTerminal;
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
        }
    }//
}