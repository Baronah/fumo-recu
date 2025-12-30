using UnityEngine;
using UnityEngine.UI;

public class DVDLogo : MonoBehaviour
{
    public float moveSpeed = 200f;
    private Vector2 direction = new Vector2(1f, 1f);
    private RectTransform rectTransform;
    [SerializeField] private Canvas canvas;
    private CanvasScaler canvasScaler;
    private Image image;

    private Vector2 canvasSize;
    private Vector2 imageSize;

    // For World Space canvas calculations
    private bool isWorldSpace = false;
    private RectTransform canvasRect;

    private Vector2 initialPosition;
    private Color initColor;

    private void Start()
    {
        image = GetComponent<Image>();
        if (!rectTransform) rectTransform = GetComponent<RectTransform>();

        initialPosition = transform.position;
        initColor = image.color;

        // Find the canvas this UI element belongs to
        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
            canvasScaler = canvas.GetComponent<CanvasScaler>();
            isWorldSpace = canvas.renderMode == RenderMode.WorldSpace;

            // Get canvas size based on render mode
            GetCanvasSize();
        }
        else
        {
            Debug.LogError("DVDLogo script requires the object to be a child of a Canvas!");
        }
    }

    private void GetCanvasSize()
    {
        if (!rectTransform) rectTransform = GetComponent<RectTransform>();

        if (isWorldSpace)
        {
            // For World Space, use the actual rect size
            canvasSize = canvasRect.rect.size;
        }
        else
        {
            // For Screen Space, use the reference resolution or screen size
            if (canvasScaler != null && canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                canvasSize = canvasScaler.referenceResolution;
            }
            else
            {
                // For Screen Space - Camera or Overlay
                canvasSize = new Vector2(canvas.pixelRect.width, canvas.pixelRect.height);
            }
        }

        // Get image size (accounting for scale)
        imageSize = rectTransform.rect.size;
        imageSize.x *= rectTransform.localScale.x;
        imageSize.y *= rectTransform.localScale.y;
    }

    float waitTime = 0f;
    void Update()
    {
        if (canvasRect == null) return;

        waitTime += Time.deltaTime;
        if (waitTime < 20.5f) return;

        // Move based on canvas render mode
        if (isWorldSpace)
        {
            // For World Space, use localPosition
            Vector3 newPos = rectTransform.localPosition +
                           new Vector3(direction.x, direction.y, 0) * moveSpeed * Time.deltaTime;
            rectTransform.localPosition = newPos;

            CheckCollisionsWorldSpace();
        }
        else
        {
            // For Screen Space, use anchoredPosition
            rectTransform.anchoredPosition += direction * moveSpeed * Time.deltaTime;
            CheckCollisionsScreenSpace();
        }
    }

    public void OnDisable()
    {
        transform.position = initialPosition;
        image.color = initColor;
    }

    private void CheckCollisionsScreenSpace()
    {
        Vector2 currentPos = rectTransform.anchoredPosition;

        // Calculate boundaries for screen space
        float leftBound = -canvasSize.x / 2 + imageSize.x / 2;
        float rightBound = canvasSize.x / 2 - imageSize.x / 2;
        float bottomBound = -canvasSize.y / 2 + imageSize.y / 2;
        float topBound = canvasSize.y / 2 - imageSize.y / 2;

        // Check for horizontal collisions
        if (currentPos.x >= rightBound || currentPos.x <= leftBound)
        {
            image.color = new Color(Random.value, Random.value, Random.value);
            direction.x *= -1;
            currentPos.x = Mathf.Clamp(currentPos.x, leftBound, rightBound);
            rectTransform.anchoredPosition = currentPos;
        }

        // Check for vertical collisions
        if (currentPos.y >= topBound || currentPos.y <= bottomBound)
        {
            image.color = new Color(Random.value, Random.value, Random.value);
            direction.y *= -1;
            currentPos.y = Mathf.Clamp(currentPos.y, bottomBound, topBound);
            rectTransform.anchoredPosition = currentPos;
        }
    }

    private void CheckCollisionsWorldSpace()
    {
        Vector3 currentPos = rectTransform.localPosition;

        // Calculate boundaries for world space
        float leftBound = -canvasSize.x / 2 + imageSize.x / 2;
        float rightBound = canvasSize.x / 2 - imageSize.x / 2;
        float bottomBound = -canvasSize.y / 2 + imageSize.y / 2;
        float topBound = canvasSize.y / 2 - imageSize.y / 2;

        // Check for horizontal collisions
        if (currentPos.x >= rightBound || currentPos.x <= leftBound)
        {
            image.color = new Color(Random.value, Random.value, Random.value);
            direction.x *= -1;
            currentPos.x = Mathf.Clamp(currentPos.x, leftBound, rightBound);
            rectTransform.localPosition = currentPos;
        }

        // Check for vertical collisions
        if (currentPos.y >= topBound || currentPos.y <= bottomBound)
        {
            image.color = new Color(Random.value, Random.value, Random.value);
            direction.y *= -1;
            currentPos.y = Mathf.Clamp(currentPos.y, bottomBound, topBound);
            rectTransform.localPosition = currentPos;
        }
    }

    // Update canvas size if it changes (for responsive UI)
    private void OnRectTransformDimensionsChange()
    {
        if (canvas != null)
        {
            GetCanvasSize();
        }
    }
}