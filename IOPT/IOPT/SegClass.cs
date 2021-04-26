// checked 2021 - May
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
        protected internal void IniNonDomEventID(int _size)
        {
            NonDomEventID = new int[_size];
            for (int i = 0; i < NonDomEventID.Count(); i++) NonDomEventID[i] = PARA.NULLINT;
        }
        public double getCapCost(double DepTime, int TrainIndex)
        {
            switch (MapLine[0].ServiceType)
            {
                case TransitServiceType.Frequency:
                    return CapCost[PARA.FindInterval(DepTime)];
                case TransitServiceType.Schedule:
                    return CapCost[TrainIndex];
            }
            return 0;
        }
        public double getDwellCost(double DepTime, int TrainIndex)
        {
            switch (MapLine[0].ServiceType)
            {
                case TransitServiceType.Frequency:
                    return DwellCost[PARA.FindInterval(DepTime)];
                case TransitServiceType.Schedule:
                    return DwellCost[TrainIndex];
            }
            return 0;
        }
    }
}
