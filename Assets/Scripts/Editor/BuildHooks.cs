﻿using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

public class BuildHooks {

    public static void PreBuild() {
    }

    public static void PostBuild(string builtProjectPath) {
        Debug.Log("BuildHooks.PostBuild builtProjectPath: "+builtProjectPath);

        var manifest = (TextAsset) Resources.Load("UnityCloudBuildManifest.json");
        Debug.Log("[PostBuild] Load manifest from json");
        if (manifest == null) {
            Debug.LogWarning("Couldn't get manifest from json");
            return;
        }

        Debug.Log("Got manifest from json");
        
        var manifestDict = MiniJSON.Json.Deserialize(manifest.text) as Dictionary<string,object>;

        if (manifestDict == null) {
            Debug.Log("Could not deserialize manifest json");
            return;
        }
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Manifest contents:");
        foreach (var kvp in manifestDict) {
            var value = (kvp.Value != null) ? kvp.Value.ToString() : "";
            sb.AppendLine(string.Format("Key: {0}, Value: {1}", kvp.Key, value));
        }
        Debug.Log(sb.ToString());

        string bundlesPath = manifestDict["assetBundles.localBundlesRelativePath"] as string;
        List<object> bundles = manifestDict["assetBundles.localBundles"] as List<object>;

        UploadAssetBundles(bundles, bundlesPath);
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
