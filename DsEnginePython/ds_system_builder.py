import time
from abc import *
from typing import List, Dict, Union
from ds_data_handler import imparter
from ds_data_handler import ds_signal_exchanger
from ds_data_handler import thread_with_exception
from ds_signal_handler import ds_status
from ds_engine_consumer import consumer_set

# 기본 틀
class basic_frame(metaclass=ABCMeta):
    def __init__(self, _name:str):
        self.raw_name:str = _name
        self.name:str = f"{_name}"
        self.imparter:imparter = imparter()

    # 내용 변경에 대한 알림자를 가져옴
    @abstractmethod
    def get_imparter(self):
        pass

    # signam exchanger를 가져옴
    @abstractmethod
    def get_exchanger(self):
        pass

    # 입력된 이름을 가져옴
    @abstractmethod
    def get_raw_name(self):
        pass

    # ds object에 대한 start signal을 등록함
    @abstractmethod
    def assign_start_signal(self):
        pass
    
    # ds object에 대한 end signal을 등록함
    @abstractmethod
    def assign_end_signal(self):
        pass
    
    # ds object에 대한 reset signal을 등록함
    @abstractmethod
    def assign_reset_signal(self):
        pass
    
    # ds object에 대한 clear signal을 등록함
    @abstractmethod
    def assign_clear_signal(self):
        pass

    # ds object을 push타입 만들거나 취소함
    @abstractmethod
    def toggle_pausable_switch(self, _on:bool):
        pass

# ds system 정의
class ds_system:
    def __init__(self, _name:str):
        self.raw_name:str = _name
        self.name:str = _name
        self.imparter:imparter = imparter()

    def get_imparter(self):
        return self.name, self.imparter

    def get_raw_name(self):
        return self.raw_name

# ds relay 정의
class ds_relay(basic_frame):
    def __init__(self,
        _name:str, _parent_name:str, _parent_imp:imparter,
        _self_check:bool = True):
        self.raw_name:str = f"{_name}"
        self.name:str = f"{_name}.Relay"
        self.imparter:imparter = imparter()
        self.exchanger:ds_signal_exchanger = ds_signal_exchanger()

        # Relay는 자신의 부모 segment와 
        self.exchanger.connect(self.name, _parent_name, _parent_imp)
        # 자신에게 속해있는 tag에게 상태를 전달해야 함
        self.exchanger.connect(self.name, self.name, self.imparter)

        if _self_check == True:
            # 자신의 상태가 going일 떄 finish 가능
            self.exchanger.end_signals[self.name] = ds_status.G

            # 자신의 상태가 homing일 때 clear -> ready 가능
            self.exchanger.clear_signals[self.name] = ds_status.H

        # Relay는 외부 신호에 크게 의존하지 않기 때문에(신뢰)
        # Macro를 통해 push타입임을 명시하지 않으면
        # 외부 신호를 이용해 start/reset에 대한 pause를 할 수 없음
        self.exchanger.pausable_type = False

    def get_imparter(self):
        return self.name, self.imparter
        
    def get_exchanger(self):
        return self.name, self.exchanger

    def get_raw_name(self):
        return self.raw_name

    # Relay는 자신이 속한 system이나 segment의 imparter로부터 
    # Start/reset 신호를 받으며
    # system에 위치했을 때는 같은 depth의 tag나 relay의 신호를 조건으로
    # 자신의 start 신호를 1로 만듦
    def assign_start_signal(self, 
        _name:str, _status:ds_status):
        self.exchanger.start_signals[_name] = _status

    # Relay는 자신의 상태가 going이고
    # Tag의 상태가 finish일 때 end 신호를 1로 변경함
    def assign_end_signal(self, 
        _name:str, _status:ds_status):
        self.exchanger.end_signals[_name] = _status
    
    # Relay는 자신과 연결된 object(relay, tag, segment)들의 상태에 따라
    # Reset 신호를 1로 만들 수 있음
    def assign_reset_signal(self, 
        _name:str, _status:ds_status):
        self.exchanger.reset_signals[_name] = _status

    # Relay는 자신에게 연결된 tag의 상태가 ready이면
    # Reset과 end를 0으로 만들 수 있음
    def assign_clear_signal(self, 
        _name:str, _status:ds_status):
        self.exchanger.clear_signals[_name] = _status

    # pausable type on off
    def toggle_pausable_switch(self, _on:bool = False):
        self.exchanger.pausable_type = _on

