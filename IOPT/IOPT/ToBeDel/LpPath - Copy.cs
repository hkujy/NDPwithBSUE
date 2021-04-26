using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

// this class convert the event array to path array list 
namespace IOPT
{
    public class LpPath
    {
        protected internal int ID { get; set; }
        protected internal int TripId { get; set; }
        protected internal double TripDemand { get; set; } 
        protected internal int StartEventID { get; set; }  // the event id at the destination node
        protected internal double TargetDepTime { get; set; }
        protected internal List<int> VisitNodes { get; set; }
        protected internal List<double> TimeBetweenNodes { get; set; }
        protected internal Dictionary<int, int> MapWaitVar { get; set; }  //<node, wait position> map to waiting time variabl
        protected internal Dictionary<int,int> MapArrVar { get; set; } // <node, arr var position>map to the arrival time variables at each node 
        protected internal Dictionary<int,int> MapDepVar { get; set; } // <node dep position>
        // ignore the arr and dep variable at the destination node
        // remark the map dep/arr varible start from the destination node. i.e., destination is 0

        protected internal Dictionary<int,int> MapTrainVar { get; set; } // <node, train position>
        protected internal Dictionary<int,int> MapBoardLine { get; set; }
        protected internal Dictionary<int,TransitServiceType> MapBoardLineType { get; set; }
        protected internal static int WaitVarPos;
        protected internal static int DepVarPos;
        protected internal static int ArrVarPos;
        protected internal static int TrainVarPos;
        protected internal static int DeltaPos;
        protected internal Dictionary<TransitVehicleType, double> InVehTime;
        protected internal Dictionary<int, int> MapDelta;
        protected internal double PathPie { get; set; }
        protected internal double PathProb { get; set; }
        LpPath()
        {
            TargetDepTime = PARA.NULLDOUBLE;
            ID = PARA.NULLINT;
            StartEventID = PARA.NULLINT;
            MapWaitVar = new Dictionary<int, int>();
            MapArrVar = new Dictionary<int, int>();
            MapDepVar = new Dictionary<int, int>();
            MapTrainVar = new Dictionary<int, int>();
            MapBoardLine = new Dictionary<int, int>();
            InVehTime = new Dictionary<TransitVehicleType, double>();
            MapBoardLineType = new Dictionary<int, TransitServiceType>();
            TimeBetweenNodes = new List<double>();
            MapDelta = new Dictionary<int, int>();

            InVehTime.Add(TransitVehicleType.Bus, 0);
            InVehTime.Add(TransitVehicleType.Metro, 0);
            InVehTime.Add(TransitVehicleType.S_Train, 0);
            InVehTime.Add(TransitVehicleType.Train, 0);
            VisitNodes = new List<int>();
        }

