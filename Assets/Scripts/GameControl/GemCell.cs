using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemCell : MonoBehaviour
{
    public enum GemType
    {
        NONE = 0,
        NORMAL = 1,
        GHOST = 2,
        WILD = 3,
        CLEANER_HOR = 4,
        CLEANER_VER = 5,
        BLOCK = 6
    }

    public static GemType CycleType(GemType b, int v)
    {
        int val = (int)b + v;
        if (val < 0) val = 6;
        if (val > 6) val = 0;
        return (GemType)(val);
    }

    public enum AnimState
    {
        NULL = 0,
        SMALL = 1,
        IDLE = 2
    }

    public static int ColorCount = 7;

    //################# Members
    private int m_x;
    private int m_y;
    private GemType m_type;
    private int m_colorIndex;

    private bool m_isPreview;

    private SpriteRenderer m_sprRdr;
    private Animator m_animator;

    [SerializeField] private ColorSet m_colorSet;
    [SerializeField] private GemManualAnimator m_manualAnimator;


    //################# Properties
    public GemType Type => m_type;
    public int ColorIndex => m_colorIndex;
    public Color GemColor => m_sprRdr.color;

    public bool IsPreview => m_isPreview;

    public bool IsEmpty => m_isPreview || Type == GemType.NONE;

    public bool HasGem => !IsEmpty;

    public bool IsSelectible => HasGem && Type != GemType.BLOCK;

    public bool IsCleaner => m_type == GemType.CLEANER_HOR || m_type == GemType.CLEANER_VER;

    public int CummulativeGCost { get; set; }
    public GemCell PathFindingPrevNode { get; set; }


    //################# Methods
    private void Awake()
    {
        m_sprRdr = GetComponent<SpriteRenderer>();
        m_animator = GetComponent<Animator>();
        m_sprRdr.sprite = null;
        Reset();
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        
    }

    /// <summary>
    /// React when the cell is selected
    /// </summary>
    public void OnSelected()
    {
        m_animator.SetBool("Pulsing", true);
        AudioManager.Instance.Play("gem_selected");
    }

    /// <summary>
    /// React when the cell is deselected
    /// </summary>
    public void OnCancelSelected()
    {
        m_animator.SetBool("Pulsing", false);
    }

    private void SetGem(GemType type, int colorIndex)
    {
        m_colorIndex = colorIndex % m_colorSet.Colors.Length;
        m_type = type;

        m_manualAnimator.ClearSpecialEffects(m_colorSet.GetColor(m_colorIndex));

        if (m_type == GemType.GHOST)
        {
            m_manualAnimator.SetGhostMode();
            m_sprRdr.color = m_colorSet.GetColor(colorIndex);
        }
        else if (m_type == GemType.CLEANER_HOR)
        {
            m_manualAnimator.SetCleaner(true);
            m_colorIndex = -2;
            m_sprRdr.color = Color.white;
        }
        else if (m_type == GemType.CLEANER_VER)
        {
            m_manualAnimator.SetCleaner(false);
            m_colorIndex = -2;
            m_sprRdr.color = Color.white;
        }
        else if (m_type == GemType.WILD)
        {
            m_colorIndex = -1;
            m_manualAnimator.SetWildMode(Color.white);
            m_sprRdr.color = Color.white;
        }
        else if (m_type == GemType.BLOCK)
        {
            m_colorIndex = -2;
            m_sprRdr.color = m_colorSet.DeadColor;
        }
        else if (m_type == GemType.NONE)
        {
            m_colorIndex = -2;
            m_sprRdr.color = m_colorSet.NullColor;
        }
        else if (m_type == GemType.NORMAL)
        {
            m_sprRdr.color = m_colorSet.GetColor(m_colorIndex);
        }
    }

    /// <summary>
    /// Set a gem of type <paramref name="type"/> and color <paramref name="colorIndex"/>, but do not allow them in play yet
    /// </summary>
    /// <param name="type">The type of the gem to set</param>
    /// <param name="colorIndex">The color of the gem to set</param>
    public void SetAsPreview(GemType type, int colorIndex)
    {
        m_isPreview = true;
        m_animator.SetInteger("TransitionState", (int)AnimState.SMALL);
        SetGem(type, colorIndex);
    }

    /// <summary>
    /// Set a gem of type <paramref name="type"/> and color <paramref name="colorIndex"/>, and allow them in play right a way
    /// without undergoing any animations.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="colorIndex"></param>
    public void SetGemIdle(GemType type, int colorIndex)
    {
        m_isPreview = false;
        m_animator.SetTrigger("ImmediatePopup");
        m_animator.SetInteger("TransitionState", (int)AnimState.IDLE);
        SetGem(type, colorIndex);
        if (Type == GemType.BLOCK) m_manualAnimator.SetAsBlock(true, m_colorSet.DeadColor);
    }

    /// <summary>
    /// Convert a cell to idle state from preview state (if it is in preview state at invocation).
    /// An animation is set, and the cell will be in play and selectible once it is done.
    /// </summary>
    public void ActualizeFromPreview()
    {
        m_isPreview = false;
        m_animator.SetInteger("TransitionState", (int)AnimState.IDLE);
        if (Type == GemType.BLOCK)
        {
            m_manualAnimator.SetAsBlock(true, m_colorSet.DeadColor);
        }
        AudioManager.Instance.Play("gem_pop");
    }

    /// <summary>
    /// Reset the cell to empty state, mostly used in START game phase
    /// </summary>
    public void Reset()
    {
        SetGem(GemType.NONE, -2);
        m_isPreview = false;
        m_animator.SetBool("Pulsing", false);
        m_animator.SetInteger("TransitionState", (int)AnimState.NULL);
    }

    /// <summary>
    /// Destroy the gem at the cell (if any).
    /// </summary>
    public void DestroyGem(float delayDestroyTime = -1.0f, bool matched = true, bool forceDestroy = false, GemType replaceType = GemType.NONE, int color = -1, bool isPreview = false)
    {
        if (HasGem)
        {
            // Play egregious animation
            m_manualAnimator.DoDestroyAnimation(delayDestroyTime, replaceType, color, isPreview);
            AudioManager.Instance.Play(matched ? "gem_match" : "gem_destroy");
            return;
        }

        if (forceDestroy)
        {
            Reset();
            AudioManager.Instance.Play(matched ? "gem_match" : "gem_destroy");
        }
    }

    public void SetAsDead(float delayDestroyTime = -1.0f)
    {
        m_sprRdr.color = m_colorSet.DeadColor;
        DestroyGem(delayDestroyTime, false, true);
    }

    /// <summary>
    /// Check whether a Cell is a match with another.
    /// The condition for a match is:
    /// <list type="bullet">
    /// <item>Both cells are in play (non-empty and non-preview)</item>
    /// <item>Either gem is WILD, or they have matching color</item>
    /// </list>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsMatch(GemCell other, ref int commonColorIdx, GemCell src = null, bool playerMove = true)
    {
        // For cleaner only
        if (src != null && src.IsCleaner && playerMove)
            if (src.Type == GemType.CLEANER_HOR) return other.X() == src.X();
            else return other.Y() == src.Y();

        if (IsEmpty || other.IsEmpty || IsCleaner) return false;

        // Check against common color
        if (commonColorIdx < 0 && ColorIndex >= 0) commonColorIdx = ColorIndex;
        return ((commonColorIdx >= 0 && ColorIndex >= 0 && commonColorIdx == ColorIndex)
            || (Type == GemType.WILD && other.ColorIndex >= -1));
    }

    public void setBoardPos(int x, int y)
    {
        m_x = x;
        m_y = y;
    }

    public int X()
    {
        return m_x;
    }

    public int Y()
    {
        return m_y;
    }

    public int HScore(GemCell other)
    {
        return Math.Abs(X() - other.X()) + Math.Abs(Y() - other.Y());
    }

    public bool IsBlocking(GemCell src)
    {
        if (src.Type == GemType.GHOST) return false;
        return Type != GemType.GHOST && HasGem;
    }

    public int Score()
    {
        if (Type == GemType.WILD) return 30;
        if (Type == GemType.BLOCK) return 100;
        else return 10;
    }

    public static bool OnSameLine(GemCell a, GemCell b, GemCell c)
    {
        return (a.X() - b.X() == b.X() - c.X()) && (a.Y() - b.Y() == c.X() - c.Y());
    }
}
