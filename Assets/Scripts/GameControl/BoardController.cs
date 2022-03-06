using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    //################# Members
    [SerializeField] private int m_boardSize = 9;
    [SerializeField] private int m_initialGemCount = 7;
    [SerializeField] private int m_newGemPerTurn = 3;
    [SerializeField] private float m_gemMovementTime = 0.2f;

    private const int c_cellPixelSize = 18;
    private const int c_cellBorderPixelSize = 1;
    private const int c_boardPixelSize = 172;

    private float m_boardWorldSpaceSize;
    private bool m_allowPlayerControl = false;
    private Vector3 m_topLeftCoord;

    [SerializeField] private GameObject m_gemCellPrefab;
    [SerializeField] private GameObject m_gemCellDummyPrefab;

    private GemCell[,] m_grid;
    private GemCell m_selectedCell;

    private List<GemCell> m_previewQueue;
    private HashSet<GemCell> m_emptyCellSet;
    private System.Random m_rng;


    //################# Events
    /// <summary>
    /// This event is invoked when the board is set up -> GameManager set Phase to TURN_START
    /// </summary>
    public event EventHandler BoardSetUp;

    /// <summary>
    /// This event is invoked when all previewed gems are in play -> GameManager set Phase to PLAYER_TURN
    /// </summary>
    public event EventHandler<Color[]> PreviewSetup;

    /// <summary>
    /// This event is invoked when the player turn is done -> GameManager set Phase to TURN_START
    /// </summary>
    public event EventHandler PlayerTurnDone;

    /// <summary>
    /// This event is invoked when the board is filled -> GameManager set Phase to NONE
    /// </summary>
    public event EventHandler BoardFillled;


    //################# Methods

    //----------// Awake set-up
    private void Awake()
    {
        var collider = GetComponent<Collider2D>();
        m_boardWorldSpaceSize = collider.bounds.size.x;
        m_topLeftCoord = new Vector3((c_cellPixelSize/2) / (float)c_boardPixelSize * m_boardWorldSpaceSize - m_boardWorldSpaceSize / 2,
                                    -(c_cellPixelSize/2) / (float)c_boardPixelSize * m_boardWorldSpaceSize + m_boardWorldSpaceSize / 2, 0);

        m_grid = new GemCell[m_boardSize, m_boardSize];
        m_rng = new System.Random(Environment.TickCount);
        CreateGameObjectGrid();
    }

    /// <summary>
    /// Create Game object grid, used in <see cref="Awake"/> to initialize game board.
    /// </summary>
    private void CreateGameObjectGrid()
    {
        Vector3 colliderSize = new Vector3((float)c_cellPixelSize / c_boardPixelSize * m_boardWorldSpaceSize,
                                           (float)c_cellPixelSize / c_boardPixelSize * m_boardWorldSpaceSize, 0);

        for (int i = 0; i < m_boardSize; i++)
        {
            for (int j = 0; j < m_boardSize; j++)
            {
                var go = Instantiate(m_gemCellPrefab);
                go.name = "Gem Cell " + i + j;
                go.transform.parent = transform;
                go.transform.localPosition = GetLocalPos(j, i);

                var gemCell = go.GetComponent<GemCell>();
                var collider = go.GetComponent<BoxCollider2D>();
                collider.size = colliderSize;
                m_grid[i, j] = gemCell;
                gemCell.setBoardPos(i, j);
            }
        }
    }


    //----------// Start set-up
    private void Start()
    {
        GameManager.Instance.EnterPhaseEvent += HandleGamePhaseEvent;
        GameManager.Instance.GamePaused += HandleGamePause;
    }


    //----------// OnDestroy set-up
    private void OnDestroy()
    {
        GameManager.Instance.EnterPhaseEvent -= HandleGamePhaseEvent;
        GameManager.Instance.GamePaused -= HandleGamePause;
    }


    /// <summary>
    /// Handle events from <see cref="GameManager"/>, each of which signifying a change in the game state.
    /// </summary>
    /// <param name="sender">is irrelevant</param>
    /// <param name="e">The current <see cref="GameManager.Phase"/> that the game is in</param>
    private void HandleGamePhaseEvent(object sender, GameManager.Phase e)
    {
       if (GameManager.Instance.IsDebug) Debug.Log(e);
        if (e == GameManager.Phase.NONE)
        {
            m_allowPlayerControl = false;
        }
        else if (e == GameManager.Phase.GAME_START)
        {
            Reset();
            m_allowPlayerControl = false;
            InitiallyPopulate();
            BoardSetUp?.Invoke(this, EventArgs.Empty);
        }
        else if (e == GameManager.Phase.TURN_START)
        {
            m_allowPlayerControl = false;
            ActualizePreviewedCells();
            PopulatePreviewCells();
            PreviewSetup?.Invoke(this, m_previewQueue.ConvertAll<Color>(x => x.GemColor).ToArray());
        }
        else if (e == GameManager.Phase.PLAYER_TURN)
        {
            m_allowPlayerControl = true;
        }
        else if (e == GameManager.Phase.TIMEATK_STALLING)
        {
            m_allowPlayerControl = true;
        }
        else if (e == GameManager.Phase.TIMEATK_POPULIZING)
        {
            m_allowPlayerControl = false;
        }
    }

    private void HandleGamePause(object sender, bool isPaused)
    {
        if (isPaused)
            m_allowPlayerControl = false;
        else
            m_allowPlayerControl = true;
    }

    /// <summary>
    /// Reset the game board, used when a new game is set
    /// </summary>
    private void Reset()
    {
        ClearGrid();
        m_previewQueue = new List<GemCell>();
        m_selectedCell = null;
        m_emptyCellSet = new HashSet<GemCell>();
        for (int i = 0; i < m_boardSize; i++)
        {
            for (int j = 0; j < m_boardSize; j++)
            {
                m_emptyCellSet.Add(m_grid[i, j]);
            }
        }
    }


    //----------// Gameplay flow set-up

    /// <summary>
    /// Initially populate the game board with a fixed set of gems as previews.
    /// These previews will be in play in the next game phase.
    /// </summary>
    private void InitiallyPopulate()
    {
        for (int i = 0; i < m_initialGemCount; ++i)
        {
            GemCell gemCell = PopAnEmptyGemCell();
            m_previewQueue.Add(gemCell);
            gemCell.SetAsPreview(GemCell.GemType.NORMAL, GetAppropriateGemColorIndex());
        }
    }

    /// <summary>
    /// Set a fixed number of cells in the board with a type and color,
    /// but do not allow them to be in play yet.
    /// <para>The game over condition is checked inside this method</para>
    /// </summary>
    private void PopulatePreviewCells()
    {
        for (int i = 0; i < m_newGemPerTurn; i++)
        {
            GemCell gemCell = PopAnEmptyGemCell();
            if (gemCell == null)
            {
                BoardFillled?.Invoke(this, EventArgs.Empty);
                return;
            }

            m_previewQueue.Add(gemCell);
            gemCell.SetAsPreview(GetAppropriateGemType(), GetAppropriateGemColorIndex());
        }

        if (GameManager.Instance.IsDebug) Debug.Log("Empty cells: " + CountEmptyCells());
    }

    private void ActualizePreviewedCells()
    {
        if (m_previewQueue == null) return;
        for (int i = 0; i < m_previewQueue.Count; i++)
        {
            m_previewQueue[i].ActualizeFromPreview();

            // Hit Check
            List<List<GemCell>> cellsToDestroy;
            bool hit = HitCheck(m_previewQueue[i], out cellsToDestroy);

            if (hit)
            {
                int count = 0;
                foreach (var line in cellsToDestroy)
                {
                    foreach (var cell in line)
                    {
                        cell.DestroyGem();
                        // Add points

                        m_emptyCellSet.Add(cell); // Add back to empty set
                    }
                    count += line.Count;
                }
                GameManager.Instance.RegisterHit(count);
            }
        }
        m_previewQueue.Clear();
    }


    //-----------// Player Interactions
    public void PlayerClick(GemCell gemCell)
    {
        if (!m_allowPlayerControl)
            return;

        if (GameManager.Instance.IsDebug) Debug.Log("Clicked " + gemCell.name);
        // If this is a move for selected GEM
        if (m_selectedCell != null && m_selectedCell.IsInPlay)
        {
            // If player want to move to an "empty" cell
            if (!gemCell.IsInPlay)
            {
                var movePath = GetMovePath(m_selectedCell, gemCell);

                m_selectedCell.OnCancelSelected();

                if (movePath != null)
                {
                    PerformMove(m_selectedCell, movePath);
                }

                m_selectedCell = null;
            }
            // If player select another cell
            else if (gemCell.IsSelectible && gemCell != m_selectedCell)
            {
                m_selectedCell.OnCancelSelected();
                m_selectedCell = gemCell;
                m_selectedCell.OnSelected();
            }
            // Else, just cancel the selection
            else
            {
                m_selectedCell.OnCancelSelected();
                m_selectedCell = null;
            }
        }
        // If player selected a new GEM
        else if (gemCell.IsSelectible && gemCell.IsInPlay)
        {
            m_selectedCell = gemCell;
            m_selectedCell.OnSelected();
        }
    }


    private Coroutine m_gemMove;

    private void PerformMove(GemCell src, List<GemCell> movePath)
    {
        StartCoroutine(PerformMoveCoroutine(src, movePath));
    }

    private IEnumerator PerformMoveCoroutine(GemCell src, List<GemCell> movePath)
    {
        m_allowPlayerControl = false;

        // Set src to null
        GemCell.GemType typeSrc = src.Type;
        int colorIdxSrc = src.ColorIndex;
        Color color = src.GemColor;
        src.Reset();
        m_emptyCellSet.Add(src);

        // Make a dummy GO
        GameObject dummy = Instantiate(m_gemCellDummyPrefab);
        dummy.transform.position = src.transform.position;
        dummy.GetComponent<SpriteRenderer>().color = color;

        // Move dummy GO
        int i = 0;
        GemCell moveSrc = src;
        GemCell moveDest = null;
        for (; i < movePath.Count; i++)
        {
            float t = 0.0f;
            moveDest = movePath[i];

            while (t <= m_gemMovementTime)
            {
                t += Time.deltaTime / m_gemMovementTime;
                dummy.transform.position = Vector3.Lerp(moveSrc.transform.position, moveDest.transform.position, t);
                yield return null;
            }

            dummy.transform.position = moveDest.transform.position;

            moveSrc = moveDest;
        }

        dummy.transform.position = new Vector3(-99.0f, -99.0f, -100.0f);
        Destroy(dummy);

        // The cell is moved to dest.
        // If dest a previewed gem, we need to move it else where, but only if the next hit check fails.
        // So, save the destination's gem first;
        GemCell dest = movePath[movePath.Count - 1];

        int colorIdxPreview = -1;
        GemCell.GemType typePreview = GemCell.GemType.NONE;
        int replacementIndex = -1;
        for (int k = 0; k < m_previewQueue.Count; k++)
        {
            if (m_previewQueue[k] == dest)
            {
                replacementIndex = k;
                break;
            }
        }
        if (replacementIndex != -1)
        {
            typePreview = m_previewQueue[replacementIndex].Type;
            colorIdxPreview = m_previewQueue[replacementIndex].ColorIndex;
        }


        // Set the destination gem
        dest.SetGemIdle(typeSrc, colorIdxSrc);


        // Hit check
        List<List<GemCell>> cellsToDestroy;
        bool hit = HitCheck(dest, out cellsToDestroy);

        // Hit: nothing happens, the saved previewed gem stays where it is
        if (hit)
        {
            int count = 0;
            foreach (var line in cellsToDestroy)
            {
                foreach (var cell in line)
                {
                    cell.DestroyGem();
                    // Add points

                    m_emptyCellSet.Add(cell); // Add back to empty set
                }
                count += line.Count;
            }

            GameManager.Instance.RegisterHit(count);

            m_allowPlayerControl = true;
        }
        else
        {
            // The new gem blocks the old one, move the old gem elsewhere
            if (replacementIndex != -1)
            {
                GemCell replacement = PopAnEmptyGemCell();

                if (replacement == null)
                    BoardFillled?.Invoke(this, EventArgs.Empty);

                m_previewQueue[replacementIndex] = replacement;
                m_previewQueue[replacementIndex].SetAsPreview(typePreview, colorIdxPreview);
            }

            PlayerTurnDone?.Invoke(this, EventArgs.Empty);
        }
    }

    //---------// Pathfinding algorithm
    private List<GemCell> GetMovePath(GemCell src, GemCell dest)
    {
        HashSet<GemCell> openSet = new HashSet<GemCell> { src };
        HashSet<GemCell> closedSet = new HashSet<GemCell>();

        for (int i = 0; i < m_boardSize; i++)
        {
            for (int j = 0; j < m_boardSize; j++)
            {
                m_grid[i, j].PathFindingPrevNode = null;
                m_grid[i, j].CummulativeGCost = 99999;
            }
        }

        src.CummulativeGCost = 0;

        while (openSet.Count > 0)
        {
            GemCell curr = GetLowestFScore(openSet, dest);
            if (curr.X() == dest.X() && curr.Y() == dest.Y())
                return MakePath(src, dest);

            int[] xs = { curr.X() - 1,  curr.X() + 1,   curr.X(),       curr.X() };
            int[] ys = { curr.Y(),      curr.Y(),       curr.Y() - 1,   curr.Y() + 1 };

            for (int i = 0; i < 4; ++i)
            {
                if (IsInsideBoard(xs[i], ys[i]) && !closedSet.Contains(m_grid[xs[i], ys[i]] ) && !m_grid[xs[i], ys[i]].IsBlocking(src))
                {
                    int gFromCurr = curr.CummulativeGCost + 1;

                    if (gFromCurr < m_grid[xs[i], ys[i]].CummulativeGCost)
                    {
                        m_grid[xs[i], ys[i]].CummulativeGCost = gFromCurr;
                        m_grid[xs[i], ys[i]].PathFindingPrevNode = curr;
                    }

                    openSet.Add(m_grid[xs[i], ys[i]]);
                }
            }

            closedSet.Add(curr);
        }

        return null;
    }

    private GemCell GetLowestFScore(HashSet<GemCell> l, GemCell dest)
    {
        int max = 999;
        GemCell res = null;

        foreach (var cell in l)
        {
            int f = cell.Distance(dest) + cell.CummulativeGCost;
            if (f < max)
            {
                res = cell;
                max = f;
            }
        }

        if (res != null) l.Remove(res);
        return res;
    }
    
    private List<GemCell> MakePath(GemCell src, GemCell dest)
    {
        var list = new List<GemCell>();
        GemCell curr = dest;
        while (curr != src)
        {
            list.Add(curr);
            curr = curr.PathFindingPrevNode;
        }

        list.Reverse();
        return list;
    }

    private bool HitCheck(GemCell cell, out List<List<GemCell>> cellsToDestroy)
    {
        cellsToDestroy = new List<List<GemCell>>();
        int[] dx = { -1, -1, 0 , 1};
        int[] dy = { -1, 1, 1, 0 };
        int[] explodeLeft = { -1, -1, -1, -1 };
        int[] explodeRight = { -1, -1, -1, -1 };

        for (int i = 0; i < 4; i++)
        {
            GemCell curr = cell;
            GemCell toCheck = null;
            var row = new List<GemCell>();
            int x = cell.X() - dx[i], y = cell.Y() - dy[i];
            while (IsInsideBoard(x, y))
            {
                toCheck = m_grid[x, y];
                if (!curr.IsMatch(toCheck))
                    break;

                row.Add(toCheck);
                curr = toCheck;
                x = x - dx[i];
                y = y - dy[i];
            }

            x = cell.X() + dx[i];
            y = cell.Y() + dy[i];
            while (IsInsideBoard(x, y))
            {
                toCheck = m_grid[x, y];
                if (!curr.IsMatch(toCheck))
                    break;

                row.Add(toCheck);
                curr = toCheck;
                x = x + dx[i];
                y = y + dy[i];
            }

            if (row.Count + 1 >= 5) cellsToDestroy.Add(row); 
        }

        if (cellsToDestroy.Count > 0) cellsToDestroy[0].Add(cell);
        return cellsToDestroy.Count > 0;
    }


    private void ClearGrid()
    {
        for (int i = 0; i < m_boardSize; i++)
        {
            for (int j = 0; j < m_boardSize; j++)
            {
                m_grid[i, j].Reset();
            }
        }
    }


    //-----------// Helper functions
    private bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < m_boardSize && y >= 0 && y < m_boardSize;
    }

    private Vector3 GetLocalPos(int x, int y)
    {
        int pX = c_cellBorderPixelSize * (x + 1) + c_cellPixelSize * x;
        int pY = c_cellBorderPixelSize * (y + 1) + c_cellPixelSize * y;

        return new Vector3(m_topLeftCoord.x + (float)pX / c_boardPixelSize * m_boardWorldSpaceSize,
                           m_topLeftCoord.y - (float)pY / c_boardPixelSize * m_boardWorldSpaceSize,
                           -1);
    }

    public int CountEmptyCells()
    {
        return m_emptyCellSet.Count;
    }

    private GemCell PopAnEmptyGemCell()
    {
        GemCell cell = null;
        while (m_emptyCellSet.Count > 0)
        {
            cell = m_emptyCellSet.ElementAt(m_rng.Next(0, m_emptyCellSet.Count));
            m_emptyCellSet.Remove(cell);
            if (cell.Type == GemCell.GemType.NONE)
                break;
        }
        return cell;
    }

    private GemCell.GemType GetAppropriateGemType()
    {
        return GemCell.GemType.NORMAL;
    }
    private int GetAppropriateGemColorIndex()
    {
        return UnityEngine.Random.Range(0, GemCell.ColorCount);
    }
}
