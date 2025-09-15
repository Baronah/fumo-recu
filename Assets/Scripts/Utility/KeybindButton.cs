using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeybindButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text buttonText;
    private TMP_Text PressAnyKeyTxt;

    [Header("Settings")]
    [SerializeField] private InputManager.KeyAction keyAction;
    [SerializeField] private string currentKeyName = "Unassigned";  
    [SerializeField] private string listeningText = "Press any key...";
    [SerializeField] private string conflictText = "Key already bound!";
    [SerializeField] private float conflictDisplayTime = 2f;

    public bool isListening = false;
    private string originalButtonText;
    private Coroutine listeningCoroutine;

    private KeybindManager keybindManager;

    public void GetComponents()
    {
        // Get references if not assigned
        if (button == null)
            button = GetComponent<Button>();
        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>();

        PressAnyKeyTxt = transform.parent.Find("PressTxt").GetComponent<TMP_Text>();
        PressAnyKeyTxt.text = "";

        // Set up button listener
        button.onClick.AddListener(StartListeningForKey);

        keybindManager = FindObjectOfType<KeybindManager>();
        UpdateButtonText();
    }

    /// <summary>
    /// Start listening for key input
    /// </summary>
    public void StartListeningForKey()
    {
        if (isListening || keybindManager.isAnyButtonListening) return;

        isListening = true;
        originalButtonText = buttonText.text;
        PressAnyKeyTxt.text = listeningText;

        // Change button appearance to show it's listening
        button.interactable = false;

        // Start listening coroutine
        if (listeningCoroutine != null)
            StopCoroutine(listeningCoroutine);
        listeningCoroutine = StartCoroutine(ListenForKeyInput());
    }

    /// <summary>
    /// Stop listening for key input
    /// </summary>
    public void StopListeningForKey()
    {
        if (!isListening) return;

        isListening = false;
        button.interactable = true;
        PressAnyKeyTxt.text = "";

        if (listeningCoroutine != null)
        {
            StopCoroutine(listeningCoroutine);
            listeningCoroutine = null;
        }

        UpdateButtonText();
    }

    /// <summary>
    /// Coroutine that listens for key input
    /// </summary>
    private IEnumerator ListenForKeyInput()
    {
        while (isListening)
        {
            // Check for any key press
            if (Input.inputString.Length > 0)
            {
                // Get the first character pressed
                char keyPressed = Input.inputString[0];

                // Skip control characters and newlines
                if (keyPressed == '\b' || keyPressed == '\n' || keyPressed == '\r')
                {
                    yield return null;
                    continue;
                }

                // Try to convert to KeyCode
                KeyCode detectedKey = GetKeyCodeFromChar(keyPressed);
                if (detectedKey != KeyCode.None)
                {
                    AssignKey(detectedKey);
                    yield break;
                }
            }

            // Check for special keys that don't appear in inputString
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    // Skip mouse buttons for keyboard binding
                    if (IsMouseButton(key))
                        continue;

                    AssignKey(key);
                    yield break;
                }
            }

            // Allow escape to cancel
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                StopListeningForKey();
                yield break;
            }

            yield return null;
        }
    }

    /// <summary>
    /// Assign the detected key to the action
    /// </summary>
    private void AssignKey(KeyCode newKey)
    {
        // Check if key is already bound to another action
        if (InputManager.Instance.IsKeyAlreadyBound(newKey, currentKeyName))
        {
            StartCoroutine(ShowConflictMessage(newKey));
            return;
        }

        // Assign the key
        InputManager.Instance.SetKeyAction(keyAction, newKey);

        // Stop listening and update UI
        StopListeningForKey();

        Debug.Log($"{keyAction} key set to: {newKey}");
    }

    /// <summary>
    /// Show conflict message temporarily
    /// </summary>
    private IEnumerator ShowConflictMessage(KeyCode conflictedKey)
    {
        PressAnyKeyTxt.text = $"'{GetDisplayNameForKey(conflictedKey)}' is already bound!";

        yield return new WaitForSeconds(conflictDisplayTime);

        if (isListening)
        {
            PressAnyKeyTxt.text = listeningText;
        }
        else
        {
            UpdateButtonText();
        }

        StopListeningForKey();
    }

    /// <summary>
    /// Update button text to show current key binding
    /// </summary>
    public void UpdateButtonText()
    {
        if (!buttonText) GetComponents();

        KeyCode currentKey = InputManager.Instance.GetKeyForAction(keyAction);
        buttonText.text = GetDisplayNameForKey(currentKey);
    }

    /// <summary>
    /// Convert character to KeyCode
    /// </summary>
    private KeyCode GetKeyCodeFromChar(char c)
    {
        // Convert to uppercase for consistency
        c = char.ToUpper(c);

        // Handle letters
        if (c >= 'A' && c <= 'Z')
        {
            return (KeyCode)System.Enum.Parse(typeof(KeyCode), c.ToString());
        }

        // Handle numbers
        if (c >= '0' && c <= '9')
        {
            return (KeyCode)System.Enum.Parse(typeof(KeyCode), "Alpha" + c);
        }

        // Handle special characters
        switch (c)
        {
            case ' ': return KeyCode.Space;
            case '.': return KeyCode.Period;
            case ',': return KeyCode.Comma;
            case ';': return KeyCode.Semicolon;
            case '\'': return KeyCode.Quote;
            case '[': return KeyCode.LeftBracket;
            case ']': return KeyCode.RightBracket;
            case '-': return KeyCode.Minus;
            case '=': return KeyCode.Equals;
            case '\\': return KeyCode.Backslash;
            case '/': return KeyCode.Slash;
            default: return KeyCode.None;
        }
    }

    /// <summary>
    /// Get a user-friendly display name for a KeyCode
    /// </summary>
    public static string GetDisplayNameForKey(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.LeftArrow: return "Left Arrow";
            case KeyCode.RightArrow: return "Right Arrow";
            case KeyCode.UpArrow: return "Up Arrow";
            case KeyCode.DownArrow: return "Down Arrow";
            case KeyCode.LeftShift: return "Left Shift";
            case KeyCode.RightShift: return "Right Shift";
            case KeyCode.LeftControl: return "Left Ctrl";
            case KeyCode.RightControl: return "Right Ctrl";
            case KeyCode.LeftAlt: return "Left Alt";
            case KeyCode.RightAlt: return "Right Alt";
            case KeyCode.Return: return "Enter";
            case KeyCode.Backspace: return "Backspace";
            case KeyCode.Delete: return "Delete";
            case KeyCode.Insert: return "Insert";
            case KeyCode.Home: return "Home";
            case KeyCode.End: return "End";
            case KeyCode.PageUp: return "Page Up";
            case KeyCode.PageDown: return "Page Down";
            case KeyCode.CapsLock: return "Caps Lock";
            case KeyCode.Tab: return "Tab";
            case KeyCode.Space: return "Space";
            default:
                // For regular keys, just return the name
                string keyName = key.ToString();
                if (keyName.StartsWith("Alpha"))
                    return keyName.Replace("Alpha", "");
                return keyName;
        }
    }

    /// <summary>
    /// Check if a KeyCode is a mouse button
    /// </summary>
    private bool IsMouseButton(KeyCode key)
    {
        return key == KeyCode.Mouse0 || key == KeyCode.Mouse1 || key == KeyCode.Mouse2 ||
               key == KeyCode.Mouse3 || key == KeyCode.Mouse4 || key == KeyCode.Mouse5 ||
               key == KeyCode.Mouse6;
    }

    /// <summary>
    /// Set the key action this button controls (useful for dynamic setup)
    /// </summary>
    public void SetKeyAction(InputManager.KeyAction action)
    {
        keyAction = action;
        UpdateButtonText();
    }

    /// <summary>
    /// Reset this key binding to default
    /// </summary>
    public void ResetToDefault()
    {
        InputManager.Instance.ResetAllKeybindsToDefault();
        UpdateButtonText();
    }

    private void OnDestroy()
    {
        if (listeningCoroutine != null)
            StopCoroutine(listeningCoroutine);
    }
}