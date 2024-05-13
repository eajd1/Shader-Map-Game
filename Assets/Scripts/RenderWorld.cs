using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class RenderWorld : MonoBehaviour
{
    // RenderWorld runs the shaders the display the world

    private ComputeShader renderShader;
    [Range(0, 1)]
    [SerializeField] private float countryOpacity;
    private Resolution screenResolution;
    private RenderTexture renderTexture;
    private PlayerController playerController;
    private WorldBufferData bufferData;

    [SerializeField] private Texture2D landTexture;
    [SerializeField] private Texture2D worldNormalMap;

    [Header("Lighting")]
    [SerializeField] private float sunSpeed;
    [SerializeField] private float sunVecticalOffset;
    [SerializeField] private Color sunColour;
    [SerializeField] private float sunIntensity;
    [SerializeField] private Color ambientColour;
    [SerializeField] private float ambientIntensity;
    [SerializeField] private float normalMultiplier;
    private float sunAngle;


    // Start is called before the first frame update
    void Start()
    {
        playerController = GetComponent<PlayerController>();
        screenResolution = Screen.currentResolution;
        renderTexture = new RenderTexture(screenResolution.width, screenResolution.height, 1);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        // Initialise renderShader
        renderShader = Resources.Load<ComputeShader>("RenderPlane");
        int kernelIndex = renderShader.FindKernel("CSMain");
        renderShader.SetTexture(kernelIndex, "Result", renderTexture);
        renderShader.SetInts("ScreenResolution", new int[] { screenResolution.width, screenResolution.height });
        renderShader.SetTexture(kernelIndex, "LandTexture", landTexture);
        renderShader.SetTexture(kernelIndex, "WorldNormalMap", worldNormalMap);

        StartCoroutine(AfterStart());
    }

    private IEnumerator AfterStart()
    {
        yield return new WaitForEndOfFrame();
        // Buffers need to be updated after start because they might not have been created by the world yet
        UpdateBuffers();
    }

    public void UpdateBuffers()
    {
        bufferData = World.Instance.Buffers;
        int kernelIndex = renderShader.FindKernel("CSMain");
        renderShader.SetBuffer(kernelIndex, "Tiles", bufferData.tileBuffer);
        renderShader.SetBuffer(kernelIndex, "CountryColours", bufferData.countryColourBuffer);
    }

    // Update is called once per frame
    void Update()
    {
        sunAngle += Time.deltaTime * sunSpeed;
        if (sunAngle > Mathf.PI * 2)
        {
            sunAngle = 0;
        }

        renderShader.SetInt("MapMode", (int)playerController.GetMapMode());
        renderShader.SetInt("Resolution", World.Instance.WorldResolution);
        renderShader.SetFloat("HighestPoint", World.Instance.MaxHeight);
        renderShader.SetFloat("LowestPoint", World.Instance.MinHeight);
        renderShader.SetFloat("CountryOpacity", countryOpacity);
        Vector2 cameraPos = playerController.GetControls().GetCameraUV();
        renderShader.SetFloats("CameraPosition", new float[] { cameraPos.x, cameraPos.y });
        renderShader.SetFloat("Zoom", playerController.GetControls().GetZoom());

        // Lighting
        Vector3 sunDirection = new Vector3(Mathf.Sin(sunAngle), sunVecticalOffset, Mathf.Cos(sunAngle));
        sunDirection = sunDirection.normalized;
        renderShader.SetFloats("SunDirection", new float[] { sunDirection.x, sunDirection.y, sunDirection.z });
        renderShader.SetFloats("SunColour", new float[] { sunColour.r, sunColour.g, sunColour.b });
        renderShader.SetFloat("SunIntensity", sunIntensity);
        renderShader.SetFloat("AmbientIntensity", ambientIntensity);
        renderShader.SetFloats("AmbientColour", new float[] { ambientColour.r, ambientColour.g, ambientColour.b });
        renderShader.SetFloat("NormalMultiplier", normalMultiplier);

        int kernelIndex = renderShader.FindKernel("CSMain");
        if (BuffersExist())
        {
            renderShader.Dispatch(kernelIndex, screenResolution.width, screenResolution.height, 1);
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(renderTexture, destination);
    }

    private bool BuffersExist()
    {
        return bufferData != null &&
            bufferData.tileBuffer != null &&
            bufferData.countryColourBuffer != null;
    }

    private void OnApplicationQuit()
    {
        bufferData.ReleaseBuffers();
    }
}
