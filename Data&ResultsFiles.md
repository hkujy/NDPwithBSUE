# Readme 
1. Instruction for the data files
2. If the name of the parameter is self-explanatory, it will not be further
   explained
## Input Folder 
1. DesignPara.csv
- MaxTimeHorizon: 1 stands for minute
- MaxLineOperTtime: Operation time for the transit lines. 
    * this is shorter than the MaxTimeHorizon
    * Because it relates to the departure time of the vehicles. 
    * A passenger boards a bus depart at 60 could arrive at destination at 90
- MaxHeadway: for both frequency and schedule based services
- MinHeadway: for both frequency and schedule based services
- Fleetsize
    * this is not used in the revised version, so its value is set to be -1
- BigM: big M value for the constraints 

2. Lines.csv
- contains the initial input for the lines 
- Doors. used in computing boarding and alighting 
    * 100 cap corresponds to 1 door
3. LineStop.csv
- record the sequence of the stops of a line
- the first number is the line id
- example "0,0,1,2,3"
> line 0, passes stops 0, 1, 2, 3

4. LineTime.csv
- the travel time between two consecutive stops 
- the first number is the line id 
- example "0,5,4,3"
> line 0, the travel times of the 1st, 2nd, and 3rd links are 5, 4, 3

5. Nodes.csv
- node information
- map node id to string node name

6. ParaPath.csv
- coefficient used for computing path cost
- WalkW: Weight for walking time
- WaitW: Weight for waiting time 
- ConW:  Weight for congestion 
- TransferP: Transfer penalty 
- InVTrainW: Invehicle value for train
- InVBusW: Invehicle value for bus
- InVMetroW: Invehicle value for metro
- TransferTime: minimum Transfer Time

7. SchDep.csv
- Schedule for transit lines
- the first number is line id
- the second number is the train/vehicle id
- the third number is departure time 
- exampe "1, 0, 6"
> for line 1, the 1st vehicle with id=0 departs at time 6

8. Trip.csv
- Demand Input
- In the current setting, the early/late depart/arrival penalties are set to be
  0