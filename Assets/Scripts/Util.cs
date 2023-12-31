using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util
{
    public static Color Int2Color(uint num)
    {
        uint rMask = 0xff000000;
        uint gMask = 0x00ff0000;
        uint bMask = 0x0000ff00;
        uint aMask = 0x000000ff;
        uint r = (num & rMask) >> 24;
        uint g = (num & gMask) >> 16;
        uint b = (num & bMask) >> 8;
        uint a = num & aMask;
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }

    public static uint Color2Int(Color color)
    {
        return 0;
    }
}
