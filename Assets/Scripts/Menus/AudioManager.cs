using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.InputSystem.XR;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioMixer audioMixer;

    [Header("Audio Clips")]
    [Header("Menus")]
    public AudioClip menuBackground;
    public AudioClip button;

    [Header("Level Music")]
    public AudioClip section1BackgroundMusic;

    [Header("Movement")]
    public AudioClip[] footsteps;
    public AudioClip landing;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Header("Misc")]
    public AudioClip pickup_dirt;
    public AudioClip pickup_item;

    public static AudioManager audioManagerInstance;

    private void Awake()
    {
        if (audioManagerInstance == null)
        {
            audioManagerInstance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            PlayMusic(menuBackground);
        }
        else
        {
            PlayMusic(section1BackgroundMusic);
        }
    }

    public void PlayMusic(AudioClip musicClip)
    {
        if (musicSource != null && musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void PlaySFX(AudioClip sfxClip)
    {
        if (sfxSource != null && sfxClip != null)
        {
            sfxSource.clip = sfxClip;
            sfxSource.PlayOneShot(sfxClip);
        }
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void StopSFX()
    {
        if (sfxSource != null)
        {
            sfxSource.Stop();
        }
    }

    public void PlayFootstep(Vector3 position)
    {
        if (footsteps.Length > 0 && sfxSource != null)
        {
            int index = Random.Range(0, footsteps.Length);

            sfxSource.transform.position = position;
            sfxSource.PlayOneShot(footsteps[index], FootstepAudioVolume);
        }
    }

    public void PlayLanding(Vector3 position)
    {
        if (landing != null && sfxSource != null)
        {
            sfxSource.transform.position = position;
            sfxSource.PlayOneShot(landing, FootstepAudioVolume);
        }
    }
}