using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayBone2 : MonoBehaviour {

    public bool DisplayAxes = true;
    public bool DisplayNames = true;

    public float scalef;
    public float voffset;

    void DrawBones( Transform t ) {

        foreach ( Transform child in t ) {
            Gizmos.color = Color.black;
            Gizmos.DrawLine( t.position, child.position );
            Gizmos.DrawSphere( t.position, 0.0025f );

            Vector3 position = child.position;
            Quaternion rotation = child.rotation;

            child.rotation = Quaternion.Euler(0, 0, 0);
            if ( DisplayNames == true ) {
#if UNITY_EDITOR
                Gizmos.color = Color.black;
                UnityEditor.Handles.Label( t.position + ( position - t.position ) / 2.0f, child.name );
#endif
            }
            if ( DisplayAxes == true ) {
                float len = 0.005f * scalef;
                Vector3 loxalX = new Vector3( len, 0, 0 );
                Vector3 loxalY = new Vector3( 0, len, 0 );
                Vector3 loxalZ = new Vector3( 0, 0, len );
                loxalX = rotation * loxalX;
                loxalY = rotation * loxalY;
                loxalZ = rotation * loxalZ;

                Gizmos.color = Color.red;
                Gizmos.DrawLine( position, position + loxalX );

                Gizmos.color = Color.green;
                Gizmos.DrawLine( position, position + loxalY );

                Gizmos.color = Color.blue;
                Gizmos.DrawLine( position, position + loxalZ );

            }

            DrawBones( child );
        }

    }

    void OnDrawGizmos() {
        DrawBones( transform );
    }
}