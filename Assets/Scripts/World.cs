using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;

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
    private Change[] changes;
    private bool changed;
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
    //public void AddChange(Change change) { lock (changes) { changes.Add(change); } }
    public float GetHeight(Vector2Int position) => heights[position.x * resolution + position.y];
    public Country GetOwner(Vector2Int position) => countries[ids[position.x * resolution + position.y]];

    public void SetOwner(int index, int owner)
    {
        if (!(ids[index] == owner))
        {
            lock (ids)
                ids[index] = owner;
            //lock (changes)
            //    changes.Add(new Change(index, 0, owner));
            lock (changes)
                changes[index] = new Change(0, owner);
            changed = true;
        }
    }

    public void SetOwnerFill(Vector2Int start, int owner)
    {
        Thread thread = new Thread(new ThreadStart(() =>
        {
            if (heights[start.x * resolution + start.y] < 0)
                return;

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            List<int> visited = new List<int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Vector2Int pos = queue.Dequeue();
                int index = pos.x * resolution + pos.y;
                SetOwner(index, owner);

                Vector2Int newPos = ValidatePosition(new Vector2Int(pos.x + 1, pos.y));
                index = newPos.x * resolution + newPos.y;
                if (heights[index] > 0 && ids[index] == 0 && !visited.Contains(index))
                {
                    queue.Enqueue(new Vector2Int(newPos.x, newPos.y));
                    visited.Add(index);
                }
                newPos = ValidatePosition(new Vector2Int(pos.x - 1, pos.y));
                index = newPos.x * resolution + newPos.y;
                if (heights[index] > 0 && ids[index] == 0 && !visited.Contains(index))
                {
                    queue.Enqueue(new Vector2Int(newPos.x, newPos.y));
                    visited.Add(index);
                }
                newPos = ValidatePosition(new Vector2Int(pos.x, pos.y + 1));
                index = newPos.x * resolution + newPos.y;
                if (heights[index] > 0 && ids[index] == 0 && !visited.Contains(index))
                {
                    queue.Enqueue(new Vector2Int(newPos.x, newPos.y));
                    visited.Add(index);
                }
                newPos = ValidatePosition(new Vector2Int(pos.x, pos.y - 1));
                index = newPos.x * resolution + newPos.y;
                if (heights[index] > 0 && ids[index] == 0 && !visited.Contains(index))
                {
                    queue.Enqueue(new Vector2Int(newPos.x, newPos.y));
                    visited.Add(index);
                }
            }
        }));
        thread.Start();
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

    // Start is called before the first frame update
    void Start()
    {
        subscribers = new List<WorldBufferData>();
        changes = new Change[2 * resolution * resolution];

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
        lock (changes)
        {
            if (changed)
            {
                // Find the centre of every country
                //for (int i = 1; i < countries.Length; i++)
                //    CalculateCountryCentre(i); // In future do this only when border changes are confirmed


                bufferData.UpdateBuffers(changes, updateShader);
                foreach (WorldBufferData subscriber in subscribers)
                {
                    subscriber.UpdateBuffers(changes, updateShader); // Uses a lot of bandwidth (thinking ahead for possible multiplayer)
                }
                changed = false;
            }
        }
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

    private void CalculateCountryCentre(int countryID)
    {
        Vector2 centre = new Vector2(0, 0);
        int count = 0;
        for (int i = 0; i < ids.Length; i++)
        {
            if (ids[i] == countryID)
            {
                int x = i / resolution;
                int y = i % resolution;

                centre += new Vector2(x, y);
                count++;
            }
        }
        if (count != 0)
            countries[countryID].namePoint = centre / count;
        else
            countries[countryID].namePoint = new Vector2(-999, -999);
    }
}

public struct Change
{
    float deltaHeight;
    int owner;

    public Change(float deltaHeight, int owner)
    {
        this.deltaHeight = deltaHeight;
        this.owner = owner;
    }

    public static int SizeOf()
    {
        return sizeof(int) + sizeof(float);
    }
}