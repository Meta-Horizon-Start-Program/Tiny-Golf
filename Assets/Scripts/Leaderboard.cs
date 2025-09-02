using UnityEngine;
using Oculus.Platform;
using Oculus.Platform.Models;
using TMPro;

public class Leaderboard : MonoBehaviour
{
    [SerializeField] private string leaderboardName = "YourLeaderboardName";
    [SerializeField] private int fetchLimit = 10;
    public TextMeshPro tmp;

    private void Awake()
    {
        Entitlements.IsUserEntitledToApplication().OnComplete(OnEntitlementChecked);
    }

    private void OnEntitlementChecked(Message msg)
    {
        if (msg.IsError)
        {
            Debug.LogError("LEADERBOARD : " + "[OculusPlatform] User not entitled. Quitting.");
            return;
        }

        FetchTopEntries(fetchLimit);
    }

    public void FetchTopEntries(int limit = 10)
    {
        Leaderboards
            .GetEntries(leaderboardName, limit, LeaderboardFilterType.None, LeaderboardStartAt.Top)
            .OnComplete(OnEntriesReceived);
    }

    private void OnEntriesReceived(Message<LeaderboardEntryList> msg)
    {
        string leaderboardtext = "";

        foreach (var e in msg.Data)
        {
            var name = e.User != null ? e.User.OculusID : "(unknown)";
            Debug.Log("LEADERBOARD : " + $"#{e.Rank}  {name}  score={e.Score}  ts={e.Timestamp}");
            leaderboardtext += e.Rank + " - " + name + " - " + e.Score + "\n"; 
        }

        tmp.text = leaderboardtext;
    }

    public void SubmitScore(long score, bool forceUpdate = false, byte[] extraData = null)
    {
        Leaderboards
            .WriteEntry(leaderboardName, score, extraData, forceUpdate)
            .OnComplete(OnWriteCompleted);
    }

    private void OnWriteCompleted(Message<bool> msg)
    {
        //refresh list:
        FetchTopEntries(fetchLimit);
    }
}
