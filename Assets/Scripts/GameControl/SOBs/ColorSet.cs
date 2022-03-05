using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Color Set", menuName = "ScriptableObjects/Color Set", order = 1)]
public class ColorSet : ScriptableObject
{
    public Color[] Colors;

    public Color GetColor(int index)
    {
        return Colors[index % Colors.Length];
    }
}
