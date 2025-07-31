using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Samples elbow and shoulder joint angles at a fixed rate and writes them to a CSV file.
/// Attach this to a GameObject in your scene and assign the bone transforms in the Inspector.
/// </summary>
public class PostureLogger : MonoBehaviour
{
    [Header("Bone Transforms (assign from your Animator)")]
    [Tooltip("Transform of the left shoulder (upper arm root)")]
    public Transform leftShoulder;
    [Tooltip("Transform of the left elbow (joint between upper arm and forearm)")]
    public Transform leftElbow;
    [Tooltip("Transform of the left wrist (end of forearm)")]
    public Transform leftWrist;

    [Tooltip("Transform of the right shoulder (upper arm root)")]
    public Transform rightShoulder;
    [Tooltip("Transform of the right elbow (joint between upper arm and forearm)")]
    public Transform rightElbow;
    [Tooltip("Transform of the right wrist (end of forearm)")]
    public Transform rightWrist;

    [Header("Logging Settings")]
    [Tooltip("Time between samples in seconds (e.g. 1/50 for 50 Hz)")]
    public float sampleInterval = 1f / 50f;
    [Tooltip("Prefix for the CSV filename (timestamp will be appended)")]
    public string fileNamePrefix = "posture_log";
    [Tooltip("Index of the current trial (manually increment from other scripts)")]
    public int trialIndex = 0;
    [Tooltip("ID of the current target (manually set before each trial)")]
    public int targetID = 0;

    private StreamWriter _writer;
    private float _lastSampleTime;

    void Start()
    {
        // Construct filename with timestamp
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string logsDir = Path.Combine(Application.dataPath, "Logs");
        Directory.CreateDirectory(logsDir);
        string fileName = $"{fileNamePrefix}_{timestamp}.csv";
        string filePath = Path.Combine(logsDir, fileName);

        _writer = new StreamWriter(filePath, false);
        _writer.WriteLine("time,trialIndex,targetID,leftElbowAngle,leftShoulderAngle,rightElbowAngle,rightShoulderAngle");
        _lastSampleTime = Time.time;
    }

    void FixedUpdate()
    {
        // if (Time.time - _lastSampleTime >= sampleInterval)
        // {
        //     LogPosture();
        //     _lastSampleTime = Time.time;
        // }
        LogPosture();
        Debug.Log("Fixed Timestep: " + Time.fixedDeltaTime);
    }

    void LogPosture()
    {
        float time = Time.time;
        // Example: Use localEulerAngles.x as the angle; adjust as needed for your rig
        float leftElbowAngle = leftElbow ? leftElbow.localEulerAngles.x : 0f;
        float leftShoulderAngle = leftShoulder ? leftShoulder.localEulerAngles.x : 0f;
        float rightElbowAngle = rightElbow ? rightElbow.localEulerAngles.x : 0f;
        float rightShoulderAngle = rightShoulder ? rightShoulder.localEulerAngles.x : 0f;

        string line = $"{time:F3},{trialIndex},{targetID},{leftElbowAngle:F2},{leftShoulderAngle:F2},{rightElbowAngle:F2},{rightShoulderAngle:F2}";
        _writer.WriteLine(line);
        _writer.Flush();
    }

    // Optionally, allow other scripts to set trial/target IDs
    public void SetTrialAndTarget(int trial, int target)
    {
        trialIndex = trial;
        targetID = target;
    }

    void OnDestroy()
    {
        if (_writer != null)
        {
            _writer.Flush();
            _writer.Close();
        }
    }
}