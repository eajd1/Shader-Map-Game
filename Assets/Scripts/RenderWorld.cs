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
    private WorldData data;


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
        data = new WorldData(World.Instance.Heights, World.Instance.IDs, World.Instance.Countries);
        World.Instance.Subscribe(data);
        int kernelIndex = renderShader.FindKernel("CSMain");
        renderShader.SetBuffer(kernelIndex, "heights", data.heightBuffer);
        renderShader.SetBuffer(kernelIndex, "countryIds", data.idBuffer);
        renderShader.SetBuffer(kernelIndex, "countryColours", data.countryColourBuffer);
    }

    // Update is called once per frame
    void Update()
    {
        renderShader.SetInt("mapMode", (int)playerController.GetMapMode());
        renderShader.SetFloat("resolution", World.Instance.WorldResolution);
        renderShader.SetFloat("deepestPoint", World.Instance.MaxDepth);
        renderShader.SetFloat("highestPoint", World.Instance.MaxHeight);
        renderShader.SetFloat("lowestPoint", 0);
        renderShader.SetFloat("countryOpacity", countryOpacity);
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

    private bool BuffersExist()
    {
        return data != null &&
            data.heightBuffer != null &&
            data.idBuffer != null &&
            data.countryColourBuffer != null;
    }

    private void OnApplicationQuit()
    {
        data.ReleaseBuffers();
    }
}
