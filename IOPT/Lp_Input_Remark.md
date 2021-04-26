# Remarks on the Lp_intput

# Terms
1. All Nodes: refer to all the nodes
2. Transfer nodes: refer only to transfer node to a differnt line

# Notes on variables 
1. m_NodeId_SchCapCostVarPos
    - include both boarding and continous 

2. Delta_FreDep_t_Pos
    - delta: for the frequency based service add departure time interval

3. m_SchLineId_TrainTerminalDepVarPos
    - record the departure time from the first terminal station, because we
      assume fixed travel time, so the depature time at the following stations
      can be inferred
