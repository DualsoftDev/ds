using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieMaterialManager : MonoBehaviour
{
    public string status;
    public string callName;
    public Material pieMaterial;

    private float blinkTime = 0.5f;
    private bool isOn = true;
    Color color;



    //DSData dsData;
    // Start is called before the first frame update
    void Start()
    {
       // dsData = GameObject.Find("DSData").GetComponent<DSData>();

       // int index = transform.GetComponent<PinMark>().index;


        pieMaterial = DSData.callDic[callName].material;

        color = pieMaterial.GetColor("_Color");
        //color = DSData.realDic[DSData.callDic[callName].parent].color;



        StartCoroutine(SetPieAlpha());
        StartCoroutine(SetPieBlink());
    }


    private IEnumerator SetPieAlpha()
    {
        var wait = new WaitForEndOfFrame();

        while(true)
        {
            status = transform.GetComponent<PinMark>().status;
            if(status== DSData.ready)
            {
                color.a = 0.5f;
            }

            if(status== DSData.finish || status== DSData.homing)
            {
                color.a = 1.0f;
            }
            pieMaterial.SetColor("_Color",color);
            yield return wait;
        }
    }
    private IEnumerator SetPieBlink()
    {
        var blink = new WaitForSeconds(blinkTime);
        while(true)
        {
            if(status== DSData.going)
            {
                color.a = isOn ? 0.7f : 0.0f;
            }
            pieMaterial.SetColor("_Color",color);
            yield return blink;
            isOn = !isOn;
        }
    }
}
