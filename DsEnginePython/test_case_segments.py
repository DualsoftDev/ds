import os, time
from ds_data_handler import imparter
from ds_data_handler import ds_signal_exchanger
from ds_data_handler import thread_with_exception
from ds_signal_handler import ds_causal, signal_set
from ds_engine_consumer import get_data

if __name__ == "__main__":
    system_me = imparter()

    # call
    tag_call_it_S1 = ds_signal_exchanger()
    imp_tag_call_it_S1 = imparter()
    tag_call_it_S1.connect("me.call_it_S1.Tag", "me.call_it_S1.Tag", imp_tag_call_it_S1)
    tag_call_it_S1.end_signals.append("it.S1.Tag")
    tag_call_it_S1.clear_signals.append("it.S1.Tag")

    tag_call_it_S2 = ds_signal_exchanger()
    imp_tag_call_it_S2 = imparter()
    tag_call_it_S2.connect("me.call_it_S2.Tag", "me.call_it_S2.Tag", imp_tag_call_it_S2)
    tag_call_it_S2.end_signals.append("it.S2.Tag")
    tag_call_it_S2.clear_signals.append("it.S2.Tag")

    relay0 = ds_signal_exchanger()
    impRelay0 = imparter()
    tag0 = ds_signal_exchanger()
    impTag0 = imparter()
    seg0 = ds_signal_exchanger()
    relay0.connect("me.S1.Remote", "me", system_me)
    relay0.connect("me.S1.Remote", "me.S1.Remote", impRelay0)
    relay0.end_signals.append("me.S1.Tag")
    relay0.reset_signals.append("me.S2.Relay")
    relay0.clear_signals.append("me.S1.Tag")
    tag0.connect("me.S1.Tag", "me.S1.Remote", impRelay0)
    tag0.connect("me.S1.Tag", "me.S1.Tag", impTag0)
    tag0.reset_signals.append("me.S1.Remote")
    tag0.end_signals.append("me.S1")
    tag0.clear_signals.append("me.S1")
    seg0.connect("me.S1", "me.S1.Tag", impTag0)
    seg0.start_signals.append("me.S1.Tag")
    seg0.reset_signals.append("me.S1.Tag")
    
    relay1 = ds_signal_exchanger()
    impRelay1 = imparter()
    tag1 = ds_signal_exchanger()
    impTag1 = imparter()
    seg1 = ds_signal_exchanger()
    relay1.connect("me.S2.Relay", "me", system_me)
    relay1.connect("me.S2.Relay", "me.S2.Relay", impRelay1)
    relay1.start_signals.append("me.S1.Remote")
    relay1.reset_signals.append("me.S3.Relay")
    relay1.end_signals.append("me.S2.Tag")
    relay1.clear_signals.append("me.S2.Tag")
    tag1.connect("me.S2.Tag", "me.S2.Relay", impRelay1)
    tag1.connect("me.S2.Tag", "me.S2.Tag", impTag1)
    tag1.start_signals.append("me.S2.Relay")
    tag1.reset_signals.append("me.S2.Relay")
    tag1.end_signals.append("me.S2")
    tag1.clear_signals.append("me.S2")
    seg1.connect("me.S2", "me.S2.Tag", impTag1)
    seg1.start_signals.append("me.S2.Tag")
    seg1.reset_signals.append("me.S2.Tag")

    relay2 = ds_signal_exchanger()
    impRelay2 = imparter()
    tag2 = ds_signal_exchanger()
    impTag2 = imparter()
    seg2 = ds_signal_exchanger()
    relay2.connect("me.S3.Relay", "me", system_me)
    relay2.connect("me.S3.Relay", "me.S3.Relay", impRelay2)
    relay2.start_signals.append("me.S2.Relay")
    relay2.reset_signals.append("me.S3.Relay")
    relay2.end_signals.append("me.S3.Tag")
    relay2.clear_signals.append("me.S3.Tag")
    tag2.connect("me.S3.Tag", "me.S3.Relay", impRelay2)
    tag2.connect("me.S3.Tag", "me.S3.Tag", impTag2)
    tag2.start_signals.append("me.S3.Relay")
    tag2.reset_signals.append("me.S3.Relay")
    tag2.end_signals.append("me.S3")
    tag2.clear_signals.append("me.S3")
    seg2.connect("me.S3", "me.S3.Tag", impTag2)
    seg2.start_signals.append("me.S3.Tag")
    seg2.reset_signals.append("me.S3.Tag")

    impSeg1 = imparter()
    seg1.connect("me.S2", "me.S2", impSeg1)
    seg1.origin_signals.append("me.call_it_S1.Tag")
    seg1.origin_signals.append("me.call_it_S2.Tag")
    seg1.start_points = [0, 1]

    child_relay0 = ds_signal_exchanger()
    impChildRelay0 = imparter()
    child_relay0.connect("me.S2.child0.Relay", "me.S2", impSeg1)
    child_relay0.connect("me.S2.child0.Relay", "me.S2.child0.Relay", impChildRelay0)
    child_relay0.start_signals.append("me.S2")
    child_relay0.reset_signals.append("me.S2")
    child_relay0.end_signals.append("me.call_it_S1.Tag")
    # child_relay0.clear_signals.append("me.call_it_S1.Tag")
    tag_call_it_S1.connect("me.call_it_S1.Tag", "me.S2.child0.Relay", impChildRelay0)
    tag_call_it_S1.connect("me.call_it_S1.Tag", "me.S2", impSeg1)
    tag_call_it_S1.start_signals.append("me.S2.child0.Relay")

    seg1.end_signals.append("me.S2.child0.Relay")
    seg1.clear_signals.append("me.S2.child0.Relay")

    child_relay1 = ds_signal_exchanger()
    impChildRelay1 = imparter()
    child_relay1.connect("me.S2.child1.Relay", "me.S2", impSeg1)
    child_relay1.connect("me.S2.child1.Relay", "me.S2.child1.Relay", impChildRelay1)
    child_relay1.start_signals.append("me.S2")
    child_relay1.start_signals.append("me.S2.child0.Relay")
    child_relay1.reset_signals.append("me.S2")
    child_relay1.end_signals.append("me.call_it_S2.Tag")
    # child_relay1.clear_signals.append("me.call_it_S2.Tag")
    
    child_relay1_h = ds_signal_exchanger()
    impChildRelay1_h = imparter()
    child_relay1_h.causal_type = ds_causal.H
    child_relay1_h.connect("me.S2.child1_h.Relay", "me.S2", impSeg1)
    child_relay1_h.connect("me.S2.child1_h.Relay", "me.S2.child1_h.Relay", impChildRelay1_h)
    child_relay1_h.reset_signals.append("me.S2")
    child_relay1_h.clear_signals.append("me.S2")
    child_relay1_h.end_signals.append("me.call_it_S2.Tag")

    tag_call_it_S2.connect("me.call_it_S2.Tag", "me.S2.child1.Relay", impChildRelay1)
    tag_call_it_S2.connect("me.call_it_S2.Tag", "me.S2.child1_h.Relay", impChildRelay1_h)
    tag_call_it_S2.connect("me.call_it_S2.Tag", "me.S2", impSeg1)
    tag_call_it_S2.start_signals.append("me.S2.child1.Relay")
    tag_call_it_S2.start_signals.append("me.S2.child1_h.Relay")

    seg1.end_signals.append("me.S2.child1.Relay")
    seg1.clear_signals.append("me.S2.child1.Relay")
    seg1.homing_signals.append("me.S2.child1_h.Relay")

    
    tag_it_S1 = ds_signal_exchanger()
    tag_it_S1.connect("it.S1.Tag", "me.call_it_S1.Tag", imp_tag_call_it_S1)

    tag_it_S2 = ds_signal_exchanger()
    tag_it_S2.connect("it.S2.Tag", "me.call_it_S2.Tag", imp_tag_call_it_S2)

    exchanger_dict = {}
    exchanger_dict["me.call_it_S1.Tag"] = tag_call_it_S1
    exchanger_dict["me.call_it_S2.Tag"] = tag_call_it_S2

    exchanger_dict["me.S1.Remote"] = relay0
    exchanger_dict["me.S1.tag"] = tag0
    exchanger_dict["me.S1"] = seg0
    exchanger_dict["me.S2.Relay"] = relay1
    exchanger_dict["me.S2.tag"] = tag1
    exchanger_dict["me.S2"] = seg1
    exchanger_dict["me.S3.Relay"] = relay2
    exchanger_dict["me.S3.tag"] = tag2
    exchanger_dict["me.S3"] = seg2
    
    exchanger_dict["me.S2.child0.Relay"] = child_relay0
    exchanger_dict["me.S2.child1.Relay"] = child_relay1
    exchanger_dict["me.S2.child1_h.Relay"] = child_relay1_h
    
    exchanger_dict["it.S1.Tag"] = tag_it_S1
    exchanger_dict["it.S2.Tag"] = tag_it_S2
    
    outer = ["it.S1.Tag", "it.S2.Tag"]

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

    thread["me.S1.tag"].client.signal_onoff = signal_set(True, False, False)
    thread["me.S1.tag"].client.event.set()
    get_data("segments",thread, outer)

# test case-6 part of segments