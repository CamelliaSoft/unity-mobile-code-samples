using System;
using System.Collections;
using UnityEngine;
using Newtonsoft.Json;
using BackEnd;

/// <summary>
/// 로컬 저장(PlayerPrefs)과 뒤끝 CloudSave를 함께 사용하는 저장 예시입니다.
/// 
/// - 평소에는 로컬 저장으로 빠르게 저장/로드
/// - 유저가 직접 클라우드 저장을 누르면 뒤끝에 업로드
/// - 클라우드 불러오기 후에는 로컬에도 다시 저장하여 데이터 흐름을 맞춤
/// - 앱 업데이트로 배열 길이나 신규 변수가 바뀌어도 기존 유저 데이터가 깨지지 않도록 보정
/// </summary>
public class LocalAndBackendSaveExample : MonoBehaviour
{
    private const string LocalSaveKey = "LOCAL_SAVE_DATA";
    private const string CloudSaveCollectionName = "USER_DATA";

    [Header("테스트용 현재 데이터")]
    [SerializeField] private double money;
    [SerializeField] private int diamond;
    [SerializeField] private int specLevel;
    [SerializeField] private int companyLevel;
    [SerializeField] private bool removeAds;

    [Header("UI")]
    [SerializeField] private GameObject saveLoadingObject;
    [SerializeField] private GameObject loadLoadingObject;
    [SerializeField] private GameObject saveSuccessObject;
    [SerializeField] private GameObject loadSuccessObject;
    [SerializeField] private GameObject failObject;

    [SerializeField] private UnityEngine.UI.Text failReasonText;

    [Header("Save Cooldown")]
    [SerializeField] private int cloudSaveCooldownSecond = 3600;
    [SerializeField] private int cloudLoadCooldownSecond = 3600;

    private bool isCloudSaveCooldown;
    private bool isCloudLoadCooldown;

    private SaveDataExample currentData;

    private void Start()
    {
        LoadLocalData();
    }

    /// <summary>
    /// 현재 게임 상태를 SaveDataExample로 묶습니다.
    /// 실제 프로젝트에서는 각 시스템의 레벨, 재화, 광고 쿨타임, 결제 상태 등을 모두 넣습니다.
    /// </summary>
    private SaveDataExample CaptureCurrentData()
    {
        SaveDataExample data = SaveDataExample.CreateDefault();

        data.money = money;
        data.diamond = diamond;
        data.specLevel = specLevel;
        data.companyLevel = companyLevel;
        data.removeAds = removeAds;

        data.lastPlayTime = DateTime.Now.ToString("O");
        data.saveVersion = SaveDataExample.CurrentSaveVersion;

        return data;
    }

    /// <summary>
    /// 저장 데이터를 현재 게임 상태에 반영합니다.
    /// </summary>
    private void ApplyData(SaveDataExample data)
    {
        if (data == null)
            return;

        data.FixLegacyData();

        money = data.money;
        diamond = data.diamond;
        specLevel = data.specLevel;
        companyLevel = data.companyLevel;
        removeAds = data.removeAds;

        currentData = data;
    }

    /// <summary>
    /// 로컬 저장입니다.
    /// 자동 저장, 앱 종료 전 저장, 클라우드 불러오기 후 로컬 반영에 사용합니다.
    /// </summary>
    public void SaveLocalData()
    {
        SaveDataExample data = CaptureCurrentData();

        string json = JsonConvert.SerializeObject(data);
        PlayerPrefs.SetString(LocalSaveKey, json);
        PlayerPrefs.Save();

        currentData = data;

        Debug.Log("[Local Save] 저장 완료");
    }

    /// <summary>
    /// 로컬 저장 데이터를 불러옵니다.
    /// 저장 데이터가 없거나 파싱에 실패하면 기본값을 사용합니다.
    /// </summary>
    public void LoadLocalData()
    {
        if (!PlayerPrefs.HasKey(LocalSaveKey))
        {
            ApplyData(SaveDataExample.CreateDefault());
            SaveLocalData();
            return;
        }

        try
        {
            string json = PlayerPrefs.GetString(LocalSaveKey);
            SaveDataExample data = JsonConvert.DeserializeObject<SaveDataExample>(json);

            if (data == null)
            {
                ApplyData(SaveDataExample.CreateDefault());
                return;
            }

            data.FixLegacyData();
            ApplyData(data);

            Debug.Log("[Local Load] 불러오기 완료");
        }
        catch (Exception e)
        {
            Debug.LogError("[Local Load] 불러오기 실패: " + e.Message);

            ApplyData(SaveDataExample.CreateDefault());
            ShowFail("로컬 데이터를 불러오지 못해 기본 데이터로 시작합니다.");
        }
    }

