using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScaleUpAndDisappear : MonoBehaviour
{
    [SerializeField] public Vector3 MaxScale = new Vector3(200f, 200f, 200f);
    [SerializeField] private float Duration = 1f;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(c_ScaleUpAndDisappear());
    }

    IEnumerator c_ScaleUpAndDisappear()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Image[] images = GetComponentsInChildren<Image>();

        Vector3 originalScale = transform.localScale;
        Color originalColor = spriteRenderer.color, 
            transparentColor = new(originalColor.r, originalColor.g, originalColor.b, 0);

        Color[] originalImageColors = new Color[images.Length], transparentImageColors = new Color[images.Length];
        for (int i = 0; i < images.Length; i++)
        {
            originalImageColors[i] = images[i].color;
            transparentImageColors[i] = new Color(originalImageColors[i].r, originalImageColors[i].g, originalImageColors[i].b, 0);
        }

        float elapsedTime = 0f;
        float duration = Duration;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            transform.localScale = Vector3.Lerp(originalScale, MaxScale, t);
            spriteRenderer.color = Color.Lerp(originalColor, transparentColor, t);

            for (int i = 0; i < images.Length; i++)
            {
                images[i].color = Color.Lerp(originalImageColors[i], transparentImageColors[i], t);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localScale = MaxScale;
        Destroy(gameObject);
    }
}
