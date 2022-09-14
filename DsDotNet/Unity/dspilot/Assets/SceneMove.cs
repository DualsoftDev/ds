using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMove : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(2))
        {
            if(SceneManager.GetActiveScene().name == "MainScene")
            {
                SceneManager.LoadScene("InfoScene");
            }
            else if(SceneManager.GetActiveScene().name == "InfoScene")
            {
                SceneManager.LoadScene("MainScene");
            }

        }
    }
}
