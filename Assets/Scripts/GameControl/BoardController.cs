using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    //################# Members
    private const int c_boardSize = 9;
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

        m_grid = new GemCell[c_boardSize, c_boardSize];
        m_rng = new System.Random(Environment.TickCount);
        CreateGameObjectGrid();
        m_sumWeight = m_normalWeight + m_ghostWeight + m_wildWeight + m_cleanerWeight;
        m_emptyCellSet = new HashSet<GemCell>();
    }

    /// <summary>
    /// Create Game object grid, used in <see cref="Awake"/> to initialize game board.
    /// </summary>
    private void CreateGameObjectGrid()
    {
        Vector3 colliderSize = new Vector3((float)c_cellPixelSize / c_boardPixelSize * m_boardWorldSpaceSize,
                                           (float)c_cellPixelSize / c_boardPixelSize * m_boardWorldSpaceSize, 0);

        for (int i = 0; i < c_boardSize; i++)
        {
            for (int j = 0; j < c_boardSize; j++)
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
        for (int i = 0; i < c_boardSize; i++)
        {
            for (int j = 0; j < c_boardSize; j++)
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
                                count+=cell.Score();
                                cell.DestroyGem();
                                m_emptyCellSet.Add(cell); // Add back to empty set
                            }
                        }
                    }
                    GameManager.Instance.RegisterScore(count);
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

    private const int GUARD = 20;
    public void LoadFromStrings(string state1, string state2)
    {
        int i = 0;
        m_previewQueue = new List<GemCell>();
        m_emptyCellSet = new HashSet<GemCell>();
        while (i < c_boardSize * c_boardSize)
        {
            GemCell curr = m_grid[i / c_boardSize, i % c_boardSize];
            bool isPreview = false;
            char encoded = state1[i];
            if (char.IsUpper(encoded))
            {
                isPreview = true;
                encoded = char.ToLower(encoded);
            }

            GemCell.GemType type = (GemCell.GemType)(encoded - 'a');
            int color = state2[i] - '0';

            if (isPreview)
            {
                m_previewQueue.Add(curr);
                curr.SetAsPreview(type, color);
            }
            else
                curr.SetGemIdle(type, color);

            if (curr.Type == GemCell.GemType.NONE)
                m_emptyCellSet.Add(curr);

            ++i;
        }

        PreviewSetup?.Invoke(this, m_previewQueue.ConvertAll(x => x.GemColor).ToArray());
    }

    public void StringifyBoardState(out string state1, out string state2)
    {
        state1 = "";
        state2 = "";
        for (int i = 0; i < c_boardSize; i++)
        {
            for (int j = 0; j < c_boardSize; j++)
            {
                char type = (char)('a' + (int)m_grid[i, j].Type);
                if (m_grid[i, j].IsPreview) type = char.ToUpper(type);
                state1 += type;
                state2 += Math.Max(0, m_grid[i, j].ColorIndex);
            }
        }
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
    }

    private void HandleGamePause(object sender, bool isPaused)
    {
        var temp = new Vector3(0f, 0f, 1000.0f);
        if (isPaused)
        {
            m_allowPlayerControl = false;
            for (int i = 0; i < c_boardSize; i++)
            {
                for (int j = 0; j < c_boardSize; j++)
                {
                    m_grid[i, j].transform.position -= temp;
                }
            }
        }
        else
        {
            m_allowPlayerControl = true;
            for (int i = 0; i < c_boardSize; i++)
            {
                for (int j = 0; j < c_boardSize; j++)
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

        for (int y = 0; y < c_boardSize; ++y)
        {
            int i = -1;
            int j = y+1;
            while (++i < c_boardSize && --j >= 0)
                m_grid[i, j].SetAsDead(m_gameOverDestroyDelay);

            yield return new WaitForSeconds(m_gameOverLineDelay);
        }

        for (int x = 0; x < c_boardSize-1; ++x)
        {
            int i = x;
            int j = c_boardSize;
            while (++i < c_boardSize && --j >= 0)
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
        m_emptyCellSet.Clear();
        for (int i = 0; i < c_boardSize; i++)
        {
            for (int j = 0; j < c_boardSize; j++)
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
                            count += cell.Score();
                            cell.DestroyGem();
                            m_emptyCellSet.Add(cell); // Add back to empty set
                        }
                    }
                }
                GameManager.Instance.RegisterScore(count);
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

        if (hit)
        {
            int count = 0;
            foreach (var line in cellsToDestroy)
            {
                foreach (var cell in line)
                {
                    if (cell.HasGem)
                    {
                        count += cell.Score();
                        if (cell == dest)
                        {
                            cell.DestroyGem(-1, false, true, typePreview, colorIdxPreview, true);
                        }
                        else
                        {
                            cell.DestroyGem();
                            m_emptyCellSet.Add(cell); // Add back to empty set
                        }
                    }
                }
            }

            // Since the moved gem is destroy, recreate the previewed gem at its own place;


            GameManager.Instance.RegisterScore(count);

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

            if (!GameManager.Instance.IsTimeAttack)
                PlayerTurnDone?.Invoke(this, EventArgs.Empty);
            else
                m_allowPlayerControl = true;
        }
    }

    //---------// Pathfinding algorithm
    private List<GemCell> GetMovePath(GemCell src, GemCell dest)
    {
        List<GemCell> openSet = new List<GemCell> { src };
        HashSet<GemCell> closedSet = new HashSet<GemCell>();

        for (int i = 0; i < c_boardSize; i++)
        {
            for (int j = 0; j < c_boardSize; j++)
            {
                m_grid[i, j].PathFindingPrevNode = null;
                m_grid[i, j].CummulativeGCost = 99999;
            }
        }

        src.CummulativeGCost = 0;

        List<int> xs = new List<int>();
        List<int> ys = new List<int>();
        while (openSet.Count > 0)
        {
            GemCell curr = GetLowestFScore(openSet, dest);
            if (curr.X() == dest.X() && curr.Y() == dest.Y())
                return MakePath(src, dest);
            xs.Clear();
            ys.Clear();
            // Order: Try left, right, up, then down


            if (curr.PathFindingPrevNode != null)
            {
                int dx = curr.X() - curr.PathFindingPrevNode.X();
                int dy = curr.Y() - curr.PathFindingPrevNode.Y();

                // First: In the heading direction
                xs.Add(curr.X() + dx);
                ys.Add(curr.Y() + dy);

                // Second: Turn sideway
                if (dx == 0)
                {
                    xs.Add(curr.X() - 1); xs.Add(curr.X() + 1);
                    ys.Add(curr.Y()); ys.Add(curr.Y());
                }
                else if (dy == 0)
                {
                    xs.Add(curr.X()); xs.Add(curr.X());
                    ys.Add(curr.Y() - 1); ys.Add(curr.Y() + 1);
                }

                // Never ever turn back
            }
            else
            {
                xs.Add(curr.X() - 1);
                xs.Add(curr.X() + 1);
                xs.Add(curr.X());
                xs.Add(curr.X());
                ys.Add(curr.Y());
                ys.Add(curr.Y());
                ys.Add(curr.Y() - 1);
                ys.Add(curr.Y() + 1);
            }

            for (int i = 0; i < xs.Count; ++i)
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

    private GemCell GetLowestFScore(List<GemCell> l, GemCell dest)
    {
        int max = 999;
        GemCell res = null;

        foreach (var cell in l)
        {
            int f = cell.HScore(dest) + cell.CummulativeGCost;
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


            curr = cell;
            toCheck = null;
            if (row.Count == 0) commonColor = -2;
            if (curr.ColorIndex >= 0 && commonColor < 0) commonColor = curr.ColorIndex;

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
        for (int i = 0; i < c_boardSize; i++)
        {
            for (int j = 0; j < c_boardSize; j++)
            {
                m_grid[i, j].Reset();
            }
        }
    }


    //-----------// Helper functions
    private bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < c_boardSize && y >= 0 && y < c_boardSize;
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
        return m_emptyCellSet != null ? m_emptyCellSet.Count : c_boardSize*c_boardSize;
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
