using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth
{
    // Fields
    float _currentHealth;
    float _currentMaxHealth;

    // Properties
    public float Health
    {
        get
        {
            return _currentHealth;
        }
        set
        {
            _currentHealth = value;
        }
    }

    public float MaxHealth
    {
        get
        {
            return _currentMaxHealth;
        }
        set
        {
            _currentMaxHealth = value;
        }
    }

    // Constructor
    public PlayerHealth(float health, float maxHealth)
    {
        _currentHealth = health;
        _currentMaxHealth = maxHealth;
    }

    // Methods
    public void DamagePlayer(float damageAmount)
    {
        if (_currentHealth > 0)
        {
            _currentHealth -= damageAmount;
        }
    }
}
