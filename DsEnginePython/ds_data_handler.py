from json import dumps
import time
import copy
import ctypes 
import threading
import traceback
from typing import List, Dict
from datetime import datetime
from observer_threading import IObserver
from observer_threading import Observable
from ds_signal_handler import ds_status, ds_object, ds_system_flag
from ds_signal_handler import signal_status, signal_set
from ds_signal_handler import compare_signals
from ds_signal_handler import get_origin_status, get_type
from ds_signal_handler import calc_now_status
from functools import reduce
from ds_engine_producer import send_data

class imparter(Observable):
    def notify_status(self, _id, _onoff_status:signal_status):
        self.notify(_id, _onoff_status)

class ds_signal_exchanger(IObserver):
    def __init__(self):
        self.system_flags:ds_system_flag = ds_system_flag
        self.name:str = None
        self.updated_by:str = None
        self.imparter:Dict[str, imparter] = {}
        self.in_signals:Dict[str, signal_set] = {}
        self.start_signals:Dict[str, ds_status] = {}
        self.start_expression:str = None
        self.reset_signals:Dict[str, ds_status] = {}
        self.reset_expression:str = None
        self.end_signals:Dict[str, ds_status] = {}
        self.clear_signals:Dict[str, signal_set] = {}
        self.homing_signals:Dict[str, signal_set] = {}
        self.origin_signals:List[str] = []
        self.white_list:List[str] = []
        self.start_points:List[int] = []
        self.signal_onoff:signal_set = signal_set()
        self.status:ds_status = ds_status.R
        self.take_time:float = 0.0
        self.consumer_group:int = -1
        self.local_broadcast:bool = False
        self.event = threading.Event()
        self.thread_id = None

    def list_extractor(self, _target):
        return [name for name in _target]

    def setting_up(self):
        self.white_list.extend(self.list_extractor(self.start_signals))
        self.white_list.extend(self.list_extractor(self.end_signals))
        self.white_list.extend(self.list_extractor(self.reset_signals))
        self.white_list.extend(self.list_extractor(self.clear_signals))
        self.white_list.extend(self.list_extractor(self.homing_signals))
        self.white_list.extend(self.list_extractor(self.origin_signals))

    def truncate_signals(self, _new_signal:signal_set, _signals):
        for signal in _signals:
            if calc_now_status(_new_signal) == ds_status.G:
                self.in_signals[signal].end = False
                self.in_signals[signal].start = False
            elif calc_now_status(_new_signal) == ds_status.H:
                self.in_signals[signal].start = False
                self.in_signals[signal].reset = False

    def get_target_signals(self, _target_signals, _signal_func):
        return [
            _signal_func(
                (
                    self.in_signals[name],
                    _target_signals[name] if type(_target_signals) == dict
                    else None
                )
            )
            for name in _target_signals
            if name in self.in_signals
        ]

    def origin_checker(self):
        try:
            if len(self.origin_signals) > 0:
                theta = reduce(
                    lambda num, bin: (num<<1) + int(bin), \
                    self.get_target_signals(self.origin_signals, get_origin_status)
                )
                if not theta in self.start_points:
                    return False
            return True
        except Exception as ex:
            print(self.name, "has origin error", ex)
            print(traceback.format_exc())
            pass

    def check_ready_to_going(self, _my_type:ds_object, _new_onoff_signal:signal_set):
        try:
            res = False
            res = self.get_target_signals(self.start_signals, compare_signals)

            # RELAY : all
            # TAG & PORT : any
            start_checker = False
            now_type = get_type(self.name)
            basic_condition = len(res) > 0 and self.system_flags.always_on
            if now_type == ds_object.Relay:
                start_checker = basic_condition and all(res)
            elif now_type == ds_object.Tag:
                start_checker = basic_condition and any(res)
            else:
                start_checker = \
                    basic_condition and any(res) and self.origin_checker()

            if start_checker == True:
                _new_onoff_signal.start = True

                # need condition of macro for push type
                self.truncate_signals(_new_onoff_signal, self.start_signals)
            
            return _new_onoff_signal
        except Exception as ex:
            print(self.name, "has start error", ex)
            print(traceback.format_exc())
            pass

    def check_going_to_finish(self, _my_type:ds_object, _new_onoff_signal:signal_set):
        try:
            res = self.get_target_signals(self.end_signals, compare_signals)
            if len(res) > 0 and self.system_flags.always_on and all(res):
                _new_onoff_signal.end = True
                _new_onoff_signal.start = False
            
            return _new_onoff_signal
        except Exception as ex:
            print(self.name, "has end error", ex)
            print(traceback.format_exc())
            pass
    
    def check_finish_to_homing(self, _my_type:ds_object, _new_onoff_signal:signal_set):
        try:
            res = self.get_target_signals(self.reset_signals, compare_signals)
            basic_condition = len(res) > 0 and self.system_flags.always_on

            if basic_condition and any(res):
                _new_onoff_signal.reset = True
                _new_onoff_signal.start = False

                # need condition of macro for push type
                # self.truncate_signals(_new_onoff_signal, self.reset_signals)

            return _new_onoff_signal
        except Exception as ex:
            print(self.name, "has reset error", ex)
            print(traceback.format_exc())
            pass

    def check_homing_to_ready(self, _my_type:ds_object, _new_onoff_signal:signal_set):
        try:
            res = self.get_target_signals(self.clear_signals, compare_signals)
            basic_condition = len(res) > 0 and self.system_flags.always_on
            if _my_type == ds_object.Segment:
                homing_res = True
                if len(self.homing_signals) > 0:
                    homing_res = \
                        self.get_target_signals(self.homing_signals, compare_signals)
                if basic_condition and all(res) \
                    and self.origin_checker() and homing_res:
                    _new_onoff_signal = signal_set()
            else:
                if basic_condition and all(res):
                    _new_onoff_signal = signal_set()
            
            return _new_onoff_signal
        except Exception as ex:
            print(self.name, "has clear error", ex)
            print(traceback.format_exc())
            pass

    def estimate(self):
        my_type = get_type(self.name)
        new_onoff_signal = copy.deepcopy(self.signal_onoff)
        now_status = calc_now_status(new_onoff_signal)

        if now_status == ds_status.R:
            return self.check_ready_to_going(my_type, new_onoff_signal)
        elif now_status == ds_status.G:
            return self.check_going_to_finish(my_type, new_onoff_signal)
        elif now_status == ds_status.F:
            return self.check_finish_to_homing(my_type, new_onoff_signal)
        elif now_status == ds_status.H:
            return self.check_homing_to_ready(my_type, new_onoff_signal)

    def update(self, _id, _onoff_status:signal_status):
        if not _onoff_status.name in self.white_list:
            return
            
        self.in_signals[_onoff_status.name] = copy.deepcopy(_onoff_status.signal)
        self.updated_by = _onoff_status.name

        estimated = self.estimate()
        if not self.signal_onoff == estimated:
            self.signal_onoff = copy.deepcopy(estimated)
            time.sleep(self.take_time)
            self.event.set()

    def connect(self, _name:str, _parent:str, _imparter:imparter):
        self.name = _name
        self.imparter[_parent] = _imparter
        self.imparter[_parent].attach(self)

    def disconnect(self, _parent):
        self.imparter[_parent].detach(self)
        self.imparter[_parent] = None

    def change_event(self, _id = ""):
        t = time.time()
        print(
            f"{self.name}[{_id}] : "\
            f"{calc_now_status(self.signal_onoff)}"\
            f" - {datetime.fromtimestamp(t)}"\
            f" / updated by {self.updated_by}"
        )
        if not self.local_broadcast == True:
            send_data(
                {
                    "name" : f"{self.name}",
                    "signal" : dumps(self.signal_onoff.__dict__),
                    "status" : f"{calc_now_status(self.signal_onoff)}",
                    "time" : f"{datetime.fromtimestamp(t)}",
                    "group_id" : self.consumer_group
                }
            )
        for parent in self.imparter:
            self.imparter[parent].notify_status(
                _id, 
                signal_status(
                    self.name, 
                    copy.deepcopy(self.signal_onoff), 
                    t
                )
            )
        self.local_broadcast = False
                       
class thread_with_exception(threading.Thread): 
    def __init__(self, _client:List[ds_signal_exchanger]): 
        threading.Thread.__init__(self)
        self.client = _client[0]
        
    def run(self):
        # target function of the thread class 
        try: 
            while True: 
                self.client.event.clear()
                if self.client.thread_id == None:
                    self.client.thread_id = self.get_id()
                self.client.change_event(self.client.thread_id)
                self.client.event.wait()
        except Exception as ex:
            print(
                "error message : ", self.client.name, \
                " - ", ex, " - ", self.client.in_signals, \
                "thread id : ", self.client.thread_id
            )
            print(traceback.format_exc())
        finally: 
            print("ended") 
           
    def get_id(self): 
        # returns id of the respective thread 
        if hasattr(self, "_thread_id"): 
            return self._thread_id 
        for id, thread in threading._active.items(): 
            if thread is self: 
                return id
   
    def raise_exception(self): 
        thread_id = self.get_id() 
        res = ctypes.pythonapi.PyThreadState_SetAsyncExc(thread_id, 
              ctypes.py_object(SystemExit)) 
        if res > 1: 
            ctypes.pythonapi.PyThreadState_SetAsyncExc(thread_id, 0) 
            print("Exception raise failure") 