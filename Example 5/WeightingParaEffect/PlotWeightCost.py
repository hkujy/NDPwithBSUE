__author__ = "Yu Jiang"
__email__ = "yujiang@dtu.dk"
"""
    plot the convergence cph results 
    I may need to update the numbers in x and y vector later
"""
from os import sep
import matplotlib.pyplot as plt
import numpy as np
import seaborn as sns
import pandas as pd
from seaborn.palettes import color_palette
import matplotlib.ticker as ticker

LabelFontSize = 14
TitleFontSize = 14
legendFontSize = 12
TickSize = 10

x1 = [1,2,3,4,5]
x2 = [1,2,3,4,5]
y1 = [6.912,4.038,3.954,3.75, 3.75]  # fre M2
y2 = [6.384,4.5,4.116,4.284, 4.284]  # fre 150
y3 = [6078.784389,6636.495451,6738.605984,6739.718351,6746.022123]
y4 = [1108,711.5,672.5,669.5,669.5]
x = []
y = []
f, ax = plt.subplots(ncols=2, nrows=1, figsize=(8.5, 3.75))
x=[]
y=[]
x.append(x2)
x.append(x1)
y.append(y3)
y.append(y1)

for index in range(0,2):
    print(x[index])
    print(y[index])
    if index ==1:
        axxx =sns.lineplot(x[index],y[index],ax=ax[index],marker="o",label="M2")
        sns.lineplot(x[index],y2,ax=ax[index],marker="X",color="red",label = "150S")
        ax[index].set_ylim([3,8])
        axxx.xaxis.set_major_locator(ticker.MultipleLocator(1))
        ax[index].legend(prop={"family":"Times New Roman","size":10})
        ax[index].set_title("(b) Frequency",fontsize=10,fontname='Times New Roman')
        ax[index].set_ylabel("Frequency (veh/hour)",fontsize = 10, fontname = 'Times New Roman',weight='bold')
    if index ==0:
        sns.lineplot(x[index],y[index],ax=ax[index],marker="o",label="Passengers' Cost")
        # ax[index].set_ylim([6000,6800])
        ax[index].set_ylabel("Passengers' Cost",fontsize = 10, fontname = 'Times New Roman',weight='bold')
        # ax[index].set_ylabel("Passengers' Cost",fontsize = 10, fontname = 'Times New Roman')
        ax[index].legend(loc=(0.55,0.42),prop={"family":"Times New Roman","size":10})
        ax[index].set_title("(a) Cost",fontsize=10,fontname='Times New Roman')
        ax2=ax[0].twinx()
        sns.lineplot(x[index],y4,color= 'red',label = "Operation Cost  ",marker="X")
        # ax2.set_ylim([650,1200])
        ax2.set_ylabel("Operation Cost",fontsize = 10, fontname = 'Times New Roman',weight='bold')
        # ax2.set_ylabel("Operation Cost",fontsize = 10, fontname = 'Times New Roman')
        ax2.legend(loc=(0.55,0.5),prop={"family":"Times New Roman","size":10})
        xtick = ax2.get_xticks()
        ytick = ax2.get_yticks()
        xmajorFormatter = plt.FormatStrFormatter('%.0f')
        ymajorFormatter = plt.FormatStrFormatter('%.0f')
        ax2.set_xticklabels(xtick, fontsize=9,fontname='Times New Roman')
        ax2.set_yticklabels(ytick, fontsize=9,fontname='Times New Roman')
        ax2.yaxis.set_major_formatter(ymajorFormatter)
        ax2.xaxis.set_major_formatter(xmajorFormatter)
        plt.xticks([1,2,3,4,5])
    xtick = ax[index].get_xticks()
    ytick = ax[index].get_yticks()
    xmajorFormatter = plt.FormatStrFormatter('%.0f')
    ymajorFormatter = plt.FormatStrFormatter('%.0f')
    ax[index].set_xticklabels(xtick, fontsize=9,fontname='Times New Roman')
    ax[index].set_yticklabels(ytick, fontsize=9,fontname='Times New Roman')
    ax[index].yaxis.set_major_formatter(ymajorFormatter)
    ax[index].xaxis.set_major_formatter(xmajorFormatter)
    ax[index].set_xlabel("Value of $\mathit{\\alpha}^{Weight}$",fontsize = 10, fontname = 'Times New Roman', weight='bold')

plt.tight_layout()
plt.savefig("Fre&&CostFig.png",bbox_inches='tight',dpi=600)
plt.show(block = False)
plt.pause(2)
plt.close()
