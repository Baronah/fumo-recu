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
        WINDBLOW_NORTH,
        WINDBLOW_SOUTH,
    }

    public static SkillTree_Manager Instance;
    private SkillTree_SkillComponent selectedSkill;
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
        if (selectedSkill == skill)
        {
            DeselectSkill();
            return;
        }

        selectedSkill = skill;
        HighlightSelectedSkill();
        ShowSkillDetails();
    }

    private void DeselectSkill()
    {
        selectedSkill.OnDeselect_SetMutuallyExclusive();
        selectedSkill = null;
        skillDetailsPanel.SetActive(false);
        skillDetailsText.text = skillNameText.text = "";
        skillIconImage.sprite = null;
    }

    private void HighlightSelectedSkill()
    {
        // Logic to highlight the selected skill
        // e.g., change button color, add outline, etc.
    }

    private void ShowSkillDetails()
    {
        if (selectedSkill == null) return;

        selectedSkill.SetMutuallyExclusive();
        skillDetailsPanel.SetActive(true);
        skillDetailsText.text = selectedSkill.skillDescription;
        skillNameText.text = selectedSkill.skillNameText;
        skillIconImage.sprite = selectedSkill.skillIcon;
    }
}