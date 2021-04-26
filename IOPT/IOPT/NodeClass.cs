// I do not touch this 7-Jan-2019
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

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
        // record the first non dominated event
        protected internal int[] NonDomEventID { get; set; }
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

        /// <summary>
        /// get the Line's arrival time 
        /// </summary>
        /// <param name="LineID"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        protected internal double getArrTime(int LineID, int q)
        {
            
            int index = LineTimes.FindIndex(x => x.LineID == LineID);
            if (index < 0)
            {
                Console.WriteLine("Warning: Can not find line ID in GetArrTime" +
                "Node = {0}, LineID ={1}",ID,LineID);
            }
            return LineTimes[index].ArrTimes[q];
        }
        /// get the Line's dep time time 
        protected internal double getDepTime(int LineID,int q)
        {
            return LineTimes.Find(x => x.LineID == LineID).DepTimes[q];
            //int index = LineTimes.FindIndex(x => x.LineID == LineID);
            //if (index < 0)
            //{
            //    Console.WriteLine("Warning: Can not find line ID in getDepTime" +
            //    "Node = {0}, LineID ={1}",ID,LineID);
            //}
            //return LineTimes[index].DepTimes[q];
        }
        /// <summary>
        /// get the lines dwell time as the difference between the dep time and arr time
        /// </summary>
        /// <param name="LineID"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        protected internal double getDwellTime(int LineID, int q)
        {
            return LineTimes.Find(x => x.LineID == LineID).DwellTimes[q];
        }

        /// <summary>
        /// clear arrival and departure time vectors of lines
        /// </summary>
        /// <param name="LineID"></param>
        /// <returns></returns>
        protected internal void ClearLineTimes(int LineID) 
        {
            for (int i=0;i<LineTimes.Count();i++)
            {
                if (LineTimes[i].LineID == LineID)
                {
                    LineTimes[i].ArrTimes.Clear();
                    LineTimes[i].DepTimes.Clear();
                    LineTimes[i].DwellTimes.Clear();
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
        #region notUsed
        /// <summary>
        /// Create incoming and outgoing segment/lines associated with the node
        /// </summary>
        /// <param name="Segs"></param>
        /// <param name="Nodes"></param>
        /// <returns></returns>
        //protected internal static void GetInOutSegsAndLines(ref List<SegClass> Segs, ref List<NodeClass> Nodes)
        //{
        //    Debug.Assert(Segs.Count > 0, "Warning_NodeClass: Segment list is not set: Seg count = 0");

        //    // step 1 create incoming and outgoing segs
        //    for (int i = 0; i < Segs.Count; i++)
        //    {
        //        Nodes[Segs[i].Tail.ID].OutSegs.Add(Segs[i]);
        //        Nodes[Segs[i].Head.ID].InSegs.Add(Segs[i]);
        //        for (int j = 0; j < Segs[i].MapLine.Count; j++)
        //        {
        //            Nodes[Segs[i].Tail.ID].OutLines.Add(Segs[i].MapLine[j]);
        //            Nodes[Segs[i].Head.ID].InLines.Add(Segs[i].MapLine[j]);
        //        }
        //    }
        //    int _lineID = -1;
        //    for (int i = 0; i < Nodes.Count; i++)
        //    {
        //        for (int s = 0; s < Nodes[i].OutSegs.Count; s++)
        //        {
        //            _lineID = Nodes[i].OutSegs[s].MapLine[0].ID;
        //            if (Nodes[i].m_LineID_Flow.ContainsKey(_lineID))
        //            {
        //                continue;
        //            }
        //            else
        //            {
        //                Nodes[i].m_LineID_Flow.Add(_lineID, new List<FlowClass>());
        //                if (Nodes[i].OutSegs[s].MapLine[0].ServiceType == TransitServiceType.Frequency)
        //                {
        //                    for (int j = 0; j < PARA.IntervalSets.Count; j++)
        //                    {
        //                        Nodes[i].m_LineID_Flow[_lineID].Add(new FlowClass(_lineID));
        //                    }
        //                }
        //                if (Nodes[i].OutSegs[s].MapLine[0].ServiceType == TransitServiceType.Schedule)
        //                {
        //                    for (int q = 0; q < Nodes[i].OutSegs[s].MapLine[0].NumOfTrains; q++)
        //                    {
        //                        Nodes[i].m_LineID_Flow[_lineID].Add(new FlowClass(_lineID));
        //                    }
        //                }
        //            }
        //        }
        //        for (int s = 0; s < Nodes[i].InSegs.Count; s++)
        //        {
        //            _lineID = Nodes[i].InSegs[s].MapLine[0].ID;
        //            if (Nodes[i].m_LineID_Flow.ContainsKey(_lineID))
        //            {
        //                continue;
        //            }
        //            else
        //            {
        //                Nodes[i].m_LineID_Flow.Add(_lineID, new List<FlowClass>());
        //                if (Nodes[i].InSegs[s].MapLine[0].ServiceType == TransitServiceType.Frequency)
        //                {
        //                    for (int j = 0; j < PARA.IntervalSets.Count; j++)
        //                    {
        //                        Nodes[i].m_LineID_Flow[_lineID].Add(new FlowClass(_lineID));
        //                    }
        //                }
        //                if (Nodes[i].InSegs[s].MapLine[0].ServiceType == TransitServiceType.Schedule)
        //                {
        //                    for (int q = 0; q < Nodes[i].InSegs[s].MapLine[0].NumOfTrains; q++)
        //                    {
        //                        Nodes[i].m_LineID_Flow[_lineID].Add(new FlowClass(_lineID));
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        #endregion

    }

}
