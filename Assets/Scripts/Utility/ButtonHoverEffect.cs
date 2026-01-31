using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color InitColor, HoverColor, DisableColor;

    Color currentColor;
    private Button thisButton;
    private Image[] Images;

    private void Start()
    {
        thisButton = GetComponent<Button>();
        Images = GetComponentsInChildren<Image>();
        currentColor = InitColor;
    }

    private void Update()
    {
        if (thisButton.interactable)
        {
            foreach (var image in Images) image.color = currentColor;
        }
        else
        {
            foreach (var image in Images) image.color = DisableColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        currentColor = HoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        currentColor = InitColor;
    }
}
