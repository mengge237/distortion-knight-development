using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("缩放")]
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 15f;
    [SerializeField] private float zoomSpeed = 2f;

    [Header("旋转（像人转头一样）")]
    [SerializeField] private float rotateSpeed = 2f;
    [SerializeField] private float minAngle = -60f;   // 左转最大角度
    [SerializeField] private float maxAngle = 60f;    // 右转最大角度

    [Header("拖拽平移")]
    [SerializeField] private float dragSpeed = 0.5f;

    [Header("平滑回正")]
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

    // 当前摄像机距离地面的高度（用于保持视角高度不变）
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
        // --- 滚轮缩放 ---
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
                // 透视相机：沿视线方向移动
                Vector3 forward = transform.forward;
                float distance = Vector3.Distance(transform.position, GetLookAtPoint());
                float newDistance = distance - scroll * zoomSpeed;
                newDistance = Mathf.Clamp(newDistance, minZoom, maxZoom);
                transform.position += forward * (newDistance - distance);
            }
        }

        // --- 鼠标左键旋转视角（像人转头） ---
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

        // --- 鼠标中键拖拽平移 ---
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

        // --- 空格键回正 ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetView();
        }

        // --- 平滑回正（可选） ---
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
    /// 应用旋转 - 摄像机围绕自身Y轴旋转（像人转头）
    /// </summary>
    void ApplyRotation(float angle)
    {
        Vector3 euler = transform.eulerAngles;
        euler.y = angle;
        transform.eulerAngles = euler;
    }

    /// <summary>
    /// 获取鼠标在世界空间中的位置（用于拖拽）
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
    /// 获取摄像机看向的点（透视相机用）
    /// </summary>
    Vector3 GetLookAtPoint()
    {
        // 简单实现：从摄像机位置沿前进方向投射
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
    /// 重置视角
    /// </summary>
    public void ResetView()
    {
        targetAngle = 0f;
        currentAngle = 0f;
        ApplyRotation(0f);

        // 重置位置（保持高度不变）
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
    /// 设置摄像机位置（外部调用）
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
