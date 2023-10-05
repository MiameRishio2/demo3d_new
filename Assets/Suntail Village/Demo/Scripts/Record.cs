using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Record : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Color _down;
    [SerializeField] private Color _up;

    private Image _image;
    private Text _text;

    private AudioClip _audioClip;

    private AudioSource _audioSource;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _text = GetComponentInChildren<Text>();

        _audioSource = GetComponent<AudioSource>();
    }
    // Start is called before the first frame update
    //private void Start()
    //{
    //    _image = GetComponent<Image>();
    //    _text = GetComponentInChildren<Text>();

    //    _audioSource = GetComponent<AudioSource>();
    //}

    public void OnPointerDown(PointerEventData eventData)
    {
        _image.color = _down;
        _text.text = "recording";
        /* 这里null会取默认值也就是第一个搜寻到的设备, false不循环，60每秒帧速率fps，而采样率这里必须选16000（语音识别sdk默认值）或者选8000*/
        _audioClip = Microphone.Start(null, false, 60, 16000);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _image.color = _up;
        _text.text = "record";
        Microphone.End(null);
        string result =  Speech.Asr(_audioClip);

        InputField input = this.GetComponentInParent<InputField>();

        input.text = result;
        if (input.text == string.Empty)
            input.text = "nothing";
    }
}
