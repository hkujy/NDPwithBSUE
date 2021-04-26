using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;


// this class convert the event array to path array list 
namespace IOPT
{

    public partial class BBmain
    {
        public partial class LpInput
        {
            public class LpPath
            {
               public class Transfer
                {
                    protected internal TransitLineClass Alight { get; set; }
                    protected internal TransitLineClass Board { get; set; }
                    public Transfer(TransitLineClass _a, TransitLineClass _b)
                    {
                        Alight = _a;
                        Board = _b;
                    }
                }
               public class LineID2DeltaSeat
                {
                    public int LineID { get; set; }
                    public Dictionary<int, int> m_BoardNodeId_DeltaPos { get; set; }
                    public LineID2DeltaSeat(int id)
                    {
                        LineID = id;
                        m_BoardNodeId_DeltaPos = new Dictionary<int, int>();
                    }
                }

                protected internal int ID { get; set; }
                protected internal double EventPie { get; set; }  // cost from event
                protected internal double EventTime { get; set; }  // arrival time from event
                protected internal TripClass Trip { get; set; }
                protected internal int StartEventID { get; set; }  // the event id at the destination node
                protected internal double TargetDepTime { get; set; }
                protected internal List<int> VisitNodes { get; set; }
                protected internal List<double> TimeBetweenNodes { get; set; } // related to the line travel time
                protected internal Dictionary<int, int> m_NodeId_WaitVarPos { get; set; }  //<node, wait position> map to waiting time variabl
                protected internal Dictionary<int, int> m_NodeId_ArrVarPos { get; set; } // <node, arr var position>map to the arrival time variables at each node 
                protected internal Dictionary<int, int> m_NodeId_DepVarPos { get; set; } // <node dep position>
                protected internal Dictionary<int, int> m_NodeId_DeltaTrainBoard { get; set; }

                /// <remarks>
                /// the transfer nodes include the first and last nodes
                /// at the last node, the boarding line ID is null
                /// at the first node, the alighting line ID is null
                /// </remarks>

                protected internal Dictionary<int, Transfer> m_NodeID_TransferLine { get; set; }  // at node, transfer from one line to another
                protected internal List<int> TranferNodes { get; set; } // including the boarding nodes
                                                                        // ignore the arr and dep variable at the destination node
                                                                        // remark the map dep/arr variable start from the destination node. i.e., destination is 0
                protected internal Dictionary<int, TransitLineClass> m_NodeID_NextLine { get; set; }

                protected internal Dictionary<int, List<int>> m_LineId_VistNodeOrder { get; set; }  // line ID: sequence of visited nodes and the last one is the ends nodes

                // the following map line section data to the lambada variable
                //protected internal Dictionary<Dictionary<int, int>, int> m_LineNodeID_DeltaSeatPos { get; set; }
                public List<LineID2DeltaSeat> m_LineNodeID_DeltaSeatPos { get; set; }
                // remark : m_LineSec_SeatLamda corresponds every visited node <LineInde, nodeID>
                // this includes both continuous and transfer set

                protected internal Dictionary<TransitVehicleType, double> m_VehType_InVehTime { get; set; }
                protected internal Dictionary<int, int> m_NodeId_SchCapCostVarPos { get; set; }
                protected internal Dictionary<int, int> m_NodeId_FreCapCostVarPos { get; set; }
                protected internal Dictionary<int, int> m_Delta_FreDep_t_pos { get; set; }
                protected internal Dictionary<int, int> m_Delta_Arr_t_pos { get; set; }
                protected internal double PathPie { get; set; }
                protected internal double PathProb { get; set; }
                protected internal double PathProb_Compute { get; set; }

