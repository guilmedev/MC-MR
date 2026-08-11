using UnityEngine;

public class QuestPainter : MonoBehaviour
{
    [Range(0.01f, 100f)]
    private float bushRadius = 10f; // Raio do pincel em pixels
    [Header("Environment Sampling")]
    [SerializeField] private Transform raySampleOrigin;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Brush Indicator")]
    [SerializeField] private Transform brushIndicator; // O sprite/quad do pincel

    [SerializeField] float rayLength = 10f;
    public PaintableObject targetObject;
    public BrushSettings brushSettings; // Uma classe simples para guardar Cor e Raio

    void Update()
    {
        if (OVRInput.Get(OVRInput.Button.Two))
        {
            Debug.Log("Button Two released, performing raycast paint.");
            PerformRaycastPaint();
        }
        else
        {
            // Opcional: esconde o indicador se não estiver pintando
            if (brushIndicator != null) brushIndicator.gameObject.SetActive(false);
        }

        HandleBrushSize();
    }

    private void HandleBrushSize()
    {
        // aumenta ou diminui o raio do pincel baseado no direcional do joystick
        float joystickY = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
        if (joystickY > 0.1f)
        {
            bushRadius += joystickY * Time.deltaTime * 20f; // Ajuste a velocidade de aumento conforme necessário
            bushRadius = Mathf.Clamp(bushRadius, 1f, 100f); // Limita o raio entre 1 e 100 pixels
            brushSettings.radius = bushRadius; // Atualiza o raio do pincel
        }
        else if (joystickY < -0.1f)
        {
            bushRadius += joystickY * Time.deltaTime * 20f; // Ajuste a velocidade de diminuição conforme necessário
            bushRadius = Mathf.Clamp(bushRadius, 1f, 100f); // Limita o raio entre 1 e 100 pixels
            brushSettings.radius = bushRadius; // Atualiza o raio do pincel
        }

    }

    public void UpdateColor(Color color)
    {
        brushSettings.color = color;
    }


    void PerformRaycastPaint()
    {
        // Corrigido: direction não precisa ser multiplicado por length para criar o Ray
        Ray ray = new Ray(raySampleOrigin.position, raySampleOrigin.forward);
        Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red, 1f);

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, ray.origin);
        // Corrigido: Posição final da linha deve ser origem + direção * comprimento
        lineRenderer.SetPosition(1, ray.origin + ray.direction * rayLength);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayLength))
        {
            PaintableObject paintable = hit.collider.GetComponent<PaintableObject>();
            if (paintable != null)
            {
                // Importante: hit.textureCoord só funciona se o objeto tiver MeshCollider!
                Debug.Log("Painting at UV: " + hit.textureCoord + " with color: " + brushSettings.color + " and radius: " + brushSettings.radius);

                // O raio precisa ser em pixels (ex: 10, 50). Não use valores pequenos como 0.1
                paintable.PaintAt(hit.textureCoord, brushSettings.color, brushSettings.radius);

                // --- Atualiza o indicador visual ---
                if (brushIndicator != null)
                {
                    // brushIndicator.gameObject.SetActive(true);
                    // Posiciona exatamente no ponto de colisão
                    brushIndicator.position = hit.point;
                    // Alinha com a normal da superfície (fica "deitado" na superfície)
                    brushIndicator.rotation = Quaternion.LookRotation(hit.normal, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
                    // Escala baseada no raio do pincel (multiplicado por 2 pois o raio é a metade do diâmetro)
                    float scale = brushSettings.radius * 0.01f; // Ajuste este multiplicador conforme necessário
                    brushIndicator.localScale = new Vector3(scale, scale, scale);
                }
            }
        }
        else
        {
            if (brushIndicator != null) brushIndicator.gameObject.SetActive(false);
        }
    }
}

[System.Serializable]
public class BrushSettings
{
    public Color color;
    public float radius;
}