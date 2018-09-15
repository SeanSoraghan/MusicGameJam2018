using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class PlayerController : MonoBehaviour
{
    public AudioManager AudioMgr;
    public EnvironmentScroller EnvironmentController;
    private float forwardForce = 0.0f;
    public float ForwardForce = 0.0f;
    public float MaxForwardForce = 10.0f;
    public float RotationDelta = 0.0f;
    public float MaxRotationDelta = 10.0f;
    public float MinTimeBetweenStrokes = 0.1f;
    public float XBounds = 25.0f;
    private float TimeSinceLastStroke = 0.0f;
    public PlayableDirector AttackSequenceDirector;
    public PlayableDirector ReleaseSequenceDirector;

    public float ReleaseWeight = 1.0f;
    private float RotationPolarity = 1.0f;
    //const int numTrailPoints = 4;
    //private List<Vector2> TrailPoints = new List<Vector2>();
    Vector2 TrailEnd =  Vector2.zero;

    public GameObject ForwardIndicator = null;

    public Transform WaterPlaneFront;
    public Transform WaterPlaneBack;

    private Rigidbody Body;

	void Start ()
    {
        Body = GetComponent<Rigidbody>();
        AttackSequenceDirector.stopped += OnDirectorStopped;
        AttackSequenceDirector.played += OnDirectorPlayed;
        ReleaseSequenceDirector.stopped += OnDirectorStopped;
        ReleaseSequenceDirector.played += OnDirectorPlayed;
        //TrailPoints.Add()
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
            ForwardIndicator.transform.position = transform.position + transform.forward * 4 + new Vector3(0.0f,1.0f,0.0f);
        }
		if (Body != null)
        {
            forwardForce = Mathf.Clamp(forwardForce + ForwardForce * MaxForwardForce, 0.0f, MaxForwardForce) * ReleaseWeight;
            Body.AddForce(transform.forward * forwardForce);
        }
        float rotDelta = RotationDelta * RotationPolarity * MaxRotationDelta * ReleaseWeight;
        Vector3 euler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(euler.x, euler.y + rotDelta, euler.z);

        if (Mathf.Abs(transform.position.x) > XBounds)
        { 
            ResetPlayer ();
            EnvironmentController.ResetGame ();
        }

        // reset level when stopped?
        //if (AudioMgr.LoopLevel > 0 && Bo)
        //    AudioMgr.LoopLevel = 0;
	}

    private void Update ()
    {
        UpdateWaterPlanes();
    }

    void UpdateWaterPlanes()
    {
        float waterWidth = EnvironmentController.TerrainWidth;
        float waterLength = EnvironmentController.TerrainLength;
        Vector2 XRange = new Vector2(WaterPlaneFront.position.x - waterWidth * 0.5f, WaterPlaneFront.position.x + waterWidth * 0.5f);
        Vector2 YRange = new Vector2(WaterPlaneFront.position.z - waterLength * 0.5f, WaterPlaneFront.position.z + waterLength * 0.5f);
        //if (transform.position.z > YRange.x && transform.position.z < YRange.y)
        //{
        //    Transform oldBack = WaterPlaneBack;
        //    WaterPlaneBack = WaterPlaneFront;
        //    WaterPlaneFront = oldBack;
        //    TrailEnd = new Vector2(TrailEnd.x, TrailEnd.y + 1);
        //}
        XRange = new Vector2(WaterPlaneBack.position.x - waterWidth * 0.5f, WaterPlaneBack.position.x + waterWidth * 0.5f);
        YRange = new Vector2(WaterPlaneBack.position.z - waterLength * 0.5f, WaterPlaneBack.position.z + waterLength * 0.5f);
        Vector2 pos = GetNormalisedPositionOnWater(WaterPlaneBack, XRange, YRange);
        Vector2 v = new Vector2(Body.velocity.x, Body.velocity.z);
        v.Normalize();
        Vector2 trailEnd = pos + v * 0.3f;
        TrailEnd = Vector2.Lerp(TrailEnd, trailEnd, Time.deltaTime);
        WaterPlaneBack.GetComponent<MeshRenderer>().material.SetVector("TrailPoints", new Vector4(pos.x, pos.y, TrailEnd.x, TrailEnd.y));
        WaterPlaneFront.GetComponent<MeshRenderer>().material.SetVector("TrailPoints", new Vector4(pos.x, pos.y + 1.0f, TrailEnd.x, TrailEnd.y + 1.0f));
    }

    public void SwapWaterPlanes()
    {
        Transform oldBack = WaterPlaneBack;
        WaterPlaneBack = WaterPlaneFront;
        WaterPlaneFront = oldBack;
        TrailEnd = new Vector2(TrailEnd.x, TrailEnd.y + 1);
    }

    Vector2 GetNormalisedPositionOnWater(Transform waterPlane, Vector2 XRange, Vector2 YRange)
    {
        float waterWidth =  100.0f;
        float waterLength = 100.0f;
        float x = (transform.position.x - XRange.x) / waterWidth;
        float y = (transform.position.z - YRange.x) / waterLength;
        return new Vector2(1.0f - x, 1.0f - y);
    }
    //private void OnCollisionEnter (Collision collision)
    //{
    //    if (collision.gameObject.GetComponent<IslandObstacleController> () != null && EnvironmentController != null)
    //    {
    //        ResetPlayer ();
    //        EnvironmentController.ResetGame ();
    //    }
    //}

    void ResetPlayer()
    {
        AttackSequenceDirector.Stop();
        ReleaseSequenceDirector.Stop();
        forwardForce = 0.0f;
        ForwardForce = 0.0f;
        RotationDelta = 0.0f;
        ReleaseWeight = 1.0f;
        Body.velocity = Vector3.zero;
        Body.angularVelocity = Vector3.zero;
        Body.ResetCenterOfMass();
        Body.ResetInertiaTensor();
        Body.Sleep();
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
        //else if (ReleaseSequenceDirector == director)
        //{
        //    ReleaseWeight = 0.0f;
        //}
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
            AudioMgr.PaddleRightPressed(TimeSinceLastStroke);
            Body.WakeUp();
            RotationPolarity = -1.0f;
            AttackSequenceDirector.Play();
            TimeSinceLastStroke = 0.0f;
        }
    }

    public void PaddleLeft()
    {
        if (TimeSinceLastStroke >= MinTimeBetweenStrokes)
        {
            AudioMgr.PaddleLeftPressed(TimeSinceLastStroke);
            Body.WakeUp();
            RotationPolarity = 1.0f;
            AttackSequenceDirector.Play();
            TimeSinceLastStroke = 0.0f;
        }
    }

    public void PaddleRightReleased()
    {
        AudioMgr.PaddleRightReleased(TimeSinceLastStroke);
        StopPaddle();
    }

    public void PaddleLeftReleased()
    {
        AudioMgr.PaddleLeftReleased(TimeSinceLastStroke);
        StopPaddle();
    }

    public void StopPaddle()
    {
        AttackSequenceDirector.Stop();
    }
}
