using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using DSData;

public class MakeDSData : MonoBehaviour
{
    //DSData dsData;

    public Material material;

    public string mode;
    int stateNum;
    string[] names = new string[]{"Player 1","Player 2","Player 3","Player 4","Player 5","Player 6"};
    string[] c_names = new string[]{"1","2","3","4","5","6","7","8","9","10","11","12","13","14","15","16","17","18"};
    //Color[] r_colors = new Color[]{Color.blue,Color.yellow,Color.green,Color.magenta,Color.red,Color.white};
    //public Material callMaterial;



    // Start is called before the first frame update
    void Start()
    {
        // dsData = GameObject.Find("DSData").GetComponent<DSData>();
       // mode = DSData.mode;


        for(int i = 0; i < names.Length ; i++)
        {
            //DSData.realList.Add(dsData.InitReal(names[i],i));
            //DSData.realList[i].value = 0.0f;

            if(!DSData.realDic.ContainsKey(names[i]))
                DSData.realDic.Add(names[i],new Real(names[i],"parent"));

            for (int j = 0; j < 3; j++){
                ////Material material = new Material(callMaterial);
                ////material.SetColor("_Color", dsData.GetRealColor(i));

                //DSData.callList.Add(dsData.InitCall(c_names[3*i + j], i, Random.Range(0f,1f)));
                //DSData.pinMapList.Add(dsData.InitPinMap(Random.Range(100f,1500),Random.Range(100f,1000f),Random.Range(100f,300f),Random.Range(100f,300f)));
                if(!DSData.callDic.ContainsKey(c_names[3 * i + j]))
                    DSData.callDic.Add(c_names[3 * i + j], new Call(c_names[3 * i + j], names[i], Random.Range(100f, 1500), Random.Range(100f, 1000f), Random.Range(100f, 300f), Random.Range(100f, 300f)));
                Material pieMaterial = new Material(material);
                pieMaterial.SetColor("_Color", DSData.realDic[names[i]].color);
                DSData.callDic[c_names[3 * i + j]].material = pieMaterial;

                if (!DSData.realDic[names[i]].targets.ContainsKey(c_names[3 * i + j]))
                    DSData.realDic[names[i]].targets.Add(c_names[3 * i + j],Random.Range(0f,1f));
            }
        }
       // DSData.initPie = true; DSData.initPin = true;
        StartCoroutine(ChangeCallState());
        StartCoroutine(MakeValue());
    }



    private IEnumerator ChangeCallState()
    {
        var wait = new WaitForSeconds(5.0f);
        while(true)
        {
            List<string> callList = new List<string>(DSData.callDic.Keys);
            for (int i=0; i < DSData.callDic.Count; i++)
            {
                int stateNum = Random.Range(1, 5);
                switch(stateNum)
                {
                    case 1: DSData.callDic[callList[i]].status = DSData.ready; break;
                    case 2: DSData.callDic[callList[i]].status = DSData.going; break;
                    case 3: DSData.callDic[callList[i]].status = DSData.finish; break;
                    case 4: DSData.callDic[callList[i]].status = DSData.homing; break;
                    default: break;
                }
            }
            yield return wait;
        }
    }

    private IEnumerator MakeValue()
    {
        var wait = new WaitForSeconds(0.1f);
        while(true)
        {
            for(int i = 0; i < names.Length ; i++)
            {
                if(Random.value > 0.5f)
                {
                    //DSData.realList[i].value =  Random.value - 0.5f;
                    DSData.realDic[names[i]].value = Random.value - 0.5f;
                }
            }  
            yield return wait;
        }
    }

    private void Update()
    {
        DSData.mode = mode;
    }
}
