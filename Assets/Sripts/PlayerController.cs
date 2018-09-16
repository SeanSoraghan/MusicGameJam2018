using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEngine.PostProcessing;

public class PlayerController : MonoBehaviour
{
    public AudioManager AudioMgr;
    public EnvironmentScroller EnvironmentController;
    private float forwardForce = 0.0f;
    public float ForwardForce = 0.0f;
    public float MinForwardForce = 4000.0f;
    public float MaxForwardForce = 8000.0f;
    private float forceMultiplier = 4000.0f;
    public float RotationDelta = 0.0f;
    public float MaxRotationDelta = 10.0f;
    public float MinTimeBetweenStrokes = 0.1f;
    public float XBounds = 25.0f;
    private float TimeSinceLastStroke = 0.0f;
    public PlayableDirector AttackSequenceDirector;
    public PlayableDirector ReleaseSequenceDirector;

    public float ReleaseWeight = 1.0f;
    public float TrailLength = 0.1f;
    private float RotationPolarity = 1.0f;

    [Range(0.0f,100.0f)] public float MinTrailGranularity = 7.0f;
    [Range(0.0f,100.0f)] public float MaxTrailGranularity = 40.0f;
    private float trailGranularity;
    [Range(0.0f,100.0f)] public float MinTrailSpeed = 10.0f;
    [Range(0.0f,100.0f)] public float MaxTrailSpeed = 80.0f;
    private float trailSpeed;
    [Range(0.0f,1.0f)] public float MinTrailWidth = 0.3f;
    [Range(0.0f,1.0f)] public float MaxTrailWidth = 0.6f;
    private float trailWidth;

    //const int numTrailPoints = 4;
    //private List<Vector2> TrailPoints = new List<Vector2>();
    Vector2 TrailEnd =  Vector2.zero;

    public GameObject ForwardIndicator = null;

    public Transform WaterPlaneFront;
    public Transform WaterPlaneBack;

    [Range(0.0f, 100.0f)] public float MinBloom = 3.0f;
    [Range(0.0f, 100.0f)] public float MaxBloom = 20.0f;
    private float bloom;
    private float normedBloom = 0.0f;
    private float TimeSinceBloomBurst = 0.0f;

    private MeshRenderer backRenderer;
    private MeshRenderer frontRenderer;

    private float t = 0.05f;
    private float rippleZDistance = 0.2f;
    private Vector2 TrailStartOffset = Vector2.zero;

    private Vector2 waterSpeedRange = new Vector2(7.0f, 10.0f);
    private Vector2 waterGranRange = new Vector2(4.0f, 10.0f);
    private float currentWaterSpeed = 0.0f;
    private float currentWaterGran = 0.0f;
    private float targetWaterSpeed = 0.0f;
    private float targetWaterGran = 0.0f;

    private float TimeSinceStopped = 0.0f;
    public float StoppedTimeThreshold = 0.5f; 
    private float level = 0.0f;
    public float Level
    {
        get
        {
            return level;
        }
        set
        {
            level = value;
            LevelUpdated();
        }
    }
    private void LevelUpdated()
    {
        trailGranularity = level * (MaxTrailGranularity - MinTrailGranularity) + MinTrailGranularity;
        trailSpeed = level * (MaxTrailSpeed - MinTrailSpeed) + MinTrailSpeed;
        trailWidth = (1.0f - level) * (MaxTrailWidth - MinTrailWidth) + MinTrailWidth;
        bloom = normedBloom * (MaxBloom - MinBloom) + MinBloom;
        forceMultiplier = level * (MaxForwardForce - MinForwardForce) + MinForwardForce;
        SetMaterialProperties();
    }

    public PostProcessingBehaviour PPBehaviour;
    private PostProcessingProfile PPProfile;

    private Rigidbody Body;

