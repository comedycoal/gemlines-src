using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemManualAnimator : MonoBehaviour
{
    private bool m_performing;

    [SerializeField] private GameObject m_dummyFallPrefab;
    [SerializeField] private SpriteRenderer m_extraRdr;
    [SerializeField] private GemCell m_main;
    [SerializeField] private ParticleSystem m_particleSystem;

    [SerializeField] private float m_destroyAnimTime = 0.5f;
    [SerializeField] private float m_shakeSpeed = 0.3f;
    [SerializeField] private float m_shakeAmount = 0.5f;
    [SerializeField] private float m_ghostTransparencyThres = 0.8f;
    [SerializeField] private float m_ghostTransparencyOscillationSpeed = 0.6f;
    [SerializeField] private float m_wildSpeed = 0.2f;

    [SerializeField] private Sprite m_cleanerHorizontal;
    [SerializeField] private Sprite m_cleanerVertical;
    [SerializeField] private Sprite m_blockSprite;

    private void Awake()
    {
        m_particleSystem.Stop();
    }

    public void SetCleaner(bool horizontal)
    {
        m_extraRdr.sprite = horizontal ? m_cleanerHorizontal : m_cleanerVertical;
    }

    public void ClearCleaner()
    {
        m_extraRdr.sprite = null;
    }

    public void SetAsBlock(bool value, Color bColor)
    {
        if (value)
        {
            m_extraRdr.sprite = m_blockSprite;
            m_extraRdr.color = bColor;
        }
        else
        {
            m_extraRdr.sprite = null;
            m_extraRdr.color = Color.white;
        }
    }

    private Coroutine m_wildMode;
    private Color m_original;
    private bool m_stopWild;
    public void SetWildMode(Color original)
    {
        if (m_wildMode != null) return;
        m_original = original;
        m_stopWild = false;
        var sprRdr = m_main.GetComponent<SpriteRenderer>();
        m_wildMode = StartCoroutine(WildCoroutine(sprRdr));
    }

    public void CancelWildMode()
    {
        if (m_wildMode != null)
        {
            m_stopWild = true;
            m_wildMode = null;
        }
    }

    private IEnumerator WildCoroutine(SpriteRenderer rdr)
    {
        while (!m_stopWild)
        {
            rdr.color = Color.HSVToRGB((Time.time * m_wildSpeed) % 1.0f, 1.0f, 1.0f);
            yield return null;
        }

        m_main.GetComponent<SpriteRenderer>().color = m_original;
    }


    private Coroutine m_ghostRoutine;
    private bool m_stopGhost;
    public void SetGhostMode()
    {
        if (m_ghostRoutine != null) return;
        m_stopGhost = false;
        var sprRdr = m_main.GetComponent<SpriteRenderer>();
        m_ghostRoutine = StartCoroutine(GhostCoroutine(sprRdr));
    }

    public void CancelGhostMode()
    {
        if (m_ghostRoutine != null)
        {
            m_stopGhost = true;
            m_ghostRoutine = null;
        }
    }

    private IEnumerator GhostCoroutine(SpriteRenderer rdr)
    {
        while (!m_stopGhost)
        {
            Color color = rdr.color;
            color.a = Mathf.Clamp01(0.5f + 0.5f * Mathf.Sin(m_ghostTransparencyOscillationSpeed * Time.time) * m_ghostTransparencyThres);
            rdr.color = color;
            yield return null;
        }

        Color a = rdr.color;
        a.a = 1.0f;
        rdr.color = a;
    }


    public void ClearSpecialEffects(Color colorToReset)
    {
        m_original = colorToReset;
        SetAsBlock(false, Color.white);
        CancelWildMode();
        CancelGhostMode();
        ClearCleaner();
    }


    public void DoDestroyAnimation(float delayDestroyTime = -1.0f, GemCell.GemType replaceType = GemCell.GemType.NONE, int color = -1, bool isPreview = false)
    {
        if (m_performing) return;
        m_performing = true;
        StartCoroutine(DestroyCoroutine(delayDestroyTime, replaceType, color, isPreview));
    }


    private Sprite m_dummyFallSprite;
    private IEnumerator DestroyCoroutine(float delayDestroyTime = -1.0f, GemCell.GemType replaceType = GemCell.GemType.NONE, int color = -1, bool isPreview = false)
    {
        if (delayDestroyTime > 0.0f) yield return new WaitForSeconds(delayDestroyTime);
        m_particleSystem.Stop();

        var main = m_particleSystem.main;
        main.duration = m_destroyAnimTime;
        main.startColor = m_main.GetComponent<SpriteRenderer>().color;

        m_particleSystem.Play();
        float time = 0f;

        var originalPos = transform.position;
        Vector3 shakeDirection = new Vector3(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 0f).normalized;
        while (time <= m_destroyAnimTime)
        {
            transform.position = originalPos + shakeDirection * Mathf.Sin(m_shakeSpeed * time) * m_shakeAmount;
            time += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;

        m_dummyFallSprite = m_main.Type == GemCell.GemType.BLOCK ? m_extraRdr.sprite : GetComponent<SpriteRenderer>().sprite;
        ClearSpecialEffects(m_main.GemColor);

        DoRandomFall();
        m_main.Reset();
        if (replaceType != GemCell.GemType.NONE)
        {
            if (isPreview)
                m_main.SetAsPreview(replaceType, color);
            else
                m_main.SetGemIdle(replaceType, color);
        }

        m_performing = false;
    }

    private void DoRandomFall()
    {
        var go = Instantiate(m_dummyFallPrefab);
        go.transform.position = transform.position + new Vector3(0f,0f,-1f);
        Vector2 force = new Vector2(Random.Range(-3.0f, 3.0f), Random.Range(2.0f, 5.0f));

        var sprRdr = go.GetComponent<SpriteRenderer>();
        var rb = go.GetComponent<Rigidbody2D>();

        sprRdr.sprite = m_dummyFallSprite;
        sprRdr.color = GetComponent<SpriteRenderer>().color;
        rb.AddForce(force * 100f);
    }
}
