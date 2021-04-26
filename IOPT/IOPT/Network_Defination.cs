// checked 2021-May
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Data;
namespace IOPT
{
    public partial class NetworkClass
    {
        protected internal List<NodeClass> Nodes;
        protected internal List<TransitLineClass> Lines;
        protected internal List<SegClass> Segs;
        protected internal List<UniqueOrigin> UniOrigins;
        protected internal List<TripClass> Trips;
        public EventClass[] Events;
        protected internal NetworkClass()
        {
            Nodes = new List<NodeClass>();
            Lines = new List<TransitLineClass>();
            Segs = new List<SegClass>();
            Trips = new List<TripClass>();
            UniOrigins = new List<UniqueOrigin>();
            Events = new EventClass[PARA.MaxNumOfEvents];
            for (int i = 0; i < PARA.MaxNumOfEvents; i++) Events[i] = new EventClass();
            ReadNodes(); Console.WriteLine("Info_NetworkDefine: Node file has been read");
            ReadLines(); Console.WriteLine("Info_NetworkDefine: Line file has been read");
            ReadLineStop(); Console.WriteLine("Info_NetworkDefine: LineStop file has been read");
            ReadTrips(); Console.WriteLine("Info_NetworkDefine: Trip file has been read");
            CreateDataStructure();
        }
        protected internal void IniSegsList()
        {
            int NumSeg = 0;
            for (int l = 0; l < Lines.Count; l++)
            {
                int SegCount = 0;
                for (int s = 0; s < Lines[l].Stops.Count - 1; s++)
                {
                    Segs.Add(new SegClass());
                    Segs[NumSeg].ID = NumSeg;
                    Segs[NumSeg].Tail = Lines[l].Stops[s];
                    Segs[NumSeg].Head = Lines[l].Stops[s + 1];
                    Segs[NumSeg].TravelTime = Lines[l].SegTravelTimes[SegCount];
                    Segs[NumSeg].MapLine.Add(Lines[l]);
                    Lines[l].MapSegs.Add(Segs[NumSeg]);

                    if (Lines[l].ServiceType == TransitServiceType.Frequency)
                    {
                        Segs[NumSeg].OnBoardFlow = new List<double>(new double[PARA.IntervalSets.Count]);
                        Segs[NumSeg].CapCost = new List<double>(new double[PARA.IntervalSets.Count]);
                        Segs[NumSeg].BoardingFlow = new List<double>(new double[PARA.IntervalSets.Count]);
                        Segs[NumSeg].DwellCost = new List<double>(new double[PARA.IntervalSets.Count]);
                    }
                    if (Lines[l].ServiceType == TransitServiceType.Schedule)
                    {
                        Segs[NumSeg].OnBoardFlow = new List<double>(new double[Lines[l].NumOfTrains]);
                        Segs[NumSeg].CapCost = new List<double>(new double[Lines[l].NumOfTrains]);
                        Segs[NumSeg].BoardingFlow = new List<double>(new double[Lines[l].NumOfTrains]);
                        Segs[NumSeg].DwellCost = new List<double>(new double[Lines[l].NumOfTrains]);
                    }
                    SegCount++;
                    NumSeg++;
                }
            }
        }
        protected internal void IniNextSegID()
        {
            for (int i = 0; i < Segs.Count; i++)
            {
                for (int s = 0; s < Nodes[Segs[i].Head.ID].OutSegs.Count; s++)
                {
                    if (Segs[i].MapLine[0].ID == Nodes[Segs[i].Head.ID].OutSegs[s].MapLine[0].ID)
                    {
                        Segs[i].NextSegID = Nodes[Segs[i].Head.ID].OutSegs[s].ID;
                        break;
                    }
                }
            }
        }
        protected internal void CreateUniqueOriginSet()
        {
            List<OrignClass> OriginSet = new List<OrignClass>();
            for (int i = 0; i < Trips.Count; i++)
            {
                OriginSet.Add(new OrignClass(Trips[i].OriginID, Trips[i].TargetDepTime, Trips[i].DepMaxEarly, Trips[i].DepMaxLate));
            }
            List<OrignClass> DistinctOris = new List<OrignClass>();
            for (int i = 0; i < OriginSet.Count(); i++)
            {
                bool IsInsert = true;
                for (int j = 0; j < DistinctOris.Count(); j++)
                {
                    if (DistinctOris[j].OriginID == OriginSet[i].OriginID)
                    {
                        IsInsert = false;
                    }
                }
                if (IsInsert) DistinctOris.Add(OriginSet[i]);
            }
            for (int i = 0; i < DistinctOris.Count(); i++)
            {
                UniOrigins.Add(new UniqueOrigin(DistinctOris[i]));
            }
            for (int i = 0; i < UniOrigins.Count; i++)
            {
                for (int j = 0; j < Trips.Count; j++)
                {
                    if (Trips[j].OriginID == UniOrigins[i].OriginID)
                    {
                        UniOrigins[i].IncludeTrips.Add(j);
                    }
                }
            }
        }
        protected internal void CreateDataStructure()
        {
            IniSegsList();
            SetInOutSegsAndLines();
            IniNextSegID();
            for (int l = 0; l < Lines.Count; l++) Lines[l].IniSchedule(ref Nodes);
            CreateUniqueOriginSet();
            PrintNetwork();
        }
        protected internal void InitNonDomEventID()
        {
            foreach (NodeClass n in Nodes)
            {
                n.IniNonDomEventID(Trips.Count());
            }
            foreach (SegClass s in Segs)
            {
                s.IniNonDomEventID(Trips.Count());
            }
        }
        protected internal void InitFlow()
        {
            foreach (NodeClass n in Nodes)
            {
                foreach (var key in n.m_LineID_Flow.Keys)
                {
                    for (int i = 0; i < n.m_LineID_Flow[key].Count; i++)
                    {
                        n.m_LineID_Flow[key][i].Ini();
                    }
                }
            }
            foreach (SegClass s in Segs)
            {
                for (int i = 0; i < s.OnBoardFlow.Count; i++) s.OnBoardFlow[i] = 0;
                for (int i = 0; i < s.CapCost.Count; i++) s.CapCost[i] = 0;
            }
        }
        protected internal void CleanEventArry()
        {
            for (int i = 0; i < Events.Count(); i++)
                Events[i].Initialization();
            Global.EventNumCount = 0;
        }
        protected internal void SetInOutSegsAndLines()
        {
            // step 1 create incoming and outgoing segs
            for (int i = 0; i < Segs.Count; i++)
            {
                Nodes[Segs[i].Tail.ID].OutSegs.Add(Segs[i]);
                Nodes[Segs[i].Head.ID].InSegs.Add(Segs[i]);
                for (int j = 0; j < Segs[i].MapLine.Count; j++)
                {
                    Nodes[Segs[i].Tail.ID].OutLines.Add(Segs[i].MapLine[j]);
                    Nodes[Segs[i].Head.ID].InLines.Add(Segs[i].MapLine[j]);
                }
            }
            int _lineID = -1;
            for (int i = 0; i < Nodes.Count; i++)
            {
                for (int s = 0; s < Nodes[i].OutSegs.Count; s++)
                {
                    _lineID = Nodes[i].OutSegs[s].MapLine[0].ID;
                    if (Nodes[i].m_LineID_Flow.ContainsKey(_lineID))
                    {
                        continue;
                    }
                    else
                    {
                        Nodes[i].m_LineID_Flow.Add(_lineID, new List<FlowClass>());
                        if (Nodes[i].OutSegs[s].MapLine[0].ServiceType == TransitServiceType.Frequency)
                        {
                            for (int j = 0; j < PARA.IntervalSets.Count; j++)
                            {
                                Nodes[i].m_LineID_Flow[_lineID].Add(new FlowClass(_lineID));
                            }
                        }
                        if (Nodes[i].OutSegs[s].MapLine[0].ServiceType == TransitServiceType.Schedule)
                        {
                            for (int q = 0; q < Nodes[i].OutSegs[s].MapLine[0].NumOfTrains; q++)
                            {
                                Nodes[i].m_LineID_Flow[_lineID].Add(new FlowClass(_lineID));
                            }
                        }
                    }
                }
                for (int s = 0; s < Nodes[i].InSegs.Count; s++)
                {
                    _lineID = Nodes[i].InSegs[s].MapLine[0].ID;
                    if (Nodes[i].m_LineID_Flow.ContainsKey(_lineID))
                    {
                        continue;
                    }
                    else
                    {
                        Nodes[i].m_LineID_Flow.Add(_lineID, new List<FlowClass>());
                        if (Nodes[i].InSegs[s].MapLine[0].ServiceType == TransitServiceType.Frequency)
                        {
                            for (int j = 0; j < PARA.IntervalSets.Count; j++)
                            {
                                Nodes[i].m_LineID_Flow[_lineID].Add(new FlowClass(_lineID));
                            }
                        }
                        if (Nodes[i].InSegs[s].MapLine[0].ServiceType == TransitServiceType.Schedule)
                        {
                            for (int q = 0; q < Nodes[i].InSegs[s].MapLine[0].NumOfTrains; q++)
                            {
                                Nodes[i].m_LineID_Flow[_lineID].Add(new FlowClass(_lineID));
                            }
                        }
                    }
                }
            }
        }

