using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scoreboard : MonoBehaviour
{
    private int m_score;

    [SerializeField] Text m_scoreText;

    public int Score => m_score;

    private void Start()
    {
        m_score = 0;
        GameManager.Instance.EnterPhaseEvent += HandleEvent;
    }
    private void OnDestroy()
    {
        GameManager.Instance.EnterPhaseEvent -= HandleEvent;
    }

    private void HandleEvent(object sender, GameManager.Phase e)
    {
        if (e == GameManager.Phase.GAME_START)
        {
            Reset();
        }
    }

    private void Reset()
    {
        m_score = 0;
        SetScoreText();
    }

    public void AddScore(int ballsHit)
    {
        StartCoroutine(AddScoreCoroutine(ballsHit));
    }

    private void SetScoreText()
    {
        m_scoreText.text = m_score.ToString().PadLeft(6, '0');
    }

    private IEnumerator AddScoreCoroutine(int ballsHit)
    {
        int value = 1;
        int thres = 5;
        while (ballsHit > 0)
        {
            m_score += value;
            SetScoreText();
            if (thres <= 0)
                value++;

            --ballsHit;
            --thres;
            yield return null;
        }
    }
}
