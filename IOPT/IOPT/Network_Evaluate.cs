using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOPT
{
    // remark : contains the evaluation function of the network 
    public partial class NetworkClass
    {
       protected internal void getTotalTravelCost()
       {
            // todo: get the total travel cost of the network 
       }
       protected internal void getOpCost()
       {
            // todo: get the operation cost of the networks
       }
       
        protected internal void getTotalCost()
       {
            // todo: get the total cost as the sum of OpCost and travel cost
       }

       protected internal void Evaulate(BBmain.LpInput _LpData, BBmain.SolClass _sol)
       {
            // todo: function to evaluate a solution 

            Loading(_LpData, _sol);
       }
    }
}
