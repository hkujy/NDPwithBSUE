
"""
find reference
https://morvanzhou.github.io/tutorials/data-manipulation/plt/2-3-axis1/
    this function plot the trajtory of the bus
"""
import os
import myclass as mc
import numpy as np
import ParaClass
import matplotlib.pyplot as MatPlt
import pandas as pd
import myclass as mc
import matplotlib.pyplot as plt
# import networkx as nx
# import matplotlib.pyplot as plt
# import plotly

def plot_train(data:mc.TestCaseClass, sol:mc.SolClass):
    """
        plot different bus lines segmemnt
    :param data:
    :param sol:
    :return:
    """
    fig = plt.figure()
    for l in sol.lines:
        line_color = 'b'
        style = 'solid'
        if l.id == 0:
            line_color = 'b'
            style = 'solid'
        elif l.id == 1:
            line_color = 'r'
            style = 'dashed'
        if l.type != 'Sch':
            continue
        for r in l.runs:
            for s in range(0, len(r.stops)):
                x = []
                y = []
                if s == 0:
                    x.append(r.dep_time[s])
                    y.append(int(r.stops[s]))
                    x.append((r.arr_time[s+1]))
                    y.append(int(r.stops[s+1]))
                    if l.id == 0:
                        L1, = plt.plot(x, y, linewidth=2.0, color=line_color, linestyle=style, label='Line 1')
                    if l.id == 1:
                        L2,= plt.plot(x, y, linewidth=2.0, color=line_color, linestyle=style, label='Line 2')
                elif s == len(r.stops)-1:
                    pass
                else:
                    x.clear()
                    y.clear()
                    x.append(r.arr_time[s])
                    x.append(r.arr_time[s] + 1)
                    y.append(int(r.stops[s]))
                    y.append(int(r.stops[s]))
                    plt.plot(x, y, linewidth=2.0, color=line_color, linestyle=style)
                    x.clear()
                    y.clear()
                    x.append(r.dep_time[s])
                    x.append(r.arr_time[s+1])
                    y.append(int(r.stops[s]))
                    y.append(int(r.stops[s+1]))
                    plt.plot(x, y, linewidth=2.0,color=line_color,linestyle=style)

    # plt.show()

    new_ticks = np.linspace(0, 3, 4)
    plt.yticks(new_ticks)
    plt.legend(handles=[L1, L2], loc='best', prop=data.font)
    plt.yticks([0, 1, 2, 3], [r'A', r'B', r'C', r'D'])
    plt.xlim((0, 14))

    axes = plt.gca()
    axes.set_yticklabels([r'A', r'B', r'C', r'D'], fontdict=data.font)
    xtick = axes.get_xticks()
    axes.set_xticklabels(xtick, fontdict=data.font)

    # lg = plt.legend(loc=())
    # plt.setp(lg.text, family='Times New Roman')
    # lg.draw_frame(False)
    plt.xlabel('Time', fontdict=data.font)
    plt.ylabel('Stop', fontdict=data.font)

    # plt.savefig(data.output_folder + 'Schedule''.png')
    plt.savefig(os.path.join(data.output_folder,'Schedule''.png'))
    plt.close('all')



    # Plot only Line 1
    fig = plt.figure()
    for l in sol.lines:
        if l.id==1:
            continue
        if l.id == 0:
            line_color = 'b'
            style = 'solid'
        if l.type != 'Sch':
            continue
        for r in l.runs:
            for s in range(0, len(r.stops)):
                x = []
                y = []
                if s == 0:
                    x.append(r.dep_time[s])
                    y.append(int(r.stops[s]))
                    x.append((r.arr_time[s+1]))
                    y.append(int(r.stops[s+1]))
                    if l.id == 0:
                        L1, = plt.plot(x, y, linewidth=2.0, color=line_color, linestyle=style, label='Line 1')
                elif s == len(r.stops)-1:
                    pass
                else:
                    x.clear()
                    y.clear()
                    x.append(r.arr_time[s])
                    x.append(r.arr_time[s] + 1)
                    y.append(int(r.stops[s]))
                    y.append(int(r.stops[s]))
                    plt.plot(x, y, linewidth=2.0, color=line_color, linestyle=style)
                    x.clear()
                    y.clear()
                    x.append(r.dep_time[s])
                    x.append(r.arr_time[s+1])
                    y.append(int(r.stops[s]))
                    y.append(int(r.stops[s+1]))
                    plt.plot(x, y, linewidth=2.0,color=line_color,linestyle=style)

    # plt.show()

    new_ticks = np.linspace(0, 3, 4)
    plt.yticks(new_ticks)
    plt.legend(handles=[L1], loc='best', prop=data.font)
    plt.yticks([0, 1, 2, 3], [r'A', r'B', r'C', r'D'])
    plt.xlim((0, 14))

    axes = plt.gca()
    axes.set_yticklabels([r'A', r'B', r'C', r'D'], fontdict=data.font)
    xtick = axes.get_xticks()
    axes.set_xticklabels(xtick, fontdict=data.font)

    # lg = plt.legend(loc=())
    # plt.setp(lg.text, family='Times New Roman')
    # lg.draw_frame(False)
    plt.xlabel('Time', fontdict=data.font)
    plt.ylabel('Stop', fontdict=data.font)

    # plt.savefig(data.output_folder + 'Schedule''.png')
    plt.savefig(os.path.join(data.output_folder,'Schedule_L1''.png'))
    plt.close('all')










    plot_path_traj(data, sol)

