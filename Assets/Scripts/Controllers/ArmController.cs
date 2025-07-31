using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Drives a single arm of the Banana Man ragdoll using Animator IK.
/// Moves the IK target in local space based on three input axes (X, Y, Z),
/// while preserving a fixed rotation for stability.
/// Attach two instances to the Banana Man root: one for LeftHand and one for RightHand.
/// </summary>
[RequireComponent(typeof(Animator))]
public class ArmController : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("Name of the Input axis controlling horizontal movement (e.g. LeftArmX)")]
    public string axisXName = "LeftArmX"; // X axis input name
    [Tooltip("Name of the Input axis controlling depth movement (e.g. LeftArmY)")]
    public string axisYName = "LeftArmY"; // Y axis input name
    [Tooltip("Name of the Input axis controlling vertical movement (e.g. LeftArmZ)")]
    public string axisZName = "LeftArmZ"; // Z axis input name

    [Header("IK Settings")]
    [Tooltip("Which hand to drive via IK (LeftHand or RightHand)")]
    public AvatarIKGoal ikGoal = AvatarIKGoal.LeftHand; // Cursor for the hand
    [Tooltip("Transform of the IK target; the hand will follow this via IK.")]
    public Transform ikTarget; // IK target

    [Tooltip("Multiplier for movement speed (higher = faster arm motion)")]
    public float moveSpeed = 0.2f; //  Movement speed multiplier per second
    [Tooltip("Local direction along X axis for horizontal reach (usually Vector3.right)")]
    public Vector3 localReachX = Vector3.right; // Local direction along X axis for horizontal reach
    [Tooltip("Local direction along Z axis for depth reach (usually Vector3.forward)")]
    public Vector3 localReachZ = Vector3.forward; // Local direction along Z axis for depth reach
    [Tooltip("Local direction along Y axis for vertical reach (usually Vector3.up)")]
    public Vector3 localReachY = Vector3.up; // Local direction along Y axis for vertical reach

    [Header("Physical Reach")]
    [Tooltip("Shoulder bone transform (e.g., Left Arm or Right Arm)")]
    public Transform shoulderTransform;
    [Tooltip("Wrist (hand) bone transform (e.g., Left Hand or Right Hand)")]
    public Transform wristTransform;

    // Automation support
    [Header("Automation")]
    public bool useExternalInput = false;
    public float externalInputX = 0f;
    public float externalInputY = 0f;
    public float externalInputZ = 0f;

    // Internal state
    private Animator _anim;

    // Store input values to be used in FixedUpdate
    private float inputX, inputY, inputZ;
    private float maxArmLength;

    // Path recording
    private bool isRecordingPath = false;
    private List<(float time, Vector3 pos)> pathPoints = new List<(float, Vector3)>();
    private float directDistance = 0f;
    private Vector3 targetPosition;
    private float pathStartTime = 0f;
    private float pathEndTime = 0f;
    private int trialIndex = 0;

    public void StartPathRecording(Vector3 targetPos, int trialIdx)
    {
        pathPoints.Clear();
        isRecordingPath = true;
        targetPosition = targetPos;
        pathStartTime = Time.time;
        trialIndex = trialIdx;
        // Immediately add the current position as the first sample
        if (ikTarget != null)
            pathPoints.Add((Time.time, ikTarget.position));
        // Calculate direct distance from the first path point to the target
        if (pathPoints.Count > 0)
            directDistance = Vector3.Distance(pathPoints[0].pos, targetPosition);
        else
            directDistance = 0f;
    }

    public (float directDist, float actualDist, float holdTime, string pathStr, int trialIdx) StopPathRecording()
    {
        isRecordingPath = false;
        pathEndTime = Time.time;
        // Add the current position as the last sample if not already
        if (ikTarget != null && (pathPoints.Count == 0 || pathPoints[pathPoints.Count - 1].pos != ikTarget.position))
            pathPoints.Add((Time.time, ikTarget.position));
        float actualDist = 0f;
        for (int i = 1; i < pathPoints.Count; i++)
        {
            actualDist += Vector3.Distance(pathPoints[i - 1].pos, pathPoints[i].pos);
        }
        float holdTime = pathEndTime - pathStartTime;
        // Format path as string: [t,x,y,z];[t,x,y,z];...
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var pt in pathPoints)
        {
            sb.Append($"[{pt.time:F3},{pt.pos.x:F3},{pt.pos.y:F3},{pt.pos.z:F3}];");
        }
        return (directDistance, actualDist, holdTime, sb.ToString(), trialIndex);
    }

    /// <summary>
    /// Cache Animator and record the IK target's local rest position and rotation.
    /// </summary>
    void Awake()
    {
        _anim = GetComponent<Animator>(); 

        // Calculate max arm length from shoulder to wrist in rest pose
        if (shoulderTransform != null && wristTransform != null)
        {
            maxArmLength = Vector3.Distance(shoulderTransform.position, wristTransform.position);
        }
    }

    /// <summary>
    /// Read input axes, compute desired local position, and apply constant linear movement.
    /// </summary>
    void Update()
    {
        if (ikTarget == null) return;
        if (useExternalInput)
        {
            inputX = externalInputX;
            inputY = externalInputY;
            inputZ = externalInputZ;
        }
        else
        {
            // Read input axes and store for FixedUpdate
            inputX = Input.GetAxis(axisXName); // Horizontal
            inputY = Input.GetAxis(axisYName); // Depth
            inputZ = Input.GetAxis(axisZName); // Vertical
        }
    }

    /// <summary>
    /// Move the IK target at a fixed rate, synchronized with physics (50 Hz).
    /// </summary>
    void FixedUpdate()
    {
        if (ikTarget == null) return;

        // Path recording
        if (isRecordingPath)
        {
            pathPoints.Add((Time.time, ikTarget.position));
        }

        // --- MOVEMENT: Compute incremental movement in 3D ---
        Vector3 moveDelta = (localReachX.normalized * inputX +
                             localReachZ.normalized * inputY +
                             localReachY.normalized * inputZ) * moveSpeed * Time.fixedDeltaTime;
        Vector3 desiredLocal = ikTarget.localPosition + moveDelta;

        // Convert local to world position
        Vector3 desiredWorld = ikTarget.parent != null
            ? ikTarget.parent.TransformPoint(desiredLocal)
            : desiredLocal;

        // Clamp to Banana Man's physical reach
        if (shoulderTransform != null)
        {
            Vector3 shoulderPos = shoulderTransform.position;
            Vector3 direction = (desiredWorld - shoulderPos).normalized;
            float distance = Mathf.Min(Vector3.Distance(desiredWorld, shoulderPos), maxArmLength);
            desiredWorld = shoulderPos + direction * distance;
        }

        // Convert back to local position for the IK target
        Vector3 clampedLocal = ikTarget.parent != null
            ? ikTarget.parent.InverseTransformPoint(desiredWorld)
            : desiredWorld;

        ikTarget.localPosition = clampedLocal;

    }

    /// <summary>
    /// Apply IK position and rotation on the hand bone each frame.
    /// </summary>
    void OnAnimatorIK(int layerIndex)
    {
        if (ikTarget == null) return;

        // Apply full IK weights
        _anim.SetIKPositionWeight(ikGoal, 1f);
        _anim.SetIKRotationWeight(ikGoal, 1f);

        // Drive hand to follow target
        _anim.SetIKPosition(ikGoal, ikTarget.position);
        _anim.SetIKRotation(ikGoal, ikTarget.rotation);
    }
}