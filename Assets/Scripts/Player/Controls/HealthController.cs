using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class HealthController : NetworkBehaviour
{

    HealthBar _healthBar;

    void Awake()
    {
        //_healthBar = StatsUi.Instance.GetComponent<StatsUi>().GetComponentInChildren<HealthBar>();
    }
}
