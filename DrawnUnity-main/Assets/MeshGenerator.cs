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
    public Material material;
    private Mesh cubeMesh;  // This will represent the cube mesh
    private Matrix4x4 matrix;  // Matrix for storing position, rotation, and scale of the cube
    public float moveSpeed = 5f;
    public float rotateSpeed = 100f;
    public float cubeSize = 1f;

    void Start()
    {
        // Create the cube mesh
        CreateCubeMesh();
        
        // Initialize the matrix for the cube (starting at the identity matrix)
        matrix = Matrix4x4.identity;
    }

    void CreateCubeMesh()
    {
        cubeMesh = new Mesh();

        // 8 vertices of a cube
        Vector3[] vertices = new Vector3[8]
        {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f)
        };

        // Define the triangles for each face (12 triangles, 2 for each face)
        int[] triangles = new int[36]
        {
            // Front face
            0, 2, 1, 0, 3, 2,
            // Back face
            4, 5, 6, 4, 6, 7,
            // Left face
            0, 4, 7, 0, 7, 3,
            // Right face
            1, 2, 6, 1, 6, 5,
            // Top face
            3, 7, 6, 3, 6, 2,
            // Bottom face
            0, 1, 5, 0, 5, 4
        };

        // Assign the vertices and triangles to the mesh
        cubeMesh.vertices = vertices;
        cubeMesh.triangles = triangles;
        cubeMesh.RecalculateNormals(); // Recalculate normals for proper lighting
    }

    void Update()
    {
        // Movement input (A & D for X axis)
        float moveInput = 0;
        if (Input.GetKey(KeyCode.A)) moveInput = -1f;
        if (Input.GetKey(KeyCode.D)) moveInput = 1f;

        // Update position by modifying the matrix directly
        Vector3 position = matrix.GetPosition();
        position.x += moveInput * moveSpeed * Time.deltaTime;

        // Rotation input (W & S for Z-axis rotation)
        float rotateInput = 0;
        if (Input.GetKey(KeyCode.W)) rotateInput = 1f;
        if (Input.GetKey(KeyCode.S)) rotateInput = -1f;

        // Update rotation by modifying the matrix directly (around the Z-axis for simplicity)
        Quaternion rotation = matrix.rotation * Quaternion.Euler(0, 0, rotateInput * rotateSpeed * Time.deltaTime);

        // Keep scale constant (scale can be modified as well, but we leave it 1x1x1 for simplicity)
        Vector3 scale = Vector3.one;

        // Update the matrix with new position, rotation, and scale
        matrix = Matrix4x4.TRS(position, rotation, scale);

        // Draw the cube using the updated transformation matrix
        Graphics.DrawMeshInstanced(cubeMesh, 0, material, new Matrix4x4[] { matrix });
    }
}