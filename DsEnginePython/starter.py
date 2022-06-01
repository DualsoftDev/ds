from ds_data_handler import imparter, signal_set
from ds_data_handler import ds_signal_exchanger
from ds_data_handler import thread_with_exception
from kafka import KafkaConsumer
from json import loads

tag_start_S1 = ds_signal_exchanger()
impStartS1 = imparter()
tag_start_S1.connect("me.S0_starter.Tag", "me.S0_starter.Tag", impStartS1)

exchanger_dict = {}
exchanger_dict["me.S0_starter.Tag"] = tag_start_S1

outer = ["me.S0_starter.Tag"]

thread = {
    obj[0] : thread_with_exception([obj[1]])
    for obj in exchanger_dict.items()
}

for th in thread.values():
    th.start()

for th in thread.values():
    th.client.system_flags.always_on = True
    th.client.consumer_group = 2
    th.client.setting_up()
    th.client.event.set()

consumer = KafkaConsumer(
    "ds_engine",
    bootstrap_servers = ['localhost:9092'],
    auto_offset_reset = 'latest',
    enable_auto_commit = True,
    group_id = f"group-{2}",
    consumer_timeout_ms = 5000,
    value_deserializer = lambda x : loads(x.decode('utf-8'))
)

print(f"id : {2}")
print('[start] get consumer list')

thread["me.S0_starter.Tag"].client.signal_onoff.start = True
thread["me.S0_starter.Tag"].client.event.set()

for message in consumer:
    dic = message.value
    key = dic['name']
    signal = loads(dic['signal'])
    if key in outer:
        now_signal = signal_set(
            signal['start'], signal['reset'], signal['end'], signal['pause']
        )
        if not now_signal == thread[key].client.signal_onoff:
            thread[key].client.signal_onoff = \
                signal_set(
                    signal['start'], signal['reset'], signal['end'], signal['pause']
                )
            thread[key].client.local_broadcast = True
            thread[key].client.event.set()
    
    if thread["me.S0_starter.Tag"].client.signal_onoff == signal_set(False, False, False, False):
        print("=====================================")
        thread["me.S0_starter.Tag"].client.signal_onoff.start = True
        thread["me.S0_starter.Tag"].client.local_broadcast = False
        thread["me.S0_starter.Tag"].client.event.set()
print('[end] get consumer list')