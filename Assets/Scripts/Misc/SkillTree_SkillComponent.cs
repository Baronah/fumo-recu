using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static SkillTree_Manager;

public class SkillTree_SkillComponent : MonoBehaviour
{
    public SkillType skillType;
    public SkillName skillName;

    public Sprite skillIcon => skillIconImage.sprite;
    public string skillNameText, skillDescription;
    public SkillName[] mutuallyExclusiveSkills;

    private Button selfButton;
    private Image skillIconImage;

    private void Awake()
    {
        selfButton = GetComponent<Button>();
        skillIconImage = GetComponent<Image>();

        selfButton.onClick.AddListener(OnSkillButtonClicked);
    }

    public void OnSkillButtonClicked()
    {
        SkillTree_Manager.Instance.OnSkillSelected(this);
    }

    public SkillName[] SetMutuallyExclusive()
    {
        skillIconImage.color = Color.white;
        foreach (var skill in mutuallyExclusiveSkills)
        {
            var skillComponent = SkillTree_Manager.Instance.allSkills.Find(s => s.skillName == skill);
            if (skillComponent != null)
            {
                var grayScale = skillComponent.skillIconImage.color;
                grayScale = new Color(0.2f, 0.2f, 0.2f);
                skillComponent.skillIconImage.color = grayScale;
            }
        }

        return mutuallyExclusiveSkills;
    }

    public void OnDeselect_SetMutuallyExclusive()
    {
        skillIconImage.color = Color.white;
        foreach (var skill in mutuallyExclusiveSkills)
        {
            var skillComponent = SkillTree_Manager.Instance.allSkills.Find(s => s.skillName == skill);
            if (skillComponent != null)
            {
                skillComponent.skillIconImage.color = Color.white;
            }
        }
    }
}
