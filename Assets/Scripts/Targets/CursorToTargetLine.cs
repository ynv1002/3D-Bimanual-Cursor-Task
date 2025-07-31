using UnityEngine;

public class CursorToTargetLine : MonoBehaviour
{
    [Header("Left Side")]
    public Transform leftCursor;
    public Transform leftTarget;
    [Header("Right Side")]
    public Transform rightCursor;
    public Transform rightTarget;

    [Header("Line Settings")]
    public float lineWidth = 0.01f;
    public Material lineMaterial;

    private LineRenderer leftLine;
    private LineRenderer rightLine;

    void Start()
    {
        // Create and configure left line
        GameObject leftLineObj = new GameObject("LeftCursorToTargetLine");
        leftLineObj.transform.parent = this.transform;
        leftLine = leftLineObj.AddComponent<LineRenderer>();
        leftLine.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        leftLine.startWidth = lineWidth;
        leftLine.endWidth = lineWidth;
        leftLine.positionCount = 2;
        leftLine.useWorldSpace = true;
        leftLine.enabled = true;

        // Create and configure right line
        GameObject rightLineObj = new GameObject("RightCursorToTargetLine");
        rightLineObj.transform.parent = this.transform;
        rightLine = rightLineObj.AddComponent<LineRenderer>();
        rightLine.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        rightLine.startWidth = lineWidth;
        rightLine.endWidth = lineWidth;
        rightLine.positionCount = 2;
        rightLine.useWorldSpace = true;
        rightLine.enabled = true;
    }

    void Update()
    {
        // Update left line
        if (leftCursor != null && leftTarget != null)
        {
            leftLine.SetPosition(0, leftCursor.position);
            leftLine.SetPosition(1, leftTarget.position);
            leftLine.enabled = true;
        }
        else
        {
            leftLine.enabled = false;
        }

        // Update right line
        if (rightCursor != null && rightTarget != null)
        {
            rightLine.SetPosition(0, rightCursor.position);
            rightLine.SetPosition(1, rightTarget.position);
            rightLine.enabled = true;
        }
        else
        {
            rightLine.enabled = false;
        }
    }

    void OnValidate()
    {
        // Update line width in editor
        if (leftLine != null)
        {
            leftLine.startWidth = lineWidth;
            leftLine.endWidth = lineWidth;
        }
        if (rightLine != null)
        {
            rightLine.startWidth = lineWidth;
            rightLine.endWidth = lineWidth;
        }
    }
}
