using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SitPoseSetter : MonoBehaviour
{
    // Flexion angles (in degrees)
    private const float hipFlexion = 90f;
    private const float kneeFlexion = -90f;

    // Seat anchor for positioning
    public Transform seatAnchor;

    // cached bone transforms + bind rotations
    private Transform  _leftUpper, _rightUpper, _leftLower, _rightLower;
    private Quaternion _leftUpperBind, _rightUpperBind, _leftLowerBind, _rightLowerBind;

    void Awake()
    {
        var anim = GetComponent<Animator>();
        _leftUpper   = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        _rightUpper  = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        _leftLower   = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        _rightLower  = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);

        // stash their original local rotations
        if (_leftUpper  != null) _leftUpperBind  = _leftUpper.localRotation;
        if (_rightUpper != null) _rightUpperBind = _rightUpper.localRotation;
        if (_leftLower  != null) _leftLowerBind  = _leftLower.localRotation;
        if (_rightLower != null) _rightLowerBind = _rightLower.localRotation;
    }

    void LateUpdate()
    {
        // 1) snap your root pivot (hips) to the chair's seat Y
        if (seatAnchor != null)
        {
            var p = transform.position;
            transform.position = new Vector3(p.x, seatAnchor.position.y, p.z);
        }

        // 2) apply flexion on top of the bind-pose
        // Apply sitting pose by rotating the upper and lower leg bones.
        // The Z axis is used due to the model's import orientation (root rotated 90° in Y).
        // Negative sign ensures the legs bend forward and down in a natural sitting pose.
        if (_leftUpper  != null) _leftUpper.localRotation  = _leftUpperBind  * Quaternion.Euler(0f, 0f, -hipFlexion); // Left thigh bends forward
        if (_rightUpper != null) _rightUpper.localRotation = _rightUpperBind * Quaternion.Euler(0f, 0f, -hipFlexion); // Right thigh bends forward

        if (_leftLower  != null) _leftLower.localRotation  = _leftLowerBind  * Quaternion.Euler(0f, 0f, -kneeFlexion); // Left calf bends down
        if (_rightLower != null) _rightLower.localRotation = _rightLowerBind * Quaternion.Euler(0f, 0f, -kneeFlexion); // Right calf bends down
    }
}