                // static variables
                protected internal static int WaitVarPos { get; set; }
                protected internal static int DepVarPos { get; set; }
                protected internal static int ArrVarPos { get; set; }
                protected internal static int TrainVarPos { get; set; }
                protected internal static int DeltaPathTrainPos { get; set; }
                // arrival time delta used in frequency capacity constraint
                // indicate whether a passenger arrive at a node 
                protected internal static int Delta_FreDep_t_Pos { get; set; }   // dep delta used in the frequency constraint 
                protected internal static int Delta_FreArr_t_Pos { get; set; }   // arrival delta used in the frequency constraint 
                protected internal static int NodeId_SchCapPos { get; set; } // only related to the transfer board node
                protected internal static int NodeId_FreCapPos { get; set; } // only related to the transfer board node

                protected internal static int LineSec_Delta_Seat_Pos { get; set; } // map from line sequence to lamada position

                protected internal LpPath()
                {
                    ID = PARA.NULLINT;
                    Trip = new TripClass();
                    StartEventID = PARA.NULLINT;
                    TargetDepTime = PARA.NULLDOUBLE;
                    VisitNodes = new List<int>();
                    TranferNodes = new List<int>();
                    TimeBetweenNodes = new List<double>();
                    m_NodeId_WaitVarPos = new Dictionary<int, int>();
                    m_NodeId_ArrVarPos = new Dictionary<int, int>();
                    m_NodeId_DepVarPos = new Dictionary<int, int>();
                    m_NodeId_DeltaTrainBoard = new Dictionary<int, int>();
                    m_NodeID_TransferLine = new Dictionary<int, Transfer>();
                    PathProb = 0.0;
                    PathProb_Compute = 0.0;
                    PathPie = 0.0;
                    WaitVarPos = 0;
                    DepVarPos = 0;
                    ArrVarPos = 0;
                    TrainVarPos = 0;
                    DeltaPathTrainPos = 0;
                    NodeId_SchCapPos = 0;
                    NodeId_FreCapPos = 0;
                    LineSec_Delta_Seat_Pos = 0;

                    //m_NodeId_TrainVarPos = new Dictionary<int, int>();
                    m_NodeID_NextLine = new Dictionary<int, TransitLineClass>();
                    m_VehType_InVehTime = new Dictionary<TransitVehicleType, double>();
                    //m_NodeId_BoardLineType = new Dictionary<int, TransitServiceType>();
                    m_NodeId_SchCapCostVarPos = new Dictionary<int, int>();
                    m_NodeId_FreCapCostVarPos = new Dictionary<int, int>();
                    m_Delta_FreDep_t_pos = new Dictionary<int, int>();
                    m_Delta_Arr_t_pos = new Dictionary<int, int>();

                    //m_LineNodeID_DeltaSeatPos = new Dictionary<Dictionary<int, int>, int>();
                    m_LineNodeID_DeltaSeatPos = new List<LineID2DeltaSeat>();
                    m_LineId_VistNodeOrder = new Dictionary<int, List<int>>();

                    m_VehType_InVehTime.Add(TransitVehicleType.Bus, 0);
                    m_VehType_InVehTime.Add(TransitVehicleType.Metro, 0);
                    m_VehType_InVehTime.Add(TransitVehicleType.S_Train, 0);
                    m_VehType_InVehTime.Add(TransitVehicleType.Train, 0);
                }


                /// <summary>
                /// obtained the order of visited node for each line
                /// </summary>
                /// <returns></returns>
                public void getLineId_VisitNodeOrder()// create line section var
                {
                    List<int> TempNodes = new List<int>();
                    int LineID = m_NodeID_NextLine[VisitNodes[0]].ID;
                    for (int i = 0; i < VisitNodes.Count - 1; i++)
                    {
                        if (m_NodeID_NextLine[VisitNodes[i]].ID == LineID)
                        {
                            TempNodes.Add(VisitNodes[i]);
                        }
                        else
                        {
                            TempNodes.Add(VisitNodes[i]);
                            m_LineId_VistNodeOrder.Add(LineID, new List<int>());
                            for (int m = 0; m < TempNodes.Count; m++)
                            {
                                m_LineId_VistNodeOrder[LineID].Add(TempNodes[m]);
                            }
                            TempNodes.Clear();
                            TempNodes.Add(VisitNodes[i]);
                        }
                        LineID = m_NodeID_NextLine[VisitNodes[i]].ID;
                    }
                    TempNodes.Add(VisitNodes[VisitNodes.Count - 1]);
                    if (m_LineId_VistNodeOrder.ContainsKey(LineID))
                    {
                        Console.WriteLine("passenger transfer back the the same line");
                    }
                        
                    m_LineId_VistNodeOrder.Add(LineID, TempNodes);

                    return;

                }


