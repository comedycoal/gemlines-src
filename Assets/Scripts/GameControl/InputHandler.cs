using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private BoardController m_board;

    private void Start()
    {
        
    }

    private GemCell RegisterHit()
    {
        Vector3 origin = new Vector3(-99, -99, 0);
        if (Input.mousePresent)
        {
            origin = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        else
        {
            for (int i = 0; i < Input.touchCount; ++i)
                if (Input.GetTouch(i).phase.Equals(TouchPhase.Began))
                {
                    origin = Camera.main.ScreenToWorldPoint(Input.GetTouch(i).position);
                    break;
                }
        }

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.zero);
        if (hit.collider != null && hit.transform.tag == "Gem")
        {
            return hit.transform.GetComponent<GemCell>();
        }
        return null;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var gemCell = RegisterHit();
            if (gemCell != null)
            {
                if (GameManager.Instance.EditorOn) m_board.EditorClick(gemCell);
                else m_board.PlayerClick(gemCell);
            }
        }

        if (GameManager.Instance.EditorOn)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                m_board.EditorCycleColor(1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                m_board.EditorCycleColor(-1);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                m_board.EditorCycleType(-1);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                m_board.EditorCycleType(1);
            }
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            GameManager.Instance.ToggleEditor();
        }
    }
}
