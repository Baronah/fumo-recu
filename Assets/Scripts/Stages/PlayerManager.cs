using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static EntityBase;

public class PlayerManager : MonoBehaviour
{
    private CameraMovement mainCamera;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;

    public enum PlayerType { MELEE, RANGED }
    [SerializeField] private PlayerType playerStartType;
    [SerializeField] private Transform PlayerSpawnpoint;
    [SerializeField] private GameObject SwapEffect;
    [SerializeField] private float SwapCooldown = 20f;

    [SerializeField] private Image Swapsymbol, AttackSprite, AttackCD, SkillSprite, SkillCD, SpecialSprite, SpecialCD, SwapCD, ActivePlayer, SwapToPlayer;
    [SerializeField] private Sprite MeleeIcon, RangedIcon;

    [Header("Input guide")]
    [SerializeField] private TMP_Text txtSwapKey; 
    [SerializeField] private TMP_Text txtViewKey, txtSViewKey, txtAttackKey, txtSpecialKey, txtSkillKey, txtSlowKey, txtMapKey;
    [SerializeField] private TMP_Text txtMapOffInstruction, txtSkillViewOffInstruction;

    [Header("Swap system")]
    private PlayerBase player;
    public PlayerBase activePlayer => player;

    [SerializeField] private float swapCooldownTimer = 0f;
    private float swapCurrentRotation = 0;

    private float SwapOverflowTimer = 0f;
    [SerializeField] private short SwapStacks = 0;
    [SerializeField] private short SwapMaxStacks = 2;
    [SerializeField] GameObject SwapReadyEffect;
    [SerializeField] private Image[] SwapStacksImg;
    [SerializeField] private Sprite SwapStackAvailable, SwapStackUnavailable;

    [Header("Skill View")]
    [SerializeField] private GameObject PlayerInfoSect;
    [SerializeField] private GameObject TechInfoSect, SkillView_Overlay, SkillView;
    [SerializeField] private Image PlayerIcon, SkillView_Attack, SkillView_Skill, SkillView_Special;
    [SerializeField] private TMP_Text SkillView_Attributes, SkillView_AttackText, SkillView_SkillName, SkillView_SkillText, SkillView_SpecialName, SkillView_SpecialText;
    private Coroutine skillViewCoroutine;

    [SerializeField] private GameObject[] TechViews;
    [SerializeField] private TMP_Text EmptyTechViewsText;

    public bool IsReadingSkillView => skillViewCoroutine != null && SkillView_Overlay.activeSelf;
    private bool CanSwapPlayer => swapCooldownTimer >= SwapCooldown && player && player.IsAlive();

    public bool IsPlayerAlive = true;

    [SerializeField] private GameObject[] Disables;

    List<Image> CDs => new() { AttackCD, SkillCD, SpecialCD, SwapCD };
    
    Coroutine AttackCooldownCoroutine, SkillCooldownCoroutine, SpecialCooldownCoroutine;

    private bool IsStageStarted = false;
    private List<AudioSource> sfxs;
    
    private AudioSource swapSfx => sfxs[0];
    private AudioSource hit_01_sfx => sfxs[1];
    private AudioSource hit_02_sfx => sfxs[2];

    public StageManager stageManager;

    private void Awake()
    {
        SkillView_Overlay.SetActive(false);
        TechInfoSect.SetActive(false);
        PlayerInfoSect.SetActive(true);

        GetPlayerStartType();
        UpdateKeybindTexts();
    }

    public void UpdateKeybindTexts()
    {
        txtViewKey.text = KeybindButton.GetDisplayNameForKey(InputManager.Instance.ViewInfoKey);
        txtAttackKey.text = KeybindButton.GetDisplayNameForKey(InputManager.Instance.AttackKey);
        txtSkillKey.text = KeybindButton.GetDisplayNameForKey(InputManager.Instance.SkillKey);
        txtSpecialKey.text = KeybindButton.GetDisplayNameForKey(InputManager.Instance.SpecialKey);
        txtSwapKey.text = KeybindButton.GetDisplayNameForKey(InputManager.Instance.PlayerSwapKey);
        txtSViewKey.text = KeybindButton.GetDisplayNameForKey(InputManager.Instance.SwapInfoKey);
        txtSlowKey.text = KeybindButton.GetDisplayNameForKey(InputManager.Instance.SlowKey);
        txtMapKey.text = KeybindButton.GetDisplayNameForKey(InputManager.Instance.ViewMapKey);

        txtMapOffInstruction.text = 
            $"Use your movement keys to move the camera." +
            $"\nUse [Ctrl] + [your move up/down keys] to zoom In/Out." +
            $"\nPress '{KeybindButton.GetDisplayNameForKey(InputManager.Instance.ViewMapKey)}' again to close this.";

        txtSkillViewOffInstruction.text =
            $"Press '{KeybindButton.GetDisplayNameForKey(InputManager.Instance.ViewInfoKey)}' again to close this.";
    }