        protected internal void CreateNewEvent(int tp, double cp, int FromEventID)
        {
            Events[Global.EventNumCount].Time = tp;
            Events[Global.EventNumCount].Cost = cp;
            Events[Global.EventNumCount].ID = Global.EventNumCount;
            Events[Global.EventNumCount].PathFromEventID = FromEventID;
            Events[FromEventID].PathToEventID = Global.EventNumCount;
            Global.EventNumCount++;
        }
        protected internal void CreateIniEvent(ref UniqueOrigin Origin, int SetTripID)
        {
            Events[Global.EventNumCount].NodeID = Origin.OriginID;
            Events[Global.EventNumCount].ID = Global.EventNumCount;
            Events[Global.EventNumCount].Cost = 0.0d;
            Events[Global.EventNumCount].Time = Trips[SetTripID].TargetDepTime;
            Events[Global.EventNumCount].Type = EventType.Node;
            Events[Global.EventNumCount].HeapPos = HeapPosSet.Head;
            Events[Global.EventNumCount].TripID = SetTripID;
            Events[Global.EventNumCount].PathFromEventID = PARA.NULLINT;
            Events[Global.EventNumCount].PathToEventID = PARA.NULLINT;
            Origin.FirtNonDomEventID = Global.EventNumCount;
            Global.EventNumCount++;
        }

