namespace Cirreum.Runtime.Extensions;

using Cirreum.Http.Filters;

/// <summary>
/// Wrapper filter that delegates to <see cref="ResultToHttpEndpointFilter"/> while
/// respecting <see cref="DisableResultFilterMetadata"/> opt-out.
/// </summary>
/// <remarks>
/// <para>
/// This wrapper is instantiated once at startup by <c>AddEndpointFilter&lt;T&gt;()</c>
/// and reused for all requests. The actual filter is injected via constructor.
/// </para>
/// <para>
/// Endpoints marked with <see cref="DisableResultFilterMetadata"/> (via 
/// <see cref="ResultFilterExtensions.WithoutResultFilter{TBuilder}"/>) bypass 
/// the filter entirely.
/// </para>
/// </remarks>
/// <param name="filter">The Result-to-HTTP filter resolved from DI.</param>
internal sealed class ResultToHttpEndpointFilterWrapper(
	ResultToHttpEndpointFilter filter
) : IEndpointFilter {

	/// <inheritdoc />
	public async ValueTask<object?> InvokeAsync(
		EndpointFilterInvocationContext context,
		EndpointFilterDelegate next) {
		var hasOptOut = context.HttpContext.GetEndpoint()?.Metadata
			.GetMetadata<DisableResultFilterMetadata>() is not null;

		if (hasOptOut) {
			return await next(context);
		}

		return await filter.InvokeAsync(context, next);
	}

}