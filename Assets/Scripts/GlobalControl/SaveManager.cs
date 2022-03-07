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
    [SerializeField] private TimeAttackTimer m_timeAttackTimer;

    private void Awake()
    {
        Instance = this;
    }

    public bool LoadGame(out string state1, out string state2, out float time, out int score, out int turn, out bool timeAtk)
    {
        state1 = "";
        state2 = "";
        time = 0f;
        turn = 0;
        score = 0;
        timeAtk = false;
        int has = PlayerPrefs.GetInt("SavedGame");
        if (has == 0) return false;

        score = PlayerPrefs.GetInt("SavedGameScore");
        time = PlayerPrefs.GetFloat("SavedGameTime");
        turn = PlayerPrefs.GetInt("SavedGameTurn");
        timeAtk = PlayerPrefs.GetInt("SavedGameTimeAtk") == 0 ? false : true;
        state1 = PlayerPrefs.GetString("SavedGameState1");
        state2 = PlayerPrefs.GetString("SavedGameState2");

        return true;
    }

    public void SaveHighscore(int score)
    {
        PlayerPrefs.SetInt("Highscore", score);
    }
    public int LoadHighscore()
    {
        return PlayerPrefs.GetInt("Highscore", 0);
    }

    public void ClearSave()
    {
        PlayerPrefs.SetInt("SavedGame", 0);
        PlayerPrefs.SetString("SavedGameState", "");
    }

    public void SaveGame()
    {
        PlayerPrefs.SetInt("SavedGame", 1);
        PlayerPrefs.SetInt("SavedGameScore", m_scoreboard.Score);
        PlayerPrefs.SetInt("SavedGameTurn", GameManager.Instance.Turn);
        PlayerPrefs.SetInt("SavedGameTimeAtk", GameManager.Instance.IsTimeAttack ? 1 : 0);
        PlayerPrefs.SetFloat("SavedGameTime", m_timer.CurrentTime);
        string a, b;
        m_board.StringifyBoardState(out a, out b);
        PlayerPrefs.SetString("SavedGameState1", a);
        PlayerPrefs.SetString("SavedGameState2", b);
    }
}
