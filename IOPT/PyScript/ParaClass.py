
import pandas as pd
import os


class ParaClass(object):
    """
        class for the adjustable parameters
    """

    def __init__(self, from_folder):
        # df = pd.read_csv(str(from_folder) + 'AdjustPara.txt', sep=',', index_col=False, header=None)
        df = pd.read_csv(os.path.join(
            str(from_folder), 'AdjustPara.txt'), sep=',', index_col=False, header=None)
        # df = pd.read_csv('AdjustPara.txt', sep=',', index_col=False, header=None)
        num_data_row = df.shape[0]
        self.assign_type = ''
        for i in range(0, num_data_row):
            if df[0][i] == "NumOfBreakPoints":
                self.num_break_point = df[1][i]
            if df[0][i] == "BcmBound":
                self.bcm_bound = df[1][i]
            if df[0][i] == "Theta":
                self.theta = df[1][i]
            if df[0][i] == "Assign":
                self.assign_type = df[1][i]
            if df[0][i] == "Case":
                self.case = df[1][i]
        self.read_output_from_folder = from_folder
        pass


class DesignParaClass:
    """
        write for the design para class
    """

    def __init__(self, _exp_id):
        self.file_name = ""   # file to be written
        self.para_dict = {
            "MaxTimeHorizon": 91,
            "MaxLineOperTime": 60,
            "MaxHeadway": 20,
            "MinHeadway": 5,
            "MinDwellTime": 1,
            "FleetSize": -1,
            "BigM": 1000
        }
        self.set_default(_exp_id)
        pass

    def write_to_file(self):
        """
            write to the target file
        """
        with open(self.file_name, 'w+') as f:
            print("Write Design Parameter to file = {0}".format(
                self.file_name))
            for key in self.para_dict.keys():
                print('{0},{1}'.format(key, self.para_dict[key]), file=f)
                print('{0},{1}'.format(key, self.para_dict[key]))

    def adjust_para(self, _adjust):
        """
            adjust parameter
        """
        for key in _adjust.keys():
            self.para_dict[key] = _adjust[key]
            print("change adjust paramter name = {0}, value = {1}".format(
                key, _adjust[key]))

    def set_design_para(self, _para_dict):
        """
            main procedure to set the design parameters
        """
        self.adjust_para(_para_dict)
        self.write_to_file()

# """
# ## setting of previous example index setting
# # ExampleIndex = 0  # test the effect of bcm
# # ExampleIndex = 1  # frequency line case
# # ExampleIndex = 2  # sch line case
# # ExampleIndex = 3  # mixed frequency and sch cas
# # ExampleIndex = 4  # three link case
# """

    def set_file_name(self, _exp_id):
        """
            set the default file name
        """
        if _exp_id == 0 or _exp_id == 5:
            self.file_name = "c:/GitCodes/IPTOP/IOPT/Input/BaseCase/DesignPara.csv"
        elif _exp_id == 1:
            self.file_name = "c:/GitCodes/IPTOP/IOPT/Input/SeatCase_fre/DesignPara.csv"
        elif _exp_id == 2:
            self.file_name = "c:/GitCodes/IPTOP/IOPT/Input/SeatCase_sch/DesignPara.csv"
        elif _exp_id == 3:
            self.file_name = "c:/GitCodes/IPTOP/IOPT/Input/SeatCase_mix/DesignPara.csv"
        elif _exp_id == 4:
            self.file_name = "c:/GitCodes/IPTOP/IOPT/Input/Case_3link/DesignPara.csv"
        else:
            print("Warning: The _exp_id is not set correctly")
            input("----------Debug------------------")

    def set_default(self, _exp_id):
        """
        set the default parameters
        """
        self.set_file_name(_exp_id)

        if _exp_id == 0:
            for key in default_exp_0.keys():
                self.para_dict[key] = default_exp_0[key]
        elif _exp_id == 1:
            for key in default_exp_1.keys():
                self.para_dict[key] = default_exp_1[key]
        elif _exp_id == 2:
            for key in default_exp_2.keys():
                self.para_dict[key] = default_exp_2[key]
        elif _exp_id == 3:
            for key in default_exp_3.keys():
                self.para_dict[key] = default_exp_3[key]
        elif _exp_id == 4:
            for key in default_exp_4.keys():
                self.para_dict[key] = default_exp_4[key]
        elif _exp_id == 5:
            for key in default_exp_5.keys():
                self.para_dict[key] = default_exp_5[key]


default_exp_0 = {
    "MaxTimeHorizon": 120,
    "MaxLineOperTime": 101,
    "MaxHeadway": 20,
    "MinHeadway": 5,
    "MinDwellTime": 1,
    "FleetSize": -1,
    # "BigM": 1000
    "BigM": 500
}

default_exp_1 = {
    "MaxTimeHorizon": 30,
    "MaxHeadway": 20,
    "MinHeadway": 5,
    "MinDwellTime": 1,
    "TimeHorizon": 20,
    "BigM": 1000
}
default_exp_2 = {
    "MaxTimeHorizon": 30,
    "MaxHeadway": 20,
    "MinHeadway": 5,
    "MinDwellTime": 1,
    "TimeHorizon": 20,
    "BigM": 1000
}
default_exp_3 = {
    "MaxTimeHorizon": 30,
    "MaxHeadway": 20,
    "MinHeadway": 4,
    "MinDwellTime": 1,
    "TimeHorizon": 20,
    "BigM": 1000
}

default_exp_4 = {
    "MaxTimeHorizon": 30,
    "MaxHeadway": 20,
    "MinHeadway": 5,
    "MinDwellTime": 1,
    "TimeHorizon": 20,
    "BigM":1000
}

default_exp_5 = {
    # effect of minimum headway
    "MaxTimeHorizon": 120,
    "MaxLineOperTime": 61,
    "MaxHeadway": 20,
    "MinHeadway": 3,
    "MinDwellTime": 1,
    "FleetSize": -1,
    "BigM": 1000
}




