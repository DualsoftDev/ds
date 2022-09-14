using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChartAndGraph;

public class StreamData : MonoBehaviour
{
    public Material lineMaterial;
    public Material pointMaterial;
    

    public GraphChart Graph;

    //PICHART
    public float time = 0.1f;
    private float Timer = 0.5f;
    //private float x = 4f;   

   // DSData dsData;
    float[] preValues;

    public GameObject loadingText;
    List<string> realList;
    Real real;

    void Start()
    {
        // dsData = GameObject.Find("DSData").GetComponent<DSData>();
        realList = new List<string>(DSData.realDic.Keys);
    }

    // Update is called once per frame
    void Update()
    {
       
        if(Graph.DataSource.GetCategoryIndex() == 0)  //조건부 개선 필요?
        {
            Graph.DataSource.StartBatch();
            preValues = new float[DSData.realDic.Count];
            for(int i = 0 ; i < preValues.Length; i++)
            {
                real = DSData.realDic[realList[i]];
                //public void AddCategory(string category, Material lineMaterial, double lineThickness, MaterialTiling lineTiling, Material innerFill, bool strechFill, Material pointMaterial, double pointSize,bool maskPoints = false)
                Material newLineMaterial = new Material(lineMaterial);
                newLineMaterial.SetColor("_Color", real.color);
                Graph.DataSource.ClearCategory(real.name);
                Graph.DataSource.AddCategory(real.name, newLineMaterial, 7.0, new MaterialTiling(), null, false, pointMaterial, 1.0, false);
                preValues[i] = real.value;
                
                loadingText.SetActive(false);
            }
            Graph.DataSource.EndBatch(); // finally we call EndBatch , this will cause the GraphChart to redraw itself
        }   




        Timer -= Time.deltaTime;
        if(Timer <= 0f){
            Timer = time;
                for(int i = 0; i < preValues.Length; i++)
                {
                    real = DSData.realDic[realList[i]];

                    if(preValues[i] != real.value)
                    {
                        Graph.DataSource.AddPointToCategoryRealtime(real.name, System.DateTime.Now, real.value); // each time we call AddPointToCategory   
                        preValues[i] = real.value;
                    } 
                }          
            }

/*
            for(int i = 0; i < c_names.Length; i++)    //파이차트 투명화
                    {
                        if(Random.Range(0, 10) < 7){
                            Color tmpColor = pieMaterials[i].GetColor("_Color");
                            tmpColor.a = Random.Range(0.2f,1f);
                            pieMaterials[i].SetColor("_Color", tmpColor);
                        }
                    }
*/
    }











    /*
        private IEnumerator goingPieBlink()
        {
            int thisState = 1;
            while(true)
            {
                 for(int i = 0; i < c_names.Length; i++)
                {
                    thisState = states[i];

                    if(real_states[i] == false){pieMaterials[i].SetColor("_Color", new Color(c_colors[i].r,c_colors[i].g,c_colors[i].b, 0.3f));} // real_state로 바꿔야함
                    else if(thisState == 2)
                    {
                        pieMaterials[i].SetColor("_Color", new Color(c_colors[i].r,c_colors[i].g,c_colors[i].b, blinkTick? 0.7f : 0f ));
                        blinkTick = !blinkTick;
                    }
                    else if(thisState[i] == 3){pieMaterials[i].SetColor("_Color", new Color(c_colors[i].r,c_colors[i].g,c_colors[i].b, 1f ));}


                }
                yield return new WaitForSeconds(0.5f);       
            }
        }


        private IEnumerator TempGenState(){
            while(true)
            {
                for(int i = 0; i < c_names.Length; i++)
                {
                    states[i] = Random.Range(1,5);
                }

                for(int i = 0; i < names.Length; i++)
                {
                    real_states[i] = Random.value > 0.5f;   
                }

                yield return new WaitForSeconds(5f);
            }


        }
    */

}



/*
 

 if (DSData.mode != DSData.stream) { return; }
        if(Graph.DataSource.GetCategoryIndex() == 0)  //조건부 개선 필요?
        {
            Graph.DataSource.StartBatch();
            preValues = new float[DSData.realList.Count];
            for(int i = 0 ; i < DSData.realList.Count; i++)
            {
                //public void AddCategory(string category, Material lineMaterial, double lineThickness, MaterialTiling lineTiling, Material innerFill, bool strechFill, Material pointMaterial, double pointSize,bool maskPoints = false)
                Material newLineMaterial = new Material(lineMaterial);
                newLineMaterial.SetColor("_Color", DSData.realList[i].color);
                Graph.DataSource.ClearCategory(DSData.realList[i].name);
                Graph.DataSource.AddCategory(DSData.realList[i].name, newLineMaterial, 7.0, new MaterialTiling(), null, false, pointMaterial, 1.0, false);
                preValues[i] = DSData.realList[i].value;
                
                loadingText.SetActive(false);
            }
            Graph.DataSource.EndBatch(); // finally we call EndBatch , this will cause the GraphChart to redraw itself
        }   




        Timer -= Time.deltaTime;
        if(Timer <= 0f){
            Timer = time;
                for(int i = 0; i < DSData.realList.Count; i++)
                {
                    if(preValues[i] != DSData.realList[i].value)
                    {
                        Graph.DataSource.AddPointToCategoryRealtime(DSData.realList[i].name, System.DateTime.Now, DSData.realList[i].value); // each time we call AddPointToCategory   
                        preValues[i] = DSData.realList[i].value;
                    } 
                }          
            }

    }
 */