using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Tile
{
    public float height; // height above sea level at the SURFACE of the land or water
    public uint owner;
    public uint details;

    // Possible Future data:
    // Population
    // Temperature
    // Moisture
    // Resource

    public Tile(float height, uint owner, bool ocean)
    {
        this.height = height;
        this.owner = owner;
        details = 0;
        if (ocean)
            details += 1u << 31; // Left-most bit is ocean
    }

    public Tile(float height, uint owner, uint details)
    {
        this.height = height;
        this.owner = owner;
        this.details = details;
    }

    public Tile(float height)
    {
        this.height = height;
        owner = 0;
        details = 0;
    }

    public Tile SetOcean(bool ocean)
    {
        if (ocean)
        {
            details = details | 0b10000000000000000000000000000000;
        }
        else
        {
            details = details - (details & 0b10000000000000000000000000000000);
        }
        return this;
    }

    public Tile SetLake(bool lake)
    {
        if (lake)
        {
            details = details | 0b01000000000000000000000000000000;
        }
        else
        {
            details = details - (details & 0b01000000000000000000000000000000);
        }
        return this;
    }

    public static int SizeOf()
    {
        return sizeof(int) + sizeof(float) + sizeof(uint);
    }

    public bool IsOcean() => (details >> 31) % 2 == 1;
}
