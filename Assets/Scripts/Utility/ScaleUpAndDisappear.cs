using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        Vector3 originalScale = transform.localScale;
        Color originalColor = spriteRenderer.color, 
            transparentColor = new(originalColor.r, originalColor.g, originalColor.b, 0);
        float elapsedTime = 0f;
        float duration = Duration;
        while (elapsedTime < duration)
        {
            transform.localScale = Vector3.Lerp(originalScale, MaxScale, (elapsedTime / duration));
            spriteRenderer.color = Color.Lerp(originalColor, transparentColor, (elapsedTime / duration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localScale = MaxScale;
        Destroy(gameObject);
    }
}
