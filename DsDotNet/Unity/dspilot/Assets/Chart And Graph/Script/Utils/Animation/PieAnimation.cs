#define Graph_And_Chart_PRO
using UnityEngine;
namespace ChartAndGraph
{
    public class PieAnimation : MonoBehaviour
    {
        public bool AnimateOnStart = true;
        public bool AnimateOnEnable = true;
        public bool AnimateRotation = true;
        [Range(0f,360f)]
        public float AnimateSpan = 360f;
        public float AnimationTime =2f;
        public AnimationCurve Curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        PieChart pieChart;
        float StartTime = -1f; 
        RectTransform rectTransform;
        float rotationVal = 0f;
        public float roattionSpeed = 10f;

        void Start()
        {
            rectTransform = this.GetComponent<RectTransform>();
            pieChart = GetComponent<PieChart>();
            if(AnimateOnStart)
                Animate();
        }

        public void OnEnable()
        {
            pieChart = GetComponent<PieChart>();
            if (AnimateOnEnable)
                Animate();
        }
        public void Animate()
        {
            if (pieChart != null)
            {
                if(StartTime <0f)
                    AnimateSpan = pieChart.AngleSpan;
                pieChart.AngleSpan = Curve.Evaluate(0f);
                StartTime = Time.time;
            }
        }
        void Update()
        {
            if(StartTime >= 0f && pieChart != null)
            {
                float curveTime = 0f;
                if (Curve.length > 0)
                    curveTime = Curve.keys[Curve.length - 1].time;
                float elasped = Time.time - StartTime;
                elasped /= AnimationTime;
                float blend = elasped;
                elasped *= curveTime;
                elasped = Curve.Evaluate(elasped);
                if (blend >= 1f)
                {
                    pieChart.AngleSpan = AnimateSpan;
                    StartTime = -1f;
                }
                else
                    pieChart.AngleSpan = Mathf.Lerp(0f, AnimateSpan, elasped);
            }

            if(AnimateRotation)
            {
                rectTransform.localRotation = Quaternion.Euler(0, 0, rotationVal);
                rotationVal += roattionSpeed * Time.deltaTime;
            }

        }
    }
}