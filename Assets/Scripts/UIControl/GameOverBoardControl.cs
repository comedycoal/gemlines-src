using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameOverBoardControl : MonoBehaviour
{
    [SerializeField] GameObject m_layout;
    [SerializeField] Text m_content;

    private void Awake()
    {
        HideBoard();
    }

    public void MakeBoard(string score, string time, bool isHighscore)
    {
        m_layout.SetActive(true);
        m_content.text = "Time: " + time
                + "\nScore: " + score
                + "\n" + (isHighscore ? "New Highscore!" : "Good Try!");
    }

    public void HideBoard()
    {
        m_layout.SetActive(false);
    }
}
