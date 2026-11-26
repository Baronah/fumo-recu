using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Acceleration : UI_TiedWithEffect
{
    [SerializeField] Image SpeedImg;
    [SerializeField] Sprite[] SpeedIndicators;

    float MaxSpeed;
    Vector2 direction;

    public void Inititialize(EntityBase target, string key, Dictionary<string, Effect> TargetDictionary, Vector2 direction, float maxSpeed)
    { 
        SpeedImg.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        Target = target;
        Key = key;
        this.TargetDictionary = TargetDictionary;
        this.MaxSpeed = maxSpeed;
        this.direction = direction;

        Initialized = true;
        frmcnt = 999;
    }

    public void OnDisable()
    {
        SpeedImg.sprite = SpeedIndicators[0];
        Initialized = false;
        frmcnt = 0;
    }

    short frmcnt = 0;
    protected override void Update()
    {
        if (!Initialized) return;
        if (!Target || !Target.IsAlive())
        {
            Destroy(gameObject);
            return;
        }

        frmcnt++;
        if (frmcnt < 5) return;
        frmcnt = 0;

        SpeedImg.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        float midSpeed = MaxSpeed * 0.4f;
        float current = TargetDictionary[Key].Value;

        if (current < midSpeed) SpeedImg.sprite = SpeedIndicators[0];
        else if (current < MaxSpeed) SpeedImg.sprite = SpeedIndicators[1];
        else SpeedImg.sprite = SpeedIndicators[2];
    }
}