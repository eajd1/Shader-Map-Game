using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneCameraControls : CameraControls
{
    [SerializeField] private float movementMultiplier = 1;
    [SerializeField] private float fasterMovementMultiplier = 10;
    [SerializeField] private float zoomMultiplier = 1;
    [SerializeField] private AnimationCurve zoomCurve;
    [SerializeField] private float maxZoom = 0.01f;
    [SerializeField] private float maxMovement = 1000f;

    private float zoom;

    // Start is called before the first frame update
    void Start()
    {
        zoom = 1;
        transform.position = new Vector3(maxMovement / 2f, maxMovement / 2f);
    }

    // Update is called once per frame
    void Update()
    {
        // Camera Inputs
        float movementSpeed = movementMultiplier;
        if (Input.GetButton(Inputs.Shift))
        {
            movementSpeed = fasterMovementMultiplier;
        }

        // Horizontal
        float horizontal = Input.GetAxis(Inputs.Horizontal) * movementSpeed * Time.deltaTime;

        // Vertical
        float vertical = Input.GetAxis(Inputs.Vertical) * movementSpeed * Time.deltaTime;

        // Zoom
        zoom += -Input.GetAxis(Inputs.Scroll) * zoomMultiplier * zoomCurve.Evaluate(zoom) * Mathf.Clamp(Time.deltaTime, 0, 0.016f);

        if (zoom < maxZoom)
        {
            zoom = maxZoom;
        }
        if (zoom > 1)
        {
            zoom = 1;
        }

        // Move
        transform.Translate(new Vector3(horizontal * Mathf.Sqrt(zoom), vertical * Mathf.Sqrt(zoom), 0));

        Vector3 pos = transform.position;

        if (transform.position.x > maxMovement)
        {
            pos.x = transform.position.x - maxMovement;
        }
        if (transform.position.x < 0)
        {
            pos.x = transform.position.x + maxMovement;
        }
        if (transform.position.y > maxMovement)
        {
            pos.y = maxMovement;
        }
        if (transform.position.y < 0)
        {
            pos.y = 0;
        }
        transform.position = pos;
    }

    public override float GetZoom()
    {
        return zoom;
    }

    public override Vector2 GetCameraUV()
    {
        // -1 to 1 (technically not uv i know)
        float u = (transform.position.x / maxMovement) * 2 - 1;
        float v = (transform.position.y / maxMovement) * 2 - 1;
        return new Vector2(u, v);
    }

    public override Vector2 GetCursorUV()
    {
        Vector3 mouseUV = Input.mousePosition;
        mouseUV.x = (mouseUV.x / Screen.width) * 2 - 1;
        mouseUV.y = (mouseUV.y / Screen.height) * 2 - 1;

        Vector2 UV = new Vector2(mouseUV.x, mouseUV.y);
        UV.Scale(new Vector2(zoom, zoom));
        UV.x *= Screen.width / (float)Screen.height / 2.0f;
        UV += GetCameraUV();

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

        return UV; // actually 0 to 1
    }
}
