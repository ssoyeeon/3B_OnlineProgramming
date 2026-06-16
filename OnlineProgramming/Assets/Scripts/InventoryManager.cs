using Firebase.Database;
using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [SerializeField] Text PotionCountText;
    [SerializeField] Text BombCountText;
    [SerializeField] Text TicketCountText;
    [SerializeField] Text MessageText;

    string userKey;

    Dictionary<string, int> inventory = new Dictionary<string, int>();


    [SerializeField] string NextSceneName = "AuctionScene";

    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://multitest-52865-default-rtdb.asia-southeast1.firebasedatabase.app/"
            );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

        LoadInventory();
    }

    void LoadInventory()
    {
        userKey = PlayerPrefs.GetString("UserKey", "");

        if (string.IsNullOrEmpty(userKey))
        {
            MessageText.text = "로그인 정보가 없습니다.";
            return;
        }
        
        reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "인벤토리 불러오기 실패";
                });
                return;
            }
            
            if(task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if(snapshot.Value == null)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "인벤토리 데이터가 없습니다.";
                    });
                    return;
                }

                string inventoryJson = snapshot.Value.ToString();
                
                inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = "인벤토리 불러오기 완료";
                });
            }    
        });
    }

    int GetItemCount(string itemName)
    {
        if (inventory.ContainsKey(itemName))
        {
            return inventory[itemName];
        }
        return 0;
    }

    void RefreshUI()
    {
        PotionCountText.text = $"EnergyDrink: {GetItemCount("EnergyDrink")}";
        BombCountText.text = $"Shield: {GetItemCount("Shield")}";
        TicketCountText.text = $"MagicStone: {GetItemCount("MagicStone")}";
        TicketCountText.text = $"GoldKey: {GetItemCount("GoldKey")}";
    }

    void UseItem(string itemName)
    {
        if(!inventory.ContainsKey(itemName))
        {
            MessageText.text = itemName + "아이템이 없네용.";
            return;
        }

        if(inventory[itemName] <= 0)
        {
            MessageText.text = itemName + "개수가 부족하네용 ㅠㅠ.";
            return;
        }
        inventory[itemName]--;

        SaveInventory(itemName);
    }

    public void OnClickUseEnergyDrink()
    {
        UseItem("EnergyDrink");
    }

    public void OnClickUseShield()
    {
        UseItem("Shield");
    }

    public void OnClickUseMagicStone() 
    {
        UseItem("MagicStone");
    }
    public void OnClickUseGoldKey()
    {
        UseItem("GoldKey");
    }
    public void OnClickInvenScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(NextSceneName);
    }

    void SaveInventory(string userItemname)
    {
        string inventoryJson = JsonConvert.SerializeObject(inventory);

        reference.Child("UserInfo").Child(userKey).Child("Inventory").SetRawJsonValueAsync(inventoryJson).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "인벤토리 저장 실패";
                });
                return;
            }
            
            if(task.IsCompleted)
            {
                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = userItemname + " 사용했네용";
                });
            }
        });
    }

}
