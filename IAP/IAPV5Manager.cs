using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// Unity IAP V5 기반 인앱결제 처리 샘플입니다.
/// 실제 프로젝트에서 사용한 결제 흐름을 포트폴리오 공개용으로 정리한 코드입니다.
/// 
/// 주요 목적:
/// - IAP 초기화 전 구매 요청 차단
/// - 상품 정보 Fetch / 구매 정보 Fetch 실패 대응
/// - 구매 완료 후 보상 지급
/// - 동일 영수증 기준 중복 보상 지급 방지
/// - 보상 지급 후 구매 확정 처리
/// 
/// 실제 상품 ID, 보상량, 프로젝트 고유 정보는 샘플용으로 변경했습니다.
/// </summary>
public class IAPV5Manager : MonoBehaviour
{
    #region References

    [Header("Reward Handler")]
    [SerializeField] private IapRewardExample rewardHandler;

    [Header("UI")]
    [SerializeField] private GameObject purchaseFailPopup;

    #endregion


    #region IAP State

    private StoreController store;
    private bool isIapReady;

    private bool productsFetchedOnce;
    private bool purchasesFetchedOnce;

    #endregion


    #region Product IDs

    // Consumable
    private const string STARTER_PACKAGE = "sample_starter_package";
    private const string GROWTH_PACKAGE = "sample_growth_package";
    private const string DIAMOND_SMALL = "sample_diamond_small";
    private const string DIAMOND_MEDIUM = "sample_diamond_medium";
    private const string DIAMOND_LARGE = "sample_diamond_large";

    // Non-Consumable
    private const string AD_REMOVE = "sample_ad_remove";

    #endregion


    #region Unity Lifecycle

    private async void Awake()
    {
        if (rewardHandler == null)
            rewardHandler = FindFirstObjectByType<IapRewardExample>();

        ResetIapState();

        store = UnityIAPServices.StoreController();

        RegisterStoreEvents();

        await store.Connect();

        Debug.Log("[IAP] Store connected. FetchProducts start.");

        store.FetchProducts(CreateProductDefinitions());
    }

    private void OnDestroy()
    {
        UnregisterStoreEvents();
    }

    #endregion


    #region Public Purchase Button Methods

    public void BuyStarterPackage()
    {
        Purchase(STARTER_PACKAGE);
    }

    public void BuyGrowthPackage()
    {
        Purchase(GROWTH_PACKAGE);
    }

    public void BuyDiamondSmall()
    {
        Purchase(DIAMOND_SMALL);
    }

    public void BuyDiamondMedium()
    {
        Purchase(DIAMOND_MEDIUM);
    }

    public void BuyDiamondLarge()
    {
        Purchase(DIAMOND_LARGE);
    }

    public void BuyAdRemove()
    {
        Purchase(AD_REMOVE);
    }

    #endregion


    #region IAP Setup

    private void ResetIapState()
    {
        isIapReady = false;
        productsFetchedOnce = false;
        purchasesFetchedOnce = false;
    }

    private void RegisterStoreEvents()
    {
        if (store == null)
            return;

        store.OnStoreDisconnected += OnStoreDisconnected;

        store.OnProductsFetched += OnProductsFetched;
        store.OnProductsFetchFailed += OnProductsFetchFailed;

        store.OnPurchasesFetched += OnPurchasesFetched;
        store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;

        store.OnPurchasePending += OnPurchasePending;
        store.OnPurchaseFailed += OnPurchaseFailed;
    }

    private void UnregisterStoreEvents()
    {
        if (store == null)
            return;

        store.OnStoreDisconnected -= OnStoreDisconnected;

        store.OnProductsFetched -= OnProductsFetched;
        store.OnProductsFetchFailed -= OnProductsFetchFailed;

        store.OnPurchasesFetched -= OnPurchasesFetched;
        store.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;

        store.OnPurchasePending -= OnPurchasePending;
        store.OnPurchaseFailed -= OnPurchaseFailed;
    }

    private List<ProductDefinition> CreateProductDefinitions()
    {
        return new List<ProductDefinition>
        {
            new ProductDefinition(STARTER_PACKAGE, ProductType.Consumable),
            new ProductDefinition(GROWTH_PACKAGE, ProductType.Consumable),
            new ProductDefinition(DIAMOND_SMALL, ProductType.Consumable),
            new ProductDefinition(DIAMOND_MEDIUM, ProductType.Consumable),
            new ProductDefinition(DIAMOND_LARGE, ProductType.Consumable),

            new ProductDefinition(AD_REMOVE, ProductType.NonConsumable),
        };
    }

    #endregion


    #region Purchase Flow

    private void Purchase(string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            Debug.LogError("[IAP] ProductId is null or empty.");
            ShowPurchaseFailPopup();
            return;
        }

        if (!isIapReady)
        {
            Debug.LogWarning("[IAP] IAP is not ready. Purchase blocked: " + productId);
            ShowPurchaseFailPopup();
            return;
        }

        if (store == null)
        {
            Debug.LogError("[IAP] Store is null. Purchase failed: " + productId);
            ShowPurchaseFailPopup();
            return;
        }

