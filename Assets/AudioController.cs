using UnityEngine;
using JacobGames.SuperInvoke;

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

    public void PlayCountDownTick() {
        AudioPlayer(CountDownTick);
    }

    public void PlayCountDownLastTick() {
        AudioPlayer(CountDownLastTick);
    }

    public void PlayPropTakeoverSuccess() {
        AudioPlayer(PropTakeoverSuccess);
    }

    public void PlayPropTakeoverFail() {
        AudioPlayer(PropTakeoverFail);
    }


    private void AudioPlayer(AudioClip clipToPlay) {
        AudioSource audSource = gameObject.AddComponent<AudioSource>();
        audSource.clip = clipToPlay;
        audSource.Play();
        SuperInvoke.Run(()=> DestroyAudioSourceAfterPlayedClip(audSource), audSource.clip.length);
    }

    private void DestroyAudioSourceAfterPlayedClip(AudioSource sourceToDestroy) {
        Destroy(sourceToDestroy);
    }

}
