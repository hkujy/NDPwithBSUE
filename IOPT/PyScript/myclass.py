"""
    The objective of this to define class
"""
import os

class RunClass:
    def __init__(self):
        self.stops = []
        self.arr_time = []
        self.dep_time = []
        self.dwell_time = []

class LineClass:
    """
        Type : "Sch" or "Fre"
        define for the line class
    """

    def __init__(self):
        self.id = -1
        self.runs = []
        self.stops = []
        self.arr_time = []
        self.dep_time = []
        self.type = 'Nul'  # Fre, Sch
        self.run_ids = []
        self.fre = -1
        self.headway = -1
        self.dwell_time = []

class PathNodeClass:
    """
        Path node class to be added in
        path and for plotting path
    """

    def __init__(self, node, wait, line, veh, seat, _arrtime, _dep_time):
        self.id = node
        self.wait = wait
        self.board_line = line
        self.board_veh = veh
        self.isBoardStop = True
        self.seat = seat
        self.arr_time = _arrtime
        self.dep_time = _dep_time
        self.dwell = -1
    #     self.get_board_condition()
    #
    # # def get_board_condition(self):
    # #     """
    # #         Check boarding conditions
    # #     :return:
    # #     """
    # #     pass


class PathClass:
    """
        define class for the path
    """

    def __init__(self, iter, solnum, od, id):
        self.prob_cplex = 0
        self.prob_compute = 0
        self.pie = 0
        self.iter = iter
        self.sol = solnum
        self.od = od
        self.id = id
        self.visit_node = []  # the class the path node class


class OdClass:
    """
    class for the OD pair
    """

    def __init__(self, id):
        self.id = id
        self.demand = 0


class SolClass:
    """
        the solution class for one solution
    """

    def __init__(self, solnum):
        self.path = []
        self.id = solnum
        self.iter = -1
        self.lines = []


class IterClass:
    """
        the solution class for the iteraions
    """

    def __init__(self, id):
        self.id = id
        self.sol = []
        self.best_sol = -1


class TestCaseClass:
    """
        test case class
    """

    def __init__(self):
        self.name = 'undefined case'
        self.od_list = []
        self.iter_list = []
        self.path_list = []
        self.sol_list = []
        self.output_folder = ''
        self.font = {'family': 'Times New Roman',
                     # 'color': 'darkred',
                     'weight': 'normal',
                     'size': 12
                     }
        self.gl_opt_sol_id = []

class TurnParaClass:
    """
        Class for the tun paralist
    """

    def __init__(self, remark_str):
        self.name = ''
        self.value = []
        self.remark = remark_str

class FFClass(object):
    """
        folder and file classs
    """
    def __init__(self):
        self.root_folder = ''
        self.set_root_folder()

    def set_root_folder(self):
        """
            set the root folder 
            just in case in the IPTOP folder is not placed under the folder of GitHub
        """
        folder = os.path.abspath(os.path.dirname(__file__))
        x = folder.split("\\")
        for i in range(0, len(x)-1):
            print("xi={0},xi+1={1}".format(x[i],x[i+1]))
            if x[i+1] !="IPTOP":
                self.root_folder = self.root_folder + x[i]+"\\"
            else:
                self.root_folder = self.root_folder  + x[i]+"\\"
                break



