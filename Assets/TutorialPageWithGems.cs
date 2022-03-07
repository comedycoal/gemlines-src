using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialPageWithGems : MonoBehaviour
{
    [SerializeField] public GemCell GhostGem;
    [SerializeField] public GemCell WildGem;
    [SerializeField] public GemCell CleanerGem;
    [SerializeField] public GemCell BlockGem;

    private float m_timePassed;
    private bool m_rendered;

    private void Start()
    {

    }

    private void Update()
    {
        if (!m_rendered)
        {
            m_rendered = true;
            GhostGem.SetGemIdle(GemCell.GemType.GHOST, 0);
            WildGem.SetGemIdle(GemCell.GemType.WILD, -1);
            CleanerGem.SetGemIdle(GemCell.GemType.CLEANER_HOR, -2);
            BlockGem.SetGemIdle(GemCell.GemType.BLOCK, -2);
        }
        m_timePassed += Time.deltaTime;
        if (m_timePassed >= 1.5f)
        {
            m_timePassed = 0.0f;
            CleanerGem.SetGemIdle(CleanerGem.Type == GemCell.GemType.CLEANER_HOR ? GemCell.GemType.CLEANER_VER : GemCell.GemType.CLEANER_HOR, -2);
        }
    }
}
