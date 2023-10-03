using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CameraControls))]
public class PlayerController : MonoBehaviour
{
    // PlayerController deals with all player input except for camera movement

    private CameraControls controls;
    private MapMode mapMode;
    private Vector2Int cursorPosition;

    public Vector2Int CursorPosition { get { return cursorPosition; } }

    public MapMode GetMapMode() => mapMode;
    public CameraControls GetControls() => controls;

    // Start is called before the first frame update
    void Start()
    {
        controls = GetComponent<CameraControls>();
        mapMode = MapMode.Terrain;
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            World.Instance.ToggleSimulation();
        }
    }

    private void UpdateCursorPosition()
    {
        Vector3 mouseUV = Input.mousePosition;
        mouseUV.x = (mouseUV.x / Screen.width) * 2 - 1;
        mouseUV.y = (mouseUV.y / Screen.height) * 2 - 1;

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

        int x = (int)(UV.x * 2 * World.Instance.WorldResolution);
        int y = (int)(UV.y * World.Instance.WorldResolution);
        y = Mathf.Clamp(y, 0, World.Instance.WorldResolution - 1);

        cursorPosition = new Vector2Int(x, y);
    }
}

public enum MapMode
{
    Terrain,
    Ocean,
    Country
}
