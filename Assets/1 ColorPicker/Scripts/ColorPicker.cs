using System.Collections;
using Meta.XR;
using Unity.Collections;
using UnityEngine;

public enum SamplingMode
{
    Environment,
    Manual
}

public class ColorPicker : MonoBehaviour
{
    [Header("Environment Sampling")]
    [SerializeField] private Transform raySampleOrigin;

    [Header("Manual Sampling")]
    [SerializeField] private Transform manualSamplingOrigin;

    [Header("Brightness Correction")]
    [SerializeField, Range(0f, 1f)] private float targetBrightness = 0.8f;
    [SerializeField, Range(0f, 1f)] private float correctionSmoothing = 0.5f;
    [SerializeField] private int roiSize = 3;
    [SerializeField] private float minCorrection = 0.8f;
    [SerializeField] private float maxCorrection = 1.5f;

    private float _prevCorrectionFactor = 1f;
    private Renderer _manualRenderer;
    [SerializeField] private PassthroughCameraAccess cameraAccess;
    private EnvironmentRaycastManager _raycastManager;
    private Vector2Int _cameraResolution;

    private void Start()
    {
        cameraAccess = ResolveCameraAccess(cameraAccess);
        _raycastManager = GetComponent<EnvironmentRaycastManager>();

        if (!cameraAccess || !_raycastManager || !raySampleOrigin)
        {
            Debug.Log("ColorPicker: Missing required references.");
            return;
        }

        if (manualSamplingOrigin)
        {
            _manualRenderer = manualSamplingOrigin.GetComponent<Renderer>();
        }

        StartCoroutine(WaitForCameraFeed());
    }

    private static PassthroughCameraAccess ResolveCameraAccess(PassthroughCameraAccess configuredAccess)
    {
        if (configuredAccess)
        {
            return configuredAccess;
        }
        return FindAnyObjectByType<PassthroughCameraAccess>(FindObjectsInactive.Include);
    }

    private IEnumerator WaitForCameraFeed()
    {
        while (cameraAccess && !cameraAccess.IsPlaying)
        {
            yield return null;
        }
        _cameraResolution = cameraAccess ? cameraAccess.CurrentResolution : Vector2Int.zero;
    }

    public bool ProcessPickColor(LineRenderer lineRenderer, out Color sampledColor)
    {
        sampledColor = Color.clear;
        if (!cameraAccess)
        {
            Debug.Log("No camera Access found.");
            return false;
        }

        if (!cameraAccess.IsPlaying)
        {
            Debug.Log("Passthrough feed not ready.");
            return false;
        }

        Ray ray = new(raySampleOrigin.position, raySampleOrigin.forward);
        var hitSuccess = _raycastManager.Raycast(ray, out var hit);
        if (!hitSuccess)
        {
            Debug.Log("ColorPicker: No environment hit to sample.");
            return false;
        }

        if (lineRenderer)
        {
            lineRenderer.SetPosition(0, ray.origin);
            lineRenderer.SetPosition(1, hit.point);
        }

        _cameraResolution = cameraAccess.CurrentResolution;
        if (_cameraResolution == Vector2Int.zero || !TryGetPixelCoordinate(hit.point, out var pixel))
        {
            Debug.Log("ColorPicker: Unable to project sampling point onto the camera.");
            return false;
        }

        var colors = cameraAccess.GetColors();
        if (!colors.IsCreated)
        {
            Debug.Log("ColorPicker: Camera colors not ready.");
            return false;
        }

        sampledColor = SampleAndCorrectColor(pixel, colors, _cameraResolution);

        if (_manualRenderer)
        {
            _manualRenderer.material.color = sampledColor;
        }

        if (lineRenderer)
        {
            lineRenderer.startColor = sampledColor;
            lineRenderer.endColor = sampledColor;
        }

        return true;
    }

    private bool TryGetPixelCoordinate(Vector3 worldPoint, out Vector2Int pixel)
    {
        pixel = default;
        if (!cameraAccess || !cameraAccess.IsPlaying || _cameraResolution == Vector2Int.zero)
        {
            return false;
        }

        var viewport = cameraAccess.WorldToViewportPoint(worldPoint);
        if (viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
        {
            return false;
        }

        pixel = new Vector2Int(
            Mathf.Clamp(Mathf.RoundToInt(viewport.x * (_cameraResolution.x - 1)), 0, _cameraResolution.x - 1),
            Mathf.Clamp(Mathf.RoundToInt(viewport.y * (_cameraResolution.y - 1)), 0, _cameraResolution.y - 1));
        return true;
    }

    private Color SampleAndCorrectColor(Vector2Int pixel, NativeArray<Color32> colors, Vector2Int resolution)
    {
        var sampledColor = (Color)colors[pixel.y * resolution.x + pixel.x];
        var brightness = CalculateRoiBrightness(pixel, colors, resolution);

        var factor = Mathf.Clamp(targetBrightness / Mathf.Max(brightness, 0.001f), minCorrection, maxCorrection);
        _prevCorrectionFactor = Mathf.Lerp(_prevCorrectionFactor, factor, correctionSmoothing);

        var corrected = (sampledColor.linear * _prevCorrectionFactor).gamma;
        return new Color(Mathf.Clamp01(corrected.r), Mathf.Clamp01(corrected.g), Mathf.Clamp01(corrected.b), corrected.a);
    }

    private float CalculateRoiBrightness(Vector2Int centerPixel, NativeArray<Color32> colors, Vector2Int resolution)
    {
        var sum = 0f;
        var count = 0;
        var half = roiSize / 2;

        for (var i = -half; i <= half; i++)
        {
            for (var j = -half; j <= half; j++)
            {
                int xi = centerPixel.x + i, yj = centerPixel.y + j;
                if (xi < 0 || xi >= resolution.x || yj < 0 || yj >= resolution.y)
                {
                    continue;
                }

                var pixel = ((Color)colors[yj * resolution.x + xi]).linear;
                sum += 0.2126f * pixel.r + 0.7152f * pixel.g + 0.0722f * pixel.b;
                count++;
            }
        }

        return count > 0 ? sum / count : 0f;
    }

}