                protected internal void CleanMap()
                {
                    PathPie = 0;
                    PathProb = 0;
                    PathProb_Compute = 0.0;
                    m_NodeId_WaitVarPos.Clear();
                    m_NodeId_ArrVarPos.Clear();
                    m_NodeId_DepVarPos.Clear();
                    m_NodeId_DeltaTrainBoard.Clear();
                    m_NodeID_TransferLine.Clear();
                    m_NodeId_SchCapCostVarPos.Clear();
                    m_NodeId_FreCapCostVarPos.Clear();
                    m_LineId_VistNodeOrder.Clear();
                    m_LineNodeID_DeltaSeatPos.Clear();
                    TranferNodes.Clear();
                    m_Delta_FreDep_t_pos.Clear();
                    m_Delta_Arr_t_pos.Clear();
                }

                /// <summary>
                /// Effective path means that a passenger do not use a same line twice 
                /// </summary>
                /// <param name="Network"></param>
                /// <param name="_StartEventID"></param>
                /// <returns></returns>
                protected static bool isEffectivePath(NetworkClass Network, int _StartEventID)
                {

                    ///<remarks>
                    ///effective path means that a passenger do not use a same line twice 
                    ///or do not transfer back to a same line
                    ///</remarks>
                    bool isEff = true;
                    int Now = _StartEventID;
                    int Next_Board_Line_ID=-1;
                    int OnBoardLineId = -1;
                    int Pre = Network.Events[Now].PathFromEventID;
                    List<int> VisitedLineSet = new List<int>();
                    int currentNode = -1;
                    do
                    {
                        if (Network.Events[Pre].Type == EventType.Node)
                        {
                            currentNode = Network.Events[Pre].NodeID;

                            Next_Board_Line_ID = Network.Segs[Network.Events[Now].SegID].MapLine[0].ID;
                            if (OnBoardLineId ==-1) OnBoardLineId = Next_Board_Line_ID;

                            if (Next_Board_Line_ID!=OnBoardLineId)// board a new line
                            {
                                if (VisitedLineSet.Exists(x => x == Next_Board_Line_ID)) return false;
                                OnBoardLineId = Next_Board_Line_ID;
                            }
                            VisitedLineSet.Add(Next_Board_Line_ID);
                        }
                        Now = Pre;
                        Pre = Network.Events[Now].PathFromEventID;

                    } while (Pre != PARA.NULLINT);
                    return isEff;
                }
                
