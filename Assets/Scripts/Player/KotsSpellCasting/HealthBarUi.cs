using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
//using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUi : NetworkBehaviour
{
    //[SerializeField] private Image _HealthBarForeground;
    [SerializeField] Slider _healthSlider;
    [SerializeField] TMP_Text text;

    //public NetworkVariable<float> health = new NetworkVariable<float>(0f);

    // public void UpdateHealthBar(float current)
    // {
    //     Debug.Log(current);
    //     _HealthBarForeground.fillAmount = current;
    // }

    public Slider HealthSlider
    {
        get { return _healthSlider; }
        set { _healthSlider = value; }
    }
    //void OnEnable()
    //{
    //    ProjectileSpell.playerHitEvent2 += ApplyDamage;
    //}
    private void Start()
    {
        _healthSlider = this.GetComponent<Slider>();
        text.text = _healthSlider.value.ToString();
        Debug.Log("_healthSlider " + _healthSlider);
    }

    public void SetMaxHealth(float maxHealth)
    {
        Debug.Log("Max health is set at " + maxHealth);
        _healthSlider.maxValue = maxHealth;
        _healthSlider.value = maxHealth;
        text.text = _healthSlider.value.ToString();
    }

    //[ServerRpc(RequireOwnership = false)]
    public void SetHealth(float health)
    {
        _healthSlider.value = health;
    }

    public void ApplyDamage(float damage)
    {
        Debug.LogFormat($"<color=green>Damage dealt: {damage}</color>");
        _healthSlider.value -= damage;
        text.text = _healthSlider.value.ToString();
        Debug.LogFormat($"<color=green>health is now at {_healthSlider.value}</color>");
    }

    public void Heal(float healAmount)
    {
        Debug.LogFormat($"<color=orange> > 3 HEALING method - amount: {healAmount} </color>");

        _healthSlider.value += healAmount;
        text.text = _healthSlider.value.ToString();
    }

    // public void UpdateHealthBar(float maxHealth, float currentHealth)
    // {
    //     _HealthBarForeground.fillAmount = currentHealth / maxHealth;
    // }


}
