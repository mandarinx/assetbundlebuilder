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
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorSceneManager.OpenScene("Assets/UploadAssetBundles.unity");
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode) {
        UploadAssetBundles upload = Object.FindObjectOfType<UploadAssetBundles>();
        if (upload == null) {
            Debug.LogError("Could not load MonoBehaviour UploadAssetBundles");
            return;
        }
        upload.Upload();
    }
}
