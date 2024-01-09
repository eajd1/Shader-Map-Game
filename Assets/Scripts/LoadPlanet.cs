using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadPlanet
{
    public static Tile[] GenerateEarth(int resolution, float maxHeight, float minHeight, Texture2D heightmap, Texture2D sealevelmask, float threshold)
    {
        Tile[] tiles = new Tile[2 * resolution * resolution];

        for (int x = 0; x < 2 * resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                int index = x * resolution + y;

                if (GetPixel(sealevelmask, x, y, resolution).grayscale == 1)
                {
                    // Land
                    tiles[index] = new Tile(Mathf.Lerp(minHeight, maxHeight, GetPixel(heightmap, x, y, resolution).r));
                }
                else if (GetPixel(sealevelmask, x, y, resolution).b == 1)
                {
                    // Ocean
                    tiles[index] = new Tile(0).SetOcean(true);
                }
                else if (GetPixel(sealevelmask, x, y, resolution).r == 1)
                {
                    // Lake
                    tiles[index] = new Tile(Mathf.Lerp(minHeight, maxHeight, GetPixel(heightmap, x, y, resolution).r))
                        .SetLake(true);
                }
            }
        }

        return tiles;
    }

    public static Tile[] GenerateEarthShader(int resolution, float maxHeight, float minHeight, Texture2D heightmap, Texture2D mask)
    {
        ComputeShader shader = Resources.Load<ComputeShader>("LoadMap");
        Tile[] tiles = new Tile[2 * resolution * resolution];
        shader.SetInt("Resolution", resolution);
        shader.SetFloat("maxHeight", maxHeight);
        shader.SetFloat("minHeight", minHeight);
        int kernelIndex = shader.FindKernel("CSMain");
        shader.SetTexture(kernelIndex, "Heightmap", heightmap);
        shader.SetTexture(kernelIndex, "Mask", mask);
        ComputeBuffer tileBuffer = new ComputeBuffer(tiles.Length, Tile.SizeOf());
        tileBuffer.SetData(tiles);
        shader.SetBuffer(kernelIndex, "Tiles", tileBuffer);
        shader.Dispatch(kernelIndex, 2 * resolution, resolution, 1);
        tileBuffer.GetData(tiles);
        tileBuffer.Release();
        return tiles;
    }

    private static Color GetPixel(Texture2D texture, int x, int y, int resolution)
    {
        float u = x / (float)(resolution * 2);
        float v = y / (float)resolution;
        return texture.GetPixel((int)(u * texture.width), (int)(v * texture.height));
    }

    public static Tile[] GeneratePlanet(int resolution, float maxDepth, float maxHeight)
    {
        Random.InitState(resolution);
        Tile[] tiles = new Tile[2 * resolution * resolution];
        for (int x = 0; x < 2 * resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                int index = x * resolution + y;
                Vector3 pointOnSphere = PointOnSphere(resolution, x, y);

                // https://stackoverflow.com/questions/6555076/how-can-i-generate-perlin-noise-on-a-spherical-surface
                // https://www.youtube.com/watch?v=lctXaT9pxA0
                float totalNoise = 0;
                float frequency = 1;
                float amplitude = 1;
                int octaves = 20;
                for (int o = 0; o < octaves; o++)
                {
                    totalNoise += Perlin3D(pointOnSphere * frequency) * amplitude;
                    frequency *= 2;
                    amplitude *= 0.5f;
                }

                if (totalNoise < 0)
                {
                    totalNoise *= -maxDepth * 2;
                    tiles[index] = new Tile(totalNoise, 0, true);
                }
                else
                {
                    totalNoise *= maxHeight * 2;
                    tiles[index] = new Tile(totalNoise, 0, false);
                }
            }
        }
        return tiles;
    }

    // https://gis.stackexchange.com/questions/4147/lat-lon-alt-to-spherical-or-cartesian-coordinates
    private static Vector3 PointOnSphere(int resolution, int x, int y)
    {
        float lat = (y / (float)resolution - 0.5f) * Mathf.PI; // -0.5PI to 0.5PI
        float lon = x / (float)resolution * Mathf.PI; // 0 to 2PI

        return new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat));
    }

    private static float Perlin3D(Vector3 point)
    {
        point += new Vector3(200, 200, 200);
        float xy = Mathf.PerlinNoise(point.x, point.y);
        float xz = Mathf.PerlinNoise(point.x, point.z);
        float yz = Mathf.PerlinNoise(point.y, point.z);
        return (xy + xz + yz) / 3f * 2 - 1;
    }
}
