using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class PlayerController : MonoBehaviour
{
    public EnvironmentScroller EnvironmentController;
    private float forwardForce = 0.0f;
    public float ForwardForce = 0.0f;
    public float MaxForwardForce = 10.0f;
    public float RotationDelta = 0.0f;
    public float MaxRotationDelta = 10.0f;
    public float MinTimeBetweenStrokes = 0.1f;
    private float TimeSinceLastStroke = 0.0f;
    public PlayableDirector AttackSequenceDirector;
    public PlayableDirector ReleaseSequenceDirector;

    public float ReleaseWeight = 1.0f;
    private float RotationPolarity = 1.0f;

    public GameObject ForwardIndicator = null;

    private Rigidbody Body;

	void Start ()
    {
        Body = GetComponent<Rigidbody>();
        AttackSequenceDirector.stopped += OnDirectorStopped;
        AttackSequenceDirector.played += OnDirectorPlayed;
        ReleaseSequenceDirector.stopped += OnDirectorStopped;
        ReleaseSequenceDirector.played += OnDirectorPlayed;
    }

    private void OnDestroy ()
    {
        AttackSequenceDirector.stopped -= OnDirectorStopped;
        AttackSequenceDirector.played -= OnDirectorPlayed;
        ReleaseSequenceDirector.stopped -= OnDirectorStopped;
        ReleaseSequenceDirector.played -= OnDirectorPlayed;     
    }

    void FixedUpdate ()
    {
        TimeSinceLastStroke += Time.deltaTime;
        if (ForwardIndicator != null)
        {
            ForwardIndicator.transform.position = transform.position + transform.forward * 4;
        }
		if (Body != null)
        {
            forwardForce = Mathf.Clamp(forwardForce + ForwardForce * MaxForwardForce, 0.0f, MaxForwardForce) * ReleaseWeight;
            Body.AddForce(transform.forward * forwardForce);
        }
        float rotDelta = RotationDelta * RotationPolarity * MaxRotationDelta * ReleaseWeight;
        Vector3 euler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(euler.x, euler.y + rotDelta, euler.z);
	}

    private void OnCollisionEnter (Collision collision)
    {
        if (collision.gameObject.GetComponent<IslandObstacleController>() != null && EnvironmentController != null)
        {
            ResetPlayer(); 
            EnvironmentController.ResetGame();
        }
    }

    void ResetPlayer()
    {
        AttackSequenceDirector.Stop();
        ReleaseSequenceDirector.Stop();
        forwardForce = 0.0f;
        ForwardForce = 0.0f;
        RotationDelta = 0.0f;
        ReleaseWeight = 1.0f;
        Body.velocity = Vector3.zero;
        Body.ResetCenterOfMass();
        Body.ResetInertiaTensor();
    }
    ///////////////////////////////////////////////////////////////////////////////////////////////////////
    // Director Stopped / Played
    ///////////////////////////////////////////////////////////////////////////////////////////////////////

    void OnDirectorStopped(PlayableDirector director)
    {
        if (AttackSequenceDirector == director)
        {
            ReleaseSequenceDirector.Play();
        }
    }

    void OnDirectorPlayed(PlayableDirector director)
    {
        if (AttackSequenceDirector == director)
        { 
            ReleaseSequenceDirector.Stop();
            ReleaseWeight = 1.0f;
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////
    // Input Responders
    ///////////////////////////////////////////////////////////////////////////////////////////////////////

    public void PaddleRight()
    {
        if (TimeSinceLastStroke >= MinTimeBetweenStrokes)
        { 
            RotationPolarity = -1.0f;
            AttackSequenceDirector.Play();
            TimeSinceLastStroke = 0.0f;
        }
    }

    public void PaddleLeft()
    {
        if (TimeSinceLastStroke >= MinTimeBetweenStrokes)
        {
            RotationPolarity = 1.0f;
            AttackSequenceDirector.Play();
            TimeSinceLastStroke = 0.0f;
        }
    }

    public void StopPaddle()
    {
        AttackSequenceDirector.Stop();
    }
}
