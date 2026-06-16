using UnityEngine;

public class AuctionData
{
    public string AuctionKey;
    public string SellerKey;
    public string SellerNickName;
    public string SeleerNickName;
    public string Itemname;
    public int Count;
    public int price;
    public bool IsSold;

    public AuctionData()
    { 
    
    }
    
    public AuctionData(string auctionKey, string sellerKey, string sellerNickName, 
        string itemname, int count, int price , bool isSold)
    {
        AuctionKey = auctionKey;
        SellerKey = sellerKey;
        SellerNickName = sellerNickName;
        Itemname = itemname;
        Count = count;
        this.price = price;
        IsSold = isSold;
    }

}
