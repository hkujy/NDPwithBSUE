using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace IOPT
{
    //TODO Consider add penalty relate to the earlier or late arrival time

    public partial class EventPath
    {
        public static void InitFlow(ref List<NodeClass> _Nodes, ref List<SegClass> _Segs)
        {
            foreach (NodeClass n in _Nodes)
            {
                foreach (var key in n.m_LineID_Flow.Keys)
                {
                    for (int i = 0; i < n.m_LineID_Flow[key].Count; i++)
                    {
                        n.m_LineID_Flow[key][i].Ini();
                    }
                }
            }
            foreach (SegClass s in _Segs)
            {
                //foreach (var f in s.OnBoardFlow) f.Ini();
                for (int i = 0; i < s.OnBoardFlow.Count; i++) s.OnBoardFlow[i] = 0;
                for (int i = 0; i < s.CapCost.Count; i++) s.CapCost[i] = 0;
            }
        }

        /// <summary>
        /// Generate Event Dom Path Set
        /// </summary>
        /// <param name="_Origin"></param>
        /// <param name="_Nodes"></param>
        /// <param name="_Segs"></param>
        /// <param name="_Lines"></param>
        /// <param name="_Events"></param>
        /// <param name="_Trips"></param>
        /// <returns></returns>
        public static void GenPathSet(List<UniqueOrigin> _Origin, ref List<NodeClass> _Nodes, ref List<SegClass> _Segs, ref List<TransitLineClass> _Lines,
                             ref EventClass[] _Events, ref List<TripClass> _Trips)
        {
            Initialization(ref _Nodes, ref _Segs);

            for (int i = 0; i < _Origin.Count; i++)
            {
                Console.WriteLine("Start Event Search for OD pair = {0}", i);
                PARA.GenEvtLog.WriteLine("Start Event Search for OD pair = {0}", i);

                //for (int j = 0; j < _Origin[i].IncludeTrips.Count; j++)
                //{
                //    LeastCost(_Origin[i], ref _Nodes, ref _Segs, ref _Lines, ref _Events, ref _Trips,
                //        _Origin[i].IncludeTrips[j]);
                //}
                foreach (int j in _Origin[i].IncludeTrips)
                {
                    LeastCost(_Origin[i], ref _Nodes, ref _Segs, ref _Lines, ref _Events, ref _Trips, j);
                }

            }
            OutputLowerLevel(_Origin, _Trips, _Segs, _Events);
        }

        /// <summary>
        /// Initialization phase for before the least cost event algorithm
        /// Initialize all the unique origins
        /// </summary>
        /// <param name="Nodes"></param>
        /// <param name="Segs"></param>
        public static void Initialization(ref List<NodeClass> Nodes, ref List<SegClass> Segs)
        {
            foreach (NodeClass n in Nodes)
            {
                n._IniNonDomEventID();
            }
            foreach (SegClass s in Segs)
            {
                s._IniNonDomEventID();
            }
        }


        public static double _AddCpCostValue(TransitVehicleType LineType, double TravelTime, double WaitTime, bool isAddTranfer)
        {
            double cp = 0.0d;
            double LineTypePara = 0.0d;
            switch (LineType)
            {
                case TransitVehicleType.Bus: LineTypePara = PARA.PathPara.InVBusW; break;
                case TransitVehicleType.Metro: LineTypePara = PARA.PathPara.InVMetroW; break;
                case TransitVehicleType.S_Train: LineTypePara = PARA.PathPara.InVSTrainW; break;
                case TransitVehicleType.Train: LineTypePara = PARA.PathPara.InVTrainW; break;
            }
            if (isAddTranfer)
            {
                cp = TravelTime * LineTypePara + PARA.PathPara.TransferP + WaitTime * PARA.PathPara.WaitW;
            }
            else
            {
                cp = TravelTime * LineTypePara + WaitTime * PARA.PathPara.WaitW;
            }
            return cp;
        }

        /// <summary>
        /// Event Dominance least cost algorithm
        /// 1. get shortest path tree
        /// </summary>
        /// <param name="Origin"></param>
        /// <param name="Nodes"></param>
        /// <param name="Segs"></param>
        /// <param name="Lines"></param>
        /// <param name="EventArray"></param>
        /// <param name="Trips"></param>
        public static void LeastCost(UniqueOrigin _Origin, ref List<NodeClass> _Nodes, ref List<SegClass> _Segs, ref List<TransitLineClass> _Lines,
                                     ref EventClass[] _Events, ref List<TripClass> _Trips, int _TripID)
        {
            // initial local variables 
            double tp = -1; // time // corresponds to the departure time == the next bus arrival time// arrival time
            double cp = -1;// cost
            int NewHeapHead = 0;
            double CurrentTime = PARA.NULLDOUBLE;
            double WaitTime = PARA.NULLDOUBLE;
            int LineID = PARA.NULLINT;
            int NextNodeID = PARA.NULLINT;
            int NextSegID = PARA.NULLINT;
            int CurrentNodeID = PARA.NULLINT;
            int CurrentSegID = PARA.NULLINT;
            // the first non dom event in the next element(i.e. node/seg)
            int FirstNonDomEventInNextElement = PARA.NULLINT;
            int NextEventID = PARA.NULLINT;
            bool IsTransfer = false; // indicate whether it is a transfer node
            TransitVehicleType VehType = new TransitVehicleType();

            // create initial events and set the head of the heap
            int HeapHeadID = Global.EventNumCount;
            EventClass.CreateIniEvent(_Origin, ref _Events, _TripID, _Trips);
            EventClass.CreatHeap(ref _Events[HeapHeadID]);
            int TrainIndex = -1;
            while (HeapHeadID != PARA.NULLINT)
            {
                // take event from the top of the heap
                NewHeapHead = _Events[HeapHeadID].DeHeap(ref _Events, out NextEventID);

                if (_Events[HeapHeadID].Type == EventType.Node)
                {
                    Console.WriteLine("HeadHeadId: {0}, AtNode: {1}", HeapHeadID, _Events[HeapHeadID].NodeID);
                    PARA.GenEvtLog.WriteLine("HeadHeadId: {0}, AtNode: {1}", HeapHeadID, _Events[HeapHeadID].NodeID);
                }
                else
                {
                    Console.WriteLine("HeapHeadId: {0}, AtSeg: {1}, Tail: {2}, Head: {3}, Line: {4}", HeapHeadID,
                        _Events[HeapHeadID].SegID, _Segs[_Events[HeapHeadID].SegID].Tail.ID, _Segs[_Events[HeapHeadID].SegID].Head.ID, _Segs[_Events[HeapHeadID].SegID].MapLine[0].ID);
                    PARA.GenEvtLog.WriteLine("HeapHeadId: {0}, AtSeg: {1}, Tail: {2}, Head: {3}, Line: {4}", HeapHeadID,
                        _Events[HeapHeadID].SegID, _Segs[_Events[HeapHeadID].SegID].Tail.ID, _Segs[_Events[HeapHeadID].SegID].Head.ID, _Segs[_Events[HeapHeadID].SegID].MapLine[0].ID);
                }

                switch (_Events[HeapHeadID].Type)
                {
                    case EventType.Node:  // refer to the current event type

                        _Events[HeapHeadID].PrintEvent(_Segs, isNewEvent: false, InFile: true, InScreen: true);

                        CurrentNodeID = _Events[HeapHeadID].NodeID;
                        for (int s = 0; s < _Nodes[CurrentNodeID].OutSegs.Count; s++)
                        {
                            CurrentTime = _Events[HeapHeadID].Time;
                            NextSegID = _Nodes[CurrentNodeID].OutSegs[s].ID;
                            LineID = _Nodes[CurrentNodeID].OutSegs[s].MapLine[0].ID;
                            VehType = _Lines[LineID].VehicleType;
                            FirstNonDomEventInNextElement = _Segs[NextSegID].NonDomEventID[_TripID];
                            Debug.Assert(_Nodes[CurrentNodeID].OutSegs[s].MapLine.Count == 1, "Segment map line size >1");

                            tp = TransitLineClass.NextArrival(CurrentTime, _Nodes[CurrentNodeID], LineID, _Lines, out TrainIndex, isBoard: true, ConsiderMinGap: true);

                            // if it not an original node, then it is a transfer
                            // because at the node, the passenger needs to board another line
                            if (CurrentNodeID != _Origin.OriginID) IsTransfer = true;
                            else IsTransfer = false;

                            if (tp < PARA.DesignPara.MaxTimeHorizon)
                            {
                                // the minimum passenger arrival and departure gap time is not considered in the waiting time
                                if (_Lines[LineID].ServiceType == TransitServiceType.Schedule) WaitTime = tp - CurrentTime - PARA.PathPara.MinPasArrDepGap;
                                else WaitTime = tp - CurrentTime;
                                //only add congestion cost when board at the node or transfer to another line
                                cp = _Events[HeapHeadID].Cost + _AddCpCostValue(VehType, 0.0d, WaitTime, IsTransfer) +
                                            _Nodes[CurrentNodeID].OutSegs[s]._CapCost(tp, TrainIndex);
                                EventClass.CreatTempEvent(tp, cp, _Events[HeapHeadID].ID, EventType.Seg, PARA.NULLINT, NextSegID, ref _Events);
                                if (_Events[Global.EventNumCount].IsCompareWin(FirstNonDomEventInNextElement, ref NewHeapHead, ref _Events, ref _Nodes, ref _Segs, _Origin.OriginID, _TripID))
                                {
                                    _Events[Global.EventNumCount - 1].PrintEvent(_Segs, isNewEvent: true, InFile: true, InScreen: true);
                                }
                            }
                        }
                        break;
                    case EventType.Seg:

                        // if current event is at segment
                        // first check the continuous segment : ie. so that passenger does not make a transfer 
                        CurrentTime = _Events[HeapHeadID].Time;
                        CurrentSegID = _Events[HeapHeadID].SegID;
                        Debug.Assert(CurrentSegID != PARA.NULLINT, "Seg ID is Null int");
                        NextSegID = _Segs[CurrentSegID].NextSegID;
                        NextNodeID = _Segs[_Events[HeapHeadID].SegID].Head.ID;


                        LineID = _Segs[CurrentSegID].MapLine[0].ID;
                        if (NextSegID != PARA.NULLINT)  // only check if next segment and current segment belong to a same transit line
                        {
                            Debug.Assert(_Segs[CurrentSegID].MapLine.Count == 1, "Segment map line size >1");
                            FirstNonDomEventInNextElement = _Segs[NextSegID].NonDomEventID[_TripID];
                            IsTransfer = false;   // using the same line board to the other segment 
                            VehType = _Lines[LineID].VehicleType;
                            Debug.Assert(_Segs[_Events[HeapHeadID].SegID].MapLine.Count == 1, "Segment map line size >1");
                            tp = TransitLineClass.NextArrival(CurrentTime + _Segs[CurrentSegID].TravelTime, _Nodes[NextNodeID], LineID, _Lines, out TrainIndex, isBoard: true,
                                ConsiderMinGap: false);
                            if (tp < PARA.DesignPara.MaxTimeHorizon)
                            {
                                cp = _Events[HeapHeadID].Cost + _AddCpCostValue(VehType, _Segs[CurrentSegID].TravelTime, 0.0d, IsTransfer)
                                    + _Segs[NextSegID]._CapCost(tp, TrainIndex);
                                EventClass.CreatTempEvent(tp, cp, HeapHeadID, EventType.Seg, PARA.NULLINT, NextSegID, ref _Events);
                                if (_Events[Global.EventNumCount].IsCompareWin(FirstNonDomEventInNextElement, ref NewHeapHead, ref _Events, ref _Nodes, ref _Segs, _Origin.OriginID, _TripID))
                                {
                                    _Events[Global.EventNumCount - 1].PrintEvent(_Segs, isNewEvent: true, InFile: true, InScreen: true);
                                }
                            }
                        }
                        // step 2: check the event associated with alight node : transfer to another line, so inthe nextarrival function isBoard is set to be fast
                        // this represent alight at the head node, in such case, the waiting time is 0.0, no transfer cost, since it is added to the boarding event
                        IsTransfer = false;  // transfer cost is only added from node to seg, to avoid duplicated compute
                        tp = TransitLineClass.NextArrival(CurrentTime + _Segs[CurrentSegID].TravelTime, _Nodes[NextNodeID], LineID, _Lines, out TrainIndex, isBoard: false,
                                                ConsiderMinGap: false);
                        Console.WriteLine("tp time = {0}", tp);
                        if (tp < PARA.DesignPara.MaxTimeHorizon)
                        {
                            cp = _Events[HeapHeadID].Cost + _AddCpCostValue(VehType, _Segs[CurrentSegID].TravelTime, 0.0d, IsTransfer);
                            //+ _Segs[NextSegID]._CapCost(tp,TrainIndex);
                            EventClass.CreatTempEvent(tp, cp, _Events[HeapHeadID].ID, EventType.Node, NextNodeID, PARA.NULLINT, ref _Events);
                            FirstNonDomEventInNextElement = _Nodes[NextNodeID].NonDomEventID[_TripID];
                            if (_Events[Global.EventNumCount].IsCompareWin(FirstNonDomEventInNextElement, ref NewHeapHead, ref _Events, ref _Nodes, ref _Segs, _Origin.OriginID, _TripID))
                            {
                                _Events[Global.EventNumCount - 1].PrintEvent(_Segs, isNewEvent: true, InFile: true, InScreen: true);
                            }
                        }
                        break;
                }
                // finish switch cases
                HeapHeadID = NewHeapHead;
            }

            _Trips[_TripID].FirstNonDomEventID = _Nodes[_Trips[_TripID].DestID].NonDomEventID[_TripID];

        }

        public static void OutputLowerLevel(List<UniqueOrigin> UniOrigins, List<TripClass> Trips, List<SegClass> Segs, EventClass[] Events)
        {
            for (int i = 0; i < Trips.Count(); i++)
            {
                Trips[i].PrintPath(Events, Segs);
            }
            EventClass.PrintAllEvent(Events);
        }
    }
}
