using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FumoScript : MonoBehaviour
{
    [SerializeField] private GameObject clearEffect, squishTargetObject, rangeIndicator;
    [SerializeField] private AudioClip WinBGM;
    public AudioClip f_WinBGM => WinBGM;

    [SerializeField] private short squishCount = 1;
    [SerializeField] private float squishDelay = 0.1f;
    [SerializeField] private float squishInterval = 5f;
    [SerializeField] private float squishDuration = 0.2f;
    [SerializeField] private float squishAmount = 0.2f;

    [SerializeField] private float SkillRange = 800f;
    RectTransform rectTransform;

    RawImage glowImg;
    Vector3 originalScale;
    AudioSource audioSource;
    GameObject sprite;

    public enum FumoObjectiveType
    {
        PICK_UP,
        PROTECT,
    }

    public FumoObjectiveType ObjectiveType = FumoObjectiveType.PICK_UP;

    public GameObject Fumo => sprite;

    private bool isPickedUp = false, isSquishing = false;
    void Start()
    {
        rectTransform = rangeIndicator.GetComponent<RectTransform>();

        glowImg = GetComponentInChildren<RawImage>();
        sprite = transform.Find("Object/Sprite").gameObject;
        Canvas[] cvs = GetComponentsInChildren<Canvas>();
        foreach (var cv in cvs) cv.sortingLayerID = SortingLayer.NameToID("Ground");

        audioSource = GetComponent<AudioSource>();

        originalScale = sprite.transform.localScale;

        StartCoroutine(SkillEffect());
        StartCoroutine(SquishCoroutine());
    }

    public IEnumerator SkillEffect()
    {
        bool canUseSkill = CharacterPrefabsStorage.Skills.ContainsKey(SkillTree_Manager.SkillName.MINT_PHALANX)
                            || CharacterPrefabsStorage.Skills.ContainsKey(SkillTree_Manager.SkillName.MINT_WINDRUSH);

        rangeIndicator.SetActive(canUseSkill);
        rectTransform.sizeDelta = new(
                SkillRange * 2f,
                SkillRange * 2f
            );

        if (!canUseSkill)
        {
            yield break;
        }

        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            var player = EntityBase.SearchForNearestEntityAroundCertainPoint(typeof(PlayerBase), transform.position, SkillRange, true);
            if (!player)
            {
                continue;
            }

            string Key = "FUMO_SKILL_PHALANX";
            if (CharacterPrefabsStorage.Skills.ContainsKey(SkillTree_Manager.SkillName.MINT_PHALANX))
            {
                player.ApplyEffect(Effect.AffectedStat.DEF, Key, 215, 0.6f, true);
                player.ApplyEffect(Effect.AffectedStat.RES, Key, 25, 0.6f, false);
            }
            else if (CharacterPrefabsStorage.Skills.ContainsKey(SkillTree_Manager.SkillName.MINT_WINDRUSH))
            {
                if (player.MspdBuffs.ContainsKey(Key))
                {
                    float BuffValue = Mathf.Min(player.MspdBuffs[Key].Value + 10, 100);
                    player.ApplyEffect(Effect.AffectedStat.MSPD, Key, BuffValue, 0.6f, true);
                }
                else
                    player.ApplyEffect(Effect.AffectedStat.MSPD, Key, 20, 0.6f, true);
            }
        }
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

        Transform targetTransform = sprite.transform;
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

        rangeIndicator.SetActive(false);
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

    public static int GetAllTimeFumo()
    {
        int aFumo = 0;
        string[] CompletedLevels = PlayerPrefs.GetString("CompletedLevels", "").Split(' ');
        foreach (var level in CompletedLevels)
        {
            if (level.StartsWith("FM-"))
                aFumo++;
            if (level.EndsWith("_CM"))
                aFumo++;
        }

        return aFumo;
    }
}
