using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterForceController : MonoBehaviour
{
    public MassSpringSystem WaterGrid;
    public WaterForce[] DownwardsRaycastOrigins;

    public float DefaultDistanceFromWater = 1.0f;
    [Range(0.0f, 1.0f)] public float SimulatedPressure = 1.0f;
    /** The mass beyond which maximum pressure will be applied */
    [Range(0.0f, 10.0f)] public float MaxMass = 10.0f;

    private Rigidbody Body;

    private RaycastHit raycastResult;

	// Use this for initialization
	void Start ()
    {
		Body = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void FixedUpdate ()
    {
		UpdateDownwardsForces();
	}

    void UpdateDownwardsForces()
    {
        if(WaterGrid != null)
        { 
            foreach(var Origin in DownwardsRaycastOrigins)
            { 
                Ray DownwardsRay = new Ray(Origin.transform.position, new Vector3(0.0f, -1.0f, 0.0f));
                var waterBitMask = 1 << 4;
                if (Physics.Raycast(DownwardsRay, out raycastResult, DefaultDistanceFromWater, waterBitMask))
                {
                    GameObject obj = raycastResult.collider.gameObject;
                    if (MassSpringSystem.IsMassUnit (obj.tag))
                    {
                        Vector3 p = obj.transform.position - WaterGrid.GetUnitOffset();
                        float pressure = SimulatedPressure * Origin.Force;
                        if (Body != null)
                            pressure *= Mathf.Clamp(Body.mass / MaxMass, 0.0f, 1.0f);
                        //need to translate back from unity world space so we use z here rather than y
                        WaterGrid.GridTouches.Add (new Vector3 (p.x, p.z, pressure));
                    }
                }
            }
        }
    }
}