    /// <summary>
    /// 뒤끝 CloudSave에 데이터를 저장합니다.
    /// 네트워크 연결, 중복 저장 방지, 쿨타임을 함께 체크합니다.
    /// </summary>
    public void SaveCloudData()
    {
        if (isCloudSaveCooldown)
        {
            ShowFail("클라우드 저장 쿨타임이 남아 있습니다.");
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowFail("인터넷 연결을 확인해주세요.");
            return;
        }

        StartCoroutine(SaveCloudDataCoroutine());
    }

    private IEnumerator SaveCloudDataCoroutine()
    {
        SetActive(saveLoadingObject, true);

        SaveDataExample data = CaptureCurrentData();
        string json = JsonConvert.SerializeObject(data);

        int byteSize = System.Text.Encoding.UTF8.GetByteCount(json);
        Debug.Log("[Cloud Save] JSON Size: " + byteSize + " bytes");

        var bro = Backend.CloudSave.Upload(CloudSaveCollectionName, json);

        yield return null;

        SetActive(saveLoadingObject, false);

        if (bro.IsSuccess())
        {
            currentData = data;

            SetActive(saveSuccessObject, true);
            SaveLocalData();

            StartCoroutine(StartCloudSaveCooldown());

            Debug.Log("[Cloud Save] 저장 성공");
        }
        else
        {
            string message = $"클라우드 저장 실패\nStatus: {bro.GetStatusCode()}\nError: {bro.GetErrorCode()}\nMessage: {bro.GetMessage()}";
            Debug.LogError(message);
            ShowFail("클라우드 저장에 실패했습니다.\n인터넷 상태를 확인하고 다시 시도해주세요.");
        }
    }

    /// <summary>
    /// 뒤끝 CloudSave에서 데이터를 불러옵니다.
    /// 성공하면 현재 게임 상태에 적용한 뒤 로컬 저장에도 반영합니다.
    /// </summary>
    public void LoadCloudData()
    {
        if (isCloudLoadCooldown)
        {
            ShowFail("클라우드 불러오기 쿨타임이 남아 있습니다.");
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowFail("인터넷 연결을 확인해주세요.");
            return;
        }

        StartCoroutine(LoadCloudDataCoroutine());
    }

    private IEnumerator LoadCloudDataCoroutine()
    {
        SetActive(loadLoadingObject, true);

        var bro = Backend.CloudSave.Download(CloudSaveCollectionName);

        yield return null;

        SetActive(loadLoadingObject, false);

        if (bro.IsSuccess())
        {
            string json = bro.ReturnValue;

            if (string.IsNullOrEmpty(json))
            {
                ShowFail("저장된 클라우드 데이터를 찾을 수 없습니다.");
                yield break;
            }

            try
            {
                SaveDataExample data = JsonConvert.DeserializeObject<SaveDataExample>(json);

                if (data == null)
                {
                    ShowFail("클라우드 데이터를 불러오지 못했습니다.");
                    yield break;
                }

                data.FixLegacyData();
                ApplyData(data);

                // 클라우드 데이터를 정상 적용한 뒤 로컬 저장도 갱신합니다.
                SaveLocalData();

                SetActive(loadSuccessObject, true);
                StartCoroutine(StartCloudLoadCooldown());

                Debug.Log("[Cloud Load] 불러오기 성공");
            }
            catch (Exception e)
            {
                Debug.LogError("[Cloud Load] 데이터 변환 실패: " + e.Message);
                ShowFail("클라우드 데이터 변환 중 문제가 발생했습니다.");
            }
        }
        else
        {
            string message = $"클라우드 불러오기 실패\nStatus: {bro.GetStatusCode()}\nError: {bro.GetErrorCode()}\nMessage: {bro.GetMessage()}";
            Debug.LogError(message);
            ShowFail("클라우드 불러오기에 실패했습니다.\n인터넷 상태를 확인하고 다시 시도해주세요.");
        }
    }

    private IEnumerator StartCloudSaveCooldown()
    {
        isCloudSaveCooldown = true;

        yield return new WaitForSeconds(cloudSaveCooldownSecond);

        isCloudSaveCooldown = false;
    }

    private IEnumerator StartCloudLoadCooldown()
    {
        isCloudLoadCooldown = true;

        yield return new WaitForSeconds(cloudLoadCooldownSecond);

        isCloudLoadCooldown = false;
    }

    private void ShowFail(string message)
    {
        SetActive(failObject, true);

        if (failReasonText != null)
            failReasonText.text = message;
    }

    private void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            SaveLocalData();
    }

    private void OnApplicationQuit()
    {
        SaveLocalData();
    }
}
