import os, time
from ds_data_handler import signal_set, imparter
from ds_data_handler import ds_signal_exchanger
from ds_data_handler import thread_with_exception

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


    system_me = imparter()

    # call
    tag_call_it_S1 = ds_signal_exchanger()
    imp_tag_call_it_S1 = imparter()
    tag_call_it_S1.connect("me.call_it_S1.Tag", "me.call_it_S1.Tag", imp_tag_call_it_S1)
    # tag_call_it_S1.reset_signals.append("me.S2.child0.Relay")
    tag_call_it_S1.end_signals.append("it.S1.Tag")
    tag_it_S1.connect("it.S1.Tag", "me.call_it_S1.Tag", imp_tag_call_it_S1)
    tag_it_S1.start_signals.append("me.call_it_S1.Tag")
    tag_call_it_S1.clear_signals.append("it.S1.Tag")

    tag_call_it_S2 = ds_signal_exchanger()
    imp_tag_call_it_S2 = imparter()
    tag_call_it_S2.connect("me.call_it_S2.Tag", "me.call_it_S2.Tag", imp_tag_call_it_S2)
    # tag_call_it_S2.reset_signals.append("me.S2.child1.Relay")
    tag_call_it_S2.end_signals.append("it.S2.Tag")
    tag_it_S2.connect("it.S2.Tag", "me.call_it_S2.Tag", imp_tag_call_it_S2)
    tag_it_S2.start_signals.append("me.call_it_S2.Tag")
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

    child_relay0 = ds_signal_exchanger()
    impChildRelay0 = imparter()
    child_relay0.connect("me.S2.child0.Relay", "me.S2", impSeg1)
    child_relay0.connect("me.S2.child0.Relay", "me.S2.child0.Relay", impChildRelay0)
    child_relay0.start_signals.append("me.S2")
    child_relay0.reset_signals.append("me.S2")
    child_relay0.end_signals.append("me.call_it_S1.Tag")
    # child_relay0.clear_signals.append("me.call_it_S1.Tag")
    tag_call_it_S1.connect("me.call_it_S1.Tag", "me.S2.child0.Relay", impChildRelay0)
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
    tag_call_it_S2.connect("me.call_it_S2.Tag", "me.S2.child1.Relay", impChildRelay1)
    tag_call_it_S2.start_signals.append("me.S2.child1.Relay")

    seg1.end_signals.append("me.S2.child1.Relay")
    seg1.clear_signals.append("me.S2.child1.Relay")


    objs = [
        relay_it_S1, tag_it_S1, it_S1,
        relay_it_S2, tag_it_S2, it_S2,
        tag_call_it_S1, tag_call_it_S2,
        relay0, tag0, seg0,
        relay1, tag1, seg1,
        relay2, tag2, seg2,
        child_relay0, child_relay1
    ]

    thread = [
        thread_with_exception([obj])
        for obj in objs
    ]
    for th in thread:
        th.start()
    
    for thr in thread:
        thr.client.system_flags.always_on = True
        thr.client.event.set()
    
    time.sleep(3.0)
    print("initialize finished")

    while True:
        if thread[9].client.signal_onoff == signal_set(False, False, False) and\
            not thread[11].client.signal_onoff == signal_set(True, False, False):
            print("=====================================")
            time.sleep(0.5)
            thread[9].client.signal_onoff = signal_set(True, False, False)
            thread[9].client.event.set()

    # thread[9].client.signal_onoff = signal_set(True, False, False)
    # thread[9].client.event.set()

# test case-6