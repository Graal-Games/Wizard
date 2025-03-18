using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MistOverlay : MonoBehaviour
{

    RawImage mistOverlay; 

    void Awake()
    {
        mistOverlay = this.GetComponent<RawImage>();
        

    }
    void Start()
    {
        this.gameObject.SetActive(false);
        //Debug.Log(mistOverlay);
    }

    // Update is called once per frame
    void Update()
    {
        // Color currentColor = mistOverlay.color;
        // currentColor.a = 0.5f;
        // currentColor.a = 1f;
    }

    void ActivateMistOverlay()
    {
        this.gameObject.SetActive(true);
    }
}
