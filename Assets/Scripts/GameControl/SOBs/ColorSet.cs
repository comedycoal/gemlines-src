using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Color Set", menuName = "ScriptableObjects/Color Set", order = 1)]
public class ColorSet : ScriptableObject
{
    public Color NullColor;
    public Color DeadColor;
    public Color[] Colors;

    public Color GetColor(int index)
    {
        if (index < 0) return NullColor;
        return Colors[index % Colors.Length];
    }
}
