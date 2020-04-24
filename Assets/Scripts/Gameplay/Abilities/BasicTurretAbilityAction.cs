using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BasicTurretAbilityAction : AbilityActionBase
{
    [SerializeField] private float m_effectTime = 7f;       // How long the disable effect lasts for

    // Begin AbilityActionBase Interface
    protected override void startAbilityActionImpl()
    {
        TowerBase tower = m_board.getTowerOnTile(m_tileIndex);
        if (tower)
            StartCoroutine(disableTowerRoutine(tower));
        else
            finishAbilityAction();
    }

    private IEnumerator disableTowerRoutine(TowerBase tower)
    {
        TowerScript script = tower.GetComponent<TowerScript>();
        if (!script)
        {
            finishAbilityAction();
            yield break;
        }

        setTowerDisabled(script, true);
        yield return new WaitForSeconds(m_effectTime);

        // Tower might have been possibly destroyed
        if (script)
            setTowerDisabled(script, false);

        finishAbilityAction();
    }

    private void setTowerDisabled(TowerScript script, bool disable)
    {
        // Was a Derp when making this, this needs to be called on other clients side since they own it
        // Since this function was only called here, we cheat by just having it set in setDisableRotation
        //script.setActionsDelayed(disable);

        if (PhotonNetwork.IsConnected)
            script.photonView.RPC("setDisableRotation", RpcTarget.All, disable);
        else
            script.setDisableRotation(disable);
    }
}
