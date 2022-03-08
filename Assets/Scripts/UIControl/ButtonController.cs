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

    public void Start()
    {
        if (m_name == "music")
        {
            bool s = PlayerPrefs.GetInt("Music", 1) == 1 ? true : false;
            AudioManager.Instance.SetMusicState(s);
            m_title.text = "Music:" + (AudioManager.Instance.MusicOn ? "ON" : "OFF");
        }
        else if (m_name == "sound")
        {
            bool s = PlayerPrefs.GetInt("Sound", 1) == 1 ? true : false;
            AudioManager.Instance.SetSoundState(s);
            m_title.text = "SFX:" + (AudioManager.Instance.SoundOn ? "ON" : "OFF");
        }
    }

    public void OnClick()
    {
        bool pressed = false;
        if (m_name == "new_game")
        {
            if (GameManager.Instance.CurrentPhase == GameManager.Phase.NONE)
            {
                m_title.text = "Give up!";
                m_image.color = Color.red;
                GameManager.Instance.NewGame();
                pressed = true;
            }
            else if (GameManager.Instance.CurrentPhase != GameManager.Phase.GAME_END)
            {
                m_title.text = "New Game";
                m_image.color = Color.white;
                GameManager.Instance.SetEndGame();
                pressed = true;
            }
        }
        else if (m_name == "time_atk")
        {
            if (!GameManager.Instance.IsTimeAttack)
            {
                m_title.color = Color.green;
                GameManager.Instance.SetTimeAttack(true);
                pressed = true;
            }
            else if (!GameManager.Instance.IsInGame)
            {
                m_title.color = Color.white;
                GameManager.Instance.SetTimeAttack(false);
                pressed = true;
            }
        }
        else if (m_name == "pause")
        {
            if (GameManager.Instance.IsInGame)
            {
                if (GameManager.Instance.IsPaused)
                {
                    m_title.text = "Pause";
                    GameManager.Instance.SetPauseState(false);
                }
                else
                {
                    m_title.text = "Resume";
                    GameManager.Instance.SetPauseState(true);
                }
                pressed = true;
            }
        }
        else if (m_name == "music")
        {
            AudioManager.Instance.SetMusicState(!AudioManager.Instance.MusicOn);
            m_title.text = "Music:" + (AudioManager.Instance.MusicOn ? "ON" : "OFF");
            pressed = true;
        }
        else if (m_name == "sound")
        {
            AudioManager.Instance.SetSoundState(!AudioManager.Instance.SoundOn);
            m_title.text = "SFX:" + (AudioManager.Instance.SoundOn ? "ON" : "OFF");
            if (AudioManager.Instance.SoundOn) pressed = true;
        }
        else if (m_name == "help")
        {
            GameManager.Instance.ShowTutorial();
            pressed = true;
        }
        else if (m_name == "credit")
        {
            GameManager.Instance.ShowCredit();
            pressed = true;
        }
        else if (m_name == "exit")
        {
            // Save
            //SaveManager.Instance.SaveGame();

            Application.Quit();
        }

        if (pressed)
            AudioManager.Instance.Play("ui_select");
    }

    private void OnApplicationQuit()
    {
        if (m_name == "music")
        {
            PlayerPrefs.SetInt("Music", AudioManager.Instance.MusicOn ? 1 : 0);
        }
        else if (m_name == "sound")
        {
            PlayerPrefs.SetInt("Sound", AudioManager.Instance.SoundOn ? 1 : 0);
        }
    }
}
