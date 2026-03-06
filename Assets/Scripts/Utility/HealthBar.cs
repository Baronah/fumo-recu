using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;

    // Start is called before the first frame update
    void Start()
    {
        slider = GetComponentInChildren<Slider>();
        
    }

    public void SetMaxHealth(float mHealth)
    {
        slider.maxValue = mHealth;
        slider.value = mHealth;
    }

	public void SetMaxHealth(float mHealth, bool alsoSetHealth)
	{
		slider.maxValue = mHealth;
	}

	// Update is called once per frame
	public void SetHealth(float health)
    {
        slider.value = health;
    }
}