    private void Start()
    {
        sfxs = GetComponents<AudioSource>().ToList();
        sfxs.Remove(sfxs.ElementAt(0)); // Remove the music audio source

        GetPlayerSkills();
        SwapPlayer();

        mainCamera = FindObjectOfType<CameraMovement>();
        stageManager = FindObjectOfType<StageManager>(true);
    }

    private void GetPlayerStartType()
    {
        playerStartType = CharacterPrefabsStorage.startingPlayer;
        if (playerStartType == PlayerType.MELEE)
        {
            SwapToPlayer.sprite = RangedIcon;
            ActivePlayer.sprite = MeleeIcon;
            Swapsymbol.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            SwapToPlayer.sprite = MeleeIcon;
            ActivePlayer.sprite = RangedIcon;
            Swapsymbol.transform.rotation = Quaternion.Euler(0, 0, 180);
            swapCurrentRotation = -180;
        }
    }

    private void Update()
    {
        if (!IsStageStarted) return;

        if (player && mainCamera)
        {
            mainCamera.UpdatePlayerMovement(player.transform);
        }

        for (int i = 0; i < SwapStacksImg.Length; ++i)
        {
            SwapStacksImg[i].sprite = CanSwapPlayer && SwapStacks >= (i + 1) ? SwapStackAvailable : SwapStackUnavailable;
        }

        swapCooldownTimer += Time.deltaTime;
        if (CanSwapPlayer)
        {
            SwapOverflowTimer += Time.deltaTime;
            if (SwapOverflowTimer >= SwapCooldown)
            {
                SwapOverflowTimer = 0;
                SwapStacks = (short) Mathf.Min(SwapStacks + 1, SwapMaxStacks);
            }
        }
        else SwapOverflowTimer = 0;
        
        SwapReadyEffect.SetActive(CanSwapPlayer);

        if (Input.GetKeyDown(InputManager.Instance.PlayerSwapKey) && CanSwapPlayer)
        {
            SwapPlayer();
        }
        else if (Input.GetKeyDown(InputManager.Instance.ViewInfoKey))
        {
            ViewSkill();
        }
        else if (Input.GetKeyDown(InputManager.Instance.SwapInfoKey))
        {
            SwapView();
        }
    }

    public bool MintBlessing = false;
    public void GetPlayerSkills()
    {
        if (CharacterPrefabsStorage.Skills.ContainsKey(SkillTree_Manager.SkillName.EQUIPMENT_RADIO))
        {
            SwapCooldown *= 0.85f;
        }

        if (CharacterPrefabsStorage.Skills.ContainsKey(SkillTree_Manager.SkillName.JUST_A_NICE_LOOKING_ROCK))
        {
            MintBlessing = true;
        }
    }

    public void SwapPlayer()
    {
        if (IsStageStarted && (!CanSwapPlayer || !player || !player.IsAlive())) return;
        if (IsStageStarted)
        {
            if (SwapStacks > 0) SwapStacks--;
            else swapCooldownTimer = 0f;

            SwapOverflowTimer = 0f;
        }

        ResetAllCooldown();

        PlayerType swapToPlayertype;
        if (IsStageStarted)
        {
            swapToPlayertype = 
                player is PlayerMelee
                ?
                PlayerType.RANGED
                :
                PlayerType.MELEE;
        }
        else
        {
            swapToPlayertype = playerStartType;
        }

        Vector3 spawnPosition = IsStageStarted ? player.transform.position : PlayerSpawnpoint.position;

        GameObject Effect = Instantiate(SwapEffect, spawnPosition, Quaternion.identity);
        GameObject newPlayerPrefab = CharacterPrefabsStorage.PlayerPrefabs[(int) swapToPlayertype];
        
        GameObject inPlayer = Instantiate(newPlayerPrefab, spawnPosition, Quaternion.identity);
        swapSfx.Play();

        if (IsStageStarted && player)
        {
            player.OnFieldSwapOut(inPlayer.GetComponent<PlayerBase>());
        }

        StartCoroutine(FadeOut(Effect, IsStageStarted ? 1f : 2f));

        if (swapToPlayertype == PlayerType.RANGED)
        {
            SwapToPlayer.sprite = MeleeIcon;
            ActivePlayer.sprite = RangedIcon;
        }
        else
        {             
            SwapToPlayer.sprite = RangedIcon;
            ActivePlayer.sprite = MeleeIcon;
        }

        StartCoroutine(SwapCooldownE(SwapCooldown, swapCooldownTimer, IsStageStarted));
    }

