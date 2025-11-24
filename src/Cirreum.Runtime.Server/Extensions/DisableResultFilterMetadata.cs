namespace Cirreum.Runtime.Extensions;

/// <summary>
/// Marker metadata indicating an endpoint has opted out of the Result-to-HTTP filter.
/// </summary>
/// <remarks>
/// Apply this metadata using <see cref="ResultFilterExtensions.WithoutResultFilter{TBuilder}"/>.
/// </remarks>
public sealed class DisableResultFilterMetadata;