using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine.CloudBuild;

#if (!UNITY_CLOUD_BUILD)
namespace UnityEngine.CloudBuild {
    public class BuildManifestObject : ScriptableObject {

        // Tries to get a manifest value - returns true if key was found and
        // could be cast to type T, false otherwise.
        public bool TryGetValue<T>(string key, out T result) {
            result = default(T);
            return true;
        }

        // Retrieve a manifest value or throw an exception if the given key
        // isn't found.
        public T GetValue<T>(string key) { return default(T); }

        // Sets the value for a given key.
        public void SetValue(string key, object value) {}

        // Copy values from a dictionary. ToString() will be called on
        // dictionary values before being stored.
        public void SetValues(Dictionary<string, object> sourceDict) {}

        // Remove all key/value pairs
        public void ClearValues() {}

        // Returns a Dictionary that represents the current BuildManifestObject
        public Dictionary<string, object> ToDictionary() { return null; }

        // Returns a JSON formatted string that represents the current
        // BuildManifestObject
        public string ToJson() { return ""; }

        // Returns an INI formatted string that represents the current
        // BuildManifestObject
        public override string ToString() { return ""; }
    }
}
#endif

public class BuildHooks {

    public static void PreBuildiOSPVRTC(BuildManifestObject manifest) {
        Debug.Log("PreBuild iOS PVRTC");
//        MobileTextureSubtarget subtarget = MobileTextureSubtarget.PVRTC;
//        int prefix = GetBuildPrefix(subtarget);
        string buildNumber = manifest.GetValue<string>("buildNumber");
        Debug.Log("Build number: "+buildNumber);
//        SetBundleVersion(buildNumber, prefix);
//        SetAndroidBuildSubtarget(subtarget);
        SetVersion(buildNumber);
    }

    public static void PostBuild(string builtProjectPath) {
        Debug.Log("BuildHooks.PostBuild builtProjectPath: "+builtProjectPath);

        TextAsset json = (TextAsset) Resources.Load("UnityCloudBuildManifest.json");
        Debug.Log("[PostBuild] Load manifest from json");
        if (json == null) {
            Debug.LogWarning("Couldn't get manifest from json");
            return;
        }

        Debug.Log("Got json from json");
        
        Dictionary<string, object> manifest = MiniJSON.Json.Deserialize(json.text) as Dictionary<string,object>;

        if (manifest == null) {
            Debug.Log("Could not deserialize manifest json");
            return;
        }
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Manifest contents:");
        foreach (var kvp in manifest) {
            var value = (kvp.Value != null) ? kvp.Value.ToString() : "";
            sb.AppendLine(string.Format("Key: {0}, Value: {1}", kvp.Key, value));
        }
        Debug.Log(sb.ToString());

        string bundlesPath = manifest["assetBundles.localBundlesRelativePath"] as string;
        List<object> bundles = manifest["assetBundles.localBundles"] as List<object>;

        UploadAssetBundles(bundles, bundlesPath);
    }

    private static int GetBuildPrefix(MobileTextureSubtarget texture) {
        switch ((int) texture) {
            case 0: // Generic. Not in use
                return 0;
            case 1: // DXT
                return 40010;
            case 2: // PVRTC
                return 50010;
            case 3: // ATC
                return 30010;
            case 4: // ETC
                return 10010;
            case 5: // ETC2
                return 20010;
            default: // ASTC
                return 60010;
        }
    }
    
    private static void SetBundleVersion(string buildNumber, int prefix) {
        PlayerSettings.Android.bundleVersionCode = (prefix * 100) + int.Parse(buildNumber);
    }
    
    private static void SetAndroidBuildSubtarget(MobileTextureSubtarget texture) {
        EditorUserBuildSettings.androidBuildSubtarget = texture;
    }

    private static void SetVersion(string version) {
        PlayerSettings.bundleVersion = version;
    }
    
