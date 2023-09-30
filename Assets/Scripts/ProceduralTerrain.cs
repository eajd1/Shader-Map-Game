using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralTerrain
{
    public static float[] Generate(int resolution, int maxDepth, int maxHeight)
    {
        float[] pixels = new float[resolution * resolution * 2];
        for (int x = 0; x < 2 * resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                int index = x * resolution + y;

                //float h = Mathf.PerlinNoise(x / (resolution / 4f), y / (resolution / 4f)) * 2 - 1;
                //float h = Random.Range(maxDepth, maxHeight);

                Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(1, 100), UnityEngine.Random.Range(1, 100), UnityEngine.Random.Range(1, 100));
                float h = PerlinNoise3D(SpherePoint(x, y, resolution) + UnityEngine.Random.insideUnitSphere + randomOffset) * 2 - 1;

                if (h < 0)
                {
                    h *= -maxDepth;
                }
                else
                {
                    h *= maxHeight;
                }

                pixels[index] = h;
            }
        }
        return pixels;
    }

    // decay is between 0 and 1, maxValue can be anything, x should be greater than or equal to 0
    public static float ExponentialDecay(float maxValue, float decay, float x)
    {
        return maxValue * Mathf.Pow(decay, x);
    }

    private static Vector3 SpherePoint(int x, int y, int resolution)
    {
        float halfY = y / (float)resolution * 2 - 1;
        float width = Mathf.Sqrt(1 - halfY * halfY);
        return new()
        {
            x = Mathf.Sin((x / (float)resolution * 2 - 1) * Mathf.PI) * width,
            y = halfY,
            z = Mathf.Cos((x / (float)resolution * 2 - 1) * Mathf.PI) * width,
        };
    }

    private static float PerlinNoise3D(Vector3 p)
    {
        float xy = Mathf.PerlinNoise(p.x, p.y);
        float xz = Mathf.PerlinNoise(p.x, p.z);
        float yz = Mathf.PerlinNoise(p.y, p.z);
        float yx = Mathf.PerlinNoise(p.y, p.x);
        float zx = Mathf.PerlinNoise(p.z, p.x);
        float zy = Mathf.PerlinNoise(p.z, p.y);

        return (xy + xz + yz + yx + zx + zy) / 6;
    }
}
