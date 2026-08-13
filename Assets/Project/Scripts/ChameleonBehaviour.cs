using System;
using UnityEngine;
using UnityEngine.Events;
using static OVRPlugin;

public class ChameleonBehaviour : MonoBehaviour
{
    public UnityEvent OnPaintMode;
    public UnityEvent OnPoseMode;

    [SerializeField]
    private Mode mode;

    [SerializeField]
    private int textureWidth = 1024;
    [SerializeField]
    private int textureHeight = 1024;
    [SerializeField]
    private Color initialColor = Color.white;

    public Texture2D runtimeAlbedo; // A textura que será alterada
    private Renderer rend;

    [SerializeField]
    private SkinnedMeshRenderer skinnedMeshRenderer;

    [SerializeField]
    private MeshFilter meshFilter;
    [SerializeField]
    private MeshCollider meshCollider;
    [SerializeField]
    private GameObject cloneMeshObject;

    private void Awake()
    {
        GenerateRuntimeTexture();
        GenerateClone();
        ApplyTextureToMeshes();
        cloneMeshObject.SetActive(false);
    }

    void Update()
    {
        if (OVRInput.GetUp(OVRInput.Button.One))
        {
            // toggle between modes
            mode = (mode == Mode.Pose) ? Mode.Paint : Mode.Pose;
            ToggleMode(mode);
            // Fire
            if (mode == Mode.Pose)
            {
                OnPoseMode.Invoke();
            }
            else
            {
                OnPaintMode.Invoke();
            }
        }
    }

    private void GenerateRuntimeTexture()
    {
        runtimeAlbedo = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = initialColor;
        }
        runtimeAlbedo.SetPixels(pixels);
        runtimeAlbedo.Apply();
    }

    private void ApplyTextureToMeshes()
    {
        if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMaterial != null)
        {
            skinnedMeshRenderer.sharedMaterial.mainTexture = runtimeAlbedo;
        }

        if (cloneMeshObject != null)
        {
            Renderer cloneRend = cloneMeshObject.GetComponent<Renderer>();
            if (cloneRend != null && cloneRend.sharedMaterial != null)
            {
                cloneRend.sharedMaterial.mainTexture = runtimeAlbedo;
            }

            cloneMeshObject.GetComponent<PaintableObject>().UpdateTexture(runtimeAlbedo);
        }
    }

    private void GenerateClone()
    {
        UnityEngine.Mesh mesh = new UnityEngine.Mesh();
        skinnedMeshRenderer.BakeMesh(mesh);
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    public void ToggleMode(Mode mode)
    {
        skinnedMeshRenderer.gameObject.SetActive(mode == Mode.Pose);
        cloneMeshObject.SetActive(mode == Mode.Paint);
    }

    public enum Mode
    {
        Paint,
        Pose
    }
}
