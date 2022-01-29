using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SimpleAnimationSpeedModifier))]
public class SimpleAnimationSpeedModifierEditor : Editor
{
    override public void OnInspectorGUI()
    {
        SimpleAnimationSpeedModifier modifier = (SimpleAnimationSpeedModifier)target;
        if (GUILayout.Button("Reset Character Position"))
        {
            modifier.ResetCharacterPosition();
        }

        DrawDefaultInspector();

        if (GUILayout.Button("Play Animation"))
        {
            modifier.PlayAnimation();
        }
        if (GUILayout.Button("Play Modified Animation"))
        {
            modifier.PlayModifiedAnimation();
        }
        if (GUILayout.Button("Play Adjusted Modified Animation"))
        {
            modifier.PlayAdjustedModifiedAnimation();
        }        


    }
}