    public static void UploadAssetBundles(List<object> bundles, string path) {
        Debug.Log("BuildHooks.UploadAssetBundles from "+path);
        
        string[] bundleFiles = new string[bundles.Count * 2];
        string fullPath = Directory.GetCurrentDirectory() + "/" + path + "/";
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("AssetBundles:");
        for (int i = 0; i < bundles.Count; ++i) {
            sb.AppendLine(bundles[i] as string);
            bundleFiles[i*2] = fullPath + bundles[i];
            bundleFiles[(i*2)+1] = fullPath + bundles[i] + ".manifest";
        }
        Debug.Log(sb.ToString());

        Debug.Log("Upload bundles to https://buildhook-mndr.herokuapp.com/upload/");
        string response = UploadFiles("https://buildhook-mndr.herokuapp.com/upload/", bundleFiles);
        Debug.Log("Response: " + response);
    }
    
    ///<summary>Uploads form fields, and n files to REST endpoint</summary>
    ///<param name="url">URL to upload data to</param>
    ///<param name="files">Array containing string paths to files to upload</param>
    ///<param name="formFields">NameValueCollection containing all non-file fields from the form, default is null if no other fields provided</param>
    public static string UploadFiles(string url, string[] files, NameValueCollection formFields = null) {
        
        string boundaryStr = "----------------------------" + DateTime.Now.Ticks.ToString("x");
        
        HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
            request.ContentType = "multipart/form-data; boundary=" + boundaryStr;
            request.Method = "POST";
            request.KeepAlive = true;
        
        // Stream contains request as you build it
        Stream memStream = new MemoryStream();
        
        byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundaryStr + "\r\n");
        byte[] endBoundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundaryStr + "--");
        
        //this is a multipart/form-data template for all non-file fields in your form data
        string formdataTemplate = "\r\n--" + 
            boundaryStr + "\r\n"+
            "Content-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}";
        
        if (formFields != null) {
            // Utilizing the template, write each field to the request, 
            // convert this data to bytes, and store temporarily in memStream
            foreach (string key in formFields.Keys) {
                string formItem = string.Format(formdataTemplate, key, formFields[key]);
                byte[] formItemBytes = Encoding.UTF8.GetBytes(formItem);
                memStream.Write(formItemBytes, 0, formItemBytes.Length);
            }
        }
        
        // This is a multipart/form-data template for all fields containing files in your form data
        string headerTemplate = 
            "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n" +
            "Content-Type: application/octet-stream\r\n\r\n";
            
        // Using the template write all files in your files[] input array to the request.
        // Each file is indexed using its array position.
        for (int i = 0; i < files.Length; i++) {
            
            Debug.Log("File: "+files[i]);
            
            memStream.Write(boundaryBytes, 0, boundaryBytes.Length);
            string headerStr = string.Format(headerTemplate, "file"+i.ToString("000"), files[i]);
            byte[] headerBytes = Encoding.UTF8.GetBytes(headerStr);
        
            memStream.Write(headerBytes, 0, headerBytes.Length);
            
            // Convert files to byte arrays for upload
            using (FileStream fileStream = new FileStream(files[i], FileMode.Open, FileAccess.Read)) {
                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0) {
                    memStream.Write(buffer, 0, bytesRead);
                }
            }
        }
        
        ServicePointManager.ServerCertificateValidationCallback = (srvPoint, certificate, chain, errors) => true;
        
        memStream.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);
        request.ContentLength = memStream.Length;
        
        // Write the data through to the request
        using (Stream requestStream = request.GetRequestStream()) {
            memStream.Position = 0;
            byte[] tempBuffer = new byte[memStream.Length];
            memStream.Read(tempBuffer, 0, tempBuffer.Length);
            memStream.Close();
            requestStream.Write(tempBuffer, 0, tempBuffer.Length);
        }
        
        // Capture the response from the server
        using (WebResponse response = request.GetResponse()) {
            Stream responseStream = response.GetResponseStream();
            return responseStream == null 
                ? "ResponseStream is null" 
                : new StreamReader(responseStream).ReadToEnd();
        }
    }
}
