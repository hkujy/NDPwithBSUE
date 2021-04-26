import plotly
import os, sys
import shutil
import myclass as mc
from shutil import copyfile
import shutil
import inspect

"""
This function is change adjustable parameters
"""
RunReleaseExe = True
# RunReleaseExe = False
RunDebugExe = False
# RunDebugExe = True


# 1 second: 
OneSecond = 1/60
def set_case(case_index, TurnParaList:mc.TurnParaClass, testId, ExampleIndex):
        # self.root_folder = self.root_folder+"GitCodes"
    """
        define a individual test cas
    :return:
    """
    para_list = {
        # 'BoardAlightTimePerPas': OneSecond*0.1,   # 2 seconds
        'BoardAlightTimePerPas': OneSecond*1,   # 2 seconds
        # 'BoardAlightTimePerPas': OneSecond*0,   # 0 seconds
        # 'BoardAlightTimePerPas':0.0 ,   # 2 seconds
        # 'NumOfBreakPoints': 8,  # default value in the first version
        'NumOfBreakPoints': 6,  
        # 'UseBcmRatio': 'true',
        'UseBcmRatio': 'false', # parameters determine whether the bcm is depends on ratio
        'BcmBound': 3,
        'Slack_Ini': 0.0,  # when this is zero, the initial path is the shortest path "event dominted"
        'Slack_Update': 10.0,  # control the path find algorithm in the following iterations
        'BcmRatio': 0.0,
        # 'Assign': 'SUE',
        # 'Assign': 'RSUE',
        'Assign': 'BCM',
        'Theta': 0.15,
        'Case': "BaseCase",
        # 'Case': "SeatCase_re",
        # 'Case': "SeatCase_sch",
        #  'Case': "Case_3link",
        # 'Case': "SeatCase_sch_simple",
        # 'CplexTimLim': 1200, ## reduce to 2 hour
        'CplexTimLim': 7200, ## reduce to 2 hour
        # 'CplexTimLim': 21600, ## reduce to 6 hour
        # 'BoardAlpha': 0.00,
        'MaxGapBetweenMinMaxCost': 5,   # Maximum time gap between the minimum and maximum cost
        'MaxBBLevel': 50, # maximum branch and bound level 
        'StandBeta': 0.02,
        'SeatBeta': 0.01,
        'StandConst': 1.0,
        'EachInterVal': 15,
        'FreOperationCost': 5000,
        # 'MinPieVal': 25,
        # 'MaxPieVal': 40,
        'MIPRelGap': 0.01,
        # 'MIPRelGap': 0.01,
        'isSeatSeq': 'true',
        # 'isSeatSeq': 'false',
        'MinProb': 0.0001,
        'MaxProb': 1.0001,
        'TestIndex': str(testId),
        'BufferCost': 5,
        'EpsObj': 0.1,
        'EpsConstraint': 0.01,
        'BoundNonDomEventLower': 0.0
    }   


    if ExampleIndex == 1:
        """
            Frequency network
        """
        para_list['NumOfBreakPoints'] = 8
        para_list['BcmBound'] = 5
        para_list['Assign'] = 'BCM'
        para_list['Theta'] = 0.15
        para_list['Case'] = "SeatCase_fre"
        para_list['MIPRelGap'] = 0.001
        para_list['FreOperationCost'] = 0
        para_list['BoardAlpha'] = 0.0
        para_list['MinProb'] = 0.0001
        para_list['MaxProb'] = 1.0001
        para_list['BoundNonDomEventLower'] = 0.0
        para_list['EachInterVal'] = 5
        para_list['EpsObj'] = 0.01

    if ExampleIndex == 2:
        """
            Schedule case network
        """
        para_list['NumOfBreakPoints'] = 9
        para_list['BoardAlightTimePerPas'] = OneSecond
        para_list['BcmBound'] = 5
        para_list['Assign'] = 'BCM'
        para_list['Theta'] = 0.15
        para_list['Case'] = "SeatCase_sch"
        para_list['MIPRelGap'] = 0.001
        para_list['FreOperationCost'] = 0
        para_list['BoardAlpha'] = 0.0
        para_list['MinProb'] = 0.0001
        para_list['MaxProb'] = 1.0001
        para_list['BoundNonDomEventLower'] = 0.0
        para_list['EpsObj'] = 0.01

    if ExampleIndex == 3:
        """
            mixed network
        """
        para_list['NumOfBreakPoints'] = 16
        # para_list['BoardAlightTimePerPas'] = 0.0
        para_list['BoardAlightTimePerPas'] = OneSecond
        para_list['BcmBound'] = 10
        para_list['Assign'] = 'BCM'
        para_list['Theta'] = 0.15
        para_list['Case'] = "SeatCase_mix"
        para_list['MIPRelGap'] = 0.001
        para_list['FreOperationCost'] = 0
        para_list['MinProb'] = 0.0001
        para_list['MaxProb'] = 1.0001
        para_list['BoundNonDomEventLower'] = 0.0
        para_list['EachInterVal'] = 10
        para_list['EpsObj'] = 0.01
    
    if ExampleIndex == 4:
        """
            Three link example demand
        """
        para_list['NumOfBreakPoints'] = 8   # when use large number may not be good for converge
        para_list['BoardAlightTimePerPas'] = 0.0
        para_list['EachInterVal'] = 30
        para_list['BcmBound'] = 10
        para_list['Assign'] = 'BCM'
        para_list['Theta'] = 0.15
        para_list['Case'] = "Case_3link"
        para_list['MIPRelGap'] = 0.001
        para_list['FreOperationCost'] = 0
        para_list['BoardAlpha'] = 0.0
        para_list['MinProb'] = 0.0001
        para_list['MaxProb'] = 1.0001
        para_list['EpsConstraint'] = 0.0001
        para_list['BoundNonDomEventLower'] = 0.0
        para_list['EpsObj'] = 0.01

    if TurnParaList.name == "Assign":
        para_list[TurnParaList.name] = TurnParaList.value
    elif TurnParaList.name == "":
        pass
    else:
        para_list[TurnParaList.name] = TurnParaList.value[case_index]

    para_list['Remark'] = TurnParaList.remark

    with open('AdjustPara.txt', 'w') as f:
        for key in para_list.keys():
            print('{0},{1}'.format(key, para_list[key]), file=f)
    print(sys.path[0])
    upper = os.path.abspath(os.path.join(os.path.dirname(__file__), os.pardir, os.pardir))
    output_from_folder = os.path.join(upper,'IOPT', 'OutPut')
    # output_from_folder = upper + "\IOPT\Output"

    if os.path.isdir(output_from_folder):
        pass
    else:
        os.mkdir(output_from_folder)

    # output_from_folder = output_from_folder + "\\" + para_list['Case']
    output_from_folder = os.path.join(output_from_folder, para_list['Case'])
    # output_from_folder + "\\" + para_list['Case']

    if os.path.isdir(output_from_folder):
        pass
    else:
        os.mkdir(output_from_folder)

    output_from_folder = os.path.join(output_from_folder, para_list['Assign']+'_'+str(testId))
    # output_from_folder = output_from_folder + "\\" + para_list['Assign'] + \
                         # "_" + str(testId) + "\\"

    if os.path.isdir(output_from_folder):
        pass
    else:
        os.mkdir(output_from_folder)
    headcolor = '#a1c3d1'
    valuecolor = 'lightgrey'

    # shutil.copyfile('AdjustPara.txt', output_from_folder + 'AdjustPara.txt')
    shutil.copyfile('AdjustPara.txt', os.path.join(output_from_folder, 'AdjustPara.txt'))
    col_name = []
    col_value = []

    for key in para_list.keys():
        col_name.append(key)
        col_value.append(para_list[key])

    trace = plotly.graph_objs.Table(
        header=dict(values=['Name', 'Val'], line=dict(color='#7D7F80'), fill=dict(color=headcolor), align=['left']),
        cells=dict(values=[col_name, col_value], line=dict(color='#7D7F80'), fill=dict(color=valuecolor),
                   align=['left']))
    table_data = [trace]
    layout = plotly.graph_objs.Layout(title='Para', width=400)
    fig = dict(data=table_data, layout=layout)

    # if os.path.isdir(output_from_folder + 'Plot'):
    if os.path.isdir(os.path.join(output_from_folder, 'Plot')):
        pass
    else:
        # os.mkdir(output_from_folder + 'Plot')
        os.mkdir(os.path.join(output_from_folder, 'Plot'))

    # plotly.offline.plot(fig, filename=output_from_folder + 'plot\Para' '.html')
    plotly.offline.plot(fig, filename=os.path.join(output_from_folder, 'Plot', 'Para.html'))

    # py.image.save_as(fig, filename = Output_from_folder + 'plot\Para'+'.png')


    Copy_input_and_test_files(output_from_folder)
    # print(output_from_folder)
    # input("check output folder")
    return output_from_folder



