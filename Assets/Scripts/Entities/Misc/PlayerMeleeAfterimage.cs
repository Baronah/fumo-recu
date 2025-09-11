using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMeleeAfterimage : MonoBehaviour
{
    [SerializeField] private float PersistTime = 3.0f;
    Slider DurationSlider;

    void Start()
    {
        DurationSlider = GetComponentInChildren<Slider>();
        StartCoroutine(Persist());
    }

    IEnumerator Persist()
    {
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
