using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneCameraControls : CameraControls
{
    [SerializeField] private float movementMultiplier = 1;
    [SerializeField] private float zoomMultiplier = 1;
    [SerializeField] private AnimationCurve zoomCurve;
    [SerializeField] private float maxZoom = 0.01f;
    [SerializeField] private float maxMovement = 1000f;

    private float zoom;

    // Start is called before the first frame update
    void Start()
    {
        zoom = 1;
    }

    // Update is called once per frame
    void Update()
    {
        // Camera Inputs
        // Horizontal
        float horizontal = Input.GetAxis(Inputs.Horizontal) * movementMultiplier * Time.deltaTime;

        // Vertical
        float vertical = Input.GetAxis(Inputs.Vertical) * movementMultiplier * Time.deltaTime;

        // Zoom
        zoom += -Input.GetAxis(Inputs.Scroll) * zoomMultiplier * Time.deltaTime * zoomCurve.Evaluate(zoom);

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

    public override Vector2 GetUV()
    {
        float u = transform.position.x / maxMovement;
        float v = transform.position.y / maxMovement;
        return new Vector2(u, v);
    }
}
