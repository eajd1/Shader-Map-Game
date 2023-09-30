using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadEarth
{
    public static float[] GenerateEarth(int resolution, float maxDepth, float maxHeight, Texture2D heightmap, Texture2D bathymap, Texture2D sealevelmask)
    {
        float[] heights = new float[2 * resolution * resolution];

        for (int x = 0; x < 2 * resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                int index = x * resolution + y;

                if (GetPixel(sealevelmask, x, y, resolution).r > 0)
                {
                    heights[index] = Mathf.Max(GetPixel(heightmap, x, y, resolution).r * maxHeight, 0.1f);
                }
                else
                {
                    heights[index] = Mathf.Min((1 - GetPixel(bathymap, x, y, resolution).r) * maxDepth, -0.1f);
                }
            }
        }

        return heights;
    }

    private static Color GetPixel(Texture2D texture, int x, int y, int resolution)
    {
        float u = x / (float)(resolution * 2);
        float v = y / (float)resolution;
        return texture.GetPixel((int)(u * texture.width), (int)(v * texture.height));
    }
}
