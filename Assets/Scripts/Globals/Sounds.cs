using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Sounds", menuName = "ScriptableObjects/Sounds", order = 1)]
public class Sounds : ScriptableObject
{
    [SerializeField] public SoundData[] soundEffects;

    public AudioClip GetSoundEffect(string name)
    {
        foreach (SoundData sound in soundEffects)
        {
            if (sound.name == name)
            {
                return sound.clip;
            }
        }
        Debug.LogWarning($"Sound effect '{name}' not found.");
        return null;
    }
}

[Serializable]
public struct SoundData
{
    public string name;
    public AudioClip clip;
}