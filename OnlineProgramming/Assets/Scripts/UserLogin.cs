using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;  
using PimDeWitte.UnityMainThreadDispatcher;
using System;

public class UserLogin : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [SerializeField] InputField NickNameInput;
    [SerializeField] Text checkText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://multitest-52865-default-rtdb.asia-southeast1.firebasedatabase.app/"
            );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

    }

    public void OnClickLogin()
    {
        string nickName = NickNameInput.text.Trim();

        if (string.IsNullOrEmpty(nickName))
        {
            checkText.text = "닉네임을 입력해주세요.";
            return;
        }

        reference.Child("UserInfo").OrderByChild("NickName").EqualTo(nickName).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    checkText.text = "Firebase 읽기 오류";
                });
            }

            DataSnapshot snapshot = task.Result;

            if (!snapshot.HasChildren)
            {
                dispatcher.Enqueue(() =>
                {
                    checkText.text = "존재하지 않는 닉네임입니다.";
                });
                return;
            }

            foreach(DataSnapshot userSnapshot in snapshot.Children)
            {
                string userKey = userSnapshot.Key;

                dispatcher.Enqueue(() =>
                {
                    PlayerPrefs.SetString("UserKey", userKey);
                    PlayerPrefs.SetString("UserNickName", nickName);
                    PlayerPrefs.Save();

                    checkText.text = "로그인 성공";
                });

                break;
            }
        });
    }
}
