using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class RenderWorld : MonoBehaviour
{
    // RenderWorld runs the shaders the display the world

    [SerializeField] private ComputeShader renderShader;
    private Resolution screenResolution;
    private RenderTexture renderTexture;
    private PlayerController playerController;

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
        renderShader.SetTexture(kernelIndex, "result", renderTexture);
        renderShader.SetInts("screenResolution", new int[] { screenResolution.width, screenResolution.height });

        StartCoroutine(AfterStart());
    }

    private IEnumerator AfterStart()
    {
        yield return new WaitForEndOfFrame();
        UpdateBuffers();
    }

    // Update is called once per frame
    void Update()
    {
        renderShader.SetInt("mapMode", (int)playerController.GetMapMode());
        renderShader.SetFloat("resolution", World.Instance.WorldResolution);
        renderShader.SetFloat("deepestPoint", World.Instance.MaxDepth);
        renderShader.SetFloat("highestPoint", World.Instance.MaxHeight);
        renderShader.SetFloat("lowestPoint", 0);
        Vector2 cameraPos = playerController.GetControls().GetUV();
        renderShader.SetFloats("cameraPosition", new float[] { cameraPos.x, cameraPos.y });
        renderShader.SetFloat("zoom", playerController.GetControls().GetZoom());

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

    private void UpdateBuffers()
    {
        int kernelIndex = renderShader.FindKernel("CSMain");
        renderShader.SetBuffer(kernelIndex, "heights", World.Instance.GetBuffer("height"));
        renderShader.SetBuffer(kernelIndex, "countryIds", World.Instance.GetBuffer("id"));
        renderShader.SetBuffer(kernelIndex, "countryColours", World.Instance.GetBuffer("countryColour"));
    }

    private bool BuffersExist()
    {
        return World.Instance.GetBuffer("height") != null &&
            World.Instance.GetBuffer("id") != null &&
            World.Instance.GetBuffer("countryColour") != null;
    }
}
