using UnityEngine;

public class SpriteInstancing : MonoBehaviour
{
    public Texture2D spriteTexture;
    public int instanceCount = 100;
    public float spriteSize = 1f;
    
    private Mesh quadMesh;
    public Material spriteMaterial;
    private Matrix4x4[] matrices;
    
    void Start()
    {
        // Create a quad mesh for the sprite
        CreateQuadMesh();
        
        // Create a material that uses the sprite texture
        CreateSpriteMaterial();
        
        // Set up matrices for instancing
        SetupSpriteInstances();
    }
    
    void CreateQuadMesh()
    {
        quadMesh = new Mesh();
        quadMesh.name = "Sprite Quad";
        
        // Create a simple quad (centered at origin, facing up on Y axis for 2D)
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, -0.5f, 0),  // bottom left
            new Vector3(0.5f, -0.5f, 0),   // bottom right
            new Vector3(0.5f, 0.5f, 0),    // top right
            new Vector3(-0.5f, 0.5f, 0)    // top left
        };
        
        int[] triangles = new int[6]
        {
            0, 2, 1,    // first triangle
            0, 3, 2     // second triangle
        };
        
        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0, 0),  // bottom left
            new Vector2(1, 0),  // bottom right
            new Vector2(1, 1),  // top right
            new Vector2(0, 1)   // top left
        };
        
        quadMesh.vertices = vertices;
        quadMesh.triangles = triangles;
        quadMesh.uv = uvs;
        quadMesh.RecalculateNormals();
    }
    
    void CreateSpriteMaterial()
    {
        // Create a material that supports transparency
        //spriteMaterial = new Material(Shader.Find("Sp`"));
        spriteMaterial.mainTexture = spriteTexture;
        spriteMaterial.enableInstancing = true;
    }
    
    void SetupSpriteInstances()
    {
        matrices = new Matrix4x4[instanceCount];
        
        for (int i = 0; i < instanceCount; i++)
        {
            // Random position in 2D (keeping Z at 0 for 2D space)
            Vector3 position = new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(-5f, 5f),
                0
            );
            
            // For 2D sprites, rotation is typically around the Z axis only
            Quaternion rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            
            // Random scale (optional)
            Vector3 scale = Vector3.one * spriteSize * Random.Range(0.5f, 1.5f);
            
            matrices[i] = Matrix4x4.TRS(position, rotation, scale);
        }
    }
    
    void Update()
    {
        // Draw all instances every frame
        Graphics.DrawMeshInstanced(quadMesh, 0, spriteMaterial, matrices);
        
        // Handle any input for moving sprites
        // (implement similar to the previous examples)
    }
}