using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using static OVRPlugin;

public class ChameleonBehaviour : MonoBehaviour
{
    public UnityEvent OnPaintMode;
    public UnityEvent OnPoseMode;
    public UnityEvent OnGrabMode;

    [SerializeField]
    private TextMeshProUGUI modeText;
    public UnityEvent<String> OnPoseModeChanged;

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

    [SerializeField]
    private GameObject[] toggleObjects; // Objetos que serão ativados/desativados com base no modo

    private void Awake()
    {
        GenerateRuntimeTexture();
        GenerateClone();
        ApplyTextureToMeshes();
        cloneMeshObject.SetActive(false);



        ToggleMode(Mode.Grab); // Inicializa no modo Grab
        OnPoseModeChanged.Invoke(mode.ToString());
        if (modeText != null)
        {
            modeText.text = "Mode: " + mode.ToString();
        }

    }

    void Update()
    {
        if (OVRInput.GetUp(OVRInput.Button.One))
        {
            // loop trough the modes and toggle between them
            Mode[] modes = (Mode[])Enum.GetValues(typeof(Mode));
            int currentIndex = Array.IndexOf(modes, mode);
            currentIndex = (currentIndex + 1) % modes.Length;
            mode = modes[currentIndex];

            // toggle between modes
            ToggleMode(mode);

            OnPoseModeChanged.Invoke(mode.ToString());
            Debug.Log("Mode changed to: " + mode.ToString());
            if (modeText != null)
            {
                modeText.text = "Mode: " + mode.ToString();
            }

            // Fire
            if (mode == Mode.Pose)
            {
                OnPoseMode.Invoke();
            }
            else if (mode == Mode.Paint)
            {
                OnPaintMode.Invoke();

            }
            else if (mode == Mode.Grab)
            {
                OnGrabMode.Invoke();
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
        skinnedMeshRenderer.gameObject.SetActive(mode == Mode.Pose || mode == Mode.Grab);
        cloneMeshObject.SetActive(mode == Mode.Paint);

        // cloneMeshObject must be uptaded with the baked mesh from the skinnedMeshRenderer
        if (mode == Mode.Paint)
        {
            GenerateClone();
        }
    }

    public enum Mode
    {
        Paint,
        Pose,
        Grab
    }
}
