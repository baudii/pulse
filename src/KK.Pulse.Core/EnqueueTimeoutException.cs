using System;

namespace KK.Pulse.Core;

/// <summary>
/// Exception that is thrown whenever the maximum time for enqueue method is reached.
/// </summary>
public class EnqueueTimeoutException(string message) : Exception(message)
{
}
