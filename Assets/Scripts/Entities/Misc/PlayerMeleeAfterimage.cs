using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMeleeAfterimage : MonoBehaviour
{
    private float PersistTime = 3.0f;
    Slider DurationSlider;

    public void SetPersist(float time)
    {
        PersistTime = time;
        StartCoroutine(Persist());
    }

    IEnumerator Persist()
    {
        DurationSlider = GetComponentInChildren<Slider>();
        DurationSlider.value = DurationSlider.maxValue = PersistTime;
        float c = PersistTime;
        while (c > 0)
        {
            c -= Time.deltaTime;
            DurationSlider.value = c;
            yield return null;
        }

        Destroy(gameObject);
    }

    public void OnPlayerDashCallback()
    {
        DurationSlider.gameObject.SetActive(false);
        StopAllCoroutines();
    }
}
