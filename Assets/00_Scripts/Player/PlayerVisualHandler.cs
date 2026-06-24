using UnityEngine;

public class PlayerVisualHandler : MonoBehaviour
{
    [SerializeField] PlayerController pc;
    [SerializeField] float WeaponOffset;

    [SerializeField] private Transform weaponPosition;

    private void Start()
    {
        pc.LookingChangeEvent.AddListener(ChangeLook);
    }
    public void ChangeLook()
    {
        weaponPosition.localPosition = pc.lookingVector * WeaponOffset;
        //Debug.Log("change direction");
    }
}