    public void SwapCooldownOnStart()
    {
        AssignPlayerSkillSprites(player);
        StartCoroutine(SwapCooldownE(SwapCooldown, swapCooldownTimer, false));
    }

    void AssignPlayerSkillSprites(PlayerBase player)
    {
        AttackSprite.sprite = AttackCD.sprite = player.AttackSprite;
        SkillSprite.sprite = SkillCD.sprite = player.SkillSprite;
        SpecialSprite.sprite = SpecialCD.sprite = player.SpecialSprite;
    }

    public void Register(PlayerBase player)
    {
        if (!this.player)
        {
            this.player = player;
            virtualCamera.Follow = player.transform;
            IsStageStarted = true;
            SwapCooldownOnStart();
            return;
        }

        StartCoroutine(AssignSwappedPlayerAttributes(player));
    }

    IEnumerator AssignSwappedPlayerAttributes(PlayerBase newPlayer)
    {
        AssignPlayerSkillSprites(newPlayer);

        short percentageHealth = player.GetHealthPercentage();
        newPlayer.SetHealth(Mathf.Max(1, newPlayer.GetMaxHealth() *  percentageHealth / 100));
        
        EntityManager.SpriteRenderers.Remove(player.GetSpriteRenderer());
        EntityManager.Enemies.ForEach(e =>
        {
            e.ChangeAggro(newPlayer);  
        });

        if (player.GetSpriteRenderer().flipX) 
        {
            newPlayer.GetSpriteRenderer().flipX = true;
            newPlayer.FlipAttackPosition();
        }

        yield return new WaitForEndOfFrame();
        Destroy(player.gameObject);
        player = newPlayer; 
        virtualCamera.Follow = player.transform;
    }

    public IEnumerator AttackCooldown(float duration, float init = 0)
    {
        if (AttackCooldownCoroutine != null) StopCoroutine(AttackCooldownCoroutine);
        AttackCooldownCoroutine = StartCoroutine(Cooldown(AttackCD, duration, init));
        yield return AttackCooldownCoroutine;
    }
    public IEnumerator SkillCooldown(float duration, float init = 0)
    {
        if (SkillCooldownCoroutine != null) StopCoroutine(SkillCooldownCoroutine);
        SkillCooldownCoroutine = StartCoroutine(Cooldown(SkillCD, duration, init));
        yield return SkillCooldownCoroutine;
    }

    public IEnumerator SpecialCooldown(float duration, float init = 0)
    {
        if (SpecialCooldownCoroutine != null) StopCoroutine(SpecialCooldownCoroutine);
        SpecialCooldownCoroutine = StartCoroutine(Cooldown(SpecialCD, duration, init));
        yield return SpecialCooldownCoroutine;
    }

