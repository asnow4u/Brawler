using UnityEditor;
using UnityEngine;

// Lone Wolf (wolf-animation pipeline, 2026-11-04): keep root motion exactly flat before launch.
// Unity builds the root-motion curves (MotionT / MotionQ) of a clip with a motion node with smoothed tangents, so a
// curve that is flat up to the launch frame and then rises (Wolf_UpTilt: Root height 0 on frames 1-13, +0.129 m on
// frame 14) undershoots between the last flat key and launch (-9 mm at frame 12.75). This sets linear tangents on the
// Root / MotionT keys up to the launch frame, so the held part can't dip; keys after launch are untouched.
// Entries: Wolf_UpTilt (rise from f14), Wolf_ForwardTilt_Lunge (travel along +X from f20, added 2026-11-06),
// Wolf_ForwardAir_2 (was Wolf_DownAir; dive along (+X, -Y) from f13, added 2026-11-06, renamed with the file).
// Source copy: C:\LoneWolfAIArt\export\WolfRootMotionFlatHold.cs
public class WolfRootMotionFlatHold : AssetPostprocessor
{
    // source FBX file name -> last flat frame (clip frame, 1-based, 60 fps)
    static readonly System.Collections.Generic.Dictionary<string, int> LaunchFrame =
        new System.Collections.Generic.Dictionary<string, int> { { "Wolf_UpTilt.fbx", 13 }, { "Wolf_ForwardTilt_Lunge.fbx", 19 }, { "Wolf_ForwardAir_2.fbx", 12 } };

    public override uint GetVersion() { return 2; }

    void OnPostprocessAnimation(GameObject root, AnimationClip clip)
    {
        if (!assetPath.StartsWith("Assets/Animation/Wolf/")) return;   // any wolf clip folder (Source/ was reorganised 2026-11-04)
        int launch;
        if (!LaunchFrame.TryGetValue(System.IO.Path.GetFileName(assetPath), out launch)) return;
        float tEnd = (launch - 1) / clip.frameRate + 1e-4f;
        foreach (var b in AnimationUtility.GetCurveBindings(clip))
        {
            bool rootPos = b.path == "Wolf_Rig/Root" && b.propertyName.StartsWith("m_LocalPosition");
            if (!rootPos && !b.propertyName.StartsWith("MotionT")) continue;
            var c = AnimationUtility.GetEditorCurve(clip, b);
            for (int k = 0; k < c.keys.Length; k++)
            {
                if (c.keys[k].time > tEnd) break;
                AnimationUtility.SetKeyLeftTangentMode(c, k, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(c, k, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(clip, b, c);
        }
    }
}
