using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProjectileClass : SpellsClass
{
    [SerializeField] private ProjectileParryHandler projectileParryHandler;
    private Vector3 pushDirection;

    [SerializeField] private TriggerListener projectileTrigger;

    // --- SERVER-ONLY VARIABLES ---
    private Vector3 lastPosition;
    private List<Rigidbody> pushSpellsList = new List<Rigidbody>();
    private bool hasCollided = false; // Simple bool to prevent multi-hits

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn(); // This already gets the Rigidbody for us
        lastPosition = transform.position;
        pushDirection = transform.forward;

        if (projectileTrigger != null)
        {
            projectileTrigger.OnEnteredTrigger += ProjectileTrigger_OnEnteredTrigger;
        }

        // The parry logic can stay, but the event handler must be server-only
        if (IsParriable() && projectileParryHandler != null)
        {
            projectileParryHandler.OnAnyPlayerPerformedParry += ProjectileParryHandler_OnAnyPlayerPerformedParry;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (projectileTrigger != null)
        {
            projectileTrigger.OnEnteredTrigger -= ProjectileTrigger_OnEnteredTrigger;
        }
        if (IsParriable() && projectileParryHandler != null)
        {
            projectileParryHandler.OnAnyPlayerPerformedParry -= ProjectileParryHandler_OnAnyPlayerPerformedParry;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // use ProjectileTrigger_OnEnteredTrigger inestead
    }

    private void ProjectileTrigger_OnEnteredTrigger(Collider collider)
    {
        base.OnTriggerEnter(collider);

        Debug.Log("ProjectileTrigger_OnEnteredTrigger (" + collider.gameObject.name + ")");
    }

    private void ProjectileParryHandler_OnAnyPlayerPerformedParry(object sender, System.EventArgs e)
    {
        if (!IsServer) return;
        DestroySpell(gameObject);
    }

    // The base FixedUpdate handles the lifetime timer.
    // The child class FixedUpdate handles projectile-specific logic.
    public override void FixedUpdate()
    {
        base.FixedUpdate();

        // All projectile simulation logic MUST be on the server.
        if (IsServer)
        {
            HandleMovement();
            if (!IsSpawned) return; // Check for in-frame despawn
            HandlePushback();
        }
    }

    private void HandleMovement()
    {
        if (hasCollided) return; // Stop moving after we've hit something.

        Vector3 currentPosition = transform.position;
        rb.velocity = transform.forward * SpellDataScriptableObject.moveSpeed;

        // Use SphereCast for fast-moving projectiles
        if (SpellDataScriptableObject.moveSpeed >= 39)
        {
            float radius = 0.5f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            Vector3 direction = currentPosition - lastPosition;
            float distance = direction.magnitude;

            if (distance > 0 && Physics.SphereCast(lastPosition, radius, direction.normalized, out RaycastHit hit, distance))
            {
                // We hit something, so call the base collision handler.
                HandleCollision(hit.collider);
            }
        }
        lastPosition = currentPosition;
    }

    // OnTriggerEnter handles slow-moving projectiles.
    // The base class already has a server-guarded OnTriggerEnter that calls this.
    protected override void HandleCollision(Collider other)
    {
        if (hasCollided) return; // Prevent hitting multiple targets.

        // Pass the collision to the base class to handle damage, self-hit checks, etc.
        base.HandleCollision(other);

        // After the collision is handled, set the flag to prevent more collisions.
        hasCollided = true;
    }

    private void HandlePushback()
    {
        // Your simplified, server-only pushback logic is good.
        // It should be called from FixedUpdate on the server.
    }

    public void ApplyPushbackToTarget(GameObject other)
    {
        // This method should be called from HandleCollision, which is server-only.
        if (other.CompareTag("Player") && SpellDataScriptableObject.pushForce > 0)
        {
            Rigidbody targetRb = other.GetComponent<Rigidbody>();
            if (targetRb != null && !pushSpellsList.Contains(targetRb))
            {
                pushSpellsList.Add(targetRb);
            }
        }
    }

    // This method handles the logic for the large, invisible parry zone
    private void HandleParryTrigger(Collider other)
    {
        if (!IsServer) return;
        Debug.Log($"'{other.name}' entered the parry zone.");
        // ... logic for parry prompt UI, etc. ...
    }
}