using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using Utility = AssetBundles.Utility;

// Should be able to specify a transport type override for each platform.
// Editor defaults to loading from file. Should easily be overridden by
// custom data to e.g. load from web.

public static class ABHelper {

    public static string GetPath(AssetBundleConfig cfg, string platform) {
        // editor
            // app.datapath
        // else
        // ios
            // if odr
                // odr://
            // if slicing
                // res://
            // else
                // http:// ??
        // else
            // http://

        switch (Application.platform) {
            case RuntimePlatform.OSXEditor:
                return Application.dataPath.Replace("Assets", "") + cfg.bundlesFolder + "/" + platform + "/";

            case RuntimePlatform.IPhonePlayer:
            default:
                return cfg.remoteURL;
        }
    }
}

public class LoadAssets : MonoBehaviour {
    
    public string assetBundleName;
    public string assetName;

    public Button btn;
    public Text txt;

    private void Start() {
        btn.onClick.AddListener(OnClick);
        txt.text = "";
    }

    private void OnClick() {
        StartCoroutine(Load());
    }

    IEnumerator Load() {
        DontDestroyOnLoad(gameObject);

        Config cfg = new Config {
            abConfig = new AssetBundleConfig {
                bundlesFolder = "AssetBundles",
                remoteURL = "https://s3-eu-west-1.amazonaws.com/mndrassetbundles/"
            }
        };
        
        string path = ABHelper.GetPath(cfg.abConfig, Utility.GetPlatformName()) + assetBundleName;

        txt.text += "Path: " + path + "\n";
        txt.text += "Platform: " + Application.platform + "\n";
        
// >> LOAD MANIFEST        
//        string pathManifest = pathAssetBundles + "iOS";
//        
//        var req = AssetBundle.LoadFromFileAsync(pathManifest);
//        yield return req;
//
//        AssetBundleManifest manifest = req.assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
// << LOAD MANIFEST        

        IAssetBundleJob job;
        
        switch (Application.platform) {
            case RuntimePlatform.OSXEditor:
                job = new AssetBundleLoadFromFile();
                break;
                
            case RuntimePlatform.IPhonePlayer:
            default:
                job = new AssetBundleLoadFromWeb();
                break;
        }

        txt.text += "Job: " + job.GetType() + "\n";
        
        yield return job.Load(path);

        if (job.error) {
            txt.text += "Error: " + job.errorMessage + "\n";
            Debug.LogWarning(job.errorMessage);
            yield break;
        }
        
        GameObject go = job.bundle.LoadAsset<GameObject>(assetName);
        Instantiate(go);
        Debug.Log(go);
    }
}

public interface IAssetBundleJob {
    AssetBundle bundle { get; }
    bool error { get; }
    string errorMessage { get; }
    IEnumerator Load(string path);
}

public class AssetBundleLoadFromFile : IAssetBundleJob {
    public AssetBundle bundle { get; private set; }
    public bool error { get; private set; }
    public string errorMessage { get; private set; }

    public IEnumerator Load(string path) {
        AssetBundleCreateRequest req = AssetBundle.LoadFromFileAsync(path);
        yield return req;

        if (req.assetBundle == null) {
            error = true;
            errorMessage = "Could not load asset bundle from "+path;
            yield break;
        }
        
        bundle = req.assetBundle;
    }
}

public class AssetBundleLoadFromWeb : IAssetBundleJob {
    public AssetBundle bundle { get; private set; }
    public bool error { get; private set; }
    public string errorMessage { get; private set; }

    public IEnumerator Load(string path) {
        UnityWebRequest www = UnityWebRequest.GetAssetBundle(path);
        yield return www.Send();
 
        if (www.isHttpError || www.isNetworkError) {
            error = true;
            errorMessage = www.error;
            yield break;
        }

        bundle = DownloadHandlerAssetBundle.GetContent(www);
    }
}
