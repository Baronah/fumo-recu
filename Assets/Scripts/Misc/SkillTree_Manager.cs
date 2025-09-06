using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTree_Manager : MonoBehaviour
{
    [SerializeField] Button TechViewBtn;
    [SerializeField] GameObject Block, SkillHighlight;

    [SerializeField] GameObject SENSES_BLOCK, TECHS_BLOCK, SPECS_BLOCK, TECHS_PRECEDE_BLOCK, SPECS_PRECEDE_BLOCK;
    [SerializeField] Button SensesUnlockBtn, TechsUnlockBtn, SpecsUnlockBtn;
    [SerializeField] short FUMO_COST_SENSE = 3, FUMO_COST_TECHS = 3, FUMO_COST_SPECS = 3;
    [SerializeField] TMP_Text FumoCnt, SelectedCnt;

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
        DASH_TOUCH,
        DASH_LETHAL,
        DASH_FAITH,
        WINDBLOW_NORTH,
        WINDBLOW_SOUTH,
        FREEZE_TIMEUP,
        FREEZE_CHARGE,
        FREEZE_SUPERCONDUCT,
        OBSCURE_VISION,
        GRAVITY,
        JUGGERNAUNT_DURATION,
        JUGGERNAUNT_IGNITE,
        JUGGERNAUNT_PULL,
        JUGGERNAUNT_AFTERSHOCK,
        VICTORY_ATK,
        VICTORY_MSPD,
        SPIRAL_MORE,
        SPIRAL_TRAVEL,
        SPIRAL_SHADOW,
        BLACKFLASH,
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

    public void GetPlayerProgress()
    {
        string[] CompletedLevels = PlayerPrefs.GetString("CompletedLevels", "").Split(' ');
        bool techUnlocked = CompletedLevels.Any(s => s.Contains("_CM"));

        TechViewBtn.interactable = techUnlocked;
        Block.SetActive(!techUnlocked);
    }

    public void CheckUnlockStatus()
    {
        bool IsSensesUnlocked = PlayerPrefs.GetInt("SensesUnlocked", 0) != 0,
             IsTechUnlocked = PlayerPrefs.GetInt("TechsUnlocked", 0) != 0,
             IsSpecsUnlocked = PlayerPrefs.GetInt("SpecsUnlocked", 0) != 0;

        SENSES_BLOCK.SetActive(!IsSensesUnlocked);
        TECHS_PRECEDE_BLOCK.SetActive(!IsSensesUnlocked);

        TECHS_BLOCK.SetActive(!IsTechUnlocked && !TECHS_PRECEDE_BLOCK.activeSelf);
        SPECS_PRECEDE_BLOCK.SetActive(!IsTechUnlocked);

        SPECS_BLOCK.SetActive(!IsSpecsUnlocked && !SPECS_PRECEDE_BLOCK.activeSelf);

        int fumo = PlayerPrefs.GetInt("Fumo", 0);
        SensesUnlockBtn.interactable = fumo >= FUMO_COST_SENSE;
        TechsUnlockBtn.interactable = fumo >= FUMO_COST_TECHS;
        SpecsUnlockBtn.interactable = fumo >= FUMO_COST_SPECS;

        FumoCnt.text = "x " + fumo;
    }

    public void UnlockSense()
    {
        int fumo = PlayerPrefs.GetInt("Fumo", 0);
        if (fumo < FUMO_COST_SENSE) return;

        fumo -= FUMO_COST_SENSE;
        PlayerPrefs.SetInt("Fumo", fumo);
        PlayerPrefs.SetInt("SensesUnlocked", 1);
        PlayerPrefs.Save();

        CheckUnlockStatus();
    }

    public void UnlockTechs()
    {
        int fumo = PlayerPrefs.GetInt("Fumo", 0);
        if (fumo < FUMO_COST_TECHS) return;

        fumo -= FUMO_COST_TECHS;
        PlayerPrefs.SetInt("Fumo", fumo);
        PlayerPrefs.SetInt("TechsUnlocked", 1);
        PlayerPrefs.Save();

        CheckUnlockStatus();
    }

    public void UnlockSpecs()
    {
        int fumo = PlayerPrefs.GetInt("Fumo", 0);
        if (fumo < FUMO_COST_SPECS) return;

        fumo -= FUMO_COST_SPECS;
        PlayerPrefs.SetInt("Fumo", fumo);
        PlayerPrefs.SetInt("SpecsUnlocked", 1);
        PlayerPrefs.Save();

        CheckUnlockStatus();
    }

    private void OnEnable()
    {
        CheckUnlockStatus();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            CheckUnlockStatus();

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
        if (selectingSkill && SkillHighlight.activeSelf)
            SkillHighlight.transform.position = selectingSkill.transform.position;

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

        int selectedCnt = CharacterPrefabsStorage.Skills.Count;
        short cnt = 0;
        foreach (var item in CharacterPrefabsStorage.Skills)
        {
            TechImgs[cnt].sprite = item.Value.skillIcon;
            cnt++;
        }

        bool isMaxed = CharacterPrefabsStorage.Skills.Count >= MaxSkillCount;
        if (isMaxed)
            allSkills.ForEach (s => s.Button.interactable = false);

        SelectedCnt.text = isMaxed
            ?
            $"<color=#FFF775>Selected: {selectedCnt}/{MaxSkillCount}</color>"
            :
            $"<color=#A1A1A1>Selected: {selectedCnt}/{MaxSkillCount}</color>";
    }

    public void ForceQuit()
    {
        Clear();
        gameObject.SetActive(false);
    }
}