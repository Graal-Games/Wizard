//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using Unity.Netcode;

//public class HealthBar2 : MonoBehaviour
//{
//    //[SerializeField] private Image _HealthBarForeground;
//    [SerializeField] Slider _healthSlider;

//    //public NetworkVariable<float> health = new NetworkVariable<float>(0f);

//    // public void UpdateHealthBar(float current)
//    // {
//    //     Debug.Log(current);
//    //     _HealthBarForeground.fillAmount = current;
//    // }
//    private void Start()
//    {
//        //_healthSlider = this.GetComponent<Slider>();
//    }

//    //public void SetMaxHealth(float maxHealth)
//    //{
//    //    Debug.Log("Max health is set at " + maxHealth);
//    //    _healthSlider.maxValue = maxHealth;
//    //    _healthSlider.value = maxHealth;
//    //}

//    ////[ServerRpc(RequireOwnership = false)]
//    //public void SetHealth(float health)
//    //{
//    //    _healthSlider.value = health;
//    //}

//    // public void UpdateHealthBar(float maxHealth, float currentHealth)
//    // {
//    //     _HealthBarForeground.fillAmount = currentHealth / maxHealth;
//    // }


//}