	void Start ()
    {
        Body = GetComponent<Rigidbody>();
        AttackSequenceDirector.stopped += OnDirectorStopped;
        AttackSequenceDirector.played += OnDirectorPlayed;
        ReleaseSequenceDirector.stopped += OnDirectorStopped;
        ReleaseSequenceDirector.played += OnDirectorPlayed;

        Init();

        PPProfile = PPBehaviour.profile;


        LevelUpdated ();


        BloomBurst();
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
            forwardForce = Mathf.Clamp(forwardForce + ForwardForce * forceMultiplier, 0.0f, forceMultiplier) * ReleaseWeight;
            Body.AddForce(transform.forward * forwardForce);
            //backRenderer.material.SetFloat("_GlobalTrailWeight", forwardForce / forceMultiplier);
            //frontRenderer.material.SetFloat("_GlobalTrailWeight", forwardForce / forceMultiplier);
        }
        float rotDelta = RotationDelta * RotationPolarity * MaxRotationDelta * ReleaseWeight;
        Vector3 euler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(euler.x, euler.y + rotDelta, euler.z);

        float eulerY = Mathf.Abs (transform.rotation.normalized.eulerAngles.y);
        if (eulerY > 75.0f && eulerY < 285.0f)
        {
            ResetPlayer ();
            AudioMgr.SetLoopLevel (0);
            EnvironmentController.ResetGame ();
            if (WaterPlaneFront.position.z < WaterPlaneBack.position.z)
                SwapWaterPlanes();
            Init();
        }

        // reset level when stopped?
        //if (AudioMgr.LoopLevel > 0 && Bo)
        //    AudioMgr.LoopLevel = 0;

