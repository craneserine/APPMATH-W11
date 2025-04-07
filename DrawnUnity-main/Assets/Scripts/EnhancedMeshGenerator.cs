using System.Collections.Generic;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;
using TMPro;

// Enhanced MeshGenerator with collision, player control, and camera following
public class EnhancedMeshGenerator : MonoBehaviour
{
    public Material material;
    public Material fireballMaterial;
    private Mesh fireballMesh;
    public int instanceCount = 100;
    private Mesh cubeMesh;
    private List<Matrix4x4> matrices = new List<Matrix4x4>();
    private List<int> colliderIds = new List<int>();
    
    [Header("Box Dimensions")]
    public float width = 1f;
    public float height = 1f;
    public float depth = 1f;
    
    [Header("Player Movement")]
    public float movementSpeed = 5f;
    public float gravity = 9.8f;
    
    private int playerID = -1;
    private Vector3 playerVelocity = Vector3.zero;
    private bool isGrounded = false;
    
    [Header("Camera")]
    public PlayerCameraFollow cameraFollow;
    
    [Header("World Settings")]
    public float constantZPosition = 0f;
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -50f;
    public float maxY = 50f;
    public float groundY = -20f;
    public float groundWidth = 200f;
    public float groundDepth = 200f;

    [Header("Jump Settings")]
    public float jumpForce = 12f;
    public float fastFallMultiplier = 2f;
    public float lowJumpMultiplier = 2f;
    
    [Header("Player Stats")]
    private int playerLives = 3;
    private bool isInvincible = false;
    private float invincibilityTimer = 0f;
    public float invincibilityDuration = 3f;
    
    [Header("Powerups")]
    public GameObject fireballPrefab;
    private bool hasFireballPowerup = false;
    private float fireballCooldown = 0f;
    
    [Header("Game State")]
    private float gameTimer = 20f;
    private Vector3 endGoalPosition;
    private bool gameEnded = false;
    
    [Header("UI References")]
    public TMP_Text livesText;
    public TMP_Text timerText;
    
    [Header("Enemies")]
    private List<int> enemyIds = new List<int>();
    private List<float> enemyDirections = new List<float>();
    public float enemySpeed = 2f;
    
    private List<int> instakillObstacleIds = new List<int>();
    private List<int> powerupIds = new List<int>();

    void Start()
    {
        SetupCamera();
        CreateCubeMesh();
        CreatePlayer();
        CreateGround();
        GenerateRandomBoxes();
        UpdateUI();
        CreateEndGoal();
        CreateEnemies(5);
        CreateInstakillObstacles(10);
        CreatePowerups(5);
        CreateFireballMesh();
    }

    void Update()
    {
        if (gameEnded) return;
        
        UpdatePlayer();
        RenderBoxes();
        UpdateGameTimer();
        UpdatePowerups();
        UpdateEnemies();
        CheckEndGoal();
        UpdateCameraVisibility();
    }


