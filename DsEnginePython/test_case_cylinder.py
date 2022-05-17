import os, time
from ds_data_handler import imparter
from ds_data_handler import ds_signal_exchanger
from ds_data_handler import thread_with_exception
from ds_engine_consumer import get_data

if __name__ == "__main__":
    system_it = imparter()

    relay_it_S1 = ds_signal_exchanger()
    imp_relay_it_S1 = imparter()
    tag_it_S1 = ds_signal_exchanger()
    imp_tag_it_S1 = imparter()
    it_S1 = ds_signal_exchanger()
    
    relay_it_S1.connect("it.S1.Remote", "it", system_it)
    relay_it_S1.connect("it.S1.Remote", "it.S1.Remote", imp_relay_it_S1)
    relay_it_S1.end_signals.append("it.S1.Tag")
    relay_it_S1.reset_signals.append("it.S2.Remote")
    relay_it_S1.clear_signals.append("it.S1.Tag")
    tag_it_S1.connect("it.S1.Tag", "it.S1.Remote", imp_relay_it_S1)
    tag_it_S1.connect("it.S1.Tag", "it.S1.Tag", imp_tag_it_S1)
    tag_it_S1.reset_signals.append("it.S1.Remote")
    tag_it_S1.end_signals.append("it.S1")
    tag_it_S1.clear_signals.append("it.S1")
    it_S1.connect("it.S1", "it.S1.Tag", imp_tag_it_S1)
    it_S1.start_signals.append("it.S1.Tag")
    it_S1.reset_signals.append("it.S1.Tag")
    
    relay_it_S2 = ds_signal_exchanger()
    imp_relay_it_S2 = imparter()
    tag_it_S2 = ds_signal_exchanger()
    imp_tag_it_S2 = imparter()
    it_S2 = ds_signal_exchanger()
    
    relay_it_S2.connect("it.S2.Remote", "it", system_it)
    relay_it_S2.connect("it.S2.Remote", "it.S2.Remote", imp_relay_it_S2)
    relay_it_S2.end_signals.append("it.S2.Tag")
    relay_it_S2.reset_signals.append("it.S1.Remote")
    relay_it_S2.clear_signals.append("it.S2.Tag")
    tag_it_S2.connect("it.S2.Tag", "it.S2.Remote", imp_relay_it_S2)
    tag_it_S2.connect("it.S2.Tag", "it.S2.Tag", imp_tag_it_S2)
    tag_it_S2.reset_signals.append("it.S2.Remote")
    tag_it_S2.end_signals.append("it.S2")
    tag_it_S2.clear_signals.append("it.S2")
    it_S2.connect("it.S2", "it.S2.Tag", imp_tag_it_S2)
    it_S2.start_signals.append("it.S2.Tag")
    it_S2.reset_signals.append("it.S2.Tag")

    
    tag_call_it_S1 = ds_signal_exchanger()
    imp_tag_call_it_S1 = imparter()
    tag_call_it_S1.connect("me.call_it_S1.Tag", "me.call_it_S1.Tag", imp_tag_call_it_S1)
    tag_it_S1.connect("it.S1.Tag", "me.call_it_S1.Tag", imp_tag_call_it_S1)
    tag_it_S1.start_signals.append("me.call_it_S1.Tag")

    tag_call_it_S2 = ds_signal_exchanger()
    imp_tag_call_it_S2 = imparter()
    tag_call_it_S2.connect("me.call_it_S2.Tag", "me.call_it_S2.Tag", imp_tag_call_it_S2)
    tag_it_S2.connect("it.S2.Tag", "me.call_it_S2.Tag", imp_tag_call_it_S2)
    tag_it_S2.start_signals.append("me.call_it_S2.Tag")

    exchanger_dict = {}
    exchanger_dict["it.S1.Remote"] = relay_it_S1
    exchanger_dict["it.S1.tag"] = tag_it_S1
    exchanger_dict["it.S1"] = it_S1
    exchanger_dict["it.S2.Remote"] = relay_it_S2
    exchanger_dict["it.S2.tag"] = tag_it_S2
    exchanger_dict["it.S2"] = it_S2
    exchanger_dict["me.call_it_S1.Tag"] = tag_call_it_S1
    exchanger_dict["me.call_it_S2.Tag"] = tag_call_it_S2
    outer = ["me.call_it_S1.Tag", "me.call_it_S2.Tag"]

    thread = {
        obj[0] : thread_with_exception([obj[1]])
        for obj in exchanger_dict.items()
    }

    for th in thread.values():
        th.start()
    
    for th in thread.values():
        th.client.system_flags.always_on = True
        th.client.event.set()
    
    time.sleep(3.0)
    print("initialize finished")

    get_data("cylinder", thread, outer)
    
# test case-6 part of cylinder