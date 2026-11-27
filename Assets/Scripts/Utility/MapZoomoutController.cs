using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class MapZoomoutController : MonoBehaviour
{
    private CinemachineVirtualCamera vcam;

    [SerializeField] private float MaxSpeed = 2000f;
    [SerializeField] private float ZoomStep = 50f;
    [SerializeField] private float MinSize = 500f;

    protected float MaxSize => CameraMovement._instance.showcaseSize;
    private float GetPerferredSpeed => MaxSpeed * (vcam.m_Lens.OrthographicSize / MaxSize);

    void Awake()
    {
        if (!vcam) vcam = GetComponent<CinemachineVirtualCamera>();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) 
                ZoomCamera();
        else MoveCamera();
    }

    void MoveCamera()
    {
        if (!InputManager.Instance) return;
        Vector2 movement = InputManager.Instance.GetMovementInput();
        Vector3 direction = movement.normalized;
        Vector3 shift = direction * GetPerferredSpeed;
        transform.position += shift * Time.unscaledDeltaTime;
    }

    void ZoomCamera()
    {
        float Vertical = InputManager.Instance.GetMovementVertical();

        vcam.m_Lens.OrthographicSize = Mathf.Clamp(
            vcam.m_Lens.OrthographicSize + Vertical * -1 * ZoomStep, 
            MinSize, 
            MaxSize);
    }

    public void SliderSizeChange()
    {
        float size = GameObject.Find("Slider").GetComponent<UnityEngine.UI.Slider>().value;
        vcam.m_Lens.OrthographicSize = size;
    }

    private void OnEnable()
    {
        vcam.m_Lens.OrthographicSize = MinSize + 300f;
        transform.position = PlayerManager._instance.activePlayer.transform.position;
    }
}
