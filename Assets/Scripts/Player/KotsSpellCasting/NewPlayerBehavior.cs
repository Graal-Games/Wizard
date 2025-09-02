using Unity.Netcode;
using UnityEngine;


public class NewPlayerBehavior : NetworkBehaviour
{
    // This script now acts as a central hub for finding other components
    // on the player. You can add references here if needed.
    [field: SerializeField] public PlayerHealth Health { get; private set; }
    [field: SerializeField] public PlayerController Controller { get; private set; }
    [field: SerializeField] public PlayerStatusEffects StatusEffects { get; private set; }
    [field: SerializeField] public K_SpellLauncher SpellLauncher { get; private set; }

    [SerializeField] private CustomPassActivationWithGameObject customPassController;

    [ClientRpc]
    public void ShowDamageEffectClientRpc(string shaderKey, float duration, ClientRpcParams clientRpcParams = default)
    {
        // This RPC will be received only by the client who was hit.
        Debug.Log($"CLIENT ({OwnerClientId}): Received request to show damage effect '{shaderKey}'.");

        // Now, tell the local visual effects script to activate the shader.
        if (customPassController != null)
        {
            customPassController.HandleShaderActivation(shaderKey, duration);
        }
        else
        {
            Debug.LogWarning("CustomPassController reference is not set on NewPlayerBehavior!");
        }
    }
}
