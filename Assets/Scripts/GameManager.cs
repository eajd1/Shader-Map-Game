using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CameraControls))]
public class GameManager : MonoBehaviour
{
    [SerializeField] private ComputeShader sphereRenderShader;
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
        transform.position = new Vector3(0, 0, 1);

        mapMode = MapMode.Terrain;

        InitialiseShader();
    }

    private void InitialiseShader()
    {
        pixelBuffer = new ComputeBuffer(pixels.Length, Pixel.SizeOf());
        pixelBuffer.SetData(pixels);

        int kernelIndex = sphereRenderShader.FindKernel("CSMain");

        sphereRenderShader.SetTexture(kernelIndex, "result", renderTexture);
        sphereRenderShader.SetBuffer(kernelIndex, "pixels", pixelBuffer);
        sphereRenderShader.SetInts("screenResolution", new int[] { screenResolution.width, screenResolution.height });

        UpdateInput();
    }

    void Update()
    {
        GetInputs();

        UpdateInput();

        //int kernelIndex = combine.FindKernel("CSMain");
        //combine.SetTexture(kernelIndex, "a", background);
        //combine.SetTexture(kernelIndex, "b", renderTexture);
        //combine.Dispatch(kernelIndex, screenResolution.width, screenResolution.height, 1);
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

        int sizeOfPixel = sizeof(float) + sizeof(uint);
        pixelBuffer = new ComputeBuffer(pixels.GetLength(0) * pixels.GetLength(1), sizeOfPixel);
        pixelBuffer.SetData(pixels);
    }

    private void UpdateInput()
    {
        sphereRenderShader.SetInt("mapMode", (int)mapMode);
        sphereRenderShader.SetFloat("resolution", resolution);
        sphereRenderShader.SetFloat("deepestPoint", bathymetryMaxDepth);
        sphereRenderShader.SetFloat("highestPoint", topographyMaxHeight);
        sphereRenderShader.SetFloat("lowestPoint", 0);
        Vector3 cameraPos = transform.position.normalized;
        sphereRenderShader.SetFloats("cameraPosition", new float[] { cameraPos.x, cameraPos.y, cameraPos.z });
        sphereRenderShader.SetFloat("zoom", controls.GetZoom());

        int kernelIndex = sphereRenderShader.FindKernel("CSMain");
        sphereRenderShader.Dispatch(kernelIndex, screenResolution.width, screenResolution.height, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(renderTexture, destination);
    }

    private void MakePixels()
    {
        pixels = new Pixel[2 * resolution * resolution];

        int kernelIndex = initialisationShader.FindKernel("CSMain");

        pixelBuffer = new ComputeBuffer(pixels.Length, Pixel.SizeOf());
        pixelBuffer.SetData(pixels);

        initialisationShader.SetTexture(kernelIndex, "topography", topographyTexture);
        initialisationShader.SetTexture(kernelIndex, "bathymetry", bathymetryTexture);
        initialisationShader.SetBuffer(kernelIndex, "pixels", pixelBuffer);

        initialisationShader.SetInt("resolution", resolution);
        initialisationShader.SetInts("textureResolution", new int[] { topographyTexture.width, topographyTexture.height });
        initialisationShader.SetFloat("maxHeight", topographyMaxHeight);
        initialisationShader.SetFloat("maxDepth", bathymetryMaxDepth);

        initialisationShader.Dispatch(kernelIndex, 2 * resolution, resolution, 1);

        pixelBuffer.GetData(pixels);

        pixelBuffer.Release();
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