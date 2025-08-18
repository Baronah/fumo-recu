using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Image Garden;
    [SerializeField] private Image Title;

    private Color TitleStartColor, TitleEndColor;
    private bool FlipX = true;

    private void Start()
    {
        TitleStartColor = Title.color;
        StartCoroutine(GardenFadeIn());
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

    public void Play() => SceneManager.LoadScene("Level_Selection");
    public void Quit() => Application.Quit();
}
