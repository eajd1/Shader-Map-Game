using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControls : MonoBehaviour
{
    [SerializeField] private float movementMultiplier = 1;
    [SerializeField] private float maxAngle = 80;
    [SerializeField] private float zoomMultiplier = 1;
    [SerializeField] private AnimationCurve zoomCurve;
    [SerializeField] private float maxZoom = 0.01f;

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
        float angle;
        if (transform.eulerAngles.x > 90)
        {
            angle = transform.eulerAngles.x - 360;
        }
        else
        {
            angle = transform.eulerAngles.x;
        }
        if (angle > maxAngle && vertical > 0)
        {
            vertical = 0f;
        }
        if (angle < -maxAngle && vertical < 0)
        {
            vertical = 0f;
        }

        transform.LookAt(new Vector3(0, 0, 0));

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
        transform.position = transform.position.normalized;
    }

    public float GetZoom()
    {
        return zoom;
    }
}
