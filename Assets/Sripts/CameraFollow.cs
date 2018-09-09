using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform Player;
    public float CameraLerpSpeed = 2.0f;
    public float PlayerZDistance = 0.0f;
	// Use this for initialization
	void Start ()
    {
        if (Player != null)
		    PlayerZDistance = transform.position.z - Player.position.z;
	}
	
	// Update is called once per frame
	void FixedUpdate ()
    {
		var currentPos = transform.position;
        var newZ = Mathf.Lerp(currentPos.z, Player.position.z + PlayerZDistance, Time.deltaTime * CameraLerpSpeed);
        transform.position = new Vector3(currentPos.x, currentPos.y, newZ);
	}
}
