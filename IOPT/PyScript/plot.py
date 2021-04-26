import myclass as mc
import numpy as np
# import matplotlib.pyplot as plt
import os
import plotly
import ParaClass
import plotly.graph_objs as go
import matplotlib.pyplot as MatPlt
import pandas as pd
import myclass as mc
import matplotlib.pyplot as plt
import networkx as nx
from plot_trajectory import plot_train

"""
    reference for plot shift label position
    https://stackoverflow.com/questions/30791334/making-networkx-plot-where-edges-only-display-edited-numeric-value-not-field-na
"""


def plot_fre(data: mc.TestCaseClass, sol: mc.SolClass):
    """
        Plot Table of frequency
    :param data:
    :param sol:
    :return:
    """

    head_coler = '#a1c3d1'
    row_coler = 'lightgrey'
    Id = []
    Fre = []
    Head = []
    Prod = []  # product of frequency and headway
    for l in range(0, len(sol.lines)):
        if sol.lines[l].type != 'Fre':
            continue
        Id.append(sol.lines[l].id)
        Fre.append(sol.lines[l].fre)
        Head.append(sol.lines[l].headway)
        Prod.append(sol.lines[l].fre * sol.lines[l].headway)

    if len(Id) == 0:
        return

    layout = plotly.graph_objs.Layout(title='Fre Sol', width=400)
    trace = plotly.graph_objs.Table(
        header=dict(values=['Id', 'Fre', 'Headway', '==1?'], line=dict(color='#7D7F80'), fill=dict(color=head_coler),
                    align=['left']),
        # cells=dict(values=[run_col, stop_col, dep_col], line=dict(color='#7D7F80'), fill=dict(color='#EDFAFF'),
        cells=dict(values=[Id, Fre, Head, Prod], line=dict(color='#7D7F80'), fill=dict(color=row_coler),
                   align=['left']))

    table_data = [trace]
    fig = dict(data=table_data, layout=layout)
    # plotly.offline.plot(fig, filename=data.output_folder + 'FreSol.html')
    plotly.offline.plot(fig, filename=os.path.join(data.output_folder, 'FreSol.html'))

    # with open(data.output_folder + 'FreSol.csv', 'w') as f:
    with open(os.path.join(data.output_folder, 'FreSol.csv'), 'w') as f:
        print('{0},{1},{2}'.format('Id', 'Fre', 'Headway'), file=f)
        for r in range(0, len(Id)):
            print('{0},{1},{2}'.format(Id[r], Fre[r], Head[r]), file=f)

    # py.image.save_as(fig, filename=data.output_folder+'FreSol.png')


def plot_schedule(data: mc.TestCaseClass, sol: mc.SolClass):
    """
        plot time time table
    :param data:
    :param sol:
    :return:
    """
    run_head_col = '#a1c3d1'
    stop_color = 'lightgrey'
    for l in range(0, len(sol.lines)):
        if sol.lines[l].type != 'Sch':
            continue
        run_col = []
        stop_col = []
        dep_col = []
        row_color = []
        now_run = -1
        for r in range(0, len(sol.lines[l].runs)):
            for s in range(0, len(sol.lines[l].runs[r].stops)):
                if now_run == -1:
                    now_run = r
                    run_col.append(now_run)
                    row_color.append(run_head_col)
                elif now_run == r:
                    run_col.append("-")
                    row_color.append(stop_color)
                else:
                    run_col.append(r)
                    now_run = r
                    row_color.append(run_head_col)
                stop_col.append(sol.lines[l].runs[r].stops[s])
                dep_col.append(sol.lines[l].runs[r].dep_time[s])
        layout = plotly.graph_objs.Layout(title='Line: ' + str(sol.lines[l].id), width=400)
        trace = plotly.graph_objs.Table(
            header=dict(values=['Run', 'Stop', 'Dep'], line=dict(color='#7D7F80'), fill=dict(color='#a1c3d1'),
                        align=['left']),
            # cells=dict(values=[run_col, stop_col, dep_col], line=dict(color='#7D7F80'), fill=dict(color='#EDFAFF'),
            cells=dict(values=[run_col, stop_col, dep_col], line=dict(color='#7D7F80'), fill=dict(color=[row_color]),
                       align=['left']))

        table_data = [trace]
        fig = dict(data=table_data, layout=layout)
        # plotly.offline.plot(fig, filename=data.output_folder + 'Line' + str(str(sol.lines[l].id) + '.html'))
        plotly.offline.plot(fig, filename=os.path.join(data.output_folder, 'Line' + str(str(sol.lines[l].id) + '.html')))
        # plotly.plotly.plot(fig, filename='Line_'+str(str(sol.lines[l].id)+'.html'))

        # with open(data.output_folder + 'Line' + str(str(sol.lines[l].id)) + ".csv", 'w') as f:
        with open(os.path.join(data.output_folder, 'Line' + str(str(sol.lines[l].id)) + ".csv"), 'w') as f:
            print('{0},{1},{2}'.format('Run', 'Stop', 'Dep'), file=f)
            for r in range(0, len(run_col)):
                print('{0},{1},{2}'.format(run_col[r], stop_col[r], dep_col[r]), file=f)

        # py.image.save_as(fig, filename=data.output_folder+'Line'+str(str(sol.lines[l].id)) +'.png')


