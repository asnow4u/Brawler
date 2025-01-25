using Game.SceneObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

public class DebuggerEditorMenu : EditorWindow
{
    private enum DebuggerMenuType { Nothing, Damage }
    private DebuggerMenuType selectedType = DebuggerMenuType.Nothing;

    //Damage Menu
    //[SerializeField] private List<SceneObject> sceneObjects = new List<SceneObject>();
    //private UnityEngine.Object targetObject;
    //private float launchInfluence;
    //private float launchDamage;
    //private float launchAngle;

    [Serializable]
    private class SceneObjectData
    {
        public UnityEngine.Object TargetObject;
        public float LaunchInfluence;
        public float LaunchDamage;
        public float LaunchAngle;
    }

    [SerializeField] private List<SceneObjectData> sceneObjectsData = new List<SceneObjectData>();


    [MenuItem("Debug/DebugMenu")]
    static void DisplayDebuggerMenu()
    {
        DebuggerEditorMenu window = EditorWindow.GetWindow<DebuggerEditorMenu>();
    }    

    private void OnGUI()
    {
        if (Application.isPlaying)
        {
            DisplayMenuSelection();
            DisplaySelectedMenu();
        }
    }


    private void DisplayMenuSelection()
    {
        GUILayout.BeginVertical("Menus", "window");

        GUILayout.Label("Selected Menu: " + selectedType);
        GUILayout.Space(15f);

        GUILayout.BeginHorizontal();

        foreach (DebuggerMenuType menuType in Enum.GetValues(typeof(DebuggerMenuType)))
        {
            if (GUILayout.Button(menuType.ToString()))
            {
                if (selectedType == menuType)
                    selectedType = DebuggerMenuType.Nothing;
                else
                    selectedType = menuType;
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }


    private void DisplaySelectedMenu()
    {
       switch (selectedType)
        {
            case DebuggerMenuType.Damage:
                DisplayDamageDebugMenu();
                break;
        }
    }


    private void DisplayDamageDebugMenu()
    {
        GUILayout.BeginVertical("Damage", "window");
        GUILayout.Space(10f);

        if (GUILayout.Button("Add SceneObject"))
        {
            sceneObjectsData.Add(new SceneObjectData());
        }

        for (int i = 0; i < sceneObjectsData.Count; i++)
        {
            var data = sceneObjectsData[i];

            GUILayout.BeginVertical("SceneObject " + (i + 1), "window");
            GUILayout.Space(10f);

            data.TargetObject = EditorGUILayout.ObjectField(data.TargetObject, typeof(SceneObject), true);
            data.LaunchInfluence = EditorGUILayout.Slider("Influence", data.LaunchInfluence, 0, 1f);
            data.LaunchDamage = EditorGUILayout.Slider("Damage", data.LaunchDamage, 0, 100f);
            data.LaunchAngle = EditorGUILayout.Slider("Angle", data.LaunchAngle, 0, 360f);

            if (GUILayout.Button("Remove SceneObject"))
            {
                sceneObjectsData.RemoveAt(i);
                i--; // Adjust index after removal
            }

            GUILayout.EndVertical();
            GUILayout.Space(10f);
        }

        if (GUILayout.Button("Apply Damage To SceneObjects"))
        {
            foreach (var data in sceneObjectsData)
            {
                if (data.TargetObject is SceneObject targetSceneObject)
                {
                    if (targetSceneObject.TryGetComponent(out ITakeDamage damageHandler))
                        damageHandler.HitByAttack(data.LaunchInfluence, targetSceneObject.transform.position, data.LaunchDamage, data.LaunchAngle);
                }
            }
        }

        GUILayout.EndVertical();
    }
}

#endif
