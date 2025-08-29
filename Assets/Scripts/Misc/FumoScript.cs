using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FumoScript : MonoBehaviour
{
    [SerializeField] private GameObject clearEffect;
    [SerializeField] private AudioClip WinBGM;
    public AudioClip f_WinBGM => WinBGM;

    [SerializeField] private short squishCount = 1;
    [SerializeField] private float squishDelay = 0.1f;
    [SerializeField] private float squishInterval = 5f;
    [SerializeField] private float squishDuration = 0.2f;
    [SerializeField] private float squishAmount = 0.2f;

    RawImage glowImg;
    Vector3 originalScale;
    AudioSource audioSource;
    GameObject sprite;

    public GameObject Fumo => sprite;

    private bool isPickedUp = false, isSquishing = false;

    void Start()
    {
        glowImg = GetComponentInChildren<RawImage>();
        sprite = transform.Find("Object/Sprite").gameObject;
        Canvas[] cvs = GetComponentsInChildren<Canvas>();
        foreach (var cv in cvs) cv.sortingLayerID = SortingLayer.NameToID("Ground");

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
        if (isSquishing) yield break;

        isSquishing = true;
        Vector3 squishedScale = new Vector3(originalScale.x * (1 + squishAmount), originalScale.y * (1 - squishAmount), originalScale.z);
        float duration = squishDuration;
        float elapsedTime;

        Transform targetTransform = isPickedUp ? sprite.transform : transform;

        for (int i = 0; i < squishCount; i++) 
        { 
            elapsedTime = 0f;
            if (audioSource != null) audioSource.Play();

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
            yield return new WaitForSecondsRealtime(squishDelay);
        }

        isSquishing = false;
    }

    public void SquishFun() => StartCoroutine(Squish());

    public Vector3 OnFumoPickUp()
    {
        if (isPickedUp) return Vector3.zero;

        Vector3 spritePosition = Camera.main.WorldToScreenPoint(sprite.transform.position);

        isPickedUp = true;
        sprite.GetComponent<Image>().color = Color.white;
        Instantiate(clearEffect, transform.position, Quaternion.identity);
        StopAllCoroutines();

        Canvas cv = GetComponent<Canvas>();
        cv.sortingLayerID = SortingLayer.NameToID("UI");
        cv.renderMode = RenderMode.ScreenSpaceOverlay;

        Canvas spriteCV = sprite.GetComponent<Canvas>(), glowCV = glowImg.GetComponent<Canvas>();
        spriteCV.overrideSorting = true;
        glowCV.transform.position = spriteCV.transform.position = spritePosition;

        transform.localScale = originalScale;
        transform.Find("Object/Shadow").gameObject.SetActive(false);
        squishCount = 1;

        return spritePosition;
    }

    public void FumoZoomInComplete()
    {
        originalScale = sprite.transform.localScale;
        GetComponentInChildren<Button>().interactable = true;
    }
}
