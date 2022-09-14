using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Pin : MonoBehaviour
{    
    //public string hexColor = "#000000ff";
    //private Image img;
    // Start is called before the first frame update

/*
    void Start()
    {
       // img = gameObject.GetComponent<Image>();
        //SetTeamColor(hexColor);
    }

    // Update is called once per frame
    void Update()
    {
        SetTeamColor(hexColor);
    }


    public void SetTeamColor(string hexCol)
    {
       ColorUtility.TryParseHtmlString(hexCol, out img.color);
    }
*/


    public void SetColor(Color color)
    {
        Image img = gameObject.GetComponent<Image>();
        img.color = color;
    }
}

