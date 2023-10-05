using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private WorldData data;
    private Country[] countries; // all the countries in the world

    private List<WorldData> subscribers;
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
    public void Subscribe(WorldData subscriber) => subscribers.Add(subscriber);
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

    // Start is called before the first frame update
    void Start()
    {
        subscribers = new List<WorldData>();
        changes = new List<Change>();

        heights = LoadEarth.GenerateEarth(resolution, maxDepth, maxHeight, heightmap, bathymap, sealevelmask, maskThreshold);
        ids = new int[2 * resolution * resolution];
        LoadCountries();

        data = new WorldData(heights, ids, countries);
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
            foreach (WorldData subscriber in subscribers)
            {
                subscriber.UpdateBuffers(changes.ToArray(), updateShader);
            }
            changes = new List<Change>();
        }
    }

    // If the given position is outside the bounds returns a position inside
    private Vector2Int ValidatePosition(Vector2Int position)
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
        data.ReleaseBuffers();
    }

    private void OnValidate()
    {
        LoadCountries();
    }

    private void LoadCountries()
    {
        // Temporary, a file will be used in the future
        countries = new Country[2];
        countries[0] = new Country(0, new Vector3(0, 0, 0));
        countries[1] = new Country(1, new Vector3(1, 0, 0));
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