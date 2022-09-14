 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Area : MonoBehaviour
{
    public string status;
    public float width;
    public float height;


    private Color color;
    private RectTransform rectTransform;
    private byte areaAlpha = 55;

    // Start is called before the first frame update

    void Start()
    {
        //img = gameObject.GetComponent<Image>();
        //rectTransform = gameObject.GetComponent<RectTransform>();
        //SetAreaSize(width, height);

       StartCoroutine(Areastatus());
    }
    // Update is called once per frame
    /*
    void Update()
    {   
        SetAreaSize(width, height);
        tmpFunc();
    }
    */
    public void SetAreaSize(float width, float height)
    {
       gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
    }

    public void setstatus(string _status){
        //color = gameObject.GetComponent<Image>().color;   //image로 다시 만들기
        if(_status == DSData.ready){gameObject.GetComponent<Image>().color = new Color32(0,255,0,areaAlpha);}
        if(_status == DSData.going){gameObject.GetComponent<Image>().color = new Color32(255,255,0,areaAlpha);}
        if(_status == DSData.finish){gameObject.GetComponent<Image>().color = new Color32(0,0,225,areaAlpha);}
        if(_status == DSData.homing){gameObject.GetComponent<Image>().color = new Color32(80,80,80,areaAlpha);}
    }


    private IEnumerator Areastatus()
    {
        while(true)
        {
            setstatus(status);
            yield return new WaitForSeconds(0.01f);
        }

    }
}
