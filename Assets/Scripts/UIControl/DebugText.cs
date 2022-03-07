using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugText : MonoBehaviour
{
    [SerializeField] BoardController m_board;

    private void Start()
    {
        if (!GameManager.Instance.IsDebug) gameObject.SetActive(false);
    }

    void Update()
    {
        GetComponent<Text>().text = "Empty Cells: " + m_board.CountEmptyCells();
        GetComponent<Text>().text += "\nTurn: " + GameManager.Instance.Turn;
        GetComponent<Text>().text += "\nEditor: " + (GameManager.Instance.EditorOn ? "ON" : "OFF");
    }
}
