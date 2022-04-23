using System.Runtime.Serialization;

namespace Utils
{
  /// <summary>
  /// An exception type for internal errors
  /// </summary>
  public class InternalErrorException : Exception
  {
    /// <summary>
    /// Create a new InternalErrorException
    /// </summary>
    public InternalErrorException()
    {
    }

    /// <summary>
    /// Create a new InternalErrorException with a message
    /// </summary>
    public InternalErrorException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Create a new InternalErrorException with a message and inner exception
    /// </summary>
    public InternalErrorException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Create a new InternalErrorException using serialization
    /// </summary>
    protected InternalErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}
