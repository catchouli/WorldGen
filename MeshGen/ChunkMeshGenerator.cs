using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Memory;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using System.Numerics;
using Utils;
using WorldGen;

namespace MeshGen
{
  using Vertex = VertexBuilder<VertexPosition, VertexTexture1, VertexEmpty>;
  using Mesh = MeshBuilder<VertexPosition, VertexTexture1, VertexEmpty>;

  /// <summary>
  /// The mesh generator that generates meshes for chunks
  /// </summary>
  public class ChunkMeshGenerator : IChunkMeshGenerator
  {
    /// <summary>
    /// A dummy texture used because I want the textures to be obtained from the API but SharpGLTF doesn't have a way
    /// to just provide a URL
    /// </summary>
    private static readonly MemoryImage _dummyTex = new MemoryImage("Data/dummy.png");

    /// <summary>
    /// The url builder
    /// </summary>
    private readonly IUrlBuilder _urlBuilder;

    /// <summary>
    /// Create a new ChunkMeshGenerator
    /// </summary>
    /// <param name="urlBuilder">The url builder</param>
    public ChunkMeshGenerator(IUrlBuilder urlBuilder)
    {
      _urlBuilder = urlBuilder;
    }

    /// <inheritdoc/>
    public void GenMesh(string outFilename, in Chunk chunk)
    {
      // Create textured material with a dummy texture, we do this because SharpGLTF really wants a MemoryImage to be
      // added to the slot, but we just want to include a custom URL in our ImageWriterCallback instead, so we reuse
      // this 1x1 dummy texture.
      var material = new MaterialBuilder()
        .WithDoubleSide(true)
        .WithMetallicRoughnessShader()
        .WithChannelParam(KnownChannel.BaseColor, new Vector4(1.0f, 1.0f, 1.0f, 1.0f))
        .WithChannelImage(KnownChannel.BaseColor, _dummyTex);

      // Create the mesh
      var mesh = new Mesh();

      // Create primitive builder
      var builder = mesh.UsePrimitive(material);

      // Iterate chunk and emit block faces
      for (int z = 0; z < Chunk.SizeZ; ++z)
      {
        for (int y = 0; y < Chunk.SizeY; ++y)
        {
          for (int x = 0; x < Chunk.SizeX; ++x)
          {
            if (chunk.IsSolid(x, y, z))
            {
              if (x + 1 == Chunk.SizeX || !chunk.IsSolid(x + 1, y, z))
                EmitFace(builder, (float)x, (float)y, (float)z, FaceDirection.PositiveX);
              if (x == 0 || !chunk.IsSolid(x - 1, y, z))
                EmitFace(builder, (float)x, (float)y, (float)z, FaceDirection.NegativeX);
              if (y + 1 == Chunk.SizeY || !chunk.IsSolid(x, y + 1, z))
                EmitFace(builder, (float)x, (float)y, (float)z, FaceDirection.PositiveY);
              if (y == 0 || !chunk.IsSolid(x, y - 1, z))
                EmitFace(builder, (float)x, (float)y, (float)z, FaceDirection.NegativeY);
              if (z + 1 == Chunk.SizeY || !chunk.IsSolid(x, y, z + 1))
                EmitFace(builder, (float)x, (float)y, (float)z, FaceDirection.PositiveZ);
              if (z == 0 || !chunk.IsSolid(x, y, z - 1))
                EmitFace(builder, (float)x, (float)y, (float)z, FaceDirection.NegativeZ);
            }
          }
        }
      }

      // Create the scene
      var scene = new SceneBuilder();
      scene.AddRigidMesh(mesh, Matrix4x4.Identity);

      // Build the SceneBuilder into a GLTF2 model
      var model = scene.ToGltf2();

      // Create an image writer callback to return the url
      ImageWriterCallback imageWriterCallback = (WriteContext context, string assetName, MemoryImage image) =>
      {
        return _urlBuilder.BuildPath(true, "api", "models", "chunk", "texture");
      };

      // Save the model to the given cache filename
      model.SaveGLB(outFilename, new WriteSettings
      {
        ImageWriting = ResourceWriteMode.SatelliteFile,
        ImageWriteCallback = imageWriterCallback
      });
    }

