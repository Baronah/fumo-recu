using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRangedFreezeMaintainObject : MonoBehaviour
{
    [SerializeField] float RotateDegree = 30;
    [SerializeField] float PulseTime = 2f;
    [SerializeField] float PulseWait = 0.5f;
    [SerializeField] float FinalPulse_Alpha = 0.6f;

    private void Start()
    {
        StartCoroutine(Pulse());
    }

    private void Update()
    {
        transform.Rotate(new(0, 0, RotateDegree * Time.deltaTime));
    }

    IEnumerator Pulse()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        Image image = GetComponent<Image>();

        if (!renderer && !image) yield break;

        Color init = renderer ? renderer.color : image.color;
        Color clear = new(init.r, init.g, init.b, FinalPulse_Alpha);

        bool pulseIn = false;

        float c;
        while (true)
        {
            c = 0;
            if (pulseIn)
            {
                while (c < PulseTime)
                {
                    Color color = Color.Lerp(init, clear, c * 1.0f / PulseTime);
                    if (renderer) renderer.color = color;
                    else image.color = color;

                    c += Time.deltaTime;
                    yield return null;
                }
                
                if (renderer) renderer.color = clear;
                else image.color = clear;
            }
            else
            {
                while (c < PulseTime)
                {
                    Color color = Color.Lerp(clear, init, c * 1.0f / PulseTime);
                    if (renderer) renderer.color = color;
                    else image.color = color;

                    c += Time.deltaTime;
                    yield return null;
                }

                if (renderer) renderer.color = init;
                else image.color = init;
            }

            pulseIn = !pulseIn;
            yield return new WaitForSeconds(PulseWait);
        }
    }
}