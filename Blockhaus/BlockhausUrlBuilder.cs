using Flurl;
using Utils;

namespace Blockhaus
{
  /// <summary>
  /// The UrlBuilder for blockhaus which takes into account the request uri and application base path
  /// </summary>
  public class BlockhausUrlBuilder : IUrlBuilder
  {
    /// <summary>
    /// The application base path, empty string or null means none
    /// </summary>
    private readonly string _basePath;

    /// <summary>
    /// The asp.net core IUrlHelper
    /// </summary>
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Construct a new BlockhausUrlBuilder
    /// </summary>
    /// <param name="basePath">The application base path, empty string or null means none</param>
    public BlockhausUrlBuilder(string basePath, IHttpContextAccessor httpContextAccessor)
    {
      _basePath = basePath;
      _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public string BuildPath(bool relative, params string[] parts)
    {
      if (!parts.Any())
        throw new ArgumentException($"{nameof(parts)} must have at least one element");

      // Construct base url
      var allParts = new List<string>();

      if (!relative)
      {
        allParts.Add("/");
        allParts.Add(_basePath);
      }
      else
      {
        allParts.Add(Path.GetRelativePath(_httpContextAccessor?.HttpContext?.Request?.Path ?? "/", "/"));
      }

      // Add rest of parts
      allParts.AddRange(parts);

      return Url.Combine(allParts.ToArray());
    }
  }
}