using UnityEngine;

using System.IO;
using RayAssets;
using System.Collections.Generic;

[UnityEditor.AssetImporters.ScriptedImporter(1, ".mdl0")]
public class ModelImporter : UnityEditor.AssetImporters.ScriptedImporter
{
    public float m_Scale = 1;

    public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
    {
        try
        {
            GameObject Parent = UnityWrangler.LoadModelObject(RayAssets.RayFile.ParseFile(ctx.assetPath), out var mats, out var textures, out var meshes);

            foreach (Texture2D texture in textures)
            {
                ctx.AddObjectToAsset(texture.name, texture);
            }

            foreach (Material mat in mats)
            {
                ctx.AddObjectToAsset(mat.name, mat);
            }

            foreach (UnityEngine.Mesh mesh in meshes)
            {
                ctx.AddObjectToAsset(mesh.name, mesh);
            }

            ctx.AddObjectToAsset("Model", Parent);  
        }
        catch
        {

        }
    }
}

[UnityEditor.AssetImporters.ScriptedImporter(1, ".anmpak")]
public class AnimationImporter : UnityEditor.AssetImporters.ScriptedImporter
{
    public float m_Scale = 1;

    public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
    {
        RayFile Pak = RayFile.ParseFile(ctx.assetPath);

        Dictionary<string, string> BoneDic = UnityWrangler.ParseBoneKey(RayFile.ParseFile(Pak.Buffers[0]));

        Debug.Log(Pak.Header.HeaderCount);

        for (int i = 1; i < Pak.Header.HeaderCount; i++)
        {
            AnimationClip Current = UnityWrangler.ParseAnimation(BoneDic, RayFile.ParseFile(Pak.Buffers[i]));

            ctx.AddObjectToAsset(Current.name, Current);
        }
    }
}