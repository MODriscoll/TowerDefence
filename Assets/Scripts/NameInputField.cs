using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

[RequireComponent(typeof(InputField))]
public class NameInputField : MonoBehaviour
{
    private InputField m_inputField;    // Field for inputting name

    void Start()
    {
        string name = string.Empty;

        m_inputField = GetComponent<InputField>();
        if (m_inputField)
        {
            if (PlayerPrefs.HasKey("PlayerName"))
            {
                name = PlayerPrefs.GetString("PlayerName");
                m_inputField.text = name;
            }
        }

        PhotonNetwork.NickName = name;
    }

    public void setPlayerName(string newName)
    {
        if (string.IsNullOrEmpty(newName))
            return;

        PlayerPrefs.SetString("PlayerName", newName);
        PhotonNetwork.NickName = newName;
    }
}
