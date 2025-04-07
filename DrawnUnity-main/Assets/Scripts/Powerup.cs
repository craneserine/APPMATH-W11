using UnityEngine;
using System.Collections.Generic;

public class Powerup : MonoBehaviour
{
    public enum PowerupType
    {
        Fireball,
        ExtraLife,
        Invincibility
    }

    public PowerupType type;
    private int powerupID;
    private Vector3 size = Vector3.one;

    void Start()
    {
        powerupID = CollisionManager.Instance.RegisterCollider(transform.position, size, false);
    }

    void Update()
    {
        // Update collider position
        CollisionManager.Instance.UpdateCollider(powerupID, transform.position, size);

        // Check for player collision
        if (CollisionManager.Instance.CheckCollision(powerupID, transform.position, out List<int> collidedIds))
        {
            foreach (int id in collidedIds)
            {
                EnhancedMeshGenerator generator = FindFirstObjectByType<EnhancedMeshGenerator>();
                if (generator != null && generator.IsPlayer(id))
                {
                    ApplyPowerup(generator);
                    DestroyPowerup();
                    return;
                }
            }
        }
    }

    void ApplyPowerup(EnhancedMeshGenerator generator)
    {
        switch (type)
        {
            case PowerupType.Fireball:
                generator.CollectFireballPowerup();
                break;
            case PowerupType.ExtraLife:
                generator.CollectExtraLife();
                break;
            case PowerupType.Invincibility:
                generator.CollectInvincibility();
                break;
        }
    }

    void DestroyPowerup()
    {
        CollisionManager.Instance.RemoveCollider(powerupID);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (CollisionManager.Instance != null)
        {
            CollisionManager.Instance.RemoveCollider(powerupID);
        }
    }
}