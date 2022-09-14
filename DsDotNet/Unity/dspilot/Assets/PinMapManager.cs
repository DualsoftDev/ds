using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinMapManager : MonoBehaviour
{
    //DSData dsData;
    public GameObject pin;
    public List<GameObject> pins = new List<GameObject>();
    void Start()
    {
        //dsData = GameObject.Find("DSData").GetComponent<DSData>();
        Debug.Log("Init activate!");
    }

    // Update is called once per frame
    void Update()
    {
        if (DSData.mode != DSData.init) { return; }
        if (pins.Count == 0)  //조건부 개선 필요?
        {
            List<string> callList = new List<string>(DSData.callDic.Keys);
            for (int i = 0; i < DSData.callDic.Count; i++)
            {
                Call call = DSData.callDic[callList[i]];
                pins.Add((GameObject)Instantiate(pin, new Vector2(0 + call.x, 1080 - call.y), Quaternion.identity, GameObject.Find("Canvas").transform));   //Screen..Height - call.y
                var pinMark = pins[i].GetComponent<PinMark>();
                var materialManager = pins[i].GetComponent<PieMaterialManager>();
                pinMark.pieMaterial = call.material;
                pinMark.width = call.width;
                pinMark.height = call.height;
                pinMark.callName = call.name;
                pinMark.parent = call.parent;
                materialManager.callName = call.name;
            }
           // DSData.initPin = false;
        }
    }
}


/*
   if(pins.Count == 0  && DSData.pinMapList != null)   //조건부 개선 필요
        {
            for(int i = 0 ; i < DSData.pinMapList.Count; i++)
            {
                pins.Add((GameObject) Instantiate(pin, new Vector2(0 + DSData.pinMapList[i].x , Screen.height - DSData.pinMapList[i].y), Quaternion.identity, GameObject.Find("Canvas").transform));
                var pinMark = pins[i].GetComponent<PinMark>();
                pinMark.pieMaterial = DSData.callList[i].material;
                pinMark.width = DSData.pinMapList[i].width;
                pinMark.height = DSData.pinMapList[i].height;
                pinMark.index = i;
            }
        } 
 
 */
