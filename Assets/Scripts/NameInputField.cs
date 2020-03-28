using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class NameInputField : PlayerPrefInputField
{
    // PlayerPrefInputField Interface
    protected override void onKeyValueSet(string value)
    {
        PhotonNetwork.NickName = value;
    }
}