    void UpdatePlayer()
    {
        if (playerID == -1) return;
        
        Matrix4x4 playerMatrix = matrices[colliderIds.IndexOf(playerID)];
        DecomposeMatrix(playerMatrix, out Vector3 pos, out Quaternion rot, out Vector3 scale);
        
        // Handle invincibility
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0)
            {
                isInvincible = false;
            }
        }
        
        // Reset velocity.y when grounded
        if (isGrounded)
        {
            playerVelocity.y = 0;

            // Jump input
            if (Input.GetKeyDown(KeyCode.Space))
            {
                playerVelocity.y = jumpForce; // Fast jump
                isGrounded = false;
            }
        }
        else
        {
            // Apply modified gravity for better jump feel
            if (playerVelocity.y < 0)
            {
                // Falling down - apply faster gravity
                playerVelocity.y -= gravity * fastFallMultiplier * Time.deltaTime;
            }
            else if (playerVelocity.y > 0 && !Input.GetKey(KeyCode.Space))
            {
                // Jump button released - apply lower jump
                playerVelocity.y -= gravity * lowJumpMultiplier * Time.deltaTime;
            }
            else
            {
                // Normal gravity
                playerVelocity.y -= gravity * Time.deltaTime;
            }
        }

        
        // Apply modified gravity for better jump feel
        if (playerVelocity.y < 0)
        {
            // Falling down - apply faster gravity
            playerVelocity.y -= gravity * fastFallMultiplier * Time.deltaTime;
        }
        else if (playerVelocity.y > 0 && !Input.GetKey(KeyCode.Space))
        {
            // Jump button released - apply lower jump
            playerVelocity.y -= gravity * lowJumpMultiplier * Time.deltaTime;
        }
        else
        {
            // Normal gravity
            playerVelocity.y -= gravity * Time.deltaTime;
        }
        
        // Get horizontal input
        float horizontal = 0;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1;
        if (Input.GetKey(KeyCode.D)) horizontal += 1;
        
        // Fireball input
        if (Input.GetKeyDown(KeyCode.F) && hasFireballPowerup && fireballCooldown <= 0)
        {
            ShootFireball();
            fireballCooldown = 1f;
        }
        
        // Calculate current movement speed (halved in air)
        float currentSpeed = isGrounded ? movementSpeed : movementSpeed * 0.5f;
        
        // Update player position based on input
        Vector3 newPos = pos;
        newPos.x += horizontal * currentSpeed * Time.deltaTime;
        
        // Apply horizontal movement if no collision
        if (!CheckCollisionAt(playerID, new Vector3(newPos.x, pos.y, pos.z)))
        {
            pos.x = newPos.x;
        }
        
        // Apply gravity/vertical movement
        newPos = pos;
        newPos.y += playerVelocity.y * Time.deltaTime;
        
        // Check for vertical collisions
        if (CheckCollisionAt(playerID, new Vector3(pos.x, newPos.y, pos.z), out List<int> collidedIds))
        {
            // Check if we hit an enemy or instakill obstacle
            foreach (int id in collidedIds)
            {
                if (enemyIds.Contains(id))
                {
                    if (isInvincible)
                    {
                        // Kill the enemy
                        DestroyEnemy(id);
                    }
                    else
                    {
                        // Take damage
                        TakeDamage();
                    }
                }
                else if (instakillObstacleIds.Contains(id))
                {
                    // Instakill
                    InstaKill();
                    return;
                }
            }
            
            // We hit something below or above
            if (playerVelocity.y < 0)
            {
                // We hit something below
                isGrounded = true;
            }
            playerVelocity.y = 0;
        }
        else
        {
            // No collision, apply movement
            pos.y = newPos.y;
            isGrounded = false;
        }
        
        // Update matrix
        Matrix4x4 newMatrix = Matrix4x4.TRS(pos, rot, scale);
        matrices[colliderIds.IndexOf(playerID)] = newMatrix;
        
        // Update collider position
        CollisionManager.Instance.UpdateCollider(playerID, pos, new Vector3(width * scale.x, height * scale.y, depth * scale.z));
        CollisionManager.Instance.UpdateMatrix(playerID, newMatrix);
        
        // Update camera to follow player
        if (cameraFollow != null)
        {
            cameraFollow.SetPlayerPosition(pos);
        }
    }

    public bool IsPlayer(int id)
    {
        return id == playerID;
    }

    public bool IsEnemy(int id)
    {
        return enemyIds.Contains(id);
    }

