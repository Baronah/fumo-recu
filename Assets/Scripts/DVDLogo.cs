using UnityEngine;
using UnityEngine.UI;

public class DVDLogo : MonoBehaviour
{
    public float moveSpeed = 200f; // Increased speed for UI elements
    private Vector2 direction = new Vector2(1f, 1f); // Initial direction
    private RectTransform rectTransform;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Image image;

    private void Start()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        // Find the canvas this UI element belongs to
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogError("DVDLogo script requires the object to be a child of a Canvas!");
        }
    }

    void Update()
    {
        if (canvasRect == null) return;

        // Move the object using anchoredPosition for UI elements
        rectTransform.anchoredPosition += direction * moveSpeed * Time.deltaTime;

        // Get canvas boundaries
        Vector2 canvasSize = canvasRect.sizeDelta;
        Vector2 imageSize = rectTransform.sizeDelta;

        // Calculate boundaries considering the image's pivot and size
        float leftBound = -canvasSize.x / 2 + imageSize.x / 2;
        float rightBound = canvasSize.x / 2 - imageSize.x / 2;
        float bottomBound = -canvasSize.y / 2 + imageSize.y / 2;
        float topBound = canvasSize.y / 2 - imageSize.y / 2;

        Vector2 currentPos = rectTransform.anchoredPosition;

        // Check for horizontal collisions
        if (currentPos.x >= rightBound || currentPos.x <= leftBound)
        {
            image.color = new Color(Random.value, Random.value, Random.value);
            direction.x *= -1; // Reverse horizontal direction

            // Clamp position to prevent getting stuck
            currentPos.x = Mathf.Clamp(currentPos.x, leftBound, rightBound);
            rectTransform.anchoredPosition = currentPos;
        }

        // Check for vertical collisions
        if (currentPos.y >= topBound || currentPos.y <= bottomBound)
        {
            image.color = new Color(Random.value, Random.value, Random.value);
            direction.y *= -1; // Reverse vertical direction

            // Clamp position to prevent getting stuck
            currentPos.y = Mathf.Clamp(currentPos.y, bottomBound, topBound);
            rectTransform.anchoredPosition = currentPos;
        }
    }
}