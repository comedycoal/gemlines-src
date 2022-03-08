using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string Name;

    public AudioClip Clip;

    [Range(0.0f, 1.0f)]
    public float Volume;

    [Range(0.1f, 5.0f)]
    public float Pitch;

    [HideInInspector]
    public AudioSource Source { get; set; }
}
