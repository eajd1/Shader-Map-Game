using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

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
    [SerializeField] private ComputeShader updateShader;

    private float[] heights; // heights of each point in the world
    private int[] ids; // id of the owner of each point in the world
    private WorldBufferData bufferData;
    private Country[] countries; // all the countries in the world

    private List<WorldBufferData> subscribers;
    private List<Change> changes;
    private bool simulate;

    public float MaxHeight { get { return maxHeight; } }
    public float MaxDepth { get { return maxDepth; } }
    public int WorldResolution { get { return resolution; } }
    public float[] Heights { get { return heights; } }
    public int[] IDs { get { return ids; } }
    public Country[] Countries { get { return countries; } }

    public void ToggleSimulation() => simulate = !simulate;
    public Country GetCountry(int index) => countries[index];
    public void Subscribe(WorldBufferData subscriber) => subscribers.Add(subscriber);
    public void GetChange(Change change) { lock (changes) { changes.Add(change); } }
    public void SetOwner(int index, int owner)
    {
        if (!(ids[index] == owner))
        {
            ids[index] = owner;
            changes.Add(new Change(index, heights[index], owner));
        }
    }
    public void SetOwnerFill(int startX, int startY, int owner)
    {

    }
    public float GetHeight(Vector2Int position) => heights[position.x * resolution + position.y];
    public Country GetOwner(Vector2Int position) => countries[ids[position.x * resolution + position.y]];

    // Start is called before the first frame update
    void Start()
    {
        subscribers = new List<WorldBufferData>();
        changes = new List<Change>();

        heights = LoadEarth.GenerateEarth(resolution, maxDepth, maxHeight, heightmap, bathymap, sealevelmask, maskThreshold);
        ids = new int[2 * resolution * resolution];
        LoadCountries();

        bufferData = new WorldBufferData(heights, ids, countries);
    }

    // Update is called once per frame
    void Update()
    {
        SendChanges();
    }

    private void SendChanges()
    {
        if (changes.Count > 0)
        {
            bufferData.UpdateBuffers(changes.ToArray(), updateShader);
            foreach (WorldBufferData subscriber in subscribers)
            {
                subscriber.UpdateBuffers(changes.ToArray(), updateShader);
            }
            changes = new List<Change>();
        }
    }

    // If the given position is outside the bounds returns a position inside
    public Vector2Int ValidatePosition(Vector2Int position)
    {
        int x = position.x;
        int y = position.y;

        x %= resolution * 2;
        if (x < 0)
        {
            x = (resolution * 2) + x;
        }

        // 2D map sphere wrapping the y is funny
        if (y < 0)
        {
            y = 0;

            if (x < resolution)
            {
                x = resolution + x;
            }
            else
            {
                x -= resolution;
            }
        }
        else if (y > resolution - 1)
        {
            y = resolution - 1;

            if (x < resolution)
            {
                x = resolution + x;
            }
            else
            {
                x -= resolution;
            }
        }

        return new Vector2Int(x, y);
    }

    private void OnApplicationQuit()
    {
        bufferData.ReleaseBuffers();
    }

    private void LoadCountries()
    {
        var countryFile = Resources.Load<TextAsset>("countries");
        string[] lines = countryFile.text.Split("\n");
        countries = new Country[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(",");
            countries[i] = new Country(i, new Vector3(float.Parse(values[1]), float.Parse(values[2]), float.Parse(values[3])), values[0]);
        }
    }

    public void SaveWorld(string name)
    {
        string path = $"{Application.dataPath}/saves";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        path = $"{path}/{name}.sav";
        FileStream file = File.Create(path);
        BinaryWriter writer = new BinaryWriter(file);

        writer.Write(resolution);
        writer.Write(maxHeight);
        writer.Write(maxDepth);

        foreach (float height in heights)
        {
            writer.Write(height);
        }

        foreach (int owner in ids)
        {
            writer.Write(owner);
        }

        foreach (Country country in countries)
        {
            writer.Write(country.ToString());
        }

        writer.Close();
        file.Close();
    }
}

public struct Change
{
    int index;
    float height;
    int owner;

    public Change(int index, float height, int owner)
    {
        this.index = index;
        this.height = height;
        this.owner = owner;
    }

    public static int SizeOf()
    {
        return 2 * sizeof(int) + sizeof(float);
    }
}