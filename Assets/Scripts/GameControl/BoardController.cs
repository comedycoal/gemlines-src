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
    [SerializeField] private float m_gameOverDestroyDelay = 1.5f;
    [SerializeField] private float m_gameOverLineDelay = 0.1f;
    [SerializeField] private float m_gameEndWait = 1.0f;

    private const int c_cellPixelSize = 18;
    private const int c_cellBorderPixelSize = 1;
    private const int c_boardPixelSize = 172;

    private float m_boardWorldSpaceSize;
    private bool m_allowPlayerControl = false;
    private Vector3 m_topLeftCoord;

    [SerializeField] private GameObject m_gemCellPrefab;
    [SerializeField] private GameObject m_gemCellDummyPrefab;

    [SerializeField] private int m_normalWeight = 73;
    [SerializeField] private int m_ghostWeight = 10;
    [SerializeField] private int m_wildWeight = 10;
    [SerializeField] private int m_cleanerWeight = 5;
    [SerializeField] private int m_blockWeight = 2;

    private int m_sumWeight;

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
    /// This event is invoked when the board is filled -> GameManager set Phase to GAME_END
    /// </summary>
    public event EventHandler BoardFillled;

    /// <summary>
    /// This event is invoked when the game end animation is finished -> GameManager set Phase to NONE
    /// </summary>
    public event EventHandler GameEndSequenceFinished;


    //################# Methods

    //----------// Awake set-up
    private void Awake()
    {
        var collider = GetComponent<Collider2D>();
        m_boardWorldSpaceSize = collider.bounds.size.x;
        m_topLeftCoord = new Vector3((c_cellPixelSize / 2) / (float)c_boardPixelSize * m_boardWorldSpaceSize - m_boardWorldSpaceSize / 2,
                                    -(c_cellPixelSize / 2) / (float)c_boardPixelSize * m_boardWorldSpaceSize + m_boardWorldSpaceSize / 2, 0);

        m_grid = new GemCell[m_boardSize, m_boardSize];
        m_rng = new System.Random(Environment.TickCount);
        CreateGameObjectGrid();
        m_sumWeight = m_normalWeight + m_ghostWeight + m_wildWeight + m_cleanerWeight;
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

    public void HitCheckAll()
    {
        for (int i = 0; i < m_boardSize; i++)
        {
            for (int j = 0; j < m_boardSize; j++)
            {
                List<List<GemCell>> cellsToDestroy;
                bool hit = HitCheck(m_grid[i,j], out cellsToDestroy, false);

                if (hit)
                {
                    int count = 0;
                    foreach (var line in cellsToDestroy)
                    {
                        foreach (var cell in line)
                        {
                            if (cell.HasGem)
                            {
                                count++;
                                cell.DestroyGem();
                                m_emptyCellSet.Add(cell); // Add back to empty set
                            }
                        }
                    }
                    GameManager.Instance.RegisterHit(count);
                }
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


    //----------// Debug Editor methods
    [SerializeField] GameObject m_editorSelectorObj;
    private GemCell m_editorSelection = null;
    private int m_editorPrevColor = -1;

    public void EditorClick(GemCell gemCell)
    {
        if (gemCell != null)
        {
            m_editorSelection = gemCell;
            m_editorSelectorObj.transform.position = m_editorSelection.transform.position;
            m_editorPrevColor = Math.Max(gemCell.ColorIndex, 0);
        }
        else
        {
            m_editorSelectorObj.transform.position = new Vector3(-99.0f, -99.0f, 0f);
            m_editorPrevColor = -1;
        }
    }

    public void EditorCycleColor(int v)
    {
        if (m_editorSelection != null)
        {
            GemCell.GemType type = m_editorSelection.Type;
            if (type == GemCell.GemType.NONE) type = GemCell.GemType.NORMAL;
            m_editorPrevColor += v;
            if (m_editorPrevColor >= GemCell.ColorCount) m_editorPrevColor = 0;
            if (m_editorPrevColor < 0) m_editorPrevColor = GemCell.ColorCount - 1;
            m_editorSelection.SetGemIdle(type, m_editorPrevColor);
        }    
    }

    public void EditorCycleType(int v)
    {
        if (m_editorSelection != null)
        {
            int color = m_editorSelection.ColorIndex;
            m_editorSelection.SetGemIdle(GemCell.CycleType(m_editorSelection.Type, v), m_editorPrevColor);
        }
    }


    /// <summary>
    /// Handle events from <see cref="GameManager"/>, each of which signifying a change in the game state.
    /// </summary>
    /// <param name="sender">is irrelevant</param>
    /// <param name="e">The current <see cref="GameManager.Phase"/> that the game is in</param>
    private void HandleGamePhaseEvent(object sender, GameManager.Phase e)
    {
        if (e == GameManager.Phase.NONE)
        {
            m_allowPlayerControl = false;
        }
        else if (e == GameManager.Phase.GAME_START)
        {
            Reset();
            m_allowPlayerControl = false;
            PopulatePreviewCells(m_initialGemCount);
            BoardSetUp?.Invoke(this, EventArgs.Empty);
        }
        else if (e == GameManager.Phase.TURN_START)
        {
            m_allowPlayerControl = false;
            ActualizePreviewedCells();
            PopulatePreviewCells(m_newGemPerTurn);
            PreviewSetup?.Invoke(this, m_previewQueue.ConvertAll(x => x.GemColor).ToArray());
        }
        else if (e == GameManager.Phase.PLAYER_TURN)
        {
            m_allowPlayerControl = true;
        }
        else if (e == GameManager.Phase.GAME_END)
        {
            m_allowPlayerControl = false;
            HandleGameEnd();
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
        var temp = new Vector3(0f, 0f, 1000.0f);
        if (isPaused)
        {
            m_allowPlayerControl = false;
            for (int i = 0; i < m_boardSize; i++)
            {
                for (int j = 0; j < m_boardSize; j++)
                {
                    m_grid[i, j].transform.position -= temp;
                }
            }
        }
        else
        {
            m_allowPlayerControl = true;
            for (int i = 0; i < m_boardSize; i++)
            {
                for (int j = 0; j < m_boardSize; j++)
                {
                    m_grid[i, j].transform.position += temp;
                }
            }
        }
    }

    private void HandleGameEnd()
    {
        StartCoroutine(GameEndCoroutine());
    }

    private IEnumerator GameEndCoroutine()
    {
        yield return new WaitForSeconds(m_gameEndWait);

        for (int y = 0; y < m_boardSize; ++y)
        {
            int i = -1;
            int j = y+1;
            while (++i < m_boardSize && --j >= 0)
                m_grid[i, j].SetAsDead(m_gameOverDestroyDelay);

            yield return new WaitForSeconds(m_gameOverLineDelay);
        }

        for (int x = 0; x < m_boardSize-1; ++x)
        {
            int i = x;
            int j = m_boardSize;
            while (++i < m_boardSize && --j >= 0)
                m_grid[i, j].SetAsDead(m_gameOverDestroyDelay);

            yield return new WaitForSeconds(m_gameOverLineDelay);
        }

        yield return new WaitForSeconds(m_gameOverDestroyDelay);

        GameEndSequenceFinished?.Invoke(this, EventArgs.Empty);
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
    /// Set a fixed number of cells in the board with a type and color,
    /// but do not allow them to be in play yet.
    /// <para>The game over condition is checked inside this method</para>
    /// </summary>
    private void PopulatePreviewCells(int count)
    {
        Color[] forPreview = new Color[count];
        for (int i = 0; i < count; i++)
        {
            GemCell gemCell = PopAnEmptyGemCell();
            if (gemCell == null)
                break;

            m_previewQueue.Add(gemCell);
            gemCell.SetAsPreview(GetAppropriateGemType(), GetAppropriateGemColorIndex());
            forPreview[i] = gemCell.GemColor;
        }

        //if (GameManager.Instance.IsDebug) Debug.Log("Empty cells: " + CountEmptyCells());

        if (m_previewQueue.Count == 0)
        {
            BoardFillled?.Invoke(this, EventArgs.Empty);
            return;
        }

        for (int i = m_previewQueue.Count; i < forPreview.Length; ++i)
            forPreview[i] = new Color(0f, 0f, 0f, 0f);
        PreviewSetup?.Invoke(this, forPreview);
    }

    private void ActualizePreviewedCells()
    {
        if (m_previewQueue == null) return;
        for (int i = 0; i < m_previewQueue.Count; i++)
        {
            m_previewQueue[i].ActualizeFromPreview();

            // Hit Check
            List<List<GemCell>> cellsToDestroy;
            bool hit = HitCheck(m_previewQueue[i], out cellsToDestroy, false);

            if (hit)
            {
                int count = 0;
                foreach (var line in cellsToDestroy)
                {
                    foreach (var cell in line)
                    {
                        if (cell.HasGem)
                        {
                            count++;
                            cell.DestroyGem();
                            m_emptyCellSet.Add(cell); // Add back to empty set
                        }
                    }
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

        // If this is a move for selected GEM
        if (m_selectedCell != null)
        {
            // If player want to move to an "empty" cell
            if (gemCell.IsEmpty)
            {
                var movePath = GetMovePath(m_selectedCell, gemCell);

                m_selectedCell.OnCancelSelected();

                if (movePath != null)
                {
                    PerformMove(m_selectedCell, movePath);
                }

                m_selectedCell = null;
            }
            // If player select another gem
            else if (gemCell != m_selectedCell && gemCell.HasGem && gemCell.IsSelectible)
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
        else if (gemCell.IsSelectible && gemCell.HasGem)
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
        bool hit = HitCheck(dest, out cellsToDestroy, true);

        // Hit: nothing happens, the saved previewed gem stays where it is
        if (hit)
        {
            int count = 0;
            foreach (var line in cellsToDestroy)
            {
                foreach (var cell in line)
                {
                    if (cell.HasGem)
                    {
                        count++;
                        cell.DestroyGem();
                        m_emptyCellSet.Add(cell); // Add back to empty set
                    }
                }
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

    private bool HitCheck(GemCell cell, out List<List<GemCell>> cellsToDestroy, bool playerMove)
    {
        cellsToDestroy = new List<List<GemCell>>();
        int[] dx = { -1, -1, 0 , 1};
        int[] dy = { -1, 1, 1, 0 };

        string ds = "";
        for (int i = 0; i < 4; i++)
        {
            int commonColor = -2;
            GemCell curr = cell;
            GemCell toCheck = null;
            if (curr.ColorIndex >= 0 && commonColor < 0) commonColor = curr.ColorIndex;
            var row = new List<GemCell>();
            int x = cell.X() - dx[i], y = cell.Y() - dy[i];
            
            while (IsInsideBoard(x, y))
            {
                toCheck = m_grid[x, y];
                if (!toCheck.IsMatch(curr, ref commonColor, cell, playerMove))
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
                if (!toCheck.IsMatch(curr, ref commonColor, cell, playerMove))
                    break;

                row.Add(toCheck);
                curr = toCheck;
                x = x + dx[i];
                y = y + dy[i];
            }
            ds += commonColor + ", ";
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
        return m_emptyCellSet != null ? m_emptyCellSet.Count : m_boardSize*m_boardSize;
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
        if (GameManager.Instance.Turn <= 5) return GemCell.GemType.NORMAL;

        int x = m_rng.Next(m_sumWeight);
        if ((x -= m_cleanerWeight) < 0)
            return m_rng.Next(0, 2) == 0 ? GemCell.GemType.CLEANER_HOR : GemCell.GemType.CLEANER_VER;
        else if ((x -= m_wildWeight) < 0)
            return GemCell.GemType.WILD;
        else if ((x -= m_ghostWeight) < 0)
            return GemCell.GemType.GHOST;
        else if ((x -= m_blockWeight) < 0)
            return GemCell.GemType.BLOCK;
        else
            return GemCell.GemType.NORMAL;
    }

    private int GetAppropriateGemColorIndex()
    {
        return UnityEngine.Random.Range(0, GemCell.ColorCount);
    }
}
