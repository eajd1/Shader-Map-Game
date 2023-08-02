using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ComputeShaderTest : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private ComputeShader sphereShader;
    [SerializeField] private ComputeShader initialisationShader;
    [Range(128, 7327)] // Cant go higher than that for some reason
    [SerializeField] private int resolution;

    [SerializeField] private Texture2D topographyTexture;
    [SerializeField] private float topographyMaxHeight;
    [SerializeField] private Texture2D bathymetryTexture;
    [SerializeField] private float bathymetryMaxDepth;

    [SerializeField] private float movementMultiplier = 1;
    [SerializeField] private float zoomMultiplier = 1;
    [SerializeField] private AnimationCurve zoomCurve;

    private float xOffset;
    private float yOffset;
    private float zoom;

    private Pixel[] pixels;
    private RenderTexture renderTexture;
    private ComputeBuffer pixelBuffer;

    private Resolution screenResolution;

    [SerializeField] private bool isSphere;

    private ComputeShader activeShader;

    public enum MapMode
    {
        Terrain,
        Ocean
    }

    private MapMode mapMode;

    void Start()
    {
        screenResolution = Screen.currentResolution;

        renderTexture = new RenderTexture(screenResolution.width, screenResolution.height, 1);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        
        MakePixels();

        xOffset = 0;
        yOffset = 0;
        zoom = 1;

        mapMode = MapMode.Terrain;

        if (isSphere)
        {
            activeShader = sphereShader;
        }
        else
        {
            activeShader = computeShader;
        }

        InitialiseShader();
    }

    private void InitialiseShader()
    {
        pixelBuffer = new ComputeBuffer(pixels.Length, Pixel.SizeOf());
        pixelBuffer.SetData(pixels);

        int kernalIndex = activeShader.FindKernel("CSMain");

        activeShader.SetTexture(kernalIndex, "result", renderTexture);
        activeShader.SetBuffer(kernalIndex, "pixels", pixelBuffer);
        activeShader.SetInts("screenResolution", new int[] { screenResolution.width, screenResolution.height });

        UpdateInput();
    }

    void Update()
    {
        GetInputs();

        UpdateInput();
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
        activeShader.SetInt("mapMode", (int)mapMode);
        activeShader.SetFloat("resolution", resolution);
        activeShader.SetFloat("deepestPoint", bathymetryMaxDepth);
        activeShader.SetFloat("highestPoint", topographyMaxHeight);
        activeShader.SetFloat("lowestPoint", 0);
        activeShader.SetFloat("xOffset", xOffset);
        activeShader.SetFloat("yOffset", yOffset);
        activeShader.SetFloat("zoom", zoom);

        int kernalIndex = activeShader.FindKernel("CSMain");
        activeShader.Dispatch(kernalIndex, screenResolution.width, screenResolution.height, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(renderTexture, destination);
    }

    private void MakePixels()
    {
        pixels = new Pixel[2 * resolution * resolution];

        int kernalIndex = initialisationShader.FindKernel("CSMain");

        pixelBuffer = new ComputeBuffer(pixels.Length, Pixel.SizeOf());
        pixelBuffer.SetData(pixels);

        RenderTexture test = new RenderTexture(topographyTexture.width, topographyTexture.height, 1);
        test.enableRandomWrite = true;

        initialisationShader.SetTexture(kernalIndex, "topography", topographyTexture);
        initialisationShader.SetTexture(kernalIndex, "bathymetry", bathymetryTexture);
        initialisationShader.SetBuffer(kernalIndex, "pixels", pixelBuffer);

        initialisationShader.SetInt("resolution", resolution);
        initialisationShader.SetInts("textureResolution", new int[] { topographyTexture.width, topographyTexture.height });
        initialisationShader.SetFloat("maxHeight", topographyMaxHeight);
        initialisationShader.SetFloat("maxDepth", bathymetryMaxDepth);

        initialisationShader.Dispatch(kernalIndex, 2 * resolution, resolution, 1);

        pixelBuffer.GetData(pixels);

        pixelBuffer.Release();

        //for (int x = 0; x < 2 * resolution; x++)
        //{
        //    for (int y = 0; y < resolution; y++)
        //    {
        //        //float texturePixel = GetTextureColor(topographyTexture, x, y, resolution).r;
        //        //float height = Mathf.Lerp(0, topographyMaxHeight, texturePixel);

        //        //texturePixel = GetTextureColor(bathymetryTexture, x, y, resolution).r;
        //        //height += Mathf.Lerp(bathymetryMaxDepth, 0, texturePixel);

        //        float height = pixels[y + x * resolution].height;

        //        bool ocean = false;
        //        if (height < 0)
        //        {
        //            ocean = true;
        //        }

        //        pixels[x * resolution + y] = new Pixel(height, ocean);
        //    }
        //}
    }

    private void GetInputs()
    {
        // Camera Inputs

        xOffset += Input.GetAxis(Inputs.Horizontal) * movementMultiplier * Time.deltaTime * (Mathf.Log10(0.9f * zoom + 0.1f) + 1);

        if (xOffset > 1)
        {
            xOffset = -1;
        }
        if (xOffset < -1)
        {
            xOffset = 1;
        }

        yOffset += Input.GetAxis(Inputs.Vertical) * movementMultiplier * Time.deltaTime * (Mathf.Log10(0.9f * zoom + 0.1f) + 1);

        if (yOffset > 1)
        {
            yOffset = 1;
        }
        if (yOffset < -1)
        {
            yOffset = -1;
        }

        zoom += -Input.GetAxis(Inputs.Scroll) * zoomMultiplier * Time.deltaTime * zoomCurve.Evaluate(zoom);

        if (zoom > 1)
        {
            zoom = 1;
        }
        if (zoom < 0.001f)
        {
            zoom = 0.001f;
        }

        if (Input.GetButtonDown(Inputs.Space))
        {
            Save();
        }

        if (Input.GetButtonDown(Inputs.LMB))
        {
            //ReadPixelBuffer();
            //WritePixelBuffer();
        }

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

    private Color GetTextureColor(Texture2D texture, int x, int y, int resolution)
    {
        int textureX = (int)((x / (float)(2 * resolution)) * texture.width);
        int textureY = (int)((y / (float)resolution) * texture.height);

        return texture.GetPixel(textureX, textureY);
    }

    private void Save()
    {
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height);
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        byte[] oceanRiverRoads = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/world.png", oceanRiverRoads);
    }

    private void OnApplicationQuit()
    {
        pixelBuffer.Dispose();
    }
}