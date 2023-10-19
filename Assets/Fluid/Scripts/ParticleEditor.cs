using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ParticleManager))]
public class ParticleEditor : Editor
{
    SerializedProperty intProp;
    SerializedProperty floatProp;
    SerializedProperty floatProp2;
    SerializedProperty floatProp3;
    SerializedProperty floatProp4;
    SerializedProperty floatProp5;
    SerializedProperty floatProp6;
    SerializedProperty boolProp;
    SerializedProperty colorProp;
    SerializedProperty gradientProp;
    SerializedProperty computeShaderProp;
    SerializedProperty gameObjectProp;

    void OnEnable()
    {
        intProp = serializedObject.FindProperty("numParticles");
        floatProp = serializedObject.FindProperty("particleSize");
        floatProp2 = serializedObject.FindProperty("mostEffectiveDistance");
        floatProp3 = serializedObject.FindProperty("stability");
        floatProp4 = serializedObject.FindProperty("steepness");
        floatProp5 = serializedObject.FindProperty("frictionStrength");
        floatProp6 = serializedObject.FindProperty("mass");
        boolProp = serializedObject.FindProperty("useGradient");
        colorProp = serializedObject.FindProperty("particleColor");
        gradientProp = serializedObject.FindProperty("particleColorGradient");
        computeShaderProp = serializedObject.FindProperty("shader");
        gameObjectProp = serializedObject.FindProperty("particlePrefab");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Particle Properties", EditorStyles.boldLabel);
        if (!Application.isPlaying)
        {
            intProp.intValue = EditorGUILayout.IntSlider(intProp.displayName, intProp.intValue, 0, 10000);
            floatProp.floatValue = EditorGUILayout.Slider(floatProp.displayName, floatProp.floatValue, 0, 0.1f);
        }
        floatProp2.floatValue = EditorGUILayout.Slider(floatProp2.displayName, floatProp2.floatValue, 0, 10f);
        floatProp3.floatValue = EditorGUILayout.Slider(floatProp3.displayName, floatProp3.floatValue, 0, 10f);
        floatProp4.floatValue = EditorGUILayout.Slider(floatProp4.displayName, floatProp4.floatValue, 0, 10f);
        floatProp5.floatValue = EditorGUILayout.Slider(floatProp5.displayName, floatProp5.floatValue, 0, 0.5f);
        floatProp6.floatValue = EditorGUILayout.FloatField(floatProp6.displayName, floatProp6.floatValue);

        boolProp.boolValue = EditorGUILayout.Toggle(boolProp.displayName, boolProp.boolValue);

        if (boolProp.boolValue)
        {
            EditorGUILayout.PropertyField(gradientProp);
        }
        else
        {
            colorProp.colorValue = EditorGUILayout.ColorField(colorProp.displayName, colorProp.colorValue);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(computeShaderProp);
        EditorGUILayout.PropertyField(gameObjectProp);

        serializedObject.ApplyModifiedProperties();
    }
}
