using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SkillTree_Intro : MonoBehaviour
{
    [SerializeField] GameObject Overlay;
    [SerializeField] TMP_Text introText;

    private SkillTree_Manager SkillTree_Manager;

    private void OnEnable()
    {
        SkillTree_Manager = GetComponent<SkillTree_Manager>();

        if (PlayerPrefs.GetInt("ResearchIntroPlayed", 0) != 0)
        {
            Destroy(Overlay);
            SkillTree_Manager.enabled = true;
            return;
        }

        StartCoroutine(Intro());
    }

    /*
     A private place filled with books, crafts, ideas, theories, and a bunch of everything else. 
    Each is a mystery awaiting to be figured out.

    She comes here whenever new idea sparks,
    I come here whenever new idea sparks,
    To implement. To experiment. To verify. Or simply, to write it down.

    She also uses this place to store valuable items to her, such as samples for experiment, books, novel-looking ores, or just thing she really likes.

    In short, it's a little of everything!
    Welcome to Mint's research.

    Feel free to look around, and if there's something that you need here, you may bring it along.
    Though, remember to return them when you're finished. 

    You are welcomed to this place anytime! 

    <color=#b1b1b1>Oh, and also, this is her secret base. 
    She loves it here more than anywhere. 
    Don't tell anyone else about it, okay?</color>
     */
    IEnumerator Intro()
    {
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(FadeInText
            ("A private place filled with books, crafts, ideas, theories,\nand a bunch of everything else.", TEXT_APPEAR_TYPE.FADE_IN, 3f, 5f));
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeInText
            ("Each is a mystery awaiting to be figured out.", TEXT_APPEAR_TYPE.FADE_IN, 1f, 4f));
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeInText
            ("For example,\na first prototype of an <color=yellow>\"Attention-holding device\"</color>.", 
            TEXT_APPEAR_TYPE.FADE_IN, 1f, 6f));
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(FadeInText
            ("It's still untested,\njust like most of <color=#00FFD5>her</color> inventions here.",
            TEXT_APPEAR_TYPE.FADE_IN, 0.5f, 6f));
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeInText
            ("<color=#00FFD5>She</color> insists that it will work.", TEXT_APPEAR_TYPE.FADE_IN, 0.4f, 3f));
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeInText
            ("<color=#f800ff>I</color>, well, doubt that it will.", TEXT_APPEAR_TYPE.FADE_IN, 0.4f, 3f));
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeInText
            ("How about <color=yellow>you</color>?", TEXT_APPEAR_TYPE.FADE_IN, 0.4f, 3f));
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeInText
            ("How exactly, will <color=yellow>we</color> find an answer to it?", TEXT_APPEAR_TYPE.FADE_IN, 0.4f, 4f));
        yield return new WaitForSeconds(1.5f);
        yield return StartCoroutine(FadeInText
            ("Welcome to <color=#00FFD5>Mint</color>'s research!", TEXT_APPEAR_TYPE.FADE_IN, 2f, 3f));
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(FadeInText
            ("There's cookies on the table.", TEXT_APPEAR_TYPE.FADE_IN, 0.5f, 2f));
        yield return new WaitForSeconds(1.5f);

        introText.color = new(0.024f, 0.769f, 0.647f);
        introText.fontStyle = FontStyles.Italic;
        yield return StartCoroutine(FadeInText
            ("\"Ah! Would you like to read with me? It's really comfortable sitting here.\"", TEXT_APPEAR_TYPE.TYPEWRITER, 3f, 3f));
        PlayerPrefs.SetInt("ResearchIntroPlayed", 1);
        SkillTree_Manager.enabled = true;
        Destroy(Overlay);
    }

    public enum TEXT_APPEAR_TYPE
    {
        FADE_IN,
        TYPEWRITER
    }
    IEnumerator FadeInText(string content, TEXT_APPEAR_TYPE type, float duration, float holdTime = 0f)
    {
        TMP_Text text = introText;
        text.text = "";

        float elapsed = 0f;
        Color originalColor = text.color;
        originalColor.a = 1f;

        if (type == TEXT_APPEAR_TYPE.FADE_IN)
        {
            text.text = content;
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / duration);
                text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
            text.color = originalColor;

        }
        else if (type == TEXT_APPEAR_TYPE.TYPEWRITER)
        {
            text.color = originalColor;
            text.text = "";
            float waitTime = duration / content.Length;
            foreach (char c in content)
            {
                text.text += c;
                yield return new WaitForSeconds(waitTime);
            }
        }

        yield return new WaitForSeconds(holdTime);

        elapsed = 0f;
        duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, elapsed / duration);
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        text.text = "";
    }
}
