using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class KeybindManager : MonoBehaviour
{
    [Header("Keybind Buttons")]
    [SerializeField] private KeybindButton attackButton;
    [SerializeField] private KeybindButton specialButton;
    [SerializeField] private KeybindButton skillButton;
    [SerializeField] private KeybindButton playerSwapButton;
    [SerializeField] private KeybindButton slowButton;
    [SerializeField] private KeybindButton viewInfoButton;
    [SerializeField] private KeybindButton swapInfoButton;
    [SerializeField] private KeybindButton viewMapButton;

    [Header("Control Buttons")]
    [SerializeField] private Button resetAllButton;
    [SerializeField] private Button resetDefaultsButton;
    [SerializeField] private Button closeButton;

    [Header("Movement Scheme")]
    [SerializeField] private Button movementSchemeButton;
    [SerializeField] private TMP_Text movementSchemeButtonText;

    private List<KeybindButton> allKeybindButtons;
    public bool isAnyButtonListening => IsAnyButtonListening();

    private void Start()
    {
        SetupKeybindButtons();
        SetupControlButtons();
        LoadCurrentSettings();
    }

    /// <summary>
    /// Set up all keybind buttons with their respective actions
    /// </summary>
    private void SetupKeybindButtons()
    {
        // Initialize the list
        allKeybindButtons = new List<KeybindButton>();

        // Set up each button with its corresponding action
        if (attackButton != null)
        {
            attackButton.SetKeyAction(InputManager.KeyAction.Attack);
            allKeybindButtons.Add(attackButton);
        }

        if (specialButton != null)
        {
            specialButton.SetKeyAction(InputManager.KeyAction.Special);
            allKeybindButtons.Add(specialButton);
        }

        if (skillButton != null)
        {
            skillButton.SetKeyAction(InputManager.KeyAction.Skill);
            allKeybindButtons.Add(skillButton);
        }

        if (playerSwapButton != null)
        {
            playerSwapButton.SetKeyAction(InputManager.KeyAction.PlayerSwap);
            allKeybindButtons.Add(playerSwapButton);
        }

        if (slowButton != null)
        {
            slowButton.SetKeyAction(InputManager.KeyAction.Slow);
            allKeybindButtons.Add(slowButton);
        }

        if (viewInfoButton != null)
        {
            viewInfoButton.SetKeyAction(InputManager.KeyAction.ViewInfo);
            allKeybindButtons.Add(viewInfoButton);
        }

        if (swapInfoButton != null)
        {
            swapInfoButton.SetKeyAction(InputManager.KeyAction.SwapInfo);
            allKeybindButtons.Add(swapInfoButton);
        }

        if (viewMapButton != null)
        {
            viewMapButton.SetKeyAction(InputManager.KeyAction.ViewMap);
            allKeybindButtons.Add(viewMapButton);
        }

        movementSchemeButtonText = movementSchemeButton.GetComponentInChildren<TMP_Text>();
    }

    /// <summary>
    /// Set up control buttons (Reset, Close, etc.)
    /// </summary>
    private void SetupControlButtons()
    {
        if (resetAllButton != null)
            resetAllButton.onClick.AddListener(ResetAllKeybinds);

        if (resetDefaultsButton != null)
            resetDefaultsButton.onClick.AddListener(ResetAllToDefaults);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseSettings);
    }

    /// <summary>
    /// Load current settings and update UI
    /// </summary>
    private void LoadCurrentSettings()
    {
        UpdateMovementSchemeButtonText();
        foreach (var button in allKeybindButtons)
        {
            if (button != null)
                button.UpdateButtonText();
        }
    }

    /// <summary>
    /// Reset all keybinds (stop any listening)
    /// </summary>
    public void ResetAllKeybinds()
    {
        foreach (var button in allKeybindButtons)
        {
            if (button != null)
                button.StopListeningForKey();
        }
    }

    /// <summary>
    /// Reset all keybinds to their default values
    /// </summary>
    public void ResetAllToDefaults()
    {
        // Stop any listening first
        ResetAllKeybinds();

        // Reset all keys to defaults
        InputManager.Instance.ResetAllKeybindsToDefault();

        // Reset movement scheme to default
        InputManager.Instance.SetMovementScheme(InputManager.MovementScheme.ArrowKeys);

        // Reload UI
        LoadCurrentSettings();

        Debug.Log("All keybinds reset to defaults");
    }

    /// <summary>
    /// Close the settings panel
    /// </summary>
    public void CloseSettings()
    {
        // Stop any listening buttons
        ResetAllKeybinds();

        // Hide the panel (adjust this based on your UI structure)
        gameObject.SetActive(false);

        // Or if this is a separate scene, you might want to:
        // SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Stop all buttons from listening (useful when opening other UI)
    /// </summary>
    public void StopAllListening()
    {
        foreach (var button in allKeybindButtons)
        {
            if (button != null)
                button.StopListeningForKey();
        }
    }

    /// <summary>
    /// Check if any button is currently listening for input
    /// </summary>
    public bool IsAnyButtonListening()
    {
        foreach (var button in allKeybindButtons)
        {
            if (button != null && button.GetComponent<KeybindButton>().isListening)
                return true;
        }
        return false;
    }

    #region Movement Scheme Methods

    /// <summary>
    /// Toggle between Arrow Keys and WASD (for single button)
    /// </summary>
    public void ToggleMovementScheme()
    {
        var currentScheme = InputManager.Instance.CurrentScheme;
        var newScheme = currentScheme == InputManager.MovementScheme.ArrowKeys
            ? InputManager.MovementScheme.WASD
            : InputManager.MovementScheme.ArrowKeys;

        InputManager.Instance.SetMovementScheme(newScheme);
        UpdateMovementSchemeButtonText();

        Debug.Log($"Movement scheme toggled to: {newScheme}");
    }

    /// <summary>
    /// Update the movement scheme button text to show current scheme
    /// </summary>
    private void UpdateMovementSchemeButtonText()
    {
        if (movementSchemeButtonText != null)
        {
            var currentScheme = InputManager.Instance.CurrentScheme;
            string buttonText = currentScheme == InputManager.MovementScheme.ArrowKeys
                ? "Arrow Keys"
                : "WASD";

            movementSchemeButtonText.text = buttonText;
        }
    }

    #endregion
}