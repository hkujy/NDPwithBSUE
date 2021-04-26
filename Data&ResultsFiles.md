# Readme 
1. Instruction for the data the results files
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

## OutPut folder
1. AdjustPara.txt
- record the parameters that can be adjust for the experiment
- BoardAlightTimePerPas
* boarding and alighting time per passenger
* 0.016 corresponds to 1 second 
- NumOfBreakPoints: No. of break points in the linearisation method
- BcmBound: Bound value to determine the bounded choice set 
- Theta: Scaling parameters in the discrete choice model
- CplexTimLim: time limit for cplex computation
* 7200 means 2 hours
- StandBeta: parameter used in computing the congestion cost for standing
  passengers
- SeatBeat: parameter used in computing the congestion cost for seating
  passengers 
- StandConst: parameter used in computing the congestion cost, a constant value
  for standing passengers
- EachInterVal: time interval used for the frequency-based services
* 15 means 15 minutes
- FreOperationCost: operation cost coefficient for the frequency based lines
- MIPRelGap: Gap value used in cplex
- MinProb: lower bound for the frequency 
- MaxProb: upper bound for the frequency 
- EpsObj: convergence values for the objective values obtained from two
  successive iterations
- EpsConstraint: epsilon value for the eps-feasible solution

2. BB_ActivePathSet.txt
- Detailed path information used in the branch and bound method
3. BB_Best_SolNum.txt
- Best solution from the branch and bound method

4. BB_LP_fre.txt
- frequency solution from the branch and bound 

5. BB_LP_PasPath.txt
- Detail path results from the branch and bound

6. BB_LP_PasPath_Data.txt
- Path data in a csv format 

7. BB_LP_Path.txt
- Path probability results from the B&B

8. BB_LP_Sch.txt
- Schedule results from B&B

9. Bcm_PathIter.txt
- Cost for the paths in the bounded choice set

10. GlobalIter.txt
- Convergent of the objective values over iterations




