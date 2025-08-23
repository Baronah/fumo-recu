using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapZoomoutController : MonoBehaviour
{
    private CinemachineVirtualCamera vcam;

    private Vector3 InitPosition;
    private float InitSize;

    [SerializeField] private float MaxSpeed = 2000f;
    [SerializeField] private float ZoomStep = 50f;
    [SerializeField] private float MinSize = 500f;

    private float GetPerferredSpeed => MaxSpeed * (vcam.m_Lens.OrthographicSize / InitSize);

    void Start()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        InitPosition = transform.position;
        InitSize = vcam.m_Lens.OrthographicSize;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) 
                ZoomCamera();
        else MoveCamera();
    }

    void MoveCamera()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"), 
            vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, vertical, 0).normalized;
        transform.position += direction * GetPerferredSpeed * Time.unscaledDeltaTime;
    }

    void ZoomCamera()
    {
        float Vertical = Input.GetAxisRaw("Vertical");

        vcam.m_Lens.OrthographicSize = Mathf.Clamp(vcam.m_Lens.OrthographicSize + Vertical * -1 * ZoomStep, MinSize, InitSize);
    }

    public void SliderSizeChange()
    {
        float size = GameObject.Find("Slider").GetComponent<UnityEngine.UI.Slider>().value;
        vcam.m_Lens.OrthographicSize = size;
    }

    private void OnDisable()
    {
        vcam.m_Lens.OrthographicSize = InitSize;
        transform.position = InitPosition;
    }
}
