using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CubeWireframe : MonoBehaviour
{
    public Material lineMaterial;
    public float lineWidth = 0.02f;

    private Vector3[] corners = new Vector3[8];
    private int[,] edges = new int[12, 2] {
        {0,1}, {1,2}, {2,3}, {3,0}, // bottom face
        {4,5}, {5,6}, {6,7}, {7,4}, // top face
        {0,4}, {1,5}, {2,6}, {3,7}  // vertical edges
    };

    void Start()
    {
        DrawWireframe();
    }

    void DrawWireframe()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        Vector3 center = box.center;
        Vector3 size = box.size * 0.5f;

        // Calculate the 8 corners in local space
        corners[0] = center + new Vector3(-size.x, -size.y, -size.z);
        corners[1] = center + new Vector3(size.x, -size.y, -size.z);
        corners[2] = center + new Vector3(size.x, -size.y, size.z);
        corners[3] = center + new Vector3(-size.x, -size.y, size.z);
        corners[4] = center + new Vector3(-size.x, size.y, -size.z);
        corners[5] = center + new Vector3(size.x, size.y, -size.z);
        corners[6] = center + new Vector3(size.x, size.y, size.z);
        corners[7] = center + new Vector3(-size.x, size.y, size.z);

        // Convert corners to world space
        for (int i = 0; i < 8; i++)
            corners[i] = transform.TransformPoint(corners[i]);

        // Draw the 12 edges
        for (int i = 0; i < 12; i++)
        {
            GameObject lineObj = new GameObject("Edge_" + i);
            lineObj.transform.parent = this.transform;
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 2;
            lr.useWorldSpace = true; // Draw in world space
            lr.SetPosition(0, corners[edges[i, 0]]);
            lr.SetPosition(1, corners[edges[i, 1]]);
        }
    }
}
