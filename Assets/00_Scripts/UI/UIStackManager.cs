using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 모달 UI 패널(UIPanel)을 스택으로 관리.
/// 맨 위 패널만 상호작용/선택 가능하며, 스택 상태에 따라 일시정지와 액션 맵(비었으면 Player, 아니면 UI)을 전환한다.
/// </summary>
public class UIStackManager : MonoBehaviour
{
    [SerializeField] private PauseManagerSO pauseManager;
    [SerializeField] private InputManagerSO inputManager;

    [SerializeField, ReadOnly] private List<UIPanel> _stack = new();

    /// <summary>맨 위 패널이 closeOnCancel=false일 때 UI/Cancel 입력이 들어오면 호출 (인자: 맨 위 패널)</summary>
    public UnityEvent<UIPanel> unhandledCancelEvent = new();

    private InputAction cancelAction;

    public UIPanel top { get { return _stack.Count > 0 ? _stack[_stack.Count - 1] : null; } }
    public int count { get { return _stack.Count; } }


    private void Awake()
    {
        inputManager.SwitchMap(InputManagerSO.InputMapType.Player);
    }

    private void Start()
    {
        cancelAction = inputManager.FindAction("UI/Cancel");
        cancelAction.performed += OnCancelPerformed;
    }

    private void OnDestroy()
    {
        if (cancelAction != null)
            cancelAction.performed -= OnCancelPerformed;
    }

    /// <summary>맨 위 패널의 선택을 유지. 빈 곳 클릭 등으로 선택이 풀리면 같은 프레임에 되돌림</summary>
    private void LateUpdate()
    {
        var panel = top;
        var eventSystem = EventSystem.current;
        if (panel == null || eventSystem == null)
            return;

        if (!panel.TryRememberSelection(eventSystem.currentSelectedGameObject))
            panel.Focus();
    }

    /// <summary>패널이 스택에 있는지 확인</summary>
    public bool Contains(UIPanel panel)
    {
        return _stack.Contains(panel);
    }

    /// <summary>패널을 스택 맨 위에 열고, 기존 맨 위 패널은 상호작용 불가로 만듦</summary>
    public void Push(UIPanel panel)
    {
        if (panel == null || _stack.Contains(panel))
        {
            Debug.LogWarning($"UIStackManager: 열 수 없는 패널입니다. ({(panel ? panel.name : "null")})");
            return;
        }

        var prevTop = top;
        if (prevTop != null)
            prevTop.SetInteractable(false);

        _stack.Add(panel);
        panel.Open();

        if (panel.pauseGame)
            pauseManager.Pause();
        if (_stack.Count == 1)
            inputManager.SwitchMap(InputManagerSO.InputMapType.UI);
    }

    /// <summary>맨 위 패널을 닫음</summary>
    public void Pop()
    {
        Close(top);
    }

    /// <summary>지정한 패널을 스택에서 제거하고 닫음. 맨 위였다면 다음 패널을 상호작용 가능하게 하고 선택을 복원</summary>
    public void Close(UIPanel panel)
    {
        int index = panel == null ? -1 : _stack.IndexOf(panel);
        if (index < 0)
            return;

        bool wasTop = index == _stack.Count - 1;
        _stack.RemoveAt(index);
        panel.Close();

        if (panel.pauseGame)
            pauseManager.Play();

        var nowTop = top;
        if (wasTop && nowTop != null)
        {
            nowTop.SetInteractable(true);
            nowTop.Focus();
        }

        if (_stack.Count == 0)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            inputManager.SwitchMap(InputManagerSO.InputMapType.Player);
        }
    }

    /// <summary>UI/Cancel(ESC) 입력 콜백. 맨 위 패널을 닫거나, 닫을 수 없는 패널이면 unhandledCancelEvent 호출</summary>
    private void OnCancelPerformed(InputAction.CallbackContext _)
    {
        var panel = top;
        if (panel == null)
            return;

        if (panel.closeOnCancel)
            Close(panel);
        else
            unhandledCancelEvent?.Invoke(panel);
    }
}