    /// <summary>
    /// Emit the face of a cube
    /// </summary>
    /// <param name="builder">The primitive builder</param>
    private void EmitFace(IPrimitiveBuilder builder, float x, float y, float z, FaceDirection faceDir)
    {
      // Calculate vertex components for box
      float x2 = x + 1.0f;
      float y2 = y + 1.0f;
      float z2 = z + 1.0f;

      if (faceDir == FaceDirection.PositiveZ)
      {
        var v1 = new Vertex { Position = new Vector3(x, y, z2) };
        v1.Material.TexCoord = new Vector2(0.0f, 1.0f);
        var v2 = new Vertex { Position = new Vector3(x2, y, z2) };
        v2.Material.TexCoord = new Vector2(1.0f, 1.0f);
        var v3 = new Vertex { Position = new Vector3(x2, y2, z2) };
        v3.Material.TexCoord = new Vector2(1.0f, 0.0f);
        var v4 = new Vertex { Position = new Vector3(x, y2, z2) };
        v4.Material.TexCoord = new Vector2(0.0f, 0.0f);

        builder.AddTriangle(v1, v2, v3);
        builder.AddTriangle(v1, v3, v4);
      }
      else if (faceDir == FaceDirection.NegativeZ)
      {
        var v1 = new Vertex { Position = new Vector3(x, y, z) };
        v1.Material.TexCoord = new Vector2(0.0f, 1.0f);
        var v2 = new Vertex { Position = new Vector3(x2, y, z) };
        v2.Material.TexCoord = new Vector2(1.0f, 1.0f);
        var v3 = new Vertex { Position = new Vector3(x2, y2, z) };
        v3.Material.TexCoord = new Vector2(1.0f, 0.0f);
        var v4 = new Vertex { Position = new Vector3(x, y2, z) };
        v4.Material.TexCoord = new Vector2(0.0f, 0.0f);

        builder.AddTriangle(v1, v2, v3);
        builder.AddTriangle(v1, v3, v4);
      }
      else if (faceDir == FaceDirection.NegativeX)
      {
        var v1 = new Vertex { Position = new Vector3(x, y2, z) };
        v1.Material.TexCoord = new Vector2(0.0f, 1.0f);
        var v2 = new Vertex { Position = new Vector3(x, y2, z2) };
        v2.Material.TexCoord = new Vector2(1.0f, 1.0f);
        var v3 = new Vertex { Position = new Vector3(x, y, z2) };
        v3.Material.TexCoord = new Vector2(1.0f, 0.0f);
        var v4 = new Vertex { Position = new Vector3(x, y, z) };
        v4.Material.TexCoord = new Vector2(0.0f, 0.0f);

        builder.AddTriangle(v1, v2, v3);
        builder.AddTriangle(v1, v3, v4);
      }
      else if (faceDir == FaceDirection.PositiveX)
      {
        //return;
        var v1 = new Vertex { Position = new Vector3(x+1.0f, y, z) };
        v1.Material.TexCoord = new Vector2(0.0f, 1.0f);
        var v2 = new Vertex { Position = new Vector3(x+1.0f, y2, z) };
        v2.Material.TexCoord = new Vector2(1.0f, 1.0f);
        var v3 = new Vertex { Position = new Vector3(x+1.0f, y2, z2) };
        v3.Material.TexCoord = new Vector2(1.0f, 0.0f);
        var v4 = new Vertex { Position = new Vector3(x+1.0f, y, z2) };
        v4.Material.TexCoord = new Vector2(0.0f, 0.0f);

        builder.AddTriangle(v3, v2, v1);
        builder.AddTriangle(v4, v3, v1);
      }
      else if (faceDir == FaceDirection.NegativeY)
      {
        //return;
        var v1 = new Vertex { Position = new Vector3(x, y, z) };
        v1.Material.TexCoord = new Vector2(0.0f, 1.0f);
        var v2 = new Vertex { Position = new Vector3(x2, y, z) };
        v2.Material.TexCoord = new Vector2(1.0f, 1.0f);
        var v3 = new Vertex { Position = new Vector3(x2, y, z2) };
        v3.Material.TexCoord = new Vector2(1.0f, 0.0f);
        var v4 = new Vertex { Position = new Vector3(x, y, z2) };
        v4.Material.TexCoord = new Vector2(0.0f, 0.0f);

        builder.AddTriangle(v1, v2, v3);
        builder.AddTriangle(v1, v3, v4);
      }
      else if (faceDir == FaceDirection.PositiveY)
      {
        //return;
        var v1 = new Vertex { Position = new Vector3(x, y2, z) };
        v1.Material.TexCoord = new Vector2(0.0f, 1.0f);
        var v2 = new Vertex { Position = new Vector3(x2, y2, z) };
        v2.Material.TexCoord = new Vector2(1.0f, 1.0f);
        var v3 = new Vertex { Position = new Vector3(x2, y2, z2) };
        v3.Material.TexCoord = new Vector2(1.0f, 0.0f);
        var v4 = new Vertex { Position = new Vector3(x, y2, z2) };
        v4.Material.TexCoord = new Vector2(0.0f, 0.0f);

        builder.AddTriangle(v3, v2, v1);
        builder.AddTriangle(v4, v3, v1);
      }
    }
  }
}
