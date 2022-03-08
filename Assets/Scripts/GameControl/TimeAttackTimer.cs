using System;
using System.Collections.Generic;
using UnityEngine;

public class TimeAttackTimer : MonoBehaviour
{
    [SerializeField] SpriteRenderer m_spriteRenderer;
    [SerializeField] float m_timeAtkDelay = 3.0f;

    private float m_verticalScale;
    private float m_timePassed;

    private bool m_overallTurnedOn;
    private bool m_ticking;
    private float m_colorH, m_colorV;

    private float m_soundCueTime;

    public float CurrentTime => m_timePassed;

    public bool DoneTicking => !m_ticking;

    //---------// Event
    public event EventHandler TurnEnded;


    private void Awake()
    {
        TurnOff();
        m_spriteRenderer.enabled = false;
        m_verticalScale = transform.localScale.y;
        Color.RGBToHSV(m_spriteRenderer.color, out m_colorH, out _, out m_colorV);
    }

    private void Start()
    {
        m_soundCueTime = m_timeAtkDelay - AudioManager.Instance.GetLength("ding") + 0.4f;
    }

    private bool m_cuePlayed;

    private void Update()
    {
        if (m_overallTurnedOn && m_ticking)
        {
            m_timePassed += Time.deltaTime;

            float ratio = m_timePassed / m_timeAtkDelay;
            // Set scale
            float scale = Mathf.Lerp(0.0f, m_verticalScale, ratio);
            transform.localScale = new Vector3(transform.localScale.x, scale, transform.localScale.z);

            // Set color
            var a = m_spriteRenderer.color.a;
            var color = Color.HSVToRGB(m_colorH, ratio, m_colorV);
            color.a = a;
            m_spriteRenderer.color = color;

            if (!m_cuePlayed && m_timePassed >= m_soundCueTime)
            {
                m_cuePlayed = true;
                AudioManager.Instance.Play("ding");
            }

            if (m_timePassed >= m_timeAtkDelay)
            {
                if (GameManager.Instance.IsTimeAttack)
                {
                    m_ticking = false;
                    TurnEnded?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    public void TurnOn()
    {
        m_overallTurnedOn = true;
        m_spriteRenderer.enabled = true;
        m_ticking = false;
        GameManager.Instance.GamePaused += TogglePause;
    }

    public void TurnOff()
    {
        m_overallTurnedOn = false;
        m_spriteRenderer.enabled = false;
        m_ticking = false;
        GameManager.Instance.GamePaused -= TogglePause;
    }

    public void TogglePause(object sender, bool value)
    {
        if (m_overallTurnedOn)
            m_ticking = !value;
    }

    public void TickOneTurn()
    {
        if (m_overallTurnedOn && !m_ticking)
        {
            m_ticking = true;
            m_timePassed = 0.0f;
            m_cuePlayed = false;
        }
    }
}
