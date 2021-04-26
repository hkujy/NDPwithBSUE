// Checked 2021-May
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace IOPT
{
    public partial class BBmain
    {
        public partial class LpInput
        {
            public class CongestLinkMap
            {
                public int LineId { get; set; }
                public List<int> TailList { get; set; }
                public List<int> HeadList { get; set; }
                public List<int> PosList { get; set; }
                public CongestLinkMap(int Id)
                {
                    LineId = Id;
                    TailList = new List<int>(); HeadList = new List<int>(); PosList = new List<int>();
                }
            }
       
            protected internal List<double> HeadwayUb { get; set; }  // upper and lower bound for each transit line
            protected internal List<double> HeadwayLb { get; set; }
            protected internal List<double> MinEventPie { get; set; }
            protected internal List<double> MaxEventPie { get; set; }
            protected internal int NumFreLines { get; set; }
            protected internal int NumSchLines { get; set; }
            protected internal int NumOfPath { get; set; }
            protected internal double UsedFleet { get; set; }
            protected internal double MaxDemand { get; set; }
            protected internal int WaitVarPos { get; set; }
            protected internal int LineSec_Delta_SeatPos { get; set; } // used in the seat capacity constraints
            protected internal int UsedLinkPos { get; set; } // map how many links are used
            protected internal int DepVarPos { get; set; }
            protected internal int ArrVarPos { get; set; }
            protected internal int TrainVarPos { get; set; }
            protected internal int DeltaPathTrainPos { get; set; }
            protected internal int Delta_FreDep_t_Pos { get; set; } // arrival delta used in the frequency constraint 
            protected internal int Delta_FreArr_t_Pos { get; set; } // arrival delta used in the frequency constraint 
            protected internal int Node_SchCapPos { get; set; } // only related to the transfer board node
            protected internal int Node_FreCapPos { get; set; }// only related to the transfer board node
            protected internal int TotalNumOfTrains { get; set; }
            protected internal List<TransitLineClass> FreLineSet { get; set; }
            protected internal List<TransitLineClass> SchLineSet { get; set; }
            protected internal List<LpPath> PathSet;
            protected internal List<List<int>> TripPathSet { get; set; }
            protected internal List<double> PieUb { get; set; }
            protected internal List<double> PieLb { get; set; }
            protected internal List<double> ProbLb { get; set; }
            protected internal List<double> ProbUb { get; set; }
            protected internal Dictionary<int, int> m_SchLineId_TrainTerminalDepVarPos { get; set; } // 
            protected internal Dictionary<int, int> m_FreLineId_HeadwayVarPos { get; set; }// consider number of train
            protected internal List<CongestLinkMap> m_LineLink_CapCongestStatusPos { get; set; }
            protected internal List<CongestLinkMap> m_Link_Delta_Congest { get; set; }
            protected internal int CongStausPos { get; set; }
            protected internal double getMinPieOfPath(int _p)
            {
                return PathSet[_p].Trip.MinPie; 
            }
            protected internal double getMaxPieOfPath(int _p)
            {
                return PathSet[_p].Trip.MaxPie;
            }
            public LpInput()
            {
                TripPathSet = new List<List<int>>();
                PathSet = new List<LpPath>();
                FreLineSet = new List<TransitLineClass>();
                SchLineSet = new List<TransitLineClass>();
                m_SchLineId_TrainTerminalDepVarPos = new Dictionary<int, int>();// 
                m_FreLineId_HeadwayVarPos = new Dictionary<int, int>();// consider number of train
                m_LineLink_CapCongestStatusPos = new List<CongestLinkMap>();
                m_Link_Delta_Congest = new List<CongestLinkMap>();
                UsedLinkPos = 0;
                CongStausPos = 0;
                PieUb = new List<double>();
                PieLb = new List<double>();
                ProbLb = new List<double>();
                ProbUb = new List<double>();
                HeadwayUb = new List<double>();
                HeadwayLb = new List<double>();
                MinEventPie = new List<double>();
                MaxEventPie = new List<double>();
            }
            protected internal int GetSchDewllDimension()
            {
                int dim = 0;
                for (int l =0;l<SchLineSet.Count;l++)
                {
                    // condition 0: if only two stop, i.e. (start, end) then it is not considered
                    if (SchLineSet[l].Stops.Count == 2) continue;
                    for (int s = 1; s<SchLineSet[l].Stops.Count-1;s++)
                    {
                        for (int q = 0; q < SchLineSet[l].NumOfTrains; q++)
                        {
                            dim++;
                        }
                    }
                }
                return dim; 
            }
            /// <summary>
            /// lid is the "True line id"
            /// sid is the count index in the stops, not the corresponding node index
            /// qid is the counter of the train
            /// *Important Remark*
            /// if the returned value is -1, then it means that there is no corresponding expr value
            /// </summary>
            protected internal int GetSchDwellExpIndex(int lid, int sid, int qid)
            {
                int pos = 0;
                for (int l = 0;l<SchLineSet.Count;l++)
                {
                    if (SchLineSet[l].Stops.Count == 2) continue;
                    for (int s = 1;s<SchLineSet[l].Stops.Count-1;s++)
                    {
                        for(int q = 0;q<SchLineSet[l].NumOfTrains;q++)
                        {
                            if(SchLineSet[l].ID == lid && s==sid && q==qid)
                            {
                                return pos;
                            }
                            else
                            {
                                pos++;
                            }
                        }
                    }
                }
                return -1;
            }

    
            /// <summary>
            /// get the dimension of the dwell time for the frequency based lines
            /// </summary>
            protected internal int GetFreDwellDimension()
            {
                int dim = 0;
                for (int l = 0; l < FreLineSet.Count; l++)
                {
                    for (int s = 1; s < FreLineSet[l].Stops.Count; s++)
                    {
                        for (int q = 0; q < PARA.IntervalSets.Count; q++)
                        {
                            dim++;
                        }
                    }
                }
                return dim;
            }

            /// <summary>
            /// get the index for the index location of the dwell time var for the frequency based 
            /// lid =  is the real "line id"
            /// sid = is the not the id, but rather the order of the stop count
            /// tauID is the tau interval
            /// </summary>
            protected internal int GetFreDwellExpIndex(int lid, int sid, int tauId)
            {
                int pos = 0;
                for (int l = 0; l < FreLineSet.Count; l++)
                {
                    for (int s = 1; s < FreLineSet[l].Stops.Count; s++)
                    {
                        for (int q = 0; q < PARA.IntervalSets.Count; q++)
                        {
                            if (FreLineSet[l].ID == lid && s == sid && q == tauId)
                            {
                                return pos;
                            }
                            else
                            {
                                pos++;
                            }
                        }
                    }
                }
                return -1;
            }

            protected internal int GetPathDwellDemension()
            {
                int cc = 0;
                for (int p = 0; p < PathSet.Count(); p++)
                {
                    for (int s = 0; s < PathSet[p].VisitNodes.Count() - 1; s++)
                    {
                        int currentnode = PathSet[p].VisitNodes[s];
                        bool isTerminal = false;
                        int BoardLineID = PathSet[p].m_NodeID_NextLine[currentnode].ID;
                        foreach (TransitLineClass l in FreLineSet)
                        {
                            if (BoardLineID == l.ID)
                            {
                                if (l.Stops[0].ID == currentnode)
                                    isTerminal = true;
                            }
                        }
                        foreach (TransitLineClass l in SchLineSet)
                        {
                            if (BoardLineID == l.ID)
                            {
                                if (l.Stops[0].ID == currentnode)
                                    isTerminal = true;
                            }
                        }
                        if (!isTerminal)
                        {
                            cc++;
                        }
                    }
                }
                return cc;
            }
            protected internal int GetPathDwellIndex(int pid, int vn)
            {
                // step 1: loop the frequency based lines
                int cc = 0;
                for (int p = 0; p < PathSet.Count(); p++)
                {
                    for (int s = 0; s < PathSet[p].VisitNodes.Count() - 1; s++)
                    {
                        int currentnode = PathSet[p].VisitNodes[s];
                        bool isTerminal = false;
                        int BoardLineID = PathSet[p].m_NodeID_NextLine[currentnode].ID;
                        foreach (TransitLineClass l in FreLineSet)
                        {
                            if (BoardLineID == l.ID)
                            {
                                if (l.Stops[0].ID == currentnode)
                                    isTerminal = true;
                            }
                        }
                        foreach (TransitLineClass l in SchLineSet)
                        {
                            if (BoardLineID == l.ID)
                            {
                                if (l.Stops[0].ID == currentnode)
                                    isTerminal = true;
                            }
                        }
                        if (!isTerminal)
                        {
                            if (vn == currentnode && p==pid)
                            {
                                return cc;
                            }
                            cc++;
                        }
                    }
                }
                return -1;
            }
            protected internal int GetTrainDepDemsion()
            {
                int dim = 0;
                for (int l =0; l<SchLineSet.Count;l++)
                {
                    for (int s = 0;s<SchLineSet[l].Stops.Count-1;s++)
                    {
                        for (int q= 0;q<SchLineSet[l].NumOfTrains;q++)
                        {
                            dim++;
                        }
                    }
                }
                return dim;
            }
            /// <summary>
            /// lorder: is the order in the schedule line set
            /// sorder: is the stop order
            /// qorder: is the train index
            /// </summary>
            protected  internal int GetTrainDepIndex(int lorder,int sorder,int qorder)
            {
                int dim = 0;
                for (int l = 0; l < SchLineSet.Count; l++)
                {
                    for (int s = 0; s < SchLineSet[l].Stops.Count - 1; s++)
                    {
                        for (int q = 0; q < SchLineSet[l].NumOfTrains; q++)
                        {
                            if (l == lorder && sorder == s && q == qorder)
                            {
                                return dim;
                            }
                            else
                            {
                                dim++;
                            }
                        }
                    }
                }

                return dim;
            }

            /// <summary>
            /// set the upper and lower bound value for the pie and headway 
            /// the bound values are read from para.input
            /// </summary>
            protected internal void SetVarBounds()
            {
                for (int l = 0; l < NumFreLines; l++)
                {
                    HeadwayUb.Add(PARA.DesignPara.MaxHeadway);
                    HeadwayLb.Add(PARA.DesignPara.MinHeadway);
                }
                for (int p = 0; p < NumOfPath; p++)
                {
                    double MinPieVal = getMinPieOfPath(p);
                    double MaxPieVal = getMaxPieOfPath(p);
                    PieLb.Add(MinPieVal);
                    if (MaxEventPie.Count()==0)
                        PieUb.Add(MaxPieVal);
                    else
                    {
                        if (PARA.DesignPara.ConstantPathCostBound)
                        {
                            PieUb.Add(MaxPieVal);
                        }
                        else
                        {
                            PieUb.Add(MaxEventPie[PathSet[p].Trip.ID]);
                        }
                    }
                    ProbLb.Add(PARA.DesignPara.MinProb);
                    ProbUb.Add(PARA.DesignPara.MaxProb);
                }
            }

            /// <summary>
            /// set map line ID to decision variables
            /// </summary>
            protected internal void SetMapDict()
            {
                int CumCount = 0; // cumulative count
                for (int l = 0; l < SchLineSet.Count; l++)
                {
                    m_SchLineId_TrainTerminalDepVarPos.Add(SchLineSet[l].ID, CumCount);
                    CumCount += SchLineSet[l].NumOfTrains;
                }
                CumCount = 0;
                for (int l = 0; l < FreLineSet.Count; l++)
                {
                    m_FreLineId_HeadwayVarPos.Add(FreLineSet[l].ID, CumCount);
                    CumCount++;
                }
            }
            /// <summary>
            /// Print active set of path for the Branch and bound problem
            /// </summary>
            protected internal void PrintPasPathSet (string FileName)
            {
                using (StreamWriter file = new StreamWriter(FileName, true))
                {
                    for (int p = 0; p < NumOfPath; p++)
                    {
                        file.Write("I={0},P={1},", Global.NumOfIter,  p);
                        for (int i = 0; i < PathSet[p].VisitNodes.Count; i++)
                        {
                            int nodeID = PathSet[p].VisitNodes[i];
                            file.Write("Trip={0},Node={1},",PathSet[p].Trip.ID, PathSet[p].VisitNodes[i]);
                            if (i != PathSet[p].VisitNodes.Count - 1)
                            {
                                int BoardLineID = PathSet[p].m_NodeID_NextLine[nodeID].ID;
                                if (PathSet[p].TranferNodes.Contains(nodeID)) file.Write("Wait," );
                                else file.Write("on board,");
                                int indexline = SchLineSet.FindIndex(x => x.ID == BoardLineID);
                                if (indexline >= 0) file.Write("Board={0},", SchLineSet[indexline].ID);
                                indexline = FreLineSet.FindIndex(x => x.ID == BoardLineID);
                                if (indexline >= 0) file.Write("Board={0},", FreLineSet[indexline].ID);
                            }
                        }
                        file.Write(Environment.NewLine);
                    }
                }
            }

            /// <summary>
            /// compare whether two paths set are equal
            /// </summary>
            protected internal bool LpPathSetIsEqual(LpInput Target, ref List<int> AddNewPath)
            {
                AddNewPath.Clear();
                bool isEqual = true;
                for (int t = 0; t < TripPathSet.Count; t++)  // for each trip od pair
                {
                    for (int i = 0; i < TripPathSet[t].Count; i++)
                    {
                        bool match = false;
                        for (int j = 0; j < Target.TripPathSet[t].Count; j++)
                        {
                            if (PathSet[TripPathSet[t][i]].isEqualToPath(Target.PathSet[Target.TripPathSet[t][j]]))
                            {
                                match = true;
                                break;
                            }
                        }
                        if (!match)
                        {
                            isEqual = false;
                            AddNewPath.Add(TripPathSet[t][i]);
                        };
                    }
                }
                return isEqual;
            }
            /// <summary>
            /// From input data to set value for the solution method
            /// </summary>
            protected internal void SetValue(NetworkClass Network)
            {
                if (PathSet.Count == 0)
                {
                    // if there is no paths, then first create the path
                    LpPath.CreatPathSets(Network, PathSet);
                }
                else
                {
                    // otherwise, first clear the paths
                    Clear(clearPathSet: false);
                    LpPath.PathSet2VarPos(ref PathSet, Network.Lines);
                }
                CreatVarPos(Network.Trips, Network.Lines);
                MaxDemand = Global.DemandScale* Network.Trips.Max(x => x.Demand);
                if (Global.NumOfIter == 0)
                {
                    for (int t = 0; t < TripPathSet.Count; t++)
                    {
                        List<double> PieList = new List<double>();
                        for (int p = 0; p < TripPathSet[t].Count; p++)
                        {
                            PieList.Add(PathSet[TripPathSet[t][p]].EventPie);
                        }
                    }
                }
            }
            /// <summary>
            /// remove specified path set
            /// </summary>
            protected internal void RemovePathSetValue(List<int> RemoveSet)
            {
                if (RemoveSet.Count == 0) return;
                PathSet.RemoveAll(x => RemoveSet.Contains(x.ID));
            }

            /// <summary>
            /// Create Variable Post 
            /// </summary>
            protected internal void CreatVarPos(List<TripClass> Trips, List<TransitLineClass> Lines)
            {
                WaitVarPos = LpPath.WaitVarPos;
                DepVarPos = LpPath.DepVarPos;
                ArrVarPos = LpPath.ArrVarPos;
                TrainVarPos = LpPath.TrainVarPos;
                DeltaPathTrainPos = LpPath.DeltaPathTrainPos;
                Delta_FreDep_t_Pos = LpPath.Delta_FreDep_t_Pos; // arrival delta used in the frequency constraint 
                Delta_FreArr_t_Pos = LpPath.Delta_FreArr_t_Pos;
                Node_SchCapPos = LpPath.NodeId_SchCapPos;
                Node_FreCapPos = LpPath.NodeId_FreCapPos;
                LineSec_Delta_SeatPos = LpPath.LineSec_Delta_Seat_Pos;
                for (int i = 0; i < Trips.Count(); i++)
                {
                    TripPathSet.Add(new List<int>());
                }
                for (int t = 0; t < Trips.Count; t++)
                {
                    for (int p = 0; p < PathSet.Count; p++)
                    {
                        if (PathSet[p].Trip.ID == Trips[t].ID)
                        {
                            TripPathSet[t].Add(p);
                        }
                    }
                }
                NumFreLines = 0;
                NumSchLines = 0;
                TotalNumOfTrains = 0;
                for (int l = 0; l < Lines.Count; l++)
                {
                    if (Lines[l].ServiceType == TransitServiceType.Frequency)
                    {
                        FreLineSet.Add(Lines[l]);
                        NumFreLines++;
                        if ( PARA.DesignPara.MinHeadway- Lines[l].Headway > PARA.GeZero)
                        {
                            Console.WriteLine("LpInput_Warning: Input headway must be greater than the minimum headway");
                            Console.WriteLine("File: Lp_input.cs: CreatVarPos");
                            Console.WriteLine("Press any key to continue");
                            Console.ReadLine();
                        }
                    }
                    else if (Lines[l].ServiceType == TransitServiceType.Schedule)
                    {
                        SchLineSet.Add(Lines[l]);
                        NumSchLines++;
                        TotalNumOfTrains += Lines[l].NumOfTrains;
                    }
                    else
                        MyLog.Instance.Debug("Transit service type is NULL");
                }

                NumOfPath = PathSet.Count();
                SetVarBounds();
                SetMapDict();
                getUsedLinkMap();
                getCongestionMap();
            }

            protected internal bool ContaiKey(Dictionary<Dictionary<int, Dictionary<int, int>>, int> m_UsedLink2CapStatus,
                                        Dictionary<int, Dictionary<int, int>> tt)
            {
                List<Dictionary<int, Dictionary<int, int>>> Tkeys = m_UsedLink2CapStatus.Keys.ToList();
                List<int> tempKey = tt.Keys.ToList();
                List<int> k2 = tt[tempKey[0]].Keys.ToList();
                for (int i = 0; i < Tkeys.Count; i++)
                {
                    if (Tkeys[i].ContainsKey(tempKey[0]))
                    {
                        if (Tkeys[i][tempKey[0]].ContainsKey(k2[0]))
                        {
                            if (Tkeys[i][tempKey[0]][k2[0]] == tt[tempKey[0]][k2[0]])
                                return true;
                        }
                    }
                }
                return false;
            }

            protected internal void getUsedLinkMap()
            {
                for (int l = 0; l < FreLineSet.Count; l++) m_LineLink_CapCongestStatusPos.Add(new CongestLinkMap(FreLineSet[l].ID));
                for (int l = 0; l < SchLineSet.Count; l++) m_LineLink_CapCongestStatusPos.Add(new CongestLinkMap(SchLineSet[l].ID));
                UsedLinkPos = 0;
                for (int p = 0; p < PathSet.Count; p++)
                {
                    for (int s = 0; s < PathSet[p].VisitNodes.Count - 1; s++)
                    {
                        int startNode = PathSet[p].VisitNodes[s];
                        int endNode = PathSet[p].VisitNodes[s + 1];
                        int lineId = PathSet[p].m_NodeID_NextLine[startNode].ID;

                        int l_lindex = m_LineLink_CapCongestStatusPos.FindIndex(x => x.LineId == lineId);
                        bool newuselink = true;
                        for (int kk = 0; kk < m_LineLink_CapCongestStatusPos[l_lindex].TailList.Count; kk++)
                        {
                            if (m_LineLink_CapCongestStatusPos[l_lindex].TailList[kk] == startNode && m_LineLink_CapCongestStatusPos[l_lindex].HeadList[kk] == endNode)
                            {
                                newuselink = false;
                                break;
                            }
                        }
                        if (newuselink)
                        {
                            m_LineLink_CapCongestStatusPos[l_lindex].TailList.Add(startNode);
                            m_LineLink_CapCongestStatusPos[l_lindex].HeadList.Add(endNode);
                            m_LineLink_CapCongestStatusPos[l_lindex].PosList.Add(UsedLinkPos);
                            UsedLinkPos++;
                        }
                    }
                }
            }

            protected internal void getCongestionMap()
            {
                CongStausPos = 0;
                int LineID;
                int nowStop, nextStop;
                for (int l = 0; l < FreLineSet.Count; l++) m_Link_Delta_Congest.Add(new CongestLinkMap(FreLineSet[l].ID));
                for (int l = 0; l < SchLineSet.Count; l++) m_Link_Delta_Congest.Add(new CongestLinkMap(SchLineSet[l].ID));

                for (int l = 0; l < FreLineSet.Count; l++)
                {
                    LineID = FreLineSet[l].ID;
                    int l_index = m_Link_Delta_Congest.FindIndex(x => x.LineId == LineID);
                    for (int s = 0; s < FreLineSet[l].Stops.Count - 1; s++)
                    {
                        nowStop = FreLineSet[l].Stops[s].ID;
                        nextStop = FreLineSet[l].Stops[s + 1].ID;
                        if (!(m_Link_Delta_Congest[l_index].TailList.Contains(nowStop)
                            && m_Link_Delta_Congest[l_index].HeadList.Contains(nextStop)))
                        {
                            m_Link_Delta_Congest[l_index].TailList.Add(nowStop);
                            m_Link_Delta_Congest[l_index].HeadList.Add(nextStop);
                            m_Link_Delta_Congest[l_index].PosList.Add(CongStausPos);
                            CongStausPos += PARA.IntervalSets.Count;  // created for each interval
                        }
                    }
                }

                for (int l = 0; l < SchLineSet.Count; l++)
                {
                    LineID = SchLineSet[l].ID;
                    for (int s = 0; s < SchLineSet[l].Stops.Count - 1; s++)
                    {
                        nowStop = SchLineSet[l].Stops[s].ID;
                        nextStop = SchLineSet[l].Stops[s + 1].ID;
                        int l_index = m_Link_Delta_Congest.FindIndex(x => x.LineId == LineID);
                        if (!(m_Link_Delta_Congest[l_index].TailList.Contains(nowStop)
                          && m_Link_Delta_Congest[l_index].HeadList.Contains(nextStop)))
                        {
                            m_Link_Delta_Congest[l_index].TailList.Add(nowStop);
                            m_Link_Delta_Congest[l_index].HeadList.Add(nextStop);
                            m_Link_Delta_Congest[l_index].PosList.Add(CongStausPos);
                            CongStausPos += SchLineSet[l].NumOfTrains;
                        }
                    }
                }
            }

            protected internal void Copy(LpInput Target)
            {
                Clear(clearPathSet: true);
                for (int i = 0; i < Target.HeadwayUb.Count; i++) { HeadwayUb.Add(Target.HeadwayUb[i]); HeadwayLb.Add(Target.HeadwayLb[i]); }
                for (int p = 0; p < Target.ProbLb.Count; p++) { ProbLb.Add(Target.ProbLb[p]); ProbUb.Add(Target.ProbUb[p]); }
                for (int p = 0; p < Target.PieUb.Count; p++) { PieLb.Add(Target.PieLb[p]); PieUb.Add(Target.PieUb[p]); }
                NumFreLines = Target.NumFreLines;
                NumSchLines = Target.NumSchLines;
                NumOfPath = Target.NumOfPath;
                UsedFleet = Target.UsedFleet;
                WaitVarPos = Target.WaitVarPos;
                DepVarPos = Target.DepVarPos;
                ArrVarPos = Target.ArrVarPos;
                TrainVarPos = Target.TrainVarPos;
                DeltaPathTrainPos = Target.DeltaPathTrainPos;
                Delta_FreDep_t_Pos = Target.Delta_FreDep_t_Pos;
                Delta_FreArr_t_Pos = Target.Delta_FreArr_t_Pos;
                Node_FreCapPos = Target.Node_FreCapPos;
                Node_SchCapPos = Target.Node_SchCapPos;
                TotalNumOfTrains = Target.TotalNumOfTrains;
                m_SchLineId_TrainTerminalDepVarPos = new Dictionary<int, int>(Target.m_SchLineId_TrainTerminalDepVarPos);
                m_FreLineId_HeadwayVarPos = new Dictionary<int, int>(Target.m_FreLineId_HeadwayVarPos);
                m_LineLink_CapCongestStatusPos = new List<CongestLinkMap>();
                m_Link_Delta_Congest = new List<CongestLinkMap>();
                for (int i = 0; i < Target.TripPathSet.Count; i++) { TripPathSet.Add(new List<int>(Target.TripPathSet[i])); }
                for (int p = 0; p < Target.FreLineSet.Count; p++) FreLineSet.Add(Target.FreLineSet[p]);
                for (int p = 0; p < Target.SchLineSet.Count; p++) SchLineSet.Add(Target.SchLineSet[p]);
                for (int p = 0; p < Target.PathSet.Count; p++) PathSet.Add(Target.PathSet[p]);
            }

            protected internal void Clear(bool clearPathSet)
            {
                HeadwayLb.Clear(); HeadwayUb.Clear();
                NumFreLines = -1; NumOfPath = -1; UsedFleet = -1;
                WaitVarPos = -1; DepVarPos = -1; ArrVarPos = -1; TrainVarPos = -1; DeltaPathTrainPos = -1;
                Delta_FreDep_t_Pos = -1; Node_SchCapPos = -1; Node_FreCapPos = -1;
                Delta_FreArr_t_Pos = -1;
                TotalNumOfTrains = -1;
                FreLineSet.Clear();
                SchLineSet.Clear();
                TripPathSet.Clear();
                PieUb.Clear(); PieLb.Clear();
                ProbLb.Clear(); ProbUb.Clear();
                m_SchLineId_TrainTerminalDepVarPos.Clear();
                m_FreLineId_HeadwayVarPos.Clear();
                m_LineLink_CapCongestStatusPos.Clear();
                m_Link_Delta_Congest.Clear();
                if (clearPathSet) PathSet.Clear();
            }
        }
    }
}
