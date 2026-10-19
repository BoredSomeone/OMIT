using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UIStackManager로 여닫는 모달 UI 패널.
/// 열기/닫기, 일시정지, 상호작용 가능 여부, 선택(포커스) 기억을 담당한다.
/// 직접 SetActive 하지 말 것. 열기는 UIStackManager.Push, 닫기는 RequestClose(버튼 연결용) 또는 UIStackManager.Close를 사용.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIPanel : MonoBehaviour
{
    [InfoBox("Pause Game이 켜져 있으면 PauseManagerSO를 연결해야 합니다.", InfoMessageType.Error, "IsPauseManagerMissing")]
    [SerializeField] private PauseManagerSO pauseManager;
    [SerializeField] private Selectable firstSelected;
    [SerializeField] private bool _pauseGame = true;
    [SerializeField] private bool _closeOnCancel = true;

    private CanvasGroup canvasGroup;
    private GameObject lastSelected;

    /// <summary>이 패널을 열어 둔 스택. 스택 밖에 있으면 null (UIStackManager가 설정)</summary>
    internal UIStackManager ownerStack { get; set; }

    /// <summary>이 패널이 열려 있는 동안 게임을 일시정지하는지 여부</summary>
    public bool pauseGame { get { return _pauseGame; } }

    /// <summary>UI/Cancel(ESC) 입력으로 닫히는지 여부. false면 UIStackManager.unhandledCancelEvent가 호출됨</summary>
    public bool closeOnCancel { get { return _closeOnCancel; } }

    private CanvasGroup Group
    {
        get
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            return canvasGroup;
        }
    }

    /// <summary>
    /// 이 패널을 닫아 달라고 소속 스택에 요청 (버튼 On Click 연결용).
    /// 스택이 정리, 일시정지 해제, 액션 맵 전환, 선택 복원을 처리한다. 스택 밖의 패널이면 경고만 남김
    /// </summary>
    public void RequestClose()
    {
        if (ownerStack == null)
        {
            Debug.LogWarning($"UIPanel: 스택에 열려 있지 않은 패널입니다. UIStackManager.Push로 연 패널만 닫을 수 있습니다. ({name})", this);
            return;
        }

        ownerStack.Close(this);
    }

    /// <summary>패널을 활성화하고 상호작용 가능 상태로 만든 뒤 첫 선택 대상을 선택. pauseGame이면 일시정지 요청 (UIStackManager 전용)</summary>
    internal void Open()
    {
        lastSelected = null;
        gameObject.SetActive(true);
        SetInteractable(true);
        if (ShouldControlPause())
            pauseManager.Pause();
        Focus();
    }

    /// <summary>패널을 비활성화. pauseGame이면 일시정지 요청 해제 (UIStackManager 전용)</summary>
    internal void Close()
    {
        gameObject.SetActive(false);
        if (ShouldControlPause())
            pauseManager.Play();
    }

    /// <summary>패널 전체의 상호작용(버튼 입력, 레이캐스트) 가능 여부를 설정</summary>
    public void SetInteractable(bool interactable)
    {
        Group.interactable = interactable;
        Group.blocksRaycasts = interactable;
    }

    /// <summary>마지막으로 선택했던 대상을, 없으면 첫 선택 대상을 선택. 선택 가능한 대상이 없으면 패널 밖의 선택을 해제</summary>
    public void Focus()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
            return;

        GameObject target = null;
        if (IsSelectable(lastSelected))
            target = lastSelected;
        else if (firstSelected != null && IsSelectable(firstSelected.gameObject))
            target = firstSelected.gameObject;

        var current = eventSystem.currentSelectedGameObject;
        if (target == null)
        {
            if (current != null && !current.transform.IsChildOf(transform))
                eventSystem.SetSelectedGameObject(null);
            return;
        }

        if (current != target)
            eventSystem.SetSelectedGameObject(target);
    }

    /// <summary>현재 선택된 대상이 이 패널 안에 있으면 마지막 선택으로 기억하고 true 반환</summary>
    public bool TryRememberSelection(GameObject selected)
    {
        if (selected == null || !selected.transform.IsChildOf(transform))
            return false;

        lastSelected = selected;
        return true;
    }

    /// <summary>pauseGame이 켜져 있는데 PauseManagerSO가 연결되지 않았는지 확인 (인스펙터 경고용)</summary>
    private bool IsPauseManagerMissing()
    {
        return _pauseGame && pauseManager == null;
    }

    /// <summary>일시정지 요청/해제를 해야 하는지 확인. 연결이 빠졌으면 콘솔 에러를 남기고 false 반환</summary>
    private bool ShouldControlPause()
    {
        if (!_pauseGame)
            return false;

        if (pauseManager == null)
        {
            Debug.LogError($"UIPanel: Pause Game이 켜져 있지만 PauseManagerSO가 연결되지 않았습니다. ({name})", this);
            return false;
        }
        return true;
    }

    /// <summary>대상이 활성 상태이고 상호작용 가능한 Selectable인지 확인</summary>
    private static bool IsSelectable(GameObject target)
    {
        return target != null
            && target.activeInHierarchy
            && target.TryGetComponent<Selectable>(out var selectable)
            && selectable.IsInteractable();
    }
}
