__author__ = "Yu Jiang"
__email__ = "yujiang@dtu.dk"
"""
    plot the convergence cph results 
    plot the computed objective value from the results
"""
import matplotlib.pyplot as plt
import numpy as np
import seaborn as sns
import matplotlib.ticker as ticker

LabelFontSize = 14
TitleFontSize = 14
legendFontSize = 12
TickSize = 10
# sns.set_style("dark")
x1 = [1,2,3,4,5]
y1 = [7202.403134,7191.821232,7184.02254,7183.971992,7186.340086 ]

x2 = [1,2,3,4]
y2 = [7202.403134,7186.826062,7181.787893,7169.230715]

x3 = [1,2,3,4,5]
y3 = [7202.382708,7188.121829,7176.094287,7174.708613,7173.756782]

x4 = [1,2,3,4,5,6,7,8]
y4 = [7202.382708,7189.485558,7189.994249,7186.339361,7184.721202,7177.010394,7191.199988,7231.010089]

x = []
y = []
x.append(x1)
x.append(x2)
x.append(x3)
x.append(x4)
y.append(y1)
y.append(y2)
y.append(y3)
y.append(y4)

f, ax = plt.subplots(ncols=3, nrows=1, figsize=(18, 6))
x=[]
y=[]
x.append(x1)
x.append(x2)
x.append(x3)
y.append(y1)
y.append(y2)
y.append(y3)
for index in range(0,3):
        # index= i*2+j
    print(x[index])
    print(y[index])
    axx = sns.lineplot(x[index],y[index],ax=ax[index],marker="o")
    axx.xaxis.set_major_locator(ticker.MultipleLocator(1))
    xtick = ax[index].get_xticks()
    ytick = ax[index].get_yticks()
    ax[index].set_ylim([7165,7205])
    xmajorFormatter = plt.FormatStrFormatter('%.0f')
    ymajorFormatter = plt.FormatStrFormatter('%.1f')
    ax[index].set_xticklabels(xtick, fontsize=TickSize,fontname='Times New Roman')
    ax[index].set_yticklabels(ytick, fontsize=TickSize,fontname='Times New Roman')
    ax[index].yaxis.set_major_formatter(ymajorFormatter)
    ax[index].xaxis.set_major_formatter(xmajorFormatter)
    if index == 0:
        ax[index].set_title("(a) $\mathit{\eta} = $"+ str(1), fontsize = TitleFontSize,fontname='Times New Roman')
    if index == 1:
        ax[index].set_title("(b) $\mathit{\eta} = $"+ str(2), fontsize = TitleFontSize,fontname='Times New Roman')
    if index == 2:
        ax[index].set_title("(c) $\mathit{\eta} = $"+ str(3), fontsize = TitleFontSize,fontname='Times New Roman')
    if index == 3:
        ax[index].set_title("(d) $\mathit{\eta} = $"+ str(4), fontsize = TitleFontSize,fontname='Times New Roman')
    if index==0:
        ax[index].set_ylabel("Objective Value",fontsize = LabelFontSize, fontname = 'Times New Roman',weight='bold')
    ax[index].set_xlabel("No. of Iterations",fontsize = LabelFontSize, fontname = 'Times New Roman',weight='bold')


plt.tight_layout()
plt.savefig("Converge.png",bbox_inches='tight',dpi=600)
plt.show(block = False)
plt.pause(2)

exit()
