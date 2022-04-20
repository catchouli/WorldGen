namespace WorldGen
{
  /// <summary>
  /// The interface for chunk generators
  /// </summary>
  public interface IChunkGenerator
  {
    /// <summary>
    /// Generate a chunk at the given origin coordinates
    /// </summary>
    /// <param name="originX">The origin X</param>
    /// <param name="originY">The origin Y</param>
    /// <param name="originZ">The origin Z</param>
    /// <param name="chunk">The chunk to write to</param>
    void GenerateChunk(int originX, int originY, int originZ, ref Chunk chunk);
  }
}
