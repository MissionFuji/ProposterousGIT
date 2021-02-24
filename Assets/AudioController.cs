using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    AudioSource AudSo;
    [SerializeField]
    AudioClip CountDownTick;
    [SerializeField]
    AudioClip CountDownLastTick;
    [SerializeField]
    AudioClip PropTakeoverSuccess;
    [SerializeField]
    AudioClip PropTakeoverFail;

    private void Awake() {
        AudSo = GetComponent<AudioSource>();
    }

    public void PlayCountDownTick() {
        AudSo.clip = CountDownTick;
        AudSo.Play();
    }

    public void PlayCountDownLastTick() {
        AudSo.clip = CountDownLastTick;
        AudSo.Play();
    }

    public void PlayPropTakeoverSuccess() {
        AudSo.clip = CountDownLastTick;
        AudSo.Play();
    }

    public void PlayPropTakeoverFail() {
        AudSo.clip = CountDownLastTick;
        AudSo.Play();
    }
}
