using UnityEngine;

/// <summary>
/// 인앱결제 성공 후 실제 보상을 지급하는 흐름을 단순화한 샘플입니다.
/// 
/// 실제 프로젝트에서는 GameSetting 내부에서 재화 지급, 패키지 구매 완료 처리,
/// 누적 결제 보상, UI 갱신, 저장 처리 등을 함께 수행했습니다.
/// 
/// 포트폴리오 공개용 코드이므로 실제 상품명, 가격, 보상량, 내부 변수명은 샘플용으로 변경했습니다.
/// </summary>
public class IapRewardExample : MonoBehaviour
{
    #region UI

    [Header("UI")]
    [SerializeField] private GameObject purchaseSuccessPopup;
    [SerializeField] private GameObject adRemovePurchasedObject;

    #endregion


    #region Sample User Data

    private int premiumCurrency;
    private int adSkipTicket;
    private int totalPaymentAmount;

    private bool isAdRemoved;

    #endregion


    #region PlayerPrefs Keys

    private const string PREMIUM_CURRENCY_KEY = "Sample_PremiumCurrency";
    private const string AD_SKIP_TICKET_KEY = "Sample_AdSkipTicket";
    private const string TOTAL_PAYMENT_AMOUNT_KEY = "Sample_TotalPaymentAmount";
    private const string AD_REMOVE_KEY = "Sample_AdRemove";

    #endregion


    #region Unity Lifecycle

    private void Awake()
    {
        LoadUserData();
        RefreshStoreState();
    }

    #endregion


    #region Package Rewards

    public void GrantStarterPackage()
    {
        AddPremiumCurrency(100);
        AddAdSkipTicket(5);
        AddPaymentAmount(1900);

        // 실제 프로젝트에서는 패키지 구매 완료 상태,
        // 첫 결제 보상, 누적 결제 보상, 구매 성공 UI 등을 함께 갱신했습니다.
        SaveUserData();
        RefreshStoreState();
        ShowPurchaseSuccessPopup();

        Debug.Log("[IAP Reward] Starter package granted.");
    }

    public void GrantGrowthPackage()
    {
        AddPremiumCurrency(300);
        AddAdSkipTicket(15);
        AddPaymentAmount(6900);

        SaveUserData();
        RefreshStoreState();
        ShowPurchaseSuccessPopup();

        Debug.Log("[IAP Reward] Growth package granted.");
    }

    public void GrantAdRemove()
    {
        isAdRemoved = true;
        AddPaymentAmount(5500);

        SaveUserData();
        RefreshStoreState();
        ShowPurchaseSuccessPopup();

        Debug.Log("[IAP Reward] Ad remove granted.");
    }

    #endregion


    #region Currency Rewards

    public void GrantDiamondSmall()
    {
        AddPremiumCurrency(45);
        AddPaymentAmount(1500);

        SaveUserData();
        RefreshStoreState();
        ShowPurchaseSuccessPopup();

        Debug.Log("[IAP Reward] Diamond small granted.");
    }

    public void GrantDiamondMedium()
    {
        AddPremiumCurrency(215);
        AddPaymentAmount(7000);

        SaveUserData();
        RefreshStoreState();
        ShowPurchaseSuccessPopup();

        Debug.Log("[IAP Reward] Diamond medium granted.");
    }

    public void GrantDiamondLarge()
    {
        AddPremiumCurrency(630);
        AddPaymentAmount(20000);

        SaveUserData();
        RefreshStoreState();
        ShowPurchaseSuccessPopup();

        Debug.Log("[IAP Reward] Diamond large granted.");
    }

    #endregion


    #region Data Update

    private void AddPremiumCurrency(int amount)
    {
        if (amount <= 0)
            return;

        premiumCurrency += amount;
    }

    private void AddAdSkipTicket(int amount)
    {
        if (amount <= 0)
            return;

        adSkipTicket += amount;
    }

    private void AddPaymentAmount(int amount)
    {
        if (amount <= 0)
            return;

        totalPaymentAmount += amount;

        // 실제 프로젝트에서는 이 값을 기준으로 누적 결제 보상,
        // 결제 관련 칭호, 추가 다이아 보상 등을 갱신했습니다.
    }

    #endregion


    #region Save / Load

    private void LoadUserData()
    {
        premiumCurrency = PlayerPrefs.GetInt(PREMIUM_CURRENCY_KEY, 0);
        adSkipTicket = PlayerPrefs.GetInt(AD_SKIP_TICKET_KEY, 0);
        totalPaymentAmount = PlayerPrefs.GetInt(TOTAL_PAYMENT_AMOUNT_KEY, 0);
        isAdRemoved = PlayerPrefs.GetInt(AD_REMOVE_KEY, 0) == 1;
    }

    private void SaveUserData()
    {
        PlayerPrefs.SetInt(PREMIUM_CURRENCY_KEY, premiumCurrency);
        PlayerPrefs.SetInt(AD_SKIP_TICKET_KEY, adSkipTicket);
        PlayerPrefs.SetInt(TOTAL_PAYMENT_AMOUNT_KEY, totalPaymentAmount);
        PlayerPrefs.SetInt(AD_REMOVE_KEY, isAdRemoved ? 1 : 0);

        PlayerPrefs.Save();
    }

    #endregion


    #region UI Refresh

    private void RefreshStoreState()
    {
        // 실제 프로젝트에서는 아래와 같은 작업을 함께 수행했습니다.
        // - 구매 완료 오브젝트 표시
        // - 패키지 재구매 제한 처리
        // - 재화 텍스트 갱신
        // - 누적 결제 보상 상태 갱신
        // - 광고 제거 상태 반영
        // - 서버 저장 또는 클라우드 저장 동기화

        if (adRemovePurchasedObject != null)
            adRemovePurchasedObject.SetActive(isAdRemoved);
    }

    private void ShowPurchaseSuccessPopup()
    {
        if (purchaseSuccessPopup != null)
            purchaseSuccessPopup.SetActive(true);
    }

    #endregion
}
