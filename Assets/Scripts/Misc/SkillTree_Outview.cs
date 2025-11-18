using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SkillTree_Outview : MonoBehaviour
{
    [SerializeField] private Sprite DefaultSkillSprite;
    [SerializeField] private Image[] SkillImages, Lines;
    [SerializeField] private Image StartImg;
    [SerializeField] private GameObject Empty, Fun_1, Fun_2, Fun_3, Fun_4;

    private UIGradient StartGradient;

    private void Start()
    {
        StartGradient = StartImg.GetComponent<UIGradient>();
    }

    short UpdateCnt = 0;
    void Update()
    {
        UpdateCnt++;
        if (UpdateCnt < 40) return;

        UpdateCnt = 0;
        SetSkills();
    }

    void SetSkills()
    {
        Empty.SetActive(CharacterPrefabsStorage.Skills.Count == 0);
        Fun_1.SetActive(CharacterPrefabsStorage.Skills.Count >= 1);
        Fun_2.SetActive(CharacterPrefabsStorage.Skills.Count >= 2);
        Fun_3.SetActive(CharacterPrefabsStorage.Skills.Count >= 3);
        Fun_4.SetActive(CharacterPrefabsStorage.Skills.Count >= 4);

        for (int i = 0; i < SkillImages.Length; i++)
        {
            var frame = SkillImages[i].transform.Find("Frame").GetComponent<Image>();

            if (CharacterPrefabsStorage.Skills.Count < (i + 1))
            {
                SkillImages[i].gameObject.SetActive(false);
                SkillImages[i].sprite = DefaultSkillSprite;
                frame.color = Color.clear;
            }
            else
            {
                SkillImages[i].gameObject.SetActive(true);
                var data = CharacterPrefabsStorage.Skills.ElementAt(i).Value;
                SkillImages[i].sprite = data.skillIcon;
                frame.color = GetColorBaseOnType(data.skillType);
            }
        }

        for (int i = 0; i < Lines.Length; i++)
        {
            UIGradient gradient = Lines[i].GetComponent<UIGradient>();
            if (CharacterPrefabsStorage.Skills.Count < (i + 2))
            {
                gradient.UpdateColors(Color.clear, Color.clear);
            }
            else
            {
                gradient.UpdateColors(
                    GetColorBaseOnType(CharacterPrefabsStorage.Skills.ElementAt(i).Value.skillType),
                    GetColorBaseOnType(CharacterPrefabsStorage.Skills.ElementAt(i + 1).Value.skillType)
                    );
            }

        }

        Color defaultColor = new(0.76f, 0.76f, 0.76f);

        if (CharacterPrefabsStorage.Skills.Count == 0)
        {
            StartGradient.UpdateColors(defaultColor, defaultColor);
        }
        else
        {
            StartGradient.UpdateColors(
                GetColorBaseOnType(CharacterPrefabsStorage.Skills.ElementAt(0).Value.skillType),
                GetColorBaseOnType(CharacterPrefabsStorage.Skills.ElementAt(CharacterPrefabsStorage.Skills.Count - 1).Value.skillType)
            );
        }
    }

    Color GetColorBaseOnType(SkillTree_Manager.SkillType type)
    {
        return type switch
        {
            SkillTree_Manager.SkillType.SENSES => new(0, 1, 0.83f),
            SkillTree_Manager.SkillType.TECHS => new(0.95f, 0.3f, 1),
            SkillTree_Manager.SkillType.SPECS => new(0.84f, 0.83f, 0),
            _ => Color.white,
        };
    }
}
