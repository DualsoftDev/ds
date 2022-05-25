import time
from ds_data_handler import imparter
from ds_data_handler import ds_signal_exchanger
from ds_data_handler import thread_with_exception
from ds_signal_handler import ds_status
from ds_engine_consumer import consumer_set

if __name__ == "__main__":
    system_me = imparter()

    # call
    tag_call_it_S1 = ds_signal_exchanger()
    imp_tag_call_it_S1 = imparter()
    tag_call_it_S1.connect("start_it_S1.Tag", "start_it_S1.Tag", imp_tag_call_it_S1)

    tag_call_it_S2 = ds_signal_exchanger()
    imp_tag_call_it_S2 = imparter()
    tag_call_it_S2.connect("start_it_S2.Tag", "start_it_S2.Tag", imp_tag_call_it_S2)

    relay0 = ds_signal_exchanger()
    impRelay0 = imparter()
    tag0 = ds_signal_exchanger()
    impTag0 = imparter()
    seg0 = ds_signal_exchanger()

    tag_start_S1 = ds_signal_exchanger()
    tag_start_S1.connect("me.start_S1.Tag", "me.start_S1.Tag", system_me)
    tag_start_S1.end_signals["me.start_S1.Tag"] = ds_status.G
    tag_start_S1.reset_signals["me.S1.Relay"] = ds_status.G
    tag_start_S1.clear_signals["me.start_S1.Tag"] = ds_status.H

    relay0.connect("me.S1.Relay", "me", system_me)
    relay0.connect("me.S1.Relay", "me.S1.Relay", impRelay0)
    relay0.start_signals["me.start_S1.Tag"] = ds_status.F
    relay0.reset_signals["me.S2.Relay"] = ds_status.G
    relay0.end_signals["me.S1.Relay"] = ds_status.G
    relay0.end_signals["me.S1.Tag"] = ds_status.F
    relay0.clear_signals["me.S1.Tag"] = ds_status.R
    tag0.connect("me.S1.Tag", "me.S1.Relay", impRelay0)
    tag0.connect("me.S1.Tag", "me.S1.Tag", impTag0)
    tag0.start_signals["me.S1.Relay"] = ds_status.G
    tag0.reset_signals["me.S1.Relay"] = ds_status.H
    tag0.end_signals["me.S1.Tag"] = ds_status.G
    tag0.end_signals["me.S1"] = ds_status.F
    tag0.clear_signals["me.S1"] = ds_status.R
    seg0.connect("me.S1", "me.S1.Tag", impTag0)
    seg0.start_signals["me.S1.Tag"] = ds_status.G
    seg0.reset_signals["me.S1.Tag"] = ds_status.H
    seg0.end_signals["me.S1"] = ds_status.G
    seg0.clear_signals["me.S1"] = ds_status.H
    
    relay1 = ds_signal_exchanger()
    impRelay1 = imparter()
    tag1 = ds_signal_exchanger()
    impTag1 = imparter()
    seg1 = ds_signal_exchanger()

    relay1.connect("me.S2.Relay", "me", system_me)
    relay1.connect("me.S2.Relay", "me.S2.Relay", impRelay1)
    relay1.start_signals["me.S1.Relay"] = ds_status.F
    relay1.reset_signals["me.S3.Relay"] = ds_status.G
    relay1.end_signals["me.S2.Relay"] = ds_status.G
    relay1.end_signals["me.S2.Tag"] = ds_status.F
    relay1.clear_signals["me.S2.Tag"] = ds_status.R
    tag1.connect("me.S2.Tag", "me.S2.Relay", impRelay1)
    tag1.connect("me.S2.Tag", "me.S2.Tag", impTag1)
    tag1.start_signals["me.S2.Relay"] = ds_status.G
    tag1.reset_signals["me.S2.Relay"] = ds_status.H
    tag1.end_signals["me.S2.Tag"] = ds_status.G
    tag1.end_signals["me.S2"] = ds_status.F
    tag1.clear_signals["me.S2"] = ds_status.R
    seg1.connect("me.S2", "me.S2.Tag", impTag1)
    seg1.start_signals["me.S2.Tag"] = ds_status.G
    seg1.reset_signals["me.S2.Tag"] = ds_status.H
    seg1.end_signals["me.S2"] = ds_status.G

    relay2 = ds_signal_exchanger()
    impRelay2 = imparter()
    tag2 = ds_signal_exchanger()
    impTag2 = imparter()
    seg2 = ds_signal_exchanger()
    
    relay2.connect("me.S3.Relay", "me", system_me)
    relay2.connect("me.S3.Relay", "me.S3.Relay", impRelay2)
    relay2.start_signals["me.S2.Relay"] = ds_status.F
    relay2.reset_signals["me.S3.Relay"] = ds_status.F
    relay2.end_signals["me.S3.Relay"] = ds_status.G
    relay2.end_signals["me.S3.Tag"] = ds_status.F
    relay2.clear_signals["me.S3.Tag"] = ds_status.R
    tag2.connect("me.S3.Tag", "me.S3.Relay", impRelay2)
    tag2.connect("me.S3.Tag", "me.S3.Tag", impTag2)
    tag2.start_signals["me.S3.Relay"] = ds_status.G
    tag2.reset_signals["me.S3.Relay"] = ds_status.H
    tag2.end_signals["me.S3.Tag"] = ds_status.G
    tag2.end_signals["me.S3"] = ds_status.F
    tag2.clear_signals["me.S3"] = ds_status.R
    seg2.connect("me.S3", "me.S3.Tag", impTag2)
    seg2.start_signals["me.S3.Tag"] = ds_status.G
    seg2.reset_signals["me.S3.Tag"] = ds_status.H
    seg2.end_signals["me.S3"] = ds_status.G
    seg2.clear_signals["me.S3"] = ds_status.H

    impSeg1 = imparter()
    seg1.connect("me.S2", "me.S2", impSeg1)
    seg1.origin_signals.append("it.S1.Tag")
    seg1.origin_signals.append("it.S2.Tag")
    seg1.start_points = [0, 1]

    child_relay0 = ds_signal_exchanger()
    impChildRelay0 = imparter()
    child_relay0.connect("me.S2.child0.Relay", "me.S2", impSeg1)
    child_relay0.connect("me.S2.child0.Relay", "me.S2.child0.Relay", impChildRelay0)
    child_relay0.start_signals["me.S2"] = ds_status.G
    child_relay0.reset_signals["me.S2"] = ds_status.H
    child_relay0.end_signals["me.S2.child0.Relay"] = ds_status.G
    child_relay0.clear_signals["me.S2.child0.Relay"] = ds_status.H
    tag_call_it_S1.connect("start_it_S1.Tag", "me.S2.child0.Relay", impChildRelay0)
    tag_call_it_S1.start_signals["me.S2.child0.Relay"] = ds_status.G

    seg1.end_signals["me.S2.child0.Relay"] = ds_status.F
    seg1.clear_signals["me.S2.child0.Relay"] = ds_status.R

    child_relay1 = ds_signal_exchanger()
    impChildRelay1 = imparter()
    child_relay1.connect("me.S2.child1.Relay", "me.S2", impSeg1)
    child_relay1.connect("me.S2.child1.Relay", "me.S2.child1.Relay", impChildRelay1)
    child_relay1.start_signals["me.S2"] = ds_status.G
    child_relay1.start_signals["me.S2.child0.Relay"] = ds_status.F
    child_relay1.reset_signals["me.S2"] = ds_status.H
    child_relay1.end_signals["me.S2.child1.Relay"] = ds_status.G
    child_relay1.clear_signals["me.S2.child1.Relay"] = ds_status.H
    
    child_relay1_h = ds_signal_exchanger()
    impChildRelay1_h = imparter()
    child_relay1_h.connect("me.S2.child1_h.Relay", "me.S2", impSeg1)
    child_relay1_h.connect("me.S2.child1_h.Relay", "me.S2.child1_h.Relay", impChildRelay1_h)
    child_relay1_h.start_signals["me.S2"] = ds_status.H
    child_relay1_h.end_signals["me.S2.child1_h.Relay"] = ds_status.G
    child_relay1_h.end_signals["it.S2.Tag"] = ds_status.F
    child_relay1_h.reset_signals["me.S2"] = ds_status.R
    child_relay1_h.clear_signals["me.S2.child1_h.Relay"] = ds_status.H
    
    tag_call_it_S2.connect("start_it_S2.Tag", "me.S2.child1_h.Relay", impChildRelay1_h)

    tag_call_it_S2.connect("start_it_S2.Tag", "me.S2.child1.Relay", impChildRelay1)
    tag_call_it_S2.start_signals["me.S2.child1.Relay"] = ds_status.G
    tag_call_it_S2.start_signals["me.S2.child1_h.Relay"] = ds_status.G

    seg1.end_signals["me.S2.child1.Relay"] = ds_status.F
    seg1.clear_signals["me.S2.child1.Relay"] = ds_status.R
    seg1.homing_signals["me.S2.child1_h.Relay"] = ds_status.F
    
    tag_it_S1 = ds_signal_exchanger()
    tag_it_S1.connect("it.S1.Tag", "me.S2.child0.Relay", impChildRelay0)
    tag_it_S1.connect("it.S1.Tag", "me.S2", impSeg1)
    child_relay0.end_signals["it.S1.Tag"] = ds_status.F

    tag_it_S2 = ds_signal_exchanger()
    tag_it_S2.connect("it.S2.Tag", "me.S2.child1.Relay", impChildRelay1)
    tag_it_S2.connect("it.S2.Tag", "me.S2.child1_h.Relay", impChildRelay1_h)
    tag_it_S2.connect("it.S2.Tag", "me.S2", impSeg1)
    child_relay1.end_signals["it.S2.Tag"] = ds_status.F

    exchanger_dict = {}
    exchanger_dict["start_it_S1.Tag"] = tag_call_it_S1
    exchanger_dict["start_it_S2.Tag"] = tag_call_it_S2

    exchanger_dict["me.S1.Relay"] = relay0
    exchanger_dict["me.S1.Tag"] = tag0
    exchanger_dict["me.S1"] = seg0
    exchanger_dict["me.S2.Relay"] = relay1
    exchanger_dict["me.S2.Tag"] = tag1
    exchanger_dict["me.S2"] = seg1
    exchanger_dict["me.S3.Relay"] = relay2
    exchanger_dict["me.S3.Tag"] = tag2
    exchanger_dict["me.S3"] = seg2
    
    exchanger_dict["me.S2.child0.Relay"] = child_relay0
    exchanger_dict["me.S2.child1.Relay"] = child_relay1
    exchanger_dict["me.S2.child1_h.Relay"] = child_relay1_h
    
    exchanger_dict["it.S1.Tag"] = tag_it_S1
    exchanger_dict["it.S2.Tag"] = tag_it_S2

    exchanger_dict["me.start_S1.Tag"] = tag_start_S1
    
    outer = ["me.start_S1.Tag", "start_it_S1.Tag", "start_it_S2.Tag", "it.S1.Tag", "it.S2.Tag"]

    thread = {
        obj[0] : thread_with_exception([obj[1]])
        for obj in exchanger_dict.items()
    }

    for th in thread.values():
        th.start()
    
    for th in thread.values():
        th.client.system_flags.always_on = True
        th.client.consumer_group = 0
        th.client.setting_up()
        th.client.event.set()
    
    time.sleep(3.0)
    print("initialize finished")

    consum = consumer_set('localhost:9092', 0)
    consum.get_data("segments", thread, outer)

# test case-6 part of segments