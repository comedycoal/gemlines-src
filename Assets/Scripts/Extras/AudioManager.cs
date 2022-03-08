using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; protected set; }

    [SerializeField] private Sound m_mainMusic;
    [SerializeField] private Sound[] m_managedSound;

    private float m_musicOriginalPitch;
    private float m_musicOriginalVolume;

    public bool MusicOn { get; protected set; }
    public bool SoundOn { get; protected set; }

    // Events
    public event EventHandler<Boolean> MusicStateChanged;
    public event EventHandler<Boolean> SoundStateChanged;

    private void Awake()
    {
        Instance = this;

        var go = new GameObject(m_mainMusic.Name + "SoundObject");
        go.transform.parent = transform;
        m_mainMusic.Source = go.AddComponent<AudioSource>();
        m_mainMusic.Source.clip = m_mainMusic.Clip;
        m_mainMusic.Source.pitch = m_mainMusic.Pitch;
        m_mainMusic.Source.volume = m_mainMusic.Volume;
        m_musicOriginalPitch = m_mainMusic.Pitch;
        m_musicOriginalVolume = m_mainMusic.Volume;
        m_mainMusic.Source.loop = true;

        foreach (var sound in m_managedSound)
        {
            go = new GameObject(sound.Name + "SoundObject");
            go.transform.parent = transform;
            sound.Source = go.AddComponent<AudioSource>();
            sound.Source.clip = sound.Clip;
            sound.Source.pitch = sound.Pitch;
            sound.Source.volume = sound.Volume;
        }
    }

    public void PlayMusic()
    {
        m_mainMusic.Source.Play();
    }

    public void StopMusic()
    {
        m_mainMusic.Source.Stop();
    }

    public void ModifyMusic(float pitchMod, float volumeMod)
    {
        m_mainMusic.Source.pitch = m_musicOriginalPitch * pitchMod;
        m_mainMusic.Source.volume = m_musicOriginalVolume * volumeMod;
    }

    public void ResetMusic()
    {
        m_mainMusic.Source.pitch = m_mainMusic.Pitch;
        m_mainMusic.Source.volume = m_mainMusic.Volume;
    }

    public void SetMusicState(bool value)
    {
        MusicOn = value;
        m_mainMusic.Source.mute = !MusicOn;
        MusicStateChanged?.Invoke(this, MusicOn);
    }

    public void SetSoundState(bool value)
    {
        SoundOn = value;
        foreach (var sound in m_managedSound)
        {
            sound.Source.mute = !SoundOn;
        }
        SoundStateChanged?.Invoke(this, SoundOn);
    }

    public void Play(string name)
    {
        var obj = Array.Find(m_managedSound, x => x.Name == name);
        if (obj != null)
            obj.Source.Play();
    }

    public void Stop(string name)
    {
        var obj = Array.Find(m_managedSound, x => x.Name == name);
        if (obj != null)
            obj.Source.Stop();
    }

    public float GetLength(string name)
    {
        var obj = Array.Find(m_managedSound, x => x.Name == name);
        if (obj != null)
            return obj.Clip.length / Mathf.Abs(obj.Pitch);
        return -1f;
    }
}