def copy_folder(_from, _todir):
    if os.path.exists(_todir):
        shutil.rmtree(_todir)
    shutil.copytree(_from, _todir)


# SFileToDFile('d:\\quiz','.txt','d:\\test')
def SFileToDFile(sourcefile,fileclass,destinationfile):
    if os.path.exists(destinationfile):
        pass
    else:
        os.mkdir(destinationfile)
    #遍历目录和子目录
    for filenames in os.listdir(sourcefile):
        #取得文件或文件名的绝对路径
        filepath = os.path.join(sourcefile,filenames)
        #判断是否为文件夹
        if os.path.isdir(filepath):
            #如果是文件夹，重新调用该函数
            SFileToDFile(filepath,fileclass,destinationfile)
        #判断是否为文件
        elif os.path.isfile(filepath):
            #如果该文件的后缀为用户指定的格式，则把该文件复制到用户指定的目录
            if filepath.endswith(fileclass):
                #dirname = os.path.split(filepath)[-1]
                #给出提示信息Script
                # print('Copy %s'% filepath +' To ' + destinationfile)
                #复制该文件到指定目录
                shutil.copy(filepath,destinationfile)

# def Copy_input_and_test_files(mp:mc.FFClass):
def Copy_input_and_test_files(_to_folder):
    """
        copy files and back the tested code
        1. copy cpp files
        2. copy input mms data files
    """
    mf = mc.FFClass()
    SFileToDFile(sourcefile=mf.root_folder+"\\IPTOP\\IOPT\\IOPT",fileclass='.cs',
            #  destinationfile=mp.root_folder+"\\IPTOP\\IOPT\\OutPut\\BackupCpp\\")
             destinationfile=_to_folder+"\\BackupCpp\\")
    SFileToDFile(sourcefile=mf.root_folder+"\\IPTOP\\IOPT\\PyScript",fileclass='.py',
            #  destinationfile=mp.root_folder+"\\IPTOP\\IOPT\\Output\\BackupScript\\")
             destinationfile=_to_folder+"\\BackupScript\\")
    # SFileToDFile(sourcefile=mp.root_folder+"\\DFS\\Script",fileclass='.py',destinationfile=mp.root_folder+"\\DFS\\Output\\Day2Day\\BackupScript\\")
    # SFileToDFile(sourcefile=mp.root_folder+"\\DFS\\Data\\MMS",fileclass='.csv',destinationfile=mp.root_folder+"\\DFS\\Output\\Day2Day\\BackupInput\\"