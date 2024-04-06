using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CameraControls))]
[RequireComponent(typeof(RenderWorld))]
[RequireComponent(typeof(UIManager))]
public class PlayerController : MonoBehaviour
{
    // PlayerController deals with all player input except for camera movement

    private CameraControls controls;
    private RenderWorld renderWorld;
    private MapMode mapMode;
    private Vector2Int cursorPosition;
    private Country country;
    private UIManager uiManager;

    public Vector2Int CursorPosition { get { return cursorPosition; } }

    public MapMode GetMapMode() => mapMode;
    public CameraControls GetControls() => controls;
    public void SelectCountry(string countryName)
    {
        foreach (Country country in World.Instance.Countries)
        {
            if (country.name.Equals(countryName))
            {
                this.country = country;
            }
        }
    }

    public void UpdateBuffers()
    {
        renderWorld.UpdateBuffers();
    }

    // Start is called before the first frame update
    void Start()
    {
        controls = GetComponent<CameraControls>();
        renderWorld = GetComponent<RenderWorld>();
        uiManager = GetComponent<UIManager>();
        mapMode = MapMode.Terrain;

        country = World.Instance.GetCountry(0);
        World.Instance.AddPlayer(this);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCursorPosition();

        // Input Changes

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
        if (Input.GetKeyDown(KeyCode.R))
        {
            mapMode = MapMode.Borders;
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            mapMode = MapMode.SimpleCountry;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            World.Instance.ToggleSimulation();
        }

        if (Input.GetButton(Inputs.LMB) && !uiManager.CursorInCollider())
        {
            World.Instance.SetOwner(GetIndex(), country.ID);
        }

        if (Input.GetButton(Inputs.RMB))
        {
            World.Instance.SetOwner(GetIndex(), 0);
        }

        if (Input.GetButtonDown(Inputs.MMB))
        {
            World.Instance.SetOwnerFill(cursorPosition, country.ID);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            World.Instance.LoadWorld("8192");
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            World.Instance.SaveWorld("8192");
        }
    }

    private void UpdateCursorPosition()
    {
        Vector2 cursor = controls.GetCursorUV();

        int x = (int)(cursor.x * 2 * World.Instance.WorldResolution);
        int y = (int)(cursor.y * World.Instance.WorldResolution);
        y = Mathf.Clamp(y, 0, World.Instance.WorldResolution - 1);

        cursorPosition = World.Instance.ValidatePosition(new Vector2Int(x, y));
    }

    private int GetIndex()
    {
        return cursorPosition.x * World.Instance.WorldResolution + cursorPosition.y;
    }
}

public enum MapMode
{
    Terrain,
    Ocean,
    Country,
    Borders,
    SimpleCountry
}
