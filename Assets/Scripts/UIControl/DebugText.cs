using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugText : MonoBehaviour
{
    [SerializeField] BoardController m_board;
    [SerializeField] TimeAttackTimer m_timeAtkTimer;

    private void Start()
    {
        if (!GameManager.Instance.IsDebug) gameObject.SetActive(false);
    }

    void Update()
    {
        GetComponent<Text>().text = "Empty Cells: " + m_board.CountEmptyCells();
        GetComponent<Text>().text += "\nTurn: " + GameManager.Instance.Turn;
        GetComponent<Text>().text += "\nEditor: " + (GameManager.Instance.EditorOn ? "ON" : "OFF");
        if (GameManager.Instance.IsTimeAttack) GetComponent<Text>().text += "\nTAtk Timer: " + m_timeAtkTimer.CurrentTime;
    }
}
