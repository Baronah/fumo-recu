using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static SkillTree_Manager;

public class SkillTree_SkillComponent : MonoBehaviour
{
    public AudioSource OnSelectSFX;
    public SkillType skillType;
    public SkillName skillName;

    public Sprite skillIcon => skillIconImage.sprite;
    public string skillNameText, skillDescription, favorText, skillDescriptionInShort = "";
    public SkillName[] mutuallyExclusiveSkills;

    private Button selfButton;
    public Button Button => selfButton;

    private Image skillIconImage;

    [HideInInspector] public Transform Overlay;

    private void Awake()
    {
        if (selfButton && skillIconImage) return;

        selfButton = GetComponent<Button>();
        skillIconImage = GetComponent<Image>();

        selfButton.onClick.AddListener(OnSkillButtonClicked);
        var colors = selfButton.colors;
        colors.disabledColor = new(0.35f, 0.35f, 0.35f);
        
        selfButton.colors = colors;
    }

    public void OnTickOverlayCreated()
    {
        Overlay = transform.Find("TickOverlay(Clone)");
        Overlay.gameObject.SetActive(false);
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
                skillComponent.skillIconImage.color = new(0.5f, 0.5f, 0.5f);
            }
        }

        return mutuallyExclusiveSkills;
    }

    public void ResetMutuallyExclusive()
    {
        skillIconImage.color = Color.white;
        foreach (var skill in mutuallyExclusiveSkills)
        {
            SkillTree_SkillComponent skillComponent = SkillTree_Manager.Instance.allSkills.Find(s => s.skillName == skill);
            if (!skillComponent) continue;

            skillComponent.SetInteractableState();
        }
    }

    public void SetInteractableState()
    {
        if (!selfButton || !skillIconImage) Awake();

        bool interactableState = GetInteractableState();

        if (interactableState) skillIconImage.color = Color.white;
        selfButton.interactable = interactableState;
    }

    bool GetInteractableState()
    {
        if (CharacterPrefabsStorage.Skills.ContainsKey(skillName)) return false;
        if (LevelSelectionScript.Instance.GetBanSkills.Contains(skillName)) return false;
        foreach (var skill in mutuallyExclusiveSkills)
        {
            if (CharacterPrefabsStorage.Skills.ContainsKey(skill)) return false;
        }
        return true;
    }

    private void FixedUpdate()
    {
        if (selfButton)
        {
            var colors = selfButton.colors;
            colors.disabledColor = CharacterPrefabsStorage.Skills.ContainsKey(skillName)
                ? Color.white
                : new(0.35f, 0.35f, 0.35f);
        }

        if (Overlay == null) return;
        Overlay.gameObject.SetActive(CharacterPrefabsStorage.Skills.ContainsKey(skillName));
    }

    public HashSet<SkillName> OnSkillSelected(HashSet<SkillName> existings, bool addToPlayerSkill = true)
    {
        selfButton.interactable = false;
        foreach (var skill in mutuallyExclusiveSkills)
        {
            var skillComponent = SkillTree_Manager.Instance.allSkills.Find(s => s.skillName == skill);
            if (skillComponent == null) continue;
            skillComponent.selfButton.interactable = false;
        }

        if (addToPlayerSkill)
        {
            CharacterPrefabsStorage.Skills.Add(
                skillName,
                new SkillDataSet()
                {
                    skillType = skillType,
                    skillIcon = skillIcon,
                    nameInString = skillNameText,
                    skillDescription = skillDescriptionInShort == "" ? skillDescription : skillDescriptionInShort,
                }
                );

            foreach (var item in CharacterPrefabsStorage.Skills)
            {
                if (mutuallyExclusiveSkills.Contains(item.Key))
                    CharacterPrefabsStorage.Skills.Remove(item.Key);
            }
        }

        existings.AddRange(mutuallyExclusiveSkills);
        return existings;
    }

    public void OnSkillClear()
    {
        if (CharacterPrefabsStorage.Skills.ContainsKey(skillName)) return;
        SetInteractableState();
    }
}

public class SkillDataSet
{
    public SkillType skillType { get; set; }
    public Sprite skillIcon { get; set; }
    public string nameInString { get; set; }
    public string skillDescription { get; set; }
}