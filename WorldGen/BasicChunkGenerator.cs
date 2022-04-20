using SimplexNoise;

namespace WorldGen
{
  /// <summary>
  /// A basic chunk generator that just generates blocks with a 50% chance of being solid
  /// </summary>
  public class BasicChunkGenerator : IChunkGenerator
  {
    /// <summary>
    /// The random number generator
    /// </summary>
    private Random _rng;

    /// <summary>
    /// Construct a new BasicChunkGenerator
    /// </summary>
    /// <param name="seed">The seed to use</param>
    public BasicChunkGenerator(int seed)
    {
      _rng = new Random(seed);
      Noise.Seed = seed;
    }

    /// <inheritdoc/>
    public void GenerateChunk(int originX, int originY, int originZ, ref Chunk chunk)
    {
      for (int z = 0; z < Chunk.SizeZ; ++z)
      {
        for (int y = 0; y < Chunk.SizeY; ++y)
        {
          for (int x = 0; x < Chunk.SizeX; ++x)
          {
            float simplex = Noise.CalcPixel2D(originX + x, originZ + z, 0.01f) / 256.0f * Chunk.SizeY;

            chunk.Put(x, y, z, new BlockDesc
            {
              //Solid = (_rng.Next(0, 2) == 0)
              Solid = simplex >= y
            });
          }
        }
      }
    }
  }
}
