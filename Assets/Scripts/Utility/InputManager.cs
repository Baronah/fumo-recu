using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    [SerializeField] private MovementScheme currentScheme = MovementScheme.ArrowKeys;

    // Key properties - these will automatically get updated values from PlayerPrefs
    public KeyCode AttackKey => KeyCodeConverter.ParseFromPlayerPrefs("AttackKey", KeyCode.Z);
    public KeyCode SpecialKey => KeyCodeConverter.ParseFromPlayerPrefs("SpecialKey", KeyCode.X);
    public KeyCode SkillKey => KeyCodeConverter.ParseFromPlayerPrefs("SkillKey", KeyCode.A);
    public KeyCode PlayerSwapKey => KeyCodeConverter.ParseFromPlayerPrefs("PlayerSwapKey", KeyCode.Space);
    public KeyCode SlowKey => KeyCodeConverter.ParseFromPlayerPrefs("SlowKey", KeyCode.Q);
    public KeyCode ViewInfoKey => KeyCodeConverter.ParseFromPlayerPrefs("ViewInfoKey", KeyCode.V);
    public KeyCode SwapInfoKey => KeyCodeConverter.ParseFromPlayerPrefs("SwapInfoKey", KeyCode.B);
    public KeyCode ViewMapKey => KeyCodeConverter.ParseFromPlayerPrefs("ViewMapKey", KeyCode.M);

    // Input scheme enumeration
    public enum MovementScheme
    {
        ArrowKeys,
        WASD
    }

    public MovementScheme CurrentScheme
    {
        get => GetCurrentScheme();
        set => currentScheme = value;
    }

    public MovementScheme GetCurrentScheme()
    {
        if (PlayerPrefs.HasKey("MovementScheme"))
        {
            currentScheme = (MovementScheme)PlayerPrefs.GetInt("MovementScheme");
        }

        return currentScheme;
    }

    public enum KeyAction
    {         
        Attack,
        Special,
        Skill,
        PlayerSwap,
        Slow,
        ViewInfo,
        SwapInfo,
        ViewMap
    }

    #region Movement Input Methods
    public Vector2 GetMovementInput()
    {
        Vector2 input = Vector2.zero;
        switch (currentScheme)
        {
            case MovementScheme.ArrowKeys:
                input.x = GetArrowHorizontal();
                input.y = GetArrowVertical();
                break;
            case MovementScheme.WASD:
                input.x = GetWASDHorizontal();
                input.y = GetWASDVertical();
                break;
        }
        return input.normalized;
    }

    private float GetArrowHorizontal()
    {
        float horizontal = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
        return horizontal;
    }

    private float GetArrowVertical()
    {
        float vertical = 0f;
        if (Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
        if (Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
        return vertical;
    }

    private float GetWASDHorizontal()
    {
        float horizontal = 0f;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.D)) horizontal += 1f;
        return horizontal;
    }

    private float GetWASDVertical()
    {
        float vertical = 0f;
        if (Input.GetKey(KeyCode.S)) vertical -= 1f;
        if (Input.GetKey(KeyCode.W)) vertical += 1f;
        return vertical;
    }

    // Method to change control scheme
    public void SetMovementScheme(MovementScheme scheme)
    {
        currentScheme = scheme;
        // Save to PlayerPrefs for persistence
        PlayerPrefs.SetInt("MovementScheme", (int)scheme);
        PlayerPrefs.Save();
    }
    #endregion

    #region Keybind Setting Methods

    /// <summary>
    /// Set the attack key binding
    /// </summary>
    public void SetAttackKey(KeyCode newKey)
    {
        PlayerPrefs.SetString("AttackKey", newKey.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Set the special key binding
    /// </summary>
    public void SetSpecialKey(KeyCode newKey)
    {
        PlayerPrefs.SetString("SpecialKey", newKey.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Set the skill key binding
    /// </summary>
    public void SetSkillKey(KeyCode newKey)
    {
        PlayerPrefs.SetString("SkillKey", newKey.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Set the player swap key binding
    /// </summary>
    public void SetPlayerSwapKey(KeyCode newKey)
    {
        PlayerPrefs.SetString("PlayerSwapKey", newKey.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Set the slow key binding
    /// </summary>
    public void SetSlowKey(KeyCode newKey)
    {
        PlayerPrefs.SetString("SlowKey", newKey.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Set the view info key binding
    /// </summary>
    public void SetViewInfoKey(KeyCode newKey)
    {
        PlayerPrefs.SetString("ViewInfoKey", newKey.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Set the swap info key binding
    /// </summary>
    public void SetSwapInfoKey(KeyCode newKey)
    {
        PlayerPrefs.SetString("SwapInfoKey", newKey.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Set the view map key binding
    /// </summary>
    public void SetViewMapKey(KeyCode newKey)
    {
        PlayerPrefs.SetString("ViewMapKey", newKey.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Generic method to set any key binding
    /// </summary>
    public void SetKeyBinding(string keyName, KeyCode newKey)
    {
        PlayerPrefs.SetString(keyName, newKey.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Check if a key is already bound to another action
    /// </summary>
    public bool IsKeyAlreadyBound(KeyCode keyToCheck, string excludeKeyName = "")
    {
        string[] keyNames = { "AttackKey", "SpecialKey", "SkillKey", "PlayerSwapKey",
                             "SlowKey", "ViewInfoKey", "SwapInfoKey", "ViewMapKey" };

        foreach (string keyName in keyNames)
        {
            if (keyName == excludeKeyName) continue; // Skip the key we're currently setting

            KeyCode boundKey = KeyCodeConverter.ParseFromPlayerPrefs(keyName, KeyCode.None);
            if (boundKey == keyToCheck)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get the name of the action that a key is bound to
    /// </summary>
    public string GetActionNameForKey(KeyCode keyToCheck)
    {
        if (AttackKey == keyToCheck) return "Attack";
        if (SpecialKey == keyToCheck) return "Special";
        if (SkillKey == keyToCheck) return "Skill";
        if (PlayerSwapKey == keyToCheck) return "Player Swap";
        if (SlowKey == keyToCheck) return "Slow";
        if (ViewInfoKey == keyToCheck) return "View Info";
        if (SwapInfoKey == keyToCheck) return "Swap Info";
        if (ViewMapKey == keyToCheck) return "View Map";

        return "None";
    }
    public void SetKeyAction(KeyAction action, KeyCode newKey)
    {
        switch (action)
        {
            case KeyAction.Attack:
                SetAttackKey(newKey);
                break;
            case KeyAction.Special:
                SetSpecialKey(newKey);
                break;
            case KeyAction.Skill:
                SetSkillKey(newKey);
                break;
            case KeyAction.PlayerSwap:
                SetPlayerSwapKey(newKey);
                break;
            case KeyAction.Slow:
                SetSlowKey(newKey);
                break;
            case KeyAction.ViewInfo:
                SetViewInfoKey(newKey);
                break;
            case KeyAction.SwapInfo:
                SetSwapInfoKey(newKey);
                break;
            case KeyAction.ViewMap:
                SetViewMapKey(newKey);
                break;
            default:
                Debug.LogWarning($"Unknown KeyAction: {action}");
                break;
        }
    }

    /// <summary>
    /// Get the current KeyCode for a specific action
    /// </summary>
    public KeyCode GetKeyForAction(KeyAction action)
    {
        switch (action)
        {
            case KeyAction.Attack:
                return AttackKey;
            case KeyAction.Special:
                return SpecialKey;
            case KeyAction.Skill:
                return SkillKey;
            case KeyAction.PlayerSwap:
                return PlayerSwapKey;
            case KeyAction.Slow:
                return SlowKey;
            case KeyAction.ViewInfo:
                return ViewInfoKey;
            case KeyAction.SwapInfo:
                return SwapInfoKey;
            case KeyAction.ViewMap:
                return ViewMapKey;
            default:
                Debug.LogWarning($"Unknown KeyAction: {action}");
                return KeyCode.None;
        }
    }

    /// <summary>
    /// Reset all keybinds to default values
    /// </summary>
    public void ResetAllKeybindsToDefault()
    {
        PlayerPrefs.SetString("AttackKey", KeyCode.Z.ToString());
        PlayerPrefs.SetString("SpecialKey", KeyCode.X.ToString());
        PlayerPrefs.SetString("SkillKey", KeyCode.A.ToString());
        PlayerPrefs.SetString("PlayerSwapKey", KeyCode.Space.ToString());
        PlayerPrefs.SetString("SlowKey", KeyCode.Q.ToString());
        PlayerPrefs.SetString("ViewInfoKey", KeyCode.V.ToString());
        PlayerPrefs.SetString("SwapInfoKey", KeyCode.B.ToString());
        PlayerPrefs.SetString("ViewMapKey", KeyCode.M.ToString());
        PlayerPrefs.Save();
    }

    #endregion
}

public class KeyCodeConverter
{
    public static KeyCode ParseFromPlayerPrefs(string key, KeyCode defaultKey)
    {
        if (!PlayerPrefs.HasKey(key)) PlayerPrefs.SetString(key, defaultKey.ToString());
        return (KeyCode)Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString(key, defaultKey.ToString()), true);
    }
}