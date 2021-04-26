import myclass as mc
import pandas as pd
import numpy as np
import os

def read_fre(folder, data:mc.TestCaseClass):
    # file = folder +"BB_LP_Fre.txt"
    file = os.path.join(str(folder), "BB_LP_Fre.txt")
    df = pd.read_csv(file, sep=',', index_col=False)
    num_row = df.shape[0]
    for i in range(0, num_row):
        sol_num = df["SolNum"][i]
        if len(data.sol_list[sol_num].lines) == 0:
            data.sol_list[sol_num].lines.append(mc.LineClass())
            data.sol_list[sol_num].lines[-1].id = df["LineId"][i]
            data.sol_list[sol_num].lines[-1].type = "Fre"
        now_line_id = df["LineId"][i]
        now_fre = df["Fre"][i]
        now_headway = df["Headway"][i]
        add = True
        for l in range(0, len(data.sol_list[sol_num].lines)):
            if data.sol_list[sol_num].lines[l].id == now_line_id:
                add = False
        if add == True:
            data.sol_list[sol_num].lines.append(mc.LineClass())
            data.sol_list[sol_num].lines[-1].id = now_line_id
            data.sol_list[sol_num].lines[-1].type = "Fre"

        for l in range(0, len(data.sol_list[sol_num].lines)):
            if data.sol_list[sol_num].lines[l].id == now_line_id:
                data.sol_list[sol_num].lines[l].fre = now_fre
                data.sol_list[sol_num].lines[l].headway = now_headway


def read_schedule(folder, data: mc.TestCaseClass):
    """
        read bus line schedule
    :param folder:
    :return:
    """
    # file = folder + "BB_LP_Sch.txt"
    file = os.path.join(folder,"BB_LP_Sch.txt" );
    df = pd.read_csv(file, sep=',', index_col=False)
    num_row = df.shape[0]
    for i in range(0, num_row):
        sol_num = df["SolNum"][i]
        if len(data.sol_list[sol_num].lines) == 0:
            data.sol_list[sol_num].lines.append(mc.LineClass())
            data.sol_list[sol_num].lines[-1].id = df["LineId"][i]
            data.sol_list[sol_num].lines[-1].type = "Sch"
        now_line_id = df["LineId"][i]
        now_run_id = df["Train"][i]
        dwell_time = df['Dwell'][i]
        add = True
        for l in range(0, len(data.sol_list[sol_num].lines)):
            if data.sol_list[sol_num].lines[l].id == now_line_id:
                add = False
        if add == True:
            data.sol_list[sol_num].lines.append(mc.LineClass())
            data.sol_list[sol_num].lines[-1].id = now_line_id
            data.sol_list[sol_num].lines[-1].type = "Sch"

        for l in range(0, len(data.sol_list[sol_num].lines)):
            if data.sol_list[sol_num].lines[l].id == now_line_id:
                if now_run_id in data.sol_list[sol_num].lines[l].run_ids:
                    data.sol_list[sol_num].lines[l].runs[now_run_id].stops.append(df["Stop"][i])
                    data.sol_list[sol_num].lines[l].runs[now_run_id].arr_time.append(df["Arr"][i])
                    data.sol_list[sol_num].lines[l].runs[now_run_id].dep_time.append(df["Dep"][i])
                    data.sol_list[sol_num].lines[l].runs[now_run_id].dwell_time.append(dwell_time)
                else:
                    data.sol_list[sol_num].lines[l].runs.append(mc.RunClass())
                    data.sol_list[sol_num].lines[l].runs[-1].stops.append(df["Stop"][i])
                    data.sol_list[sol_num].lines[l].runs[-1].arr_time.append(df["Arr"][i])
                    data.sol_list[sol_num].lines[l].runs[-1].dep_time.append(df["Dep"][i])
                    data.sol_list[sol_num].lines[l].runs[-1].dwell_time.append(df["Dep"][i])
                    data.sol_list[sol_num].lines[l].run_ids.append(now_run_id)