def plot_prob_vs_cost_one(data: mc.TestCaseClass, sol: mc.SolClass):
    """
        plot one solution case
    :param data:
    :return:
    """

    for o in range(0, len(data.od_list)):
        pie_x = []
        prob_y = []
        for p in range(0, len(sol.path)):
            if sol.path[p].od == o:
                pie_x.append(sol.path[p].pie)
                prob_y.append(sol.path[p].prob_compute)
        # plt.scatter(pie_x, prob_y)
        # title = "Pie vs Prob"
        # plt.title(title)
        # # plt.savefig('fig_' + title + '.pdf')
        # plt.show()

        trace = plotly.graph_objs.Scatter(
            x=pie_x,
            y=prob_y,
            mode='markers'
        )
        title_name = 'Pie vs Cost OD:' + str(o)
        layout = dict(title=title_name, xaxis=dict(title="Cost", gridwidth=1), yaxis=dict(title="Prob", gridwidth=1))
        scatter_data = [trace]

        fig = dict(data=scatter_data, layout=layout)
        # plotly.offline.plot(fig, filename=data.output_folder + "prob_OD_" + str(o) + ".html")
        plotly.offline.plot(fig, filename=os.path.join(data.output_folder, "prob_OD_" + str(o) + ".html"))
        # plotly.plotly.plot(fig, filename='Prob_Scatter.html')

        MatPlotFig = MatPlt.figure()
        MatPlt.scatter(pie_x, prob_y)
        MatPlt.xlabel('Cost')
        MatPlt.ylabel('Time')
        MatPlt.title("OD " + str(o))
        # MatPlotFig.savefig(data.output_folder + "prob_OD_" + str(o))
        MatPlotFig.savefig(os.path.join(data.output_folder,  "prob_OD_" + str(o)))

        # with open(data.output_folder + "prob_OD_" + str(o) + ".csv", 'w') as f:
        with open(os.path.join(data.output_folder, "prob_OD_" + str(o) + ".csv"), 'w') as f:
            print('{0},{1}'.format('pie_x', 'prob_y'), file=f)
            for r in range(0, len(pie_x)):
                print('{0},{1}'.format(pie_x[r], prob_y[r]), file=f)

        # py.image.save_as(fig, filename=data.output_folder+"prob_OD_"+str(o)+'.png')

        # from IPython.display import Image
        # Image('a-simple-plot.png')
        plt.clf()
        plt.close('all')
        # MatPlotFig.clear()
        MatPlt.close()
    pass


def plot_one_sol(data: mc.TestCaseClass):
    """
        plot general
    :return:
    """
    # the following plot the best solution
    opt_sol_index = data.gl_opt_sol_id
    plot_prob_vs_cost_one(data, data.sol_list[opt_sol_index])
    plot_schedule(data, data.sol_list[opt_sol_index])
    plot_fre(data, data.sol_list[opt_sol_index])


def plot_global(data: mc.TestCaseClass, para: ParaClass):
    """
        Plot the global iteration table
    :param data:
    :return:
    """
    # file = para.read_output_from_folder + 'GlobalIter.txt'
    file =os.path.join(para.read_output_from_folder,  'GlobalIter.txt')
    df = pd.read_csv(file, index_col=False)

    Iter = []
    NumCol = []
    CplexObj = []
    ComputeObj = []
    Cpu = []

    num_row = df.shape[0]
    for i in range(0, num_row):
        Iter.append(df['Iter'][i])
        NumCol.append(df['NumCol'][i])
        CplexObj.append(df['BestCplexObj'][i])
        ComputeObj.append(df['BestComputeObj'][i])
        Cpu.append(df['CpuTime'][i])

    trace = go.Table(
        header=dict(values=['Iter', 'No.Col', 'Obj(cplex)', 'Obj(exact)', 'CpuTime'],
                    fill=dict(color='#C2D4FF'),
                    align=['left']),
        cells=dict(values=[Iter, NumCol, CplexObj, ComputeObj, Cpu],
                   fill=dict(color='#F5F8FF'),
                   align=['left']))
    table_data = [trace]
    layout = plotly.graph_objs.Layout(title='Iteration', width=600)
    fig = dict(data=table_data, layout=layout)
    # plotly.offline.plot(fig, filename=data.output_folder + 'GlobalIter.html')
    plotly.offline.plot(fig, filename=os.path.join(data.output_folder, 'GlobalIter.html'))
    # py.image.save_as(fig, filename=data.output_folder + 'GlobalIter.png')