        protected internal void CreatTempEvent(double Time, double Cost, int FromEventID, EventType Type, int NodeID, int SegID)
        {
            Events[Global.EventNumCount].ID = Global.EventNumCount;
            Events[Global.EventNumCount].PathFromEventID = FromEventID;
            Events[Global.EventNumCount].Time = Time;
            Events[Global.EventNumCount].Cost = Cost;
            Events[Global.EventNumCount].Type = Type;
            Events[Global.EventNumCount].NodeID = NodeID;
            Events[Global.EventNumCount].SegID = SegID;
            Events[Global.EventNumCount].HeapPos = HeapPosSet.Head;
        }
        protected internal bool CompareNonDomSet(EventClass TempEvent, int StartEventID, int OriginID, int TripID)
        {
            int[] RemoveDomEvents = new int[PARA.MaxNumNonDomEvent];
            int CountRemoveEvents = -1;
            bool isCreate = true;
            bool IsRemoveComparedEvent = false;
            int NextEvent = StartEventID;
            while (NextEvent >= 0)
            {
                if (TempEvent.IsDomByEvent(Events[NextEvent], out IsRemoveComparedEvent))
                {
                    isCreate = false;
                    return false;
                }
                if (IsRemoveComparedEvent)
                {
                    CountRemoveEvents++;
                    Trace.Assert(CountRemoveEvents < PARA.MaxNumNonDomEvent, "Increase the value of PARA.MaxNumNonDomEvent");
                    RemoveDomEvents[CountRemoveEvents] = NextEvent;
                }
                NextEvent = Events[NextEvent].LateNonDomEventID;
            }
            if (isCreate)
            {
                TempEvent.ConvertTempToNewEvent();
                for (int i = 0; i <= CountRemoveEvents; i++)
                {
                    Events[RemoveDomEvents[i]].DeDomEvent(ref Events, ref Nodes, ref Segs, OriginID, TripID);
                }
                TempEvent.EnDomEvent(ref Events, ref Nodes, ref Segs, OriginID, TripID);
            }
            return isCreate;
        }

