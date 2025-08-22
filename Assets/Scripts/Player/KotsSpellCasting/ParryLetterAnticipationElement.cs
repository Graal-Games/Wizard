using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ParryLetterAnticipationElement : NetworkBehaviour
{
    [SerializeField] GameObject parryAnticipationCanvas;
    [SerializeField] TextMeshProUGUI parryText;

    public override void OnNetworkSpawn()
    {
        parryAnticipationCanvas.SetActive(false);
    }

    public void showParryLetter(string parryLetter)
    {
        parryText.SetText(parryLetter.ToString());
        parryAnticipationCanvas.SetActive(true);
    }

    public void hideParryLetter()
    {
        parryAnticipationCanvas.SetActive(false);
    }
}
