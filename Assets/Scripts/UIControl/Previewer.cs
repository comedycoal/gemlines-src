using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Previewer : MonoBehaviour
{
    [SerializeField] Image[] m_gemImages;

    public void Reset()
    {
        foreach (var img in m_gemImages)
            img.color = Color.white;
    }

    public void SetPreviews(Color[] colors)
    {
        for (int i = 0; i < m_gemImages.Length; i++)
        {
            m_gemImages[i].color = colors[i];
        }
    }
}
