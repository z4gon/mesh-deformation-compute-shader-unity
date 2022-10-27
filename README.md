# Mesh deformation Compute Shader

Written in HLSL in **Unity 2021.3.10f1**

### References

- [Compute Shaders course by Nik Lever](https://www.udemy.com/course/compute-shaders)

## Sections

- [Implementation](#implementation)
  - [C# Code](#c#-code)
    - [Initialize Vertices Array](#initialize-vertices-array)
    - [Initialize Compute Buffers](#initialize-compute-buffers)
    - [Initialize IndirectArguments ComputeBuffer](#initialize-indirectarguments-computebuffer)
    - [Dispatch Compute Shader](#dispatch-compute-shader)
    - [Draw Mesh Instanced Indirect](#draw-mesh-instanced-indirect)
  - [Compute Shader](#compute-shader)
  - [Vertex Fragment Shader](#vertex-fragment-shader)

## Implementation

### C# Code

#### Initialize Vertices Array

- Define a **struct** to contain data from the vertices on the mesh.
- Exract the vertices data from the supplied **Mesh**.

```cs
struct Vertex
{
    public Vector3 position;
    public Vector3 normal;
}
```

```cs
private void InitializeVertices()
{
    _vertices = new Vertex[Mesh.vertices.Length];

    for (var i = 0; i < Mesh.vertices.Length; i++)
    {
        var v = Mesh.vertices[i];

        _vertices[i] = new Vertex
        {
            position = Mesh.vertices[i],
            normal = Mesh.normals[i]
        };
    }
}
```

#### Initialize Compute Buffers

- Initialize two **Compute Buffers** to hold the initial vertices data, and the deformed vertices data.
- The **DeformedVertices** **Compute Buffer** will be written by the **Compute Shader**, and then used by the **Vertex/Fragment Shader**, without CPU involvement.
- The **Material** also needs access to the **Compute Buffer**, so it can connect to the shared **StructuredBuffer**.

```cs
private void InitializeVertexBuffers()
{
    var vertexMemorySize = (3 + 3) * sizeof(float);

    _initialVerticesBuffer = new ComputeBuffer(_vertices.Length, _vertices.Length * vertexMemorySize);
    _deformedVerticesBuffer = new ComputeBuffer(_vertices.Length, _vertices.Length * vertexMemorySize);

    _initialVerticesBuffer.SetData(_vertices);
    _deformedVerticesBuffer.SetData(_vertices);

    _kernelIndex = ComputeShader.FindKernel("DeformVertices");

    // this will let compute shader access the buffers
    ComputeShader.SetBuffer(_kernelIndex, "InitialVertices", _initialVerticesBuffer);
    ComputeShader.SetBuffer(_kernelIndex, "DeformedVertices", _deformedVerticesBuffer);

    // this will let the surface shader access the buffer
    Material.SetBuffer("DeformedVertices", _deformedVerticesBuffer);
}
```

#### Initialize IndirectArguments ComputeBuffer

- Initialize the **Compute Buffer** that will be used by the **Vertex/Fragment Shader** to obtain the **vertex_id: SV_VERTEXID** and the **instance_id: SV_INSTANCEID** from the **GPU Instancing** done by **Graphics.DrawMeshInstancedIndirect**.

```c
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
        (int)Mesh.GetIndexCount(0),     // indices of the mesh
        1,                              // number of objects to render
        0,0,0                           // unused args
    };

    _argsBuffer.SetData(args);
}
```

#### Dispatch Compute Shader

- Dispatch the **Compute Shader** with a **Thread Groups Count** equals to the total count of **vertices** in the **mesh**.

```c
ComputeShader.SetFloat("Time", Time.time);
ComputeShader.SetFloat("Radius", Radius);
ComputeShader.SetFloat("Velocity", Velocity);
ComputeShader.Dispatch(_kernelIndex, _vertices.Length, 1, 1);
```

#### Draw Mesh Instanced Indirect

- Make a direct call to the GPU to draw the mesh.

```c
Graphics.DrawMeshInstancedIndirect(
    mesh: Mesh,
    submeshIndex: 0,
    material: Material,
    bounds: _bounds,
    bufferWithArgs: _argsBuffer
);
```

### Compute Shader

- Each thread will handle updating the position and normal of each vertex.
- The **Compute Shader** will write to the **RWStructuredBuffer** so the data is available for the **Vertex Fragment Shader**.

```c
StructuredBuffer<Vertex> InitialVertices;
RWStructuredBuffer<Vertex> DeformedVertices;
float Time;
float Radius;
float Velocity;

[numthreads(1,1,1)]
void DeformVertices (uint3 id : SV_DispatchThreadID)
{
    Vertex v = InitialVertices[id.x];
    float3 spherePosition = normalize(v.position) * Radius;
    float3 sphereNormal = normalize(v.position) * Radius;

    float progress = (sin(Time * Velocity) / 2) + 0.5;

    v.position = lerp(spherePosition, v.position, progress);
    v.normal = lerp(sphereNormal, v.normal, progress);

    DeformedVertices[id.x] = v;
}
```

### Vertex Fragment Shader

- Will access the **StructuredBuffer** that was passed in to the **Material**.
- Effectively bypassing the CPU to gather the information of the deformed mesh and rendering it.
- It will use the **SV_VERTEXID** and **SV_INSTANCEID** set by the **IndirectArguments** **Compute Buffer**.

```c
StructuredBuffer<Vertex> DeformedVertices;

Varyings vert (uint vertex_id: SV_VERTEXID, uint instance_id: SV_INSTANCEID)
{
    Varyings OUT;

    uint index = vertex_id; // since we are rendering just one instance
    Vertex v = DeformedVertices[index];

    OUT.position = UnityObjectToClipPos(v.position);
    OUT.normal = float4(v.normal, 0);

    // lambert lighting also happens here

    return OUT;
}
```
