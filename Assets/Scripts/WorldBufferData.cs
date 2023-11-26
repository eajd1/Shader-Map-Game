using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WorldBufferData
{
    // Deal with holding the ComputeBuffers and updating them with a ComputeShader

    public ComputeBuffer tileBuffer;
    public ComputeBuffer countryColourBuffer;

    public int resolution = World.Instance.WorldResolution;

    public WorldBufferData(Tile[] tiles, Country[] countries)
    {
        tileBuffer = new ComputeBuffer(2 * resolution * resolution, Tile.SizeOf());
        countryColourBuffer = new ComputeBuffer(countries.Length, sizeof(float) * 3);

        tileBuffer.SetData(tiles);
        countryColourBuffer.SetData(countries.Select(country => country.colour).ToArray());
    }

    // update the other buffers from the changes
    public void UpdateBuffers(Tile[] changes)
    {
        ComputeShader updateAllShader = World.Instance.UpdateAllShader;
        int kernelIndex = updateAllShader.FindKernel("CSMain");
        updateAllShader.SetInt("Resolution", resolution);

        ComputeBuffer changeBuffer = new ComputeBuffer(changes.Length, Tile.SizeOf());
        changeBuffer.SetData(changes);
        updateAllShader.SetBuffer(kernelIndex, "Changes", changeBuffer);
        updateAllShader.SetBuffer(kernelIndex, "Tiles", tileBuffer);

        updateAllShader.Dispatch(kernelIndex, 2 * resolution, resolution, 1);
        changeBuffer.Release();
    }

    public void UpdateSingleTile(Tile change, int x, int y)
    {
        ComputeShader updateSingleShader = World.Instance.UpdateSingleShader;
        int kernalIndex = updateSingleShader.FindKernel("CSMain");
        updateSingleShader.SetInt("Resolution", resolution);
        updateSingleShader.SetInt("PositionX", x);
        updateSingleShader.SetInt("PositionY", y);
        updateSingleShader.SetFloat("Height", change.height);
        updateSingleShader.SetInt("Owner", change.owner);
        updateSingleShader.SetBuffer(kernalIndex, "Tiles", tileBuffer);

        updateSingleShader.Dispatch(kernalIndex, 2 * resolution, resolution, 1);
    }

    ~WorldBufferData()
    {
        ReleaseBuffers();
    }

    public void ReleaseBuffers()
    {
        tileBuffer.Release();
        countryColourBuffer.Release();
    }
}
