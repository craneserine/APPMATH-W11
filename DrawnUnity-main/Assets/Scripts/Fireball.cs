using UnityEngine;
using System.Collections.Generic;

public class Fireball : MonoBehaviour
{
    private int ownerID;
    private Vector3 size;
    private float speed = 10f;
    private int fireballID;
    private bool isActive = true;
    private float lifetime = 3f;
    
    // Mesh rendering variables
    private Mesh fireballMesh;
    private Material fireballMaterial;
    private Matrix4x4 fireballMatrix;
    private Vector3 position;
    private Quaternion rotation;
    private Vector3 scale;

    public void Initialize(int ownerId, Vector3 colliderSize, Mesh mesh, Material material)
    {
        ownerID = ownerId;
        size = colliderSize;
        fireballMesh = mesh;
        fireballMaterial = material;
        
        // Initialize transform values
        position = transform.position;
        rotation = transform.rotation;
        scale = Vector3.one * 0.5f; // Smaller size for fireball
        
        fireballMatrix = Matrix4x4.TRS(position, rotation, scale);
        fireballID = CollisionManager.Instance.RegisterCollider(position, size, false);
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!isActive) return;

        // Move fireball forward
        position += transform.right * speed * Time.deltaTime;
        rotation = transform.rotation;
        fireballMatrix = Matrix4x4.TRS(position, rotation, scale);

        // Check for collisions
        if (CollisionManager.Instance.CheckCollision(fireballID, position, out List<int> collidedIds))
        {
            foreach (int id in collidedIds)
            {
                // Don't collide with owner
                if (id == ownerID) continue;

                // Check if we hit an enemy
                EnhancedMeshGenerator generator = FindFirstObjectByType<EnhancedMeshGenerator>();
                if (generator != null && generator.IsEnemy(id))
                {
                    generator.DestroyEnemy(id);
                }
            }
            
            // Destroy fireball on any collision
            DestroyFireball();
        }
        else
        {
            // Update position if no collision
            CollisionManager.Instance.UpdateCollider(fireballID, position, size);
            CollisionManager.Instance.UpdateMatrix(fireballID, fireballMatrix);
            
            // Render the fireball
            Graphics.DrawMesh(fireballMesh, fireballMatrix, fireballMaterial, 0);
        }
    }

    void DestroyFireball()
    {
        isActive = false;
        CollisionManager.Instance.RemoveCollider(fireballID);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (CollisionManager.Instance != null && isActive)
        {
            CollisionManager.Instance.RemoveCollider(fireballID);
        }
    }
}