# TODO: Define all six paths
# TODO: Plot all six paths corresponding

def find_plot_position(path: mc.PathClass):
    """
        Find the path position in the sub plot
    :param path:
    :return:
    """
    """
        Path #1:0-> 150s (L5) ->2 ->M2(L4) ->3
        Path #2:0-> 150s (L5) ->2 ->ores (L2) ->3
        Path #3:0-> 300s (L3) ->1-> B (L0) ->2 ->M2(L4) -> 3
        Path #4:0-> 300s (L3) ->1-> B (L0) ->2 ->ores (L2) -> 3
        Path #5:0-> 300s (L3) ->1-> E (L1) ->2 ->M2(L4) -> 3
        Path #6:0-> 300s (L3) ->1-> E (L1) ->2 ->ores (L2) -> 3
    """
    if len(path.visit_node) == 3:
        if path.visit_node[1].board_line == 4:
            return 1
        else:
            return 2
    if len(path.visit_node) == 4:
        if path.visit_node[1].board_line == 0:
            if path.visit_node[2].board_line == 4:
                return 3
            else:
                return 4
        elif path.visit_node[1].board_line == 1:
            if path.visit_node[2].board_line == 4:
                return 5
            else:
                return 6
    return -1


def plot_path(path: mc.PathClass, data: mc.TestCaseClass):
    """
        Plot the path
    :param path:
    :return:
    """
    fig = plt.figure(figsize=(10, 6))

    plt.subplot(322)
    plt.suptitle('This is title for OD pair', fontsize=16)
    # TODO add save fig and plot the last solution
    # add more label on link and path
    G = nx.Graph()
    # G = nx.MultiDiGraph()
    title = "OD_" + str(path.od) + "_Path_" + str(path.id)
    plt.axis('off')
    plt.title(title)
    plt.xlim(0, 380)
    plt.ylim(0, 100)
    # remark, the plot code only suitable for base network
    G.add_node('0', pos=(5, 45))
    G.add_node('1', pos=(120, 30))
    G.add_node('2', pos=(240, 60))
    G.add_node('3', pos=(360, 45))

    # G.node['0']['pos'] = (5, 50)
    # G.node['1']['pos'] = (85, 50)
    # G.node['2']['pos'] = (160, 50)
    # G.node['3']['pos'] = (245, 50)
    for i in range(0, len(path.visit_node) - 1):
        now_node = path.visit_node[i].id
        next_node = path.visit_node[i + 1].id
        label = "(W:" + str(path.visit_node[i].wait) + ",L" + str(path.visit_node[i].board_line) + "," + str(
            path.visit_node[i].seat) + "," + str(path.visit_node[i].board_veh) + ")"

        G.add_edge(str(now_node), str(next_node), edge_labels=str(label))

    # add different color for different line
    lines = []
    for i in range(0, 6):
        lines.append([(u, v) for (u, v, d) in G.edges(data=True) if d['edge_labels'] == str(i)])
    # Line0 = [(u, v) for (u, v, d) in G.edges(data=True) if d['edge_labels'] == '0']

    # specify edges to be draw
    elarge = [(u, v) for (u, v, d) in G.edges(data=True)]
    pos = nx.get_node_attributes(G, 'pos')
    edge_labels = nx.get_edge_attributes(G, 'edge_labels')

    # draw edges and nodes
    nx.draw_networkx_nodes(G, pos, node_size=100)
    nx.draw_networkx_edges(G, pos, edgelist=elarge, width=2)
    # for l in range(0, len(lines)):
    #     # nx.draw_networkx_edges(G, pos, edgelist=lines[i], width=2, edge_color='green')
    #     nx.draw_networkx_edges(G, pos, edgelist=lines[i], width=2)

    # shifted_pos = {k: [v[0], v[1] + 3.5*pow(-1,int(k))] for k, v in pos.items()}
    shifted_pos = {k: [v[0], v[1] + 3.5] for k, v in pos.items()}

    # edge_label_handles = nx.draw_networkx_edge_labels(G, pos=shifted_pos, edge_labels=edge_labels)
    nx.draw_networkx_edge_labels(G, pos=shifted_pos, edge_labels=edge_labels, font_size=8)
    nx.draw_networkx_labels(G, pos, font_size=8, font_family='sans-serif')
    # nx.draw_networkx_node_labels(G, pos, font_size=10, font_family='sans-serif')

    # nx.draw_networkx_edge_labels(G, pos, edge_labels=edge_labels, font_size=10)

    # plt.savefig(data.output_folder + 'Fig_' + title + '.png')
    plt.savefig(os.path.join(data.output_folder, 'Fig_' + title + '.png'))
    plt.close()
    # plt.show()

    pass