        protected internal void SetLpPath(EventClass[] EventArray, int SetStartEventID, int PathID, List<SegClass> Segs, List<NodeClass> Nodes,
                                        List<TransitLineClass> Lines)
        {


            // initialize static variables at beginning 
            if (PathID == 0)
            {
                WaitVarPos = 0;
                DepVarPos = 0;
                ArrVarPos = 0;
                TrainVarPos = 0;
                DeltaPos = 0;
            }
            // step 0: initialize path 
            ID = PathID;
            StartEventID = SetStartEventID;
          
            int Now = StartEventID;
            int Pre = EventArray[Now].PathFromEventID;
            VisitNodes.Add(EventArray[Now].NodeID);
            // set dep / arrivl time at the destination nodes 

            MapArrVar.Add(EventArray[Now].NodeID, ArrVarPos);
            ArrVarPos++;
            MapDepVar.Add(EventArray[Now].NodeID, DepVarPos);
            DepVarPos++;
            int BoardLineID =  PARA.NULLINT;
            Debug.Assert(Pre != PARA.NULLINT, "Invalid Pre event ID");
            double BetweenTime = 0;
            do
            {
                if (EventArray[Pre].Type == EventType.Node)
                {
                    VisitNodes.Insert(0, EventArray[Pre].NodeID);
                    // add and map wait, arrival and dep varibles
                    MapWaitVar.Add(EventArray[Pre].NodeID, WaitVarPos);
                    WaitVarPos++;
                    Debug.Assert(EventArray[Now].Type == EventType.Seg);
                    MapArrVar.Add(EventArray[Pre].NodeID, ArrVarPos);
                    ArrVarPos++;
                    MapDepVar.Add(EventArray[Pre].NodeID, DepVarPos);
                    DepVarPos++;

                    BoardLineID = Segs[EventArray[Now].SegID].MapLine[0].ID;
                    MapBoardLine.Add(EventArray[Pre].NodeID, BoardLineID);
                    MapBoardLineType.Add(BoardLineID, Lines[BoardLineID].ServiceType);

                    if (Lines[BoardLineID].ServiceType == TransitServiceType.Frequency)
                    {

                    }
                    else if (Lines[BoardLineID].ServiceType== TransitServiceType.Schedule)
                    {
                        MapTrainVar.Add(EventArray[Pre].NodeID, TrainVarPos);
                        TrainVarPos += Lines[BoardLineID].NumOfTrains;
                        MapDelta.Add(EventArray[Pre].NodeID, DeltaPos);
                        DeltaPos += Lines[BoardLineID].NumOfTrains;
                    }

                    TimeBetweenNodes.Insert(0, BetweenTime);
                    BetweenTime = 0;

                }
                else if (EventArray[Pre].Type == EventType.Seg)
                {
                    int SegID = EventArray[Pre].SegID;
                    int LineID = Segs[SegID].MapLine[0].ID;
                    
                    if (EventArray[Now].Type==EventType.Seg)
                    {
                        // if it cross more than one segments 
                        // then the cost should add the dwell time in the middle stop 
                        BetweenTime += Segs[SegID].TravelTime + PARA.DwellTime;
                    }
                    else
                    {
                        BetweenTime += Segs[SegID].TravelTime;
                    }
                    // add in-vehicle travel time
                    InVehTime[Lines[LineID].VehicleType] += Segs[SegID].TravelTime;

                    if (Lines[LineID].ServiceType==TransitServiceType.Frequency)
                    {

                    }
                    else if (Lines[LineID].ServiceType == TransitServiceType.Schedule)
                    {

                    }
                    Debug.Assert(Lines[LineID].ServiceType != TransitServiceType.IsNull,"Null transit line service type");

                }
                Debug.Assert(EventArray[Pre].Type != EventType.IsNull, "Invalid Event Type");

                Now = Pre;
                Pre = EventArray[Now].PathFromEventID;

            } while (Pre != PARA.NULLINT);
            // reverse add the time between nodes


        }
        public static void CreatPathSets(List<UniqueOrigin> Origins, EventClass[] EventArray, List<TripClass> Trips, List<LpPath> LpPathSet,
                                        List<NodeClass> Nodes, List<SegClass> Segs, List<TransitLineClass> Lines)
        {
            int count = 0;
            int NowEventID;
            for (int i = 0; i < Origins.Count(); i++)
            {
                //Console.WriteLine("Create Path set of OD pair 0 only");
                //if (i == 1) continue;
                for (int j = 0; j < Origins[i].IncludeTrips.Count(); j++)
                {
                    NowEventID = Trips[Origins[i].IncludeTrips[j]].FirstNonDomEventID;
                    do
                    {
                        LpPathSet.Add(new LpPath());
                        LpPathSet[count].SetLpPath(EventArray, NowEventID, count, Segs, Nodes, Lines);
                        LpPathSet[count].TargetDepTime = Trips[Origins[i].IncludeTrips[j]].TargetDepTime;
                        LpPathSet[count].TripDemand = Trips[Origins[i].IncludeTrips[j]].Demand;
                        NowEventID = EventArray[NowEventID].LateNonDomEventID;
                        LpPathSet[count].TripId = Trips[Origins[i].IncludeTrips[j]].ID;
                        count++;
                    } while (NowEventID != PARA.NULLINT);
                }
            }
        }
    }
    
}
