using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using Utility = AssetBundles.Utility;

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
            default:
            case RuntimePlatform.IPhonePlayer:
                return cfg.remoteURL;
        }
    }
}

public class LoadAssets : MonoBehaviour {
    
    public string assetBundleName;
    public string assetName;

    IEnumerator Start() {
        DontDestroyOnLoad(gameObject);

        Config cfg = new Config {
            abConfig = new AssetBundleConfig {
                bundlesFolder = "AssetBundles",
                remoteURL = "https://s3-eu-west-1.amazonaws.com/mndrassetbundles/"
            }
        };
        
        string path = ABHelper.GetPath(cfg.abConfig, Utility.GetPlatformName()) + assetBundleName;

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
                
            default:
            case RuntimePlatform.IPhonePlayer:
                job = new AssetBundleLoadFromWeb();
                break;
        }
        
        yield return job.Load(path);

        if (job.error) {
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

        bundle = req.assetBundle;
        
        if (bundle == null) {
            error = true;
            errorMessage = "Could not load asset bundle from "+path;
        }
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
