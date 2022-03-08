using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialController : MonoBehaviour
{
    [SerializeField] GameObject[] m_tutorialPages;
    [SerializeField] Button m_doneButton;
    [SerializeField] Button m_leftButton;
    [SerializeField] Button m_rightButton;

    private int m_currPage;

    private void Start()
    {
        m_currPage = 0;
        foreach (var go in m_tutorialPages)
        {
            go.SetActive(false);
        }
        m_tutorialPages[m_currPage].SetActive(true);
    }

    public void OnLeftButton()
    {
        var prevPage = Mathf.Max(0, m_currPage - 1); ;
        if (prevPage == m_currPage) return;
        m_tutorialPages[m_currPage].SetActive(false);
        m_currPage = prevPage;
        m_tutorialPages[m_currPage].SetActive(true);
    }

    public void OnRightButton()
    {
        var nextPage = Mathf.Min(m_tutorialPages.Length - 1, m_currPage + 1);
        if (nextPage == m_currPage) return;
        m_tutorialPages[m_currPage].SetActive(false);
        m_currPage = nextPage;
        m_tutorialPages[m_currPage].SetActive(true);
    }

    public void OnCloseButton()
    {
        Hide();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
