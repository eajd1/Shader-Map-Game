using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class World : MonoBehaviour
{
    // World contains all the data for the world and deals with updating it

    // Singleton
    private static World instance;
    public static World Instance { get { if (instance == null) { Debug.LogError("No GameManager"); } return instance; } }
    private void Awake() { if (instance != null && instance != this) { Destroy(this); } else { instance = this; } }

    [Range(128, 7327)] // Cant go higher than that for some reason
    [SerializeField] private int resolution;
    [SerializeField] private Texture2D heightmap;
    [SerializeField] private Texture2D bathymap;
    [SerializeField] private Texture2D sealevelmask;
    [SerializeField] private float maskThreshold;
    [SerializeField] private float maxHeight;
    [SerializeField] private float maxDepth;
    [SerializeField] private ComputeShader simulationShader;

    private float[] heights; // heights of each point in the world
    private ComputeBuffer heightBuffer;
    private int[] ids; // id of the owner of each point in the world
    private ComputeBuffer idBuffer;
    private Country[] countries; // all the countries in the world
    private ComputeBuffer countryColourBuffer;

    private bool simulate;

    public float MaxHeight { get { return maxHeight; } }
    public float MaxDepth { get { return maxDepth; } }
    public int WorldResolution { get { return resolution; } }

    public ComputeBuffer GetBuffer(string name)
    {
        return name switch
        {
            "height" => heightBuffer,
            "id" => idBuffer,
            "countryColour" => countryColourBuffer,
            _ => throw new System.Exception($"Invalid buffer name: '{name}'"),
        };
    }

    public void ToggleSimulation() => simulate = !simulate;

    // Start is called before the first frame update
    void Start()
    {
        heights = LoadEarth.GenerateEarth(resolution, maxDepth, maxHeight, heightmap, bathymap, sealevelmask, maskThreshold);
        ids = new int[2 * resolution * resolution];
        LoadCountries();

        MakeBuffers();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // MakeBuffers should be called last in Start()
    private void MakeBuffers()
    {
        heightBuffer = new ComputeBuffer(heights.Length, sizeof(float));
        heightBuffer.SetData(heights);

        idBuffer = new ComputeBuffer(ids.Length, sizeof(int));
        idBuffer.SetData(ids);

        countryColourBuffer = new ComputeBuffer(countries.Length, sizeof(float) * 3);
        countryColourBuffer.SetData(countries.Select(country => country.Colour).ToArray());
    }

    private void LoadCountries()
    {
        // Temporary, a file will be used in the future
        countries = new Country[2];
        countries[0] = new Country(0, new Vector3(0, 0, 0));
        countries[1] = new Country(1, new Vector3(1, 0, 0));
    }

    private void OnApplicationQuit()
    {
        heightBuffer.Release();
        idBuffer.Release();
        countryColourBuffer.Release();
    }
}
