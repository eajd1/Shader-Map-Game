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

        pointBuffer = new ComputeBuffer(numPoints, sizeof(float) * 3);
        pointBuffer.SetData(points);
        colourBuffer = new ComputeBuffer(numPoints, sizeof(float));
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
            foreach (Vector3 point in points)
            {
                Gizmos.DrawSphere(point, 0.01f);
            }

            Gizmos.DrawSphere(test.normalized, 0.02f);

            Vector3 closestPoint = points[LinearSearch()];

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

    private void OnApplicationQuit()
    {
        pointBuffer.Release();
        colourBuffer.Release();
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

        //int height = numPoints / 2;
        //points = new Vector3[height * numPoints];
        //colours = new float[height * numPoints];
        //for (int i = 0; i < height; i++)
        //{
        //    float y = (i / (float)height * 2f) - 1f;
        //    float radius = Mathf.Sqrt(1 - y * y);
        //    float anglestep = (2 * Mathf.PI) / (numPoints * radius);
        //    for (int j = 0; j < numPoints * radius; j++)
        //    {
        //        float angle = anglestep * j;
        //        float x = Mathf.Cos(angle) * radius;
        //        float z = Mathf.Sin(angle) * radius;

        //        points[i + j] = new Vector3(x, y, z);
        //        colours[i + j] = (float)(i + j) / (height * numPoints);
        //    }
        //}

        //int height = numPoints / 2;
        //points = new Vector3[height];
        //colours = new float[height];
        //for (int i = 0; i < height; i++)
        //{
        //    float y = (i / (float)height * 2f) - 1f;
        //    float radius = Mathf.Sqrt(1 - y * y);
        //    float x = Mathf.Cos(0) * radius;
        //    float z = Mathf.Sin(0) * radius;

        //    points[i] = new Vector3(x, y, z);
        //    colours[i] = (float)(i) / (height * numPoints);
        //}
    }
}
