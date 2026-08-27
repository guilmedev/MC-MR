using UnityEngine;
using static OVRInput;
public class QuestPainter : MonoBehaviour
{
    [SerializeField]
    private Controller controller;

    [Header("Brush")]
    [SerializeField, Min(0.01f)] private float minimumBrushRadius = 10f;
    [SerializeField, Min(0.01f)] private float maximumBrushRadius = 100f;
    [SerializeField, Min(0.01f)] private float bushRadius = 10f; // Raio do pincel em pixels
    [Header("Environment Sampling")]
    [SerializeField] private Transform raySampleOrigin;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Brush Indicator")]
    [SerializeField] private Transform brushIndicator;
    [SerializeField, Min(0f)] private float indicatorSurfaceOffset = 0.08f;

    [SerializeField] private float rayLength = 10f;
    [SerializeField] private ColorPicker colorPicker;
    public BrushSettings brushSettings = new BrushSettings { color = Color.white, radius = 10f };

    private RaycastHit _lastHit;
    private PaintableObject _lastPaintable;
    private bool _hasHit;
    private Vector3 _indicatorBaseScale;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        if (brushIndicator)
        {
            _indicatorBaseScale = brushIndicator.localScale;
        }

        if (lineRenderer)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
        }
    }

    private void Update()
    {
        HandleBrushSize();

        if (OVRInput.Get(OVRInput.Button.Two, controller))
        {
            if (colorPicker && colorPicker.ProcessPickColor(lineRenderer, out Color sampledColor))
            {
                brushSettings.color = sampledColor;
                lineRenderer.material.color = brushSettings.color;
            }

            return;
        }

        UpdateLine();

        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, controller))
        {
            PerformRaycastPaint();
        }
    }

    private void UpdateLine()
    {
        Ray ray = new Ray(raySampleOrigin.position, raySampleOrigin.forward);
        Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red, 1f);

        if (lineRenderer)
        {
            lineRenderer.SetPosition(0, ray.origin);
        }

        if (Physics.Raycast(ray, out var hit, rayLength))
        {
            _hasHit = true;
            _lastPaintable = hit.collider.GetComponent<PaintableObject>();
            _lastHit = hit;
            SetRayEnd(hit.point);

            if (_lastPaintable != null && brushIndicator)
            {
                brushIndicator.gameObject.SetActive(true);
                brushIndicator.position = hit.point + hit.normal * indicatorSurfaceOffset;
                brushIndicator.rotation = Quaternion.LookRotation(hit.normal, ray.direction);
                float scaleFactor = brushSettings.radius / minimumBrushRadius;
                brushIndicator.localScale = _indicatorBaseScale * scaleFactor;
            }
            else if (brushIndicator)
            {
                brushIndicator.gameObject.SetActive(false);
            }
        }
        else
        {
            _hasHit = false;
            _lastPaintable = null;
            _lastHit = default;
            if (brushIndicator) brushIndicator.gameObject.SetActive(false);
            SetRayEnd(ray.origin + ray.direction * rayLength);
        }
    }

    private void SetRayEnd(Vector3 position)
    {
        if (lineRenderer) lineRenderer.SetPosition(1, position);
    }

    private void HandleBrushSize()
    {
        float joystickY = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controller).y;
        if (Mathf.Abs(joystickY) > 0.1f)
        {
            bushRadius = Mathf.Clamp(
                bushRadius + joystickY * Time.deltaTime * (maximumBrushRadius - minimumBrushRadius),
                minimumBrushRadius,
                maximumBrushRadius);
            brushSettings.radius = bushRadius;
        }

    }

    private void PerformRaycastPaint()
    {
        if (_lastPaintable != null)
        {
            try
            {
                Vector2 textureCoord = _lastHit.textureCoord;
                _lastPaintable.PaintAt(textureCoord, brushSettings.color, brushSettings.radius);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to get texture coordinates: " + e.Message +
                    "\nEnsure the mesh collider uses a mesh with float32 UV format." +
                    "\nCheck: Model Import Settings > Meshes > Optimize Mesh");

                // Fallback: use hit.point instead
                if (_lastPaintable != null)
                {
                    _lastPaintable.PaintAt(_lastHit.point, brushSettings.color, brushSettings.radius);
                }
            }
        }
    }

}

[System.Serializable]
public class BrushSettings
{
    public Color color;
    public float radius;
}