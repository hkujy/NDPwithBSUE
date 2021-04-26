# Remark on the results unit 
1. all the unit is in minutes
2. fre  = 0.2 buses / per min  = > 12 buses/ hour => headway = 5 min
3. headway  = 5 min 
# Remarks on the examples



## Example 1
> example is to show the effect of the BCM value on the network design solutions 
it also compares the SUE solution

> The code and results are stored in the folder of results under the IPTOP

## Example 2
> this example plot the effect of seating, the results are stored under the IPTOP results folder
> the input network folder is "SeatCase_sch"
* Checked 15-June-2018

## Example 3 
### Log 
1. It dose not work if directly adjust the 2 line schedule network. Because of the data setting
2. second test it makes a litte sense: I forget that interval parameters is changed in the python main code

3. In the new test, I change it into a mixed case, in which one line is fre and the other line is sch

4. it seems good that i can increase the value of the interval to observe the tradeoff between the total cost and the next step is check the congestion cost detail

5. which is change the value of the interval values

## Example 4
> use three link network to compare the performance with the exact solution method


# Log notes
1. It is noticed the minimum headway, together with the BCM bounded value will also affect the feasiblity of the solution
2. Because the minimum headway will affect the minimum waiting time, the minimum waiting time will affect path cost, which in turn affects the diffference between different paths. Such difference should be less than the BCM value, otherwise there is no feasible solution to the problem
