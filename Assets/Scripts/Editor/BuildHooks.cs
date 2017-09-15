using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BuildHooks {

    public static void PreBuild() {
    }

    public static void PostBuild() {
    }

    [MenuItem("Test/Upload Asset Bundles")]
    public static void UploadAssetBundles() {
        GameObject go = new GameObject();
        go.AddComponent<UploadAssetBundles>();
//    
//        EditorSceneManager.sceneOpened += OnSceneOpened;
//        EditorSceneManager.OpenScene("Assets/UploadAssetBundles.unity");
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode) {
        Debug.Log("Opened scene: "+scene.name);
        List<GameObject> gos = new List<GameObject>();
        scene.GetRootGameObjects(gos);
        bool found = false;
        for (int i = 0; i < gos.Count; ++i) {
            UploadAssetBundles upload = gos[i].GetComponent<UploadAssetBundles>();
            if (upload != null) {
                found = true;
                upload.Upload();
                return;
            }
        }
        if (!found) {
            Debug.LogError("Could not find UploadAssetBundles in any of the root objects");
        }
    }
}
