using System.Collections;
using UnityEngine;

public class PlayerRangedFreezeMaintainObject : MonoBehaviour
{
    [SerializeField] float RotateDegree = 30;
    [SerializeField] float PulseTime = 2f;
    [SerializeField] float PulseWait = 0.5f;
    [SerializeField] float FinalPulse_A = 0.6f;

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
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Color init = spriteRenderer.color;
        Color clear = new(init.r, init.g, init.b, FinalPulse_A);

        bool pulseIn = false;

        float c = 0;
        while (true)
        {
            c = 0;
            if (pulseIn)
            {
                while (c < PulseTime)
                {
                    spriteRenderer.color = Color.Lerp(init, clear, c * 1.0f / PulseTime);
                    c += Time.deltaTime;
                    yield return null;
                }
                spriteRenderer.color = clear;
            }
            else
            {
                while (c < PulseTime)
                {
                    spriteRenderer.color = Color.Lerp(clear, init, c * 1.0f / PulseTime);
                    c += Time.deltaTime;
                    yield return null;
                }
                spriteRenderer.color = init;
            }

            pulseIn = !pulseIn;
            yield return new WaitForSeconds(PulseWait);
        }
    }
}