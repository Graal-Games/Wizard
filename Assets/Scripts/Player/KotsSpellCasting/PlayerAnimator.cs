using Unity.Netcode;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private NetworkObject networkObject;

    [SerializeField] private PlayerController player;

    // Animator parameter hashes for efficiency
    private readonly int verticalAxisHash = Animator.StringToHash("vertical");
    private readonly int horizontalAxisHash = Animator.StringToHash("horizontal");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        networkObject = GetComponentInParent<NetworkObject>(); // Get the NetworkObject
    }

    private void Update() // Update is better for animations for smoother visuals
    {
        // This script should only run on the client that owns this player object
        if (!networkObject.IsOwner)
        {
            return;
        }

        // Update the local animator directly.
        // The NetworkAnimator will see this change and sync it for us.
        animator.SetFloat(verticalAxisHash, player.getVerticalAxis());
        animator.SetFloat(horizontalAxisHash, player.getHorizontalAxis());

        // Handle jumping here as well if you add it back
        // animator.SetBool("isJumping", player.IsJumping());
    }
}
