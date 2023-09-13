using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralTerrain
{
    public static Pixel[] Generate(int resolution, int maxDepth, int maxHeight)
    {
        Pixel[] pixels = new Pixel[resolution * resolution * 2];
        for (int x = 0; x < 2 * resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                int index = x * resolution + y;

                float h = Mathf.PerlinNoise(x / (resolution / 4f), y / (resolution / 4f)) * 2 - 1;

                if (h < 0)
                {
                    pixels[index] = new Pixel(h * maxDepth, true);
                }
                else
                {
                    pixels[index] = new Pixel(h * maxHeight, false);
                }
            }
        }
        return pixels;
    }

    // decay is between 0 and 1, maxValue can be anything, x should be greater than or equal to 0
    public static float ExponentialDecay(float maxValue, float decay, float x)
    {
        return maxValue * Mathf.Pow(decay, x);
    }
}
