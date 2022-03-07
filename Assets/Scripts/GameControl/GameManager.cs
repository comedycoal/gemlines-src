using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private TimeAttackTimer m_timeAtkTimer;
    [SerializeField] private Button m_newGameButton;
    [SerializeField] private Button m_timeAtkButton;
    [SerializeField] private Button m_pauseButton;
    [SerializeField] private TutorialController m_tutorial;

    // Gameplay controls
    [SerializeField] private BoardController m_boardController;
    [SerializeField] private MusicPlayer m_musicPlayer;
    [SerializeField] private SoundPlayer m_soundPlayer;

    [SerializeField] private bool m_debug;


    //################# Properties
    public Phase CurrentPhase => m_currPhase;
    public bool IsInGame => CurrentPhase != Phase.NONE && CurrentPhase != Phase.GAME_END;


    public bool IsTimeAttack => m_isTimeAtk;
    public bool IsDebug => m_debug;
    public bool EditorOn { get; protected set; }
    public int Turn => m_turnCount;
    public bool IsPaused => m_isPaused;


    //################# Events
    public event EventHandler<Phase> EnterPhaseEvent;
    public event EventHandler<Boolean> GamePaused;


    //################# Methods
    private void Awake()
    {
        Instance = this;
        m_isTimeAtk = false;
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
        if (m_isTimeAtk) m_timeAtkTimer.TurnOn();
        else m_timeAtkTimer.TurnOff();

        m_timeAtkTimer.TickOneTurn();
        if (IsPaused) m_timeAtkTimer.TogglePause(this, true);

    }

    private void OnBoardSetUp(object sender, EventArgs e)
    {
        if (m_currPhase == Phase.GAME_START)
            SetPhase(Phase.TURN_START);
    }

    private void OnPreviewPopulated(object sender, Color[] e)
    {
        m_previewer.SetPreviews(e);
        if (m_currPhase == Phase.TURN_START)
        {
            m_turnCount += 1;

            if (IsTimeAttack)
                m_timeAtkTimer.TickOneTurn();

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
        if (IsPaused)
            m_pauseButton.onClick.Invoke();

        if (IsTimeAttack)
            m_timeAtkTimer.TurnOff();

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
        SaveManager.Instance.SaveHighscore(m_highScoreboard.HighScore);
        m_newGameButton.onClick.Invoke();
        SetPhase(Phase.NONE);
    }

    public void ShowTutorial()
    {
        m_pauseButton.onClick.Invoke();
        m_tutorial.gameObject.SetActive(true);
    }


    private void Start()
    {
        m_tutorial.gameObject.SetActive(false);

        m_boardController.BoardSetUp += OnBoardSetUp;
        m_boardController.PreviewSetup += OnPreviewPopulated;
        m_boardController.PlayerTurnDone += OnPlayerTurnDone;
        m_boardController.BoardFillled += OnBoardFillled;
        m_boardController.GameEndSequenceFinished += OnEnded;

        m_timeAtkTimer.TurnEnded += OnPlayerTurnDone;

        // TODO: Load preferences, saved games, high scores.
        m_highScoreboard.HighScore = SaveManager.Instance.LoadHighscore();

        string state1, state2;
        float time;
        int score, turn;
        bool timeAtk;
        bool res = SaveManager.Instance.LoadGame(out state1, out state2, out time, out score, out turn, out timeAtk);
        if (res)
        {
            m_newGameButton.onClick.Invoke();

            m_boardController.LoadFromStrings(state1, state2);
            SaveManager.Instance.ClearSave();

            SetPhase(Phase.PLAYER_TURN);

            m_timer.StartTiming();
            m_timer.CurrentTime = time;
            m_scoreboard.Score = score;
            m_turnCount = turn;

            if (timeAtk)
                m_timeAtkButton.onClick.Invoke();

            m_pauseButton.onClick.Invoke();

        }
        else
        {
            m_currPhase = Phase.NONE;
            EnterPhaseEvent?.Invoke(this, m_currPhase);
        }
    }

    private void OnDestroy()
    {
        m_boardController.BoardSetUp -= OnBoardSetUp;
        m_boardController.PreviewSetup -= OnPreviewPopulated;
        m_boardController.PlayerTurnDone -= OnPlayerTurnDone;
        m_boardController.BoardFillled -= OnBoardFillled;
        m_boardController.GameEndSequenceFinished -= OnEnded;

        m_timeAtkTimer.TurnEnded -= OnPlayerTurnDone;
    }

    private void OnApplicationQuit()
    {
        if (IsInGame)
            SaveManager.Instance.SaveGame();
    }

    private void SetPhase(Phase phase)
    {
        m_currPhase = phase;
        EnterPhaseEvent?.Invoke(this, m_currPhase);
    }

    public void SetPauseState(bool value)
    {
        m_isPaused = value;
        GamePaused?.Invoke(this, m_isPaused);
        m_pauseScreenObj.SetActive(m_isPaused);
    }

    public void RegisterScore(int score)
    {
        m_scoreboard.AddScore(score);
    }
}
