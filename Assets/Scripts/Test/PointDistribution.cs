using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CameraControls))]
public class PointDistribution : MonoBehaviour
{
    [Range(1, 10000)]
    [SerializeField] private int numPoints;
    [SerializeField] private ComputeShader shader;

    private Vector3[] points;
    private float[] colours;
    private Resolution screenResolution;
    private RenderTexture renderTexture;

    private ComputeBuffer pointBuffer;
    private ComputeBuffer colourBuffer;

    private CameraControls controls;

    // Start is called before the first frame update
    void Start()
    {
        controls = GetComponent<CameraControls>();

        colours = new float[numPoints];
        // https://stackoverflow.com/questions/9600801/evenly-distributing-n-points-on-a-sphere
        points = new Vector3[numPoints];
        float phi = Mathf.PI * (Mathf.Sqrt(5f) - 1f);

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

        shader.Dispatch(kernelIndex, screenResolution.width, screenResolution.height, 1);
    }

    void OnValidate()
    {
        colours = new float[numPoints];
        // https://stackoverflow.com/questions/9600801/evenly-distributing-n-points-on-a-sphere
        points = new Vector3[numPoints];
        float phi = Mathf.PI * (Mathf.Sqrt(5f) - 1f);

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

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(renderTexture, destination);
    }

    //private void OnDrawGizmos()
    //{
    //    foreach (Vector3 point in points) {
    //        Gizmos.DrawSphere(point, 0.01f);
    //    }
    //}

    private void OnApplicationQuit()
    {
        pointBuffer.Release();
        colourBuffer.Release();
    }
}
