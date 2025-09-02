using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTree_Manager : MonoBehaviour
{
    public enum SkillType
    {
        PASSIVE,
        ACTIVE,
    }

    public enum SkillName
    {
        NONE,
        WINGED_STEPS_A,
        WINGED_STEPS_B,
        WINGED_STEPS_C,
        GEOGOLIST_A,
        GEOGOLIST_B,
        GEOGOLIST_C,
        WINDBLOW_NORTH,
        WINDBLOW_SOUTH,
    }

    public static SkillTree_Manager Instance;
    private HashSet<SkillTree_SkillComponent> selectedSkill = new();
    [SerializeField] private Image skillIconImage;
    [SerializeField] private TMP_Text skillNameText, skillDetailsText;
    [SerializeField] private GameObject skillDetailsPanel;

    public List<SkillTree_SkillComponent> allSkills;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            allSkills = FindObjectsOfType<SkillTree_SkillComponent>().ToList();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnSkillSelected(SkillTree_SkillComponent skill)
    {
        if (selectedSkill.Contains(skill))
        {
            DeselectSkill(skill);
            return;
        }

        selectedSkill.Add(skill);
        HighlightSelectedSkill(skill);
        ShowSkillDetails(skill);
    }

    private void DeselectSkill(SkillTree_SkillComponent skill)
    {
        skill.OnDeselect_SetMutuallyExclusive();
        selectedSkill.Remove(skill);
        skillDetailsPanel.SetActive(false);
        skillDetailsText.text = skillNameText.text = "";
        skillIconImage.sprite = null;
    }

    private void HighlightSelectedSkill(SkillTree_SkillComponent skill )
    {
        // Logic to highlight the selected skill
        // e.g., change button color, add outline, etc.
    }

    private void ShowSkillDetails(SkillTree_SkillComponent skill)
    {
        if (!selectedSkill.Contains(skill)) return;

        var exclusiveSkills = skill.SetMutuallyExclusive();
        selectedSkill.ExceptWith(allSkills.Where(s => exclusiveSkills.Contains(s.skillName)));

        skillDetailsPanel.SetActive(true);
        skillDetailsText.text = skill.skillDescription;
        skillNameText.text = skill.skillNameText;
        skillIconImage.sprite = skill.skillIcon;
    }
}