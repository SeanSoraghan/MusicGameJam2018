using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    enum AudioLevel
    {
        NumLevels = 7
    };

    static string GetLevelName(int level)
    {
        return "Level"+(level+1);
    }
    public int NumClipsPerLevel = 4;
    //private List<List<AudioClip>> AudioClipsRight = new List<List<AudioClip>>();
    //private List<List<AudioClip>> AudioClipsLeft = new List<List<AudioClip>>();
    private List<List<AudioClip>> AudioClips = new List<List<AudioClip>>();
    private AudioSource AudioSourceRight;
    private AudioSource AudioSourceLeft;
    private AudioSource AudioSourceForward;
    public int LoopLevel = 0;
    public int LoopsPerLevel = 4;
    public double NumEighthNotesPerLoop = 6.0;
    public double BeatsPerMinute = 80.0;
    public double LoopCompleteThreshold = 0.2;
    public PlayerController Player;
    public double SecondsPerLoop = 0.0;
    private int NumConsecutiveLoops = 0;

    private bool RightSourceFading = false;
    private bool LeftSourceFading = false;
    private bool ForwardSourceFading = false;
    private double StopSampleThreshold = 0.8;
    private float fadeRate = 10.0f;

    private int NumIncorrectStrokes = 0;
	// Use this for initialization
	void Start ()
    {
        double EighthsPerMinute = BeatsPerMinute * 2.0;
        double EighthsPerSecond = EighthsPerMinute / 60.0;
        double SecondsPerEighth = 1.0 / EighthsPerSecond;
        SecondsPerLoop = 1.5;//SecondsPerEighth * NumEighthNotesPerLoop;

        AudioSourceRight = gameObject.AddComponent<AudioSource>();
        AudioSourceLeft = gameObject.AddComponent<AudioSource>();
        AudioSourceForward = gameObject.AddComponent<AudioSource>();
        AudioSourceRight.playOnAwake = false;
        AudioSourceLeft.playOnAwake = false;
        AudioSourceRight.loop = false;
        AudioSourceLeft.loop = false;
        AudioSourceForward.playOnAwake = false;
        AudioSourceForward.loop = false;
        
	    for (int i = 0; i < (int)AudioLevel.NumLevels; ++i)
        {
            //AudioClipsRight.Add(new List<AudioClip>());
            //AudioClipsLeft.Add(new List<AudioClip>());

            AudioClips.Add(new List<AudioClip>());

            //string levelName = GetLevelName((AudioLevel)i);
            string levelName = GetLevelName(i);
            for (int j = 0; j < NumClipsPerLevel; ++j)
            {
                int loopIndex = j + 1;
                string clipNameRight = "LoopR" + loopIndex;
                string clipNameLeft = "LoopL" + loopIndex;
                string clipPathRight = levelName + "/" + clipNameRight;
                string clipPathLeft = levelName + "/" + clipNameLeft;

                AudioClip clipRight = Resources.Load(clipPathRight) as AudioClip;
                if (clipRight != null)
                    AudioClips/*Right*/[i].Add(clipRight);
                else
                    Debug.LogError("Clip " + clipPathRight + " Not Found!");

                AudioClip clipLeft = Resources.Load(clipPathLeft) as AudioClip;
                if (clipLeft != null)
                    AudioClips/*Left*/[i].Add(clipLeft);
                else
                    Debug.LogError("Clip " + clipPathLeft + " Not Found!");
            }
        }
	}

    private void Update ()
    {
        if (LeftSourceFading && AudioSourceLeft.isPlaying)
        {
            AudioSourceLeft.volume = Mathf.Lerp(AudioSourceLeft.volume, 0.0f, Time.deltaTime * fadeRate);
            if (AudioSourceLeft.volume <= 0)
                AudioSourceLeft.Stop();
        }
        if (RightSourceFading && AudioSourceRight.isPlaying)
        {
            AudioSourceRight.volume = Mathf.Lerp(AudioSourceRight.volume, 0.0f, Time.deltaTime * fadeRate);
            if (AudioSourceRight.volume <= 0)
                AudioSourceRight.Stop();
        }
        if (ForwardSourceFading && AudioSourceForward.isPlaying)
        {
            AudioSourceForward.volume = Mathf.Lerp(AudioSourceForward.volume, 0.0f, Time.deltaTime * fadeRate);
            if (AudioSourceForward.volume <= 0)
                AudioSourceForward.Stop();
        }
    }

    public void SetLoopLevel(int l)
    {
        LoopLevel = l;
        Player.Level = (float)LoopLevel / (float)AudioLevel.NumLevels;
    }

    public void PaddleRightPressed(float TimeSinceLastStroke)
    {
        AudioSourceRight.Stop();
        RightSourceFading = false;
        AudioSourceRight.volume = 1.0f;
        //AudioSourceRight.Stop();
        //UpdateLevel((double)TimeSinceLastStroke);
        //int clipIndex = GetClipNumber();
        AudioSourceRight.clip = GetNextClip();//AudioClipsRight[LoopLevel][clipIndex];
        AudioSourceRight.Play();
    }
    
    public void PaddleRightReleased(float TimeSinceLastStroke)
    {
        if (TimeSinceLastStroke < SecondsPerLoop * StopSampleThreshold)
        {
            RightSourceFading = true;
            //NumConsecutiveLoops = Mathf.Clamp(NumConsecutiveLoops - 1, 0, LoopsPerLevel);
        }
    }

    public void PaddleLeftPressed(float TimeSinceLastStroke)
    {
        AudioSourceLeft.Stop();
        LeftSourceFading = false;
        AudioSourceLeft.volume = 1.0f;
        //UpdateLevel((double)TimeSinceLastStroke);
        //int clipIndex = GetClipNumber();
        AudioSourceLeft.clip = GetNextClip();//AudioClipsLeft[LoopLevel][clipIndex];
        AudioSourceLeft.Play();
    }
    
    public void PaddleLeftReleased(float TimeSinceLastStroke)
    {
        if (TimeSinceLastStroke < SecondsPerLoop * StopSampleThreshold)
        { 
            LeftSourceFading = true;
            //NumConsecutiveLoops = Mathf.Clamp(NumConsecutiveLoops - 1, 0, LoopsPerLevel);
        }
    }

    public void PaddleForwardPressed(float TimeSinceLastStroke)
    {
        AudioSourceForward.Stop();
        ForwardSourceFading = false;
        AudioSourceForward.volume = 1.0f;
        //UpdateLevel((double)TimeSinceLastStroke);
        //int clipIndex = GetClipNumber();
        AudioSourceForward.clip = GetNextClip();//AudioClipsLeft[LoopLevel][clipIndex];
        AudioSourceForward.Play();
    }
    
    public void PaddleForwardReleased(float TimeSinceLastStroke)
    {
        if (TimeSinceLastStroke < SecondsPerLoop * StopSampleThreshold)
        { 
            ForwardSourceFading = true;
            //NumConsecutiveLoops = Mathf.Clamp(NumConsecutiveLoops - 1, 0, LoopsPerLevel);
        }
    }

    public AudioClip GetNextClip()
    {
        Debug.Log("Num Consec: " + NumConsecutiveLoops + " Num Clips Per Level: " + NumClipsPerLevel);
        Debug.Log("Chosen Clip: " + NumConsecutiveLoops % (NumClipsPerLevel * 2));
        return AudioClips[LoopLevel][NumConsecutiveLoops % (NumClipsPerLevel * 2)];
        //return NumConsecutiveLoops == 0 ? 0 : 
        //       NumConsecutiveLoops == LoopsPerLevel - 1 ? NumClipsPerLevel - 1 : 
        //       Random.Range(1,NumClipsPerLevel - 2);
    }

    public bool UpdateLevel(double TimeSinceLastStroke, double TimeSinceLastGlobalMetronomeTick)
    {
        if (Mathf.Abs((float)(TimeSinceLastStroke - SecondsPerLoop)) <= LoopCompleteThreshold)
        {
            ++NumConsecutiveLoops;
            if (NumConsecutiveLoops % LoopsPerLevel == 0 && NumConsecutiveLoops > 0)
            {
                LoopLevel = Mathf.Clamp(LoopLevel + 1, 0, (int)AudioLevel.NumLevels - 1);
                Debug.Log("^^^^^^ LEVEL INCREASED! ^^^^^^");
                Player.Level = (float)LoopLevel / (float)AudioLevel.NumLevels;
                NumConsecutiveLoops = 0;
            }
            return true;
        }
        if (NumConsecutiveLoops > 0)
        {
            NumConsecutiveLoops = 0;
            LoopLevel = Mathf.Clamp(LoopLevel - 1, 0, (int)AudioLevel.NumLevels - 1);
            Debug.Log("...... Decreased Level .....");
            Player.Level = (float)LoopLevel / (float)AudioLevel.NumLevels;
            return false;
        }
        if (NumIncorrectStrokes > 0)
        {
            Debug.Log("Incorrect Stroke");
            NumIncorrectStrokes = 0;
            return false;
        }
        // Initial stroke.
        float firstStrokeAllowanceAnticipate = (float)SecondsPerLoop * 0.3f;
        float firstStrokeAllowanceReact = (float)SecondsPerLoop * 0.1f;
        if (Mathf.Abs((float)(TimeSinceLastGlobalMetronomeTick - SecondsPerLoop)) <= firstStrokeAllowanceAnticipate ||
            TimeSinceLastGlobalMetronomeTick <= firstStrokeAllowanceReact)
        {
            NumIncorrectStrokes = 0;
            return true;
        }
        ++NumIncorrectStrokes;
        return false;
    }
}
