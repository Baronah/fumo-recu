using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AssassinFeudBondObject : MonoBehaviour
{
    [SerializeField] Image FillPart, Glow;

    private void Start()
    {
        Glow.gameObject.SetActive(false);
    }

    public void SetFeud(int feudLevel, int maxFeud)
    {
        FillPart.fillAmount = Mathf.Clamp01(feudLevel * 1.00f / maxFeud);
        if (feudLevel >= maxFeud) FillPart.color = Color.red;
        else FillPart.color = new Color(Color.red.a, Color.red.g, Color.red.b, 0.35f); ;

        Glow.gameObject.SetActive(feudLevel >= maxFeud);
    }
}
