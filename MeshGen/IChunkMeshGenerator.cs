using WorldGen;

namespace MeshGen
{
  /// <summary>
  /// The mesh generator that generates meshes for chunks
  /// </summary>
  public interface IChunkMeshGenerator
  {
    /// <summary>
    /// Generate a mesh for the given chunk
    /// </summary>
    /// <param name="outFilename">The output filename</param>
    /// <param name="chunk">The chunk</param>
    /// <returns>A stream to the mesh data that must be closed by the caller</returns>
    /// TODO: not ideal that this takes a filename, I'd rather that SharpGLTF could just write to a stream
    void GenMesh(string outFilename, in Chunk chunk);
  }
}
