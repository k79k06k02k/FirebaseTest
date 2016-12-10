
/**********************************************************
// Author   : K.(k79k06k02k)
// FileName : TestFirebase.cs
// Reference: https://firebase.google.com/docs/database/unity/start
**********************************************************/
using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class User
{
    public string username;
    public string email;

    public User()
    {
    }

    public User(string username, string email)
    {
        this.username = username;
        this.email = email;
    }
}

public class LeaderBoardEntry
{
    public string uid;
    public int score = 0;

    public LeaderBoardEntry()
    {
    }

    public LeaderBoardEntry(string uid, int score)
    {
        this.uid = uid;
        this.score = score;
    }

    public Dictionary<string, Object> ToDictionary()
    {
        Dictionary<string, Object> result = new Dictionary<string, Object>();
        result["uid"] = uid;
        result["score"] = score;

        return result;
    }
}

public class TestFirebase : MonoBehaviour
{
    const string URL = "https://arkaitest.firebaseio.com/";
    DatabaseReference root;
    DatabaseReference scores;


    void Start()
    {
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl(URL);
        root = FirebaseDatabase.DefaultInstance.RootReference;
        scores = FirebaseDatabase.DefaultInstance.GetReference("scores");

        //Write();

        Get();

        //Remove();
    }


    private void Write()
    {
        WriteNewUser("01", "KKK", "KKK@gma.com");
        WriteNewUser("02", "JJJ", "JJJ@gma.com");

        WriteNewScore("KKK", 200);
    }

    private void Get()
    {
        scores.GetValueAsync().ContinueWith(task => 
           {
               if (task.IsFaulted)
               {
                   // Handle the error...
               }
               else if (task.IsCompleted)
               {
                   DataSnapshot snapshot = task.Result;
                   Debug.Log(snapshot.GetRawJsonValue());

                   Dictionary<string, Object> uids = (Dictionary<string, Object>)snapshot.GetValue(false);

                   foreach (KeyValuePair<string, Object> item in uids)
                   {
                       Dictionary<string, Object> datas = (Dictionary<string, Object>)item.Value;
                       foreach (KeyValuePair<string, Object> item2 in datas)
                       {
                           Debug.LogFormat("Key:{0} Value:{1}", item2.Key, item2.Value);
                       }
                   }
               }
           });
    }

    private void Remove()
    {
        root.Child("users").RemoveValueAsync();
    }


    private void WriteNewUser(string userId, string name, string email)
    {
        User user = new User(name, email);
        string json = JsonUtility.ToJson(user);

        root.Child("users").Child(userId).SetRawJsonValueAsync(json);
    }

    private void WriteNewScore(string userId, int score)
    {
        // Create new entry at /user-scores/$userid/$scoreid and at
        // /leaderboard/$scoreid simultaneously
        string key = root.Child("scores").Push().Key;
        LeaderBoardEntry entry = new LeaderBoardEntry(userId, score);
        Dictionary<string, Object> entryValues = entry.ToDictionary();

        Dictionary<string, Object> childUpdates = new Dictionary<string, Object>();
        childUpdates["/scores/" + key] = entryValues;
        childUpdates["/user-scores/" + userId + "/" + key] = entryValues;

        root.UpdateChildrenAsync(childUpdates);
    }

    private void AddScoreToLeaders(string email, long score, DatabaseReference leaderBoardRef)
    {
        leaderBoardRef.RunTransaction(mutableData =>
        {
            List<object> leaders = mutableData.Value as List<object>;

            if (leaders == null)
            {
                leaders = new List<object>();
            }
            else if (mutableData.ChildrenCount >= 5)
            {
                long minScore = long.MaxValue;
                object minVal = null;
                foreach (var child in leaders)
                {
                    if (!(child is Dictionary<string, object>)) continue;
                    long childScore = (long)
                                ((Dictionary<string, object>)child)["score"];
                    if (childScore < minScore)
                    {
                        minScore = childScore;
                        minVal = child;
                    }
                }
                if (minScore > score)
                {
                    // The new score is lower than the existing 5 scores, abort.
                    return TransactionResult.Abort();
                }

                // Remove the lowest score.
                leaders.Remove(minVal);
            }

            // Add the new high score.
            Dictionary<string, object> newScoreMap =
                             new Dictionary<string, object>();
            newScoreMap["score"] = score;
            newScoreMap["email"] = email;
            leaders.Add(newScoreMap);
            mutableData.Value = leaders;
            return TransactionResult.Success(mutableData);
        });
    }
}
