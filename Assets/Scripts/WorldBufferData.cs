using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WorldBufferData
{
    // Deal with holding the ComputeBuffers and updating them with a ComputeShader

    public ComputeBuffer tileBuffer;
    public ComputeBuffer countryColourBuffer;

    private ComputeShader updateAllShader;
    private ComputeShader updateSingleShader;

    public int resolution = World.Instance.WorldResolution;

    public WorldBufferData(Tile[] tiles, Country[] countries)
    {
        tileBuffer = new ComputeBuffer(2 * resolution * resolution, Tile.SizeOf());
        countryColourBuffer = new ComputeBuffer(countries.Length, sizeof(float) * 3);

        tileBuffer.SetData(tiles);
        countryColourBuffer.SetData(countries.Select(country => country.colour).ToArray());

        updateAllShader = Resources.Load<ComputeShader>("UpdateBuffers");
        updateSingleShader = Resources.Load<ComputeShader>("UpdateSingle");
    }

    // update the other buffers from the changes
    public void UpdateBuffers(Tile[] changes)
    {
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
        int kernelIndex = updateSingleShader.FindKernel("CSMain");
        ComputeBuffer buffer = new ComputeBuffer(1, Tile.SizeOf());
        Tile[] tile = new Tile[] { change };
        buffer.SetData(tile);
        updateSingleShader.SetBuffer(kernelIndex, "Change", buffer);
        updateSingleShader.SetInt("Resolution", resolution);
        updateSingleShader.SetInt("PositionX", x);
        updateSingleShader.SetInt("PositionY", y);
        updateSingleShader.SetBuffer(kernelIndex, "Tiles", tileBuffer);

        updateSingleShader.Dispatch(kernelIndex, 1, 1, 1);
        buffer.Release();
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
