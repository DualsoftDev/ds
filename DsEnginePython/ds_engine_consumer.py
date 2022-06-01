from ds_data_handler import signal_set
from kafka import KafkaConsumer
from json import loads

# default address - 'localhost:9092'
class consumer_set():
    def __init__(self, _topic_name, _target_server, _group_id):
        self.group_id = _group_id
        self.server =_target_server
        self.consumer = KafkaConsumer(
            _topic_name,
            bootstrap_servers = [self.server],
            auto_offset_reset = 'latest',
            enable_auto_commit = True,
            group_id = f"group-{self.group_id}",
            consumer_timeout_ms = 60000,
            value_deserializer = lambda x : loads(x.decode('utf-8'))
        )

    # "Topic : {message.topic}, "
    # "Partition : {message.partition}, "
    # "Key : {message.key}, "
    # "Offset : {message.offset}"
    # "Value : {message.value}"
    def get_data(self, _threads, _targets):
        print(f"[start] get consumer list, group id : {self.group_id}")
        print(_targets)
        for message in self.consumer:
            dic = message.value
            key = dic['name']
            signal = loads(dic['signal'])
            now_id = dic['group_id']
            if not now_id == self.group_id:
                if key in _targets:
                    now_signal = signal_set(signal['start'], signal['reset'], signal['end'])
                    if not now_signal == _threads[key].client.signal_onoff:
                        _threads[key].client.signal_onoff = \
                            signal_set(signal['start'], signal['reset'], signal['end'])
                        _threads[key].client.local_broadcast = True
                        _threads[key].client.event.set()
        print("[end] get consumer list")