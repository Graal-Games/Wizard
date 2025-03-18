using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FuntionTimer
{

    private static List<FuntionTimer> activeTimerList;
    private static GameObject initGameObject;

    private static void InitIfNeeded() {
        if(initGameObject == null)
        {
            initGameObject = new GameObject("FunctionTimer_InitGameObject");
            activeTimerList = new List<FuntionTimer>();
        }
    }

    public static FuntionTimer Create(Action action, float timer, string timerName = null) {
        
        InitIfNeeded();

        GameObject gameObject = new GameObject("FunctionTimer", typeof(MonoBehaviorHook));
        FuntionTimer funtionTimer = new FuntionTimer(action, timer, timerName, gameObject);
        gameObject.GetComponent<MonoBehaviorHook>().onUpdate = funtionTimer.Update;

        activeTimerList.Add(funtionTimer);

        return funtionTimer;
    }

    private static void RemoveTimer(FuntionTimer funtionTimer) {
        InitIfNeeded();
        activeTimerList.Remove(funtionTimer);
    }

    private static void StopTimer(string timerName)
    {
        InitIfNeeded();

        for (int i = 0; i < activeTimerList.Count; i++)
        {
            if (activeTimerList[i].timerName == timerName) {
                // Stop this timer
                activeTimerList[i].DestroySelf();
            }
        }


    }

    // Dummy class to have access to MonoBehaviour funtions
    public class MonoBehaviorHook : MonoBehaviour
    {
        public Action onUpdate;

        private void Update()
        {
            if (onUpdate != null) onUpdate();
        }
    }

    private Action action;
    private float timer;
    private bool isDestroyed;
    private string timerName;
    private GameObject gameObject;

    public FuntionTimer(Action action, float timer, string timerName, GameObject gameObject)
    {
        this.action = action;
        this.timer = timer;
        isDestroyed = false;
        this.timerName = timerName;
        this.gameObject = gameObject;
    }

    public void Update() {
        if (!isDestroyed)
        {
            timer -= Time.deltaTime;

            if (timer < 0)
            {
                // Trigger action
                action();
                DestroySelf();
            }
        }
    }

    private void DestroySelf() { 
        isDestroyed = true;
        UnityEngine.Object.Destroy(gameObject);
        RemoveTimer(this);
    }
}
