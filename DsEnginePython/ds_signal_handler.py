from enum import Enum
from dataclasses import dataclass

# Status types of segment
class ds_status(Enum):
    R = 'R' # ready
    G = 'G' # going
    F = 'F' # finish
    H = 'H' # homing
    P = 'P' # pause

# Object type
class ds_object(Enum):
    Relay = 'Relay' # normal relay
    Tag = 'Tag' # tag of segment
    Segment = 'Segment' # port of segment
    Expression = 'Expression' # expression

# Represent the signal status as a result of estimation
@dataclass
class signal_set:
    start:bool = False
    reset:bool = False
    end:bool = False
    pause:bool = False

# For input signal
@dataclass
class signal_status:
    name:str = None
    signal:signal_set = signal_set()
    timestamp:float = 0.0

# Flags of ds system
@dataclass
class ds_system_flag(Enum):
    always_on:bool
    always_off:bool
    now_runing:bool
    run_rising:bool
    run_falling:bool
    system_stop:bool
    emergency_stop:bool
    virtual_test:bool

def calc_now_status(_signal:signal_set):
    now_status = ds_status.R
    if _signal == signal_set():
        now_status = ds_status.R
    elif _signal.start == True and _signal.end == False:
        now_status = ds_status.G
    elif _signal.end == True and _signal.reset == False:
        now_status = ds_status.F
    elif _signal.reset == True:
        now_status = ds_status.H
    elif _signal.pause == True:
        now_status = ds_status.P
    return now_status

def get_type(_name):
    obj_type = _name.split('.').pop()
    if obj_type == "Relay":
        return ds_object.Relay
    elif obj_type == "Tag":
        return ds_object.Tag
    else:
        return ds_object.Segment

# params[0] : object:signal_set
# params[1] : target_status:ds_status
def compare_signals(_params):
    object, target_signal = _params
    return calc_now_status(object) == target_signal

def get_origin_status(_params):
    object, _ = _params
    return object.end