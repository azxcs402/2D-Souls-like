using System;
using System.IO;
using UnityEngine;

public class FileDataHandler
{
    private readonly string fullPath;
    private readonly bool encryptData;
    private const string CodeWord = "unitysoulslike";

    public FileDataHandler(string dataDirPath, string dataFileName, bool encryptData)
    {
        fullPath = Path.Combine(dataDirPath, dataFileName);
        this.encryptData = encryptData;
    }

    public void SaveData(GameData gameData)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            string dataToSave = JsonUtility.ToJson(gameData, true);
            if (encryptData)
            {
                dataToSave = EncryptDecrypt(dataToSave);
            }

            using FileStream stream = new FileStream(fullPath, FileMode.Create);
            using StreamWriter writer = new StreamWriter(stream);
            writer.Write(dataToSave);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving data to file '{fullPath}': {e}");
        }
    }

    public GameData LoadData()
    {
        if (!File.Exists(fullPath))
        {
            return null;
        }

        try
        {
            string dataToLoad;
            using FileStream stream = new FileStream(fullPath, FileMode.Open);
            using StreamReader reader = new StreamReader(stream);
            dataToLoad = reader.ReadToEnd();

            if (encryptData)
            {
                dataToLoad = EncryptDecrypt(dataToLoad);
            }

            return JsonUtility.FromJson<GameData>(dataToLoad);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading data from file '{fullPath}': {e}");
            return null;
        }
    }

    public void Delete()
    {
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    public bool HasFile()
    {
        return File.Exists(fullPath);
    }

    private string EncryptDecrypt(string data)
    {
        char[] result = new char[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (char)(data[i] ^ CodeWord[i % CodeWord.Length]);
        }
        return new string(result);
    }
}
