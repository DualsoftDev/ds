using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorTest : MonoBehaviour
{
    DSData dsData;
    Image img;
    // Start is called before the first frame update
    void Start()
    {
        dsData = GameObject.Find("DSData").GetComponent<DSData>();
        img = gameObject.GetComponent<Image>();
        StartCoroutine(ChangeColor());
    }

    private IEnumerator ChangeColor()
    {
        var wait = new WaitForSeconds(1f);
        int i = 0;
        while(true)
        {
            Debug.Log("test = " + i);
            //img.color = dsData.GetRealColor(i);    //old test
            i++;
            yield return wait;
        }
    }
}
