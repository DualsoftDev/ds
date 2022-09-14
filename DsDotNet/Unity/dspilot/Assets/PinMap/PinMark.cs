using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PinMark : MonoBehaviour
{   
    //public Call call;

    [SerializeField]  
    private GameObject area;    //AreaSize, AreaStat
    [SerializeField] 
    private GameObject pin;  //TeamColor
    [SerializeField] 
    private GameObject circle;  //Health

    //[SerializeField] 
    //private byte areaAlpha = 55;

    public Material pieMaterial;
    public bool isRealOn = false;
    public string status;
    public float width = 100;
    public float height = 100;
    public float health = 100;
    //public int index = 1;

    public string callName;    //call
    public string parent;    //real


    //private bool blinkTick = false; 
    //private Color pieColor;

    //DSData dsData;



    private void Start()
    {
        //dsData = GameObject.Find("DSData").GetComponent<DSData>();



        //pin.GetComponent<Pin>().SetColor(DSData.realList[DSData.callList[index].realIndex].color);
        //area.GetComponent<Area>().SetAreaSize(width,height);


        pin.GetComponent<Pin>().SetColor(DSData.realDic[parent].color);
        area.GetComponent<Area>().SetAreaSize(DSData.realDic[parent].children[callName].width, DSData.realDic[parent].children[callName].height);

    }



    private void Update() {

        status = DSData.realDic[parent].children[callName].status;
        health = DSData.realDic[parent].children[callName].health;

        area.GetComponent<Area>().status = status;
        circle.GetComponent<Health>().health = health;
    }    

}

