using UnityEngine;
using UnityEngine.EventSystems;

public class EnableOnHover : MonoBehaviour
{
    private GameObject targetObject;
    [SerializeField] private CanvasGroup[] canvasGroups;

    private void Start()
    {
        EventTrigger trigger = gameObject.GetComponent<EventTrigger>();
        EventTrigger.Entry entryPointerEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter,
            callback = new EventTrigger.TriggerEvent(),
        };

        entryPointerEnter.callback.AddListener((data) => { ShowInfo(); });
        trigger.triggers.Add(entryPointerEnter);

        EventTrigger.Entry entryPointerExit = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit,
            callback = new EventTrigger.TriggerEvent(),
        };

        entryPointerExit.callback.AddListener((data) => { HideInfo(); });
        trigger.triggers.Add(entryPointerExit);

        targetObject = transform.GetChild(3).gameObject;
    }

    public void ShowInfo()
    {
        targetObject.SetActive(true);

        if (canvasGroups == null) return;
        
        foreach (var canvasGroup in canvasGroups)
        {
            canvasGroup.alpha = canvasGroup.gameObject == this.gameObject ? 1f : 0.05f;
        }
    }

    public void HideInfo()
    {
        targetObject.SetActive(false);

        if (canvasGroups == null) return;
        
        foreach (var canvasGroup in canvasGroups)
        {
            canvasGroup.alpha = 1f;
        }
    }
}