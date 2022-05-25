import time
from ds_data_handler import imparter
from ds_data_handler import ds_signal_exchanger
from ds_data_handler import thread_with_exception
from ds_signal_handler import ds_status
from ds_engine_consumer import consumer_set

if __name__ == "__main__":
    system_it = imparter()

    relay_it_S1 = ds_signal_exchanger()
    imp_relay_it_S1 = imparter()
    tag_it_S1 = ds_signal_exchanger()
    imp_tag_it_S1 = imparter()
    it_S1 = ds_signal_exchanger()
    
    start_it_S1 = ds_signal_exchanger()
    start_it_S1.connect("start_it_S1.Tag", "start_it_S1.Tag", system_it)
    start_it_S1.end_signals["start_it_S1.Tag"] = ds_status.G
    start_it_S1.reset_signals["it.S1.Relay"] = ds_status.G
    start_it_S1.clear_signals["start_it_S1.Tag"] = ds_status.H

    relay_it_S1.connect("it.S1.Relay", "it", system_it)
    relay_it_S1.connect("it.S1.Relay", "it.S1.Relay", imp_relay_it_S1)
    relay_it_S1.start_signals["start_it_S1.Tag"] = ds_status.F
    relay_it_S1.end_signals["it.S1.Relay"] = ds_status.G
    relay_it_S1.end_signals["it.S1.Tag"] = ds_status.F
    relay_it_S1.reset_signals["it.S2.Relay"] = ds_status.G
    relay_it_S1.clear_signals["it.S1.Tag"] = ds_status.R
    tag_it_S1.connect("it.S1.Tag", "it.S1.Relay", imp_relay_it_S1)
    tag_it_S1.connect("it.S1.Tag", "it.S1.Tag", imp_tag_it_S1)
    tag_it_S1.start_signals["it.S1.Relay"] = ds_status.G
    tag_it_S1.reset_signals["it.S1.Relay"] = ds_status.H
    tag_it_S1.end_signals["it.S1.Tag"] = ds_status.G
    tag_it_S1.end_signals["it.S1"] = ds_status.F
    tag_it_S1.clear_signals["it.S1"] = ds_status.R
    it_S1.connect("it.S1", "it.S1.Tag", imp_tag_it_S1)
    it_S1.start_signals["it.S1.Tag"] = ds_status.G
    it_S1.reset_signals["it.S1.Tag"] = ds_status.H
    it_S1.end_signals["it.S1"] = ds_status.G
    it_S1.clear_signals["it.S1"] = ds_status.H
    
    relay_it_S2 = ds_signal_exchanger()
    imp_relay_it_S2 = imparter()
    tag_it_S2 = ds_signal_exchanger()
    imp_tag_it_S2 = imparter()
    it_S2 = ds_signal_exchanger()
    
    start_it_S2 = ds_signal_exchanger()
    start_it_S2.connect("start_it_S2.Tag", "start_it_S2.Tag", system_it)
    start_it_S2.end_signals["start_it_S2.Tag"] = ds_status.G
    start_it_S2.reset_signals["it.S2.Relay"] = ds_status.G
    start_it_S2.clear_signals["start_it_S2.Tag"] = ds_status.H
    
    relay_it_S2.connect("it.S2.Relay", "it", system_it)
    relay_it_S2.connect("it.S2.Relay", "it.S2.Relay", imp_relay_it_S2)
    relay_it_S2.start_signals["start_it_S2.Tag"] = ds_status.F
    relay_it_S2.end_signals["it.S2.Relay"] = ds_status.G
    relay_it_S2.end_signals["it.S2.Tag"] = ds_status.F
    relay_it_S2.reset_signals["it.S1.Relay"] = ds_status.G
    relay_it_S2.clear_signals["it.S2.Tag"] = ds_status.R
    tag_it_S2.connect("it.S2.Tag", "it.S2.Relay", imp_relay_it_S2)
    tag_it_S2.connect("it.S2.Tag", "it.S2.Tag", imp_tag_it_S2)
    tag_it_S2.start_signals["it.S2.Relay"] = ds_status.G
    tag_it_S2.reset_signals["it.S2.Relay"] = ds_status.H
    tag_it_S2.end_signals["it.S2.Tag"] = ds_status.G
    tag_it_S2.end_signals["it.S2"] = ds_status.F
    tag_it_S2.clear_signals["it.S2"] = ds_status.R
    it_S2.connect("it.S2", "it.S2.Tag", imp_tag_it_S2)
    it_S2.start_signals["it.S2.Tag"] = ds_status.G
    it_S2.reset_signals["it.S2.Tag"] = ds_status.H
    it_S2.end_signals["it.S2"] = ds_status.G
    it_S2.clear_signals["it.S2"] = ds_status.H

    exchanger_dict = {}
    exchanger_dict["start_it_S1.Tag"] = start_it_S1
    exchanger_dict["it.S1.Relay"] = relay_it_S1
    exchanger_dict["it.S1.Tag"] = tag_it_S1
    exchanger_dict["it.S1"] = it_S1
    exchanger_dict["start_it_S2.Tag"] = start_it_S2
    exchanger_dict["it.S2.Relay"] = relay_it_S2
    exchanger_dict["it.S2.Tag"] = tag_it_S2
    exchanger_dict["it.S2"] = it_S2
    outer = ["start_it_S1.Tag", "start_it_S2.Tag"]

    thread = {
        obj[0] : thread_with_exception([obj[1]])
        for obj in exchanger_dict.items()
    }

    for th in thread.values():
        th.start()
    
    for th in thread.values():
        th.client.system_flags.always_on = True
        th.client.consumer_group = 1
        th.client.setting_up()
        th.client.event.set()
    
    time.sleep(3.0)
    print("initialize finished")

    consum = consumer_set('localhost:9092', 1)
    consum.get_data("segments", thread, outer)
    
# test case-6 part of cylinder