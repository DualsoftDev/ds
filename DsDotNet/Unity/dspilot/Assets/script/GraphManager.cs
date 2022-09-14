using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChartAndGraph;
using TMPro;

public class GraphManager : MonoBehaviour
{
    public Material lineMaterial;
    public Material pointMaterial;
    public Material pieMaterial;
    public GameObject NoticeText;
    public GameObject pin;
    public GraphChart Graph;
    public PieChart pie;

    private float blinkTime = 0.5f;
    private bool isOn = true;

    Dictionary<string, GameObject> pinDic = new Dictionary<string, GameObject>();
    Dictionary<string, Material> pieMaterialDic = new Dictionary<string, Material>();



    Color[] colors = new Color[] { Color.blue, Color.green, Color.red, Color.white, Color.magenta, Color.yellow };
    int i;
    Color getColor(int index)
    {
        return index < colors.Length - 1 ? colors[index] : Random.ColorHSV();
    }
    // Update is called once per frame

    private void OnEnable()
    {
        if (!DSData.needInitialize && DSData.percent == 100) { DSData.needInitialize = true; }
        NoticeText.transform.SetAsLastSibling();
    }

    private void Start()
    {
        StartCoroutine(SetPieAlpha());
        StartCoroutine(SetPieBlink());
    }

    private void FixedUpdate()
    {
        InitializePilot();

        StreamPilot();

        NoticeText.GetComponent<TextMeshProUGUI>().text = DSData.percent == 100 ? "" : $"Loading..({DSData.percent}%)";
    }


    void InitializePilot()
    {
        if (!DSData.needInitialize) { return; }
        //realGraph
        i = 0;
        foreach (string name in DSData.realDic.Keys)
        {
            Real real = DSData.realDic[name];
            Color realColor = getColor(i++);
            //realColor.a = 0.5f; // all alpha
            real.color = realColor;
            Material newLineMaterial = new Material(lineMaterial);
            newLineMaterial.SetColor("_Color", real.color);
            Graph.DataSource.ClearCategory(real.name);
            Graph.DataSource.AddCategory(real.name, newLineMaterial, 7.0, new MaterialTiling(), null, false, null, 1.0, false);


        }
        Graph.DataSource.EndBatch(); // finally we call EndBatch , this will cause the GraphChart to redraw itself
        DSData.percent = 50;

        //pin and pie


        foreach (string name in DSData.realDic.Keys) // callDic -> realDic >call
        {
            Real real = DSData.realDic[name];
            foreach (string callName in real.children.Keys)
            {
                Call call = real.children[callName];
                Debug.Log($"{call.x} : {call.y} || {call.width} : {call.health}");
                pinDic.Add(callName, (GameObject)Instantiate(pin, new Vector2(1920 * call.x, 1080 - 1080 * call.y), Quaternion.identity, GameObject.Find("Canvas").transform));   //Screen..Height - call.y
                var pinMark = pinDic[callName].GetComponent<PinMark>();
                pinMark.width = call.width;
                pinMark.height = call.height;
                pinMark.callName = call.name;
                pinMark.parent = call.parent;


                Material newPieMaterial = new Material(pieMaterial);
                if (!pieMaterialDic.ContainsKey(callName))
                    pieMaterialDic.Add(callName, newPieMaterial);
                Color color = DSData.realDic[name].color;
                color.a = 0.3f; // all alpha
                newPieMaterial.SetColor("_Color", color);

                pie.DataSource.AddCategory(call.name, newPieMaterial);
                pie.DataSource.SetValue(call.name, 1f);
                pie.DataSource.RestoreRadiusScale(call.name, call.finishValue);  //Á¤´ä°ª


            }

            //materialManager.callName = call.name;
        }
        DSData.percent = 100;
        Debug.Log("init realGraph Done!");
        DSData.needInitialize = false;
        DSData.mode = DSData.stream;
    }



    void StreamPilot()
    {
        //RealGraph 
        if (!(DSData.mode == DSData.stream)) { return; }
        foreach (var rdct in DSData.realDic)
        { 
            Real real = rdct.Value;
            Graph.DataSource.AddPointToCategoryRealtime(real.name, System.DateTime.Now, real.theta); // each time we call AddPointToCategory   Debug.Log("1");
        }
        Debug.Log("StreamPilot done");
        
    }



    private IEnumerator SetPieAlpha()
    {
        var wait = new WaitForEndOfFrame();
        while(true)
        {
            //if (!(DSData.mode == DSData.stream)) { continue; }
            if (pieMaterialDic.Count != 0) 
            {
                foreach (var pie in pieMaterialDic)
                {

                    string name = pie.Key;
                    Material mat = pie.Value;
                    Color color = mat.GetColor("_Color");
                    Call pieCall = DSData.callDic[name];


                    if (pieCall.status == DSData.finish) { color.a = 1.0f; }
                    else if (DSData.realDic[pieCall.parent].status == DSData.ready) { color.a = 0.3f; }
                    else if (pieCall.status == DSData.going) { color.a = isOn ? 0.8f : 0.4f; }
                    else { color.a = 0.3f; }
                    mat.SetColor("_Color", color);
                }
            }
            
            yield return wait;
        }
    }
    private IEnumerator SetPieBlink()
    {
        var blink = new WaitForSeconds(blinkTime);
        while (true)
        {
            yield return blink;
            isOn = !isOn;           
        }
    }
}
