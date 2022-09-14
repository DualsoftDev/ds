using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChartAndGraph;
using System.Linq;

public class PieManager : MonoBehaviour
{


    public PieChart pie;

    /*

        // Update is called once per frame
        void Update()
        {
            if (DSData.mode != DSData.init) { return; }
            if (pie.DataSource.GetCategoryIndex() == 0 && DSData.initPie)  //조건부 개선 필요?
            {
                List<string> callList = new List<string>(DSData.callDic.Keys);
                for (int i = 0; i < DSData.callDic.Count; i++)
                {
                    Call call = DSData.callDic[callList[i]];
                    pie.DataSource.AddCategory(call.name, call.material);
                    pie.DataSource.SetValue(call.name, 1f);
                    pie.DataSource.RestoreRadiusScale(call.name, DSData.realDic[call.parent].targets[call.name]);  //정답값
                }
                DSData.initPie = false;
            }   
        }
    }
    //foreach(var target in DSdata.CallDic["targets"])

    //////////////////
       void Update()
        {
            if (DSData.mode != DSData.init) { return; }
            if (pie.DataSource.GetCategoryIndex() == 0)  //조건부 개선 필요?
            {
                for(int i = 0 ; i < DSData.pinMapList.Count; i++)
                {
                    pie.DataSource.AddCategory(DSData.callList[i].name, DSData.callList[i].material);
                    pie.DataSource.SetValue(DSData.callList[i].name, 1f);
                    pie.DataSource.RestoreRadiusScale(DSData.callList[i].name, DSData.callList[i].value);  //정답값
                }
            }   
        }


     */
}