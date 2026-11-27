using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

[Singleton]
public class CameraMovement : MonoBehaviour
{
    public static CameraMovement _instance;
    private void Awake()
    {
        if (!_instance) _instance = this;
        else Destroy(gameObject);
    }

    public float moveTime, shakeDuration = 0.2f, shakeMagnitude = 1f;
    public Vector2 shakeValue;
    public Vector3 offset;

    public float showcaseSize;
    private float size;
    private Camera mainCamera;

    private bool isShaking = false;
    public bool TriggerStopHit => isShaking && shakeStrength >= 0.5f;

    private float shakeStrength = 0;

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        size = mainCamera.orthographicSize;
    }

    private void Update()
    {
        if (shakeTimer < shakeCooldown) shakeTimer += Time.deltaTime;
    }

    public void UpdatePlayerMovement(Transform targetTransform)
    {
        if (targetTransform == null || isShaking) return;

        Vector3 finalPosition = targetTransform.position + offset;
        transform.position = Vector3.Lerp(transform.position, finalPosition, moveTime * Time.unscaledDeltaTime);
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, size, moveTime * Time.unscaledDeltaTime);
    }

    public IEnumerator MoveShowcases(Transform[] points, float[] waittimes)
    {
        if (points.Length <= 0 || waittimes.Length <= 0) yield break;

        yield return new WaitForSeconds(waittimes[0]);

        bool scaleToSize = true;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;
            Vector3 targetPosition = points[i].position + offset;
            float elapsedTime = 0f;
            while (elapsedTime < moveTime)
            {
                if (scaleToSize) mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, showcaseSize, elapsedTime / moveTime);
                transform.position = Vector3.Lerp(transform.position, targetPosition, elapsedTime / moveTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (scaleToSize)
            {
                mainCamera.orthographicSize = showcaseSize;
                scaleToSize = false;
            }
            transform.position = targetPosition;
            yield return new WaitForSeconds(waittimes[i]);
        }

        yield return null;
    }

    readonly float shakeCooldown = 0.65f;
    float shakeTimer = 0f;
    public void CallShakeCoroutine(float percentageScale, float duration)
    {
        if (shakeTimer < shakeCooldown) return;
        if (percentageScale <= shakeStrength) return;

        shakeStrength = percentageScale;
        shakeTimer = 0f;
        shakeStrength = percentageScale;
        StartCoroutine(Shake(percentageScale, duration));
    }

    public IEnumerator Shake(float percentageScale, float duration)
    {
        duration = duration > 0 ? duration : shakeDuration;
        isShaking = true;
        
        Vector3 originalPosition = transform.position;
        float elapsedTime = 0f, dJump = shakeDuration / 10;
        while (elapsedTime < duration)
        {
            float x = Random.Range(-shakeValue.x, shakeValue.x) * shakeMagnitude * percentageScale;
            float y = Random.Range(-shakeValue.y, shakeValue.y) * shakeMagnitude * percentageScale;
            transform.position = new Vector3(originalPosition.x + x, originalPosition.y + y);
            elapsedTime += dJump;
            yield return null;
        }

        isShaking = false;
        shakeStrength = 0;
    }
}
