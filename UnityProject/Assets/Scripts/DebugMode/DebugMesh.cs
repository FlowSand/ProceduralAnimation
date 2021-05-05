using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//功能：场景绘制网格线

public class DebugMesh : MonoBehaviour
{
    public bool OpenMesh;

    public int spacing = 20;

    public int width = 1400;
    public int height = 800;

    private void OnDrawGizmosSelected()
    {
        if (!OpenMesh) return;

        Gizmos.color = Color.gray;
        for (int x = 0; x <= width;)
        {
            Gizmos.DrawLine(new Vector3(x, 0, 0), new Vector3(x, height, 0));
            x += spacing;
        }
        for (int y = 0; y <= height;)
        {
            Gizmos.DrawLine(new Vector3(0, y, 0), new Vector3(width, y, 0));
            y += spacing;
        }
    }
}
