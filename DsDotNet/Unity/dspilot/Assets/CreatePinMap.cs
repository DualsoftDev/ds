using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatePinMap : MonoBehaviour
{
    public GameObject prefab;
    //private DSData dsData;
    public List<GameObject> pins = new List<GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        //dsData = GameObject.GetComponent<DsData>();
        //GameObject thisObject = Instantiate(prefab, new Vector2(0 + 400 , Screen.height - 300), Quaternion.identity, GameObject.Find("Canvas").transform) as GameObject;        //0 + input width , Screen.height - input height
        //pins.Add(thisObject);
        pins.Add((GameObject) Instantiate(prefab, new Vector2(0 + 400 , Screen.height - 300), Quaternion.identity, GameObject.Find("Canvas").transform));    //struct call 추가예정



                Instantiate(prefab, new Vector2(0 + 200 , Screen.height - 500), Quaternion.identity, GameObject.Find("Canvas").transform);
                        Instantiate(prefab, new Vector2(0 + 1400 , Screen.height - 550), Quaternion.identity, GameObject.Find("Canvas").transform);
                                Instantiate(prefab, new Vector2(0 + 600 , Screen.height - 500), Quaternion.identity, GameObject.Find("Canvas").transform);
                                        Instantiate(prefab, new Vector2(0 + 400 , Screen.height - 270), Quaternion.identity, GameObject.Find("Canvas").transform);
                                                Instantiate(prefab, new Vector2(0 + 400 , Screen.height - 1000), Quaternion.identity, GameObject.Find("Canvas").transform);


        pins[0].name = "testPin";
        var test = pins[0].GetComponent<PinMark>();
       // Debug.Log(test);

    } 
}
