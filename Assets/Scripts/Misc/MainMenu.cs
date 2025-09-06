using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Image Garden;
    [SerializeField] private Image Title;

    [SerializeField] private Slider BGMSlider, SFXSlider;
    [SerializeField] private TMP_Text ResolutionTxt;
    [SerializeField] private Button ResolutionUpBtn, ResolutionDownBtn;

    private int ResolutionIndex = 0;

    private Color TitleStartColor, TitleEndColor;
    private bool FlipX = true;

    private AudioSource BGM;

    private void Start()
    {
        BGM = GetComponent<AudioSource>();
        InitValues();
        TitleStartColor = Title.color;
        StartCoroutine(GardenFadeIn());

        Application.targetFrameRate = 60;
    }

    IEnumerator GardenFadeIn()
    {
        yield return new WaitForSeconds(8f);

        float duration = 10f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Garden.color = new Color(Garden.color.r, Garden.color.g, Garden.color.b, Mathf.Lerp(0, 0.8f, elapsed / duration));
            yield return null;
        }
        Garden.color = new Color(Garden.color.r, Garden.color.g, Garden.color.b, 0.8f);

        yield return new WaitForSeconds(8f);
        Title.GetComponent<DVDLogo>().enabled = true; // Enable DVDLogo script
    }

    IEnumerator TitleColorPulse()
    {
        short count = 0;
        while (true)
        {
            TitleEndColor = new Color(Random.Range(0, 1.0f), Random.Range(0, 1.0f), Random.Range(0, 1.0f));
            float pulseDuration = 8f;
            float elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.PingPong(elapsed / pulseDuration, 1f);
                Title.color = Color.Lerp(TitleStartColor, TitleEndColor, alpha);
                yield return null;
            }

            TitleStartColor = TitleEndColor;
            Title.color = TitleEndColor;
            count++;

            if (count >= 3)
            {
                StartCoroutine(FlipTitle());  
                count = 0;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator FlipTitle()
    {
        float duration = 0.75f;
        float elapsed = 0f;
        Vector3 startScale = Title.transform.localScale;
        Vector3 endScale = FlipX 
            ? new Vector3(-startScale.x, startScale.y, startScale.z)
            : new Vector3(startScale.x, -startScale.y, startScale.z);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Title.transform.localScale = Vector3.Lerp(startScale, endScale, elapsed / duration);
            yield return null;
        }
        Title.transform.localScale = endScale;

        yield return new WaitForSeconds(duration / 2);

        // Reset to original scale
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Title.transform.localScale = Vector3.Lerp(endScale, startScale, elapsed / duration);
            yield return null;
        }
        Title.transform.localScale = startScale;

        FlipX = !FlipX;
    }

    private void InitValues()
    {
        BGMSlider.value = PlayerPrefs.GetFloat("BGM", 1f);
        SFXSlider.value = PlayerPrefs.GetFloat("SFX", 1f);

        BGMSlider.maxValue = SFXSlider.maxValue = 1f;

        BGM.volume = BGMSlider.value;

        ResolutionIndex = PlayerPrefs.GetInt("Resolution", 3);
        ChangeResolution(0);
    }

    public void SetBGMVolume(float v)
    {
        PlayerPrefs.SetFloat("BGM", v);
        BGMSlider.value = v;
        BGM.volume = v;
    }

    public void SetSFXVolume(float v)
    {
        PlayerPrefs.SetFloat("SFX", v);
        SFXSlider.value = v;
    }

    public void ChangeResolution(int dir)
    {
        ResolutionIndex += dir;
        ResolutionIndex = Mathf.Clamp(ResolutionIndex, 0, 3);
        PlayerPrefs.SetInt("Resolution", ResolutionIndex);
        switch (ResolutionIndex)
        {
            case 0:
                Screen.SetResolution(1280, 720, false);
                ResolutionTxt.text = "1280 x 720";
                break;
            case 1:
                Screen.SetResolution(1366, 786, false);
                ResolutionTxt.text = "1366 x 786";
                break;
            case 2:
                Screen.SetResolution(1920, 1080, false);
                ResolutionTxt.text = "1920 x 1080";
                break;
            case 3:
                Screen.SetResolution(Screen.width, Screen.height, true);
                ResolutionTxt.text = "Full-screen";
                break;
        }
        ResolutionDownBtn.interactable = ResolutionIndex > 0;
        ResolutionUpBtn.interactable = ResolutionIndex < 3;
    }

    public void Play() => SceneManager.LoadScene("Level_Selection");
    public void Quit() => Application.Quit();
}
