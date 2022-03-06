using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugText : MonoBehaviour
{
    [SerializeField] BoardController m_board;
    void Update()
    {
        GetComponent<Text>().text = "Empty Cells: " + m_board.CountEmptyCells();
    }
}
