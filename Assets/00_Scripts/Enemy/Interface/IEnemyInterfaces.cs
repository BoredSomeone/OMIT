using UnityEngine;

public interface IHitable
{
    public Collider2D getCollider { get; }
    public Vector2 GetTargetPoint { get; }
    public void Hit(int damage);
}


public interface EnemyDatas
{
    public int maxHP { get; }
    public int nowHP { get; }

    public void moveSpeedRatioChange(float speedRatio);
}