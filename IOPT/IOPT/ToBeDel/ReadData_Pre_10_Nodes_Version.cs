using CsvHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using CsvHelper.Configuration;
using System.IO;

namespace IOPT
{
   public class Input
    {
        public static void SetTestData(ref List<NodeClass> Nodes, ref List<TransitLineClass> TransitLines, 
            ref List<SegClass> Segs, ref List<TripClass> Trips, ref List<UniqueOrigin> UniOrigins)
        {

            // for the stop name : s_ Stands for stops
            Nodes.Add(new NodeClass(0, "z_DTU", NodeType.Zone));
            Nodes.Add(new NodeClass(1, "s_DTU", NodeType.Stop));
            Nodes.Add(new NodeClass(2, "s_Lyn_bus", NodeType.Stop));
            Nodes.Add(new NodeClass(3, "s_Lyn_s_tog", NodeType.Stop));
            Nodes.Add(new NodeClass(4, "s_Nor_bus", NodeType.Stop));
            Nodes.Add(new NodeClass(5, "s_Nor_tog", NodeType.Stop));
            Nodes.Add(new NodeClass(6, "s_Nor_metro", NodeType.Stop));
            Nodes.Add(new NodeClass(8, "s_CPH", NodeType.Stop));
            Nodes.Add(new NodeClass(9, "z_CPH", NodeType.Zone));
            
            //add schedule transit lines
            TransitLines.Add(new TransitLineClass(0,"15E",400,600,12,13f, TransitServiceType.Schedule));
            TransitLines.Add(new TransitLineClass(1,"Line B",400,600,10,12.5f, TransitServiceType.Schedule));
            TransitLines.Add(new TransitLineClass(2, "Line E", 405, 600, 10, 12.5f, TransitServiceType.Schedule));
            TransitLines.Add(new TransitLineClass(3,"oreundstog", 409, 600,10,13,TransitServiceType.Schedule));
            TransitLines.Add(new TransitLineClass(4,"300s", 401,600,6,3.5f,TransitServiceType.Schedule));

            // add frequency based transit lines
            TransitLines.Add(new TransitLineClass(5, "150S", 5, 13, TransitServiceType.Frequency));
            TransitLines.Add(new TransitLineClass(6, "M2", 4, 9, TransitServiceType.Frequency));



            //TODO: add tranfer lines 


            //add stops for schedule based lines
            TransitLines[0].Stops.Add(2);
            TransitLines[0].Stops.Add(5);

            TransitLines[1].Stops.Add(4);
            TransitLines[1].Stops.Add(6);

            TransitLines[2].Stops.Add(4);
            TransitLines[2].Stops.Add(6);

            TransitLines[3].Stops.Add(7);
            TransitLines[3].Stops.Add(9);

            TransitLines[4].Stops.Add(2);
            TransitLines[4].Stops.Add(3);

            //add frequency based lines
            TransitLines[5].Stops.Add(2);
            TransitLines[5].Stops.Add(5);
            TransitLines[6].Stops.Add(8);
            TransitLines[6].Stops.Add(9);


            foreach (TransitLineClass Line in TransitLines)
            {
                Line.NumOfSegs = Line.Stops.Count - 1;
            }
            // change stop number index to let them start with 0
            for(int l = 0;l<TransitLines.Count;l++)
            {
                for (int s=0;s<TransitLines[l].Stops.Count;s++)
                {
                    TransitLines[l].Stops[s]--;
                }
            }
            // set segment time
            TransitLines[0].SegTravelTimes.Add(26f);
            TransitLines[1].SegTravelTimes.Add(20f);
            TransitLines[2].SegTravelTimes.Add(17f);
            TransitLines[3].SegTravelTimes.Add(19f);
            TransitLines[4].SegTravelTimes.Add(9f);
            TransitLines[5].SegTravelTimes.Add(29f);
            TransitLines[6].SegTravelTimes.Add(15f);

            SegClass.GetSegsList(ref Segs, ref TransitLines);
            NodeClass.GetInOutSegsAndLines(ref Segs,ref Nodes);

            for (int l = 0; l<TransitLines.Count;l++)
            {
                TransitLines[l].CreateRunTable(ref Nodes, Segs);
            }

            int OriginNode = 1;
            int DestNode = 8;
            Trips.Add(new TripClass(0, OriginNode, DestNode, 405, 600, 0, 0, 0, 0));
            Trips.Add(new TripClass(1, OriginNode, DestNode, 420, 600, 0, 0, 0, 0));
            //TODO: think a same OD with different departure time use different structure

            //Trips.Add(new TripClass(2, OriginNode, DestNode, 435, 600, 0, 0, 0, 0));
            //Trips.Add(new TripClass(3, OriginNode, DestNode, 450, 600, 0, 0, 0, 0));


            //TODO set transit line vehicle Type for a general network 
            TransitLines[0].VehicleType = TransitVehicleType.Bus;
            TransitLines[1].VehicleType = TransitVehicleType.Train;
            TransitLines[2].VehicleType = TransitVehicleType.Train;
            TransitLines[3].VehicleType = TransitVehicleType.S_Train;
            TransitLines[4].VehicleType = TransitVehicleType.Bus;
            TransitLines[5].VehicleType = TransitVehicleType.Bus;
            TransitLines[6].VehicleType = TransitVehicleType.Metro;

            UniqueOrigin.CreateUniqueOriginSet(Trips, ref UniOrigins);


            TransitLineClass.PrintScheduleTable(TransitLines);
            TransitLineClass.PrintLineStops(TransitLines);
            SegClass.PrintSegs(Segs);

            NodeClass.PrintAllNodes(Nodes);
        }
    }
}
