using System;
using System.Runtime.Serialization;

namespace Light.DataAccessMocks;

/// <summary>
/// Represents the exception that is thrown by the mocks that implement the abstractions of Light.SharedCore.
/// </summary>
[Serializable]
public class TestException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="TestException" /> with the specified message
    /// and an optional inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception (optional).</param>
    public TestException(string message, Exception? innerException = null) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of <see cref="TestException" /> with serialized data.
    /// </summary>
    /// <param name="info">
    /// The <see cref="SerializationInfo" /> that holds the serialized
    /// object data about the exception being thrown.
    /// </param>
    /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
    protected TestException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
