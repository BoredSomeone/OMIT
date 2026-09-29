using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UIStackManager로 여닫는 모달 UI 패널.
/// 열기/닫기, 상호작용 가능 여부, 선택(포커스) 기억을 담당한다. 직접 SetActive 하지 말고 UIStackManager를 통해 여닫을 것.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIPanel : MonoBehaviour
{
    [SerializeField] private Selectable firstSelected;
    [SerializeField] private bool _pauseGame = true;
    [SerializeField] private bool _closeOnCancel = true;

    private CanvasGroup canvasGroup;
    private GameObject lastSelected;

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

    /// <summary>패널을 활성화하고 상호작용 가능 상태로 만든 뒤 첫 선택 대상을 선택 (UIStackManager 전용)</summary>
    public void Open()
    {
        lastSelected = null;
        gameObject.SetActive(true);
        SetInteractable(true);
        Focus();
    }

    /// <summary>패널을 비활성화 (UIStackManager 전용)</summary>
    public void Close()
    {
        gameObject.SetActive(false);
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

    /// <summary>대상이 활성 상태이고 상호작용 가능한 Selectable인지 확인</summary>
    private static bool IsSelectable(GameObject target)
    {
        return target != null
            && target.activeInHierarchy
            && target.TryGetComponent<Selectable>(out var selectable)
            && selectable.IsInteractable();
    }
}