void CreatePowerups(int count)
{
    for (int i = 0; i < count; i++)
    {
        Vector3 position = new Vector3(
            Random.Range(minX, maxX),
            Random.Range(groundY + 1, maxY),
            constantZPosition
        );

        GameObject powerupObj = new GameObject("Powerup");
        powerupObj.transform.position = position;

        Powerup powerup = powerupObj.AddComponent<Powerup>();
        powerup.type = (Powerup.PowerupType)Random.Range(0, 3); // Randomly assign powerup type

        int id = CollisionManager.Instance.RegisterCollider(
            position, 
            Vector3.one, 
            false);
        
        powerupIds.Add(id);
    }
}
    
    void ShootFireball()
    {
        if (fireballPrefab == null || fireballMesh == null) return;
        
        Matrix4x4 playerMatrix = matrices[colliderIds.IndexOf(playerID)];
        DecomposeMatrix(playerMatrix, out Vector3 pos, out Quaternion rot, out Vector3 scale);
        
        GameObject fireball = Instantiate(fireballPrefab, pos, rot);
        Fireball fireballScript = fireball.GetComponent<Fireball>();
        if (fireballScript != null)
        {
            fireballScript.Initialize(
                playerID, 
                new Vector3(width * scale.x, height * scale.y, depth * scale.z),
                fireballMesh,
                fireballMaterial
            );
        }
    }

    void CreateEndGoal()
    {
        Vector3 goalPosition = new Vector3(
            Random.Range(minX + 50, maxX - 50),
            Random.Range(minY + 10, maxY - 10),
            constantZPosition
        );
        
        Vector3 goalScale = new Vector3(3f, 3f, 3f);
        Quaternion goalRotation = Quaternion.identity;
        
        int goalId = CollisionManager.Instance.RegisterCollider(
            goalPosition, 
            new Vector3(width * goalScale.x, height * goalScale.y, depth * goalScale.z), 
            false);
        
        Matrix4x4 goalMatrix = Matrix4x4.TRS(goalPosition, goalRotation, goalScale);
        matrices.Add(goalMatrix);
        colliderIds.Add(goalId);
        
        CollisionManager.Instance.UpdateMatrix(goalId, goalMatrix);
        
        endGoalPosition = goalPosition;
    }
    
    void UpdateEnemies()
    {
        for (int i = 0; i < enemyIds.Count; i++)
        {
            int id = enemyIds[i];
            int index = colliderIds.IndexOf(id);
            if (index == -1) continue;

            Matrix4x4 enemyMatrix = matrices[index];
            DecomposeMatrix(enemyMatrix, out Vector3 pos, out Quaternion rot, out Vector3 scale);

            // Move enemy left/right
            float direction = enemyDirections[i];
            Vector3 newPos = pos;
            newPos.x += direction * enemySpeed * Time.deltaTime;

            // Check for collision or bounds to change direction
            if (CheckCollisionAt(id, newPos) || newPos.x < minX || newPos.x > maxX)
            {
                direction *= -1;
                enemyDirections[i] = direction;
            }
            else
            {
                pos = newPos;
            }

            // Update matrix
            Matrix4x4 newMatrix = Matrix4x4.TRS(pos, rot, scale);
            matrices[index] = newMatrix;
            CollisionManager.Instance.UpdateCollider(id, pos, new Vector3(width * scale.x, height * scale.y, depth * scale.z));
            CollisionManager.Instance.UpdateMatrix(id, newMatrix);
        }
    }

    
    void CreateInstakillObstacles(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 position = new Vector3(
                Random.Range(minX, maxX),
                Random.Range(groundY + 1, maxY),
                constantZPosition
            );
            
            Vector3 scale = new Vector3(
                Random.Range(0.5f, 2f),
                Random.Range(0.5f, 2f),
                Random.Range(0.5f, 2f)
            );
            
            Quaternion rotation = Quaternion.identity;
            
            int id = CollisionManager.Instance.RegisterCollider(
                position, 
                new Vector3(width * scale.x, height * scale.y, depth * scale.z), 
                false);
            
            Matrix4x4 obstacleMatrix = Matrix4x4.TRS(position, rotation, scale);
            matrices.Add(obstacleMatrix);
            colliderIds.Add(id);
            instakillObstacleIds.Add(id);
            
            CollisionManager.Instance.UpdateMatrix(id, obstacleMatrix);
        }
    }

    
    public void DestroyEnemy(int id)
{
    int index = enemyIds.IndexOf(id);
    if (index != -1)
    {
        enemyIds.RemoveAt(index);
        enemyDirections.RemoveAt(index);
    }
    
    index = colliderIds.IndexOf(id);
    if (index != -1)
    {
        matrices.RemoveAt(index);
        colliderIds.RemoveAt(index);
    }
    
    CollisionManager.Instance.RemoveCollider(id);
}

    void TakeDamage()
    {
        if (isInvincible) return;
        
        playerLives--;
        UpdateUI();
        
        if (playerLives <= 0)
        {
            GameOver(false);
        }
        else
        {
            // Brief invincibility after hit
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;
        }
    }
    
    void InstaKill()
    {
        playerLives = 0;
        UpdateUI();
        GameOver(false);
    }
    
    void CheckEndGoal()
    {
        if (playerID == -1) return;
        
        Matrix4x4 playerMatrix = matrices[colliderIds.IndexOf(playerID)];
        DecomposeMatrix(playerMatrix, out Vector3 pos, out Quaternion rot, out Vector3 scale);
        
        // Simple distance check to end goal
        if (Vector3.Distance(pos, endGoalPosition) < 3f)
        {
            GameOver(true);
        }
    }
    
    void GameOver(bool won)
    {
        gameEnded = true;
        // You can add game over logic here (show menu, restart, etc.)
        Debug.Log(won ? "You Win!" : "Game Over");
    }
    
