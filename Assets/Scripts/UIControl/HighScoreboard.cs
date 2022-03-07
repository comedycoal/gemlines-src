using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HighScoreboard : MonoBehaviour
{
    private int m_topScore = 0;

    private string FormattedHighScore => m_topScore.ToString().PadLeft(6, '0');
    public int HighScore
    { 
        get { return m_topScore; }
        set
        {
            if (m_topScore < value)
            {
                m_topScore = value;
                SetScoreText();
            }
        }
    }

    [SerializeField] private Text m_scoreText;

    private void Start()
    {
        // Load topScore


        if (m_topScore >= 0)
            SetScoreText();
    }

    private void SetScoreText()
    {
        m_scoreText.text = FormattedHighScore;
    }
}