        protected internal void PrintAllNodes()
        {
            string FileName;
            FileName = MyFileNames.OutPutFolder + "Nodes.txt";
            using (StreamWriter file =
                 new StreamWriter(@FileName))
            {
                file.WriteLine("ID, Name,OutSeg,InSeg");
                for (int i = 0; i < Nodes.Count; i++)
                {
                    file.Write(Nodes[i].ID);
                    file.Write(";");
                    file.Write(Nodes[i].Name);
                    file.Write(";");
                    file.Write("(");
                    for (int s = 0; s < Nodes[i].OutSegs.Count; s++)
                    {
                        file.Write(Nodes[i].OutSegs[s].ID);
                        file.Write(",");
                    }
                    file.Write(");");
                    file.Write("(");
                    for (int s = 0; s < Nodes[i].InSegs.Count; s++)
                    {
                        file.Write(Nodes[i].InSegs[s].ID);
                        file.Write(",");
                    }
                    file.Write(")");
                    file.Write(Environment.NewLine);
                }
            }
        }
        protected internal void PrintSegs()
        {
            string FileName;
            FileName = MyFileNames.OutPutFolder + "Segs.txt";
            using (System.IO.StreamWriter file =
                 new System.IO.StreamWriter(FileName))
            {
                file.WriteLine("ID,Tail,Head,Time,Line,Next,NonDom");
                for (int i = 0; i < Segs.Count; i++)
                {
                    file.WriteLine("{0},{1},{2},{3},{4},{5},{6}",
                        Segs[i].ID, Segs[i].Tail.ID, Segs[i].Head.ID, Segs[i].TravelTime, Segs[i].MapLine[0].ID, Segs[i].NextSegID, Segs[i].NonDomEventID);
                }
            }
        }
        protected internal void PrintLineStops()
        {
            string FileName;

            FileName = MyFileNames.OutPutFolder + "Lines.txt";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@FileName))
            {
                file.WriteLine("ID,Name,SerType,HeadWay,StartTim,EndTime,NumOfRun,NumSegs,VehType,Stops,Doors");
                for (int i = 0; i < Lines.Count; i++)
                {
                    file.Write(Lines[i].ID); file.Write(",");
                    file.Write(Lines[i].Name); file.Write(",");
                    file.Write(Lines[i].ServiceType); file.Write(",");
                    file.Write(Lines[i].Headway); file.Write(",");
                    file.Write(Lines[i].StartOperationTime); file.Write(",");
                    file.Write(Lines[i].EndOperationTime); file.Write(",");
                    file.Write(Lines[i].NumOfTrains); file.Write(",");
                    file.Write(Lines[i].NumOfSegs); file.Write(",");
                    file.Write(Lines[i].VehicleType); file.Write(",");
                    for (int s = 0; s < Lines[i].Stops.Count; s++)
                    {
                        file.Write(Lines[i].Stops[s].ID); file.Write(",");
                    }
                    file.Write(Lines[i].NumOfDoors);
                    file.Write(Environment.NewLine);
                }
            }
        }
        protected internal void PrintScheduleTable()
        {
            DataTable Table = new DataTable("TransitLineTable");
            DataColumn column;
            DataRow row;
            column = new DataColumn();
            column.DataType = System.Type.GetType("System.Int32");
            column.ColumnName = "ID";
            Table.Columns.Add(column);
            Table.Columns.Add("Name", System.Type.GetType("System.String"));

            for (int i = 0; i < Lines.Count; i++)
            {
                row = Table.NewRow();
                row["ID"] = Lines[i].ID;
                row["Name"] = Lines[i].Name;
                Table.Rows.Add(row);
            }
            string FileName;

            FileName = MyFileNames.OutPutFolder + "InputSchedule.txt";

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@FileName))
            {
                foreach (DataRow dataRow in Table.Rows)
                {
                    foreach (var item in dataRow.ItemArray)
                    {
                        file.Write(item + ",");
                    }
                    file.Write(Environment.NewLine);
                }
            }
        }
        protected internal void PrintTrips()
        {
            string FileName;
            FileName = MyFileNames.OutPutFolder + "Trips.txt";
            using (StreamWriter file =
                  new StreamWriter(@FileName))
            {
                file.WriteLine("ID,Origin,Dest,TarDep,TarArr,DepMaxEarly,DepMaxLate,ArrMaxEarly,ArrMaxLate,Demand");
                for (int i = 0; i < Trips.Count; i++)
                {
                    file.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                    Trips[i].ID, Trips[i].OriginID, Trips[i].DestID, Trips[i].TargetDepTime, Trips[i].TargetArrTime,
                    Trips[i].DepMaxEarly, Trips[i].DepMaxLate, Trips[i].ArrMaxEarly, Trips[i].ArrMaxLate, Trips[i].Demand);
                }
            }
        }

        protected internal void PrintNetwork()
        {
            PrintTrips();
            PrintScheduleTable();
            PrintLineStops();
            PrintAllNodes();
        }

        protected internal void PrintAllEvent()
        {
            string FileName = MyFileNames.OutPutFolder + "Event_All.txt";
            using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(FileName, true))
            {
                if (Global.NumOfIter == 0 && Global.BBSolNum == -1)
                    file.WriteLine("Iter,EventID,Path_from,Path_to,Time,Cost,NodeId,SegId,TripId,Early_Dom,Late_Dom");
                for (int i = 0; i < Events.Count(); i++)
                {
                    if (Events[i].ID != PARA.NULLINT)
                    {
                        file.Write("{0},", Global.NumOfIter);
                        file.Write("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", Events[i].ID, Events[i].PathFromEventID, Events[i].PathToEventID, Events[i].Time,
                            Events[i].Cost, Events[i].NodeID, Events[i].SegID, Events[i].TripID, Events[i].EearlyNonDomEventID, Events[i].LateNonDomEventID);
                        file.Write(Environment.NewLine);
                    }
                }
            }
        }
    }
}

