using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WorldBufferData
{
    // Deal with holding the ComputeBuffers and updating them with a ComputeShader

    public ComputeBuffer heightBuffer;
    public ComputeBuffer idBuffer;
    public ComputeBuffer countryColourBuffer;

    public int resolution = World.Instance.WorldResolution;

    public WorldBufferData(float[] heights, int[] ids, Country[] countries)
    {
        heightBuffer = new ComputeBuffer(2 * resolution * resolution, sizeof(float));
        idBuffer = new ComputeBuffer(2 * resolution * resolution, sizeof(int));
        countryColourBuffer = new ComputeBuffer(countries.Length, sizeof(float) * 3);

        heightBuffer.SetData(heights);
        idBuffer.SetData(ids);
        countryColourBuffer.SetData(countries.Select(country => country.colour).ToArray());
    }

    // update the other buffers from the changes
    public void UpdateBuffers(Change[] changes, ComputeShader updateShader)
    {
        int kernelIndex = updateShader.FindKernel("CSMain");
        updateShader.SetInt("Resolution", resolution);

        ComputeBuffer changeBuffer = new ComputeBuffer(changes.Length, Change.SizeOf());
        changeBuffer.SetData(changes);
        updateShader.SetBuffer(kernelIndex, "Changes", changeBuffer);
        updateShader.SetBuffer(kernelIndex, "Heights", heightBuffer);
        updateShader.SetBuffer(kernelIndex, "Ids", idBuffer);

        updateShader.Dispatch(kernelIndex, 2 * resolution, resolution, 1);
        changeBuffer.Release();
    }

    ~WorldBufferData()
    {
        heightBuffer.Release();
        idBuffer.Release();
        countryColourBuffer.Release();
    }

    public void ReleaseBuffers()
    {
        heightBuffer.Release();
        idBuffer.Release();
        countryColourBuffer.Release();
    }
}
