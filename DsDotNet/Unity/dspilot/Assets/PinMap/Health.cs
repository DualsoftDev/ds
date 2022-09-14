using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    private Image img;
    public float health = 50;

    void Start()
    {
        StartCoroutine(SetHealth());
    }
/*    
    // Update is called once per frame
    void Update()
    {
        setHealthColor(health);
    }
*/
    public void setHealthColor(float hp)
    {
        img = gameObject.GetComponent<Image>();
        img.color = new Color32((byte)(255*health/100), (byte)(255*health/100), (byte)(255*health/100), 255);
    }


    private IEnumerator SetHealth()
    {
        while(true)
        {
            setHealthColor(health);
            yield return new WaitForSeconds(0.01f);
        }

    }
}
