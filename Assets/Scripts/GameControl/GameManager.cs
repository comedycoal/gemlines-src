using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum Phase
    {
        NONE = 0,
        START,
        TURN_START,
        PLAYER_TURN,
        PAUSED,
        TIMEATK_STALLING,
        TIMEATK_POPULIZING
    }

    public enum GameMode
    {
        NORMAL,
        TIME_ATK
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
    private GameMode m_gameMode;
    private int m_turnCount;
    private Phase m_currPhase;

    // UI controls
    [SerializeField] private Timer m_timer;
    [SerializeField] private Previewer m_previewer;
    [SerializeField] private Scoreboard m_scoreboard;
    [SerializeField] private HighScoreboard m_highScoreboard;

    // Gameplay controls
    [SerializeField] private BoardController m_boardController;
    [SerializeField] private MusicPlayer m_musicPlayer;
    [SerializeField] private SoundPlayer m_soundPlayer;


    //################# Properties
    public Phase CurrentPhase => m_currPhase;


    //################# Methods
    private void Awake()
    {
        Instance = this;
        m_gameMode = GameMode.NORMAL;
        // TODO: Load preferences, saved games, high scores.

    }

    private void OnBoardSetUp(object sender, EventArgs e)
    {
        if (m_currPhase == Phase.START)
            SetPhase(Phase.TURN_START);
    }

    private void OnPreviewPopulated(object sender, EventArgs e)
    {
        if (m_currPhase == Phase.TURN_START)
            SetPhase(Phase.PLAYER_TURN);
    }

    private void OnPlayerTurnDone(object sender, EventArgs e)
    {
        if (m_currPhase == Phase.PLAYER_TURN)
            SetPhase(Phase.TURN_START);
    }

    private void OnBoardFillled(object sender, EventArgs e)
    {
        EndGame();
    }

    private void Start()
    {
        m_boardController.BoardSetUp += OnBoardSetUp;
        m_boardController.PreviewSetup += OnPreviewPopulated;
        m_boardController.PlayerTurnDone += OnPlayerTurnDone;
        m_boardController.BoardFillled += OnBoardFillled;

        m_currPhase = Phase.NONE;
        EnterPhaseEvent?.Invoke(this, m_currPhase);
    }

    private void Update()
    {

    }

    private void OnDestroy()
    {
        m_boardController.BoardSetUp -= OnBoardSetUp;
        m_boardController.PreviewSetup -= OnPreviewPopulated;
        m_boardController.PlayerTurnDone -= OnPlayerTurnDone;
        m_boardController.BoardFillled -= OnBoardFillled;
    }

    private void SetPhase(Phase phase)
    {
        m_currPhase = phase;
        EnterPhaseEvent?.Invoke(this, m_currPhase);
    }

    public void NewGame()
    {
        SetPhase(Phase.START);
    }

    public void SetToTimeAttack()
    {
        m_gameMode = GameMode.TIME_ATK;
    }

    public void EndGame()
    {
        SetPhase(Phase.NONE);

        // Update hi-score, etc.
        int score = m_scoreboard.GetScore();
        m_highScoreboard.Set(score);
    }


    //################# Events

    public event EventHandler<Phase> EnterPhaseEvent;
}
