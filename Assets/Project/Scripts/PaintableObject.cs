using UnityEngine;

public class PaintableObject : MonoBehaviour
{
    public Texture2D runtimeAlbedo; // A textura que será alterada
    private Renderer rend;

    private void Awake()
    {
        rend = GetComponent<Renderer>();

        // Cria a textura dinâmica de 1024x1024
        runtimeAlbedo = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);

        Color[] albedoPixels = new Color[runtimeAlbedo.width * runtimeAlbedo.height];

        // Preenche a textura com branco
        for (int i = 0; i < albedoPixels.Length; i++)
        {
            albedoPixels[i] = Color.white;
        }

        runtimeAlbedo.SetPixels(albedoPixels);
        runtimeAlbedo.Apply();

        // Atribui a nova textura ao material
        Material mat = rend.material;
        mat.SetTexture("_BaseMap", runtimeAlbedo); // Use "_MainTexture" se estiver usando o pipeline Built-in
    }

    // Função que será chamada pelo controller
    public void PaintAt(Vector2 uv, Color color, float radius)
    {
        if (runtimeAlbedo == null) return;

        int width = runtimeAlbedo.width;
        int height = runtimeAlbedo.height;

        int centerX = Mathf.RoundToInt(uv.x * width);
        int centerY = Mathf.RoundToInt(uv.y * height);
        int radiusInt = Mathf.RoundToInt(radius);

        // Proteção para raio muito pequeno
        if (radiusInt < 1) radiusInt = 1;
        for (int y = -radiusInt; y <= radiusInt; y++)
        {
            for (int x = -radiusInt; x <= radiusInt; x++)
            {
                int px = centerX + x;
                int py = centerY + y;
                // Verifica se o pixel está dentro dos limites da textura
                if (px >= 0 && px < width && py >= 0 && py < height)
                {
                    // Verifica se o pixel está dentro do círculo do brush
                    float distance = Vector2.Distance(new Vector2(x, y), Vector2.zero);
                    if (distance <= radiusInt)
                    {
                        runtimeAlbedo.SetPixel(px, py, color);
                    }
                }
            }
        }
        runtimeAlbedo.Apply();
    }
}

