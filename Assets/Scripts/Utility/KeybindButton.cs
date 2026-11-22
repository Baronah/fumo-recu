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
            if (Input.inputString.Length > 0)
            {
                char keyPressed = Input.inputString[0];

                if (keyPressed == '\b' || keyPressed == '\n' || keyPressed == '\r')
                {
                    yield return null;
                    continue;
                }

                KeyCode detectedKey = GetKeyCodeFromChar(keyPressed);
                if (detectedKey != KeyCode.None)
                {
                    AssignKey(detectedKey);
                    yield break;
                }
            }

            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    if (IsMouseButton(key))
                        continue;

                    AssignKey(key);
                    yield break;
                }
            }

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

        return c switch
        {
            ' ' => KeyCode.Space,
            '.' => KeyCode.Period,
            ',' => KeyCode.Comma,
            ';' => KeyCode.Semicolon,
            '\'' => KeyCode.Quote,
            '[' => KeyCode.LeftBracket,
            ']' => KeyCode.RightBracket,
            '-' => KeyCode.Minus,
            '=' => KeyCode.Equals,
            '\\' => KeyCode.Backslash,
            '/' => KeyCode.Slash,
            _ => KeyCode.None,
        };
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
            case KeyCode.LeftShift: return "L.Shift";
            case KeyCode.RightShift: return "R.Shift";
            case KeyCode.LeftControl: return "L.Ctrl";
            case KeyCode.RightControl: return "R.Ctrl";
            case KeyCode.LeftAlt: return "L.Alt";
            case KeyCode.RightAlt: return "R.Alt";
            case KeyCode.Return: return "Enter";
            case KeyCode.Backspace: return "Backspace";
            case KeyCode.Delete: return "DEL";
            case KeyCode.Insert: return "INS";
            case KeyCode.Home: return "Home";
            case KeyCode.End: return "END";
            case KeyCode.PageUp: return "PG UP";
            case KeyCode.PageDown: return "PG DN";
            case KeyCode.CapsLock: return "Caps";
            case KeyCode.Tab: return "Tab";
            case KeyCode.Space: return "Space";
            case KeyCode.Semicolon: return ";";
            case KeyCode.LeftBracket: return "[";
            case KeyCode.RightBracket: return "]";
            case KeyCode.Quote: return "'";
            default:
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