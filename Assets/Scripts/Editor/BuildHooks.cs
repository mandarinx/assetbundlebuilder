using UnityEditor;
using UnityEngine;

public class BuildHooks {

    public static void PreBuild() {
    }

    public static void PostBuild() {
    }

    [MenuItem("Test/Upload Asset Bundles")]
    public static void UploadAssetBundles() {
        Debug.Log("BuildHooks.UploadAssetBundles");
        GameObject go = new GameObject("UploadAssetBundles");
        Debug.Log("Create GameObject "+go.name);
        go.AddComponent<UploadAssetBundles>();
        Debug.Log("Added component UploadAssetBundles");
    }
}
