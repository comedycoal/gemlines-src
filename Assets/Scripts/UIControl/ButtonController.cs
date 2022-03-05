using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonController : MonoBehaviour
{
    [SerializeField] string m_name;

    public void OnClick()
    {
        if (m_name == "new_game")
        {
            if (GameManager.Instance.CurrentPhase == GameManager.Phase.NONE)
                GameManager.Instance.NewGame();
            else
            {
                GameManager.Instance.EndGame();
            }
        }
    }
}
