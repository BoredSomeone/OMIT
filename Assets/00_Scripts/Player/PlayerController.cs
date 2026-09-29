using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D myrb;
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] private PlayerDefaultStatDataSO _playerControlData;
    [SerializeField] private UpgradeDataSO _upgradeData;

    [SerializeField] private float _acceleration = 1;
    [SerializeField] private float _breakDamping;

    [SerializeField] private Vector2 _lookingVector;
    [SerializeField, ReadOnly] private Vector2 targetVector;

    private InputAction upAction;
    private InputAction downAction;
    private InputAction leftAction;
    private InputAction rightAction;

    private Vector2 lastInput = Vector2.zero;

    public float maxSpeed => _upgradeData.GetCached(UpgradeDataSO.StatType.MoveSpeed);
    public Vector2 lookingVector => _lookingVector;

    public UnityEvent LookingChangeEvent;

    private void Start()
    {
        _acceleration = _playerControlData.baseAcceleration;
        _breakDamping = _playerControlData.baseBreakDamping;

        upAction = actionAsset.FindAction("Player/Up");
        downAction = actionAsset.FindAction("Player/Down");
        leftAction = actionAsset.FindAction("Player/Left");
        rightAction = actionAsset.FindAction("Player/Right");

        upAction.performed      += OnUpPerformed;
        downAction.performed    += OnDownPerformed;
        leftAction.performed    += OnLeftPerformed;
        rightAction.performed   += OnRightPerformed;

        upAction.canceled       += OnUpCanceled;
        downAction.canceled     += OnDownCanceled;
        leftAction.canceled     += OnLeftCanceled;
        rightAction.canceled    += OnRightCanceled;
    }

    private void OnDestroy()
    {
        upAction.performed      -= OnUpPerformed;
        downAction.performed    -= OnDownPerformed;
        leftAction.performed    -= OnLeftPerformed;
        rightAction.performed   -= OnRightPerformed;

        upAction.canceled       -= OnUpCanceled;
        downAction.canceled     -= OnDownCanceled;
        leftAction.canceled     -= OnLeftCanceled;
        rightAction.canceled    -= OnRightCanceled;
    }

    private void OnUpPerformed(InputAction.CallbackContext _)    { lastInput.y = 1; UpdateInput(); }
    private void OnDownPerformed(InputAction.CallbackContext _)  { lastInput.y = -1; UpdateInput(); }
    private void OnLeftPerformed(InputAction.CallbackContext _)  { lastInput.x = -1; UpdateInput(); }
    private void OnRightPerformed(InputAction.CallbackContext _) { lastInput.x = 1; UpdateInput(); }

    private void OnUpCanceled(InputAction.CallbackContext _)     { lastInput.y = downAction.IsPressed() ? -1 : 0; UpdateInput(); }
    private void OnDownCanceled(InputAction.CallbackContext _)   { lastInput.y = upAction.IsPressed() ? 1 : 0; UpdateInput(); }
    private void OnLeftCanceled(InputAction.CallbackContext _)   { lastInput.x = rightAction.IsPressed() ? 1 : 0; UpdateInput(); }
    private void OnRightCanceled(InputAction.CallbackContext _)  { lastInput.x = leftAction.IsPressed() ? -1 : 0; UpdateInput(); }

    private void FixedUpdate()
    {
        Move();
    }

    [Button()]
    private void UpdateForSO()
    {
        _breakDamping = _playerControlData.baseBreakDamping;
    }

    private void UpdateInput()
    {
        targetVector = lastInput;

        if (targetVector == Vector2.zero)
            myrb.linearDamping = _breakDamping;
        else
            myrb.linearDamping = 0;
    }

    private void Move()
    {
        if (targetVector != Vector2.zero)
        {
            var force = targetVector * _acceleration;

            if (Mathf.Abs(myrb.linearVelocityX) >= maxSpeed && Mathf.Sign(force.x) == Mathf.Sign(myrb.linearVelocityX))
                force.x = 0;

            if (Mathf.Abs(myrb.linearVelocityY) >= maxSpeed && Mathf.Sign(force.y) == Mathf.Sign(myrb.linearVelocityY))
                force.y = 0;

            myrb.AddForce(force);
            if (_lookingVector != targetVector)
            {
                _lookingVector = targetVector;
                LookingChangeEvent.Invoke();
            }
        }
    }
}
