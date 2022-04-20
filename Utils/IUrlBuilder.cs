namespace Utils
{
  /// <summary>
  /// A helper for building application urls
  /// </summary>
  public interface IUrlBuilder
  {
    /// <summary>
    /// Build a URL path to the given resource in the application
    /// </summary>
    /// <param name="relative">
    /// Whether the path should be built relative to the request URI, or from /
    /// </param>
    /// <param name="parts">The path parts</param>
    /// <returns>A path</returns>
    string BuildPath(bool relative, params string[] parts);
  }
}