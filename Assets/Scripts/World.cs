using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using System;

// World contains all the data for the world and deals with updating it
public class World : MonoBehaviour
{
    // Singleton
    private static World instance;
    public static World Instance { get { if (instance == null) { Debug.LogError("No World"); } return instance; } }
    private void Awake() { if (instance != null && instance != this) { Destroy(this); } else { instance = this; } }

    [Range(128, 8192)]
    [SerializeField] private int resolution;
    [SerializeField] private Texture2D heightmap;
    [SerializeField] private Texture2D detailMap;
    [SerializeField] private float maxHeight;
    [SerializeField] private float minHeight;

    private Tile[] tiles; // Tiles of the world
    private WorldBufferData bufferData;
    private Country[] countries; // all the countries in the world
    private List<PlayerController> players;

    private bool changed;
    private bool simulate;

    public float MaxHeight { get { return maxHeight; } }
    public float MinHeight { get { return minHeight; } }
    public int WorldResolution { get { return resolution; } }
    public Tile[] Tiles { get { return tiles; } }
    public Country[] Countries { get { return countries; } }
    public WorldBufferData Buffers { get { return bufferData; } }

    public void ToggleSimulation() => simulate = !simulate;
    public Country GetCountry(int index) => countries[index];
    //public void AddChange(Change change) { lock (changes) { changes.Add(change); } }
    public Tile GetTile(Vector2Int position) => tiles[position.x * resolution + position.y];
    public float GetHeight(Vector2Int position) => tiles[position.x * resolution + position.y].height;
    public Country GetOwner(Vector2Int position) => countries[tiles[position.x * resolution + position.y].owner];
    public PlayerController GetPlayer(int index) => players.ToArray()[index];

    public void ChangeTile(Tile tile, Vector2Int position)
    {
        int index = position.x * resolution + position.y;
        tiles[index] = tile;
        bufferData.UpdateSingleTile(tiles[index], index / resolution, index % resolution);
    }

    public void AddPlayer(PlayerController player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
        }
    }

    // Instantly updates the buffer using the update single shader
    public void SetOwnerInstant(Vector2Int pos, uint owner)
    {
        int index = pos.x * resolution + pos.y;
        if (!(tiles[index].owner == owner) && !tiles[index].IsOcean())
        {
            lock (tiles)
                tiles[index].owner = owner;
            bufferData.UpdateSingleTile(tiles[index], index / resolution, index % resolution);
        }
    }

    // Updates the buffer when ready
    public void SetOwner(Vector2Int pos, uint owner)
    {
        int index = pos.x * resolution + pos.y;
        if (!(tiles[index].owner == owner) && !tiles[index].IsOcean())
        {
            lock (tiles)
                tiles[index].owner = owner;
            changed = true;
        }
    }

    public void SaveWorld(string name)
    {
        string path = $"{Application.dataPath}/saves/{name}";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        byte[] heights = new byte[2 * resolution * resolution * 4];
        for (int x = 0; x < 2 * resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                int index = x * resolution + y;
                byte[] bytes = BitConverter.GetBytes(tiles[index].height);
                heights[index * 4] = bytes[0];
                heights[index * 4 + 1] = bytes[1];
                heights[index * 4 + 2] = bytes[2];
                heights[index * 4 + 3] = bytes[3];
            }
        }
        File.WriteAllBytes($"{path}/heights.sav", heights);

        byte[] owners = new byte[2 * resolution * resolution * 4];
        for (int x = 0; x < 2 * resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                int index = x * resolution + y;
                byte[] bytes = BitConverter.GetBytes(tiles[index].owner);
                owners[index * 4] = bytes[0];
                owners[index * 4 + 1] = bytes[1];
                owners[index * 4 + 2] = bytes[2];
                owners[index * 4 + 3] = bytes[3];
            }
        }
        File.WriteAllBytes($"{path}/owners.sav", owners);

        byte[] details = new byte[2 * resolution * resolution * 4];
        for (int x = 0; x < 2 * resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                int index = x * resolution + y;
                byte[] bytes = BitConverter.GetBytes(tiles[index].details);
                details[index * 4] = bytes[0];
                details[index * 4 + 1] = bytes[1];
                details[index * 4 + 2] = bytes[2];
                details[index * 4 + 3] = bytes[3];
            }
        }
        File.WriteAllBytes($"{path}/details.sav", details);

        // Save info
        path = $"{path}/data.sav";
        FileStream file = File.Create(path);
        BinaryWriter writer = new BinaryWriter(file);
        writer.Write(BitConverter.IsLittleEndian);
        writer.Write(resolution);
        writer.Write(maxHeight);
        writer.Write(minHeight);
        writer.Close();
        file.Close();
    }

    public void LoadWorld(string name)
    {
        string path = $"{Application.dataPath}/saves/{name}";
        if (!Directory.Exists(path))
        {
            // Save doesnt exist
            Debug.Log($"Directory: '{path}' doesnt exist");
            return;
        }

        FileStream file = File.OpenRead($"{path}/data.sav");
        BinaryReader reader = new BinaryReader(file);
        bool littleEndian = reader.ReadBoolean();
        resolution = reader.ReadInt32();
        maxHeight = reader.ReadSingle();
        minHeight = reader.ReadSingle();

        byte[] heights = File.ReadAllBytes($"{path}/heights.sav");
        byte[] owners = File.ReadAllBytes($"{path}/owners.sav");
        byte[] details = File.ReadAllBytes($"{path}/details.sav");
        tiles = new Tile[2 * resolution * resolution];
        for (int x = 0; x < 2 * resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                int index = x * resolution + y;

                byte[] bytes = new byte[4];
                Array.Copy(heights, index * 4, bytes, 0, 4);
                if (BitConverter.IsLittleEndian != littleEndian)
                    Array.Reverse(bytes);
                float height = BitConverter.ToSingle(bytes);

                Array.Copy(owners, index * 4, bytes, 0, 4);
                if (BitConverter.IsLittleEndian != littleEndian)
                    Array.Reverse(bytes);
                uint owner = BitConverter.ToUInt32(bytes);
                Array.Copy(details, index * 4, bytes, 0, 4);
                if (BitConverter.IsLittleEndian != littleEndian)
                    Array.Reverse(bytes);
                uint detail = BitConverter.ToUInt32(bytes);

                tiles[index] = new Tile(height, owner, detail);
            }
        }

        bufferData.ReleaseBuffers();
        bufferData = new WorldBufferData(tiles, countries);

        reader.Close();
        file.Close();

        foreach (PlayerController player in players)
            player.UpdateBuffers();
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
        tiles = LoadPlanet.GenerateEarthShader(resolution, maxHeight, minHeight, heightmap, detailMap);
        //tiles = LoadPlanet.GenerateEarth(resolution, maxHeight, minHeight, heightmap, detailMap, 0.5f);
        //tiles = LoadPlanet.GeneratePlanet(resolution, minHeight, maxHeight, 1);
        LoadCountries();

        players = new List<PlayerController>();

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
        for (uint i = 0; i < lines.Length; i++)
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