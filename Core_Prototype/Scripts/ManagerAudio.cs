using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("-------------Audio Source--------------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource soundFXSource;

    [Header("-------------Audio Clip--------------")]
    public AudioClip MainMenuSong;
    public AudioClip Credits;
    public AudioClip Game;

    private void Start()
    {
        musicSource.clip = MainMenuSong;
        musicSource.Play();
    }
}
