// I do not want to touch this 7-Jan-2019
using System;
using System.Linq;

namespace IOPT
{

    public partial class NetworkClass
    {
        /// <summary>
        /// Generate non dominate events for the network
        /// </summary>
        /// <returns></returns>
        protected internal void GenPathSet()
        {
            InitNonDomEventID();
            for (int i = 0; i < UniOrigins.Count; i++)
            {
                foreach (int j in UniOrigins[i].IncludeTrips)
                {
                    LeastCost(UniOrigins[i], j);
                }
            }
            PrintLowerLevelPaths();
        }

        protected internal void PrintLowerLevelPaths()
        {
            PrintAllEvent();
            for (int i = 0; i < Trips.Count(); i++)
            {
                Trips[i].PrintPath(Events, Segs);
            }
        }
        protected internal double AddCpCostValue(TransitVehicleType LineType, double TravelTime, double WaitTime, bool isAddTranfer)
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
                cp = TravelTime * LineTypePara + PARA.PathPara.TransferPenalty + WaitTime * PARA.PathPara.WaitW;
            }
            else
            {
                cp = TravelTime * LineTypePara + WaitTime * PARA.PathPara.WaitW;
            }
            return cp;
        }

        /// <summary>
        /// Generate Least Cost Path
        /// _trip Id is an index for the OD pair
        /// </summary>
        protected internal void LeastCost(UniqueOrigin Origin, int _TripID)
        {
            /// step 0: initial local variables 
            double tp = -1; // time // corresponds to the departure time == the next bus arrival time// arrival time
            double cp = -1;// cost
            int NewHeapHead = 0;
            double CurrentTime = PARA.NULLDOUBLE;
            double WaitTime = PARA.NULLDOUBLE;
            int LineID = PARA.NULLINT; int NextNodeID = PARA.NULLINT; int NextSegID = PARA.NULLINT;
            int CurrentNodeID = PARA.NULLINT; int CurrentSegID = PARA.NULLINT;
            // the first non dom event in the next element(i.e. node/seg)
            int FirstNonDomEventInNextElementSet = PARA.NULLINT;
            int NextEventID = PARA.NULLINT;
            bool IsTransfer = false; // indicate whether it is a transfer node
            TransitVehicleType VehType = new TransitVehicleType();
            // create initial events and set the head of the heap
            int HeapHeadID = Global.EventNumCount;
            CreateIniEvent(ref Origin, _TripID);
            EventClass.CreatHeapHead(ref Events[HeapHeadID]);
            int TrainIndex = -1;
            while (HeapHeadID != PARA.NULLINT)
            {
                // take event from the top of the heap
                NewHeapHead = Events[HeapHeadID].DeHeap(ref Events, out NextEventID);
                Events[HeapHeadID].PrintEvent(Segs, isNewEvent: false, InFile: true, InScreen: PARA.PrintEventPathOnScreen);
                switch (Events[HeapHeadID].Type)
                {
                    case EventType.Node:  // refer to the current event type
                        CurrentNodeID = Events[HeapHeadID].NodeID;
                        int fromLineId = -1;
                        bool isToBoardaNewLine = true;
                        if (Events[HeapHeadID].PathFromEventID > -1)
                        {
                            fromLineId = Segs[Events[Events[HeapHeadID].PathFromEventID].SegID].MapLine[0].ID;
                        }

                        for (int s = 0; s < Nodes[CurrentNodeID].OutSegs.Count; s++)
                        {
                            CurrentTime = Events[HeapHeadID].Time;
                            NextSegID = Nodes[CurrentNodeID].OutSegs[s].ID;
                            LineID = Nodes[CurrentNodeID].OutSegs[s].MapLine[0].ID;
                            if (fromLineId == LineID)
                                isToBoardaNewLine = false;
                            else
                                isToBoardaNewLine = true;
                            VehType = Lines[LineID].VehicleType;
                            FirstNonDomEventInNextElementSet = Segs[NextSegID].NonDomEventID[_TripID];
                            if (CurrentNodeID != Origin.OriginID&&isToBoardaNewLine) { IsTransfer = true; }
                            else IsTransfer = false;
                            if (IsTransfer)
                            {
                                //not the boarding node
                                tp = TransitLineClass.NextArrival
                                    (CurrentTime, Nodes[CurrentNodeID], LineID, Lines, out TrainIndex, 
                                    isToBoardaNewLine, ConsiderMinTransferGap: true);
                            }
                            else
                            {
                                // if it is the boarding node then do not consider minimum transfer 
                                tp = TransitLineClass.NextArrival
                                    (CurrentTime, Nodes[CurrentNodeID], LineID, Lines, out TrainIndex, isToBoardaNewLine, ConsiderMinTransferGap: false);
                            }
                            if (tp < PARA.DesignPara.MaxTimeHorizon)
                            {
                                if (!(TrainIndex == -1 && Nodes[CurrentNodeID].OutSegs[s].MapLine[0].ServiceType == TransitServiceType.Schedule))
                                {
                                    WaitTime = tp - CurrentTime;
                                    cp = Events[HeapHeadID].Cost + AddCpCostValue(VehType, 0.0d, WaitTime, IsTransfer)
                                                + Nodes[CurrentNodeID].OutSegs[s].getCapCost(tp, TrainIndex)*PARA.PathPara.ConW
                                                + Nodes[CurrentNodeID].OutSegs[s].getDwellCost(tp, TrainIndex)*PARA.PathPara.WaitW;

                                    CreatTempEvent(tp, cp, Events[HeapHeadID].ID, EventType.Seg, PARA.NULLINT, NextSegID);
                                    if (Events[Global.EventNumCount].IsCompareWin(FirstNonDomEventInNextElementSet, ref NewHeapHead, ref Events, ref Nodes, ref Segs, Origin.OriginID, _TripID))
                                    {
                                        Events[Global.EventNumCount - 1].PrintEvent(Segs, isNewEvent: true, InFile: true, InScreen: PARA.PrintEventPathOnScreen);
                                    }
                                }
                            }
                        }
                        break;
                    case EventType.Seg:
                        CurrentTime = Events[HeapHeadID].Time;
                        CurrentSegID = Events[HeapHeadID].SegID;
                        NextSegID = Segs[CurrentSegID].NextSegID;
                        NextNodeID = Segs[Events[HeapHeadID].SegID].Head.ID;
                        LineID = Segs[CurrentSegID].MapLine[0].ID;
                        IsTransfer = false;  // transfer cost is only added from node to seg, to avoid duplicated compute
                        tp = TransitLineClass.NextArrival
                            (CurrentTime + Segs[CurrentSegID].TravelTime, Nodes[NextNodeID], LineID, Lines, 
                                out TrainIndex, isToBoardaNewLine: false, ConsiderMinTransferGap: false);
                        if (tp < PARA.DesignPara.MaxTimeHorizon)
                        {
                            cp = Events[HeapHeadID].Cost + AddCpCostValue(VehType, Segs[CurrentSegID].TravelTime, 0.0d, IsTransfer);
                            CreatTempEvent(tp, cp, Events[HeapHeadID].ID, EventType.Node, NextNodeID, PARA.NULLINT);
                            FirstNonDomEventInNextElementSet = Nodes[NextNodeID].NonDomEventID[_TripID];
                            if (Events[Global.EventNumCount].IsCompareWin(FirstNonDomEventInNextElementSet, ref NewHeapHead, ref Events, ref Nodes, ref Segs, Origin.OriginID, _TripID))
                            {
                                Events[Global.EventNumCount - 1].PrintEvent(Segs, isNewEvent: true, InFile: true, InScreen: PARA.PrintEventPathOnScreen);
                            }
                        }
                        break;
                }
                HeapHeadID = NewHeapHead;
            }
            Trips[_TripID].FirstNonDomEventID = Nodes[Trips[_TripID].DestID].NonDomEventID[_TripID];
        }
    }
}