    IEnumerator RotateSwapSymbol(float duration)
    {
        float elapsed = 0f;
        Quaternion startRotation = Quaternion.Euler(0, 0, swapCurrentRotation);
        Quaternion endRotation = startRotation * Quaternion.Euler(0, 0, 180);
        swapCurrentRotation += 180;

        while (elapsed < duration)
        {
            Swapsymbol.transform.rotation = Quaternion.Lerp(startRotation, endRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Swapsymbol.transform.rotation = endRotation;
    }

    public IEnumerator SwapCooldownE(float duration, float init = 0, bool rotateSymbol = true)
    {
        float waitDuration = duration - init;
        StartCoroutine(Cooldown(SwapCD, duration, init));

        float minusTime = 0f;
        if (rotateSymbol)
        {
            minusTime = 0.35f;
            yield return StartCoroutine(RotateSwapSymbol(0.35f));
        }
        Swapsymbol.color = new Color(1, 1, 1, 0.25f);
        yield return new WaitForSeconds(waitDuration - minusTime);
        Swapsymbol.color = Color.white;
    }

    public IEnumerator Cooldown(Image CD, float duration, float init = 0)
    {
        TMP_Text Count = CD.GetComponentInChildren<TMP_Text>();
        float c = init;
        while (c < duration)
        {
            CD.fillAmount = Mathf.Lerp(1, 0, c * 1.0f / duration);
            Count.text = Math.Round(duration - c, 1) + "s";
            c += Time.deltaTime;
            yield return null;
        }

        Count.text = "";
        CD.fillAmount = 0;
    }

    public void ResetAllCooldown()
    {
        if (AttackCooldownCoroutine != null) StopCoroutine(AttackCooldownCoroutine);
        if (SkillCooldownCoroutine != null) StopCoroutine(SkillCooldownCoroutine);
        if (SpecialCooldownCoroutine != null) StopCoroutine(SpecialCooldownCoroutine);
        CDs.ForEach(cd =>
        {
            cd.fillAmount = 0;
            TMP_Text Count = cd.GetComponentInChildren<TMP_Text>();
            Count.text = "";
        });
    }

    IEnumerator FadeOut(GameObject o, float duration)
    {
        SpriteRenderer renderer = o.GetComponentInChildren<SpriteRenderer>();
        Color startColor = renderer.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            renderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        renderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        Destroy(o);
    }

    public DamageInstance OnPlayerDamageTarget(PlayerBase player, DamageInstance dmg, EntityBase target)
    {
        if (!player || !player.IsAlive()) return dmg;
        return dmg;
    }

    public void OnPlayerAttacked(float strength)
    {
        if (strength >= 0.8f) hit_02_sfx.Play();
        else hit_01_sfx.Play();

        mainCamera.StartCoroutine(mainCamera.Shake(strength));
    }

    public void OnPlayerDeath()
    {
        IsPlayerAlive = false;
        StopAllCoroutines();

        foreach (var item in Disables)
        {
            item.SetActive(true);
        } 

        CDs.ForEach(cd =>
        {
            cd.fillAmount = 1;
            TMP_Text Count = cd.GetComponentInChildren<TMP_Text>();
            Count.text = "";
        });

        Swapsymbol.color = new Color(1, 1, 1, 0.25f);
    }

    public void ViewSkill()
    {
        if (skillViewCoroutine != null) StopCoroutine(skillViewCoroutine);

        if (SkillView_Overlay.activeSelf)
        {
            skillViewCoroutine = StartCoroutine(HideSkillView());
        }
        else
        {
            SetSkillViewAttributes();
            SkillView_Overlay.SetActive(true);
            skillViewCoroutine = StartCoroutine(ShowSkillView());
        }
    }

    IEnumerator ShowSkillView()
    {
        float InitY = SkillView.transform.localPosition.y, TargetY = 170;
        float c = 0, d = 0.35f, cJump = 0.02f;
        while (c < d)
        {
            SkillView.transform.localPosition = new Vector3(SkillView.transform.localPosition.x, Mathf.Lerp(InitY, TargetY, c * 1.0f / d), SkillView.transform.localPosition.z);
            c += cJump;
            yield return new WaitForSecondsRealtime(cJump);
        }

        SkillView.transform.localPosition = new Vector3(SkillView.transform.localPosition.x, TargetY, SkillView.transform.localPosition.z);
    }

    IEnumerator HideSkillView()
    {
        float InitY = SkillView.transform.localPosition.y, TargetY = -500;
        float c = 0, d = 0.35f, cJump = 0.02f;
        while (c < d)
        {
            SkillView.transform.localPosition = new Vector3(SkillView.transform.localPosition.x, Mathf.Lerp(InitY, TargetY, c * 1.0f / d), SkillView.transform.localPosition.z);
            c += cJump;
            yield return new WaitForSecondsRealtime(cJump);
        }

        SkillView.transform.localPosition = new Vector3(SkillView.transform.localPosition.x, TargetY, SkillView.transform.localPosition.z);
        SkillView_Overlay.SetActive(false);
    }

    private void SetSkillViewAttributes()
    {
        PlayerTooltipsInfo info = player.GetPlayerTooltipsInfo();

        PlayerIcon.sprite = info.Icon;

        SkillView_Attributes.text =
            (info.attackPattern == AttackPattern.MELEE ? $"<color=yellow>{info.attackPattern}</color>" : $"<color=blue>{info.attackPattern}</color>") 
            + ", " 
            + (info.damageType == DamageType.MAGICAL ? $"<color=#800080>{info.damageType}</color>" : $"<color=#9C2007>{info.damageType}</color>") + "\n\n" +
            $"<color=green>HP: {info.health} / {info.mHealth} ({info.health * 100 / info.mHealth}%)</color>\n\n" +
            $"<color=#9C2007>ATK: {info.atk} ({info.bAtk} + {info.atk - info.bAtk})</color>\n\n" +
            $"<color=#800000>ASPD: {info.ASPD}</color>\n\n" +
            $"<color=yellow>DEF: {info.def} ({info.bDef} + {info.def - info.bDef})</color>\n\n" +
            $"<color=#00ffff>RES: {info.res} ({info.bRes} + {info.res - info.bRes})</color>\n\n" +
            $"<color=black>MSPD: {info.MSPD}</color>";

        SkillView_Attack.sprite = info.AttackSprite;
        SkillView_Skill.sprite = info.SkillSprite;
        SkillView_Special.sprite = info.SpecialSprite;
        SkillView_AttackText.text = info.AttackText;
        SkillView_SkillName.text = info.SkillName;
        SkillView_SkillText.text = info.SkillText;
        SkillView_SpecialName.text = info.SpecialName;
        SkillView_SpecialText.text = info.SpecialText;

        // techs
        int techCount = CharacterPrefabsStorage.Skills.Count;
        bool hasTech = techCount > 0;

        EmptyTechViewsText.text = hasTech
            ?
            "RESEARCHES:"
            :
            "";
        
        for (int i = 0; i < TechViews.Length; i++)
        {
            GameObject techView = TechViews[i];
            bool hasTechThisLoop = (i + 1) <= techCount;
            if (!hasTechThisLoop) continue;
            
            SkillDataSet skillDataSet = CharacterPrefabsStorage.Skills.ElementAt(i).Value;
            Image skillIcon = techView.GetComponentInChildren<Image>();
            TMP_Text[] txts = techView.GetComponentsInChildren<TMP_Text>();

            skillIcon.sprite = skillDataSet.skillIcon;
            txts[0].text = skillDataSet.nameInString;
            txts[1].text = skillDataSet.skillDescription;
        }
    }

    public void SwapView()
    {
        if (!SkillView_Overlay.activeSelf) return;
        bool activeInfoView = !PlayerInfoSect.activeSelf;
        PlayerInfoSect.SetActive(activeInfoView);
        TechInfoSect.SetActive(!activeInfoView);
    }

    public void ClearStageBGM(float duration)
    {
        StartCoroutine(ClearBGMCouroutine(duration));
    }

    IEnumerator ClearBGMCouroutine(float duration)
    {
        stageManager.StageBGM.Pause();
        yield return new WaitForSeconds(duration);
        stageManager.StageBGM.Play();
    }
}

public class PlayerTooltipsInfo
{
    public Sprite Icon { get; set; }
    public Sprite AttackSprite { get; set; }
    public Sprite SkillSprite { get; set; }
    public Sprite SpecialSprite { get; set; }
    public string AttackText { get; set; }
    public string SkillName { get; set; }
    public string SkillText { get; set; }
    public string SpecialName { get; set; }
    public string SpecialText { get; set; }
    public int mHealth { get; set; }
    public int health { get; set; }
    public short bDef { get; set; }
    public short def { get; set; }
    public short bAtk { get; set; }
    public short atk { get; set; }
    public short bRes { get; set; }
    public short res { get; set; }
    public float moveSpeed { get; set; }
    public AttackPattern attackPattern { get; set; }
    public DamageType damageType { get; set; }
    public float attackSpeed { get; set; }
    public float attackRange { get; set; }
    public float attackInterval { get; set; }

    public string ASPD
    {
        get
        {
            if (attackSpeed <= 0.15f) return "VERY FAST";
            if (attackSpeed <= 0.3f) return "FAST";
            if (attackSpeed <= 0.6f) return "NORMAL";
            if (attackSpeed <= 1.1f) return "SLOW";
            return "VERY SLOW";
        }
    }

    public string MSPD
    {
        get
        {
            if (moveSpeed <= 60f) return "VERY SLOW";
            if (moveSpeed <= 100f) return "SLOW";
            if (moveSpeed <= 160f) return "NORMAL";
            if (moveSpeed <= 240f) return "FAST";
            return "VERY FAST";
        }
    }
}