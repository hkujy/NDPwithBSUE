using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace IOPT
{
    public class Input
    {

        /// <summary>
        /// read file and set input data 
        /// </summary>
        /// <param name="Nodes"></param>
        /// <param name="Lines"></param>
        /// <param name="Segs"></param>
        /// <param name="Trips"></param>
        /// <param name="UniOrigins"></param>
        public static void SetTestData(ref List<NodeClass> Nodes, ref List<TransitLineClass> Lines,
            ref List<SegClass> Segs, ref List<TripClass> Trips, ref List<UniqueOrigin> UniOrigins)
        {
            // Set parameters
            ReadClass.Nodes(ref Nodes);
            Console.WriteLine("Node file has been read");
            ReadClass.Lines(ref Lines);
            ReadClass.LineStop(ref Lines,ref Nodes);
            Console.WriteLine("Line file and line stop file have been read");
            ReadClass.Trips(ref Trips);
            Console.WriteLine("Trip file has been read");

            foreach (TransitLineClass l in Lines)
            {
                if (l.ServiceType == TransitServiceType.Frequency)
                {
                    l.NumOfTrains = PARA.NULLINT;
                    l.StartTime = PARA.NULLINT;
                    l.EndTime = PARA.NULLINT;
                    l.FreCap = PARA.DesignPara.FreBusCap;
                }
                if (l.ServiceType == TransitServiceType.Schedule)
                {
                    l.NumOfTrains = (int)((l.EndTime - l.StartTime) / l.Headway);
                    for (int i = 0; i < l.NumOfTrains; i++)
                        l.TrainCap.Add(PARA.DesignPara.TrainCap);
                }
            }

            CreateDataStructure(ref Nodes, ref Lines, ref Segs, ref Trips, ref UniOrigins);

        }
        public static void PrintInput(List<TripClass> Trips, List<TransitLineClass> TransitLines,
                                       List<NodeClass> Nodes)
        {
            TripClass.PrintTrips(Trips);
            TransitLineClass.PrintScheduleTable(TransitLines);
            TransitLineClass.PrintLineStops(TransitLines);
            NodeClass.PrintAllNodes(Nodes);
        }



        /// <summary>
        /// create data structure : node and link link list
        /// </summary>
        /// <param name="Nodes"></param>
        /// <param name="TransitLines"></param>
        /// <param name="Segs"></param>
        /// <param name="Trips"></param>
        /// <param name="UniOrigins"></param>
        /// <returns></returns>
        public static void CreateDataStructure(ref List<NodeClass> Nodes, ref List<TransitLineClass> TransitLines,
            ref List<SegClass> Segs, ref List<TripClass> Trips, ref List<UniqueOrigin> UniOrigins)
        {

            SegClass.IniSegsList(ref Segs, ref TransitLines);
            SegClass.IniNextSegID(ref Segs, Nodes);
            SegClass.PrintSegs(Segs);
            NodeClass.GetInOutSegsAndLines(ref Segs, ref Nodes);

            foreach (TransitLineClass l in TransitLines) l.getStopTimeDifMap();

            for (int l = 0; l < TransitLines.Count; l++)  TransitLines[l].IniSchedule(ref Nodes);

            UniqueOrigin.CreateUniqueOriginSet(Trips, ref UniOrigins);
            PrintInput(Trips, TransitLines, Nodes);
            //PARA.NumOfUniOrigins = UniOrigins.Count;

        }

      
    }
    public class ReadClass
    {

        /// <summary>
        /// read the visited stops of a transit line
        /// </summary>
        /// <param name="Lines"></param>
        /// <param name="Nodes"></param>
        /// <returns></returns>
        public static void LineStop(ref List<TransitLineClass> Lines, ref List<NodeClass> Nodes)
        {
            string LineStopFile;
            LineStopFile = PARA.InputFolder + "LineStop.csv";
            char[] delimiters = new char[] { ',' };
            using (StreamReader reader = new StreamReader(LineStopFile))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    string[] parts = line.Split(delimiters);
                    int LineID = Convert.ToInt32(parts[0]);
                    for (int i = 0; i < parts.Count() - 1; i++)
                    {
                        int stopid = Convert.ToInt32(parts[i + 1]);
                        Lines[LineID].Stops.Add(Nodes.Find(x => x.ID == stopid));
                    }
                }
            }

            foreach (TransitLineClass Line in Lines)
            {
                Line.NumOfSegs = Line.Stops.Count - 1;
            }
            string LineStopTime = PARA.InputFolder + "LineTime.csv";

            using (StreamReader reader = new StreamReader(LineStopTime))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }
                    string[] parts = line.Split(delimiters);
                    int LineID = Convert.ToInt32(parts[0]);
                    Lines[LineID].Length = 0d;
                    for (int i = 0; i < parts.Count() - 1; i++)
                    {
                        Lines[LineID].Length += double.Parse(parts[i + 1]);
                        Lines[LineID].SegTravelTimes.Add(double.Parse(parts[i + 1]));
                    }
                }
            }
        }

        /// <summary>
        /// read nodes from file
        /// </summary>
        /// <param name="Nodes"></param>
        /// <returns></returns>
        public static void Nodes(ref List<NodeClass> Nodes)
        {
            string NodeFileName = PARA.InputFolder + "Node.csv";
            char[] delimiters = new char[] { ',' };
            if (!File.Exists(NodeFileName)) Console.WriteLine("Cannot find node.csv");
            using (StreamReader reader = new StreamReader(NodeFileName))
            {
                int count = 0;
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    string[] parts = line.Split(delimiters);
                    if (count == 0)
                    {
                        count++; //line for the header
                    }
                    else
                    {
                        count++;
                        int SetId = Convert.ToInt32(parts[0]);
                        if (parts[2].Equals("Stop")) Nodes.Add(new NodeClass(SetId, parts[1], NodeType.Stop));
                        if (parts[2].Equals("Zone")) Nodes.Add(new NodeClass(SetId, parts[1], NodeType.Zone));
                    }
                }
            }
        }


        /// <summary>
        /// read trip data, which is also the OD demand
        /// </summary>
        /// <param name="Trips"></param>
        /// <returns></returns>
        public static void Trips(ref List<TripClass> Trips)
        {
            string TripFileName = PARA.InputFolder + "Trip.csv";
            char[] delimiters = new char[] { ',' };
            using (StreamReader reader = new StreamReader(TripFileName))
            {
                int count = 0;
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    string[] parts = line.Split(delimiters);
                    if (count == 0) { count++; }
                    else
                    {
                        count++;
                        Trips.Add(new TripClass(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]), Convert.ToInt32(parts[2]),
                        Convert.ToDouble(parts[3]), Convert.ToDouble(parts[4]), Convert.ToDouble(parts[5]),
                        Convert.ToDouble(parts[6]), Convert.ToDouble(parts[7]), Convert.ToDouble(parts[8])));
                        Trips[Convert.ToInt32(parts[0])].Demand = Convert.ToDouble(parts[9]);
                    }
                }
            }
            PARA.NumOfTrips = Trips.Count();
        }

        public static void Lines(ref List<TransitLineClass> Lines)
        {
            string LineFileName = PARA.InputFolder + "Lines.csv";
            if (!File.Exists(LineFileName)) Console.WriteLine("can not find lines.csv");
            char[] delimiters = new char[] { ',' };
            using (StreamReader reader = new StreamReader(LineFileName))
            {
                int Count = 0;
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    string[] parts = line.Split(delimiters);
                    if (Count == 0)
                    {
                        Count++;
                    }
                    else
                    {
                        Count++;
                        TransitServiceType ServiceType = TransitServiceType.IsNull;
                        int SetID = Convert.ToInt32(parts[0]);
                        string SetName = parts[1];
                        if (parts[2].Equals("Schedule")) ServiceType = TransitServiceType.Schedule;
                        if (parts[2].Equals("Frequency")) ServiceType = TransitServiceType.Frequency;
                        double SetHeadWay = Convert.ToDouble(parts[3]);
                        double SetStartTime = Convert.ToDouble(parts[4]);
                        double SetEndTime = Convert.ToDouble(parts[5]);
                        TransitVehicleType VehType = TransitVehicleType.IsNull;
                        if (parts[6].Equals("Bus")) VehType = TransitVehicleType.Bus;
                        if (parts[6].Equals("Train")) VehType = TransitVehicleType.Train;
                        if (parts[6].Equals("S_Train")) VehType = TransitVehicleType.S_Train;
                        if (parts[6].Equals("Metro")) VehType = TransitVehicleType.Metro;

                        Debug.Assert(SetEndTime < PARA.DesignPara.MaxTimeHorizon, "Sch Line end time is larger than maxTimeHorizon");

                        Lines.Add(new TransitLineClass(SetID, SetName, SetStartTime, SetEndTime, SetHeadWay, ServiceType, VehType));
                        if (ServiceType == TransitServiceType.Frequency)
                        {
                            if (!SetStartTime.Equals(PARA.NULLINT) || !SetEndTime.Equals(PARA.NULLINT))
                                MyLog.Instance.Error("fre line start or end time != -1");
                        }
                        else if (ServiceType == TransitServiceType.Schedule)
                        {
                            if (SetStartTime.Equals(PARA.NULLINT) || SetEndTime.Equals(PARA.NULLINT))
                                MyLog.Instance.Error("sch line start/end time is not set");
                        }
                        Debug.Assert(ServiceType != TransitServiceType.IsNull, "read line services type is null");
                    }
                }
            }
        }

        /// <summary>
        /// Cost para associated with that path variables
        /// </summary>
        /// <returns></returns>

    }
}