        //if (Body.velocity.magnitude <= 0.001f)
        //{
        //    TimeSinceStopped += Time.deltaTime;
        //    if (TimeSinceStopped >= StoppedTimeThreshold && AudioMgr.LoopLevel != 0)
        //        AudioMgr.LoopLevel = 0;
        //}
        //else
        //{
        //    TimeSinceStopped = 0.0f;
        //}
    }

    private void Init()
    {
        float waterWidth = EnvironmentController.TerrainWidth;
        float waterLength = EnvironmentController.TerrainLength;
        Vector2 XRange = new Vector2(WaterPlaneBack.position.x - waterWidth * 0.5f, WaterPlaneBack.position.x + waterWidth * 0.5f);
        Vector2 YRange = new Vector2(WaterPlaneBack.position.z - waterLength * 0.5f, WaterPlaneBack.position.z + waterLength * 0.5f);
        TrailEnd = GetNormalisedPositionOnWater(WaterPlaneBack, XRange, YRange);
        backRenderer = WaterPlaneBack.GetComponent<MeshRenderer>();
        frontRenderer = WaterPlaneFront.GetComponent<MeshRenderer>();
    }
    private void Update ()
    {
        UpdateWaterPlanes();
        if (currentWaterGran != targetWaterGran)
        {
            currentWaterGran = Mathf.Lerp(currentWaterGran, targetWaterGran, Time.deltaTime);
            if (Mathf.Abs(currentWaterGran - targetWaterGran) < 0.001f)
                currentWaterGran = targetWaterGran;
            float gr = currentWaterGran * 6.0f + 4.0f;
            backRenderer.material.SetFloat("_TextureShiftGranularity", gr);
            frontRenderer.material.SetFloat("_TextureShiftGranularity", gr);        
        }
        if (currentWaterSpeed != targetWaterSpeed)
        {
            currentWaterSpeed = Mathf.Lerp(currentWaterSpeed, targetWaterSpeed, Time.deltaTime);
            if (Mathf.Abs(currentWaterSpeed - targetWaterSpeed) < 0.001f)
                currentWaterSpeed = targetWaterSpeed;
            float s = currentWaterSpeed * 3.0f + 7.0f;
            backRenderer.material.SetFloat("_NSpeed", s);
            frontRenderer.material.SetFloat("NSpeed", s);
        }
        if (TimeSinceBloomBurst < AudioMgr.SecondsPerLoop)
        {
            TimeSinceBloomBurst += Time.deltaTime;
            float bloomT = (TimeSinceBloomBurst / (float)AudioMgr.SecondsPerLoop);
            normedBloom = Mathf.Lerp(normedBloom, level, bloomT);
            if (TimeSinceBloomBurst >= AudioMgr.SecondsPerLoop)
            { 
                normedBloom = level;
                BloomBurst();
            }
            bloom = normedBloom * (MaxBloom - MinBloom) + MinBloom;
            SetBloom(bloom);
        }
        float trailWeight = 1.0f - Mathf.Clamp(TimeSinceLastStroke / (float)AudioMgr.SecondsPerLoop, 0.0f, 1.0f);
        float globalWeight = forwardForce / forceMultiplier;
        backRenderer.material.SetFloat("_GlobalTrailWeight", globalWeight);
        frontRenderer.material.SetFloat("_GlobalTrailWeight", globalWeight);

        if (globalWeight > 0.0f)
        {
            TrailEnd = Vector2.Lerp(TrailEnd, new Vector2(TrailEnd.x, TrailEnd.y + 0.01f), Time.deltaTime);
        }
    }

    void BloomBurst()
    {
        TimeSinceBloomBurst = 0.0f;
        normedBloom = 1.5f;
    }

    void UpdateWaterPlanes()
    {
        float waterWidth = EnvironmentController.TerrainWidth;
        float waterLength = EnvironmentController.TerrainLength;
        Vector2 XRange = new Vector2(WaterPlaneBack.position.x - waterWidth * 0.5f, WaterPlaneBack.position.x + waterWidth * 0.5f);
        Vector2 YRange = new Vector2(WaterPlaneBack.position.z - waterLength * 0.5f, WaterPlaneBack.position.z + waterLength * 0.5f);
        Vector2 pos = GetNormalisedPositionOnWater(WaterPlaneBack, XRange, YRange);
        Vector2 v = new Vector2(Body.velocity.x, Body.velocity.z);
        //v.Normalize();
        Vector2 trailEnd = pos + v * TrailLength;
        TrailEnd = /*pos + new Vector2(0.0f, 0.1f);//*/Vector2.Lerp(TrailEnd, trailEnd, Time.deltaTime);
        //pos += TrailStartOffset;
        //TrailEnd += TrailStartOffset;
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
        return new Vector2(1.0f - x, 1.0f - y) + TrailStartOffset;
    }

    void SetMaterialProperties()
    {
        backRenderer.material.SetFloat("_TrailGranularity", trailGranularity);
        frontRenderer.material.SetFloat("_TrailGranularity", trailGranularity);
  
        backRenderer.material.SetFloat("_TrailSpeed", trailSpeed);
        frontRenderer.material.SetFloat("_TrailSpeed", trailSpeed);

        backRenderer.material.SetFloat("_MaxTrailWidth", trailWidth);
        frontRenderer.material.SetFloat("_MaxTrailWidth", trailWidth);

        backRenderer.material.SetFloat("_MinTrailWidth", 0.05f);
        frontRenderer.material.SetFloat("_MinTrailWidth", 0.05f);

        targetWaterSpeed = level;
        targetWaterGran = level;
        //float s = level * 3.0f + 7.0f;
        //backRenderer.material.SetFloat("_NSpeed", s);
        //frontRenderer.material.SetFloat("NSpeed", s);

        //float g = level * 6.0f + 4.0f;
        //backRenderer.material.SetFloat("_TextureShiftGranularity", g);
        //frontRenderer.material.SetFloat("_TextureShiftGranularity", g);

        SetBloom(bloom);
    }

    void SetBloom(float b)
    {
        BloomModel pb = PPProfile.bloom;
        BloomModel.Settings bms = pb.settings;
        BloomModel.BloomSettings bs = bms.bloom;
        bs.intensity = b;
        bms.bloom = bs;
        pb.settings = bms; 
        PPProfile.bloom = pb;
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
            BloomBurst();
            EnvironmentController.OnBeat();
            ReleaseSequenceDirector.Stop();
            ReleaseWeight = 1.0f;
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////
    // Input Responders
    ///////////////////////////////////////////////////////////////////////////////////////////////////////

    float GetLoopLengthTimeDifference()
    {
        return Mathf.Abs(TimeSinceLastStroke - (float) AudioMgr.SecondsPerLoop);
    }

    public void PaddleRight()
    {
        Debug.Log("Right");
        if (TimeSinceLastStroke >= MinTimeBetweenStrokes)
        { 
            if (AudioMgr.UpdateLevel(TimeSinceLastStroke, TimeSinceBloomBurst))
            { 
                if (AudioMgr.LoopLevel >= 6)
                {
                    targetWaterGran = 1.1f;
                    targetWaterSpeed = 1.1f;
                }
                RotationPolarity = 1.0f;
                //TrailStartOffset = new Vector2(-RotationPolarity * t, -rippleZDistance);
                AudioMgr.PaddleRightPressed(TimeSinceLastStroke);
                Body.WakeUp();
                AttackSequenceDirector.Play();
                TimeSinceLastStroke = 0.0f;

                //UpdateWaterPlanes();
            }
        }
        else
        {
            Debug.Log("Since last: " + TimeSinceLastStroke);
        }
    }

    public void PaddleRightReleased()
    {
        if (AudioMgr.LoopLevel >= 6)
        {
            targetWaterGran = level;
            targetWaterSpeed = level;
        }
        AudioMgr.PaddleRightReleased(TimeSinceLastStroke);
        StopPaddle();
    }

    public void PaddleLeft()
    {
        Debug.Log("Left");
        if (TimeSinceLastStroke >= MinTimeBetweenStrokes)
        {
            if (AudioMgr.UpdateLevel(TimeSinceLastStroke, TimeSinceBloomBurst))
            { 
                if (AudioMgr.LoopLevel >= 6)
                {
                    targetWaterGran = 1.1f;
                    targetWaterSpeed = 1.1f;
                }
                RotationPolarity = -1.0f;
                //TrailStartOffset = new Vector2(-RotationPolarity * t, -rippleZDistance);
                AudioMgr.PaddleLeftPressed(TimeSinceLastStroke);
                Body.WakeUp();
                AttackSequenceDirector.Play();
                TimeSinceLastStroke = 0.0f;

                //UpdateWaterPlanes();
            }
        }
        else
        {
            Debug.Log("Since last: " + TimeSinceLastStroke);
        }
    }

    public void PaddleLeftReleased()
    {
        if (AudioMgr.LoopLevel >= 6)
        {
            targetWaterGran = level;
            targetWaterSpeed = level;
        }
        AudioMgr.PaddleLeftReleased(TimeSinceLastStroke);
        StopPaddle();
    }

    public void PaddleForward()
    {
        if (TimeSinceLastStroke >= MinTimeBetweenStrokes)
        {
            if (AudioMgr.UpdateLevel(TimeSinceLastStroke, TimeSinceBloomBurst))
            { 
                RotationPolarity = 0.0f;
                TrailStartOffset = new Vector2(RotationPolarity, -rippleZDistance);
                AudioMgr.PaddleForwardPressed(TimeSinceLastStroke);
                Body.WakeUp();
                AttackSequenceDirector.Play();
                TimeSinceLastStroke = 0.0f;
                //UpdateWaterPlanes();
            }
        }
        else
        {
            Debug.Log("Since last: " + TimeSinceLastStroke);
        }
    }

    public void PaddleForwardReleased()
    {
        if (AudioMgr.LoopLevel >= 6)
        {
            targetWaterGran = level;
            targetWaterSpeed = level;
        }
        AudioMgr.PaddleForwardReleased(TimeSinceLastStroke);
        StopPaddle();
    }

    

    public void StopPaddle()
    {
        AttackSequenceDirector.Stop();
    }
}
