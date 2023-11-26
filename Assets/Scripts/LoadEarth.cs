using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadEarth
{
    public static Tile[] GenerateEarth(int resolution, float maxDepth, float maxHeight, Texture2D heightmap, Texture2D bathymap, Texture2D sealevelmask, float threshold)
    {
        Tile[] tiles = new Tile[2 * resolution * resolution];

        for (int x = 0; x < 2 * resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                int index = x * resolution + y;

                if (GetPixel(sealevelmask, x, y, resolution).r > threshold)
                {
                    tiles[index] = new Tile(Mathf.Max(GetPixel(heightmap, x, y, resolution).r * maxHeight, 0.1f), 0);
                }
                else
                {
                    tiles[index] = new Tile(Mathf.Min((1 - GetPixel(bathymap, x, y, resolution).r) * maxDepth, -0.1f), 0);
                }
            }
        }

        return tiles;
    }

    private static Color GetPixel(Texture2D texture, int x, int y, int resolution)
    {
        float u = x / (float)(resolution * 2);
        float v = y / (float)resolution;
        return texture.GetPixel((int)(u * texture.width), (int)(v * texture.height));
    }
}
