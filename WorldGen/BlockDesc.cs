namespace WorldGen
{
  /// <summary>
  /// The data type for a single block description
  /// </summary>
  public struct BlockDesc
  {
    /// <summary>
    /// The bit for whether a block is solid
    /// </summary>
    private const int BIT_SOLID = 1;

    /// <summary>
    /// The solid flag
    /// </summary>
    public bool Solid { get; set; }

    /// <summary>
    /// Encode a block description as an int
    /// </summary>
    /// <param name="block">The block description</param>
    /// <returns>The encoded block value</returns>
    public static int Encode(BlockDesc block)
    {
      return (block.Solid ? 1 : 0) << (BIT_SOLID-1);
    }

    /// <summary>
    /// Decode a block description from an int
    /// </summary>
    /// <param name="val">The encoded block value</param>
    /// <returns>The block description</returns>
    public static BlockDesc Decode(int val)
    {
      return new BlockDesc
      {
        Solid = IsSolid(val)
      };
    }

    /// <summary>
    /// Returns whether the given encoded block value is solid
    /// </summary>
    /// <param name="val">The value</param>
    /// <returns>Whether it's solid</returns>
    public static bool IsSolid(int val)
    {
      return (val & BIT_SOLID) != 0;
    }
  }
}
