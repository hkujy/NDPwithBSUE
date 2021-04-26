import os

from matplotlib.pyplot import pause
import ParaClass
import plot as mp  # my plot
import sys
import myclass as mc  # my class
import platform
from read_results import read_main
from para import set_case
import para as para_setting

# RunReleaseExe = True
# # RunReleaseExe = False
# RunDebugExe = False
# # RunDebugExe = True

double_check = "Null"   # no double check and run all the cases
# double_check = "StandBeta"
# double_check = "BCM"
# double_check = "Breakpoints"
# double_check = "Theta"
# double_check = "OpCost"


def set_turn_para_bcm(remarks: str):
    """
        set tune value for the bcm
    :return:
    """
    t = mc.TurnParaClass(remarks)
    t.name = 'BcmBound'
    # in the test we can claim that these values are percentage of the total trip utility
    # t.value = [1.0, 2.0, 3.0, 4.0, 5.0]
    t.value = [1.0, 3.0, 5.0]
    # t.value = [2.0]
    # t.value = [3.0]
    return t


def set_turn_para_stand_beta(remarks: str):
    """
        set tune value for the stand beta
        Not used in the formal test
    :return:
    """
    t = mc.TurnParaClass(remarks)
    t.name = 'StandBeta'
    # t.value = [0.10, 0.15, 0.2, 0.25, 0.3]
    t.value = [0.05, 0.10, 0.15, 0.2, 0.25, 0.3]
    # t.value = [5.0]
    t.value = [1.0, 2.0, 3.0, 4.0, 5.0]

    return t


def set_3_link_para_bcm(remarks: str):
    """
        set tune value for the three link case bcm
    :return:
    """
    t = mc.TurnParaClass(remarks)
    t.name = 'BcmBound'
    t.value = [1, 3, 5]
    return t


def set_3_link_para_SUE(remarks: str):
    """
        set tune value for the three link case bcm
    :return:
    """
    t = mc.TurnParaClass(remarks)
    t.name = 'Assign'
    t.value = 'SUE'
    return t


def set_tune_para_fre_interval(remarks: str):
    """
        set tune value for the fre interval
        used for one example only
    :return:
    """
    t = mc.TurnParaClass(remarks)
    t.name = 'EachInterVal'
    t.value = [5, 10, 15, 20, 25]
    return t

def set_tune_para_num_break_points(remarks: str):
    """
        set tune value for the number of break points
    :return:
    """
    t = mc.TurnParaClass(remarks)
    t.name = 'NumOfBreakPoints'
    # t.value = [4, 6, 8, 10, 12]
    t.value = [4, 6, 8]

    return t

def set_tune_para_theta(remarks: str):
    """
        set tune value for the number of break points
        Not used in the formal test
    :return:
    """

    t = mc.TurnParaClass(remarks)
    t.name = 'Theta'
    t.value = [0.05, 0.2, 0.25, 0.3, 0.35]
    return t

def set_tune_para_operation_cost(remarks: str):
    """
        set tune value for the number of break points
    :return:
    """

    t = mc.TurnParaClass(remarks)
    t.name = 'FreOperationCost'
    # t.value = [5000, 10000, 15000, 20000, 25000]
    t.value = [10000, 15000, 20000, 25000]
    # t.value = [1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10000,
    #          15000, 20000, 25000, 30000, 35000, 40000, 50000]
    return t

def set_tune_para_sue(remarks: str):
    """
        set tune value for the number of break points
    :return:
    """
    # test sue
    t = mc.TurnParaClass(remarks)
    t.name = 'Assign'
    t.value = "SUE"
    return t


def test_one_case(turn_para: mc.TurnParaClass, test_index, testID, ExampleIndex):
    """
        test one case for given tun value
    :param turn_para:
    :param test_index:
    :return:
    """
    upper = os.path.abspath(os.path.join(
        os.path.dirname(__file__), os.pardir, os.pardir))
    release_exe_file = os.path.join(
        upper, 'IOPT', 'IOPT', 'obj', 'x64', 'Release', 'IOPT.exe')
    debug_exe_file = os.path.join(
        upper, 'IOPT', 'IOPT', 'obj', 'x64', 'Debug', 'IOPT.exe')
    out_put_folder = set_case(test_index, turn_para, testID, ExampleIndex)

    if para_setting.RunReleaseExe:
        if platform.system() == "Windows":
            os.system(release_exe_file)
        if platform.system() == "Linux":
            os.system("mono "+release_exe_file)
        print("run release exe")
    if para_setting.RunDebugExe:
        if platform.system() is "Windows":
            os.system(debug_exe_file)
        if platform.system() is "Linux":
            os.system("mono" + debug_exe_file)
        print("run debug exe")

    if (not para_setting.RunDebugExe) and (not para_setting.RunReleaseExe):
        print("does not run exe and just check plot")
        pass

    print("Start Process Data")
    para = ParaClass.ParaClass(out_put_folder)

    with open('OutPutFolderList.txt', 'a') as f:
        print(para.read_output_from_folder, file=f)
    cases = read_main(para.read_output_from_folder)
    cases.output_folder = os.path.join(para.read_output_from_folder, 'Plot')
    if os.path.isdir(cases.output_folder):
        pass
    else:
        os.mkdir(cases.output_folder)

    mp.plot_main(cases, para)

    print("Complete one test case")
    pass

# TODO Write a function for different case studies


