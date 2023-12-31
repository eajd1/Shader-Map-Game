using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class RenderWorld : MonoBehaviour
{
    // RenderWorld runs the shaders the display the world

    [SerializeField] private ComputeShader renderShader;
    [Range(0, 1)]
    [SerializeField] private float countryOpacity;
    private Resolution screenResolution;
    private RenderTexture renderTexture;
    private PlayerController playerController;
    private WorldBufferData bufferData;


    // Start is called before the first frame update
    void Start()
    {
        playerController = GetComponent<PlayerController>();
        screenResolution = Screen.currentResolution;
        renderTexture = new RenderTexture(screenResolution.width, screenResolution.height, 1);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        // Initialise renderShader
        int kernelIndex = renderShader.FindKernel("CSMain");
        renderShader.SetTexture(kernelIndex, "Result", renderTexture);
        renderShader.SetInts("ScreenResolution", new int[] { screenResolution.width, screenResolution.height });

        StartCoroutine(AfterStart());
    }

    private IEnumerator AfterStart()
    {
        yield return new WaitForEndOfFrame();
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
        renderShader.SetInt("MapMode", (int)playerController.GetMapMode());
        renderShader.SetInt("Resolution", World.Instance.WorldResolution);
        renderShader.SetFloat("DeepestPoint", World.Instance.MaxDepth);
        renderShader.SetFloat("HighestPoint", World.Instance.MaxHeight);
        renderShader.SetFloat("LowestPoint", 0);
        renderShader.SetFloat("CountryOpacity", countryOpacity);
        Vector2 cameraPos = playerController.GetControls().GetUV();
        renderShader.SetFloats("CameraPosition", new float[] { cameraPos.x, cameraPos.y });
        renderShader.SetFloat("Zoom", playerController.GetControls().GetZoom());

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
