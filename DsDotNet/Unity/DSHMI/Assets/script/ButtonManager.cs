using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class ButtonManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    Image img;
    Sequence mySequence;

    [SerializeField] private bool isPress = false;
    [SerializeField] private bool isStartButton = false;
    public bool isAutoMode = false;


    private void Start()
    {
        img = transform.GetComponent<Image>();
    }

    private void FixedUpdate()
    {
        if (isAutoMode) //������Ʈ ����� �ٽ� ������ + ��ư ��Ȱ�� Ȱ��ȭ,   ������ ��� �ش� ����� �� ���� �ȹް� �����
        {
            //transform.
        }
        else
        {

        }

    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isPress) { return; }
        ChangeColor();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPress) { return; }
        img.DOColor(Color.white, 0.5f);
        mySequence.Kill();
    }

    private void ChangeColor()
    {
        mySequence = DOTween.Sequence();

        mySequence.Append(img.DOColor(Color.grey, 0.5f));
        mySequence.Append(img.DOColor(Color.white, 0.5f)).OnComplete(ChangeColor);
    }


/*

    public void ButtonClicked()
    {
        mySequence = DOTween.Sequence();

        mySequence.Append(img.DOColor(Color.black, 0.5f).SetLoops(1, LoopType.Yoyo));
        mySequence.Append(img.DOColor(Color.white, 0.5f).SetLoops(1, LoopType.Yoyo));

    }


    public void OnMouseDown()
    {
        mySequence = DOTween.Sequence();

        mySequence.Append(img.DOColor(Color.black, 0.5f).SetLoops(-1, LoopType.Yoyo));
        mySequence.Append(img.DOColor(Color.white, 0.5f).SetLoops(-1, LoopType.Yoyo));

    }
*/
}

