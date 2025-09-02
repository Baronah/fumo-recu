using UnityEngine;
using UnityEngine.UI;

public class LineController : MonoBehaviour
{ 
    private Image lineImage;
    private Transform[] points;
    [SerializeField] private float lineWidth = 10f;

    private void Awake()
    {
        lineImage = GetComponent<Image>();
        points = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            points[i] = transform.GetChild(i);
        }
    }

    private void Update()
    {
        if (points.Length < 2) return;
        Vector3 startPoint = points[0].position;
        Vector3 endPoint = points[1].position;
        // Calculate the direction and distance between the two points
        Vector3 direction = endPoint - startPoint;
        float distance = direction.magnitude;
        // Set the position to the midpoint between the two points
        transform.position = startPoint + direction / 2;
        // Set the size of the line
        RectTransform rectTransform = lineImage.rectTransform;
        rectTransform.sizeDelta = new Vector2(distance, lineWidth);
        // Calculate the angle and set the rotation
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.rotation = Quaternion.Euler(0, 0, angle);
    }
}