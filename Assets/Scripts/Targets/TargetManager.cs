using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Places the left- and right-hand targets at random positions inside a predefined
/// ReachVolume (a BoxCollider you position in the scene). Call <see cref="PlaceTargets"/>
/// after each successful trial to respawn the targets.
/// </summary>
public class TargetManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Left-hand target GameObject")] public Transform leftTarget;
    [Tooltip("Right-hand target GameObject")] public Transform rightTarget;

    [Header("Physical Reach")]
    [Tooltip("Left shoulder bone transform (e.g., Left Arm)")]
    public Transform leftShoulder;
    [Tooltip("Right shoulder bone transform (e.g., Right Arm)")]
    public Transform rightShoulder;
    [Tooltip("Maximum left arm length (meters)")]
    public float leftArmLength = 0.4f;
    [Tooltip("Left wrist bone transform (e.g., Left Hand)")]
    public Transform leftWrist;
    [Tooltip("Right wrist bone transform (e.g., Right Hand)")]
    public Transform rightWrist;
    [Tooltip("Maximum right arm length (meters)")]
    public float rightArmLength = 0.4f;

    [Header("Target Behaviors")]
    public TargetBehavior leftTargetBehavior;
    public TargetBehavior rightTargetBehavior;

    [Header("Target Colors")]
    public Color leftOriginalColor = Color.blue;
    public Color rightOriginalColor = Color.red;
    public Color hoverColor = Color.green;

    [Header("Target Cube")]
    [Tooltip("Name of the cube GameObject used for target positions")] 
    public string cubeObjectName = "CubeForTargets";
    private Transform cubeTransform;
    private List<Vector3> targetPositions = new List<Vector3>();
    private List<int> leftSequence = new List<int>();
    private List<int> rightSequence = new List<int>();
    private int leftSeqIdx = 0;
    private int rightSeqIdx = 0;
    private int numRepeats = 3; // Number of times each position is used per block

    private float bimanualHoldTimer = 0f;
    public float holdDuration = 0.5f;
    private float lastRespawnTime = 0f;

    // Instead, use all indices for both arms:
    private int[] allIndices = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }; // 8 corners + center

    [Header("Arm Controllers")]
    private ArmController leftArm;
    private ArmController rightArm;
    private AutoPlayer autoPlayer;
    private bool armsInitialized = false;

    private int trialIndex = 0;

    private void InitializeArms()
    {
        if (armsInitialized) return;
        var arms = FindObjectsByType<ArmController>(FindObjectsSortMode.None);
        foreach (var arm in arms)
        {
            if (arm.ikGoal == AvatarIKGoal.LeftHand)
                leftArm = arm;
            else if (arm.ikGoal == AvatarIKGoal.RightHand)
                rightArm = arm;
        }
        autoPlayer = FindAnyObjectByType<AutoPlayer>();
        armsInitialized = true;
    }

    void Start()
    {
        // Automatically calculate arm lengths
        if (leftShoulder != null && leftWrist != null)
            leftArmLength = Vector3.Distance(leftShoulder.position, leftWrist.position);
        if (rightShoulder != null && rightWrist != null)
            rightArmLength = Vector3.Distance(rightShoulder.position, rightWrist.position);
        // Find the cube and compute positions
        cubeTransform = GameObject.Find(cubeObjectName)?.transform;
        if (cubeTransform == null)
        {
            Debug.LogError($"TargetManager: Could not find cube named '{cubeObjectName}' in scene!");
        }
        else
        {
            ComputeCubePositions();
            GenerateSequences();
        }
        PlaceTargets();
        lastRespawnTime = Time.time;
        InitializeArms();
        DistanceLogger.Initialize();
        if (leftTargetBehavior != null)
            leftTargetBehavior.SetColor(leftOriginalColor);
        if (rightTargetBehavior != null)
            rightTargetBehavior.SetColor(rightOriginalColor);
    }

    private void ComputeCubePositions()
    {
        targetPositions.Clear();
        BoxCollider box = cubeTransform.GetComponent<BoxCollider>();
        Vector3 center = box.center;
        Vector3 size = box.size * 0.5f;
        // 8 corners
        Vector3[] corners = new Vector3[8];
        corners[0] = center + new Vector3(-size.x, -size.y, -size.z);
        corners[1] = center + new Vector3(size.x, -size.y, -size.z);
        corners[2] = center + new Vector3(size.x, -size.y, size.z);
        corners[3] = center + new Vector3(-size.x, -size.y, size.z);
        corners[4] = center + new Vector3(-size.x, size.y, -size.z);
        corners[5] = center + new Vector3(size.x, size.y, -size.z);
        corners[6] = center + new Vector3(size.x, size.y, size.z);
        corners[7] = center + new Vector3(-size.x, size.y, size.z);
        for (int i = 0; i < 8; i++) {
            corners[i] = cubeTransform.TransformPoint(corners[i]);
            //Debug.Log($"Corner {i}: {corners[i]}");
        }
        // Center
        Vector3 centerWorld = cubeTransform.TransformPoint(center);
        // Add to list
        targetPositions.AddRange(corners);
        targetPositions.Add(centerWorld);
    }

    private void GenerateSequences()
    {
        leftSequence.Clear();
        rightSequence.Clear();
        List<int> leftBaseList = new List<int>();
        List<int> rightBaseList = new List<int>();
        for (int r = 0; r < numRepeats; r++)
        {
            foreach (int idx in allIndices)
                leftBaseList.Add(idx);
            foreach (int idx in allIndices)
                rightBaseList.Add(idx);
        }
        // Shuffle for left
        leftSequence.AddRange(ShuffleNoImmediateRepeat(leftBaseList));
        // Shuffle for right, ensure no overlap at same index
        do {
            rightSequence.Clear();
            rightSequence.AddRange(ShuffleNoImmediateRepeat(rightBaseList));
        } while (HasOverlap(leftSequence, rightSequence));
        leftSeqIdx = 0;
        rightSeqIdx = 0;
    }

    private List<int> ShuffleNoImmediateRepeat(List<int> input)
    {
        List<int> result = new List<int>(input);
        System.Random rng = new System.Random();
        int n = result.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int temp = result[i];
            result[i] = result[j];
            result[j] = temp;
        }
        // Ensure no immediate repeats
        for (int i = 1; i < result.Count; i++)
        {
            if (result[i] == result[i - 1])
            {
                // Find a non-repeating swap
                for (int j = i + 1; j < result.Count; j++)
                {
                    if (result[j] != result[i - 1])
                    {
                        int temp = result[i];
                        result[i] = result[j];
                        result[j] = temp;
                        break;
                    }
                }
            }
        }
        return result;
    }

    private bool HasOverlap(List<int> a, List<int> b)
    {
        int n = Mathf.Min(a.Count, b.Count);
        for (int i = 0; i < n; i++)
            if (a[i] == b[i]) return true;
        return false;
    }

    /// <summary>Call this whenever you need fresh target positions.</summary>
    public void PlaceTargets()
    {
        if (targetPositions.Count < 2)
        {
            return;
        }
        // Left
        if (leftTarget != null)
        {
            leftTarget.position = targetPositions[leftSequence[leftSeqIdx]];
            leftSeqIdx = (leftSeqIdx + 1) % leftSequence.Count;
        }
        // Right
        if (rightTarget != null)
        {
            rightTarget.position = targetPositions[rightSequence[rightSeqIdx]];
            rightSeqIdx = (rightSeqIdx + 1) % rightSequence.Count;
        }
        // Start path recording for both arms
        InitializeArms();
        if (leftArm != null && leftArm.ikTarget != null && leftTarget != null)
        {
            leftArm.StartPathRecording(leftTarget.position, trialIndex);
        }
        if (rightArm != null && rightArm.ikTarget != null && rightTarget != null)
        {
            rightArm.StartPathRecording(rightTarget.position, trialIndex);
        }
    }

    void Update()
    {
        if (leftTargetBehavior == null || rightTargetBehavior == null)
            return;
        InitializeArms();

        bool leftOn = leftTargetBehavior.isHovered;
        bool rightOn = rightTargetBehavior.isHovered;

        if (leftOn && rightOn)
        {
            bimanualHoldTimer += Time.deltaTime;
            leftTargetBehavior.SetColor(hoverColor);
            rightTargetBehavior.SetColor(hoverColor);
            if (bimanualHoldTimer >= holdDuration)
            {
                float elapsed = Time.time - lastRespawnTime;
                Debug.Log($"Bimanual hold success! Time: {elapsed:F3}s");
                lastRespawnTime = Time.time;
                // Stop path recording and log for both arms
                string mode = (leftArm != null && rightArm != null && leftArm.useExternalInput && rightArm.useExternalInput && autoPlayer != null && autoPlayer.enableAutomation)
                    ? "AutoPlayer" : "Manual";
                if (leftArm != null)
                {
                    var stats = leftArm.StopPathRecording();
                    DistanceLogger.LogFullTrial(trialIndex, mode, "Left", stats.directDist, stats.actualDist, elapsed, stats.pathStr);
                }
                if (rightArm != null)
                {
                    var stats = rightArm.StopPathRecording();
                    DistanceLogger.LogFullTrial(trialIndex, mode, "Right", stats.directDist, stats.actualDist, elapsed, stats.pathStr);
                }
                trialIndex++;
                PlaceTargets();
                bimanualHoldTimer = 0f;
                leftTargetBehavior.SetColor(leftOriginalColor);
                rightTargetBehavior.SetColor(rightOriginalColor);
            }
        }
        else
        {
            bimanualHoldTimer = 0f;
            leftTargetBehavior.SetColor(leftOn ? hoverColor : leftOriginalColor);
            rightTargetBehavior.SetColor(rightOn ? hoverColor : rightOriginalColor);
        }
    }

    void OnDestroy()
    {
        DistanceLogger.Close();
    }
}