using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    private float m_cummulativeTime;
    private bool m_ticking;
    private bool m_doSetText;

    [SerializeField] private Text m_timerText;

    private void Start()
    {
        m_ticking = false;
        m_doSetText = false;

        GameManager.Instance.EnterPhaseEvent += HandleEvent;
        GameManager.Instance.GamePaused += HandleGamePause;
    }

    private void OnDestroy()
    {
        GameManager.Instance.EnterPhaseEvent -= HandleEvent;
        GameManager.Instance.GamePaused -= HandleGamePause;
    }

    private void HandleEvent(object sender, GameManager.Phase e)
    {
        if (e == GameManager.Phase.NONE)
        {
            StopTiming();
        }
        else if (e == GameManager.Phase.GAME_START)
        {
            Reset();
            StartTiming();
        }
    }

    private void HandleGamePause(object sender, bool paused)
    {
        if (paused)
            m_ticking = false;
        else
            m_ticking = true;
    }

    public void Reset()
    {
        m_cummulativeTime = 0f;
        SetTextWithTime();
    }
    void Update()
    {
        if (m_ticking)
        {
            m_cummulativeTime += Time.deltaTime;
            if (m_doSetText)
                SetTextWithTime();
        }
    }

    private void StartTiming()
    {
        m_doSetText = true;
        m_ticking = true;
    }

    private void StopTiming()
    {
        m_doSetText = false;
        m_ticking = false;
    }

    private void SetTextWithTime()
    {
        int val = (int)m_cummulativeTime;
        m_timerText.text = (val / 60).ToString().PadLeft(2, '0') + ":" + (val % 60).ToString().PadLeft(2, '0');
    }
}
