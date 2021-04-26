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
#if DEBUG
                Console.WriteLine("Start Event Search for OD pair = {0}", i);
#endif
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
        /// <param name="Origin"></param>
        /// <param name="_TripID"></param>
        /// <returns></returns>
        /// <remarks>
        /// In the revised version 2021 Feb. I added the dwell cost to the node
        /// which is computed at the same time as the capacity cost
        /// </remarks>
        protected internal void LeastCost(UniqueOrigin Origin, int _TripID)
        {
            //PARA.PrintEventPathOnScreen = true;
            ///<remarks>
            ///To debug, I can print the path info on the screen
            ///</remarks>
            //if (Origin.OriginID == 1 )
            //{
            //    PARA.PrintEventPathOnScreen = true;
            //}
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
                            ///<remarks>
                            ///it is not an origin node, then it is a transfer node
                            ///because at this node passenger need to board another node
                            ///</remarks>
                            if (CurrentNodeID != Origin.OriginID&&isToBoardaNewLine)
                            {
                                IsTransfer = true;
                            }
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
                                // Revise check: the following condition actually contains the case for the frequency-based lines services. 
                                // it seems I did it correct in the first version
                                if (!(TrainIndex == -1 && Nodes[CurrentNodeID].OutSegs[s].MapLine[0].ServiceType == TransitServiceType.Schedule))
                                {
                                    WaitTime = tp - CurrentTime;

                                    //if (Nodes[CurrentNodeID].ID == 1 && TrainIndex == 0) Console.WriteLine("wtf");
                                    //only add congestion cost when board at the node or transfer to another line
                                    cp = Events[HeapHeadID].Cost + AddCpCostValue(VehType, 0.0d, WaitTime, IsTransfer)
                                                + Nodes[CurrentNodeID].OutSegs[s].getCapCost(tp, TrainIndex)*PARA.PathPara.ConW
                                                + Nodes[CurrentNodeID].OutSegs[s].getDwellCost(tp, TrainIndex)*PARA.PathPara.WaitW;
#if DEBUG
                                    //Console.WriteLine("L={0},Node={1},Train={2},Dwell={3}",
                                    //    LineID, Nodes[CurrentNodeID].ID, TrainIndex, Nodes[CurrentNodeID].OutSegs[s].getDwellCost(tp, TrainIndex));
#endif
                                    //EventClass.CreatTempEvent(tp, cp, Events[HeapHeadID].ID, EventType.Seg, PARA.NULLINT, NextSegID, ref Events);
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
                        // if current event is at segment
                        // first check the continuous segment : ie. so that passenger does not make a transfer 
                        CurrentTime = Events[HeapHeadID].Time;
                        CurrentSegID = Events[HeapHeadID].SegID;
                        NextSegID = Segs[CurrentSegID].NextSegID;
                        NextNodeID = Segs[Events[HeapHeadID].SegID].Head.ID;
                        LineID = Segs[CurrentSegID].MapLine[0].ID;

                        // step 2: check the event associated with alight node : transfer to another line, so in the next arrival function isBoard is set to be fast
                        // this represent alight at the head node, in such case, the waiting time is 0.0, no transfer cost, since it is added to the boarding event
                        IsTransfer = false;  // transfer cost is only added from node to seg, to avoid duplicated compute
                        // tp here seems to be the arrival time at the next node
                        tp = TransitLineClass.NextArrival
                            (CurrentTime + Segs[CurrentSegID].TravelTime, Nodes[NextNodeID], LineID, Lines, 
                                out TrainIndex, isToBoardaNewLine: false, ConsiderMinTransferGap: false);
                        //Console.WriteLine("tp time = {0}", tp);

                        if (tp < PARA.DesignPara.MaxTimeHorizon)
                        {
                            // no cap congestion cost due to the node
                            cp = Events[HeapHeadID].Cost + AddCpCostValue(VehType, Segs[CurrentSegID].TravelTime, 0.0d, IsTransfer);
                            //+ _Segs[NextSegID]._CapCost(tp,TrainIndex);
                            CreatTempEvent(tp, cp, Events[HeapHeadID].ID, EventType.Node, NextNodeID, PARA.NULLINT);
                            FirstNonDomEventInNextElementSet = Nodes[NextNodeID].NonDomEventID[_TripID];
                            if (Events[Global.EventNumCount].IsCompareWin(FirstNonDomEventInNextElementSet, ref NewHeapHead, ref Events, ref Nodes, ref Segs, Origin.OriginID, _TripID))
                            {
                                Events[Global.EventNumCount - 1].PrintEvent(Segs, isNewEvent: true, InFile: true, InScreen: PARA.PrintEventPathOnScreen);
                            }
                        }
                        break;
                }
                // finish switch cases
                HeapHeadID = NewHeapHead;
            }
            Trips[_TripID].FirstNonDomEventID = Nodes[Trips[_TripID].DestID].NonDomEventID[_TripID];
        }
    }
}

#region continousline
// the following part will ignore the continuous node
///<remarks>
/// first version:The following part should not be deleted
/// revise: 2021 Feb: I think the continous line is considered at the node
///</remarks>
//if (NextSegID != PARA.NULLINT)  // only check if next segment and current segment belong to a same transit line
//{
//    FirstNonDomEventInNextElementSet = Segs[NextSegID].NonDomEventID[_TripID];
//    IsTransfer = false;   // using the same line board to the other segment 
//    VehType = Lines[LineID].VehicleType;
//    tp = TransitLineClass.NextArrival(CurrentTime + Segs[CurrentSegID].TravelTime, Nodes[NextNodeID], LineID, Lines, out TrainIndex, isBoard: true,
//        ConsiderMinTransferGap: false);
//    if (tp < PARA.DesignPara.MaxTimeHorizon)
//    {
//        cp = Events[HeapHeadID].Cost + _AddCpCostValue(VehType, Segs[CurrentSegID].TravelTime, 0.0d, IsTransfer)
//            + Segs[NextSegID]._CapCost(tp, TrainIndex);
//        EventClass.CreatTempEvent(tp, cp, HeapHeadID, EventType.Seg, PARA.NULLINT, NextSegID, ref Events);
//        if (Events[Global.EventNumCount].IsCompareWin(FirstNonDomEventInNextElementSet, ref NewHeapHead, ref Events, ref Nodes, ref Segs, Origin.OriginID, _TripID))
//        {
//            Events[Global.EventNumCount - 1].PrintEvent(Segs, isNewEvent: true, InFile: true, InScreen: PARA.PrintEventPathOnScreen);
//        }
//    }
//}
    #endregion continousline


