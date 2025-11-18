using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMututallyExclusive : MonoBehaviour
{
    private Image[] Lines;
    private TMP_Text[] Texts;

    private List<float> storeWidths = new();

    List<RectTransform> rectTransforms = new();

    private void Start()
    {
        Lines = GetComponentsInChildren<Image>();
        Texts = GetComponentsInChildren<TMP_Text>();

        if (!SkillTree_Manager.ShowIntro) return;

        foreach (var item in Texts)
        {
            item.color = new(1, 1, 1, 0);
        }

        for (int i = 0; i < Lines.Length; i++)
        {
            var Line = Lines[i];
            var rectTransform = Line.GetComponent<RectTransform>();
            storeWidths.Add(rectTransform.sizeDelta.x);
            rectTransform.sizeDelta = new(0, rectTransform.sizeDelta.y);

            rectTransforms.Add(rectTransform);
        }
    }

    public void DoIntro() =>
                StartCoroutine(IntroCoroutine());

    private IEnumerator IntroCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        float duration = 0.7f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            foreach (var item in Texts)
            {
                item.color = new(1, 1, 1, t);
            }

            foreach (var rectTransform in rectTransforms)
                rectTransform.sizeDelta = new(Mathf.Lerp(0, storeWidths[rectTransforms.IndexOf(rectTransform)], t), rectTransform.sizeDelta.y);
        
            yield return null;
        }

        foreach (var rectTransform in rectTransforms) 
            rectTransform.sizeDelta = new(storeWidths[rectTransforms.IndexOf(rectTransform)], rectTransform.sizeDelta.y);
        
        foreach (var item in Texts)
        {
            item.color = new(1, 1, 1, 1);
        }
        yield return null;
    }
}
