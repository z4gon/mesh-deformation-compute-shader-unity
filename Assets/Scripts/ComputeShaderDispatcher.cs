using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ComputeShaderDispatcher : MonoBehaviour
{
    public ComputeShader ComputeShader;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Vertex[] _vertices;
    private ComputeBuffer _initialVerticesBuffer;
    private ComputeBuffer _deformedVerticesBuffer;

    private int _kernelIndex;

    // for Graphics.DrawMeshInstancedIndirect
    private ComputeBuffer _argsBuffer;
    private Bounds _bounds;

    // Start is called before the first frame update
    void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        InitializeVertices();
        InitializeVertexBuffers();
        InitializeIndirectArgsBuffer();
    }

    private void InitializeVertices()
    {
        var mesh = _meshFilter.mesh;
        _vertices = new Vertex[mesh.vertices.Length];

        for (var i = 0; i < mesh.vertices.Length; i++)
        {
            var v = mesh.vertices[i];

            _vertices[i] = new Vertex
            {
                position = mesh.vertices[i],
                normal = mesh.normals[i]
            };
        }
    }

    private void InitializeVertexBuffers()
    {
        var vertexMemorySize = (3 + 3) * sizeof(float);

        _initialVerticesBuffer = new ComputeBuffer(_vertices.Length, _vertices.Length * vertexMemorySize);
        _deformedVerticesBuffer = new ComputeBuffer(_vertices.Length, _vertices.Length * vertexMemorySize);

        _initialVerticesBuffer.SetData(_vertices);
        _deformedVerticesBuffer.SetData(_vertices);

        _kernelIndex = ComputeShader.FindKernel("DeformVertices");

        ComputeShader.SetBuffer(_kernelIndex, "InitialVertices", _initialVerticesBuffer);
        ComputeShader.SetBuffer(_kernelIndex, "DeformedVertices", _deformedVerticesBuffer);
    }

    private void InitializeIndirectArgsBuffer()
    {
        _bounds = new Bounds(center: Vector3.zero, size: Vector3.one * 1000);

        const int _argsCount = 5;

        _argsBuffer = new ComputeBuffer(
            count: 1,
            stride: _argsCount * sizeof(uint),
            type: ComputeBufferType.IndirectArguments
        );

        // for Graphics.DrawMeshInstancedIndirect
        // this will be used by the vertex/fragment shader
        // to get the instance_id and vertex_id
        var args = new int[_argsCount] {
            (int)_meshFilter.mesh.GetIndexCount(0),     // indices of the mesh
            1,                                          // number of objects to render
            0,0,0                                       // unused args
        };

        _argsBuffer.SetData(args);
    }

    // Update is called once per frame
    void Update()
    {
        ComputeShader.Dispatch(_kernelIndex, _vertices.Length, 1, 1);
        Graphics.DrawMeshInstancedIndirect(
            mesh: _meshFilter.mesh,
            submeshIndex: 0,
            material: _meshRenderer.material,
            bounds: _bounds,
            bufferWithArgs: _argsBuffer
        );
    }
}
