using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private string musicGroupName = "playlist_levels";

    private void Start()
    {
        if (AudioManager.instance != null)
        {
            if (AudioKeyMap.TryParse(musicGroupName, out AudioKey audioKey))
            {
                AudioManager.instance.StartBGM(audioKey);
                return;
            }

            AudioManager.instance.StartBGM(musicGroupName);
        }
    }
}
