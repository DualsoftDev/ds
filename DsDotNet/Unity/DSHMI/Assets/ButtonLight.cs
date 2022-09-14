using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonLight : MonoBehaviour
{
    public bool isOn = false;
    public Color offColor;
    public Color onColor;
    private Image img;
    private bool trigger = true;
    private void Start()
    {
        img = transform.GetComponent<Image>();
    }

    private void FixedUpdate()    //È®ÀÎ¿ë
    {
        if (isOn == trigger) { return; }
        if(isOn && img.color !=onColor)
        {
            img.DOColor(onColor, 0.5f);
            //img.color = onColor;
        }
        else if(img.color != offColor)
        {
            //img.color = offColor;
            img.DOColor(offColor, 0.5f);
        }
        trigger = isOn;
    }







    public void LightOn()
    {
        img.DOColor(onColor, 0.5f);
    }

    public void LightOff()
    {
        img.DOColor(offColor, 0.5f);
    }
}
