using UnityEngine;
using static OVRInput;

public class QuestPainter : MonoBehaviour
{
    [SerializeField]
    private Controller controller;

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

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, raySampleOrigin.position);
        // Corrigido: Posição final da linha deve ser origem + direção * comprimento
        lineRenderer.SetPosition(1, raySampleOrigin.position + raySampleOrigin.forward * rayLength);


        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, controller))
        {
            Debug.Log("performing raycast paint.");
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
        float joystickY = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controller).y;
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



        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayLength))
        {
            PaintableObject paintable = hit.collider.GetComponent<PaintableObject>();
            if (paintable != null)
            {
                // Corrigido: Posição final da linha deve ser origem + direção * comprimento
                lineRenderer.SetPosition(1, hit.point);

                try
                {
                    // Importante: hit.textureCoord só funciona se o objeto tiver MeshCollider com UVs em float32!
                    Vector2 textureCoord = hit.textureCoord;
                    Debug.Log("Painting at UV: " + textureCoord + " with color: " + brushSettings.color + " and radius: " + brushSettings.radius);

                    // O raio precisa ser em pixels (ex: 10, 50). Não use valores pequenos como 0.1
                    paintable.PaintAt(textureCoord, brushSettings.color, brushSettings.radius);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to get texture coordinates: " + e.Message +
                        "\nEnsure the mesh collider uses a mesh with float32 UV format." +
                        "\nCheck: Model Import Settings > Meshes > Optimize Mesh");

                    // Fallback: use hit.point instead
                    if (paintable != null)
                    {
                        paintable.PaintAt(hit.point, brushSettings.color, brushSettings.radius);
                    }
                }

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