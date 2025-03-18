using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AoeCast
{
    public class AoeCastVisualization : MonoBehaviour
    {
        bool isAoeRaycastActive = false;

        // Update is called once per frame
        void Update()
        {
            if (isAoeRaycastActive)
            {
                float vertical = Input.GetAxis("Vertical");

                Vector3 newPosition = transform.position + transform.forward * vertical;

                // This defines what layers are NOT ignored by the raycast
                int includedLayer = LayerMask.GetMask("FloorWallsAndObstacles");

                int layerMask = ~includedLayer;

                RaycastHit hit;
                Ray ray = new Ray(newPosition, transform.forward);

                Debug.DrawRay(newPosition, transform.forward * 1000f, Color.green);

                //// This 
                //if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~layerMask))
                //{ //&&  ( hit.transform.name.Contains("Floor") || hit.transform.name.Contains("Pillar") )
                //    if (hit.transform.name != null)
                //    {
                //        //Debug.Log("Floor hit");
                //        //spawnLocationIsValid = true;

                //        // ?? Does this function need to also be inside update?
                //        //AoePlacement();
                //        //aoeSpawnPosition = new Vector3(hit.point.x, -1f, hit.point.z);
                //    }

                //}

                // Updates the Aoe placement gameobject (that shows where the actual Aoe object will be spawned) 's position
                // >> aoeSpawnPosition = new Vector3 (hit.point.x, -1f, hit.point.z);

                // This makes sure the AoE placement model is pointing in the right direction
                {
                    float yRotation = transform.rotation.eulerAngles.y;
                    Quaternion newRotation = Quaternion.Euler(0, yRotation, 0);

                    //aoeSpawnRotation = newRotation;
                }

                //Debug.LogFormat($"<color=red>{aoeSpawnRotation}</color>");
            }
        }

        public void ActivateAoePlacementVisualizer()
        {
            isAoeRaycastActive = true;
        }
    }
}
