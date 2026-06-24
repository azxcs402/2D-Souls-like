using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Audio Database")]
public class AudioDatabaseSO : ScriptableObject
{
    public List<AudioClipData> player;
    public List<AudioClipData> combatAudio;
    public List<AudioClipData> uiAudio;
    public List<AudioClipData> bonfireAudio;

    [Header("Music Lists")]
    public List<AudioClipData> mainMenuMusic;
    public List<AudioClipData> levelMusic;
    public List<AudioClipData> doorBattleMusic;
    public List<AudioClipData> bossBattleMusic;


    private Dictionary<string, AudioClipData> clipCollection;

    private void OnEnable()
    {
        clipCollection = new Dictionary<string, AudioClipData>();

        AddToCollection(player);
        AddToCollection(combatAudio);
        AddToCollection(uiAudio);
        AddToCollection(bonfireAudio);
        AddToCollection(mainMenuMusic);
        AddToCollection(levelMusic);
        AddToCollection(doorBattleMusic);
        AddToCollection(bossBattleMusic);
    }

    public bool TryGet(AudioKey audioKey, out AudioClipData data)
    {
        if (AudioKeyMap.TryGetAudioName(audioKey, out string audioName))
        {
            return TryGet(audioName, out data);
        }

        data = null;
        return false;
    }

    public bool TryGet(string groupName, out AudioClipData data)
    {
        if (clipCollection == null)
        {
            data = null;
            return false;
        }

        return clipCollection.TryGetValue(groupName, out data) && data != null;
    }

    private void AddToCollection(List<AudioClipData> listToAdd)
    {
        if (listToAdd == null)
        {
            return;
        }

        foreach (var data in listToAdd)
        {
            if (data != null && clipCollection.ContainsKey(data.audioName) == false)
            {
                clipCollection.Add(data.audioName, data);
            }
        }
    }
}

[System.Serializable]
public class AudioClipData
{
    public string audioName;
    public List<AudioClip> clips = new List<AudioClip>();
    [Range(0f, 3f)] public float maxVolume = 1f;
    [Min(0.01f)] public float maxHearDistance = 12f;

    public bool TryGetRandomClip(out AudioClip clip)
    {
        clip = null;
        if (clips == null || clips.Count == 0)
        {
            return false;
        }

        clip = clips[Random.Range(0, clips.Count)];
        return clip != null;
    }
}
