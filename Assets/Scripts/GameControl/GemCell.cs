using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemCell : MonoBehaviour
{
    public enum GemType
    {
        NONE,
        NORMAL,
        GHOST,
        WILD,
        CLEANER
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

    private bool m_isInPlay;
    private bool m_isSelectible;

    private SpriteRenderer m_sprRdr;
    private Animator m_animator;

    [SerializeField] private ColorSet m_colorSet;
    [SerializeField] private GemManualAnimator m_manualAnimator;


    //################# Properties
    public GemType Type => m_type;
    public int ColorIndex => m_colorIndex;
    public Color GemColor => m_sprRdr.color;

    /// <summary>
    /// Retrieves whether a cell is in play.
    /// A cell is in play if it has a gem (not a previewed one)
    /// </summary>
    public bool IsInPlay => m_isInPlay && Type != GemType.NONE;

    /// <summary>
    /// Retrieves whether a cell is selecitble.
    /// A cell is selectible if it is not undergoing the "SMALL_TO_BIG" animation
    /// </summary>
    public bool IsSelectible => m_isSelectible;

    public int CummulativeGCost { get; set; }
    public GemCell PathFindingPrevNode { get; set; }


    //################# Methods
    private void Awake()
    {
        m_sprRdr = GetComponent<SpriteRenderer>();
        m_animator = GetComponent<Animator>();
        m_sprRdr.sprite = null;
    }

    private void Start()
    {
        Reset();
    }

    private void Update()
    {
        
    }

    /// <summary>
    /// React when the cell is selected
    /// </summary>
    public void OnSelected()
    {
        if (m_isSelectible)
        {
            m_animator.SetBool("Pulsing", true);
        }
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
        m_type = type;
        m_colorIndex = colorIndex;

        // TODO: Change sprites, anims, and all that
        if (colorIndex == -1)
        {
            m_sprRdr.color = Color.white;
        }
        else if (type == GemType.WILD)
        {
            // TO DO: rapid color switch
        }
        else
            m_sprRdr.color = m_colorSet.GetColor(colorIndex);
    }

    /// <summary>
    /// Set a gem of type <paramref name="type"/> and color <paramref name="colorIndex"/>, but do not allow them in play yet
    /// </summary>
    /// <param name="type">The type of the gem to set</param>
    /// <param name="colorIndex">The color of the gem to set</param>
    public void SetAsPreview(GemType type, int colorIndex)
    {
        m_isInPlay = false;
        m_isSelectible = true;
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
        m_isInPlay = true;
        m_isSelectible = true;
        m_animator.SetInteger("TransitionState", (int)AnimState.IDLE);
        m_animator.SetTrigger("ImmediatePopup");
        SetGem(type, colorIndex);
    }

    /// <summary>
    /// Convert a cell to idle state from preview state (if it is in preview state at invocation).
    /// An animation is set, and the cell will be in play and selectible once it is done.
    /// </summary>
    public void ActualizeFromPreview()
    {
        m_isInPlay = true;
        m_animator.SetInteger("TransitionState", (int)AnimState.IDLE);
    }

    public void SetSelectible(bool value)
    {
        m_isSelectible = value;
    }

    /// <summary>
    /// Reset the cell to empty state, mostly used in START game phase
    /// </summary>
    public void Reset()
    {
        SetGem(GemType.NONE, -1);
        m_isInPlay = false;
        m_isSelectible = true;
        m_animator.SetInteger("TransitionState", 0);
        m_animator.Play("Gem_null");
    }

    /// <summary>
    /// Destroy the gem at the cell (if any).
    /// </summary>
    public void DestroyGem()
    {
        if (IsInPlay)
        {
            // Play egregious animation
            m_manualAnimator.DoDestroyAnimation();
            
        }
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
    public bool IsMatch(GemCell other, GemCell src = null)
    {
        if (!other.IsInPlay || !IsInPlay) return false;
        return other.Type == GemType.WILD || Type == GemType.WILD || other.m_colorIndex == m_colorIndex;
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

    public int Distance(GemCell other)
    {
        return Math.Abs(X() - other.X()) + Math.Abs(Y() - other.Y());
    }

    public bool IsBlocking(GemCell src)
    {
        if (src.Type == GemType.GHOST) return false;
        return IsInPlay;
    }
}