def read_optimal_sol(folder, case_data):
    # file = folder + "BB_Best_SolNum.txt"
    file =os.path.join(folder, "BB_Best_SolNum.txt")
    df = pd.read_csv(file, sep=',', index_col=False)

    num_row = df.shape[0]
    best_sol_val = 100000000000.0
    global_best_sol_index = -1
    for i in range(0, num_row):
        if df["BestSolNum"][i] >= 0:
            iter_index = df["Iter"][i]
            best_sol_index = df["BestSolNum"][i]
            case_data.iter_list[iter_index].best_sol = best_sol_index
            if float(df["BestCplObj"][i]) < best_sol_val:
                global_best_sol_index = best_sol_index
                best_sol_val =float(df["BestCplObj"][i])
                # print("i = {0}, best sol id ={1}".format(i, best_sol_index))
    return global_best_sol_index


def read_flow(folder):
    # file = folder + "BB_LP_Path.txt"
    file =os.path.join(folder, "BB_LP_Path.txt")
    df = pd.read_csv(file, sep=',', index_col=False)
    num_iter = df["Iter"].max() + 1
    num_sol = df["SolNum"].max() + 1
    num_row = df.shape[0]
    all_path = []
    all_sol = []
    all_iter = []
    for i in range(0, num_row):
        all_path.append(mc.PathClass(df['Iter'][i], df['SolNum'][i], df['OD'][i], df['PathId'][i]))
        all_path[i].pie = df['Pie'][i]
        all_path[i].prob_cplex = df['Prob_Cplex'][i]
        all_path[i].prob_compute = df['Prob_Compute'][i]

    for i in range(0, num_sol):
        all_sol.append(mc.SolClass(i))
        for j in range(0, num_row):
            if all_path[j].sol == i:
                all_sol[i].path.append(all_path[j])
                all_sol[i].iter = all_path[i].iter

    for i in range(0, num_iter):
        all_iter.append(mc.IterClass(i))
        for j in range(0, len(all_sol)):
            if all_sol[j].iter == i:
                all_iter[i].sol.append(all_sol[j])

    return all_path, all_sol, all_iter


def read_od(folder):
    """
        read od data
    :param folder:
    :return:
    """
    # file = folder + 'Trips.txt'
    file =os.path.join(folder,'Trips.txt')
    df = pd.read_csv(file, sep=',', index_col=False)
    num_row = df.shape[0]
    od_list = []
    for i in range(0, num_row):
        od_list.append(mc.OdClass(i))
        od_list[i].demand = df['Demand'][i]

    return od_list



def read_path_nodes(folder, data:mc.TestCaseClass):
    """
        read path data from
    :return:
    """
    # file = folder + 'BB_LP_PasPath_2.txt'
    file = os.path.join(folder, 'BB_Lp_PasPath_Data.txt')
    df = pd.read_csv(file, sep=',', index_col=False)
    num_row = df.shape[0]

    BestSolNum = data.gl_opt_sol_id
    # if BestSolNum == -1:
    #     BestSolNum = data.iter_list[-2].best_sol

    for i in range(0, num_row):
        Iter = df['Iter'][i]
        SolNum = df['SolNum'][i]
        OD = df['OD'][i]
        Path = df['Path'][i]
        Node = df['Node'][i]
        Wait = df['Wait'][i]
        Line = df['Line'][i]
        Veh = df['Veh'][i]
        Seat = df['Seat'][i]
        PathCost = df['PathCost'][i]
        PathProb = df['PathProb'][i]
        PathFlow = df['PathFlow'][i]
        SegTime = df['SegTime'][i]
        _arr_time = df['ArrTime'][i]
        _dep_time = df['DepTime'][i]

        # only proceed to plot the last solution
        if SolNum == BestSolNum:
            NewNode = mc.PathNodeClass(Node, Wait, Line, Veh, Seat,_arr_time, _dep_time)
            for p in range(0, len(data.sol_list[SolNum].path)):
                if data.sol_list[SolNum].path[p].id == Path:
                    data.sol_list[SolNum].path[p].visit_node.append(NewNode)

def read_main(folder):
    case_data = mc.TestCaseClass()
    (case_data.path_list, case_data.sol_list, case_data.iter_list) = read_flow(folder)
    case_data.od_list = read_od(folder)
    case_data.gl_opt_sol_id = read_optimal_sol(folder, case_data)
    read_schedule(folder, case_data)
    read_fre(folder, case_data)
    read_path_nodes(folder, case_data)

    return case_data
