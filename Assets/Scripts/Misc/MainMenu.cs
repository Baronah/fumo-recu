using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Image Title;

    [SerializeField] private Slider BGMSlider, SFXSlider;
    [SerializeField] private TMP_Text ResolutionTxt;
    [SerializeField] private Button ResolutionUpBtn, ResolutionDownBtn;

    [SerializeField] Image StartObj;
    [SerializeField] AudioSource SquishAudioSrc;
    [SerializeField] Image Overlay;

    [SerializeField] GameObject RealMenu, FakeMenu;
    private int ResolutionIndex = 0;

    private Color TitleStartColor, TitleEndColor;
    private bool FlipX = true;

    private AudioSource BGM;

    private void Awake()
    {
        BGM = GetComponent<AudioSource>();
        if (SaveDataManager.IsResearchUnlocked)
        {
            RealMenu.SetActive(true);
            FakeMenu.SetActive(false);
            BGM.clip = FindFirstObjectByType<SongPlayer>().Vocal;
            BGM.Play();
        }
    }

    private void Start()
    {
        InitValues();
        TitleStartColor = Title.color;
        StartCoroutine(GardenFadeIn());
    }

    IEnumerator GardenFadeIn()
    {
        yield return new WaitForSeconds(14f);
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

    public void Play() => StartCoroutine(OnStartCoroutine());
    public void Quit() => Application.Quit();

    IEnumerator OnStartCoroutine()
    {
        Overlay.gameObject.SetActive(true);
        StartObj.GetComponent<Button>().interactable = false;

        if (SaveDataManager.IsResearchUnlocked)
        {
            BGM.Stop();
            yield return StartCoroutine(Squish());
            yield return new WaitForSeconds(1f);
        }

        yield return StartCoroutine(OverlayFadeIn());

        SceneManager.LoadSceneAsync("Level_Selection");
    }

    IEnumerator Squish()
    {
        float squishAmount = 0.35f, squishDuration = 0.15f;
        Vector3 originalScale = StartObj.transform.localScale;
        Transform targetTransform = StartObj.transform;
        Vector3 squishedScale = new Vector3(originalScale.x * (1 + squishAmount), originalScale.y * (1 - squishAmount), originalScale.z);
        float duration = squishDuration;
        float elapsedTime;

        elapsedTime = 0f;
        if (SquishAudioSrc != null) SquishAudioSrc.Play();

        // Squish down
        while (elapsedTime < duration)
        {
            targetTransform.localScale = Vector3.Lerp(originalScale, squishedScale, (elapsedTime / duration));
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        targetTransform.localScale = squishedScale;
        elapsedTime = 0f;
        // Return to original scale
        while (elapsedTime < duration)
        {
            targetTransform.localScale = Vector3.Lerp(squishedScale, originalScale, (elapsedTime / duration));
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        targetTransform.localScale = originalScale;
    }

    IEnumerator OverlayFadeIn()
    {
        float duration = 2f;
        float elapsed = 0f;
        Color startColor = Overlay.color;
        Color endColor = Color.black;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Overlay.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
        }
        Overlay.color = endColor;
    }
}
