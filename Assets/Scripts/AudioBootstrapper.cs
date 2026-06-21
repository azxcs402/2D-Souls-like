using UnityEngine;

public static class AudioBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateAudioManager()
    {
        if (Object.FindFirstObjectByType<AudioManager>() != null)
        {
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("Audio/___ AUDIO MANAGER ___");
        if (prefab != null)
        {
            Object.Instantiate(prefab);
            return;
        }

        new GameObject("___ AUDIO MANAGER ___").AddComponent<AudioManager>();
    }
}
