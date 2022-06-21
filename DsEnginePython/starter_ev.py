from ds_data_handler import imparter, signal_set
from ds_data_handler import ds_signal_exchanger
from ds_data_handler import thread_with_exception
from kafka import KafkaConsumer
from json import loads
import time

tag_start_f1 = ds_signal_exchanger()
tag_start_f2 = ds_signal_exchanger()
tag_start_f3 = ds_signal_exchanger()
tag_v_tester_btn_push = ds_signal_exchanger()
tag_v_tester_snr_onoff = ds_signal_exchanger()
tag_v_tester_mtr_rot = ds_signal_exchanger()
starter_sys = imparter()
tag_start_f1.connect("button.push.floor_1_starter.Tag", "button.push.floor_1_starter.Tag", starter_sys)
tag_start_f2.connect("button.push.floor_2_starter.Tag", "button.push.floor_2_starter.Tag", starter_sys)
tag_start_f3.connect("button.push.floor_3_starter.Tag", "button.push.floor_3_starter.Tag", starter_sys)
tag_v_tester_btn_push.connect  ("button.push.virtual_test.Tag", "button.push.virtual_test.Tag", starter_sys)
tag_v_tester_snr_onoff.connect("sensor.onoff.virtual_test.Tag", "sensor.onoff.virtual_test.Tag", starter_sys)
tag_v_tester_mtr_rot.connect("motor.rotate.virtual_test.Tag", "motor.rotate.virtual_test.Tag", starter_sys)

exchanger_dict = {}
exchanger_dict["button.push.floor_1_starter.Tag"] = tag_start_f1
exchanger_dict["button.push.floor_2_starter.Tag"] = tag_start_f2
exchanger_dict["button.push.floor_3_starter.Tag"] = tag_start_f3
exchanger_dict["button.push.virtual_test.Tag"] = tag_v_tester_btn_push
exchanger_dict["sensor.onoff.virtual_test.Tag"] = tag_v_tester_snr_onoff
exchanger_dict["motor.rotate.virtual_test.Tag"] = tag_v_tester_mtr_rot

# outer = ["button.push.floor_1_starter.Tag", "button.push.floor_2_starter.Tag", "button.push.floor_3_starter.Tag"]

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

thread["button.push.virtual_test.Tag"].client.signal_onoff.end = True
thread["button.push.virtual_test.Tag"].client.event.set()

thread["sensor.onoff.virtual_test.Tag"].client.signal_onoff.end = True
thread["sensor.onoff.virtual_test.Tag"].client.event.set()

thread["motor.rotate.virtual_test.Tag"].client.signal_onoff.end = True
thread["motor.rotate.virtual_test.Tag"].client.event.set()

thread["button.push.floor_1_starter.Tag"].client.signal_onoff.start = True
thread["button.push.floor_1_starter.Tag"].client.event.set()
time.sleep(10.0)

while True:
    thread["button.push.floor_2_starter.Tag"].client.signal_onoff.start = True
    thread["button.push.floor_2_starter.Tag"].client.event.set()
    time.sleep(10.0)

    thread["button.push.floor_3_starter.Tag"].client.signal_onoff.start = True
    thread["button.push.floor_3_starter.Tag"].client.event.set()
    time.sleep(10.0)

    thread["button.push.floor_2_starter.Tag"].client.signal_onoff.start = True
    thread["button.push.floor_2_starter.Tag"].client.event.set()
    time.sleep(10.0)

    thread["button.push.floor_1_starter.Tag"].client.signal_onoff.start = True
    thread["button.push.floor_1_starter.Tag"].client.event.set()
    time.sleep(10.0)
        
print('[end] get consumer list')