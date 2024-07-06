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
    [SerializeField] private List<SceneObject> sceneObjects = new List<SceneObject>();
    private UnityEngine.Object targetObject;
    private float launchInfluence;
    private float launchDamage;
    private float launchAngle;
    
    
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

        //Return to "none" when object is destroyed
        if (targetObject == null)
            targetObject = null;

        targetObject = EditorGUILayout.ObjectField(targetObject, typeof(SceneObject), true);
        launchInfluence = EditorGUILayout.Slider("Influence", launchInfluence, 0, 1f);
        launchDamage = EditorGUILayout.Slider("Damage: ", launchDamage, 0, 100f);
        launchAngle = EditorGUILayout.Slider("Angle: ", launchAngle, 0, 360f);

        GUILayout.Space(10f);
        if (GUILayout.Button("Apply Damage To SceneObjects"))
        {
            if (targetObject is SceneObject targetSceneObject)
            {
                if (targetSceneObject.TryGetComponent(out ITakeDamage damageHandler))
                {
                    damageHandler.HitByAttack(launchInfluence, targetSceneObject.transform.position, launchDamage, launchAngle);
                }
            }
        }

        GUILayout.EndVertical();
    }
}

#endif
