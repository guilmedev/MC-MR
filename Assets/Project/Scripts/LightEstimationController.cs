using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Unity.Collections;
using UnityEngine;

public class LightEstimationController : MonoBehaviour
{

    [SerializeField] private PassthroughCameraAccess m_cameraAccess;
    [SerializeField] private float m_refreshTime = 0.05f;
    [SerializeField][Range(1, 100)] private int m_bufferSize = 10;

    [Header("Meta Brightness Estimation")]
    [Range(0, 1)]
    public float brightnessValue;

    [Header("Lights")]
    public Light directionalLight;

    [Header("Light Rotation — Follow Camera")]
    [Tooltip("Se ativado, a luz direcional segue a rotação da câmera como um holofote")]
    public bool m_followCamera = true;
    public Vector3 lightRotationOffset = new Vector3(50f, 0f, 0f);
    [Min(0f)] public float lightRotationSmoothing = 5f;

    [Header("Ambient")]
    public float minAmbientIntensity = 0.2f;
    public float maxAmbientIntensity = 1.5f;

    [Header("Directional Light Intensity")]
    public float minDirectionalIntensity = 0.3f;
    public float maxDirectionalIntensity = 0.9f;

    [Header("Ambient Color")]
    public Color ambientLightColor = Color.white;
    public bool useAutoAmbientColor = true;

    [Header("Brightness Normalization")]
    public float brightnessMin = 0f;
    public float brightnessMax = 255f;

    [Header("Debug")]
    [SerializeField] private bool m_enableDebugLogs = true;
    [SerializeField, Min(0.1f)] private float m_debugLogInterval = 0.5f;

    private float m_refreshCurrentTime;
    private float m_debugLogCurrentTime;
    private List<float> m_brightnessVals = new();
    private float m_normalizedBrightness;
    private Quaternion m_smoothedLightRotation;
    private bool m_hasLightRotation;

    private void Update()
    {
        if (m_cameraAccess == null || !m_cameraAccess.IsPlaying)
            return;

        if (IsWaiting())
            return;

        // --- Light follows camera (headlight approach) ---
        if (m_followCamera && directionalLight != null)
        {
            UpdateLightFollowCamera();
        }

        var globalBrightness = GetRoomAmbientLight();

        if (globalBrightness >= 0)
        {
            ApplyLightingAdjustments(globalBrightness);
            Debug.Log($"brightness={m_normalizedBrightness:F2} | " +
                     $"ambient={RenderSettings.ambientIntensity:F2} | " +
                     $"directional={directionalLight.intensity:F2} | " +
                     $"dir<{(m_followCamera ? "follow" : "fixed")}>");
        }
        else
        {
            Debug.Log("sample skipped | camera not ready");
        }
    }

    private void UpdateLightFollowCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        // Luz aponta na mesma direção que a câmera, com offset para ficar acima
        var targetRotation = cam.transform.rotation * Quaternion.Euler(lightRotationOffset);

        if (!m_hasLightRotation)
        {
            m_smoothedLightRotation = targetRotation;
            m_hasLightRotation = true;
        }
        else
        {
            var t = 1f - Mathf.Exp(-lightRotationSmoothing * Time.deltaTime);
            m_smoothedLightRotation = Quaternion.Slerp(m_smoothedLightRotation, targetRotation, t);
        }

        directionalLight.transform.rotation = m_smoothedLightRotation;
    }

    /// <summary>
    /// Aplica os ajustes de iluminação baseado no brilho estimado do ambiente
    /// </summary>
    /// <param name="globalBrightness">Valor de brilho global (média dos valores capturados)</param>
    private void ApplyLightingAdjustments(float globalBrightness)
    {
        // Normalizar o brightness para um valor entre 0 e 1
        m_normalizedBrightness = Mathf.Clamp01((globalBrightness - brightnessMin) / (brightnessMax - brightnessMin));

        // Armazenar para debug/visualização
        brightnessValue = m_normalizedBrightness;

        // Ajustar Directional Light Intensity
        if (directionalLight != null)
        {
            directionalLight.intensity = Mathf.Lerp(minDirectionalIntensity, maxDirectionalIntensity, m_normalizedBrightness);
        }

        // Ajustar RenderSettings - Ambient Intensity
        float ambientIntensity = Mathf.Lerp(minAmbientIntensity, maxAmbientIntensity, m_normalizedBrightness);
        RenderSettings.ambientIntensity = ambientIntensity;

        // Ajustar RenderSettings - Ambient Color (interpolação dinâmica baseada no brilho)
        if (useAutoAmbientColor)
        {
            // Criar uma cor de ambiente dinâmica baseada no brilho
            // Ambientes escuros tendem a tons mais azulados, claros tendem a tons mais amarelados
            Color darkAmbient = new Color(0.2f, 0.2f, 0.3f); // Azulado para ambientes escuros
            Color brightAmbient = new Color(1f, 1f, 0.8f);   // Amarelado para ambientes claros

            Color calculatedAmbientColor = Color.Lerp(darkAmbient, brightAmbient, m_normalizedBrightness);
            RenderSettings.ambientLight = calculatedAmbientColor;
        }
        else
        {
            // Usar cor configurada manualmente, mas ajustar sua intensidade
            RenderSettings.ambientLight = ambientLightColor * m_normalizedBrightness;
        }
    }

    /// <summary>
    /// Estimate the Brightness Level using a Texture2D
    /// </summary>
    /// <returns>String data for debugging purposes</returns>
    private float GetRoomAmbientLight()
    {
        m_refreshCurrentTime = m_refreshTime;
        var pixels = m_cameraAccess.GetColors();

        float colorSum = 0;
        for (int x = 0, len = pixels.Length; x < len; x++)
        {
            colorSum += 0.2126f * pixels[x].r + 0.7152f * pixels[x].g + 0.0722f * pixels[x].b;
        }
        var size = m_cameraAccess.CurrentResolution;
        var brightnessVals = Mathf.Floor(colorSum / (size.x * size.y));

        m_brightnessVals.Add(brightnessVals);

        if (m_brightnessVals.Count > m_bufferSize)
        {
            m_brightnessVals.RemoveAt(0);
        }

        Debug.Log("Brightness Values: " + string.Join(", ", m_brightnessVals));
        return brightnessVals;
    }

    /// <summary>
    /// Return true if the waiting time is bigger than zero.
    /// </summary>
    /// <returns>True or False</returns>
    private bool IsWaiting()
    {
        m_refreshCurrentTime -= Time.deltaTime;
        return m_refreshCurrentTime > 0.0f;
    }

    /// <summary>
    /// Get the average Brightness level based on the buffer size.
    /// </summary>
    /// <returns>Average brightness level (float)</returns>
    private float GetGlobalBrigthnessLevel()
    {
        if (m_brightnessVals.Count == 0)
        {
            return -1;
        }

        var sum = 0.0f;
        foreach (var b in m_brightnessVals)
        {
            sum += b;
        }
        return sum / m_brightnessVals.Count;
    }

}