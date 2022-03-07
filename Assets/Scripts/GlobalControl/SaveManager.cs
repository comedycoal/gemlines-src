using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; internal set; }

    [SerializeField] private BoardController m_board;
    [SerializeField] private Timer m_timer;
    [SerializeField] private Scoreboard m_scoreboard;
    [SerializeField] private HighScoreboard m_highScoreboard;

    public void SaveGame()
    {
        throw new NotImplementedException();
    }
}
