using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CameraControls : MonoBehaviour
{
    public abstract float GetZoom();

    public abstract Vector2 GetCameraUV();

    public abstract Vector2 GetCursorUV();
}
