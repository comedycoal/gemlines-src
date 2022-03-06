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

    private void Awake()
    {
        m_particleSystem.Stop();
    }

    public void DoDestroyAnimation()
    {
        if (m_performing) return;
        m_performing = true;
        StartCoroutine(DestroyCoroutine());
    }

    private IEnumerator DestroyCoroutine()
    {
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

        DoRandomFall();
        m_main.Reset();

        m_performing = false;
    }

    private void DoRandomFall()
    {
        var go = Instantiate(m_dummyFallPrefab);
        go.transform.position = transform.position + new Vector3(0f,0f,-1f);
        Vector2 force = new Vector2(Random.Range(-3.0f, 3.0f), Random.Range(2.0f, 5.0f));

        var sprRdr = go.GetComponent<SpriteRenderer>();
        var rb = go.GetComponent<Rigidbody2D>();

        sprRdr.sprite = GetComponent<SpriteRenderer>().sprite;
        sprRdr.color = GetComponent<SpriteRenderer>().color;
        rb.AddForce(force * 100f);
    }
}
