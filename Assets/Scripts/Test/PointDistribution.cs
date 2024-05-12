using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CameraControls))]
public class PointDistribution : MonoBehaviour
{
    [Range(1, 1000)]
    [SerializeField] private int numPoints;
    [SerializeField] private ComputeShader shader;
    [SerializeField] private bool gizmos;
    [SerializeField] private Vector3 test;
    [Range(0, 100)]
    [SerializeField] private int testIndex;

    private Vector3[] points;
    private float[] colours;
    private Resolution screenResolution;
    private RenderTexture renderTexture;

    private ComputeBuffer pointBuffer;
    private ComputeBuffer colourBuffer;

    private CameraControls controls;

    private readonly float phi = Mathf.PI * (Mathf.Sqrt(5f) - 1f);

    // Start is called before the first frame update
    void Start()
    {
        controls = GetComponent<CameraControls>();
        gizmos = false;

        GetPoints();

        screenResolution = Screen.currentResolution;

        renderTexture = new RenderTexture(screenResolution.width, screenResolution.height, 1);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        pointBuffer = new ComputeBuffer(points.Length, sizeof(float) * 3);
        pointBuffer.SetData(points);
        colourBuffer = new ComputeBuffer(colours.Length, sizeof(float));
        colourBuffer.SetData(colours);

        int kernelIndex = shader.FindKernel("CSMain");
        shader.SetTexture(kernelIndex, "Result", renderTexture);
        shader.SetBuffer(kernelIndex, "points", pointBuffer);
        shader.SetBuffer(kernelIndex, "colours", colourBuffer);
    }

    void Update()
    {
        int kernelIndex = shader.FindKernel("CSMain");
        shader.SetInts("screenResolution", new int[] { screenResolution.width, screenResolution.height });
        Vector3 cameraPos = transform.position.normalized;
        shader.SetFloats("cameraPosition", new float[] { cameraPos.x, cameraPos.y, cameraPos.z });
        shader.SetFloat("zoom", controls.GetZoom());
        shader.SetInt("bufferLength", numPoints);

        if (!gizmos)
        {
            shader.Dispatch(kernelIndex, screenResolution.width, screenResolution.height, 1);
        }
    }

    void OnValidate()
    {
        GetPoints();

        if (!gizmos)
        {
            pointBuffer.Release();
            pointBuffer = new ComputeBuffer(numPoints, sizeof(float) * 3);
            pointBuffer.SetData(points);
            colourBuffer.Release();
            colourBuffer = new ComputeBuffer(numPoints, sizeof(float));
            colourBuffer.SetData(colours);

            int kernelIndex = shader.FindKernel("CSMain");
            shader.SetBuffer(kernelIndex, "points", pointBuffer);
            shader.SetBuffer(kernelIndex, "colours", colourBuffer);
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(renderTexture, destination);
    }

    private void OnDrawGizmos()
    {
        if (gizmos)
        {
            int index = 0;
            foreach (Vector3 point in points)
            {
                if (index == testIndex)
                {
                    Gizmos.DrawSphere(point, 0.02f);
                }
                else
                {
                    Gizmos.DrawSphere(point, 0.01f);
                }
                index++;
            }

            Gizmos.DrawSphere(test.normalized, 0.02f);

            Vector3 closestPoint = points[UVSearch()];

            Gizmos.DrawLine(test.normalized, closestPoint);
        }
    }

    private int LinearSearch()
    {
        int closestIndex = 0;
        float closest = 0f;
        for (int i = 0; i < points.Length; i++)
        {
            if (Vector3.Dot(test.normalized, points[i]) > closest)
            {
                closest = Vector3.Dot(test.normalized, points[i]);
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    private int Alternate()
    {
        int index = (int)((test.normalized.y + 1) / 2 * numPoints);
        //float theta = Mathf.Acos(test.normalized.x);
        return index;// + (int)(theta);
    }

    private int UVSearch()
    {
        Vector3 p = test.normalized;
        float u = Mathf.Atan2(p.x, p.z) / (2 * Mathf.PI) + 0.5f;
        float v = Mathf.Asin(p.y) / Mathf.PI + 0.5f;
        return 0;
    }

    private void OnApplicationQuit()
    {
        pointBuffer.Release();
        colourBuffer.Release();
    }

    private float GetWidth(float y)
    {
        y = Mathf.Clamp(y, -1, 1);
        return Mathf.Sqrt(1 - y * y);
    }

    private void GetPoints()
    {
        colours = new float[numPoints];
        // https://stackoverflow.com/questions/9600801/evenly-distributing-n-points-on-a-sphere
        points = new Vector3[numPoints];

        for (int i = 0; i < numPoints; i++)
        {
            float y = (i / (float)numPoints * 2f) - 1f;
            float radius = Mathf.Sqrt(1 - y * y);

            float theta = phi * i;
            float x = Mathf.Cos(theta) * radius;
            float z = Mathf.Sin(theta) * radius;

            points[i] = new Vector3(x, y, z);

            colours[i] = (float)i / numPoints;
        }

        //colours = new float[(int)(Mathf.PI * numPoints * numPoints) - numPoints];
        //points = new Vector3[(int)(Mathf.PI * numPoints * numPoints) - numPoints];

        //int index = 0;
        //for (int y = 0; y < numPoints * 2; y++)
        //{
        //    int width = Mathf.Min((int)(GetWidth((y - numPoints) / (float)numPoints) * 2 * numPoints), 2 * numPoints - 1);
        //    for (int x = 0; x < width; x++)
        //    {
        //        colours[index] = index / (float)colours.Length;

        //        float yPos = (y - numPoints) / (float)numPoints;
        //        float xPos = Mathf.Cos(x / (float)width * 2 * Mathf.PI) * GetWidth(yPos);
        //        float zPos = Mathf.Sin(x / (float)width * 2 * Mathf.PI) * GetWidth(yPos);
        //        points[index] = new Vector3(xPos, yPos, zPos);
        //        index++;
        //    }
        //}
        //Debug.Log(index);
        //Debug.Log(points.Length);
    }
}
