# The logic for capacity constraints 



## To Implement 
1. Cap_stand : para_stand*max(dif,0)
2. Cap_board : para_Para_board*P_board
3. Cap_seat : 0
4. Cap_stepwise: Cap_stand + cap_board

    if (boarding stop)
        switch (CapCostType)
            case: stand only 
                cap_stand= para_stand * max(dif, 0)
            case: stepwise 
                cap_stepwise = para.seat*p_board
                                + para.stand*max(dif, 0)
    if (not a boarding stop)
        if (consider seat sequence)
        {
            switch (CapCostType)
                case: stand only 
                    if (has a seat): cap = 0
                    else (not has seat) : cap_stand = para.stand*max(dif,0)
                case: stepwise:
                    if (has a seat) : cap=0
                    if (not has a seat): cap_stand = para.stand*max(dif,0)
        }
        else
        {
            switch (CapCostType)
                case: StandOnly:
                    cap_stand=para.stand*max(dif,0)
                case: stepwise:
                    cap_stand=para.stand*max(dif,0)
        }



