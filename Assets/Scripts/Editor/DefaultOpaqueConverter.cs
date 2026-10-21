using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;


// Vibecoded af
// Quick import setting for models, basically right click on anything from sketchfab and it
// generates the proper materials + prefab with the rigth shader attached to it :)
static class DefaultOpaqueConverter
{
    const string sShaderPath = "Assets/Rendering/DefaultOpaque.shadergraph";
    static readonly string[] aAlbedoSuffixes = { "albedo", "basecolor", "basecolour", "diffuse", "color", "colour" };
    static readonly string[] aNormalSuffixes = { "normal", "normalmap", "nrm", "nor" };

    [MenuItem("Assets/Make Default Opaque Copy", true)]
    static bool CanConvert() => Selection.objects.Any(IsModel);

    [MenuItem("Assets/Make Default Opaque Copy")]
    static void ConvertSelection()
    {
        foreach(Object model in Selection.objects.Where(IsModel).ToArray())
        {
            Convert(AssetDatabase.GetAssetPath(model));
        }
    }

    static bool IsModel(Object asset) => AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(asset)) is ModelImporter;

    static void Convert(string sModelPath)
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(sShaderPath);
        if(shader == null)
        {
            Debug.LogError($"No shader at {sShaderPath}");
            return;
        }

        string sDir = Path.GetDirectoryName(sModelPath).Replace('\\', '/');
        string sRoot = string.Equals(Path.GetFileName(sDir), "source", StringComparison.OrdinalIgnoreCase) ? Path.GetDirectoryName(sDir).Replace('\\', '/') : sDir;
        string sMaterialDir = sRoot + "/Materials";
        string sPrefabPath = sRoot + "/" + Path.GetFileNameWithoutExtension(sModelPath) + ".prefab";
        string[] aTextures = AssetDatabase.FindAssets("t:Texture2D", new[] { sRoot }).Select(AssetDatabase.GUIDToAssetPath).ToArray();

        if(!AssetDatabase.IsValidFolder(sMaterialDir))
        {
            AssetDatabase.CreateFolder(sRoot, "Materials");
        }

        var converted = new Dictionary<Material, Material>();
        Material Opaque(Material source)
        {
            if(source == null || source.renderQueue > (int)RenderQueue.GeometryLast)
            {
                return source;
            }

            if(converted.TryGetValue(source, out Material done))
            {
                return done;
            }

            string sPath = $"{sMaterialDir}/{string.Concat(source.name.Split(Path.GetInvalidFileNameChars()))}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(sPath);
            if(material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, sPath);
            }
            material.shader = shader;

            Texture albedo = FindTexture(aTextures, source.name, aAlbedoSuffixes) ?? source.mainTexture;
            Texture normal = FindTexture(aTextures, source.name, aNormalSuffixes) ?? (source.HasTexture("_BumpMap") ? source.GetTexture("_BumpMap") : null);

            if(albedo != null)
            {
                material.SetTexture("_MainTex", albedo);
            }
            else if(material.GetTexture("_MainTex") == null)
            {
                Debug.LogWarning($"{source.name}: no albedo found, assign it by hand", material);
            }

            if(normal != null)
            {
                MarkAsNormalMap(normal);
                material.SetTexture("_NormalMap", normal);
            }

            EditorUtility.SetDirty(material);
            return converted[source] = material;
        }

        bool bExists = AssetDatabase.LoadAssetAtPath<GameObject>(sPrefabPath) != null;
        GameObject root = bExists ? PrefabUtility.LoadPrefabContents(sPrefabPath) : (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(sModelPath));

        foreach(Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Renderer original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(renderer);
            if(original != null)
            {
                renderer.sharedMaterials = original.sharedMaterials.Select(Opaque).ToArray();
            }
        }

        PrefabUtility.SaveAsPrefabAsset(root, sPrefabPath);
        if(bExists)
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        else
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(sPrefabPath);
        Debug.Log($"{sPrefabPath}: {converted.Count} materials on DefaultOpaque", Selection.activeObject);
    }

    static Texture FindTexture(string[] aPaths, string sMaterial, string[] aSuffixes)
    {
        string sKey = Key(sMaterial);
        string sPath = aPaths.FirstOrDefault(p => aSuffixes.Any(s => Key(Path.GetFileNameWithoutExtension(p)) == sKey + s));
        return sPath != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(sPath) : null;
    }

    static string Key(string sName) => Regex.Replace(sName.ToLowerInvariant(), "[^a-z0-9]", "");

    static void MarkAsNormalMap(Texture texture)
    {
        if(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) is TextureImporter { textureType: not TextureImporterType.NormalMap } importer)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
    }
}
