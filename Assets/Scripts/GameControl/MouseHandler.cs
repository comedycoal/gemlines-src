using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseHandler : MonoBehaviour
{
    [SerializeField] private BoardController m_board;

    private void Start()
    {
        
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 origin = new Vector3(-99,-99,0);
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
                m_board.PlayerClick(hit.transform.GetComponent<GemCell>());
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            
        }
    }
}
