using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum Phase
    {
        NONE = 0,
        DEBUG,
        GAME_START,
        TURN_START,
        PLAYER_TURN,
        GAME_END,
        TIMEATK_STALLING,
        TIMEATK_POPULIZING
    }


    //public class TaskCounter
    //{
    //    private int m_counter;

    //    public event EventHandler OnAllTaskDone;

    //    public void Increase()
    //    {
    //        m_counter++;
    //    }

    //    public void Decrease()
    //    {
    //        m_counter--;
    //        if (m_counter <= 0)
    //        {
    //            m_counter = 0;
    //            OnAllTaskDone.Invoke(this, EventArgs.Empty);
    //        }
    //    }
    //}
    
    public static GameManager Instance;


    //################# Members
    private bool m_isTimeAtk;
    private int m_turnCount;
    private Phase m_currPhase;
    private bool m_isPaused;

    // UI controls
    [SerializeField] private Timer m_timer;
    [SerializeField] private Previewer m_previewer;
    [SerializeField] private Scoreboard m_scoreboard;
    [SerializeField] private HighScoreboard m_highScoreboard;
    [SerializeField] private GameOverBoardControl m_gameOverBoard;
    [SerializeField] private GameObject m_pauseScreenObj;

    // Gameplay controls
    [SerializeField] private BoardController m_boardController;
    [SerializeField] private MusicPlayer m_musicPlayer;
    [SerializeField] private SoundPlayer m_soundPlayer;

    [SerializeField] private bool m_debug;


    //################# Properties
    public Phase CurrentPhase => m_currPhase;
    public bool IsInGame => CurrentPhase != Phase.NONE;
    public bool IsTimeAttack => m_isTimeAtk;
    public bool IsDebug => m_debug;
    public bool EditorOn { get; protected set; }
    public int Turn => m_turnCount;
    public bool IsPaused => m_isPaused;


    //################# Methods
    private void Awake()
    {
        Instance = this;
        m_isTimeAtk = false;

        // TODO: Load preferences, saved games, high scores.

    }

    public void NewGame()
    {
        m_gameOverBoard.HideBoard();
        m_turnCount = 0;
        SetPhase(Phase.GAME_START);
    }

    public void SetTimeAttack(bool value)
    {
        if (IsInGame && IsTimeAttack) return;
        m_isTimeAtk = value;
    }

    private void OnBoardSetUp(object sender, EventArgs e)
    {
        if (m_currPhase == Phase.GAME_START)
            SetPhase(Phase.TURN_START);
    }

    private void OnPreviewPopulated(object sender, Color[] e)
    {
        if (m_currPhase == Phase.TURN_START)
        {
            m_turnCount += 1;
            m_previewer.SetPreviews(e);
            SetPhase(Phase.PLAYER_TURN);
        }
    }

    private void OnPlayerTurnDone(object sender, EventArgs e)
    {
        if (m_currPhase == Phase.PLAYER_TURN)
            SetPhase(Phase.TURN_START);
    }

    private void OnBoardFillled(object sender, EventArgs e)
    {
        SetEndGame();
    }

    public void SetEndGame()
    {
        SetPhase(Phase.GAME_END);
    }

    public void ToggleEditor()
    {
        if (IsDebug)
        {
            EditorOn = !EditorOn;
            if (!EditorOn)
            {
                m_boardController.HitCheckAll();
                m_boardController.EditorClick(null);
            }
        }
    }

    private void OnEnded(object sender, EventArgs e)
    {
        m_gameOverBoard.MakeBoard(m_scoreboard.FormattedScore, m_timer.FormattedTime, m_highScoreboard.HighScore < m_scoreboard.Score);
        m_highScoreboard.HighScore = m_scoreboard.Score;
        SetPhase(Phase.NONE);
    }


    private void Start()
    {
        m_boardController.BoardSetUp += OnBoardSetUp;
        m_boardController.PreviewSetup += OnPreviewPopulated;
        m_boardController.PlayerTurnDone += OnPlayerTurnDone;
        m_boardController.BoardFillled += OnBoardFillled;
        m_boardController.GameEndSequenceFinished += OnEnded;

        m_currPhase = Phase.NONE;
        EnterPhaseEvent?.Invoke(this, m_currPhase);
    }

    private void OnDestroy()
    {
        m_boardController.BoardSetUp -= OnBoardSetUp;
        m_boardController.PreviewSetup -= OnPreviewPopulated;
        m_boardController.PlayerTurnDone -= OnPlayerTurnDone;
        m_boardController.BoardFillled -= OnEnded;
    }

    private void SetPhase(Phase phase)
    {
        m_currPhase = phase;
        EnterPhaseEvent?.Invoke(this, m_currPhase);
    }

    public bool TogglePause()
    {
        m_isPaused = !m_isPaused;
        GamePaused?.Invoke(this, m_isPaused);
        m_pauseScreenObj.SetActive(m_isPaused);
        return m_isPaused;
    }

    public void RegisterHit(int totalHitCount)
    {
        m_scoreboard.AddScore(totalHitCount);
    }


    //################# Events

    public event EventHandler<Phase> EnterPhaseEvent;
    public event EventHandler<Boolean> GamePaused;
}
