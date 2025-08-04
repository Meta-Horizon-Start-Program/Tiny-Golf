using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// This script Toggles on / off the video player after each loop to fix a bug where the video player freezes after time.
/// </summary>
public class ResetVideoOnLoop : MonoBehaviour
{
    [SerializeField] VideoPlayer m_VideoPlayer;
    // Start is called before the first frame update
    void Start() => m_VideoPlayer.loopPointReached += OnLoopPointReached;
    void OnDestroy() => m_VideoPlayer.loopPointReached -= OnLoopPointReached;


    private void OnLoopPointReached(VideoPlayer source)
    {
        source.enabled = false;
        source.enabled = true;
    }
}
