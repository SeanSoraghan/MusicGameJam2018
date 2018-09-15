using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioFadeOut
{
    public static IEnumerator FadeOut (AudioSource audioSource, float FadeTime)
    {
        float startVolume = audioSource.volume;
 
        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / FadeTime;
 
            yield return null;
        }
 
        audioSource.Stop ();
        audioSource.volume = startVolume;
    }
}

public class AudioManager : MonoBehaviour
{
    enum AudioLevel
    {
        Pad,
        PadLead,
        PadLeadDrums,
        PadLeadDrumsBass,
        NumLevels
    };

    public int NumClipsPerLevel = 4;
    static string GetLevelName(AudioLevel level)
    {
        switch (level)
        {
            case AudioLevel.Pad: return "NoLead";
            case AudioLevel.PadLead: return "NoDrums";
            case AudioLevel.PadLeadDrums: return "NoBass";
            case AudioLevel.PadLeadDrumsBass: return "AllInstruments";
            default: return "";
        }
    }
    private List<List<AudioClip>> AudioClipsRight = new List<List<AudioClip>>();
    private List<List<AudioClip>> AudioClipsLeft = new List<List<AudioClip>>();
    private AudioSource AudioSourceRight;
    private AudioSource AudioSourceLeft;
    public int LoopLevel = 0;
    public int LoopsPerLevel = 4;
    public double NumEighthNotesPerLoop = 6.0;
    public double BeatsPerMinute = 80.0;
    public double LoopCompleteThreshold = 0.2;
    private double SecondsPerLoop = 0.0;
    private int NumConsecutiveLoops = 0;

    private double StopSampleThreshold = 0.3;

	// Use this for initialization
	void Start ()
    {
        double EighthsPerMinute = BeatsPerMinute * 2.0;
        double EighthsPerSecond = EighthsPerMinute / 60.0;
        double SecondsPerEighth = 1.0 / EighthsPerSecond;
        SecondsPerLoop = 1.5;//SecondsPerEighth * NumEighthNotesPerLoop;

        AudioSourceRight = gameObject.AddComponent<AudioSource>();
        AudioSourceLeft = gameObject.AddComponent<AudioSource>();
        AudioSourceRight.playOnAwake = false;
        AudioSourceLeft.playOnAwake = false;
        AudioSourceRight.loop = false;
        AudioSourceLeft.loop = false;
        
	    for (int i = 0; i < (int)AudioLevel.NumLevels; ++i)
        {
            AudioClipsRight.Add(new List<AudioClip>());
            AudioClipsLeft.Add(new List<AudioClip>());
            string levelName = GetLevelName((AudioLevel)i);
            for (int j = 0; j < NumClipsPerLevel; ++j)
            {
                int loopIndex = j + 1;
                if (i == 3)
                    loopIndex = loopIndex * 10 + 2;
                string clipNameRight = "LoopR" + loopIndex;
                string clipNameLeft = "LoopL" + loopIndex;
                string clipPathRight = levelName + "/" + clipNameRight;
                string clipPathLeft = levelName + "/" + clipNameLeft;

                AudioClip clipRight = Resources.Load(clipPathRight) as AudioClip;
                if (clipRight != null)
                    AudioClipsRight[i].Add(clipRight);
                else
                    Debug.LogError("Clip " + clipPathRight + " Not Found!");

                AudioClip clipLeft = Resources.Load(clipPathLeft) as AudioClip;
                if (clipLeft != null)
                    AudioClipsLeft[i].Add(clipLeft);
                else
                    Debug.LogError("Clip " + clipPathLeft + " Not Found!");
            }
        }
	}

    public void PaddleRightPressed(float TimeSinceLastStroke)
    {
        AudioFadeOut.FadeOut(AudioSourceRight, 0.1f);
        //AudioSourceRight.Stop();
        UpdateLevel((double)TimeSinceLastStroke);
        int clipIndex = GetClipNumber();
        AudioSourceRight.clip = AudioClipsRight[LoopLevel][clipIndex];
        AudioSourceRight.Play();
    }
    
    public void PaddleRightReleased(float TimeSinceLastStroke)
    {
        if (TimeSinceLastStroke < SecondsPerLoop * StopSampleThreshold)
        {
            AudioFadeOut.FadeOut(AudioSourceRight, 0.1f);
            NumConsecutiveLoops = Mathf.Clamp(NumConsecutiveLoops - 1, 0, LoopsPerLevel);
        }
    }

    public void PaddleLeftPressed(float TimeSinceLastStroke)
    {
        AudioFadeOut.FadeOut(AudioSourceLeft, 0.1f);
        UpdateLevel((double)TimeSinceLastStroke);
        int clipIndex = GetClipNumber();
        AudioSourceLeft.clip = AudioClipsLeft[LoopLevel][clipIndex];
        AudioSourceLeft.Play();
    }
    
    public int GetClipNumber()
    {
        return NumConsecutiveLoops == 0 ? 0 : 
               NumConsecutiveLoops == LoopsPerLevel - 1 ? NumClipsPerLevel - 1 : 
               Random.Range(0,NumClipsPerLevel - 1);
    }

    public void PaddleLeftReleased(float TimeSinceLastStroke)
    {
        if (TimeSinceLastStroke < SecondsPerLoop * StopSampleThreshold)
        { 
            AudioFadeOut.FadeOut(AudioSourceLeft, 0.1f);
            NumConsecutiveLoops = Mathf.Clamp(NumConsecutiveLoops - 1, 0, LoopsPerLevel);
        }
    }

    void UpdateLevel(double TimeSinceLastStroke)
    {
        Debug.Log("Since Last Stroke: " + TimeSinceLastStroke);
        Debug.Log("Seconds Per Loop: " + SecondsPerLoop);
        Debug.Log("Difference: " + Mathf.Abs((float)(TimeSinceLastStroke - SecondsPerLoop)));
        if (Mathf.Abs((float)(TimeSinceLastStroke - SecondsPerLoop)) <= LoopCompleteThreshold)
        {
            ++NumConsecutiveLoops;
            if (NumConsecutiveLoops % LoopsPerLevel == 0 && NumConsecutiveLoops > 0)
            {
                LoopLevel = Mathf.Clamp(LoopLevel + 1, 0, (int)AudioLevel.NumLevels - 1);
                Debug.Log("^^^^^^ LEVEL INCREASED! ^^^^^^");
                NumConsecutiveLoops = 0;
            }
        }
        else
        {
            NumConsecutiveLoops = 0;
            LoopLevel = Mathf.Clamp(LoopLevel - 1, 0, (int)AudioLevel.NumLevels - 1);
            Debug.Log("...... Decreased Level .....");
        }
    }
}
