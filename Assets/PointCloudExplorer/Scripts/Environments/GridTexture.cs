using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridTexture
{

    public static Texture2D CreatePattern(int size, int division = 4, int marker = 16)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGB24, true);

        var unit = size / division;
        float
            markerMin = size / marker * 0.5f,
            markerMax = unit - markerMin;

        Color
            background = new Color(0f, 0f, 0f, 0f),
            foreground = Color.white;

        for (int y = 0; y < size; y++)
        {
            var ry = y % unit;
            var dy = Mathf.Abs(unit - ry);
            var flagY = (dy <= markerMin || dy >= markerMax);

            for (int x = 0; x < size; x++)
            {
                var rx = x % unit;
                var dx = Mathf.Abs(unit - rx);
                var flagX = (dx <= markerMin || dx >= markerMax);

                if (
                    (ry == 0 || rx == 0) && (flagY && flagX)
                )
                {
                    tex.SetPixel(x, y, foreground);
                }
                else
                {
                    tex.SetPixel(x, y, background);
                }
            }
        }
        tex.Apply();
        return tex;
    }


}
