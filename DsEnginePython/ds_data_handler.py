
import time
import copy
import ctypes 
import threading
from typing import List, Dict
from datetime import datetime
from observer_threading import IObserver
from observer_threading import Observable
from ds_signal_handler import ds_status, ds_object, ds_system_flag
from ds_signal_handler import signal_status, signal_set
from ds_signal_handler import get_start_signals, get_end_signals
from ds_signal_handler import get_reset_signals, get_clear_signals
from ds_signal_handler import get_type

class imparter(Observable):
    def notify_status(self, _id, _onoff_status:signal_status):
        self.notify(_id, _onoff_status)

class ds_signal_exchanger(IObserver):
    def __init__(self):
        self.system_flags:ds_system_flag = ds_system_flag
        self.name:str = None
        self.imparter:Dict[str, imparter] = {}
        self.in_signals:Dict[str, signal_set] = {}
        self.start_signals:List[str] = []
        self.reset_signals:List[str] = []
        self.end_signals:List[str] = []
        self.clear_signals:List[str] = []
        self.signal_onoff = signal_set()
        self.status = ds_status.R
        self.event = threading.Event()
        self.thread_id = None

    def calc_now_status(self, _signal:signal_set):
        if _signal == signal_set(False, False, False):
            self.status = ds_status.R
        elif _signal.start == True and not _signal.end == True:
            my_type = get_type(self.name)
            if my_type == ds_object.Remote:
                tag_name = self.name.replace("Remote", "Tag")
                if tag_name in self.in_signals and\
                    self.in_signals[tag_name].start == True:
                    self.status = ds_status.G
            else:
                self.status = ds_status.G
        elif _signal.end == True and _signal.reset == False:
            self.status = ds_status.F
        elif _signal.reset == True and _signal.end == True:
            self.status = ds_status.H

        return self.status

    def truncate_signals(self, _new_signal:signal_set, _signals):
        for signal in _signals:
            if self.calc_now_status(_new_signal) == ds_status.G:
                self.in_signals[signal].end = False
                self.in_signals[signal].start = False
            elif self.calc_now_status(_new_signal) == ds_status.H:
                self.in_signals[signal].start = False
                self.in_signals[signal].reset = False

    def get_target_signals(self, my_type, target_signals, signal_func):
        return [
            signal_func(self.name, my_type, name, self.in_signals[name])
            for name in target_signals
            if name in self.in_signals
        ]

    def estimate(self):
        my_type = get_type(self.name)
        new_onoff_signal = copy.deepcopy(self.signal_onoff)
        if new_onoff_signal == signal_set(): # Ready
            try:
                res = self.get_target_signals(my_type, self.start_signals, get_start_signals)

                # RELAY : all
                # TAG & PORT : any
                start_checker = False
                now_type = get_type(self.name)
                if now_type == ds_object.Relay:
                    start_checker = len(res) > 0 and all(res) and self.system_flags.always_on
                elif now_type == ds_object.Remote:
                    going_checker = False
                    tag_name = copy.deepcopy(self.name)
                    tag_name = tag_name.replace("Remote", "Tag")
                    if tag_name in self.in_signals and\
                        self.in_signals[tag_name].start == True:
                        going_checker = True

                    start_checker = self.system_flags.always_on and going_checker
                else:
                    start_checker = len(res) > 0 and any(res) and self.system_flags.always_on

                if start_checker == True:
                    new_onoff_signal.start = True

                    # need condition of macro for push type
                    self.truncate_signals(new_onoff_signal, self.start_signals)
            except Exception as ex:
                # print(self.name, "has start error", ex)
                pass
        elif new_onoff_signal.start == True and new_onoff_signal.end == False: # Going
            try:
                res = self.get_target_signals(my_type, self.end_signals, get_end_signals)
                if len(res) > 0 and all(res) and self.system_flags.always_on == True:
                    new_onoff_signal.end = True
                    new_onoff_signal.start = False
            except Exception as ex:
                # print(self.name, "has end error", ex)
                pass
        elif new_onoff_signal.reset == False and new_onoff_signal.end == True: # Finish
            try:
                res = self.get_target_signals(my_type, self.reset_signals, get_reset_signals)
                if len(res) > 0 and any(res) and self.system_flags.always_on == True:
                    new_onoff_signal.reset = True
                    new_onoff_signal.start = False

                # For force clear by a remote tag when my type is a tag
                if my_type == ds_object.Tag:
                    res = self.get_target_signals(my_type, self.clear_signals, get_clear_signals)
                    if len(res) > 0 and all(res) and self.system_flags.always_on == True:
                        new_onoff_signal = signal_set()

                    # need condition of macro for push type
                    # self.truncate_signals(new_onoff_signal, self.reset_signals)
            except Exception as ex:
                # print(self.name, "has reset error", ex)
                pass
        elif new_onoff_signal.reset == True: # Homing
            try:
                res = self.get_target_signals(my_type, self.clear_signals, get_clear_signals)
                if len(res) > 0 and all(res) and self.system_flags.always_on == True:
                    new_onoff_signal = signal_set()
            except Exception as ex:
                # print(self.name, "has clear error", ex)
                pass

        return new_onoff_signal

    def estimate_new_status(self, _onoff_status:signal_status):
        estimated = self.estimate()
        if not self.signal_onoff == estimated:
            self.signal_onoff = copy.deepcopy(estimated)
            self.event.set()

            if len(self.clear_signals) == 0:
                if self.signal_onoff.start == True and\
                    self.signal_onoff.end == False and\
                    get_type(self.name) == ds_object.Segment:
                    if len(self.end_signals) > 0:
                        estimated = self.estimate()
                        if not self.signal_onoff == estimated:
                            time.sleep(1.0)
                            self.signal_onoff.end = True
                            self.signal_onoff.start = False
                            self.event.set()
                            return
                    else:
                        time.sleep(1.0)
                        self.signal_onoff.end = True
                        self.signal_onoff.start = False
                        self.event.set()
                        return
                elif self.signal_onoff.end == True and\
                    self.signal_onoff.reset == True:
                    if len(self.clear_signals) == 0:
                        time.sleep(0.5)
                        self.signal_onoff = signal_set()
                        self.event.set()
                        return

    def update(self, _id, _onoff_status:signal_status):
        self.in_signals[_onoff_status.name] = copy.deepcopy(_onoff_status.signal)

        if not self.name == _onoff_status.name:
            self.estimate_new_status(_onoff_status)
        
        if get_type(self.name) == ds_object.Relay and\
            self.calc_now_status(self.signal_onoff) == ds_status.R or\
            self.calc_now_status(self.signal_onoff) == ds_status.F:
            estimated = self.estimate()
            if not self.signal_onoff == estimated:
                self.signal_onoff = copy.deepcopy(estimated)
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
            f"{self.calc_now_status(self.signal_onoff)}"\
            f" - {datetime.fromtimestamp(t)}"
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
            
class thread_with_exception(threading.Thread): 
    def __init__(self, _client:List[ds_signal_exchanger]): 
        threading.Thread.__init__(self)
        self.client = _client[0]
        
    def run(self):
        # target function of the thread class 
        try: 
            while True: 
                if self.client.thread_id == None:
                    self.client.thread_id = self.get_id()
                self.client.event.wait()
                self.client.change_event(self.client.thread_id)
                self.client.event.clear()
        except Exception as ex:
            print(
                "error message : ", self.client.name, \
                " - ", ex, " - ", self.client.in_signals
            )
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