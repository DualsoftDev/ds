from enum import Enum
from dataclasses import dataclass

# Causal type for relaying
class ds_causal(Enum):
    G = 0
    H = 1

# Status types of segment
class ds_status(Enum):
    R = 0 # ready
    G = 1 # going
    F = 2 # finish
    H = 3 # homing
    
# Object type
class ds_object(Enum):
    Relay = 0 # normal relay
    Remote = 1  # remote relay
    Tag = 2 # tag of segment
    Segment = 3 # port of segment

# Represent the signal status as a result of estimation
@dataclass
class signal_set:
    start:bool = False
    reset:bool = False
    end:bool = False

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

def get_type(_name):
    obj_type = _name.split('.').pop()
    if obj_type == "Relay":
        return ds_object.Relay
    elif obj_type == "Remote":
        return ds_object.Remote
    elif obj_type == "Tag":
        return ds_object.Tag
    else:
        return ds_object.Segment

# params[0] : my_name
# params[1] : my_type
# params[2] : target_name
# params[3] : object
# params[4] : causal_type
def get_start_signals(params):
    _, _my_type, _target_name,\
    _object = params 

    obj_type = get_type(_target_name)
    if _my_type == ds_object.Relay:
        if obj_type == ds_object.Relay or \
            obj_type == ds_object.Remote:
            return _object.end
        elif obj_type == ds_object.Segment:
            return _object.start
    elif _my_type == ds_object.Tag:
        if obj_type == ds_object.Relay:
            return _object.start
        elif obj_type == ds_object.Tag:
            return _object.start
    else:
        if obj_type == ds_object.Tag:
            return _object.start

def get_reset_signals(params):
    _my_name, _my_type, _target_name,\
    _object = params 

    obj_type = get_type(_target_name)
    if _my_type == ds_object.Relay or \
        _my_type == ds_object.Remote:
        if obj_type == ds_object.Relay or \
            obj_type == ds_object.Remote:
            if _target_name == _my_name:
                return _object.end
            else:
                return _object.start
        elif obj_type == ds_object.Segment:
            return _object.reset
    elif _my_type == ds_object.Tag:
        if obj_type == ds_object.Relay or \
            obj_type == ds_object.Remote:
            return _object.reset
        elif obj_type == ds_object.Tag:
            return _object.reset
    else:
        if obj_type == ds_object.Tag:
            return _object.reset

def get_end_signals(params):
    _, _my_type, _target_name, _object = params 

    obj_type = get_type(_target_name)
    if _my_type == ds_object.Relay or \
        _my_type == ds_object.Remote:
        if obj_type == ds_object.Tag:
            return _object.end
    elif _my_type == ds_object.Tag:
        if obj_type == ds_object.Segment:
            return _object.end
        elif obj_type == ds_object.Tag:
            return _object.end
    else:
        if obj_type == ds_object.Relay:
            return (not _object.reset) & _object.end

def get_clear_signals(params):
    _, _my_type, _target_name, _object = params 

    obj_type = get_type(_target_name)
    if _my_type == ds_object.Tag:
        if obj_type == ds_object.Segment:
            return _object == signal_set()
        elif obj_type == ds_object.Tag:
            return _object == signal_set()
    elif _my_type == ds_object.Relay or \
        _my_type == ds_object.Remote:
        if obj_type == ds_object.Tag:
            return _object == signal_set()
        elif obj_type == ds_object.Segment:
            return _object == signal_set()
    else:
        if obj_type == ds_object.Relay:
            return _object == signal_set()

def get_origin_status(params):
    _, _my_type, _target_name, _object = params 

    obj_type = get_type(_target_name)
    if _my_type == ds_object.Segment:
        if obj_type == ds_object.Tag:
            return _object.end