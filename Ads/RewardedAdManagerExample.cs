using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using GoogleMobileAds.Api;

/// <summary>
/// AdMob RewardedAd 샘플 코드입니다.
/// 실제 라이브 서비스에서 사용했던 광고 로드, 실패 재시도, 로딩 UI,
/// 보상 지급, 쿨타임 처리 흐름을 포트폴리오용으로 정리한 예시입니다.
/// </summary>
public class RewardedAdManagerExample : MonoBehaviour
{
    [Header("Ad Unit ID")]
    [SerializeField] private bool useTestAd = true;

    [SerializeField] private string androidRewardAdUnitId = "ca-app-pub-xxxxxxxxxxxxxxxx/yyyyyyyyyy";
    [SerializeField] private string iosRewardAdUnitId = "ca-app-pub-xxxxxxxxxxxxxxxx/zzzzzzzzzz";

    private const string AndroidTestRewardAdUnitId = "ca-app-pub-3940256099942544/5224354917";
    private const string IosTestRewardAdUnitId = "ca-app-pub-3940256099942544/1712485313";

    [Header("UI")]
    [SerializeField] private GameObject adLoadingObject;
    [SerializeField] private GameObject adLoadFailedObject;
    [SerializeField] private GameObject nonNetworkObject;
    [SerializeField] private GameObject rewardSuccessObject;

    [SerializeField] private Text adErrorLogText;
    [SerializeField] private Text cooldownText;

    [Header("Setting")]
    [SerializeField] private int maxRetryCount = 3;
    [SerializeField] private float retryDelay = 2f;
    [SerializeField] private int rewardAdCooldownSecond = 300;

    private RewardedAd rewardedAd;
    private int retryCount;
    private bool isLoading;
    private bool isShowing;
    private bool isCooldown;
    private int cooldownRemainSecond;

    private Action rewardCallback;

    private void Start()
    {
        MobileAds.Initialize(_ =>
        {
            LoadRewardedAd();
        });
    }

