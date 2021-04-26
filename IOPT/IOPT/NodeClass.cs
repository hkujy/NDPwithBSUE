// Checked 2021-May
using System.Collections.Generic;
using System.Linq;
namespace IOPT
{
    public class DepArrTimeClass
    {
        protected internal int LineID { get; set; }
        protected internal List<double> ArrTimes { get; set; }
        protected internal List<double> DepTimes { set; get; }
        protected internal List<double> DwellTimes { set; get; }
        protected internal StopType Type { get; set; }

        protected internal DepArrTimeClass(int SetLineID, List<double> SetDwellTime,  StopType SetType)
        {
            // this is created for the frequency-based lines
            LineID = SetLineID; Type = SetType;
            DwellTimes = new List<double>(SetDwellTime);
        }
        protected internal DepArrTimeClass(int SetLineID, List<double> SetArrTime, List<double> SetDepTimes, List<double> SetDwellTimes, StopType SetType)
        {
            LineID = SetLineID; Type = SetType;
            ArrTimes = new List<double>(SetArrTime);
            DepTimes = new List<double>(SetDepTimes);
            DwellTimes = new List<double>(SetDwellTimes);
        }
    }
    public class NodeClass
    {
        protected internal int ID { get; private set; }
        protected internal string Name { get; private set; }
        /*map line id to flow class*/
        protected internal Dictionary<int, List<FlowClass>> m_LineID_Flow { get; set; } // the first dimension is LineID, the second dimension is line flow
        // NonDomEventID: dimension corresponds to the number of destinations/trips
        protected internal int[] NonDomEventID { get; set; }        // record the first non dominated event
        protected internal List<SegClass> InSegs { get; set; }
        protected internal List<SegClass> OutSegs { get; set; }
        protected internal List<DepArrTimeClass> LineTimes { get; set;}
        protected internal List<TransitLineClass> InLines { get; set; }
        protected internal List<TransitLineClass> OutLines { get; set; }
        protected internal NodeType Type { get; set; }
        public NodeClass()
        {
            Name = "NullName";
            ID = PARA.NULLINT;
            InSegs = new List<SegClass>();
            OutSegs = new List<SegClass>();
            LineTimes = new List<DepArrTimeClass>();
            InLines = new List<TransitLineClass>();
            OutLines = new List<TransitLineClass>();
            Type = NodeType.IsNull;
            m_LineID_Flow = new Dictionary<int, List<FlowClass>>();
        }

        protected internal double getArrTime(int LineID, int q)
        {
            return LineTimes.Find(x => x.LineID == LineID).ArrTimes[q];
        }
        protected internal double getDepTime(int LineID,int q)
        {
            return LineTimes.Find(x => x.LineID == LineID).DepTimes[q];
        }
        protected internal double getDwellTime(int LineID, int q)
        {
            return LineTimes.Find(x => x.LineID == LineID).DwellTimes[q];
        }

        protected internal void ClearLineTimes(int LineID) 
        {
            for (int i=0;i<LineTimes.Count();i++)
            {
                if (LineTimes[i].LineID == LineID)
                {
                    LineTimes[i].ArrTimes.Clear(); LineTimes[i].DepTimes.Clear(); LineTimes[i].DwellTimes.Clear();
                }
            }
        }
        protected internal NodeClass(int SetID, string SetName, NodeType SetType)
        {
            ID = SetID;
            Name = SetName;
            Type = SetType;
            InSegs = new List<SegClass>();
            OutSegs = new List<SegClass>();
            LineTimes = new List<DepArrTimeClass>();
            InLines = new List<TransitLineClass>();
            OutLines = new List<TransitLineClass>();
            m_LineID_Flow = new Dictionary<int, List<FlowClass>>();
        }
        protected internal void IniNonDomEventID(int _size)
        {
            NonDomEventID = new int[_size];
            for (int i = 0; i < NonDomEventID.Count(); i++) NonDomEventID[i] = PARA.NULLINT;
        }
    }
}
