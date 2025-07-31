using UnityEngine;

/// <summary>
/// Handles hover detection and random repositioning for a single target sphere.
/// Tracks a specified hand transform, requires a brief hold time to trigger relocation.
/// </summary>
public class TargetBehavior : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Transform of the hand to track (e.g., LeftIK_Target or RightIK_Target)")]
    public Transform handTransform;

    [Tooltip("Distance (in world units) at which the hand is considered overlapping the target.")]
    public float detectionThreshold = 0.2f;

    [Tooltip("Is this the left target?")]
    public bool isLeftTarget = false;

    /// <summary>
    /// Event fired when this target is successfully hovered for holdTime.
    /// </summary>

    // Internal hover state
    [HideInInspector]
    public bool isHovered = false;

    private Renderer _renderer;
    private Color _currentColor;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
            _currentColor = _renderer.material.color;
    }

    void Update()
    {
        if (handTransform == null)
            return;

        float dist = Vector3.Distance(transform.position, handTransform.position);
        bool hovering = dist < detectionThreshold;
        if (hovering != isHovered)
        {
            isHovered = hovering;
        }
    }

    public void SetColor(Color c)
    {
        if (_renderer != null)
        {
            _renderer.material.color = c;
            _currentColor = c;
            }
        }

    public Color GetCurrentColor() => _currentColor;

}