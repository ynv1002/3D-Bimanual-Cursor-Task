using UnityEngine;
using System.Collections;

public class AutoPlayer : MonoBehaviour
{
    [Header("Automation Toggle")]
    public bool enableAutomation = false;

    [Header("References")]
    private ArmController leftArm;
    private ArmController rightArm;
    public Transform leftTarget;
    public Transform rightTarget;
    public TargetBehavior leftTargetBehavior;
    public TargetBehavior rightTargetBehavior;

    [Header("Tuning Parameters")]
    public float moveSpeed = 1.0f; // Proportional gain for input
    public float arrivalTolerance = 0.01f; // How close is "at target"
    public float unreachableTimeout = 3.0f; // Seconds to try before logging unreachable

    private Coroutine leftRoutine;
    private Coroutine rightRoutine;

    void Awake()
    {
        var arms = FindObjectsByType<ArmController>(FindObjectsSortMode.None);
        foreach (var arm in arms)
        {
            if (arm.ikGoal == AvatarIKGoal.LeftHand)
                leftArm = arm;
            else if (arm.ikGoal == AvatarIKGoal.RightHand)
                rightArm = arm;
        }
    }

    void Update()
    {
        if (!enableAutomation)
        {
            // Release control if automation is off
            if (leftArm != null) leftArm.useExternalInput = false;
            if (rightArm != null) rightArm.useExternalInput = false;
            return;
        }
        else
        {
            if (leftArm != null) leftArm.useExternalInput = true;
            if (rightArm != null) rightArm.useExternalInput = true;
        }

        // Start routines if not running
        if (leftRoutine == null && leftArm != null && leftTarget != null && leftTargetBehavior != null)
            leftRoutine = StartCoroutine(MoveArmToTarget(leftArm, leftTarget, leftTargetBehavior, "Left"));
        if (rightRoutine == null && rightArm != null && rightTarget != null && rightTargetBehavior != null)
            rightRoutine = StartCoroutine(MoveArmToTarget(rightArm, rightTarget, rightTargetBehavior, "Right"));
    }

    IEnumerator MoveArmToTarget(ArmController arm, Transform target, TargetBehavior targetBehavior, string label)
    {
        float timer = 0f;
        bool reached = false;
        while (enableAutomation)
        {
            Vector3 cursorPos = arm.ikTarget.position;
            Vector3 targetPos = target.position;
            Vector3 toTarget = targetPos - cursorPos;
            float dist = toTarget.magnitude;
            // Debug.Log($"[{label}] Cursor-Target distance: {dist:F3}");

            // Proportional controller for input
            Vector3 moveDir = toTarget.normalized;
            // Project moveDir onto local axes
            Vector3 localMove = arm.ikTarget.parent != null ? arm.ikTarget.parent.InverseTransformDirection(moveDir) : moveDir;
            arm.externalInputX = localMove.x * moveSpeed;
            arm.externalInputY = localMove.z * moveSpeed; // Note: Y input is mapped to Z axis in ArmController
            arm.externalInputZ = localMove.y * moveSpeed;

            // Check if within detection threshold
            if (dist < targetBehavior.detectionThreshold)
            {
                if (!reached)
                {
                    reached = true;
                    timer = 0f;
                    Debug.Log($"[{label}] Arrived at target. Holding...");
                }
                timer += Time.deltaTime;
            }
            else
            {
                if (reached)
                {
                    reached = false;
                    timer = 0f;
                }
                timer += Time.deltaTime;
                if (timer > unreachableTimeout)
                {
                    Debug.LogWarning($"[{label}] Target unreachable after {unreachableTimeout} seconds. Closest distance: {dist:F3}");
                    break;
                }
            }
            yield return null;
        }
        // Release input after done
        arm.externalInputX = 0f;
        arm.externalInputY = 0f;
        arm.externalInputZ = 0f;
        if (label == "Left") leftRoutine = null;
        if (label == "Right") rightRoutine = null;
    }
} 