def plot_sol_path(sol: mc.SolClass, data: mc.TestCaseClass):
    """
        Plot the path
    :param path:
    :return:
    """
    num_od = len(data.od_list)
    for o in range(0, num_od):
        od_path_set_id = []
        G = []
        sub = []
        path_title = []
        for p in range(0, len(sol.path)):
            if sol.path[p].od == o:
                od_path_set_id.append(p)
                G.append(nx.Graph())
                sub.append(int("32" + str(find_plot_position(sol.path[p]))))
                path_title.append("Path_" + str(find_plot_position(sol.path[p])))
        for p in range(0, len(od_path_set_id)):
            G[p].add_node('0', pos=(5, 45))
            G[p].add_node('1', pos=(120, 30))
            G[p].add_node('2', pos=(240, 60))
            G[p].add_node('3', pos=(360, 45))
            pathId = od_path_set_id[p]
            for i in range(0, len(sol.path[pathId].visit_node) - 1):
                now_node = sol.path[pathId].visit_node[i].id
                next_node = sol.path[pathId].visit_node[i + 1].id
                label = "(W:" + str(sol.path[pathId].visit_node[i].wait) + ",L" + str(
                    sol.path[pathId].visit_node[i].board_line) \
                        + "," + str(sol.path[pathId].visit_node[i].seat) + "," + str(
                    sol.path[p].visit_node[i].board_veh) + ")"
                G[p].add_edge(str(now_node), str(next_node), edge_labels=str(label))

        fig = plt.figure(figsize=(10, 6))
        fig.suptitle('OD_' + str(o), fontsize=16)
        for p in range(0, len(od_path_set_id)):
            elarge = [(u, v) for (u, v, d) in G[p].edges(data=True)]
            edge_labels = nx.get_edge_attributes(G[p], 'edge_labels')
            plt.subplot(sub[p])
            plt.xticks([])
            plt.yticks([])
            # plt.axis('off')
            # plt.xlim(0, 380)
            # plt.ylim(0, 100)
            pos = nx.get_node_attributes(G[p], 'pos')
            nx.draw_networkx_nodes(G[p], pos, node_size=100)
            nx.draw_networkx_labels(G[p], pos, font_size=8, font_family='sans-serif')
            nx.draw_networkx_edges(G[p], pos, edgelist=elarge, width=2)
            shifted_pos = {k: [v[0], v[1] + 3.5] for k, v in pos.items()}
            nx.draw_networkx_edge_labels(G[p], pos=shifted_pos, edge_labels=edge_labels, font_size=8)
            plt.title(path_title[p])
            # nx.draw_networkx_edge_labels(G, pos, edge_labels=edge_labels, font_size=10)
        # plt.savefig(data.output_folder + 'Fig_OD_' + str(o) + '.png')
        plt.savefig(os.path.join(data.output_folder, 'Fig_OD_' + str(o) + '.png'))
        plt.close(fig)
    """
        The hold on feature is switched on by default in matplotlib.pyplot. 
        So each time you evoke plt.plot() before plt.show() a
        drawing is added to the plot. Launching plt.plot() after the function plt.show() 
        leads to redrawing the whole picture.
    """


def plot_main(data: mc.TestCaseClass, para: ParaClass):
    """
        plot data
    :param data:
    :return:
    """

    plot_global(data, para)
    plot_one_sol(data)
    opt_sol_index = data.gl_opt_sol_id
    # only plot path for the base case and the origin node is 0
    if para.case == 'BaseCase':
        plot_path_graph = True
        for p in data.sol_list[opt_sol_index].path:
            if p.visit_node[0].id != 0:
                plot_path_graph = False
        if plot_path_graph is True:
            plot_sol_path(data.sol_list[opt_sol_index], data)

    # only plot trajtory for the seat schedule case
    if para.case == 'SeatCase_sch':
        plot_train(data, data.sol_list[opt_sol_index])