def main_test(ExampleIndex,testId):
    open('OutPutFolderList.txt', 'w')
    remark = "test"
    print("Test Exp = {0}".format(ExampleIndex))

    if ExampleIndex == 2:
        # test the schedule case
        test_one_case(mc.TurnParaClass(remark), 0, testId, ExampleIndex)
        with open("CaseList.md", "a") as f:
            print("{0}. Test schedule case".format(testId), file=f)
        testId = testId + 1

    elif ExampleIndex == 3:
        # test the mixed fre-sch based case using the two line example
        # test_one_case(mc.TurnParaClass(remark), 0, testId)
        tune = set_tune_para_fre_interval(remark)
        for i in range(0, len(tune.value)):
            test_one_case(tune, i, testId, ExampleIndex)
            with open("CaseList.md", "a") as f:
                print("{0}. Test_para={1}, para_val={2} ".format(
                    testId, tune.name, tune.value[i]), file=f)
            testId = testId + 1
            # break
        pass

    elif ExampleIndex == 4:
        tune = set_3_link_para_bcm(remark)
        for i in range(0, len(tune.value)):
            test_one_case(tune, i, testId, ExampleIndex)
            with open("CaseList.md", "a") as f:
                print("{0}. 3 link case test_para={1}, para_val={2} ".format(
                    testId, tune.name, tune.value[i]), file=f)
            testId = testId + 1
        
        tune = set_3_link_para_SUE(remark)
        for i in range(0, len(tune.value)):
            test_one_case(tune, i, testId, ExampleIndex)
            with open("CaseList.md", "a") as f:
                print("{0}. 3 link case test_para={1}, para_val={2} ".format(
                    testId, tune.name, tune.value[i]), file=f)
            testId = testId + 1


    elif ExampleIndex == 1:
        """
          case for the pure frequency example
        """
        tune = set_turn_para_bcm(remark)
        for i in range(0, len(tune.value)):
            test_one_case(tune, i, testId, ExampleIndex)
            with open("CaseList.md", "a") as f:
                print("{0}. 3 link case test_para={1}, para_val={2} ".format(
                    testId, tune.name, tune.value[i]), file=f)
            testId = testId + 1
            # input("complete one test")

    elif ExampleIndex == 5:

        if double_check == "Null" or double_check == "OpCost":
            tune = set_tune_para_operation_cost(remark)
            for i in range(0, len(tune.value)):
                test_one_case(tune, i, testId, ExampleIndex)
                with open("CaseList.md", "a") as f:
                    print("ReducedMinFre: {0}. Test_para={1}, para_val={2} ".format(
                        testId, tune.name, tune.value[i]), file=f)
                testId = testId + 1
 
        if double_check == "Null" or double_check == "BCM":
            tune = set_turn_para_bcm(remark)
            for i in range(0, len(tune.value)):
                test_one_case(tune, i, testId, ExampleIndex)
                with open("CaseList.md", "a") as f:
                    print("ReducedMinFre: {0}. test_para={1}, para_val={2} ".format(
                        testId, tune.name, tune.value[i]), file=f)
                testId = testId + 1

    elif ExampleIndex == 0:
        # test the effect of BCM value
        # revised in 2021-Apr-12: in this version only test the effect of BCM and op cost
        if double_check == "Null" or double_check == "BCM":
            tune = set_turn_para_bcm(remark)
            for i in range(0, len(tune.value)):
                test_one_case(tune, i, testId, ExampleIndex)
                with open("CaseList.md", "a") as f:
                    print("{0}. test_para={1}, para_val={2} ".format(
                        testId, tune.name, tune.value[i]), file=f)
                testId = testId + 1
      # test for the tun operational cost
        if double_check == "Null" or double_check == "OpCost":
            tune = set_tune_para_operation_cost(remark)
            for i in range(0, len(tune.value)):
                test_one_case(tune, i, testId, ExampleIndex)
                with open("CaseList.md", "a") as f:
                    print("{0}. Test_para={1}, para_val={2} ".format(
                        testId, tune.name, tune.value[i]), file=f)
                testId = testId + 1
 
        # test the effect of number of breakpoints
        # if double_check == "Null" or double_check == "Breakpoints":
        #     tune = set_tune_para_num_break_points(remark)
        #     for i in range(0, len(tune.value)):
        #         test_one_case(tune, i, testId, ExampleIndex)
        #         with open("CaseList.md", "a") as f:
        #             print("{0}. test_para={1}, para_val={2} ".format(
        #                 testId, tune.name, tune.value[i]), file=f)
        #         testId = testId + 1

    #     # # Test stand beta
    #     if double_check == "Null" or double_check == "StandBeta":
    #         tune = set_turn_para_stand_beta(remark)
    #         for i in range(0, len(tune.value)):
    #             test_one_case(tune, i, testId, ExampleIndex)
    #             with open("CaseList.md", "a") as f:
    #                 print("{0}. test_para={1}, para_val={2} ".format(
    #                     testId, tune.name, tune.value[i]), file=f)
    #             testId = testId + 1
    #    # test the effect of theta
    #     if double_check == "Null" or double_check == "Theta":
    #         tune = set_tune_para_theta(remark)
    #         for i in range(0, len(tune.value)):
    #             test_one_case(tune, i, testId, ExampleIndex)
    #             with open("CaseList.md", "a") as f:
    #                 print("{0}. Test_para={1}, para_val={2} ".format(
    #                     testId, tune.name, tune.value[i]), file=f)
    #             testId = testId + 1

    return testId
