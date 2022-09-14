using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;




[Serializable]
public class DSData : MonoBehaviour
{
    // instance 멤버변수는 private하게 선언
    private static DSData instance = null;

    private void Awake()
    {
        if (null == instance)
        {
            // 씬 시작될때 인스턴스 초기화, 씬을 넘어갈때도 유지되기위한 처리
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            // instance가, GameManager가 존재한다면 GameObject 제거 
            Destroy(this.gameObject);
        }
    }

    // Public 프로퍼티로 선언해서 외부에서 private 멤버변수에 접근만 가능하게 구현
    public static DSData Instance
    {
        get
        {
            if (null == instance)
            {
                return null;
            }
            return instance;
        }
    }

    //Color[] colors = new Color[]{Color.blue,Color.green,Color.magenta,Color.red,Color.white,Color.yellow};

    public  const string start = "start";
    public  const string init = "initialize";
    public  const string stream = "stream";
    public  const string update = "updating";
    public  const string info = "infomation";
    public const string stop = "stop"; //stop thread

    public const string ready = "ds_status.R";
    public const string going = "ds_status.G";
    public const string finish = "ds_status.F";
    public const string homing = "ds_status.H";

    public static string prevMessage = ""; // Save Previous Message

    public static bool needInitialize = false;
    public static bool isDisplayInfo = false;

    public static string mode = start;
    public static int percent = 0;

    //=============================================================================

    public static Dictionary<string, Real> realDic = new Dictionary<string, Real>();
    public static Dictionary<string, Call> callDic = new Dictionary<string, Call>();

    
    public Dictionary<string, Segments> segments = new Dictionary<string, Segments>();


    
    public struct Segments
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Parent { get; set; }
        public string Status { get; set; }

        //real
        public double Theta { get; set; }
        public Dictionary<string, int> Indices { get; set; }
        public Dictionary<string, Dictionary<string, bool>> Targets { get; set; }
        public Color color { get; set; }
        //call
        public bool Value { get; set; }
        public Position Position { get; set; }
        public Size Size { get; set; }
        public string Error { get; set; }
        
    }



    public struct Position
    {
        float X { get; set; }
        float Y { get; set; }

        public Position(float x, float y)
        {
            X = x;
            Y = y;
        }
    }



    public struct Size
    {
        float W { get; set; }
        float H { get; set; }

        public Size(float w, float h)
        {
            W = w;
            H = h;
        }
    }
}















[Serializable]
public class Real
{
    Color[] colors = new Color[] { Color.blue, Color.green, Color.magenta, Color.red, Color.white, Color.yellow };

    public string name;
    public Color color;
    public Dictionary<string, Call> children;
    public Dictionary<string, int> indices;
    public float theta;    //theta
    public string status; // 1. Ready, 2. Going, 3. Finish, 4. Homing


    public Dictionary<string, float> targets;    //call, 정답값 
    public string parent;
    public float value;

    public Real(string _name, int colorNum = 9999)
    {
        name = _name;
        children = new Dictionary<string, Call>();
        //color는 그래프 만들때 부여

    }
    public Real(string _name, string _status, float _value, Color _color)
    {
        name = _name;
        value = _value;
        status = _status;
        color = _color;
    }

    public Real(string _name, string _parent, string _status = DSData.ready)
    {
        name = _name;
        parent = _parent;
        children = new Dictionary<string, Call>();
        indices = new Dictionary<string, int>();
        theta = 0;
        status = _status;
    }

}

[Serializable]
public class Call
{
    public string name;
    public string parent;    //부모 Real
    public int index;
    public float finishValue;    //정답값

    //public Dictionary<string, int> targets;  //real?

    //public float value;    //정답값 - > Real targets
    public string status; // 1. Ready, 2. Going, 3. Finish, 4. Homing
    public string error;
    public float health;
    public float value;
    public float theta;    //theta
    public int realIndex;
    public Material material;

    //pin
    public float x;
    public float y;
    public float width;    //input int
    public float height;    //input int

    public Call(string _name, string _parent, int _index, float _finishValue, float _x, float _y, float _width, float _height)
    {
        name = _name;
        index = _index;
        finishValue = _finishValue;
        parent = _parent;
        x = _x;
        y = _y;
        width = _width;
        height = _height;

        status = DSData.ready;
        health = 100f;
    }
    public Call(string _name, string _status, string _error, float _health, float _value, int _realIndex, Material _material)
    {
        name = _name;
        status = _status;
        error = _error;
        health = _health;
        value = _value;
        realIndex = _realIndex;
        material = _material;
    }

    public Call(string _name, string _parent, float _x, float _y, float _width, float _height)
    {
        name = _name;
        //targets = new Dictionary<string, int>();
        parent = _parent;
        x = _x;
        y = _y;
        width = _width;
        height = _height;
        health = 100f;
        status = DSData.ready;
    }

    public Call(string _name, string _parent, float _x, float _y, float _width, float _height, Material _material)
    {
        name = _name;
        //targets = new Dictionary<string, int>();
        parent = _parent;
        x = _x;
        y = _y;
        width = _width;
        height = _height;
        status = DSData.ready;
        material = _material;
    }


    public Call(string _name, string _parent)
    {
        name = _name;
        //targets = new Dictionary<string, int>();
        parent = _parent;
        status = DSData.ready;
    }
}

[Serializable]
public struct PinMap
{
    public float x;
    public float y;
    public float width;
    public float height;
    //public int CallIndex;

    public PinMap(float _x, float _y, float _width, float _height)
    {
        x = _x;
        y = _y;
        width = _width;
        height = _height;
    }
}





