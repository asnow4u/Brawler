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

        // If Visual Studio Code is already open, do nothing
        if (Process.GetProcessesByName("Code").Length > 0)
            return;

        string projectPath = System.IO.Path.GetDirectoryName(Application.dataPath);

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "code",
                Arguments = $"\"{projectPath}\"",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(startInfo);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Failed to open VS Code. Make sure it's in your PATH. Error: " + e.Message);
        }
    }
}