# ds tag 정의
class ds_tag(basic_frame):
    def __init__(self,
        _name:str, _parent_name:str, _parent_imp:imparter,
        _self_check:bool = True):
        self.raw_name:str = _name
        self.name:str = f"{_name}.Tag"
        self.imparter:imparter = imparter()
        self.exchanger:ds_signal_exchanger = ds_signal_exchanger()

        # Tag는 자신의 부모 Relay와
        self.exchanger.connect(self.name, _parent_name, _parent_imp)
        # 자신에게 속해있는 segment에게 상태를 전달해야 함
        self.exchanger.connect(self.name, self.name, self.imparter)

        if _self_check == True:
            # 자신의 상태가 going일 떄 finish 가능
            self.exchanger.end_signals[self.name] = ds_status.G

            # 자신의 상태가 homing일 때 clear -> ready 가능
            self.exchanger.clear_signals[self.name] = ds_status.H

        # Tag는 start와 reset을 외부 신호에 의존하는
        # pushs/pushr 타입
        self.exchanger.pausable_type = True

    def get_imparter(self):
        return self.name, self.imparter
        
    def get_exchanger(self):
        return self.name, self.exchanger

    def get_raw_name(self):
        return self.raw_name

    # Tag는 연결된 relay의 상태가 going일 때 start 신호를 1로 변경함
    def assign_start_signal(self, 
        _name:str, _status:ds_status):
        self.exchanger.start_signals[_name] = _status
    
    # Tag는 자신의 상태가 going이고 자신과 연결된 segment의 상태가
    # Finish일 때 end 신호를 1로 변경함
    def assign_end_signal(self, 
        _name:str, _status:ds_status):
        self.exchanger.end_signals[_name] = _status

    # Tag는 relay의 상태가 homing일 때 reset 신호를 1로 변경함
    def assign_reset_signal(self, 
        _name:str, _status:ds_status):
        self.exchanger.reset_signals[_name] = _status

    # Tag는 자신에게 연결된 semgnet의 상태가 ready이면
    # Reset과 end를 0으로 만들 수 있음
    def assign_clear_signal(self, 
        _name:str, _status:ds_status):
        self.exchanger.clear_signals[_name] = _status

    # pausable type on off
    def toggle_pausable_switch(self, _on:bool = True):
        self.exchanger.pausable_type = _on

# ds segment 정의
class ds_segment(basic_frame):
    def __init__(self,
        _name:str, _parent_name:str, _parent_imp:imparter,
        _self_check:bool = True):
        self.raw_name:str = _name
        self.name:str = f"{_name}"
        self.imparter:imparter = imparter()
        self.exchanger:ds_signal_exchanger = ds_signal_exchanger()

        # Segment(port)는 자신의 부모 tag와
        self.exchanger.connect(self.name, _parent_name, _parent_imp)
        # 자신에게 속해있는 relay에게 상태를 전달해야 함
        self.exchanger.connect(self.name, self.name, self.imparter)

        if _self_check == True:
            # 자신의 상태가 going일 떄 finish 가능
            self.exchanger.end_signals[self.name] = ds_status.G

            # 자신의 상태가 homing일 때 clear -> ready 가능
            self.exchanger.clear_signals[self.name] = ds_status.H

        # Segment(port)는 start와 reset을 외부 신호에 의존하는
        # pushs/pushr 타입
        self.exchanger.pausable_type = True

    def get_imparter(self):
        return self.name, self.imparter
        
    def get_exchanger(self):
        return self.name, self.exchanger

    def get_raw_name(self):
        return self.raw_name

    # 연결된 tag의 상태가 going일 때 start 신호를 1로 변경함
    def assign_start_signal(self, 
        _name:str, _status:ds_status):
        self.exchanger.start_signals[_name] = _status
    
    # Segment(port)는 자신의 상태가 going이면서
    # 자식 relay들의 상태가 finish일 때 end 신호를 1로 변경할 수 있음
    def assign_end_signal(self, 
        _name:str, _status:ds_status):
        self.exchanger.end_signals[_name] = _status
    
    # Tag의 상태가 homing일 때 reset 신호를 1로 변경함
    def assign_reset_signal(self, 
        _name:str, _status:ds_status):
        self.exchanger.reset_signals[_name] = _status
    
    # Segment(port)는 자신의 상태가 going이면서 
    # 자식 relay들의 상태가 finish일 때 end 신호를 1로 변경할 수 있음   
    def assign_clear_signal(self, 
        _name:str, _status:ds_status):
        self.exchanger.clear_signals[_name] = _status
    
    # Segment(port)는 자신이 homing상태 일 때
    # 등록된 자식 relay들을 통해 강제로 자식들의 상태 조합을
    # Start point로 만들어줄 수 있음
    def assign_homing_signal(self,
        _name:str, _status:ds_status):
        self.exchanger.homing_signals[_name] = _status
     
    # Segment(port)는 자신의 상태가 homing이면서
    # 자식 relay들의 상태가 등록된 start point중 하나이면 
    # Reset과 end를 0으로 만들 수 있음
    def assign_origin_signal(self, _name:str):
        self.exchanger.origin_signals.append(_name)

    def assign_start_points(self, _theta:int):
        self.exchanger.start_points.append(_theta)

    # pausable type on off
    def toggle_pausable_switch(self, _on:bool = True):
        self.exchanger.pausable_type = _on

