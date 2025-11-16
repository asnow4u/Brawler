using System.Diagnostics;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AutoOpenIDE
{
    private const string SESSIONKEY = "AutoOpenIDE_Ran";

    static AutoOpenIDE()
    {
        // Delay so Unity finishes importing first
        EditorApplication.update += OnEditorStartup;
    }

    private static void OnEditorStartup()
    {
        EditorApplication.update -= OnEditorStartup;

        //NOTE: SessionStates only reset when Unity is closed. This ensures it only runs once per session. 
        if (SessionState.GetBool(SESSIONKEY, false))
            return;

        SessionState.SetBool(SESSIONKEY, true);

        string solutionPath = $"{Application.dataPath}/../{PlayerSettings.productName}.sln";

        // Fallback: If the standard name isn't found, just search for .sln files
        if (!System.IO.File.Exists(solutionPath))
        {
            var dir = System.IO.Path.GetDirectoryName(Application.dataPath);
            var files = System.IO.Directory.GetFiles(dir, "*.sln");
            if (files.Length > 0)
                solutionPath = files[0];
            else
                return; // No .sln? Nothing to open.
        }

        Process.Start(solutionPath);
    }
}
