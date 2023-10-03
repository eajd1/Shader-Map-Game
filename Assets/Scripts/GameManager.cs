using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CameraControls))]
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("No GameManager");
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    [SerializeField] private ComputeShader renderShader;
    [SerializeField] private ComputeShader initialisationShader;
    [Range(128, 7327)] // Cant go higher than that for some reason
    [SerializeField] private int resolution;

    [SerializeField] private float maxHeight;
    [SerializeField] private float maxDepth;

    [SerializeField] private Texture2D heightmap;
    [SerializeField] private Texture2D bathymap;
    [SerializeField] private Texture2D sealevelmask;
    [SerializeField] private float maskThreshold;

    private CameraControls controls;
    private Vector2Int cursorPosition;

    private float[] heights;
    private ComputeBuffer heightBuffer;
    private RenderTexture renderTexture;
    private Resolution screenResolution;

    private CountryManager countryManager;
    private int[] countryids; // an id for each pixel in world
    private ComputeBuffer idBuffer;
    private ComputeBuffer countryColoursBuffer;

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

        countryManager = new CountryManager();
        countryids = new int[2 * resolution * resolution];

        MakePixels();

        mapMode = MapMode.Terrain;

        InitialiseShader();
    }

    private void InitialiseShader()
    {
        int kernelIndex = renderShader.FindKernel("CSMain");

        renderShader.SetTexture(kernelIndex, "result", renderTexture);
        renderShader.SetBuffer(kernelIndex, "heights", heightBuffer);
        renderShader.SetInts("screenResolution", new int[] { screenResolution.width, screenResolution.height });

        UpdateCountryBuffers();
        RenderWorld();
    }

    private void UpdateCountryBuffers()
    {
        if (idBuffer != null)
        {
            idBuffer.Release();
        }
        idBuffer = new ComputeBuffer(countryids.Length, sizeof(int));
        idBuffer.SetData(countryids);

        if (countryColoursBuffer != null)
        {
            countryColoursBuffer.Release();
        }
        Vector3[] colours = countryManager.GetColours();
        countryColoursBuffer = new ComputeBuffer(colours.Length, sizeof(float) * 3);
        countryColoursBuffer.SetData(colours);

        int kernelIndex = renderShader.FindKernel("CSMain");
        renderShader.SetBuffer(kernelIndex, "countryIds", idBuffer);
        renderShader.SetBuffer(kernelIndex, "countryColours", countryColoursBuffer);
    }

    void Update()
    {
        GetInputs();
        UpdateCursorPosition();

        if (simulate)
        {
            //UpdateWorld();
        }

        RenderWorld();
    }

    private void UpdateWorld()
    {
        int kernalIndex = simulationShader.FindKernel("Test");
        simulationShader.SetBuffer(kernalIndex, "pixels", heightBuffer);
        simulationShader.SetInt("resolution", resolution);
        simulationShader.Dispatch(kernalIndex, 2 * resolution, resolution, 1);
    }

    private void RenderWorld()
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
        heights = LoadEarth.GenerateEarth(resolution, maxDepth, maxHeight, heightmap, bathymap, sealevelmask, maskThreshold);

        heightBuffer = new ComputeBuffer(heights.Length, sizeof(float));
        heightBuffer.SetData(heights);
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

        if (Input.GetKeyDown(KeyCode.F))
        {
            mapMode = MapMode.Country;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            simulate = !simulate;
        }

        if (Input.GetButton(Inputs.LMB))
        {
            Vector2Int pos = GetCursorIndex();
            if (countryids[pos.x * resolution + pos.y] != 1)
            {
            countryids[pos.x * resolution + pos.y] = 1;
            UpdateCountryBuffers();
            }
        }
        if (Input.GetButton(Inputs.RMB))
        {
            Vector2Int pos = GetCursorIndex();
            if (countryids[pos.x * resolution + pos.y] != 0)
            {
                countryids[pos.x * resolution + pos.y] = 0;
                UpdateCountryBuffers();
            }
        }
    }

    public void SetPixelOwner(Vector2Int position, int id)
    {
        countryids[position.x * resolution + position.y] = id;
        UpdateCountryBuffers();
    }

    private void OnApplicationQuit()
    {
        //pixelBuffer.Dispose();
        heightBuffer.Release();
        idBuffer.Release();
        countryColoursBuffer.Release();
    }

    private void UpdateCursorPosition()
    {
        Vector3 mouseUV = Input.mousePosition;
        mouseUV.x = (mouseUV.x / screenResolution.width) * 2 - 1;
        mouseUV.y = (mouseUV.y / screenResolution.height) * 2 - 1;

        Vector2 UV = new Vector2(mouseUV.x, mouseUV.y);
        UV.Scale(new Vector2(controls.GetZoom(), controls.GetZoom()));
        UV += controls.GetUV();

        while (UV.x > 1)
        {
            UV.x = -1 + (UV.x - 1);
        }
        while (UV.x < -1)
        {
            UV.x = 1 + (UV.x + 1);
        }
        if (UV.y > 1)
        {
            UV.y = 1;
        }
        if (UV.y < -1)
        {
            UV.y = -1;
        }

        UV.x = (UV.x + 1) / 2;
        UV.y = (UV.y + 1) / 2;

        int x = (int)(UV.x * 2 * resolution);
        int y = (int)(UV.y * resolution);
        y = Mathf.Clamp(y, 0, resolution - 1);

        cursorPosition = new Vector2Int(x, y);
    }

    public Vector2Int GetCursorIndex()
    {
        return cursorPosition;
    }

    public float GetHeightAtCursor()
    {
        Vector2Int pos = GetCursorIndex();
        int index = pos.x * resolution + pos.y;
        return heights[index];
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