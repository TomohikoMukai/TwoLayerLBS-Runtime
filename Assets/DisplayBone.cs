using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DisplayBone : MonoBehaviour
{

    public bool DisplayAxes = true;
    public bool DisplayNames = true;
    public Matrix4x4[] jointPose;

    public float scalef;
    public float voffset;
    void Start()
    {
    }

    static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        Quaternion q = new Quaternion();
        q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
        q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
        q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
        q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
        q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
        q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
        q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
        return q;
    }

    public static void DrawThickLine(Vector3 start, Vector3 end, float thickness, Color color)
    {
    #if !UNITY_EDITOR
        Gizmos.color = color;
        Gizmos.DrawLine(start, end);
        return;
    #endif

        Camera c = Camera.current;
        if (c == null)
        {
            return;
        }
        if (c.clearFlags == CameraClearFlags.Depth || c.clearFlags == CameraClearFlags.Nothing)
        {
            return;
        }
        // Only draw the line when it is the closest thing to the camera
        // (Remove the Z-test code and other objects will not occlude the line.)
        var prevZTest = Handles.zTest;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        Handles.color = color;
        Handles.DrawAAPolyLine(thickness * 10, new Vector3[] { start, end });
        Handles.zTest = prevZTest;
    }

    void DrawBones(Transform t, float size = 1.0f)
    {
        foreach (Transform child in t)
        {
            int id = System.Int32.Parse(child.name.Substring(child.name.Length - 4)) - 1;
            Vector3 position = new Vector3(jointPose[id][0, 3], jointPose[id][1, 3], jointPose[id][2, 3]);
            //child.position = position;
            Quaternion rotation = QuaternionFromMatrix(jointPose[id]);
            //Quaternion rotation = Quaternion.identity;
            Gizmos.color = Color.black;
            //Gizmos.DrawLine(t.position, position);
            //Gizmos.DrawSphere(t.position, 0.0025f);
            child.rotation = Quaternion.Euler(0, 0, 0);
            if (DisplayNames == true)
            {
#if UNITY_EDITOR
                Gizmos.color = Color.black;
                UnityEditor.Handles.Label(t.position + (position - t.position) / 2.0f, child.name);
#endif
            }
            if (DisplayAxes == true)
            {
                float len = 0.005f * scalef;
                Vector3 axisX = new Vector3(len, 0, 0);
                Vector3 axisY = new Vector3(0, len, 0);
                Vector3 axisZ = new Vector3(0, 0, len);
                axisX = rotation * axisX;
                axisY = rotation * axisY;
                axisZ = rotation * axisZ;
                DrawThickLine(position, position + axisX * size, size, Color.red);
                DrawThickLine(position, position + axisY * size, size, Color.green);
                DrawThickLine(position, position + axisZ * size, size, Color.blue);
            }
            DrawBones(child);
        }
    }

    void OnDrawGizmos()
    {
        DrawBones(transform, 1.5f);
    }
}
