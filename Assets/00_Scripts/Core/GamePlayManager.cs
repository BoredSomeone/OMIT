using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class GamePlayManager : MonoBehaviour
{
    [SerializeField] private LevelManagerSO levelManager;
    [SerializeField] private PauseManagerSO pauseManager;
    [SerializeField] private TimeScaleManagerSO timeScaleManager;
    [SerializeField] private SettingManagerSO settingManager;
    [SerializeField] private LocalizedDataSO localizedData;
    [SerializeField] private InputManagerSO inputManager;

    [SerializeField] private UIStackManager uiStack;
    [SerializeField] private UIPanel pauseUI;
    [SerializeField] private UIPanel levelUpUI;

    [SerializeField, ReadOnly] private int _pendingLevelUpCount = 0;

    private InputAction pauseAction;


    private void Awake()
    {
        InitScene();
    }

    private void Start()
    {
        pauseAction = inputManager.FindAction("Player/Pause");
        pauseAction.performed += OnPausePerformed;

        levelManager.LevelUpEvent.AddListener(OnLevelUp);
        uiStack.unhandledCancelEvent.AddListener(OnUnhandledCancel);
    }

    private void OnDestroy()
    {
        if (pauseAction != null)
            pauseAction.performed -= OnPausePerformed;

        if (levelManager)
            levelManager.LevelUpEvent.RemoveListener(OnLevelUp);
        if (uiStack)
            uiStack.unhandledCancelEvent.RemoveListener(OnUnhandledCancel);
    }

    private void InitScene()
    {
        settingManager.Init();
        localizedData.ChangeLanguage(settingManager.NowLanguage).Forget();

        //저장 기능 완성되면 불러오기 기능 만들어야되요.
        levelManager.LevelSet(1);
        levelManager.ExpSet(0);

        timeScaleManager.ResetAll();
        pauseManager.ResetPauseCount();

        _pendingLevelUpCount = 0;
        pauseUI.gameObject.SetActive(false);
        levelUpUI.gameObject.SetActive(false);
    }

    /// <summary>Player/Pause(ESC) 입력 콜백. 게임 플레이 중(Player 맵)에만 호출됨</summary>
    private void OnPausePerformed(InputAction.CallbackContext _)
    {
        PauseByPlayer();
    }

    /// <summary>ESC로 닫히지 않는 패널(레벨업 UI 등)에서 UI/Cancel 입력 시 일시정지 UI를 연다</summary>
    private void OnUnhandledCancel(UIPanel _)
    {
        PauseByPlayer();
    }

    /// <summary>일시정지 UI를 스택에 연다. 이미 열려 있으면 무시</summary>
    private void PauseByPlayer()
    {
        if (uiStack.Contains(pauseUI))
            return;

        uiStack.Push(pauseUI);
    }

    /// <summary>일시정지 UI를 닫음 (버튼 연결용)</summary>
    public void ClosePauseUI()
    {
        uiStack.Close(pauseUI);
    }

    /// <summary>레벨업 이벤트 콜백. 대기 레벨업 수를 늘리고, 레벨업 UI가 닫혀 있으면 연다</summary>
    private void OnLevelUp()
    {
        ++_pendingLevelUpCount;
        if (uiStack.Contains(levelUpUI))
            return;

        uiStack.Push(levelUpUI);
    }

    /// <summary>레벨업 선택 완료 시 호출. 대기 레벨업이 남아 있으면 UI를 유지하고, 없으면 닫음. 레벨업 UI가 맨 위가 아니면 무시</summary>
    public void CloseLevelUpUI()
    {
        if (uiStack.top != levelUpUI)
            return;

        --_pendingLevelUpCount;
        if (_pendingLevelUpCount > 0)
            return;

        _pendingLevelUpCount = 0;
        uiStack.Close(levelUpUI);
    }
}
