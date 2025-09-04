using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SkillTree_Outview : MonoBehaviour
{
    [SerializeField] private Sprite DefaultSkillSprite;
    [SerializeField] private Image[] SkillImages;

    short UpdateCnt = 0;
    void Update()
    {
        UpdateCnt++;
        if (UpdateCnt < 60) return;

        UpdateCnt = 0;
        SetSkills();
    }

    void SetSkills()
    {
        for (int i = 0; i < SkillImages.Length; i++)
        {
            var frame = SkillImages[i].transform.Find("Frame").GetComponent<Image>();

            if (CharacterPrefabsStorage.Skills.Count < (i + 1))
            {
                SkillImages[i].sprite = DefaultSkillSprite;
                frame.color = Color.clear;
            }
            else
            {
                var data = CharacterPrefabsStorage.Skills.ElementAt(i).Value;
                SkillImages[i].sprite = data.skillIcon;
                frame.color = data.skillType switch
                { 
                    SkillTree_Manager.SkillType.SENSES => new(0, 1, 0.83f, 0.5f),
                    SkillTree_Manager.SkillType.TECHS => new(0.95f, 0.3f, 1, 0.5f),
                    SkillTree_Manager.SkillType.SPECS => new(0.84f, 0.83f, 0, 0.5f),
                    _ => Color.white,
                };
            }
        }
    }
}
