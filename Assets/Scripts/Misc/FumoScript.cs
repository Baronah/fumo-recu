using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FumoScript : MonoBehaviour
{
    [SerializeField] private AudioClip WinBGM;
    public AudioClip f_WinBGM => WinBGM;

    [SerializeField] private short squishCount = 1;
    [SerializeField] private float squishDelay = 0.1f;
    [SerializeField] private float squishInterval = 5f;
    [SerializeField] private float squishDuration = 0.2f;
    [SerializeField] private float squishAmount = 0.2f;

    Vector3 originalScale;
    AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        originalScale = transform.localScale;
        StartCoroutine(SquishCoroutine());
    }

    IEnumerator SquishCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(squishInterval);
            StartCoroutine(Squish());
        }
    }

    IEnumerator Squish()
    {
        Vector3 squishedScale = new Vector3(originalScale.x * (1 + squishAmount), originalScale.y * (1 - squishAmount), originalScale.z);
        float duration = squishDuration;
        float elapsedTime;

        for (int i = 0; i < squishCount; i++) 
        { 
            elapsedTime = 0f;
            if (audioSource != null) audioSource.Play();

            // Squish down
            while (elapsedTime < duration)
            {
                transform.localScale = Vector3.Lerp(originalScale, squishedScale, (elapsedTime / duration));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.localScale = squishedScale;
            elapsedTime = 0f;
            // Return to original scale
            while (elapsedTime < duration)
            {
                transform.localScale = Vector3.Lerp(squishedScale, originalScale, (elapsedTime / duration));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.localScale = originalScale;
            yield return new WaitForSeconds(squishDelay);
        }
    }

    public void OnFumoPickUp()
    {
        StopAllCoroutines();
        transform.localScale = originalScale;
    }
}
