using UnityEngine;

public class PlayerVisualHandler : MonoBehaviour
{
    public enum WeaponAnchorType
    {
        ForwardPlayer,
        NearestEnemy,
    }

    [SerializeField] PlayerController pc;
    [SerializeField] float WeaponOffset;
    [SerializeField] private float weaponAnchorSpeed;

    [SerializeField] private Transform weaponAnchor;
    [SerializeField] private WeaponAnchorType weaponAnchorControlType;

    Collider2D[] overlapResult = new Collider2D[32];
    private Vector2 weaponAnchorTargetPosition;

    private void Start()
    {
        //pc.LookingChangeEvent.AddListener(ChangeLook);
        weaponAnchorTargetPosition = pc.lookingVector * WeaponOffset;
    }

    private void FixedUpdate()
    {
        switch (weaponAnchorControlType)
        {
            case WeaponAnchorType.ForwardPlayer:
                ChangeLook();
                break;
            case WeaponAnchorType.NearestEnemy:
                TrackNearestEnemy();
                weaponAnchor.localPosition = Vector2.MoveTowards(weaponAnchor.localPosition, weaponAnchorTargetPosition, weaponAnchorSpeed * Time.fixedDeltaTime);
                break;
        }
    }

    public void ChangeLook()
    {
        weaponAnchor.localPosition = pc.lookingVector * WeaponOffset;
        //Debug.Log("change direction");
    }
    void TrackNearestEnemy()
    {
        Vector2 origin = pc.transform.position;
        float overlapRange = 30f;
        int count = Physics2D.OverlapCircle(origin, overlapRange, ContactFilter2D.noFilter, results: overlapResult);

        Vector2? closestPoint = null;
        float closestDist = float.MaxValue;
        for (int i = 0; i < count; ++i)
        {
            if (overlapResult[i].TryGetComponent<IHitable>(out var hitable))
            {
                Vector2 p = hitable.GetTargetPoint;
                float dist = Vector2.Distance(origin, p);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPoint = p;
                }
            }
        }

        if(closestPoint.HasValue)
            weaponAnchorTargetPosition = (closestPoint.Value - origin).normalized * WeaponOffset;
    }
}
