using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform Player;
    public float CameraLerpSpeed = 2.0f;
    private float PlayerZDistance = 0.0f;
    private float PlayerXDistance = 0.0f;

    //int fixedUpdatesPerFrame = 0;

	// Use this for initialization
	void Start ()
    {
        if (Player != null)
		    PlayerZDistance = transform.position.z - Player.position.z;
        if (Player != null)
            PlayerXDistance = transform.position.x - Player.position.x;
	}
	
	// Update is called once per frame
	void FixedUpdate ()
    {
        //++fixedUpdatesPerFrame;
		var currentPos = transform.position;
        var newZ = /*Player.position.z + PlayerZDistance;//*/Mathf.Lerp(currentPos.z, Player.position.z + PlayerZDistance, Time.deltaTime * CameraLerpSpeed);
        var newX = Mathf.Lerp(currentPos.x, Player.position.x + PlayerXDistance, Time.deltaTime * CameraLerpSpeed);
        transform.position = new Vector3(newX, currentPos.y, newZ);
	}

    //private void Update ()
    //{
    //    Debug.Log("Fixed Updates Per Frame: " + fixedUpdatesPerFrame);
    //    fixedUpdatesPerFrame = 0;
    //}
}
