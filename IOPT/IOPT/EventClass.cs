/// Checked 2021-May
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IOPT
{
    public class EventClass
    {
        protected internal int ID { get; set; }
        protected internal int PathFromEventID { get; set; }  // ID of the from/to event    
        protected internal int PathToEventID { get; set; }
        protected internal int UpHeapEventID { get; set; }
        protected internal int DownHeapEventID { get; set; }
        protected internal int EearlyNonDomEventID { get; set; }
        protected internal int LateNonDomEventID { get; set; }
        protected internal double Time { get; set; }  // this refers the time enter the segment/link, the arrival time at head node should plus the seg traveltime
        protected internal double Cost { get; set; }
        protected internal int NodeID { get; set; }
        protected internal int SegID { get; set; }
        protected internal int TripID { get; set; }   // set the root(origin)/ of th event
        protected internal EventType Type { get; set; }
        protected internal HeapPosSet HeapPos { get; set; }
        protected internal void Initialization()
        {
            ID = PARA.NULLINT;
            PathFromEventID = PARA.NULLINT;
            PathToEventID = PARA.NULLINT;
            NodeID = PARA.NULLINT;
            SegID = PARA.NULLINT;
            UpHeapEventID = PARA.NULLINT;
            DownHeapEventID = PARA.NULLINT;
            EearlyNonDomEventID = PARA.NULLINT;
            LateNonDomEventID = PARA.NULLINT;
            Time = PARA.NULLDOUBLE;
            Cost = PARA.NULLDOUBLE;
            TripID = PARA.NULLINT;
            Type = EventType.IsNull;
            HeapPos = HeapPosSet.IsNull;
        }
        protected internal EventClass()
        {
            Initialization();
        }
        EventClass(int SetID, double SetTime, NodeClass Node)
        {
            Initialization();
            ID = SetID;
            Time = SetTime;
            NodeID = Node.ID;
        }
        #region HeapOperations
        protected internal static void CreatHeapHead(ref EventClass Obj)
        {
            Obj.HeapPos = HeapPosSet.Head;
            Obj.UpHeapEventID = PARA.NULLINT;
            Obj.DownHeapEventID = PARA.NULLINT;
        }
        /// <summary>
        /// put the new event in the heap to be scanned
        /// </summary>
        protected internal int EnHeap(int HeapHeadID, ref EventClass[] EventArray)
        {
            int NewHeapHead = PARA.NULLINT;
            //UpHeapEventID = PARA.NULLINT
            NewHeapHead = ID;
            HeapPos = HeapPosSet.Head;
            if (HeapHeadID == PARA.NULLINT)
            {
                // if the heap is empty. 
                DownHeapEventID = PARA.NULLINT;
            }
            else
            {
                // next heap id is the current heap id
                DownHeapEventID = EventArray[HeapHeadID].ID;
                if (EventArray[HeapHeadID].DownHeapEventID == PARA.NULLINT)
                {
                    EventArray[HeapHeadID].HeapPos = HeapPosSet.End;
                }
                else
                {
                    EventArray[HeapHeadID].HeapPos = HeapPosSet.Middle;
                }
            }
            NewHeapHead = ID;
            return NewHeapHead;
        }
        protected internal int DeHeap(ref EventClass[] EventsArray, out int NextEventID)
        {
            // remove the heap from the queue
            NextEventID = PARA.NULLINT;
            int NewHeapHead = PARA.NULLINT;
            switch (HeapPos)
            {
                case HeapPosSet.Head:
                    UpHeapEventID = PARA.NULLINT;
                    if (DownHeapEventID >= 0)
                    {
                        EventsArray[DownHeapEventID].HeapPos = HeapPosSet.Head;
                        EventsArray[DownHeapEventID].UpHeapEventID = PARA.NULLINT;
                        NewHeapHead = EventsArray[DownHeapEventID].ID;
                        NextEventID = NewHeapHead;
                    }
                    break;
                case HeapPosSet.Middle:
                    EventsArray[UpHeapEventID].DownHeapEventID = DownHeapEventID;
                    EventsArray[DownHeapEventID].UpHeapEventID = UpHeapEventID;
                    NextEventID = DownHeapEventID;
                    break;
                case HeapPosSet.End:
                    if (UpHeapEventID >= 0)
                    {
                        EventsArray[UpHeapEventID].DownHeapEventID = PARA.NULLINT;
                        EventsArray[UpHeapEventID].HeapPos = HeapPosSet.End;
                    }
                    NextEventID = PARA.NULLINT;
                    break;
                case HeapPosSet.IsNull:
                    MyLog.Instance.Debug("Invalid Heap Position in EventClass.cs");
                    break;
            }
            HeapPos = HeapPosSet.IsNull;
            return NewHeapHead;
        }
        #endregion #region HeapOperations

        /// <summary>
        ///  compare the event with the non dominated set associated with next element
        /// </summary>
        protected internal static bool CompareNonDomSet(EventClass TempEvent, ref EventClass[] EventArray,
            int StartEventID, ref List<NodeClass> Nodes, ref List<SegClass> Segs, int OriginID, int TripID)
        {
            int[] RemoveDomEvents = new int[PARA.MaxNumNonDomEvent];
            int CountRemoveEvents = -1;
            bool isCreate = true;
            bool IsRemoveComparedEvent;
            int NextEvent = StartEventID;
            while (NextEvent >= 0)
            {
                if (TempEvent.IsDomByEvent(EventArray[NextEvent], out IsRemoveComparedEvent))
                {
                    return false;
                }
                if (IsRemoveComparedEvent)
                {
                    CountRemoveEvents++;
                    Trace.Assert(CountRemoveEvents < PARA.MaxNumNonDomEvent, "Increase the value of PARA.MaxNumNonDomEvent");
                    RemoveDomEvents[CountRemoveEvents] = NextEvent;
                }
                NextEvent = EventArray[NextEvent].LateNonDomEventID;
            }
            if (isCreate)
            {
                TempEvent.ConvertTempToNewEvent();
                for (int i = 0; i <= CountRemoveEvents; i++)
                {
                    EventArray[RemoveDomEvents[i]].DeDomEvent(ref EventArray, ref Nodes, ref Segs, OriginID, TripID);
                }
                TempEvent.EnDomEvent(ref EventArray, ref Nodes, ref Segs, OriginID, TripID);
            }
            return isCreate;
        }

        public void ConvertTempToNewEvent() { Global.EventNumCount++; }
  
        protected internal bool IsSameElement(EventClass CompareEvent)
        {
            if (this.NodeID == CompareEvent.NodeID && this.SegID == CompareEvent.SegID)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void CompareHeapSet(ref int HeapHeadID, ref EventClass[] EventArray)
        {
            // Next Event refers to the next event associated with the next element and in the heap
            int NextEventID = EventArray[this.ID].DownHeapEventID;
            bool IsRemoveComparedEvent;
            while (NextEventID != PARA.NULLINT)
            {
                if (EventArray[NextEventID].IsSameElement(this))
                {
                    if (EventArray[NextEventID].IsDomByEvent(this, out IsRemoveComparedEvent))
                    {
                        HeapHeadID = EventArray[NextEventID].DeHeap(ref EventArray, out NextEventID);
                    }
                    NextEventID = EventArray[NextEventID].DownHeapEventID;
                }
                else
                    NextEventID = EventArray[NextEventID].DownHeapEventID;
            }
        }

        protected internal bool IsDomByEvent(EventClass CompareEvent, out bool IsRemoveComparedEvent)
        {
            /// comparedEvent is E1, (t1,c1)
            /// this event is E2, (t2,c2)
            bool isDominatedByComparedEvent;
            // compare event is the next event
            if (PARA.DesignPara.AssignMent.Equals(AssignMethod.SUE))
            {
                IsRemoveComparedEvent = false;
                isDominatedByComparedEvent = false;
                return isDominatedByComparedEvent;
            }

            if (CompareEvent.Time <= this.Time)  //  the new event arrival earlier t1<t2
            {
                IsRemoveComparedEvent = false;
                if (CompareEvent.Cost + (this.Time - CompareEvent.Time) * PARA.PathPara.WaitW + PARA.PathPara.BoundNonDomEventLower <= this.Cost)
                {
                    // c1 + (t2-t1)*w + s < = c2
                    isDominatedByComparedEvent = true; // c2 (this) is dominated by event t1
                }
                else
                {
                    isDominatedByComparedEvent = false;   // non dominated
                }
            }
            else   //else if (CompareEvent.Time > this.Time) t1>t2
            {
                if (this.Cost + (CompareEvent.Time - this.Time) * PARA.PathPara.WaitW + PARA.PathPara.BoundNonDomEventLower <= CompareEvent.Cost)
                {
                    // c2+(t1-t2)*w+ slack <=c1
                    // c1<c2
                    //IsRemoveComparedEvent = false;
                    IsRemoveComparedEvent = true;   // t1 is high and cost high so remove 1
                    isDominatedByComparedEvent = false;
                }
                else  // c1>c2
                {
                    IsRemoveComparedEvent = false;  // t1 is late, but c1 is less, so it is not dominate
                    isDominatedByComparedEvent = false;
                }
                if (this.Cost + (CompareEvent.Time - this.Time) * PARA.PathPara.WaitW - PARA.PathPara.BoundNonDomEventUpper > CompareEvent.Cost)
                { 
                    /// c2 +(t1-t2)*w -s > c1
                    IsRemoveComparedEvent = false;
                    isDominatedByComparedEvent = true;
                }
            }
            return isDominatedByComparedEvent;
        }
        /// <summary>
        /// Contains two comparison procedures 
        /// 1. compare with the event in the Non dominated set associated with next element
        /// 2. compare with the event in still in the heap, associated with the next element 
        /// </summary>
        protected internal bool IsCompareWin(int FirstDomEventID,
            ref int HeapHeadID, ref EventClass[] EventArray,
            ref List<NodeClass> Nodes, ref List<SegClass> Segs, int OriginID, int TripID)
        {
            if (CompareNonDomSet(this, ref EventArray, FirstDomEventID, ref Nodes, ref Segs, OriginID, TripID))
            {
                HeapHeadID = EnHeap(HeapHeadID, ref EventArray);
                CompareHeapSet(ref HeapHeadID, ref EventArray);
                return true;
            }
            return false;
        }

        protected internal void EnDomEvent(ref EventClass[] EventArray, ref List<NodeClass> Nodes, ref List<SegClass> Segs, int OriginID, int TripID)
        {
            // if the is the first event
            int StartID = -999;
            if (this.Type == EventType.Node) StartID = Nodes[this.NodeID].NonDomEventID[TripID];
            if (this.Type == EventType.Seg) StartID = Segs[this.SegID].NonDomEventID[TripID];

            Trace.Assert(StartID != -999, "Wraning_EventClass: Invalided start ID");

            if (StartID == PARA.NULLINT)
            {
                this.EearlyNonDomEventID = PARA.NULLINT;
                this.LateNonDomEventID = PARA.NULLINT;
                if (this.Type == EventType.Node)
                {
                    Nodes[this.NodeID].NonDomEventID[TripID] = this.ID;
                }
                else if (this.Type == EventType.Seg)
                {
                    Segs[this.SegID].NonDomEventID[TripID] = this.ID;
                }
            }
            else
            {
                // if it is not the first event
                bool IsDone = false;
                int NowID = StartID;
                int NextID;
                if (EventArray[StartID].Time > this.Time)
                {
                    // if early than the first event
                    this.EearlyNonDomEventID = PARA.NULLINT;
                    this.LateNonDomEventID = StartID;
                    EventArray[StartID].EearlyNonDomEventID = this.ID;
                    if (this.Type == EventType.Node) Nodes[this.NodeID].NonDomEventID[TripID] = this.ID;
                    if (this.Type == EventType.Seg) Segs[this.SegID].NonDomEventID[TripID] = this.ID;
                }
                else
                {
                    while (IsDone == false)
                    {
                        NextID = EventArray[NowID].LateNonDomEventID;
                        if (NextID == PARA.NULLINT)
                        {
                            // late than the last event do not need to check the time
                            this.LateNonDomEventID = PARA.NULLINT;
                            this.EearlyNonDomEventID = NowID;
                            // adjust the previous event pointer
                            EventArray[NowID].LateNonDomEventID = this.ID;
                            IsDone = true;
                        }
                        else
                        {
                            if (this.Time <= EventArray[NextID].Time
                                && this.Time >= EventArray[NowID].Time)
                            {
                                // in the middle
                                this.EearlyNonDomEventID = NowID;
                                this.LateNonDomEventID = EventArray[NowID].LateNonDomEventID;
                                // adjust the pre event and late event pointer
                                EventArray[NowID].LateNonDomEventID = this.ID;
                                EventArray[EventArray[NowID].LateNonDomEventID].EearlyNonDomEventID = this.ID;
                                IsDone = true;
                            }
                            else
                            {
                                NowID = NextID;
                            }
                        }
                    }
                }
            }
        }

        protected internal void DeDomEvent(ref EventClass[] EventArray, ref List<NodeClass> Nodes, ref List<SegClass> Segs, int OriginID,
            int TripID)
        {
            // check dominate event is not empty
            if (this.Type == EventType.Node)
            {
                Debug.Assert(this.NodeID != PARA.NULLINT);
                Debug.Assert(Nodes[this.NodeID].NonDomEventID[TripID] != PARA.NULLINT);
            }
            else if (this.Type == EventType.Seg)
            {
                Debug.Assert(this.SegID != PARA.NULLINT);
                Debug.Assert(Segs[this.SegID].NonDomEventID[TripID] != PARA.NULLINT);
            }
            // Remove an event from the dominated set
            if (this.EearlyNonDomEventID == PARA.NULLINT && this.LateNonDomEventID == PARA.NULLINT)
            {
                // if this is the only event, remove it 
                if (this.Type == EventType.Node)
                {
                    Nodes[this.NodeID].NonDomEventID[TripID] = PARA.NULLINT;
                }
                else if (this.Type == EventType.Seg)
                {
                    Segs[this.SegID].NonDomEventID[TripID] = PARA.NULLINT;
                }
            }
            else
            {
                if (this.EearlyNonDomEventID == PARA.NULLINT)
                {
                    // If it is the first event,then reset the non dominated event ID
                    if (this.Type == EventType.Node)
                    {
                        Nodes[this.NodeID].NonDomEventID[TripID] = this.LateNonDomEventID;
                    }
                    else if (this.Type == EventType.Seg)
                    {
                        Segs[this.SegID].NonDomEventID[TripID] = this.LateNonDomEventID;
                    }

                    EventArray[this.LateNonDomEventID].EearlyNonDomEventID = PARA.NULLINT;
                }
                else if (this.LateNonDomEventID == PARA.NULLINT)
                {
                    // last event 
                    EventArray[this.EearlyNonDomEventID].LateNonDomEventID = PARA.NULLINT;
                }
                else
                {
                    // middle event
                    EventArray[this.EearlyNonDomEventID].LateNonDomEventID = this.LateNonDomEventID;
                    EventArray[this.LateNonDomEventID].EearlyNonDomEventID = this.EearlyNonDomEventID;
                }
            }

            this.EearlyNonDomEventID = PARA.NULLINT;
            this.LateNonDomEventID = PARA.NULLINT;
        }

        // print event on the screen
        public void PrintEvent(List<SegClass> Segs, bool isNewEvent, bool InFile, bool InScreen)
        {
            if (Type == EventType.Node)
            {
                if (isNewEvent)
                {
                    if (InFile)
                        PARA.PrintEventLog.WriteLine("New: ID: {0}, AtNode: {1}, AtTime: {2}, Cost:{3}", ID, NodeID, Time, Cost);
                    if (InScreen)
                        Console.WriteLine("New: ID: {0}, AtNode: {1}, AtTime: {2}, Cost:{3}", ID, NodeID, Time, Cost);
                }
                else
                {
                    if (InFile)
                        PARA.PrintEventLog.WriteLine("Old: ID: {0}, AtNode: {1}, AtTime: {2}, Cost:{3}", ID, NodeID, Time, Cost);
                    if (InScreen)
                        Console.WriteLine("Old: ID: {0}, AtNode: {1}, AtTime: {2}, Cost:{3}", ID, NodeID, Time, Cost);
                }

            }
            else
            {
                if (isNewEvent)
                {
                    if (InFile)
                        PARA.PrintEventLog.WriteLine("New: ID: {0}, AtSeg: {1}, AtTime: {2}, Cost:{3}, Tail: {4}, Head: {5}, Line: {6}", ID, SegID, Time, Cost, Segs[SegID].Tail.ID, Segs[SegID].Head.ID, Segs[SegID].MapLine[0].ID);
                    if (InScreen)
                        Console.WriteLine("New: ID: {0}, AtSeg: {1}, AtTime: {2}, Cost:{3}, Tail: {4}, Head: {5}, Line: {6}", ID, SegID, Time, Cost, Segs[SegID].Tail.ID, Segs[SegID].Head.ID, Segs[SegID].MapLine[0].ID);
                }
                else
                {
                    if (InFile)
                        PARA.PrintEventLog.WriteLine("Old: {0}, AtSeg: {1}, AtTime: {2}, Cost:{3}, Tail: {4}, Head: {5}, Line: {6}", ID, SegID, Time, Cost, Segs[SegID].Tail.ID, Segs[SegID].Head.ID, Segs[SegID].MapLine[0].ID);
                    if (InScreen)
                        Console.WriteLine("Old: {0}, AtSeg: {1}, AtTime: {2}, Cost:{3}, Tail: {4}, Head: {5}, Line: {6}", ID, SegID, Time, Cost, Segs[SegID].Tail.ID, Segs[SegID].Head.ID, Segs[SegID].MapLine[0].ID);
                }

            }
        }
  
    }// event class

}
