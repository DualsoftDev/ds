import time
from ds_data_handler import signal_set
from kafka import KafkaConsumer
from json import loads
import numpy as np

rand_nums = np.random.rand(1000)
consumer = KafkaConsumer(
    'test',
    bootstrap_servers = ['localhost:9092'],
    auto_offset_reset = 'latest',
    enable_auto_commit = True,
    group_id = f"group-{rand_nums}",
    consumer_timeout_ms = 10000,
    value_deserializer = lambda x : loads(x.decode('utf-8'))
)

# "Topic : {message.topic}, "
# "Partition : {message.partition}, "
# "Key : {message.key}, "
# "Offset : {message.offset}"
# "Value : {message.value}"
def get_data(_name, _threads, _targets):
    print('[start] get consumer list')
    for message in consumer:
        dic = message.value
        key = dic['name']
        signal = loads(dic['signal'])
        # print(f"{_name} --------- {dic['name']} : {signal['start']}, {signal['reset']}, {signal['end']}")
        if key in _targets:
            now_signal = signal_set(signal['start'], signal['reset'], signal['end'])
            if not now_signal == _threads[key].client.signal_onoff:
                _threads[key].client.signal_onoff = \
                    signal_set(signal['start'], signal['reset'], signal['end'])
                _threads[key].client.event.set()
        # print(f"{dic['name']} : {signal['start']}, {signal['reset']}, {signal['end']}")
    print('[end] get consumer list')