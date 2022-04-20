using MeshGen;
using Microsoft.AspNetCore.Mvc;
using WorldGen;

namespace Blockhaus.Api
{
  /// <summary>
  /// The controller for accessing models and textures
  /// </summary>
  [Route("api/[controller]")]
  [ApiController]
  public class ModelsController : ControllerBase
  {
    /// <summary>
    /// The content type for GLB models
    /// </summary>
    private const string GlbContentType = "model/gltf-binary";

    /// <summary>
    /// The content type for PNG images
    /// </summary>
    private const string PngContentType = "image/png";

    /// <summary>
    /// The chunk generator
    /// </summary>
    private readonly IChunkGenerator _chunkGenerator;

    /// <summary>
    /// The chunk mesh generator
    /// </summary>
    private readonly IChunkMeshGenerator _chunkMeshGenerator;

    public ModelsController(IChunkGenerator chunkGenerator, IChunkMeshGenerator chunkMeshGenerator)
    {
      _chunkGenerator = chunkGenerator;
      _chunkMeshGenerator = chunkMeshGenerator;
    }

    /// <summary>
    /// Gets the GLB model for a given chunk
    /// </summary>
    /// <returns>The GLB model file</returns>
    [HttpGet("chunk/{x}/{y}/{z}")]
    public IActionResult GetChunkModel(int x, int y, int z)
    {
      // Generate cache filename for model
      // TODO: make race condition safe
      string cacheFilename = $"/tmp/{x}-{y}-{z}.glb";

      if (!System.IO.File.Exists(cacheFilename))
      {
        // Generate chunk
        var chunk = new Chunk();
        _chunkGenerator.GenerateChunk(x * Chunk.SizeX, y * Chunk.SizeY, z * Chunk.SizeZ, ref chunk);

        // Generate chunk mesh
        _chunkMeshGenerator.GenMesh(cacheFilename, in chunk);
      }

      return File(System.IO.File.OpenRead(cacheFilename), GlbContentType);
    }

    /// <summary>
    /// Gets the chunk texture atlas
    /// </summary>
    /// <returns>The PNG texture</returns>
    [HttpGet("chunk/texture")]
    public IActionResult GetTexture()
    {
      var file = System.IO.File.OpenRead("Data/46.png");
      return File(file, PngContentType);
    }
  }
}
