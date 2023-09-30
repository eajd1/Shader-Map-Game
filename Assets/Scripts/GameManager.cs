using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CameraControls))]
public class GameManager : MonoBehaviour
{
    [SerializeField] private ComputeShader renderShader;
    [SerializeField] private ComputeShader initialisationShader;
    [Range(128, 7327)] // Cant go higher than that for some reason
    [SerializeField] private int resolution;

    [SerializeField] private float maxHeight;
    [SerializeField] private float maxDepth;

    [SerializeField] private Texture2D heightmap;
    [SerializeField] private Texture2D bathymap;
    [SerializeField] private Texture2D sealevelmask;

    private CameraControls controls;

    private float[] pixels;
    private ComputeBuffer pixelBuffer;
    private RenderTexture renderTexture;
    private Resolution screenResolution;
    //[SerializeField] private RenderTexture background;
    //[SerializeField] private ComputeShader combine;

    [SerializeField] private ComputeShader simulationShader;
    private bool simulate = false;

    private MapMode mapMode;

    void Start()
    {
        screenResolution = Screen.currentResolution;
        controls = GetComponent<CameraControls>();

        renderTexture = new RenderTexture(screenResolution.width, screenResolution.height, 1);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        //background.enableRandomWrite = true;

        MakePixels();

        mapMode = MapMode.Terrain;

        InitialiseShader();
    }

    private void InitialiseShader()
    {
        int kernelIndex = renderShader.FindKernel("CSMain");

        renderShader.SetTexture(kernelIndex, "result", renderTexture);
        renderShader.SetBuffer(kernelIndex, "pixels", pixelBuffer);
        renderShader.SetInts("screenResolution", new int[] { screenResolution.width, screenResolution.height });

        UpdateInput();
    }

    void Update()
    {
        GetInputs();

        if (simulate)
        {
            //UpdateWorld();
        }

        UpdateInput();
    }

    private void UpdateWorld()
    {
        int kernalIndex = simulationShader.FindKernel("Test");
        simulationShader.SetBuffer(kernalIndex, "pixels", pixelBuffer);
        simulationShader.SetInt("resolution", resolution);
        simulationShader.Dispatch(kernalIndex, 2 * resolution, resolution, 1);
    }

    private void UpdateInput()
    {
        renderShader.SetInt("mapMode", (int)mapMode);
        renderShader.SetFloat("resolution", resolution);
        renderShader.SetFloat("deepestPoint", maxDepth);
        renderShader.SetFloat("highestPoint", maxHeight);
        renderShader.SetFloat("lowestPoint", 0);
        Vector2 cameraPos = controls.GetUV();
        renderShader.SetFloats("cameraPosition", new float[] { cameraPos.x, cameraPos.y });
        renderShader.SetFloat("zoom", controls.GetZoom());

        int kernelIndex = renderShader.FindKernel("CSMain");
        renderShader.Dispatch(kernelIndex, screenResolution.width, screenResolution.height, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(renderTexture, destination);
    }

    private void MakePixels()
    {
        // Init using compute shader
        //pixels = new float[2 * resolution * resolution];

        //int kernelIndex = initialisationShader.FindKernel("CSMain");

        //pixelBuffer = new ComputeBuffer(pixels.Length, sizeof(float));
        //pixelBuffer.SetData(pixels);

        //initialisationShader.SetTexture(kernelIndex, "topography", heightmap);
        //initialisationShader.SetTexture(kernelIndex, "bathymetry", bathymap);
        //initialisationShader.SetBuffer(kernelIndex, "pixels", pixelBuffer);

        //initialisationShader.SetInt("resolution", resolution);
        //initialisationShader.SetInts("textureResolution", new int[] { heightmap.width, heightmap.height });
        //initialisationShader.SetFloat("maxHeight", maxHeight);
        //initialisationShader.SetFloat("maxDepth", maxDepth);

        //initialisationShader.Dispatch(kernelIndex, 2 * resolution, resolution, 1);

        //pixelBuffer.GetData(pixels);

        //pixelBuffer.Release();

        // Init using procedural generation
        //pixels = ProceduralTerrain.Generate(resolution, (int)maxDepth, (int)maxHeight);

        // Init using c#
        pixels = LoadEarth.GenerateEarth(resolution, maxDepth, maxHeight, heightmap, bathymap, sealevelmask);

        pixelBuffer = new ComputeBuffer(pixels.Length, sizeof(float));
        pixelBuffer.SetData(pixels);
    }

    private void GetInputs()
    {
        // Map Changes

        if (Input.GetKeyDown(KeyCode.Q))
        {
            mapMode = MapMode.Terrain;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            mapMode = MapMode.Ocean;
        }

        if (Input.GetButtonDown(Inputs.LMB))
        {
            simulate = !simulate;
        }
    }

    private void OnApplicationQuit()
    {
        //pixelBuffer.Dispose();
        pixelBuffer.Release();
    }
}

public struct Pixel
{
    public float height;
    public uint details; // Bitwise number representing the bool values: ocean, road, river
    public float pressure;
    public float temperature;
    public float humidity;

    public Pixel(float height, bool ocean)
    {
        this.height = height;
        pressure = 1000;
        temperature = 0;
        humidity = 0;

        details = 0;
        if (ocean)
        {
            details += 1 << 0;
        }
    }

    public static int SizeOf()
    {
        return sizeof(float) * 4 + sizeof(uint);
    }
}

public enum MapMode
{
    Terrain,
    Ocean
}