    /// <summary>
    /// 보상형 광고를 미리 로드합니다.
    /// 기존 광고 객체가 남아있으면 Destroy 후 새로 로드합니다.
    /// </summary>
    public void LoadRewardedAd()
    {
        if (isLoading)
            return;

        if (IsNetworkDisconnected())
        {
            ShowNonNetworkUI();
            return;
        }

        isLoading = true;
        HideFailUI();

        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        string adUnitId = GetRewardAdUnitId();
        AdRequest adRequest = new AdRequest();

        RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            isLoading = false;

            if (error != null || ad == null)
            {
                LogAdError("RewardedAd Load Failed", error != null ? error.ToString() : "Ad is null");
                StartCoroutine(RetryLoadRewardedAd());
                return;
            }

            rewardedAd = ad;
            retryCount = 0;

            RegisterAdEventHandler(rewardedAd);
        });
    }

    /// <summary>
    /// 광고 버튼에서 호출하는 함수입니다.
    /// 광고가 준비되어 있으면 바로 노출하고, 없으면 로딩 후 재시도합니다.
    /// </summary>
    public void ShowRewardedAd(Action onReward = null)
    {
        if (isShowing)
            return;

        if (isCooldown)
        {
            LogAdError("RewardedAd Cooldown", "광고 쿨타임이 아직 남아 있습니다.");
            return;
        }

        if (IsNetworkDisconnected())
        {
            ShowNonNetworkUI();
            return;
        }

        rewardCallback = onReward;

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            InternalShowRewardedAd();
            return;
        }

        StartCoroutine(LoadAndShowRewardedAd());
    }

    /// <summary>
    /// 광고가 아직 로드되지 않은 경우 로딩 UI를 보여주고,
    /// 일정 시간 동안 로드 완료를 기다린 뒤 광고를 노출합니다.
    /// </summary>
    private IEnumerator LoadAndShowRewardedAd()
    {
        ShowLoadingUI();

        LoadRewardedAd();

        float waitTime = 0f;
        const float maxWaitTime = 5f;

        while (waitTime < maxWaitTime)
        {
            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                HideLoadingUI();
                InternalShowRewardedAd();
                yield break;
            }

            waitTime += Time.deltaTime;
            yield return null;
        }

        HideLoadingUI();
        ShowLoadFailedUI();

        LogAdError("RewardedAd Show Failed", "광고 로드 시간이 초과되었습니다.");
    }

    private void InternalShowRewardedAd()
    {
        if (rewardedAd == null || !rewardedAd.CanShowAd())
        {
            ShowLoadFailedUI();
            LogAdError("RewardedAd Show Failed", "광고가 준비되지 않았습니다.");
            return;
        }

        isShowing = true;

        rewardedAd.Show(reward =>
        {
            GiveReward();
        });
    }

    /// <summary>
    /// 광고 시청 완료 후 실제 보상을 지급하는 부분입니다.
    /// 실제 프로젝트에서는 다이아 지급, 재화 지급, 시간 단축, 버프 적용 등으로 연결됩니다.
    /// </summary>
    private void GiveReward()
    {
        rewardCallback?.Invoke();
        rewardCallback = null;

        if (rewardSuccessObject != null)
            rewardSuccessObject.SetActive(true);

        StartCoroutine(StartAdCooldown());
    }

    /// <summary>
    /// 광고 로드 실패 시 일정 횟수까지 재시도합니다.
    /// 네트워크 불안정, 광고 응답 지연 상황에서 유저 경험이 끊기지 않게 하기 위한 처리입니다.
    /// </summary>
    private IEnumerator RetryLoadRewardedAd()
    {
        if (retryCount >= maxRetryCount)
        {
            ShowLoadFailedUI();
            yield break;
        }

        retryCount++;

        yield return new WaitForSeconds(retryDelay);

        LoadRewardedAd();
    }

    /// <summary>
    /// 광고 시청 후 쿨타임을 적용합니다.
    /// 광고 보상이 반복 악용되지 않도록 하고, UI에 남은 시간을 표시합니다.
    /// </summary>
    private IEnumerator StartAdCooldown()
    {
        isCooldown = true;
        cooldownRemainSecond = rewardAdCooldownSecond;

        while (cooldownRemainSecond > 0)
        {
            if (cooldownText != null)
                cooldownText.text = cooldownRemainSecond + "초";

            cooldownRemainSecond--;
            yield return new WaitForSeconds(1f);
        }

        isCooldown = false;

        if (cooldownText != null)
            cooldownText.text = string.Empty;

        LoadRewardedAd();
    }

    /// <summary>
    /// 광고 객체의 이벤트를 등록합니다.
    /// 광고가 닫히거나 실패했을 때 다음 광고를 다시 로드합니다.
    /// </summary>
    private void RegisterAdEventHandler(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            isShowing = false;
            LoadRewardedAd();
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            isShowing = false;

            LogAdError("RewardedAd FullScreen Failed", error != null ? error.ToString() : "Unknown error");
            StartCoroutine(RetryLoadRewardedAd());
        };
    }

    private string GetRewardAdUnitId()
    {
#if UNITY_ANDROID
        return useTestAd ? AndroidTestRewardAdUnitId : androidRewardAdUnitId;
#elif UNITY_IOS
        return useTestAd ? IosTestRewardAdUnitId : iosRewardAdUnitId;
#else
        return string.Empty;
#endif
    }

    private bool IsNetworkDisconnected()
    {
        return Application.internetReachability == NetworkReachability.NotReachable;
    }

    private void ShowLoadingUI()
    {
        if (adLoadingObject != null)
            adLoadingObject.SetActive(true);

        if (adLoadFailedObject != null)
            adLoadFailedObject.SetActive(false);
    }

    private void HideLoadingUI()
    {
        if (adLoadingObject != null)
            adLoadingObject.SetActive(false);
    }

    private void ShowLoadFailedUI()
    {
        if (adLoadFailedObject != null)
            adLoadFailedObject.SetActive(true);
    }

    private void HideFailUI()
    {
        if (adLoadFailedObject != null)
            adLoadFailedObject.SetActive(false);
    }

    private void ShowNonNetworkUI()
    {
        if (nonNetworkObject != null)
            nonNetworkObject.SetActive(true);

        LogAdError("Network Error", "인터넷 연결이 없어 광고를 불러올 수 없습니다.");
    }

    private void LogAdError(string title, string message)
    {
        string log = $"[{DateTime.Now:HH:mm:ss}] {title}\n{message}";

        Debug.LogWarning(log);

        if (adErrorLogText != null)
            adErrorLogText.text = log;
    }

    private void OnDestroy()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }
    }
}
