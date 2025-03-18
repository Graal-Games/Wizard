using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HealthBar : NetworkBehaviour
{
    [SerializeField] Slider _healthSlider;
    //[SerializeField] Slider _healthSlider2;
    public NetworkVariable<float> PlayerHealth = new NetworkVariable<float>(90,
        NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);

    private void OnEnable()
    {
        this.GetComponent<Slider>().value = PlayerHealth.Value;

        PlayerHealth.OnValueChanged += SetHealth;

        //K_Spell.playerHitEvent += HandleSpellInflicted;
        //_healthSlider.value = 50;

        //this.GetComponent<NetworkObject>().ChangeOwnership(this.gameObject.GetComponentInParent<NetworkBehaviour>().OwnerClientId);

        //Debug.LogFormat($"<color=blue>This is the owner of the health bar script: {OwnerClientId} </color>");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.LogFormat($"<color=blue>This is the owner of the health bar script: {OwnerClientId} </color>");
    }

    public void SetNewHealth(float newHealth)
    {
        _healthSlider.value = newHealth;
    }


    public void SetMaxHealth(float maxHealth)
    {
        Debug.Log("Max health is set at " + maxHealth);
        //_healthSlider.maxValue = maxHealth;
        //_healthSlider.value = maxHealth;
        this.GetComponent<Slider>().maxValue = maxHealth;
        this.GetComponent<Slider>().value = maxHealth;
    }
    
    public void SetHealth(float previousValue, float newValue)
    {        
        Debug.LogFormat($"<color=green>SetHealth: {this.GetComponent<Slider>().value} : {OwnerClientId}</color>");
        this.GetComponent<Slider>().value = newValue;
        Debug.LogFormat($"<color=green>SetHealth: {this.GetComponent<Slider>().value} : {OwnerClientId}</color>");
    }

    public void ApplyDamage (float damage)
    {   
        Debug.LogFormat($"<color=green>ApplyDamage: {PlayerHealth.Value} : {OwnerClientId}</color>");
        PlayerHealth.Value -= damage;
        //_healthSlider.value = PlayerHealth.Value;
        Debug.LogFormat($"<color=green>ApplyDamage: {PlayerHealth.Value} : {OwnerClientId}</color>");


        Debug.LogFormat($"<color=green>edit health bar ui: {this.GetComponent<Slider>().value} : {OwnerClientId}</color>");
        this.GetComponent<Slider>().value -= damage;
        Debug.LogFormat($"<color=green>edit health bar ui: {this.GetComponent<Slider>().value} : {OwnerClientId}</color>");
    }

    public void TestMethod()
    {
        Debug.Log("HEALTH BAR TEST METHOD");
    }
}
