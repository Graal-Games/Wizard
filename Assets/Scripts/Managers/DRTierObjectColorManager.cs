using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DRTierObjectColorManager : MonoBehaviour
{
    [Header("DR Objects")]
    [SerializeField] GameObject charmDRObject;
    [SerializeField] GameObject sphereDRObject;
    [SerializeField] GameObject barrierDRObject;
    [SerializeField] GameObject aoeDRObject;
    [SerializeField] GameObject invokeDRObject;
    [SerializeField] GameObject boltDRObject;
    [SerializeField] GameObject beamDRObject;
    [SerializeField] GameObject summonDRObject;

    [Header("DR Object Colors")]
    [SerializeField] Material DRTier1Color;
    [SerializeField] Material DRTier2Color;
    [SerializeField] Material DRTier3Color;
    [SerializeField] Material DRTier4Color;
    [SerializeField] Material DRTier5Color;
    [SerializeField] Material DRTier6Color;
    [SerializeField] Material DRTier7Color;

    [Header("DR Objects Renderer Components")]
    Renderer charmDRObjectRenderer;
    Renderer sphereDRObjectRenderer;
    Renderer barrierDRObjectRenderer;
    Renderer aoeDRObjectRenderer;
    Renderer invokeDRObjectRenderer;
    Renderer boltDRObjectRenderer;
    Renderer beamDRObjectRenderer;
    Renderer summonDRObjectRenderer;



    private void Start()
    {
        charmDRObjectRenderer = charmDRObject.GetComponent<Renderer>();
        sphereDRObjectRenderer = sphereDRObject.GetComponent<Renderer>();
        barrierDRObjectRenderer = barrierDRObject.GetComponent<Renderer>();
        aoeDRObjectRenderer = aoeDRObject.GetComponent<Renderer>();
        invokeDRObjectRenderer = invokeDRObject.GetComponent<Renderer>();
        boltDRObjectRenderer = boltDRObject.GetComponent<Renderer>();
        beamDRObjectRenderer = beamDRObject.GetComponent<Renderer>();
        summonDRObjectRenderer = summonDRObject.GetComponent<Renderer>();
    }


    // For more advanced bolt spells
    // Regular bolt spell, those that are transmuted too, don't require DR
    public void SetBoltDRObjectTierColor(string tier)
    {
        beamDRObject.transform.parent.parent.gameObject.SetActive(true);

        switch (tier)
        {
            case "tier1":

                Debug.LogFormat($"<color=orange>Beam DR Object Color Changer Accessed</color>");

                // Change DR Object color here
                if (beamDRObjectRenderer != null)
                {
                    beamDRObjectRenderer.material = DRTier1Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier2":

                // Change DR Object color here
                if (beamDRObjectRenderer != null)
                {
                    beamDRObjectRenderer.material = DRTier2Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier3":

                // Change DR Object color here
                if (beamDRObjectRenderer != null)
                {
                    beamDRObjectRenderer.material = DRTier3Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier4":

                // Change DR Object color here
                if (beamDRObjectRenderer != null)
                {
                    beamDRObjectRenderer.material = DRTier4Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier5":

                // Change DR Object color here
                if (beamDRObjectRenderer != null)
                {
                    beamDRObjectRenderer.material = DRTier5Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier6":

                // Change DR Object color here
                if (beamDRObjectRenderer != null)
                {
                    beamDRObjectRenderer.material = DRTier6Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier7":

                // Change DR Object color here
                if (beamDRObjectRenderer != null)
                {
                    beamDRObjectRenderer.material = DRTier7Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

        }
    }




    // Either a switch case or separate methods

    // case beam > beam Renderer
    // case tier > dr tier color

    public void SetBeamDRObjectTierColor(string tier)
    {
        beamDRObject.transform.parent.parent.gameObject.SetActive(true);

        switch (tier)
        {
            case "tier1":

                Debug.LogFormat($"<color=orange>Beam DR Object Color Changer Accessed</color>");

                // Change DR Object color here
                if (beamDRObjectRenderer != null)
                {
                    beamDRObjectRenderer.material = DRTier1Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier2":

                // Change DR Object color here
                if (beamDRObjectRenderer != null)
                {
                    beamDRObjectRenderer.material = DRTier2Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier3":

                // Change DR Object color here
                if (beamDRObjectRenderer != null)
                {
                    beamDRObjectRenderer.material = DRTier3Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier4":

                // Change DR Object color here
                if (beamDRObjectRenderer != null)
                {
                    beamDRObjectRenderer.material = DRTier4Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier5":

                // Change DR Object color here
                if (beamDRObjectRenderer != null)
                {
                    beamDRObjectRenderer.material = DRTier5Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier6":

                // Change DR Object color here
                if (beamDRObjectRenderer != null)
                {
                    beamDRObjectRenderer.material = DRTier6Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier7":

                // Change DR Object color here
                if (beamDRObjectRenderer != null)
                {
                    beamDRObjectRenderer.material = DRTier7Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

        }
    }



    public void SetAoeDRObjectTierColor(string tier)
    {
        switch (tier)
        {
            case "tier1":

                aoeDRObject.transform.parent.parent.gameObject.SetActive(true);

                // Change DR Object color here
                if (aoeDRObjectRenderer != null)
                {
                    aoeDRObjectRenderer.material = DRTier1Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier2":

                // Change DR Object color here
                if (aoeDRObjectRenderer != null)
                {
                    aoeDRObjectRenderer.material = DRTier2Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier3":

                // Change DR Object color here
                if (aoeDRObjectRenderer != null)
                {
                    aoeDRObjectRenderer.material = DRTier3Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier4":

                // Change DR Object color here
                if (aoeDRObjectRenderer != null)
                {
                    aoeDRObjectRenderer.material = DRTier4Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier5":

                // Change DR Object color here
                if (aoeDRObjectRenderer != null)
                {
                    aoeDRObjectRenderer.material = DRTier5Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier6":

                // Change DR Object color here
                if (aoeDRObjectRenderer != null)
                {
                    aoeDRObjectRenderer.material = DRTier6Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier7":

                // Change DR Object color here
                if (aoeDRObjectRenderer != null)
                {
                    aoeDRObjectRenderer.material = DRTier7Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

        }
    }


    // TO COMPLETE
    public void SetCharmDRObjectTierColor(string tier)
    {
        switch (tier)
        {
            case "tier1":

                charmDRObject.transform.parent.parent.gameObject.SetActive(true);

                // Change DR Object color here
                if (charmDRObjectRenderer != null)
                {
                    charmDRObjectRenderer.material = DRTier1Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier2":

                // Change DR Object color here
                if (charmDRObjectRenderer != null)
                {
                    charmDRObjectRenderer.material = DRTier2Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier3":

                // Change DR Object color here
                if (charmDRObjectRenderer != null)
                {
                    charmDRObjectRenderer.material = DRTier3Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier4":

                // Change DR Object color here
                if (charmDRObjectRenderer != null)
                {
                    charmDRObjectRenderer.material = DRTier4Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier5":

                // Change DR Object color here
                if (charmDRObjectRenderer != null)
                {
                    charmDRObjectRenderer.material = DRTier5Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier6":

                // Change DR Object color here
                if (charmDRObjectRenderer != null)
                {
                    charmDRObjectRenderer.material = DRTier6Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier7":

                // Change DR Object color here
                if (charmDRObjectRenderer != null)
                {
                    charmDRObjectRenderer.material = DRTier7Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

        }
    }



    public void SetSphereDRObjectTierColor(string tier)
    {
        switch (tier)
        {
            case "tier1":

                sphereDRObject.transform.parent.parent.gameObject.SetActive(true);

                // Change DR Object color here
                if (sphereDRObjectRenderer != null)
                {
                    sphereDRObjectRenderer.material = DRTier1Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier2":

                // Change DR Object color here
                if (sphereDRObjectRenderer != null)
                {
                    sphereDRObjectRenderer.material = DRTier2Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier3":

                // Change DR Object color here
                if (sphereDRObjectRenderer != null)
                {
                    sphereDRObjectRenderer.material = DRTier3Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier4":

                // Change DR Object color here
                if (sphereDRObjectRenderer != null)
                {
                    sphereDRObjectRenderer.material = DRTier4Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier5":

                // Change DR Object color here
                if (sphereDRObjectRenderer != null)
                {
                    sphereDRObjectRenderer.material = DRTier5Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier6":

                // Change DR Object color here
                if (sphereDRObjectRenderer != null)
                {
                    sphereDRObjectRenderer.material = DRTier6Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier7":

                // Change DR Object color here
                if (sphereDRObjectRenderer != null)
                {
                    sphereDRObjectRenderer.material = DRTier7Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

        }
    }



    public void SetBarrierDRObjectTierColor(string tier)
    {
        switch (tier)
        {
            case "tier1":

                barrierDRObject.transform.parent.parent.gameObject.SetActive(true);

                // Change DR Object color here
                if (barrierDRObjectRenderer != null)
                {
                    barrierDRObjectRenderer.material = DRTier1Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier2":

                // Change DR Object color here
                if (barrierDRObjectRenderer != null)
                {
                    barrierDRObjectRenderer.material = DRTier2Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier3":

                // Change DR Object color here
                if (barrierDRObjectRenderer != null)
                {
                    barrierDRObjectRenderer.material = DRTier3Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier4":

                // Change DR Object color here
                if (barrierDRObjectRenderer != null)
                {
                    barrierDRObjectRenderer.material = DRTier4Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier5":

                // Change DR Object color here
                if (barrierDRObjectRenderer != null)
                {
                    barrierDRObjectRenderer.material = DRTier5Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier6":

                // Change DR Object color here
                if (barrierDRObjectRenderer != null)
                {
                    barrierDRObjectRenderer.material = DRTier6Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

            case "tier7":

                // Change DR Object color here
                if (barrierDRObjectRenderer != null)
                {
                    barrierDRObjectRenderer.material = DRTier7Color;
                }
                else
                {
                    Debug.LogError("The targetObject does not have a Renderer component.");
                }

                return;

        }
    }
}
