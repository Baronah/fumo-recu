using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTree_Manager : MonoBehaviour
{
    [SerializeField] GameObject SkillHighlight;

    public enum SkillType
    {
        SENSES,
        TECHS,
        SPECS,
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
        EQUIPMENT_BLADE,
        EQUIPMENT_SCOPE,
        EQUIPMENT_RADIO,
        EQUIPMENT_PROVISIONS,
        DASH_HASTEN,
        DASH_LETHAL,
        DASH_FAITH,
        WINDBLOW_NORTH,
        WINDBLOW_SOUTH,
        FREEZE_TIMEUP,
        FREEZE_CHARGE,
        FREEZE_SUPERCONDUCT,
        OBSCURE_VISION,
        GRAVITY,
    }

    public static SkillTree_Manager Instance;
    [SerializeField] private GameObject TickOverlay;

    [SerializeField] private Image skillIconImage;
    Sprite defaultSkillIcon;

    [SerializeField] private TMP_Text skillNameText, skillDetailsText;
    string defaultSkillName, defaultSkillDetailsText;

    [SerializeField] private GameObject skillDetailsPanel;

    [HideInInspector] public List<SkillTree_SkillComponent> allSkills;
    private SkillTree_SkillComponent selectingSkill;
    private HashSet<SkillName> exclusions = new();

    Image SkillViewPanelImg;
    Image[] TechImgs;

    [SerializeField] Button SelectButton, OkButton;
    [SerializeField] private short MaxSkillCount = 2;   

    [SerializeField] Color 
          IdleColor = new(0.35f, 0.35f, 0.35f),
          SpecsSkillViewPanelColor = new(0.57f, 0.51f, 0), 
          SensesSkillViewPanelColor = new(0, 0.59f, 0.65f),
          TechsSkillViewPanelColor = new(0.57f, 0, 0.8f);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            allSkills = FindObjectsOfType<SkillTree_SkillComponent>().ToList();
            allSkills.ForEach(skill =>
            {
                Instantiate(TickOverlay, skill.transform.position, Quaternion.identity, skill.transform);
                skill.OnTickOverlayCreated();
            });

            SkillViewPanelImg = skillDetailsPanel.GetComponent<Image>();
            defaultSkillIcon = skillIconImage.sprite;
            defaultSkillName = skillNameText.text;
            defaultSkillDetailsText = skillDetailsText.text;

            TechImgs = Techs.GetComponentsInChildren<Image>();
            for (int i = 0; i < TechImgs.Length; i++)
            {
                TechImgs[i].gameObject.SetActive(MaxSkillCount >= (i + 1));
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        OkButton.interactable = CharacterPrefabsStorage.Skills.Count > 0;
        SelectButton.interactable = selectingSkill && CharacterPrefabsStorage.Skills.Count < MaxSkillCount;
    }

    public void OnSkillSelected(SkillTree_SkillComponent skill)
    {
        allSkills.ForEach(s => { if (!exclusions.Contains(s.skillName)) s.OnSkillClear(); });

        if (selectingSkill == skill)
        {
            selectingSkill = null;
            DeselectSkill(skill);
            return;
        }

        selectingSkill = skill;
        ShowSkillDetails(skill);
    }

    private void DeselectSkill(SkillTree_SkillComponent skill)
    {
        SkillHighlight.SetActive(false);

        skill.ResetMutuallyExclusive();
        
        skillDetailsText.text = defaultSkillDetailsText;
        skillNameText.text = defaultSkillName;
        skillIconImage.sprite = defaultSkillIcon;

        SkillViewPanelImg.color = IdleColor;
    }

    private void ShowSkillDetails(SkillTree_SkillComponent skill)
    {
        if (!selectingSkill) return;

        SkillHighlight.transform.position = skill.transform.position;
        SkillHighlight.SetActive(true);

        skill.SetMutuallyExclusive();

        SkillViewPanelImg.color = skill.skillType switch
        {
            SkillType.SENSES => SensesSkillViewPanelColor,
            SkillType.TECHS => TechsSkillViewPanelColor,
            SkillType.SPECS => SpecsSkillViewPanelColor,
            _ => IdleColor,
        };

        skillDetailsText.text = skill.skillDescription;
        skillNameText.text = skill.skillNameText;
        skillIconImage.sprite = skill.skillIcon;
    }

    public void ConfirmSkillSelect()
    {
        if (selectingSkill == null) return;
        exclusions = selectingSkill.OnSkillSelected(exclusions);
        selectingSkill.Button.interactable = false;
        selectingSkill = null;
        SkillHighlight.SetActive(false);

        OnSelect_Update();
    }

    public void Clear()
    {
        CharacterPrefabsStorage.Skills.Clear();

        exclusions.Clear();
        allSkills.ForEach(s => s.OnSkillClear());
        selectingSkill = null;
        SkillHighlight.SetActive(false);

        skillDetailsText.text = defaultSkillDetailsText;
        skillNameText.text = defaultSkillName;
        skillIconImage.sprite = defaultSkillIcon;

        SkillViewPanelImg.color = IdleColor;

        OnSelect_Update();
    }

    [SerializeField] GameObject Techs;
    public void OnSelect_Update()
    {
        foreach (var item in TechImgs)
        {
            item.sprite = defaultSkillIcon;
        }

        short cnt = 0;
        foreach (var item in CharacterPrefabsStorage.Skills)
        {
            TechImgs[cnt].sprite = item.Value.skillIcon;
            cnt++;
        }

        if (CharacterPrefabsStorage.Skills.Count >= MaxSkillCount)
            allSkills.ForEach (s => s.Button.interactable = false);
    }

    public void ForceQuit()
    {
        Clear();
        gameObject.SetActive(false);
    }
}