using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class AoeDispel : AoeSpell
{

    [SerializeField] private LayerMask spellLayer;
    private Collider col;
    private float sphereRadius;

    // # VIBE'D
    void Awake()
    {
        col = GetComponent<Collider>();

        // For a SphereCollider, use its radius & scale
        if (col is SphereCollider sphereCol)
        {
            sphereRadius = sphereCol.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        }
        else
        {
            // For other colliders, approximate using bounds
            sphereRadius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y, col.bounds.extents.z);
        }
    }

    // # VIBE'D
    // Actively checks for spells that are overlapping with it and destroys them
    // This was a solution because the OnTriggerEnter and Stay only detect gameObjects
    public override void FixedUpdate()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, sphereRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Spell") || hit.CompareTag("ActiveShield"))
            {
                Dispel(hit);
            }
        }
    }

    public override void OnTriggerEnter(Collider collider)
    {
        base.OnTriggerEnter(collider);

        Dispel(collider);
    }

    void OnTriggerStay(Collider collider)
    {
        Dispel(collider);
    }

    void Dispel(Collider hitCollider)
    {
        var spellScript = hitCollider.GetComponent<SpellsClass>();
        var spellScriptInParent = hitCollider.GetComponentInParent<SpellsClass>();
        var spellScriptInChild = hitCollider.GetComponentInChildren<SpellsClass>();

        if (spellScript != null)
        {
            spellScript.DestroySpell(hitCollider.gameObject);
        }
        else if (spellScriptInParent != null)
        {
            spellScriptInParent.DestroySpell(hitCollider.gameObject);

        }
        else if (spellScriptInChild != null)
        {
            spellScriptInChild.DestroySpell(hitCollider.gameObject);

        }
        else if (hitCollider.gameObject.GetComponent<K_Spell>() != null)
        {
            hitCollider.gameObject.GetComponent<K_Spell>().DestroySpell(hitCollider.gameObject);

        }
        else if (hitCollider.gameObject.GetComponentInChildren<K_Spell>() != null)
        {
            hitCollider.gameObject.GetComponent<K_Spell>().DestroySpell(hitCollider.gameObject);
        }
        else if (hitCollider.gameObject.GetComponentInParent<K_Spell>() != null)
        {
            hitCollider.gameObject.GetComponentInParent<K_Spell>().DestroySpell(hitCollider.gameObject);
        }
    }
}
