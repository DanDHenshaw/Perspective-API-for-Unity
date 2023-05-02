using System;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

/// <summary>
/// PerspectiveAPI allows for toxicity string detection.
/// </summary>
public class PerspectiveAPI
{
    #region Menu Items

    [MenuItem("Tools/PerspectiveAPI/Data/Create", priority = 1)]
    static void Create()
    {
        if (Load())
        {
            Debug.LogWarning("Data already exists, won't create a new one.");
            return;
        }

        string path = "Assets/PerspectiveAPI";
        if (!AssetDatabase.IsValidFolder("Assets/PerspectiveAPI"))
        {
            string guid = AssetDatabase.CreateFolder("Assets", "PerspectiveAPI");
            path = AssetDatabase.GUIDToAssetPath(guid);
        }

        try
        {
            //Creates asset
            var asset = ScriptableObject.CreateInstance<PerspectiveData>();
            AssetDatabase.CreateAsset(asset, path + $"/{fileName}.asset");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        AssetDatabase.SaveAssets();
    }

    private const string fileName = "PerspectiveData";
    static PerspectiveData Load()
    {
        var result = EditorGUIUtility.Load($"PerspectiveAPI/{fileName}.asset") as PerspectiveData;
        if (result != null)
            return result;

        var guids = UnityEditor.AssetDatabase.FindAssets("t:" + nameof(PerspectiveData));
        if (guids.Length == 0)
            return null;

        return AssetDatabase.LoadAssetAtPath<PerspectiveData>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }
    #endregion

    /// <summary>
    /// Checks toxicity of string msg and returns either true (toxic) or false (not toxic).
    /// </summary>
    public static bool CheckToxic(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg))
        {
            Debug.LogError("Empty string!");
            return false;
        }

        var guids = UnityEditor.AssetDatabase.FindAssets("t:" + nameof(PerspectiveData));
        if (guids.Length == 0)
        {
            Debug.LogError("Cannot find PerspectiveData.asset");
            return false;
        }

        var asset = AssetDatabase.LoadAssetAtPath<PerspectiveData>(AssetDatabase.GUIDToAssetPath(guids[0]));

        return GetData(asset.API, msg) > asset.Toxicity;
    }

    #region JSON
    static float GetData(string API_Key, string msg)
    {
        var url = "https://commentanalyzer.googleapis.com/v1alpha1/comments:analyze?key=" + API_Key;

        var httpRequest = (HttpWebRequest)WebRequest.Create(url);
        httpRequest.Method = "POST";

        httpRequest.ContentType = "application/json";

        var data = "{comment: {text: \"" + msg + "\"}, languages: [\"en\"], requestedAttributes: {TOXICITY:{}} }";

        using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
        {
            streamWriter.Write(data);
        }

        PerspectiveResult pResult = new PerspectiveResult();
        var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
        {
            var result = streamReader.ReadToEnd();
            pResult = JsonUtility.FromJson<PerspectiveResult>(result);
        }

        return pResult.attributeScores.TOXICITY.summaryScore.value * 100;
    }

    [System.Serializable] public class Score
    {
        public float value;
        public string type;
    }
    [System.Serializable] public class SpanScores
    {
        public float begin;
        public float end;
        public Score score;
    }
    [System.Serializable] public class Toxicity
    {
        public SpanScores[] spanScores;
        public Score summaryScore;
    }
    [System.Serializable] public class AttributeScores
    {
        public Toxicity TOXICITY;
    }
    [System.Serializable] public class PerspectiveResult
    {
        public AttributeScores attributeScores;
        public string languages;
        public string detectedLanguages;
    }
    #endregion
}
