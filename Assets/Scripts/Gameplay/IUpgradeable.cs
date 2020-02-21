using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUpgradeable
{
    bool canUpgrade();
    int getUpgradeCost();
    void upgrade();
}
