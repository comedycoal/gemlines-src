using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Previewer : MonoBehaviour
{
    [SerializeField] Image[] m_gemImages;
    [SerializeField] Sprite m_unknown;
    [SerializeField] Sprite m_normal;

    private void Start()
    {
        GameManager.Instance.EnterPhaseEvent += HandleEvent;
    }

    private void OnDestroy()
    {
        GameManager.Instance.EnterPhaseEvent -= HandleEvent;
    }

    private void HandleEvent(object sender, GameManager.Phase e)
    {
        if (e == GameManager.Phase.GAME_END) Reset();
    }


    public void Reset()
    {
        foreach (var img in m_gemImages)
            img.color = Color.clear;
    }

    public void SetPreviews(Color[] colors)
    {
        for (int i = 0; i < m_gemImages.Length; i++)
        {
            m_gemImages[i].sprite = m_normal;
            m_gemImages[i].color = colors[i];
            if (colors[i] == Color.white)
                m_gemImages[i].sprite = m_unknown;
        }
    }
}
