using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Image Garden;

    private void Start()
    {
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
            Garden.color = new Color(Garden.color.r, Garden.color.g, Garden.color.b, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        Garden.color = new Color(Garden.color.r, Garden.color.g, Garden.color.b, 1f);
    }

    public void Play() => SceneManager.LoadScene("Level_Selection");
    public void Quit() => Application.Quit();
}
