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
    [SerializeField] private ComputeShader updateAllShader; // The shader for updating the whole screen of changes
    [SerializeField] private ComputeShader updateSingleShader; // The shader for updating a single change

    private Tile[] tiles; // Tiles of the world
    private WorldBufferData bufferData;
    private Country[] countries; // all the countries in the world

    private bool changed;
    private bool simulate;

    public float MaxHeight { get { return maxHeight; } }
    public float MaxDepth { get { return maxDepth; } }
    public int WorldResolution { get { return resolution; } }
    public Tile[] Tiles { get { return tiles; } }
    public Country[] Countries { get { return countries; } }
    public WorldBufferData Buffers { get { return bufferData; } }
    public ComputeShader UpdateAllShader { get { return updateAllShader; } }
    public ComputeShader UpdateSingleShader { get { return updateSingleShader; } }

    public void ToggleSimulation() => simulate = !simulate;
    public Country GetCountry(int index) => countries[index];
    //public void AddChange(Change change) { lock (changes) { changes.Add(change); } }
    public float GetHeight(Vector2Int position) => tiles[position.x * resolution + position.y].height;
    public Country GetOwner(Vector2Int position) => countries[tiles[position.x * resolution + position.y].owner];

    public void SetOwner(int index, int owner)
    {
        if (!(tiles[index].owner == owner) && tiles[index].height > 0)
        {
            lock (tiles)
                tiles[index].owner = owner;
            bufferData.UpdateSingleTile(tiles[index], index / resolution, index % resolution);
        }
    }

    public void SetOwnerFill(Vector2Int start, int owner)
    {
        Thread thread = new Thread(new ThreadStart(() =>
        {
            if (tiles[start.x * resolution + start.y].height < 0)
                return;

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            List<int> visited = new List<int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Vector2Int pos = queue.Dequeue();
                int index = pos.x * resolution + pos.y;
                if (!(tiles[index].owner == owner))
                {
                    lock (tiles)
                        tiles[index].owner = owner;
                    changed = true;
                }

                Vector2Int newPos = ValidatePosition(new Vector2Int(pos.x + 1, pos.y));
                index = newPos.x * resolution + newPos.y;
                if (tiles[index].height > 0 && tiles[index].owner == 0 && !visited.Contains(index))
                {
                    queue.Enqueue(new Vector2Int(newPos.x, newPos.y));
                    visited.Add(index);
                }
                newPos = ValidatePosition(new Vector2Int(pos.x - 1, pos.y));
                index = newPos.x * resolution + newPos.y;
                if (tiles[index].height > 0 && tiles[index].owner == 0 && !visited.Contains(index))
                {
                    queue.Enqueue(new Vector2Int(newPos.x, newPos.y));
                    visited.Add(index);
                }
                newPos = ValidatePosition(new Vector2Int(pos.x, pos.y + 1));
                index = newPos.x * resolution + newPos.y;
                if (tiles[index].height > 0 && tiles[index].owner == 0 && !visited.Contains(index))
                {
                    queue.Enqueue(new Vector2Int(newPos.x, newPos.y));
                    visited.Add(index);
                }
                newPos = ValidatePosition(new Vector2Int(pos.x, pos.y - 1));
                index = newPos.x * resolution + newPos.y;
                if (tiles[index].height > 0 && tiles[index].owner == 0 && !visited.Contains(index))
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

        foreach (Tile tile in tiles)
        {
            writer.Write(tile.height);
            writer.Write(tile.owner);
        }

        writer.Close();
        file.Close();
    }

    public void LoadWorld(string name)
    {
        string path = $"{Application.dataPath}/saves";
        if (!Directory.Exists(path))
        {
            Debug.Log($"Directory: '{path}' doesnt exist");
            return;
        }

        path = $"{path}/{name}.sav";
        FileStream file = File.OpenRead(path);
        BinaryReader reader = new BinaryReader(file);

        resolution = reader.ReadInt32();
        maxHeight = reader.ReadSingle();
        maxDepth = reader.ReadSingle();

        tiles = new Tile[2 * resolution * resolution];
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = new Tile(reader.ReadSingle(), reader.ReadInt32());
        }

        bufferData = new WorldBufferData(tiles, countries);

        reader.Close();
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
        tiles = LoadEarth.GenerateEarth(resolution, maxDepth, maxHeight, heightmap, bathymap, sealevelmask, maskThreshold);
        LoadCountries();

        bufferData = new WorldBufferData(tiles, countries);
    }

    // Update is called once per frame
    void Update()
    {
        SendChanges();
    }

    private void SendChanges()
    {
        if (changed)
        {
            lock (tiles)
            {
                // Find the centre of every country
                //for (int i = 1; i < countries.Length; i++)
                //    CalculateCountryCentre(i); // In future do this only when border changes are confirmed


                bufferData.UpdateBuffers(tiles);
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
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i].owner == countryID)
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

public struct Tile
{
    public float height;
    public int owner;

    public Tile(float height, int owner)
    {
        this.height = height;
        this.owner = owner;
    }

    public static int SizeOf()
    {
        return sizeof(int) + sizeof(float);
    }
}