# ds system 빌드를 위한 내용 정의 및
# kafka 연결을 위한 consumer 생성
class ds_consumer_builder:
    def __init__(self):
        self.outer_signals:List[str] = []
        self.exchanger_dict:Dict[str, ds_signal_exchanger] = {}
        self.threads:Dict[str, thread_with_exception] = {}
        self.consumer:consumer_set = None
        self.ds_objects:Dict[
            str, Union(ds_system, ds_relay, ds_tag, ds_segment)
        ] = {}
        self.raw_name_list:List[str] = []

    # Parsing과정에서 생성한 ds object(system/relay/tag/segment)를 등록함
    def assign_object(self, 
        _object:Union[ds_system, ds_relay, ds_tag, ds_segment]):
        if type(_object) == ds_system:
            _name, _ = _object.get_imparter()
        else:
            _name, _exchanger = _object.get_exchanger()
            self.exchanger_dict[_name] = _exchanger
        self.ds_objects[_name] = _object
        self.raw_name_list.append(_object.get_raw_name())

    # Kafka를 통해 변경 내용을 받아올 object 이름을 등록함
    def assign_outer_object(self, _name:str):
        self.outer_signals.append(_name)

    # 등록된 object 확인

    def check_object_in(self, _name:str):
        if _name in self.ds_objects:
            return True
        else:
            return False
    def check_object_in_raw(self, _name:str):
        if _name in self.raw_name_list:
            return True
        else:
            return False

    # 등록된 object를 가져옴
    def get_object(self, _name:str):
        return self.ds_objects[_name]

    def get_object_list(self):
        return self.ds_objects
    
    # 등록된 ds object들을 이용하여 thread를 생성
    def build_threads(self):
        self.threads = {
            obj[0] : thread_with_exception([obj[1]])
            for obj in self.exchanger_dict.items()
        }

    # 생성된 thread를 start함
    def start_threads(self):
        for thread in self.threads.values():
            thread.start()
    
    # start된 thread들에 기본적인 내용들을 셋 해줌
    def initialize_threads(self, _id:int):
        for thread in self.threads.values():
            thread.client.system_flags.always_on = True
            thread.client.consumer_group = _id
            thread.client.setting_up()
            thread.client.event.set()
    
        time.sleep(3.0)
        print("initialize finished")

    # kafak를 통한 엔진 구동부
    # 각 object별로 thread를 생성하고
    # kafka에 consumer로 등록하여 입력을 기다림
    def execute_system(self, _topic:str, _address:str, _id:int):
        self.build_threads()
        self.start_threads()
        self.initialize_threads(_id)

        self.consumer = consumer_set(_topic, _address, _id)
        self.consumer.get_data(self.threads, self.outer_signals)