                /// <summary>
                /// add path from event
                /// </summary>
                /// <param name="Network"></param>
                /// <param name="_StartEventID"></param>
                /// <param name="PathID"></param>
                /// <returns></returns>
                protected internal void AddNewPath(NetworkClass Network, int _StartEventID, int PathID)
                {
                    ID = PathID;
                    StartEventID = _StartEventID;
                    int Now = StartEventID;
                    int Pre = Network.Events[Now].PathFromEventID;
                    VisitNodes.Add(Network.Events[Now].NodeID);
                    // set dep / arrival time at the destination nodes 
                    int BoardLineID = PARA.NULLINT;
                    double BetweenTime = 0;
                    do
                    {
                        if (Network.Events[Pre].Type == EventType.Node)
                        {
                            VisitNodes.Insert(0, Network.Events[Pre].NodeID);
                            BoardLineID = Network.Segs[Network.Events[Now].SegID].MapLine[0].ID;
                            m_NodeID_NextLine.Add(Network.Events[Pre].NodeID, Network.Lines[BoardLineID]);
                            TimeBetweenNodes.Insert(0, BetweenTime);
                            BetweenTime = 0;
                        }
                        else if (Network.Events[Pre].Type == EventType.Seg)
                        {
                            int SegID = Network.Events[Pre].SegID;
                            int LineID = Network.Segs[SegID].MapLine[0].ID;

                            if (Network.Events[Now].Type == EventType.Seg)
                            {
                                ///<remarks>
                                ///the between time considers the dwell time if it is a continuous lines
                                ///</remarks>
                                if (Network.Segs[SegID].MapLine[0].ServiceType == TransitServiceType.Schedule)
                                {
                                    Console.WriteLine("revise add path function this");
                                    Console.WriteLine("for adding new path, we do not know the exact train to be board in then new Timetable" +
                                        "Therefore, the dwell and boarding time is hard to add an accurate one");
                                    Console.WriteLine("Nevertheless, I need to know when this is activated");
                                    BetweenTime += Network.Segs[SegID].TravelTime + PARA.DesignPara.MinDwellTime;
                                    Console.ReadLine();
                                }
                                else
                                {
                                    BetweenTime += Network.Segs[SegID].TravelTime + PARA.DesignPara.MinDwellTime;
                                }
                            }
                            else
                            {
                                BetweenTime += Network.Segs[SegID].TravelTime;
                            }
                            // get the in vehicle travel time for sch and frequency based line services
                            m_VehType_InVehTime[Network.Lines[LineID].VehicleType] += Network.Segs[SegID].TravelTime;
                        }

                        Now = Pre;
                        Pre = Network.Events[Now].PathFromEventID;

                    } while (Pre != PARA.NULLINT);

                    // set the transfer boarding line at the first nodes
                    m_NodeID_TransferLine.Add(VisitNodes[0], new Transfer(new TransitLineClass(), m_NodeID_NextLine[VisitNodes[0]]));

                    // check whether it transfer to a new line
                    for (int i = 1; i < VisitNodes.Count() - 1; i++)
                    {
                        if (m_NodeID_NextLine[VisitNodes[i - 1]] != m_NodeID_NextLine[VisitNodes[i]])
                        {
                            m_NodeID_TransferLine.Add(VisitNodes[i], new Transfer(m_NodeID_NextLine[VisitNodes[i - 1]], m_NodeID_NextLine[VisitNodes[i]]));
                        }
                    }

                    // add last stop transfer line to be the end 
                    m_NodeID_TransferLine.Add(VisitNodes[VisitNodes.Count - 1], new Transfer(m_NodeID_NextLine[VisitNodes[VisitNodes.Count - 2]], new TransitLineClass()));
                    TranferNodes = m_NodeID_TransferLine.Keys.ToList();
                }

