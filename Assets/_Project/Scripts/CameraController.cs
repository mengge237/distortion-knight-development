using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("缩放设置")]
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 15f;
    [SerializeField] private float zoomSpeed = 2f;

    [Header("旋转设置（转头一下转一下）")]
    [SerializeField] private float rotateSpeed = 2f;
    [SerializeField] private float minAngle = -60f;   // 旋转最小角度
    [SerializeField] private float maxAngle = 60f;    // 旋转最大角度

    [Header("拖拽平移")]
    [SerializeField] private float dragSpeed = 0.5f;

    [Header("平移回归开关")]
    [SerializeField] private bool enableSmoothReturn = true;
    [SerializeField] private float returnSpeed = 3f;

    private Camera cam;
    private Vector3 dragOrigin;
    private float currentAngle = 0f;
    private float targetAngle = 0f;
    private bool isDragging = false;
    private bool isRotating = false;
    private Vector3 lastMousePosition;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    // 保存相机初始的观察高度，用于拖拽平移和透视缩放时保持高度不变
    private float fixedHeight;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        fixedHeight = transform.position.y;

        if (cam.orthographic)
        {
            minZoom = Mathf.Clamp(minZoom, 1f, 50f);
            maxZoom = Mathf.Clamp(maxZoom, 1f, 50f);
        }
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f && cam != null)
        {
            if (cam.orthographic)
            {
                cam.orthographicSize -= scroll * zoomSpeed;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            }
            else
            {
                // 透视相机缩放：沿相机方向前后移动
                Vector3 forward = transform.forward;
                float distance = Vector3.Distance(transform.position, GetLookAtPoint());
                float newDistance = distance - scroll * zoomSpeed;
                newDistance = Mathf.Clamp(newDistance, minZoom, maxZoom);
                transform.position += forward * (newDistance - distance);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                isRotating = false;
                return;
            }
            lastMousePosition = Input.mousePosition;
            isRotating = true;
        }

        if (Input.GetMouseButton(0) && isRotating)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            float rotationDelta = delta.x * rotateSpeed * 0.1f;

            targetAngle = Mathf.Clamp(targetAngle + rotationDelta, minAngle, maxAngle);
            ApplyRotation(targetAngle);

            lastMousePosition = Input.mousePosition;
            currentAngle = targetAngle;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isRotating = false;
        }

        if (Input.GetMouseButtonDown(2))
        {
            dragOrigin = GetMouseWorldPosition();
            isDragging = true;
        }

        if (Input.GetMouseButton(2) && isDragging)
        {
            Vector3 currentWorldPos = GetMouseWorldPosition();
            Vector3 diff = dragOrigin - currentWorldPos;
            transform.position += diff;
        }

        if (Input.GetMouseButtonUp(2))
        {
            isDragging = false;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetView();
        }

        if (enableSmoothReturn && !isRotating && Mathf.Abs(currentAngle) > 0.5f)
        {
            targetAngle = Mathf.Lerp(targetAngle, 0, Time.deltaTime * returnSpeed);
            if (Mathf.Abs(targetAngle) < 0.01f)
                targetAngle = 0f;
            ApplyRotation(targetAngle);
            currentAngle = targetAngle;
        }
    }

    /// <summary>
    /// 应用相机旋转
    /// </summary>
    void ApplyRotation(float angle)
    {
        Vector3 euler = transform.eulerAngles;
        euler.y = angle;
        transform.eulerAngles = euler;
    }

    /// <summary>
    /// 获取鼠标在地面平面（相机初始高度）上的世界坐标，用于平移拖拽
    /// </summary>
    Vector3 GetMouseWorldPosition()
    {
        if (cam == null) return Vector3.zero;

        Plane plane = new Plane(Vector3.up, new Vector3(0, fixedHeight, 0));
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        float distance;
        if (plane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 获取相机正前方的地面注视点（透视相机缩放用）
    /// </summary>
    Vector3 GetLookAtPoint()
    {
        // 实际实现：以相机位置为起点、朝向为射线，与地面相交
        Ray ray = new Ray(transform.position, transform.forward);
        Plane plane = new Plane(Vector3.up, new Vector3(0, fixedHeight, 0));
        float distance;
        if (plane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }
        return transform.position + transform.forward * 10f;
    }

    /// <summary>
    /// 重置相机视角
    /// </summary>
    public void ResetView()
    {
        targetAngle = 0f;
        currentAngle = 0f;
        ApplyRotation(0f);

        // 重置位置：保持高度不变
        Vector3 pos = transform.position;
        pos.x = originalPosition.x;
        pos.z = originalPosition.z;
        transform.position = pos;

        if (cam.orthographic)
        {
            cam.orthographicSize = 10f;
        }
    }

    /// <summary>
    /// 把相机直接设置到指定位置和朝向，外部调用
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        originalPosition = position;
        transform.position = position;
        fixedHeight = position.y;
    }

    /// <summary>
    /// 获取当前角度
    /// </summary>
    public float GetCurrentAngle() => currentAngle;

    /// <summary>
    /// 是否到达最大角度
    /// </summary>
    public bool IsAtMaxAngle() => Mathf.Abs(currentAngle) >= Mathf.Abs(maxAngle);
}