def plot_path_traj(data:mc.TestCaseClass, sol:mc.SolClass):
    """
        Plot the path
    :param data:
    :param sol:
    :return:
    """
    for p in sol.path:
        fig = plt.figure()
        x = []
        y = []
        for s in range(0, len(p.visit_node)):
            if s == 0:
                x.clear()
                y.clear()
                x.append(p.visit_node[s].dep_time)
                x.append(p.visit_node[s+1].arr_time)
                y.append(int(p.visit_node[s].id))
                y.append(int(p.visit_node[s+1].id))
                if p.visit_node[s].board_line == 0:
                    co = 'b'
                    style = 'solid'
                if p.visit_node[s].board_line == 1:
                    co = 'r'
                    style = 'dashed'
                plt.plot(x, y, color=co,linewidth=2.0,linestyle=style)
            elif s == len(p.visit_node) - 1:
                pass
            else:
                x.clear()
                y.clear()
                # plot waiting time
                x.append(p.visit_node[s].arr_time)
                x.append(p.visit_node[s].dep_time)
                y.append(int(p.visit_node[s].id))
                y.append(int(p.visit_node[s].id))
                plt.plot(x, y, linestyle=':', color='green', linewidth=2.0)

                x.clear()
                y.clear()
                x.append(p.visit_node[s].dep_time)
                x.append(p.visit_node[s+1].arr_time)
                y.append(int(p.visit_node[s].id))
                y.append(int(p.visit_node[s+1].id))
                if p.visit_node[s].board_line == 0:
                    co = 'b'
                    style = 'solid'
                if p.visit_node[s].board_line == 1:
                    co = 'r'
                    style = 'dashed'
                plt.plot(x, y, color=co, linewidth=2.0, linestyle=style)

                pass
        new_ticks = np.linspace(0, 3, 4)
        plt.yticks([0, 1, 2, 3], [r'A', r'B', r'C', r'D'])
        plt.yticks(new_ticks)
        axes = plt.gca()
        axes.set_yticklabels([r'A', r'B', r'C', r'D'], fontdict=data.font)

        plt.xlim((0, 14))
        xtick = axes.get_xticks()
        axes.set_xticklabels(xtick, fontdict=data.font)
        title = 'OD ' + str(p.od) + "_PathID_" + str(p.id)
        plt.title(title, fontdict=data.font)
        plt.xlabel('Time', fontdict=data.font)
        plt.ylabel('Stop', fontdict=data.font)
        # plt.savefig(data.output_folder + 'PathTra_' + 'OD_' + str(p.od) + "_PathID_" + str(p.id) + '.png')
        plt.savefig(os.path.join(data.output_folder, 'PathTra_' + 'OD_' + str(p.od) + "_PathID_" + str(p.id) + '.png'))
        fig.show()
        pass









