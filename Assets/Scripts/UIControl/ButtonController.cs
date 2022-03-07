using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    [SerializeField] string m_name;
    [SerializeField] Image m_image;
    [SerializeField] Text m_title;

    private EventHandler<GameManager.Phase> m_gamePhaseAdhocHandler;

    public void OnClick()
    {
        if (m_name == "new_game")
        {
            if (GameManager.Instance.CurrentPhase == GameManager.Phase.NONE)
            {
                m_title.text = "Give up!";
                m_image.color = Color.red;
                GameManager.Instance.NewGame();
            }
            else
            {
                m_title.text = "";
                GameManager.Instance.SetEndGame();
                m_gamePhaseAdhocHandler = (s, e) => 
                {
                    if (e == GameManager.Phase.NONE)
                    {
                        m_title.text = "New Game";
                        m_image.color = Color.white;
                        if (m_gamePhaseAdhocHandler != null)
                        {
                            GameManager.Instance.EnterPhaseEvent -= m_gamePhaseAdhocHandler;
                            m_gamePhaseAdhocHandler = null;
                        }
                    }
                };
                GameManager.Instance.EnterPhaseEvent += m_gamePhaseAdhocHandler;
            }
        }
        else if (m_name == "time_atk")
        {
            if (!GameManager.Instance.IsTimeAttack)
            {
                m_title.color = Color.green;
                GameManager.Instance.SetTimeAttack(true);
            }
            else if (GameManager.Instance.IsInGame)
            {
                m_title.color = Color.white;
                GameManager.Instance.SetTimeAttack(false);
            }
        }
        else if (m_name == "pause")
        {
            if (!GameManager.Instance.IsInGame) return;
            bool isPaused = GameManager.Instance.TogglePause();
            if (isPaused)
            {
                m_title.text = "Resume";
            }
            else
            {
                m_title.text = "Pause";
            }
        }
        else if (m_name == "exit")
        {
            // Save
            //SaveManager.Instance.SaveGame();

            Application.Quit();
        }
    }
}
