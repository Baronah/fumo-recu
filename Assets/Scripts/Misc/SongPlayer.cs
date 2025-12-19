using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongPlayer : MonoBehaviour
{
    [SerializeField] AudioSource BGM;
    [SerializeField] public AudioClip Vocal, NonVocal;
    [SerializeField] TMP_Text Length, CurrentDuration;
    [SerializeField] Slider LengthSlider;

    // Start is called before the first frame update
    void Start()
    {
        LengthSlider.maxValue = BGM.clip.length;
        Length.text = string.Format("{0:D2}:{1:D2}", (int)BGM.clip.length / 60, (int)BGM.clip.length % 60);
    }

    // Update is called once per frame
    void Update()
    {
        float currentTime = BGM.time;
        CurrentDuration.text = string.Format("{0:D2}:{1:D2}", (int)currentTime / 60, (int)currentTime % 60);
        LengthSlider.value = currentTime;
    }

    public void ToggleVocal()
    {
        float currentTime = BGM.time;
        if (BGM.clip == Vocal)
        {
            BGM.clip = NonVocal;
        }
        else
        {
            BGM.clip = Vocal;
        }
        BGM.time = currentTime;
        BGM.Play();
    }
}
