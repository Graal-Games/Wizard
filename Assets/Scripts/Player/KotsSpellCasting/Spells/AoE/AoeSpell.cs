using UnityEngine;

public class AoeSpell : SpellsClass, IDamageable
{
    public virtual void OnTriggerEnter(Collider other) {
        HandleAllInteractions(other);
    }
}