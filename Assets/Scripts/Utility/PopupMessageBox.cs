using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupMessageBox : MonoBehaviour
{
    [SerializeField] private TMP_Text MessageTxt;

    Button CloseBtn;
    private void Start()
    {
        CloseBtn = GetComponentInChildren<Button>();
        CloseBtn.onClick.AddListener(Close);
    }

    public void SetMessage(string message) => MessageTxt.text = message.Replace(@"\n", "\n");

    public void Display(float duration) => StartCoroutine(StartDisplaying(duration));

    IEnumerator StartDisplaying(float duration)
    {
        StartCoroutine(IDisplayCoroutine());
        yield return new WaitForSeconds(duration);
        
        if (CloseCoroutine != null) yield break;
        
        CloseCoroutine = StartCoroutine(ICloseCoroutine());
    }

    Coroutine CloseCoroutine = null;
    public void Close()
    {
        if (CloseCoroutine != null) return;
        CloseCoroutine = StartCoroutine(ICloseCoroutine());
    }

    IEnumerator IDisplayCoroutine()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        cg.alpha = 0;

        float c = 0, d = 0.25f;
        while (c < d)
        {
            cg.alpha = Mathf.Lerp(0, 1f, c * 1.0f / d);

            c += Time.deltaTime;
            yield return null;
        }

        cg.alpha = 1f;
    }

    IEnumerator ICloseCoroutine()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        cg.alpha = 1;

        float c = 0, d = 0.25f;
        while (c < d)
        {
            cg.alpha = Mathf.Lerp(1, 0f, c * 1.0f / d);

            c += Time.deltaTime;
            yield return null;
        }

        cg.alpha = 1f;
        Destroy(gameObject);
    }
}