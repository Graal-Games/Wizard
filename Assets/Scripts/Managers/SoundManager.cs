using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    [SerializeField] private AudioSource _musicSource, _castMode, _instantSpellcastBuffer, _spellcastSuccessful, _spellcastUnsuccessful;


    void Awake() 
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }



    // ** REFACTOR TO MAKE IT SO THAT THE CLIP DOES NOT HAVE TO BE PASSED THROUGH ARGS
    public void PlayCastBuffer(AudioClip clip)
    {
        _instantSpellcastBuffer.clip = clip;
        _instantSpellcastBuffer.PlayOneShot(clip);
        //_instantSpellcastBuffer.Play();

        //if (_instantSpellcastBuffer.isPlaying)
        //{
        //    _instantSpellcastBuffer.Stop(); // Stop the currently playing audio
        //    return;
        //}
        //else
        //{
        //    _instantSpellcastBuffer.clip = clip;
        //    _instantSpellcastBuffer.Play();
        //}
    }

    public void StopCastBuffer()
    {
        _instantSpellcastBuffer.Stop();
    }



    // ** REFACTOR TO MAKE IT SO THAT THE CLIP DOES NOT HAVE TO BE PASSED THROUGH ARGS
    public void StopCastBuffer(AudioClip clip)
    {
        _instantSpellcastBuffer.clip = clip;
        //_instantSpellcastBuffer.PlayDelayed(-40f);
        _instantSpellcastBuffer.Stop();
    }



    // ** REFACTOR TO MAKE IT SO THAT THE CLIP DOES NOT HAVE TO BE PASSED THROUGH ARGS
    public void PlayCastSuccessful(AudioClip clip) 
    {
        _spellcastSuccessful.PlayOneShot(clip);

        //if (_spellcastSuccessful.isPlaying)
        //{
        //    _spellcastSuccessful.Stop(); // Stop the currently playing audio
        //    return;
        //}
        //else
        //{
        //    _spellcastSuccessful.clip = clip;
        //    _spellcastSuccessful.Play();
        //}
    }



    // ** REFACTOR TO MAKE IT SO THAT THE CLIP DOES NOT HAVE TO BE PASSED THROUGH ARGS
    public void PlayCastUnsuccessful(AudioClip clip)
    {
        _spellcastUnsuccessful.PlayOneShot(clip);
    }



    public void StopCastModeSound()
    {
        _castMode.Stop();
    }



    // ** REFACTOR TO MAKE IT SO THAT THE CLIP DOES NOT HAVE TO BE PASSED THROUGH ARGS
    public void PlaySound(AudioClip clip)
    {
        //Debug.LogFormat($"<color=blue>{_castMode.isPlaying}</color>");
        //if (_castMode.isPlaying)
        //{
        //    //Debug.LogFormat($"<color=blue>{_castMode.isPlaying}</color>");
        //    _castMode.Stop(); // Stop the currently playing audio
        //    return;
        //} else
        //{
            _castMode.clip = clip;
            _castMode.Play();
        //}

        
    }
}
