// Checked 2021-May
using System;
using System.Collections.Generic;

namespace IOPT
{
    public class OrignClass
    {
        protected internal int OriginID;
        protected internal double TargetDepTime;
        protected internal double DepMaxEarly;
        protected internal double DepMaxLate;
        public OrignClass()
        {
            OriginID = PARA.NULLINT; TargetDepTime = PARA.NULLDOUBLE;
            DepMaxEarly = PARA.NULLDOUBLE; DepMaxLate = PARA.NULLDOUBLE;
        }
        public OrignClass(int ID, double SetTargetDepTime, double SetDepMaxEarly, double SetDepMaxLate)
        {
            OriginID = ID; TargetDepTime = SetTargetDepTime;
            DepMaxEarly = SetDepMaxEarly; DepMaxLate = SetDepMaxLate;
        }
    }

    public class TripClass
    {
        protected internal int ID { get; private set; }
        protected internal int OriginID { get; set; }
        protected internal int DestID { get; set; }
        protected internal double TargetDepTime { get; set; }
        protected internal double TargetArrTime { get; set; }
        protected internal double DepMaxEarly { get; set; }
        protected internal double DepMaxLate { get; set; }
        protected internal double ArrMaxEarly { get; set; }
        protected internal double ArrMaxLate { get; set; }
        protected internal int FirstNonDomEventID { get; set; }
        protected internal double Demand { get; set; }
        protected internal double MinPie { get; set; } 
        protected internal double MaxPie { get; set; }
        private void ini()
        {
            ID = PARA.NULLINT; OriginID = PARA.NULLINT;
            TargetArrTime = PARA.NULLDOUBLE; TargetDepTime = PARA.NULLDOUBLE;
            DepMaxEarly = PARA.NULLDOUBLE; DepMaxLate = PARA.NULLDOUBLE;
            ArrMaxEarly = PARA.NULLDOUBLE; ArrMaxLate = PARA.NULLDOUBLE;
            FirstNonDomEventID = PARA.NULLINT; Demand = PARA.NULLDOUBLE;
        }
        public TripClass() { ini(); }
        public TripClass(int SetID, int SetOrigin, int SetDest, double SetTarDep, 
            double SetTarArr,double SetDepMaxEar, double SetDepMaxlate, 
            double SetArrMaxEar, double SetArrMaxLate, double SetMinPie, double SetMaxPie)
        {
            ini();
            ID = SetID; OriginID = SetOrigin; DestID = SetDest;
            TargetDepTime = SetTarDep; TargetArrTime = SetTarArr;
            DepMaxEarly = SetDepMaxEar; DepMaxLate = SetDepMaxlate;
            ArrMaxEarly = SetArrMaxEar; ArrMaxLate = SetArrMaxLate;
            MinPie = SetMinPie; MaxPie = SetMaxPie;
        }
        protected internal void PrintPath(EventClass[] EventArray, List<SegClass> Segs)
        {
            // print the event path for the OD pair considered
            string FileName;
            FileName = MyFileNames.OutPutFolder+ "EventPath.txt";

            int NonDomEventCount = 0;
            int NowDomEventPath = FirstNonDomEventID;
            
            int NowElement, PreElement;
            using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(FileName,true))
            {
                do
                {
                    if (NowDomEventPath < 0)
                    {
                        Console.WriteLine("NonDomEvnet = {0}", NowDomEventPath);
                        Console.WriteLine("Warning_TripClass: probably need to increase modeling horizon");
                        //Console.ReadLine();
                    }
                    file.Write("Iter={0},OD={1},PathNo={2},Time={3},Cost={4}:",
                        Global.NumOfIter,ID, NonDomEventCount, EventArray[NowDomEventPath].Time,
                        EventArray[NowDomEventPath].Cost );
                    NowElement = NowDomEventPath;
                    PreElement = EventArray[NowElement].PathFromEventID;
                    while (PreElement != PARA.NULLINT)
                    {
                        if (EventArray[NowElement].Type==EventType.Node)
                        {
                            file.Write(EventArray[NowElement].NodeID);
                        }
                        else
                        {
                            file.Write("[");
                            file.Write("Line");
                            file.Write("(");
                            file.Write(Segs[EventArray[NowElement].SegID].MapLine[0].ID);
                            file.Write(")");
                            file.Write("]");
                        }
                        file.Write("<-");
                        NowElement = PreElement;
                        PreElement = EventArray[NowElement].PathFromEventID;
                    }
                    file.Write(OriginID);
                    file.Write(Environment.NewLine);
                    file.Flush();
                    NonDomEventCount++;
                    NowDomEventPath = EventArray[NowDomEventPath].LateNonDomEventID;
                } while (NowDomEventPath != PARA.NULLINT);
            }
        }
    }

    public class UniqueOrigin
    {
        // group the trips that has the same origin node
        protected internal int FirtNonDomEventID { get; set; }
        protected internal int OriginID { get; set; }
        protected internal double TargetDepTime { get; set; }
        protected internal List<int> IncludeTrips { get; set; }// map the origin information to the OD pair

        protected internal UniqueOrigin(OrignClass origin)
        {
            OriginID = origin.OriginID; TargetDepTime = origin.TargetDepTime;
            FirtNonDomEventID = PARA.NULLINT;
            IncludeTrips = new List<int>();
        }
    }
}