                protected internal static void PathSet2VarPos(ref List<LpPath> ps, List<TransitLineClass> Lines)
                {

                    // initialize static variables at beginning 
                    WaitVarPos = 0;
                    DepVarPos = 0;
                    ArrVarPos = 0;
                    TrainVarPos = 0;
                    DeltaPathTrainPos = 0;
                    NodeId_SchCapPos = 0;
                    NodeId_FreCapPos = 0;
                    Delta_FreDep_t_Pos = 0;
                    Delta_FreArr_t_Pos = 0;

                    // step 0: initialize path 
                    foreach (LpPath p in ps) p.CleanMap();

                    for (int p = 0; p < ps.Count; p++)
                    {
                        int NowNode = -1;
                        int NextLineID = -1;
                        // Add Transfer Line Map Id 
                        ps[p].m_NodeID_TransferLine.Add(ps[p].VisitNodes[0], new Transfer(new TransitLineClass(), ps[p].m_NodeID_NextLine[ps[p].VisitNodes[0]]));
                        for (int i = 1; i < ps[p].VisitNodes.Count() - 1; i++)
                        {
                            if (ps[p].m_NodeID_NextLine[ps[p].VisitNodes[i - 1]].ID != ps[p].m_NodeID_NextLine[ps[p].VisitNodes[i]].ID)
                            {
                                ps[p].m_NodeID_TransferLine.Add(ps[p].VisitNodes[i], new Transfer(ps[p].m_NodeID_NextLine[ps[p].VisitNodes[i - 1]], ps[p].m_NodeID_NextLine[ps[p].VisitNodes[i]]));
                            }
                        }
                        /// add last stop transfer line to be the end 
                        ps[p].m_NodeID_TransferLine.Add(ps[p].VisitNodes[ps[p].VisitNodes.Count - 1], new Transfer(ps[p].m_NodeID_NextLine[ps[p].VisitNodes[ps[p].VisitNodes.Count - 2]], new TransitLineClass()));
                        ps[p].TranferNodes = ps[p].m_NodeID_TransferLine.Keys.ToList();


                        for (int s=0;s<ps[p].VisitNodes.Count;s++)
                        {
                            NowNode = ps[p].VisitNodes[s];
                            ps[p].m_NodeId_ArrVarPos.Add(NowNode, ArrVarPos);
                            ArrVarPos++;
                            if (s!=ps[p].VisitNodes.Count-1)
                            {
                                // departure position does not contain the last node
                                ps[p].m_NodeId_DepVarPos.Add(NowNode, DepVarPos);
                                DepVarPos++;
                            }

                        }
                        
                        // waiting node only associated with the transfer node 
                        // and exclude the destination node
                        for (int s=0;s<ps[p].TranferNodes.Count;s++)
                        {
                            NowNode = ps[p].TranferNodes[s];
                            if (NowNode!=ps[p].VisitNodes[ps[p].VisitNodes.Count-1])
                            {
                                ps[p].m_NodeId_WaitVarPos.Add(NowNode, WaitVarPos);
                                WaitVarPos++;
                            }
                        }

                        for (int s=0;s<ps[p].VisitNodes.Count;s++)
                        {
                            NowNode = ps[p].VisitNodes[s];
                            if (ps[p].m_NodeID_NextLine.ContainsKey(NowNode))
                            {
                                NextLineID = ps[p].m_NodeID_NextLine[NowNode].ID;
                                if (Lines[NextLineID].ServiceType == TransitServiceType.Frequency)
                                {
                                    continue;
                                }
                                else if (Lines[NextLineID].ServiceType.Equals(TransitServiceType.Schedule))
                                {
                                    if (ps[p].TranferNodes.Contains(NowNode))
                                        // if it is a transfer and boarding node
                                    {
                                        ps[p].m_NodeId_DeltaTrainBoard.Add(NowNode, DeltaPathTrainPos);
                                        DeltaPathTrainPos += Lines[NextLineID].NumOfTrains;
                                    }
                                    else
                                    {
                                        ps[p].m_NodeId_DeltaTrainBoard.Add(NowNode, DeltaPathTrainPos - Lines[NextLineID].NumOfTrains);

                                    }
                                }
                            }
  
                        }
                        #region unknown and forget the purpose of the followng code
                        //if (ps[p].TranferNodes.Contains(NowNode))
                        //{
                        //    ps[p].m_NodeId_WaitVarPos.Add(NowNode, WaitVarPos);
                        //    WaitVarPos++;
                        //}

                        //int NowNode = ps[p].VisitNodes[ps[p].VisitNodes.Count - 1];
                        //ps[p].m_NodeId_ArrVarPos.Add(NowNode, ArrVarPos);
                        //ArrVarPos++;
                        //ps[p].m_NodeId_DepVarPos.Add(NowNode, DepVarPos);
                        //DepVarPos++;

                        //int NextLineID = PARA.NULLINT;
                        //for (int ls = 2; ls <= ps[p].VisitNodes.Count; ls++)
                        //{
                        //    NowNode = ps[p].VisitNodes[ps[p].VisitNodes.Count - ls];
                        //    // add and map wait, arrival and dep variables
                        //    // wait variable only associated with the transfer nodes
                        //    //if (ps[p].TranferNodes.Contains(NowNode))
                        //    //{
                        //    //    ps[p].m_NodeId_WaitVarPos.Add(NowNode, WaitVarPos);
                        //    //    WaitVarPos++;
                        //    //}

                        //    //ps[p].m_NodeId_ArrVarPos.Add(NowNode, ArrVarPos);
                        //    //ArrVarPos++;

                        //    //if (ls <= ps[p].VisitNodes.Count)
                        //    //{
                        //    //    ps[p].m_NodeId_DepVarPos.Add(NowNode, DepVarPos);
                        //    //    DepVarPos++;
                        //    //}
                        //    //NextLineID = ps[p].m_NodeID_NextLine[NowNode].ID;

                        //    //if (Lines[NextLineID].ServiceType == TransitServiceType.Frequency)
                        //    //{
                        //    //    continue;
                        //    //}
                        //    //else if (Lines[NextLineID].ServiceType.Equals(TransitServiceType.Schedule))
                        //    //{
                        //    //    ps[p].m_NodeId_DeltaTrainBoard.Add(NowNode, DeltaPathTrainPos);
                        //    //    DeltaPathTrainPos += Lines[NextLineID].NumOfTrains;
                        //    //}
                        //}
                        #endregion
                        /**************************************************************/

                        List<int> myKeys = ps[p].m_NodeID_NextLine.Keys.ToList();
                        ///<remarks>
                        ///sch/fre capacity cost are made associated at each boarding node
                        ///</remarks>

                        for (int n = myKeys.Count-1; n >=0; n--)
                        {
                            if (Lines[ps[p].m_NodeID_NextLine[myKeys[n]].ID].ServiceType.Equals(TransitServiceType.Schedule))
                            {
                                ps[p].m_NodeId_SchCapCostVarPos.Add(myKeys[n], NodeId_SchCapPos);
                                NodeId_SchCapPos++;
                            }
                            if (Lines[ps[p].m_NodeID_NextLine[myKeys[n]].ID].ServiceType.Equals(TransitServiceType.Frequency))
                            {
                                ps[p].m_NodeId_FreCapCostVarPos.Add(myKeys[n], NodeId_FreCapPos);
                                NodeId_FreCapPos++;
                            }
                        }

                        for (int n = 0; n < myKeys.Count; n++)
                        {
                            if (ps[p].m_NodeID_NextLine[myKeys[n]].ServiceType.Equals(TransitServiceType.Frequency))
                            {
                                //if (myKeys[n] == ps[p].VisitNodes[ps[p].VisitNodes.Count() - 1]) continue;
                                ps[p].m_Delta_FreDep_t_pos.Add(myKeys[n], Delta_FreDep_t_Pos);
                                Delta_FreDep_t_Pos += (int)PARA.DesignPara.MaxTimeHorizon;  // number of time intervals, so if is less than one. i.e.,if t=0,1, the interval is only 1. 
                            }
                        }

                        /// arrival position  
                        
                        if (Delta_FreDep_t_Pos > 0)  // only need arrival position when the 
                        {
                            for (int n = 0; n < myKeys.Count; n++)
                            {
                                if (myKeys[n] == ps[p].VisitNodes[0]) continue;
                                ps[p].m_Delta_Arr_t_pos.Add(myKeys[n], Delta_FreArr_t_Pos);
                                Delta_FreArr_t_Pos += (int)PARA.DesignPara.MaxTimeHorizon;
                            }
                            // add arrival position associated with the last node 
                            int LastArrNode = ps[p].Trip.DestID;
                            Debug.Assert(LastArrNode == ps[p].VisitNodes[ps[p].VisitNodes.Count - 1]);

                            ps[p].m_Delta_Arr_t_pos.Add(LastArrNode, Delta_FreArr_t_Pos);
                            Delta_FreArr_t_Pos += (int)PARA.DesignPara.MaxTimeHorizon;
                        }
                    }


                    // map seat variable position
                    LineSec_Delta_Seat_Pos = 0;
                    for (int p = 0; p < ps.Count(); p++)
                    {
                        ps[p].getLineId_VisitNodeOrder();
                        List<int> LineKeys = new List<int>(ps[p].m_LineId_VistNodeOrder.Keys);
                        //LineID2DeltaSeat temp = new LineID2DeltaSeat();
                        for (int i = 0; i < LineKeys.Count; i++)
                            ps[p].m_LineNodeID_DeltaSeatPos.Add(new LineID2DeltaSeat(LineKeys[i]));
                        for (int i = 0; i < LineKeys.Count; i++)
                        {
                            int NumStop = ps[p].m_LineId_VistNodeOrder[LineKeys[i]].Count-1;
                            int l_index = ps[p].m_LineNodeID_DeltaSeatPos.FindIndex(x => x.LineID == LineKeys[i]);
                            for (int s=0;s<NumStop;s++)
                            {
                                int nowstop = ps[p].m_LineId_VistNodeOrder[LineKeys[i]][s];

                                ps[p].m_LineNodeID_DeltaSeatPos[l_index].m_BoardNodeId_DeltaPos.Add(nowstop, LineSec_Delta_Seat_Pos);
                                LineSec_Delta_Seat_Pos++;
                            }
                        }
                    }
                }


