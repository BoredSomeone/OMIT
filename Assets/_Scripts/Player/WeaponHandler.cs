using System.Collections;
using System.Linq.Expressions;
using UnityEngine;

public class WeaponContoller : MonoBehaviour
{
    [SerializeField] PlayerControlDataSO _playerControlData;
    [SerializeField] private float _attackPerSec;
    [SerializeField] private float _attackAngle;

    private float _baseWeaponSpeed;


    private Coroutine attackCoroutine;
    private Coroutine moveCoroutine;

    private void Start()
    {
        if (_attackPerSec <= 0)
            Debug.LogError("_attackPerSec is 0!!!");

        _baseWeaponSpeed = _playerControlData.baseWeaponSpeed;
    }

    IEnumerator Attack()
    {
        while (true)
        {

            yield return new WaitForSeconds(1 / _attackPerSec);
        }
    }

    void targetting()
    {

    }
}
