// Checked 2021-May
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace IOPT
{

    public partial class NetworkClass
    {
        /// <summary>
        /// read the visited stops of a transit line
        /// </summary>
        protected internal void ReadLineStop()
        {
            string LineStopFile;
            LineStopFile = MyFileNames.InputFolder + "LineStop.csv";
            Trace.Assert(File.Exists(LineStopFile), "LineStop.csv File does not exist");

            char[] delimiters = new char[] { ',' };
            using (StreamReader reader = new StreamReader(LineStopFile))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    string[] parts = line.Split(delimiters);
                    if (parts.Length == 0) break;

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
            string LineStopTime = MyFileNames.InputFolder + "LineTime.csv";

            using (StreamReader reader = new StreamReader(LineStopTime))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) { break; }
                    string[] parts = line.Split(delimiters);
                    if (parts.Length == 0) break;

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
        protected internal void ReadNodes()
        {
            string NodeFileName = MyFileNames.InputFolder + "Node.csv";
            char[] delimiters = new char[] { ',' };
            if (!File.Exists(NodeFileName)) Console.WriteLine("Warning_NetworkRead: Cannot find node.csv");
            using (StreamReader reader = new StreamReader(NodeFileName))
            {
                int count = 0;
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    string[] parts = line.Split(delimiters);
                    if (parts.Length == 0) break;
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
        protected internal void ReadTrips()
        {
            string TripFileName = MyFileNames.InputFolder + "Trip.csv";
            Trace.Assert(File.Exists(TripFileName), "Trip.csv file does not exist");

            char[] delimiters = new char[] { ',' };
            using (StreamReader reader = new StreamReader(TripFileName))
            {
                int count = 0;
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    string[] parts = line.Split(delimiters);
                    if (parts.Length == 0) break;
                    if (count == 0) { count++; }
                    else
                    {
                        count++;
                        if (Convert.ToInt32(parts[0]) != count - 2)
                        {
                            Console.WriteLine("Warning: check the input trip id");
                        }

                        Trips.Add(new TripClass(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]), Convert.ToInt32(parts[2]),
                            Global.MyString2Double(parts[3]), Global.MyString2Double(parts[4]),
                             Global.MyString2Double(parts[5]), Global.MyString2Double(parts[6]),
                             Global.MyString2Double(parts[7]), Global.MyString2Double(parts[8]),
                             Global.MyString2Double(parts[10]), Global.MyString2Double(parts[11]))
                             );
                        Trips[Convert.ToInt32(parts[0])].Demand = Global.MyString2Double(parts[9]);
                    }
                }
            }
        }

        protected internal void ReadLines()
        {
            char[] delimiters = new char[] { ',' };

            Dictionary<int, List<double>> m_Line_Vehdep = new Dictionary<int, List<double>>();
            string SchFileName = MyFileNames.InputFolder + "SchDep.csv";
            Trace.Assert(File.Exists(SchFileName), "File lines.csv does not exist");
            using (StreamReader depreader = new StreamReader(SchFileName))
            {
                while (true)
                {
                    string line = depreader.ReadLine();
                    if (line == null) break;
                    string[] parts = line.Split(delimiters);
                    if (parts.Length == 0) break;
                    int LineId = Convert.ToInt32(parts[0]);
                    double Dep = Global.MyString2Double(parts[2]);
                    if (m_Line_Vehdep.ContainsKey(LineId))
                    {
                        m_Line_Vehdep[LineId].Add(Dep);
                    }
                    else
                    {
                        List<double> dd = new List<double>();
                        dd.Add(Dep);
                        m_Line_Vehdep.Add(LineId, dd);
                    }
                }
            }

            string LineFileName = MyFileNames.InputFolder + "Lines.csv";
            Trace.Assert(File.Exists(LineFileName), "File lines.csv does not exist");

            using (StreamReader reader = new StreamReader(LineFileName))
            {
                int Count = 0;
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    string[] parts = line.Split(delimiters);

                    if (parts.Length == 0) break;
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
                        double SetHeadWay = Global.MyString2Double(parts[3]);
                        double SetStartTime = Global.MyString2Double(parts[4]);
                        double SetEndTime = Global.MyString2Double(parts[5]);
                        TransitVehicleType VehType = TransitVehicleType.IsNull;
                        if (parts[6].Equals("Bus")) VehType = TransitVehicleType.Bus;
                        if (parts[6].Equals("Train")) VehType = TransitVehicleType.Train;
                        if (parts[6].Equals("S_Train")) VehType = TransitVehicleType.S_Train;
                        if (parts[6].Equals("Metro")) VehType = TransitVehicleType.Metro;

                        if (ServiceType == TransitServiceType.Schedule)
                        {
                            Trace.Assert(SetEndTime < PARA.DesignPara.MaxTimeHorizon, "Warning_NetworkRead: Sch Line end time is larger than maxTimeHorizon");
                        }


                        Lines.Add(new TransitLineClass(SetID, SetName, SetStartTime, SetEndTime, SetHeadWay, ServiceType, VehType));
                        

                        double SetCap = Global.MyString2Double(parts[7]);
                        Lines[Lines.Count()-1].NumOfDoors = Convert.ToInt32(parts[8]);

                        if (ServiceType == TransitServiceType.Schedule)
                        {

                            Lines[Lines.Count - 1].NumOfTrains = m_Line_Vehdep[SetID].Count();
                            for (int i = 0; i < Lines[Lines.Count - 1].NumOfTrains; i++)
                            {
                                Lines[Lines.Count - 1].TrainCap.Add(SetCap);
                                Lines[Lines.Count - 1].IniDepTime.Add(m_Line_Vehdep[SetID][i]);
                            }
                        }

                        if (ServiceType == TransitServiceType.Frequency)
                        {
                            Lines[Lines.Count - 1].NumOfTrains = PARA.NULLINT;
                            Lines[Lines.Count - 1].FreCap = SetCap;
                        }

                        if (ServiceType == TransitServiceType.Frequency)
                        {
                            if (!SetStartTime.Equals(PARA.NULLINT) || !SetEndTime.Equals(PARA.NULLINT))
                                MyLog.Instance.Info("The input fre line start or end time != -1");
                        }
                        else if (ServiceType == TransitServiceType.Schedule)
                        {
                            if (SetStartTime.Equals(PARA.NULLINT) || SetEndTime.Equals(PARA.NULLINT))
                                MyLog.Instance.Error("sch line start/end time is not set");
                        }
                        Trace.Assert(ServiceType != TransitServiceType.IsNull, "read line services type is null");
                    }
                }
            }
        }
    }
}
