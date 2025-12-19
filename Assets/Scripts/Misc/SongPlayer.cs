using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongPlayer : MonoBehaviour
{
    [SerializeField] AudioSource BGM;
    [SerializeField] public AudioClip Vocal, NonVocal;
    [SerializeField] TMP_Text Lyrics, Length, CurrentDuration;
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
        LyricsUpdate(currentTime);
    }

    void LyricsUpdate(float currentTime)
    {
        if (currentTime >= 43.2f)
            Lyrics.text = "For what tomorrow may bright.";
        else if (currentTime >= 39.5f)
            Lyrics.text = "As always, we're still praying for what tomorrow may bright.";
        else if (currentTime >= 34.5f)
            Lyrics.text = "As always, we're still praying";
        else if (currentTime >= 25.5f)
            Lyrics.text = "Just as always, we are carrying our own sorrow";
        else if (currentTime >= 20.5f)
            Lyrics.text = "Just as always, we are";
        else
            Lyrics.text = "";
    }

    public void ToggleVocal()
    {
        float currentTime = BGM.time;
        if (BGM.clip == Vocal)
        {
            BGM.clip = NonVocal;
            Lyrics.text = "";
        }
        else
        {
            BGM.clip = Vocal;
            Lyrics.text = "";
        }
        BGM.time = currentTime;
        BGM.Play();
    }
}