void UpdateGameTimer()
{
    gameTimer -= Time.deltaTime;
    UpdateUI();
    
    if (gameTimer <= 0 && !gameEnded)
    {
        gameTimer = 0;
        GameOver(false); // Time's up - game over
    }
}
    
    void UpdateUI()
{
    if (livesText != null)
    {
        livesText.text = $"Lives: {playerLives}";
    }
    else
    {
        Debug.LogWarning("Lives text reference not set!");
    }

    if (timerText != null)
    {
        timerText.text = $"Time: {Mathf.Floor(gameTimer)}";
    }
    else
    {
        Debug.LogWarning("Timer text reference not set!");
    }
}


    
    void UpdatePowerups()
    {
        if (fireballCooldown > 0)
        {
            fireballCooldown -= Time.deltaTime;
        }
    }
    
    void UpdateCameraVisibility()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        
        for (int i = 0; i < matrices.Count; i++)
        {
            Matrix4x4 mat = matrices[i];
            DecomposeMatrix(mat, out Vector3 pos, out Quaternion rot, out Vector3 scale);
            
            // Calculate if object is in camera view using dot product
            Vector3 viewportPos = cam.WorldToViewportPoint(pos);
            bool isVisible = viewportPos.x >= 0 && viewportPos.x <= 1 && 
                           viewportPos.y >= 0 && viewportPos.y <= 1 && 
                           viewportPos.z > 0;
            
            // If not visible, set scale to zero
            if (!isVisible)
            {
                Matrix4x4 newMat = Matrix4x4.TRS(pos, rot, Vector3.zero);
                matrices[i] = newMat;
                CollisionManager.Instance.UpdateMatrix(colliderIds[i], newMat);
            }
            else
            {
                // Restore original scale if visible
                Matrix4x4 newMat = Matrix4x4.TRS(pos, rot, scale);
                matrices[i] = newMat;
                CollisionManager.Instance.UpdateMatrix(colliderIds[i], newMat);
            }
        }
    }
    
    // Powerup collection methods
    public void CollectFireballPowerup()
    {
        hasFireballPowerup = true;
        fireballCooldown = 0f;
    }
    
    public void CollectExtraLife()
    {
        playerLives++;
        UpdateUI();
    }
    
    public void CollectInvincibility()
    {
        isInvincible = true;
        invincibilityTimer = invincibilityDuration;
    }

    
    void SetupCamera()
    {
        if (cameraFollow == null)
        {
            // Try to find existing camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Check if it already has our script
                cameraFollow = mainCamera.GetComponent<PlayerCameraFollow>();
                if (cameraFollow == null)
                {
                    // Add our script to existing camera
                    cameraFollow = mainCamera.gameObject.AddComponent<PlayerCameraFollow>();
                }
            }
            else
            {
                // No main camera found, create a new one
                GameObject cameraObj = new GameObject("PlayerCamera");
                Camera cam = cameraObj.AddComponent<Camera>();
                cameraFollow = cameraObj.AddComponent<PlayerCameraFollow>();
                
                // Set this as the main camera
                cam.tag = "MainCamera";
            }
            
            // Configure default camera settings
            cameraFollow.offset = new Vector3(0, 0, -15);
            cameraFollow.smoothSpeed = 0.1f;
        }
    }

    void CreateCubeMesh()
    {
        cubeMesh = new Mesh();
        
        // Create 8 vertices for the cube (corners)
        Vector3[] vertices = new Vector3[8]
        {
            // Bottom face vertices
            new Vector3(0, 0, 0),       // Bottom front left - 0
            new Vector3(width, 0, 0),   // Bottom front right - 1
            new Vector3(width, 0, depth),// Bottom back right - 2
            new Vector3(0, 0, depth),   // Bottom back left - 3
            
            // Top face vertices
            new Vector3(0, height, 0),       // Top front left - 4
            new Vector3(width, height, 0),   // Top front right - 5
            new Vector3(width, height, depth),// Top back right - 6
            new Vector3(0, height, depth)    // Top back left - 7
        };
        
        // Triangles for the 6 faces (2 triangles per face)
        int[] triangles = new int[36]
        {
            // Front face triangles (facing -Z)
            0, 4, 1,
            1, 4, 5,
            
            // Back face triangles (facing +Z)
            2, 6, 3,
            3, 6, 7,
            
            // Left face triangles (facing -X)
            0, 3, 4,
            4, 3, 7,
            
            // Right face triangles (facing +X)
            1, 5, 2,
            2, 5, 6,
            
            // Bottom face triangles (facing -Y)
            0, 1, 3,
            3, 1, 2,
            
            // Top face triangles (facing +Y)
            4, 7, 5,
            5, 7, 6
        };
        
        Vector2[] uvs = new Vector2[8];
        for (int i = 0; i < 8; i++)
        {
            uvs[i] = new Vector2(vertices[i].x / width, vertices[i].z / depth);
        }

        cubeMesh.vertices = vertices;
        cubeMesh.triangles = triangles;
        cubeMesh.uv = uvs;
        cubeMesh.RecalculateNormals();
        cubeMesh.RecalculateBounds();
    }
    
    void CreatePlayer()
    {
        // Create player at a specific position
        Vector3 playerPosition = new Vector3(0, 10, constantZPosition);
        Vector3 playerScale = Vector3.one;
        Quaternion playerRotation = Quaternion.identity;
        
        // Register with collision system - properly handle width/height/depth
        playerID = CollisionManager.Instance.RegisterCollider(
            playerPosition, 
            new Vector3(width * playerScale.x, height * playerScale.y, depth * playerScale.z), 
            true);
        
        // Create transformation matrix
        Matrix4x4 playerMatrix = Matrix4x4.TRS(playerPosition, playerRotation, playerScale);
        matrices.Add(playerMatrix);
        colliderIds.Add(playerID);
        
        // Update the matrix in collision manager
        CollisionManager.Instance.UpdateMatrix(playerID, playerMatrix);
    }
    
    void CreateGround()
    {
        // Create a large ground plane
        Vector3 groundPosition = new Vector3(0, groundY, constantZPosition);
        Vector3 groundScale = new Vector3(groundWidth, 1f, groundDepth);
        Quaternion groundRotation = Quaternion.identity;
        
        // Register with collision system - use actual dimensions
        int groundID = CollisionManager.Instance.RegisterCollider(
            groundPosition, 
            new Vector3(groundWidth, 1f, groundDepth), 
            false);
        
        // Create transformation matrix
        Matrix4x4 groundMatrix = Matrix4x4.TRS(groundPosition, groundRotation, groundScale);
        matrices.Add(groundMatrix);
        colliderIds.Add(groundID);
        
        // Update the matrix in collision manager
        CollisionManager.Instance.UpdateMatrix(groundID, groundMatrix);
    }
    
    void GenerateRandomBoxes()
    {
        // Create random boxes (excluding player and ground)
        for (int i = 0; i < instanceCount - 2; i++)
        {
            // Random position (constant Z)
            Vector3 position = new Vector3(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY),
                constantZPosition
            );
            
            // Random rotation only around Z axis
            Quaternion rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            
            // Random non-uniform scale - different for each dimension
            Vector3 scale = new Vector3(
                Random.Range(0.5f, 3f),
                Random.Range(0.5f, 3f),
                Random.Range(0.5f, 3f)
            );
            
            // Register with collision system - properly handle rectangular shapes
            int id = CollisionManager.Instance.RegisterCollider(
                position, 
                new Vector3(width * scale.x, height * scale.y, depth * scale.z), 
                false);
            
            // Create transformation matrix
            Matrix4x4 boxMatrix = Matrix4x4.TRS(position, rotation, scale);
            matrices.Add(boxMatrix);
            colliderIds.Add(id);
            
            // Update the matrix in collision manager
            CollisionManager.Instance.UpdateMatrix(id, boxMatrix);
        }
    }

    void RenderBoxes()
    {
        // Convert list to array for Graphics.DrawMeshInstanced
        Matrix4x4[] matrixArray = matrices.ToArray();
        
        // Draw instanced meshes in batches of 1023 (GPU limit)
        for (int i = 0; i < matrixArray.Length; i += 1023) {
            int batchSize = Mathf.Min(1023, matrixArray.Length - i);
            Matrix4x4[] batchMatrices = new Matrix4x4[batchSize];
            System.Array.Copy(matrixArray, i, batchMatrices, 0, batchSize);
            Graphics.DrawMeshInstanced(cubeMesh, 0, material, batchMatrices, batchSize);
        }
    }

    void DecomposeMatrix(Matrix4x4 matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        position = matrix.GetPosition();
        rotation = matrix.rotation;
        scale = matrix.lossyScale;
    }
    
    // Add a new random box at runtime (can be called from button or other trigger)
    public void AddRandomBox()
    {
        Vector3 position = new Vector3(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY),
            constantZPosition
        );
        
        Quaternion rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        
        // Random non-uniform scale - different for each dimension
        Vector3 scale = new Vector3(
            Random.Range(0.5f, 3f),
            Random.Range(0.5f, 3f),
            Random.Range(0.5f, 3f)
        );
        
        // Register with collision system - properly handle rectangular shapes
        int id = CollisionManager.Instance.RegisterCollider(
            position, 
            new Vector3(width * scale.x, height * scale.y, depth * scale.z), 
            false);
        
        Matrix4x4 boxMatrix = Matrix4x4.TRS(position, rotation, scale);
        matrices.Add(boxMatrix);
        colliderIds.Add(id);
        
        CollisionManager.Instance.UpdateMatrix(id, boxMatrix);
    }

    void CreateFireballMesh()
{
    fireballMesh = new Mesh();
    
    // Simple pyramid mesh for fireball (pointing right)
    Vector3[] vertices = new Vector3[6] // Changed from 5 to 6
    {
        // Base vertices
        new Vector3(0, 0, 0),    // 0 - back center
        new Vector3(0, 0.5f, 0.5f),  // 1
        new Vector3(0, -0.5f, 0.5f), // 2
        new Vector3(0, -0.5f, -0.5f),// 3
        new Vector3(0, 0.5f, -0.5f), // 4
        
        // Tip vertex
        new Vector3(1, 0, 0)     // 5 - front tip
    };

    int[] triangles = new int[24] // Changed from 18 to 24 (8 triangles × 3 vertices)
    {
        // Base quad (two triangles)
        0, 1, 2,
        0, 2, 3,
        0, 3, 4,
        0, 4, 1,
        
        // Sides (triangles to tip)
        1, 5, 2,
        2, 5, 3,
        3, 5, 4,
        4, 5, 1
    };

    fireballMesh.vertices = vertices;
    fireballMesh.triangles = triangles;
    fireballMesh.RecalculateNormals();
    fireballMesh.RecalculateBounds();
}

    bool CheckCollisionAt(int id, Vector3 position)
    {
        return CollisionManager.Instance.CheckCollision(id, position, out _);
    }

    bool CheckCollisionAt(int id, Vector3 position, out List<int> collidedIds)
    {
        return CollisionManager.Instance.CheckCollision(id, position, out collidedIds);
    }

    void CreateEnemies(int count)
{
    for (int i = 0; i < count; i++)
    {
        Vector3 position = new Vector3(
            Random.Range(minX, maxX),
            Random.Range(groundY + 2, maxY),
            constantZPosition
        );

        Vector3 scale = new Vector3(
            Random.Range(0.8f, 1.5f),
            Random.Range(0.8f, 1.5f),
            Random.Range(0.8f, 1.5f)
        );

        Quaternion rotation = Quaternion.identity;

        // Register with collision system
        int id = CollisionManager.Instance.RegisterCollider(
            position, 
            new Vector3(width * scale.x, height * scale.y, depth * scale.z), 
            false);

        Matrix4x4 enemyMatrix = Matrix4x4.TRS(position, rotation, scale);
        matrices.Add(enemyMatrix);
        colliderIds.Add(id);
        enemyIds.Add(id); // Store enemy ID for later use
        enemyDirections.Add(Random.Range(0, 2) == 0 ? -1f : 1f); // Randomly assign direction

        CollisionManager.Instance.UpdateMatrix(id, enemyMatrix);
    }
}


    void OnDestroy()
{
    // Cleanup all spawned objects
    foreach (int id in colliderIds)
    {
        CollisionManager.Instance.RemoveCollider(id);
    }
    
    // Reset static references if needed
    if (cameraFollow != null)
    {
        Destroy(cameraFollow.gameObject);
    }
}

}