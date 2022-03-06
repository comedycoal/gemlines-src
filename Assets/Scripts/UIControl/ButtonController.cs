using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    [SerializeField] string m_name;
    [SerializeField] Text m_title;

    public void OnClick()
    {
        if (m_name == "new_game")
        {
            if (GameManager.Instance.CurrentPhase == GameManager.Phase.NONE)
            {
                m_title.text = "Give up!";
                GameManager.Instance.NewGame();
            }
            else
            {
                m_title.text = "New Game";
                GameManager.Instance.EndGame();
            }
        }
    }
}