                /// <summary>
                ///   create path set data from dominate event trees
                /// </summary>
                /// <param name="Network"></param>
                /// <param name="LpPathSet"></param>
                /// <returns></returns>
                public static void CreatPathSets(NetworkClass Network, List<LpPath> LpPathSet)
                {

                    int count = 0;
                    int NowEventID;
                    for (int i = 0; i < Network.UniOrigins.Count(); i++)
                    {
                        // step 1: set path set 
                        for (int j = 0; j < Network.UniOrigins[i].IncludeTrips.Count(); j++)
                        {
                            NowEventID = Network.Trips[Network.UniOrigins[i].IncludeTrips[j]].FirstNonDomEventID;
                            do
                            {
                                if(isEffectivePath(Network, NowEventID))
                                {
                                    LpPathSet.Add(new LpPath());
                                    LpPathSet[count].AddNewPath(Network, NowEventID, count);
                                    LpPathSet[count].EventPie = Network.Events[NowEventID].Cost;
                                    LpPathSet[count].EventTime = Network.Events[NowEventID].Time;
                                    LpPathSet[count].TargetDepTime = Network.Trips[Network.UniOrigins[i].IncludeTrips[j]].TargetDepTime;
                                    LpPathSet[count].Trip = Network.Trips[Network.UniOrigins[i].IncludeTrips[j]];
                                    count++;
                                }
                                NowEventID = Network.Events[NowEventID].LateNonDomEventID;

                            } while (NowEventID != PARA.NULLINT);
                        }
                    }

                    // step 2: set var position masticated with path set
                    PathSet2VarPos(ref LpPathSet, Network.Lines);

                    // update whether a transit line is involved in the decision variables

                    for (int l=0;l<Network.Lines.Count();++l)
                    {
                        Network.Lines[l].isInvolvedInDecsionVar = false;
                    }
                    for (int p=0;p<LpPathSet.Count();p++)
                    {
                        List<int> Thiskeys = LpPathSet[p].m_NodeID_NextLine.Keys.ToList();
                        for (int k = 0; k < Thiskeys.Count; k++)
                        {
                            LpPathSet[p].m_NodeID_NextLine[Thiskeys[k]].isInvolvedInDecsionVar = true;
                        }
                    }





            }

            /// <summary>
            /// Check whether the path is identical to the target path
            /// </summary>
            /// <param name="Target"></param>
            /// <returns></returns>
            public bool isEqualToPath(LpPath Target)
                {
                    bool IsEqual = true;
                    if (VisitNodes.Count != Target.VisitNodes.Count) return false;

                    List<int> Thiskeys = m_NodeID_NextLine.Keys.ToList();
                    List<int> TargetKeys = Target.m_NodeID_NextLine.Keys.ToList();

                    for (int k=0;k<Thiskeys.Count;k++)
                    {
                        if (Thiskeys[k] != TargetKeys[k]) return false;
                        if (m_NodeID_NextLine[Thiskeys[k]] != Target.m_NodeID_NextLine[TargetKeys[k]]) return false;
                    }
                    return IsEqual;
                }
            }
        }
    }
}