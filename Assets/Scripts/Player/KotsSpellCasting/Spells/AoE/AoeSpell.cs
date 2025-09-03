using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class AoeSpell : K_Spell
{
    //public float damage = 10f;
    //public float movementSlowAmount = 2.5f;
    //public float slowTime = 4;
    //public bool hasHitShield;
    float lifeTime = 500f; // This is to become a SO property

    //[SerializeField]
    //private K_SpellData spellDataScriptableObject;

    PlayerHitPayload playerHitPayload;

    Renderer rendererComponent;

    float halfTransparency = 0f;
    float noTransparency = 1;

    // The amount of alpha to apply (range 0 = fully transparent, 1 = fully opaque)
    //public float alphaValue = 0.5f;

    // Gott make it so that the aoe can only affect the player once
    //public bool hasCollided;
    void Awake()
    {
        //this.gameObject.GetComponent<Rigidbody>().useGravity = false;
        //this.gameObject.GetComponent<Rigidbody>().isKinematic = true;



        rendererComponent = GetComponent<Renderer>();
    }

    private void Start()
    {

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        StartCoroutine(TimeUntilDestroyed());
        StartCoroutine(EffectsActivation());
        //StartCoroutine(ActivateEffect());
        this.gameObject.transform.position = new Vector3(this.gameObject.transform.position.x, this.gameObject.transform.position.y + 0.0001f, this.gameObject.transform.position.z); // This still needed?
    }


    // To turn this into a timer
    IEnumerator TimeUntilDestroyed()
    {
        yield return new WaitForSeconds(lifeTime);
        DestroyAoeServerRpc();  
    }


    IEnumerator SetTransparencyDelayed()
    {
        //SetTransparency(0.5f);
        yield return new WaitForSeconds(0.5f);
        //SetTransparency(1);
    }

    //void SetTransparency(float alphaValue)
    //{
    //    // Check if the GameObject has a material
    //    if (rendererComponent != null && rendererComponent.material != null)
    //    {
    //        // Get the current color of the material
    //        Color currentColor = rendererComponent.material.color;

    //        // Set the new alpha value
    //        currentColor.a = alphaValue;

    //        // Apply the updated color back to the material
    //        rendererComponent.material.color = currentColor;

    //        // Ensure the material's shader supports transparency
    //        //SetMaterialToTransparent(rendererComponent.material);
    //    }
    //}

    void ChangeColor(float opacity)
    {
        Color fullOpacity = new Color(1.0f, 0.0f, 1.0f);
        fullOpacity.a = opacity; // Set the alpha channel to 1.0 (fully opaque)

        if (this.gameObject.transform.GetComponent<Renderer>())
        {
            this.gameObject.transform.GetComponent<Renderer>().material.color = fullOpacity;

        } else if (this.gameObject.transform.GetComponentInParent<Renderer>())
        {
            this.gameObject.transform.GetComponentInParent<Renderer>().material.color = fullOpacity;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyAoeServerRpc()
    {
        NetworkObject.Despawn(this.gameObject);
        return;
    }



    // Element router for color, vfx and effects logic
    IEnumerator EffectsActivation()
    {
        string SpellBehavior = SpellDataScriptableObject.element.ToString();

        switch (SpellBehavior)
        {
            case "Arcane":
                yield return StartCoroutine(ActivationDelay(0.5f));
                break;
            case "Water":
                yield return StartCoroutine(ActivationDelay(0.5f));
                break;
            case "Earth":
                Debug.LogFormat($"<color=orange> EffectsActivation </color>");
                yield return StartCoroutine(ActivationDelay(0.5f));
                yield return StartCoroutine(DurationActive(0.5f));
                break;
            case "Fire":
                yield return StartCoroutine(ActivationDelay(0.5f));
                break;
            case "Air":
                yield return StartCoroutine(ActivationDelay(2f));
                break;
        }
    }



    // Turn these into timers
    // How much time before the spell is activated
    IEnumerator ActivationDelay(float duration)
    {
        Debug.LogFormat($"<color=orange> ActivationDelay 1 </color>");
        //SetTransparency(halfTransparent);
        //ChangeColor(halfTransparency);
        yield return new WaitForSeconds(duration);
        //ChangeColor(noTransparency);
        //SetTransparency(noTransparency);
        this.gameObject.GetComponent<Collider>().enabled = true;
        Debug.LogFormat($"<color=orange> ActivationDelay 2 <color>");
    }



    // For how long the spell will be active
    IEnumerator DurationActive(float duration)
    {
        Debug.LogFormat($"<color=orange> DurationActive 1 </color>");
        yield return new WaitForSeconds(duration);
        this.gameObject.GetComponent<Collider>().enabled = false;
        Debug.LogFormat($"<color=orange> DurationActive 2 </color>");
    }



    public override void Fire()
    {
        // To change this?
    }




    public override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
    }

}
