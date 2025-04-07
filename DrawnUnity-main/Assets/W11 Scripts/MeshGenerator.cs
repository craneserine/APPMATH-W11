using System.Numerics;
using Unity.Mathematics;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


public class MeshGenerator : MonoBehaviour
{
    public Material material;  // Material to render the cube with
    private Mesh cubeMesh;     // Stores the generated cube mesh data
    private Matrix4x4 matrix;  // Transformation matrix for position, rotation, and scale

    // Movement parameters
    public float moveSpeed = 5f;     // Speed for left/right movement
    public float rotateSpeed = 100f; // Speed for rotation
    public float cubeSize = 1f;      // Size of the cube (currently not used in mesh generation)

    void Start()
    {
        // Create the cube mesh when the script starts
        CreateCubeMesh();
        
        // Initialize the transformation matrix to identity (no transformation)
        matrix = Matrix4x4.identity;
    }

    /// Creates a cube mesh with vertices and triangles
    void CreateCubeMesh()
    {
        cubeMesh = new Mesh();

        // Define the 8 vertices of a cube (centered at origin)
        Vector3[] vertices = new Vector3[8]
        {
            // Bottom face vertices (counter-clockwise)
            new Vector3(-0.5f, -0.5f, -0.5f), // 0: back-left-bottom
            new Vector3( 0.5f, -0.5f, -0.5f), // 1: back-right-bottom
            new Vector3( 0.5f,  0.5f, -0.5f), // 2: back-right-top
            new Vector3(-0.5f,  0.5f, -0.5f), // 3: back-left-top

            // Top face vertices (counter-clockwise)
            new Vector3(-0.5f, -0.5f,  0.5f), // 4: front-left-bottom
            new Vector3( 0.5f, -0.5f,  0.5f), // 5: front-right-bottom
            new Vector3( 0.5f,  0.5f,  0.5f), // 6: front-right-top
            new Vector3(-0.5f,  0.5f,  0.5f)  // 7: front-left-top
        };

        // Define triangles for each face (2 triangles per face, 6 vertices per face)
        int[] triangles = new int[36]
        {
            // Front face (facing -Z)
            0, 2, 1, // First triangle
            0, 3, 2, // Second triangle

            // Back face (facing +Z)
            4, 5, 6, // First triangle
            4, 6, 7, // Second triangle

            // Left face (facing -X)
            0, 4, 7, // First triangle
            0, 7, 3, // Second triangle

            // Right face (facing +X)
            1, 2, 6, // First triangle
            1, 6, 5, // Second triangle

            // Top face (facing +Y)
            3, 7, 6, // First triangle
            3, 6, 2, // Second triangle

            // Bottom face (facing -Y)
            0, 1, 5, // First triangle
            0, 5, 4  // Second triangle
        };

        // Assign vertices and triangles to the mesh
        cubeMesh.vertices = vertices;
        cubeMesh.triangles = triangles;

        // Recalculate normals for proper lighting calculations
        cubeMesh.RecalculateNormals();
    }

    void Update()
    {
        // Handle movement input (A/D keys for X-axis movement)
        float moveInput = 0;
        if (Input.GetKey(KeyCode.A)) moveInput = -1f; // Move left
        if (Input.GetKey(KeyCode.D)) moveInput = 1f;  // Move right

        // Get current position from matrix and update X component
        Vector3 position = matrix.GetPosition();
        position.x += moveInput * moveSpeed * Time.deltaTime;

        // Handle rotation input (W/S keys for Z-axis rotation)
        float rotateInput = 0;
        if (Input.GetKey(KeyCode.W)) rotateInput = 1f;  // Rotate counter-clockwise
        if (Input.GetKey(KeyCode.S)) rotateInput = -1f; // Rotate clockwise

        // Get current rotation from matrix and apply additional rotation
        // Note: multiply quaternions to combine rotations
        Quaternion rotation = matrix.rotation * Quaternion.Euler(0, 0, rotateInput * rotateSpeed * Time.deltaTime);

        // Keep scale constant (1,1,1)
        Vector3 scale = Vector3.one;

        // Rebuild the transformation matrix with updated position, rotation, and scale
        // TRS stands for Translation (position), Rotation, Scale
        matrix = Matrix4x4.TRS(position, rotation, scale);

        // Draw the cube using the updated transformation matrix
        // Note: We're using DrawMeshInstanced even though we only have one instance,
        // which is slightly less efficient but works for this example
        Graphics.DrawMeshInstanced(cubeMesh, 0, material, new Matrix4x4[] { matrix });
    }
}