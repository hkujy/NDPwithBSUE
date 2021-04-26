using System;
using System.Collections.Generic;
using System.Linq;

namespace IOPT
{
    public class SegClass
    {
        // TailNode -----> HeadNode
        protected internal int ID { get; set; }
        protected internal int NextSegID { get; set; }
        protected internal int[] NonDomEventID { get; set; }
        protected internal double TravelTime { get; set; }
        protected internal NodeClass Tail { get; set; }
        protected internal NodeClass Head { get; set; }
        protected internal List<double> OnBoardFlow { get; set; }  //associated with number of trains or the number of intervals 
        protected internal List<double> BoardingFlow { get; set; } //associated with number of trains or the number of intervals 
        protected internal List<double> CapCost { get; set; }
        /// <summary>
        /// revise in 2021 Feb: get dwell time cost 
        /// </summary>
        protected internal List<double> DwellCost { get; set; }
        protected internal List<TransitLineClass> MapLine { set; get; }
     
        public SegClass()
        {
            Tail = new NodeClass();
            Head = new NodeClass();
            ID = PARA.NULLINT;
            TravelTime = PARA.NULLDOUBLE;
            MapLine = new List<TransitLineClass>();
            NextSegID = PARA.NULLINT;
            OnBoardFlow = new List<double>();
            CapCost = new List<double>();
            DwellCost = new List<double>();
            BoardingFlow = new List<double>();
        }
        protected internal void PrintSeg(int SolId=-1)
        {
            string FileName = MyFileNames.OutPutFolder + "Bcm_Seg.txt";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(FileName, true))
            {
                for (int i = 0; i < CapCost.Count; i++)
                {
                    file.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}", Global.NumOfIter, SolId,
                             ID, Tail.ID, Head.ID, TravelTime, MapLine[0].ID, i, OnBoardFlow[i], BoardingFlow[i], CapCost[i]);
                }
            }
        }
        protected internal void _IniNonDomEventID(int _size)
        {
            NonDomEventID = new int[_size];
            for (int i = 0; i < NonDomEventID.Count(); i++) NonDomEventID[i] = PARA.NULLINT;
        }
        public double getCapCost(double DepTime, int TrainIndex)
        {

            // ignore capacity cost during the generation
            // because the could overestimate the cost
            //return 0;
            switch (MapLine[0].ServiceType)
            {
                case TransitServiceType.Frequency:
                    return CapCost[PARA.FindInterval(DepTime)];
                case TransitServiceType.Schedule:
                    return CapCost[TrainIndex];
            }

            Console.WriteLine("Warninig_SegClass: Set._CapCost does not return val");
            Console.ReadLine();

            return 0;
        }
        public double getDwellCost(double DepTime, int TrainIndex)
        {
            // ignore capacity cost during the generation
            // because the could overestimate the cost
            //return 0;
            switch (MapLine[0].ServiceType)
            {
                case TransitServiceType.Frequency:
                    return DwellCost[PARA.FindInterval(DepTime)];
                case TransitServiceType.Schedule:
                    return DwellCost[TrainIndex];
            }

            Console.WriteLine("Warninig_SegClass: Set.dwellCost does not return val");
            Console.ReadLine();

            return 0;
        }

        #region notused
        /// <summary>
        /// create initial list of segment based on the line information
        /// </summary>
        /// <param name="Segs"></param>
        /// <param name="Lines"></param>
        /// <returns></returns>
        //protected internal static void IniSegsList(ref List<SegClass> Segs, ref List<TransitLineClass> Lines)
        //{
        //    int NumSeg = 0;
        //    for (int l = 0; l < Lines.Count; l++)
        //    {
        //        int SegCount = 0;
        //        for (int s = 0; s < Lines[l].Stops.Count - 1; s++)
        //        {
        //            Segs.Add(new SegClass());
        //            Segs[NumSeg].ID = NumSeg;
        //            Segs[NumSeg].Tail = Lines[l].Stops[s];
        //            Segs[NumSeg].Head = Lines[l].Stops[s + 1];
        //            Segs[NumSeg].TravelTime = Lines[l].SegTravelTimes[SegCount];
        //            Segs[NumSeg].MapLine.Add(Lines[l]);
        //            Lines[l].MapSegs.Add(Segs[NumSeg]);

        //            if (Lines[l].ServiceType == TransitServiceType.Frequency)
        //            {
        //                Segs[NumSeg].OnBoardFlow = new List<double>(new double[PARA.IntervalSets.Count]);
        //                Segs[NumSeg].CapCost = new List<double>(new double[PARA.IntervalSets.Count]);
        //                Segs[NumSeg].BoardingFlow = new List<double>(new double[PARA.IntervalSets.Count]);
        //            }
        //            if (Lines[l].ServiceType == TransitServiceType.Schedule)
        //            {
        //                Segs[NumSeg].OnBoardFlow = new List<double>(new double[Lines[l].NumOfTrains]);
        //                Segs[NumSeg].CapCost = new List<double>(new double[Lines[l].NumOfTrains]);
        //                Segs[NumSeg].BoardingFlow = new List<double>(new double[Lines[l].NumOfTrains]);
        //            }

        //            SegCount++;
        //            NumSeg++;
        //        }
        //    }
        //}

        /// <summary>
        /// create link list for next segment Id
        /// </summary>
        /// <param name="Segs"></param>
        /// <param name="Nodes"></param>
        /// <returns></returns>
        //protected internal static void IniNextSegID(ref List<SegClass> Segs, List<NodeClass> Nodes)
        //{
        //    for (int i = 0; i < Segs.Count; i++)
        //    {
        //        for (int s = 0; s < Nodes[Segs[i].Head.ID].OutSegs.Count; s++)
        //        {
        //            if (Segs[i].MapLine[0].ID == Nodes[Segs[i].Head.ID].OutSegs[s].MapLine[0].ID)
        //            {
        //                Segs[i].NextSegID = Nodes[Segs[i].Head.ID].OutSegs[s].ID;
        //                break;
        //            }
        //        }
        //    }
        //}

        //public static void PrintSegs(ref List<SegClass> Segs)
        //{
        //    string FileName;
        //    FileName = MyFileNames.OutPutFolder+ "Segs.txt";
        //    using (System.IO.StreamWriter file =
        //         new System.IO.StreamWriter(FileName))
        //    {
        //        file.WriteLine("ID,Tail,Head,Time,Line,Next,NonDom");
        //        for (int i = 0; i < Segs.Count; i++)
        //        {
        //            file.WriteLine("{0},{1},{2},{3},{4},{5},{6}",
        //                Segs[i].ID, Segs[i].Tail.ID, Segs[i].Head.ID, Segs[i].TravelTime, Segs[i].MapLine[0].ID, Segs[i].NextSegID, Segs[i].NonDomEventID);
        //        }
        //    }
        //}

        #endregion
    }

}
