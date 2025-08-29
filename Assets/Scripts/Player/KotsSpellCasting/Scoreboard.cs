using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Scoreboard : NetworkBehaviour
{
    [SerializeField] private TMP_Text p1ScoreText;
    [SerializeField] private TMP_Text p2ScoreText;

    private NetworkVariable<int> p1Score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> p2Score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        p1Score.OnValueChanged += OnP1ScoreChanged;
        p2Score.OnValueChanged += OnP2ScoreChanged;

        p1ScoreText.text = p1Score.Value.ToString();
        p2ScoreText.text = p2Score.Value.ToString();
    }

    private void OnP1ScoreChanged(int previousValue, int newValue)
    {
        p1ScoreText.text = newValue.ToString();
    }

    private void OnP2ScoreChanged(int previousValue, int newValue)
    {
        p2ScoreText.text = newValue.ToString();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerScoredServerRpc(ulong scoringPlayerId)
    {
        // For a 1v1, if one player scores, the other must have died.
        // We find the player who ISN'T the scoring player and increment their score.
        // NOTE: This assumes only two players with ClientIds 0 and 1.
        if (scoringPlayerId == 0)
        {
            // If player 0 scored, player 1's score goes up.
            p1Score.Value++;
        }
        else if (scoringPlayerId == 1)
        {
            // If player 1 scored, player 2's score goes up.
            p2Score.Value++;
        }
    }
}