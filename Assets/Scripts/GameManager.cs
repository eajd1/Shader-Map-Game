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

    [SerializeField] private Texture2D topographyTexture;
    [SerializeField] private float topographyMaxHeight;
    [SerializeField] private Texture2D bathymetryTexture;
    [SerializeField] private float bathymetryMaxDepth;

    private CameraControls controls;

    private Pixel[] pixels;
    private ComputeBuffer pixelBuffer;
    private RenderTexture renderTexture;
    private Resolution screenResolution;
    //[SerializeField] private RenderTexture background;
    //[SerializeField] private ComputeShader combine;

    [SerializeField] private ComputeShader simulationShader;

    private MapMode mapMode;

    void Start()
    {
        screenResolution = Screen.currentResolution;

        renderTexture = new RenderTexture(screenResolution.width, screenResolution.height, 1);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        //background.enableRandomWrite = true;

        MakePixels();

        controls = GetComponent<CameraControls>();

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
        UpdateWorld();

        GetInputs();

        UpdateInput();

        //int kernelIndex = combine.FindKernel("CSMain");
        //combine.SetTexture(kernelIndex, "a", background);
        //combine.SetTexture(kernelIndex, "b", renderTexture);
        //combine.Dispatch(kernelIndex, screenResolution.width, screenResolution.height, 1);
    }

    private void UpdateWorld()
    {
        int kernalIndex = simulationShader.FindKernel("Test");
        simulationShader.SetBuffer(kernalIndex, "pixels", pixelBuffer);
        simulationShader.SetInt("resolution", resolution);
        simulationShader.Dispatch(kernalIndex, 2 * resolution, resolution, 1);
        pixelBuffer.GetData(pixels);
        Debug.Log(pixels[0].height);
    }

    private void ReadPixelBuffer()
    {
        Pixel[] temp = new Pixel[2 * resolution * resolution];
        pixelBuffer.GetData(temp);
        for (int i = 0; i < temp.Length; i++)
        {
            pixels[i] = temp[i];
        }
        pixelBuffer.Release();
    }

    private void WritePixelBuffer()
    {
        //pixelBuffer.Dispose();

        pixelBuffer = new ComputeBuffer(pixels.GetLength(0) * pixels.GetLength(1), Pixel.SizeOf());
        pixelBuffer.SetData(pixels);
    }

    private void UpdateInput()
    {
        renderShader.SetInt("mapMode", (int)mapMode);
        renderShader.SetFloat("resolution", resolution);
        renderShader.SetFloat("deepestPoint", bathymetryMaxDepth);
        renderShader.SetFloat("highestPoint", topographyMaxHeight);
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
        //pixels = new Pixel[2 * resolution * resolution];

        //int kernelIndex = initialisationShader.FindKernel("CSMain");

        //pixelBuffer = new ComputeBuffer(pixels.Length, Pixel.SizeOf());
        //pixelBuffer.SetData(pixels);

        //initialisationShader.SetTexture(kernelIndex, "topography", topographyTexture);
        //initialisationShader.SetTexture(kernelIndex, "bathymetry", bathymetryTexture);
        //initialisationShader.SetBuffer(kernelIndex, "pixels", pixelBuffer);

        //initialisationShader.SetInt("resolution", resolution);
        //initialisationShader.SetInts("textureResolution", new int[] { topographyTexture.width, topographyTexture.height });
        //initialisationShader.SetFloat("maxHeight", topographyMaxHeight);
        //initialisationShader.SetFloat("maxDepth", bathymetryMaxDepth);

        //initialisationShader.Dispatch(kernelIndex, 2 * resolution, resolution, 1);

        //pixelBuffer.GetData(pixels);

        //pixelBuffer.Release();

        pixels = ProceduralTerrain.Generate(resolution, (int)bathymetryMaxDepth, (int)topographyMaxHeight);

        pixelBuffer = new ComputeBuffer(pixels.Length, Pixel.SizeOf());
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