        Debug.Log("[IAP] Purchase requested: " + productId);
        store.PurchaseProduct(productId);
    }

    private void OnPurchasePending(PendingOrder order)
    {
        string productId = GetFirstProductId(order);

        if (string.IsNullOrEmpty(productId))
        {
            Debug.LogError("[IAP] Pending order productId is empty.");
            ShowPurchaseFailPopup();
            return;
        }

        string receipt = GetReceipt(order);
        string dedupeKey = CreateDedupeKey(productId, receipt);

        if (IsRewardAlreadyGranted(dedupeKey))
        {
            Debug.Log("[IAP] Reward already granted. Confirm only: " + productId);
            store.ConfirmPurchase(order);
            return;
        }

        Debug.Log("[IAP] Purchase pending. Grant reward start: " + productId);

        bool rewardGranted = GrantReward(productId);

        if (!rewardGranted)
        {
            Debug.LogError("[IAP] Reward grant failed: " + productId);
            ShowPurchaseFailPopup();
            return;
        }

        SaveRewardGranted(dedupeKey);

        store.ConfirmPurchase(order);

        Debug.Log("[IAP] Purchase confirmed: " + productId);
    }

    private void OnPurchaseFailed(FailedOrder order)
    {
        ShowPurchaseFailPopup();

        if (order == null)
        {
            Debug.LogError("[IAP] Purchase failed. FailedOrder is null.");
            return;
        }

        Debug.LogWarning("[IAP] Purchase failed. reason=" + order.FailureReason + " / details=" + order.Details);
    }

    #endregion


    #region Store Events

    private void OnStoreDisconnected(StoreConnectionFailureDescription description)
    {
        isIapReady = false;
        Debug.LogWarning("[IAP] Store disconnected: " + description);
    }

    private void OnProductsFetched(List<Product> products)
    {
        if (productsFetchedOnce)
            return;

        productsFetchedOnce = true;

        if (products == null || products.Count == 0)
        {
            isIapReady = false;
            Debug.LogError("[IAP] Products fetched, but product count is zero.");
            return;
        }

        Debug.Log("[IAP] Products fetched. count=" + products.Count);

        for (int i = 0; i < products.Count; i++)
        {
            Debug.Log("[IAP] Product: " + products[i].definition.id);
        }

        Debug.Log("[IAP] FetchPurchases start.");
        store.FetchPurchases();
    }

    private void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        isIapReady = false;
        Debug.LogError("[IAP] Products fetch failed: " + failure);
    }

    private void OnPurchasesFetched(Orders orders)
    {
        if (purchasesFetchedOnce)
            return;

        purchasesFetchedOnce = true;

        int confirmedOrderCount = 0;

        if (orders != null && orders.ConfirmedOrders != null)
            confirmedOrderCount = orders.ConfirmedOrders.Count;

        Debug.Log("[IAP] Purchases fetched. confirmed=" + confirmedOrderCount);

        isIapReady = true;

        Debug.Log("[IAP] Ready = true");
    }

    private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        isIapReady = false;
        Debug.LogError("[IAP] Purchases fetch failed: " + failure);
    }

    #endregion


    #region Reward

    private bool GrantReward(string productId)
    {
        if (rewardHandler == null)
        {
            rewardHandler = FindFirstObjectByType<IapRewardExample>();

            if (rewardHandler == null)
            {
                Debug.LogError("[IAP] RewardHandler is null.");
                return false;
            }
        }

        switch (productId)
        {
            case STARTER_PACKAGE:
                rewardHandler.GrantStarterPackage();
                return true;

            case GROWTH_PACKAGE:
                rewardHandler.GrantGrowthPackage();
                return true;

            case DIAMOND_SMALL:
                rewardHandler.GrantDiamondSmall();
                return true;

            case DIAMOND_MEDIUM:
                rewardHandler.GrantDiamondMedium();
                return true;

            case DIAMOND_LARGE:
                rewardHandler.GrantDiamondLarge();
                return true;

            case AD_REMOVE:
                rewardHandler.GrantAdRemove();
                return true;

            default:
                Debug.LogError("[IAP] Unknown productId: " + productId);
                return false;
        }
    }

    #endregion


    #region Dedupe

    private static string GetReceipt(PendingOrder order)
    {
        if (order == null || order.Info == null || order.Info.Receipt == null)
            return string.Empty;

        return order.Info.Receipt.ToString();
    }

    private static string CreateDedupeKey(string productId, string receipt)
    {
        string rawKey = productId + "|" + receipt;
        return "iap_reward_granted_" + Sha1(rawKey);
    }

    private static bool IsRewardAlreadyGranted(string dedupeKey)
    {
        return PlayerPrefs.GetInt(dedupeKey, 0) == 1;
    }

    private static void SaveRewardGranted(string dedupeKey)
    {
        PlayerPrefs.SetInt(dedupeKey, 1);
        PlayerPrefs.Save();
    }

    #endregion


    #region Utility

    private void ShowPurchaseFailPopup()
    {
        if (purchaseFailPopup != null)
            purchaseFailPopup.SetActive(true);
    }

    private static string GetFirstProductId(PendingOrder order)
    {
        if (order == null || order.CartOrdered == null)
            return null;

        foreach (var item in order.CartOrdered.Items())
        {
            if (item != null && item.Product != null)
                return item.Product.definition.id;
        }

        return null;
    }

    private static string Sha1(string value)
    {
        using var sha = SHA1.Create();

        byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        StringBuilder builder = new StringBuilder(hashBytes.Length * 2);

        for (int i = 0; i < hashBytes.Length; i++)
            builder.Append(hashBytes[i].ToString("x2"));

        return builder.ToString();
    }

    #endregion
}
