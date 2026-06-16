using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections.Generic;
using Unity.VisualScripting;

public class AuctionManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("UI")]
    [SerializeField] Text EnergyDrinkCountText;
    [SerializeField] Text ShieldCountText;
    [SerializeField] Text MagicStoneCountText;
    [SerializeField] Text GoldKeyCountText;
    [SerializeField] Text MessageText;

    [Header("UI")]
    [SerializeField] InputField PriceInput;

    [Header("Scene")]
    [SerializeField] string NextSceneName = "InventoryScene";

    string userKey;
    string nickName;

    int currentCoin;

    Dictionary<string, int> inventory = new Dictionary<string, int>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        database = FirebaseDatabase.GetInstance(
            "https://multitest-52865-default-rtdb.asia-southeast1.firebasedatabase.app/"
            );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

        LoadMyData();
    }


    void LoadMyData()
    {
        userKey = PlayerPrefs.GetString("Userkey");
        
        if(string.IsNullOrEmpty(userKey))
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
                    MessageText.text = "내 정보 불러오기 실패";
                });
                return;
            }
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                currentCoin = int.Parse(snapshot.Child("Coin").Value.ToString());

                string inventoryJson = snapshot.Child("Inventory").Value.ToString();

                inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);
    
                dispatcher.Enqueue(() =>
                {
                RefreshUI();
                MessageText.text = "인벤토리 불러오기 성공";
                });
            }
        });
    }

    void SellItem(string itemName)
    {
        if(string.IsNullOrEmpty(PriceInput.text))
        {
            MessageText.text = "가격을 입력해주세요.";
            return;
        }
         
        int price = int.Parse(PriceInput.text);

        if (price <= 0)
        {
            MessageText.text = "가격은 0보다 커야 합니다.";
            return;
        }

        if(!inventory.ContainsKey(itemName) || inventory[itemName] <= 0)
        {
            MessageText.text = $"인벤토리에 {itemName}이(가) 없습니다.";
            return;
        }

        inventory[itemName]--;

        string inventoryJson = JsonConvert.SerializeObject(inventory);

        DatabaseReference auctionRef = reference.Child("AuctionList").Push();
        string auctionKey = auctionRef.Key;

        Dictionary<string, object> updataData = new Dictionary<string, object>();

        updataData["UserInfo/" + userKey + "/Inventory"] = inventoryJson;

        updataData["AuctionList/" + auctionKey + "/AuctionKey"] = auctionKey;
        updataData["AuctionList/" + auctionKey + "/SellerKey"] = userKey;
        updataData["AuctionList/" + auctionKey + "/SellerNickName"] = nickName;
        updataData["AuctionList/" + auctionKey + "/ItemName"] = itemName;
        updataData["AuctionList/" + auctionKey + "/Count"] = 1;
        updataData["AuctionList/" + auctionKey + "/Price"] = price;
        updataData["AuctionList/" + auctionKey + "/IsSold"] = false;
        updataData["AuctionList/" + auctionKey + "/ListedAt"] = ServerValue.Timestamp;

        reference.UpdateChildrenAsync(updataData).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "아이템 판매 등록 실패";
                });
            }
            if(task.IsCompleted)
            {
                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = "아이템 판매 등록 성공";
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
        EnergyDrinkCountText.text = $"EnergyDrink: {GetItemCount("EnergyDrink")}";
        ShieldCountText.text = $"Shield: {GetItemCount("Shield")}";
        GoldKeyCountText.text = $"GoldKey: {GetItemCount("GoldKey")}";
        MagicStoneCountText.text = $"MagicStone: {GetItemCount("MagicStone")}";
    }

    public void OnClickSellEnergyDrink()
    {
        SellItem("EnergyDrink");
    }

    public void OnClickSellShield()
    {
        SellItem("Shield");
    }

    public void OnClickSellMagicStone()
    {
        SellItem("MagicStone");
    }

    public void OnClickSellGoldKey()
    {
       SellItem("GoldKey");
    }

    public void OnClickInvenScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(NextSceneName);
    }

    void BuyItem()
    {
        FirebaseDatabase.GetInstance(
            "https://multitest-52865-default-rtdb.asia-southeast1.firebasedatabase.app/"
            ).RootReference.Child("AuctionList").OrderByChild("IsSold").EqualTo(false).GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "경매장 정보 불러오기 실패";
                    });
                    return;
                }
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    foreach (DataSnapshot auction in snapshot.Children)
                    {
                        string itemName = auction.Child("ItemName").Value.ToString();
                        int price = int.Parse(auction.Child("Price").Value.ToString());
                        // 구매 로직 구현
                    }
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "경매장 정보 불러오기 성공";
                    });
                }
            });
    }
    public void OnClickBuyItem(string auctionKey, string itemName, int price)
    {
        // 1. 방어 코드: 내 돈이 아이템 가격보다 적으면 구매 거부
        if (currentCoin < price)
        {
            MessageText.text = "코인이 부족하여 아이템을 구매할 수 없습니다!";
            return;
        }

        MessageText.text = "아이템 구매 처리 중...";

        // 2. 내 데이터 계산 (코인 차감 및 인벤토리 개수 +1)
        int nextCoin = currentCoin - price;

        if (inventory.ContainsKey(itemName))
        {
            inventory[itemName]++;
        }
        else
        {
            inventory[itemName] = 1;
        }

        // 인벤토리 딕셔너리를 파이어베이스에 넣을 수 있게 JSON 문자열로 변환
        string updatedInventoryJson = JsonConvert.SerializeObject(inventory);

        // 3. 파이어베이스에 '코인 차감 + 인벤토리 추가 + 경매장에서 제거(판매완료)'를 동시에 요청할 딕셔너리 생성
        Dictionary<string, object> childUpdates = new Dictionary<string, object>();

        // 내 유저 정보 노드 업데이트 경로
        childUpdates["UserInfo/" + userKey + "/Coin"] = nextCoin;
        childUpdates["UserInfo/" + userKey + "/Inventory"] = updatedInventoryJson;

        // 경매장 노드 업데이트 경로 (IsSold를 true로 바꾸면 기존 BuyItem()의 EqualTo(false) 필터링에 의해 목록에서 자동 삭제됩니다!)
        childUpdates["AuctionList/" + auctionKey + "/IsSold"] = true;
        childUpdates["AuctionList/" + auctionKey + "/BuyerKey"] = userKey;

        // 4. 파이어베이스 서버에 원자적(Atomic)으로 일괄 업데이트 던지기
        reference.UpdateChildrenAsync(childUpdates).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "구매 실패 (네트워크 오류가 발생했습니다)";
                });
                return;
            }

            if (task.IsCompleted)
            {
                // 5. 서버 저장 성공 시 내 로컬 메모리의 코인 변수도 동기화
                currentCoin = nextCoin;

                dispatcher.Enqueue(() =>
                {
                    // UI 글자들 갱신하고 성공 메시지 띄우기
                    RefreshUI();
                    MessageText.text = $" {itemName}을(를) {price}코인에 구매했습니다";

                    // 구매 성공 후 경매장 리스트를 최신화하기 위해 기존에 짜두신 리스트 함수 호출
                    BuyItem();
                });
            }
        });
    }
    public void OnClickBuyItemMock()
    {
        // 테스트용: 현재 경매장에 있는 아이템 목록을 한 번 긁어와서 맨 첫 번째 녀석을 사버리는 테스트 로직
        reference.Child("AuctionList").OrderByChild("IsSold").EqualTo(false).LimitToFirst(1).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                foreach (DataSnapshot auction in task.Result.Children)
                {
                    string key = auction.Child("AuctionKey").Value.ToString();
                    string name = auction.Child("ItemName").Value.ToString();
                    int prc = int.Parse(auction.Child("Price").Value.ToString());

                    dispatcher.Enqueue(() => {
                        OnClickBuyItem(key, name, prc); // 위의 진짜 구매 함수 호출
                    });
                }
            }
            else
            {
                dispatcher.Enqueue(() => { MessageText.text = "경매장에 구매 가능한 아이템이 없습니다."; });
            }
        });
    }
}
