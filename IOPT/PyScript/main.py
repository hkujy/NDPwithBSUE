import set_tests as st
import myclass as mc
import os
from shutil import copyfile
import para
import ParaClass
import sys 
import datetime
"""
## setting of previous example index setting
# ExampleIndex = 0  # test the effect of bcm
# ExampleIndex = 1  # frequency line case
# ExampleIndex = 2  # sch line case
# ExampleIndex = 3  # mixed frequency and sch cas
# ExampleIndex = 4  # three link case
"""


def loop_test(_test_index,_tid,_adjust_design_para={}):
    """
    """
    # mf = mc.FFClass()
    design_para = ParaClass.DesignParaClass(_exp_id = _test_index)
    # adjust_design_para = { 
        # "MinHeadway": 3,
        # }
    design_para.set_design_para(_adjust_design_para)
    _tid =st.main_test(ExampleIndex = _test_index,testId=_tid)

    return _tid

if __name__ == "__main__":
    """
        main procedure for the test
    
    TestCaseIDs
    ID = 4: Example 2 in the paper: the three link network aj
    ID = 2: Example 3 in the paper: schedule line case
    ID = 3: Example 4 in the paper: effect of tp in mixed fre and cap
    ID = 0: cph case

    """
    now = datetime.datetime.now()
    with open("CaseList.md", "w+") as f:
        print("# Tests Begin ", file=f)
        print("> day = {0}-{1}-{2}, time = {3}:{4}".format(now.year,
                                                           now.month, now.day, now.hour, now.minute), file=f)
    tid = 0
    # TestCaseIDs = [1,2,3,4,0]
    # TestCaseIDs = [4]   # example 2 : three links case
    # TestCaseIDs = [2]   # example 3 : effect of seating capacity 
    # TestCaseIDs = [3]   # example 4 : effect of the tp in mixed fre and cap
    # TestCaseIDs = [1]   # test fre
    TestCaseIDs = [0]   # Test case for CPH network
    # TestCaseIDs = [0, 5]

    for i in TestCaseIDs:
        tid = loop_test(i,tid)

    upper = os.path.abspath(os.path.join(
        os.path.dirname(__file__), os.pardir, os.pardir))
    copyfile("CaseList.md", os.path.join(
        upper, 'IOPT', 'OutPut', 'CaseList.md'))
    sys.exit(0)

    mf = mc.FFClass()
    print (mf.root_folder)
    # test_exp_index = 0
    # test_exp_index = 1
    # test_exp_index = 2
    # test_exp_index = 3
    test_exp_index = 4

    design_para = ParaClass.DesignParaClass(_exp_id = test_exp_index)
    adjust_design_para = {
        # "MinDwellTime":0.0
    }
    design_para.set_design_para(adjust_design_para)

    st.main_test(ExampleIndex = test_exp_index)
    

