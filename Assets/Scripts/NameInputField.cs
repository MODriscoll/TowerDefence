using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class NameInputField : PlayerPrefInputField
{
    public NameInputField()
    {
        m_prefKey = "PlayerName";
    }

    // PlayerPrefInputField Interface
    protected override void onKeyValueSet(string value)
    {
        //PhotonNetwork.NickName = value;
    }
}
