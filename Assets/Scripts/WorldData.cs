using System.Collections;
using System.Collections.Generic;

public class WorldData
{
    public Tile[] tiles; // Tiles of the world
    public WorldBufferData bufferData;

    public WorldData(Tile[] tiles, WorldBufferData bufferData)
    {
        this.tiles = tiles;
        this.bufferData = bufferData;
    }
}
