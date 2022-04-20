namespace WorldGen
{
  /// <summary>
  /// A world chunk
  /// </summary>
  public struct Chunk
  {
    public const int SizeX = 16;
    public const int SizeY = 16;
    public const int SizeZ = 16;

    private int[] _blocks;

    public Chunk()
    {
      _blocks = new int[SizeX * SizeY * SizeZ];
    }

    public bool IsSolid(int x, int y, int z)
    {
      return BlockDesc.IsSolid(_blocks[PosToIndex(x, y, z)]);
    }

    public BlockDesc Get(int x, int y, int z)
    {
      return BlockDesc.Decode(_blocks[PosToIndex(x, y, z)]);
    }

    public void Put(int x, int y, int z, BlockDesc block)
    {
      _blocks[PosToIndex(x, y, z)] = BlockDesc.Encode(block);
    }

    private int PosToIndex(int x, int y, int z)
    {
      if (x < 0 || x >= SizeX)
        throw new ArgumentException("x out of range");
      if (y < 0 || y >= SizeX)
        throw new ArgumentException("y out of range");
      if (z < 0 || z >= SizeX)
        throw new ArgumentException("z out of range");

      return (z * SizeY * SizeX) + (y * SizeX) + x;
    }
  }
}