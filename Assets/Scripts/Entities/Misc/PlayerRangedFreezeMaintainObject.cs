using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRangedFreezeMaintainObject : MonoBehaviour
{
    [SerializeField] float RotateDegree = 30;
    [SerializeField] float PulseTime = 2f;
    [SerializeField] float PulseWait = 0.5f;
    [SerializeField] float FinalPulse_Alpha = 0.6f;

    SpriteRenderer renderer;
    Image image;

    private void Update()
    {
        transform.Rotate(new(0, 0, RotateDegree * Time.deltaTime));
    }

    Color init;
    Color clear;
    IEnumerator Pulse()
    {
        if (!renderer && !image) yield break;

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

    bool initialized = false;
    private void OnEnable()
    {
        if (!initialized)
        {
            image = GetComponent<Image>();
            renderer = GetComponent<SpriteRenderer>();
            init = renderer ? renderer.color : image.color;
            clear = new(init.r, init.g, init.b, FinalPulse_Alpha);
            initialized = true;
        }

        StartCoroutine(Pulse());
    }
}