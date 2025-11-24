namespace Cirreum.Runtime.Extensions;

using Cirreum.Http.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Extension methods for registering the Cirreum Result-to-HTTP endpoint filter.
/// </summary>
public static class ResultFilterExtensions {

	/// <summary>
	/// Maps API endpoints with the Cirreum Result-to-HTTP filter applied globally.
	/// </summary>
	/// <param name="builder">The endpoint route builder.</param>
	/// <param name="configure">An action to configure endpoints within the filtered group.</param>
	/// <returns>The endpoint route builder for chaining.</returns>
	/// <example>
	/// <code>
	/// app.MapApiEndpoints(api =>
	/// {
	///     api.MapGet("/orders/{id}", OrderEndpoints.Get);
	///     api.MapPost("/orders", OrderEndpoints.Create);
	/// });
	/// </code>
	/// </example>
	public static IEndpointRouteBuilder MapApiEndpoints(
		this IEndpointRouteBuilder builder,
		Action<RouteGroupBuilder> configure) {
		var group = builder.MapGroup("")
			.AddEndpointFilter<ResultToHttpEndpointFilterWrapper>();

		configure(group);

		return builder;
	}

	/// <summary>
	/// Maps API endpoints with the Cirreum Result-to-HTTP filter applied globally,
	/// using the specified route prefix.
	/// </summary>
	/// <param name="builder">The endpoint route builder.</param>
	/// <param name="prefix">The route prefix to apply to all endpoints in the group.</param>
	/// <param name="configure">An action to configure endpoints within the filtered group.</param>
	/// <returns>The endpoint route builder for chaining.</returns>
	/// <example>
	/// <code>
	/// app.MapApiEndpoints("/api/v1", api =>
	/// {
	///     api.MapGet("/orders/{id}", OrderEndpoints.Get);
	///     api.MapPost("/orders", OrderEndpoints.Create);
	/// });
	/// </code>
	/// </example>
	public static IEndpointRouteBuilder MapApiEndpoints(
		this IEndpointRouteBuilder builder,
		string prefix,
		Action<RouteGroupBuilder> configure) {
		var group = builder.MapGroup(prefix)
			.AddEndpointFilter<ResultToHttpEndpointFilterWrapper>();

		configure(group);

		return builder;
	}

	/// <summary>
	/// Opts out this endpoint from the Cirreum Result-to-HTTP endpoint filter.
	/// </summary>
	/// <typeparam name="TBuilder">The type of endpoint convention builder.</typeparam>
	/// <param name="builder">The endpoint convention builder.</param>
	/// <returns>The builder for chaining.</returns>
	/// <example>
	/// <code>
	/// api.MapGet("/health", () => "OK")
	///    .WithoutResultFilter();
	/// </code>
	/// </example>
	public static TBuilder WithoutResultFilter<TBuilder>(this TBuilder builder)
		where TBuilder : IEndpointConventionBuilder {
		builder.WithMetadata(new DisableResultFilterMetadata());
		return builder;
	}

	/// <summary>
	/// Explicitly adds the Cirreum Result-to-HTTP endpoint filter to this endpoint.
	/// </summary>
	/// <param name="builder">The route handler builder.</param>
	/// <returns>The builder for chaining.</returns>
	/// <remarks>
	/// Use this when endpoints are registered outside of <see cref="MapApiEndpoints(IEndpointRouteBuilder, Action{RouteGroupBuilder})"/>
	/// and require Result-to-HTTP transformation.
	/// </remarks>
	public static RouteHandlerBuilder WithResultFilter(this RouteHandlerBuilder builder) {
		return builder.AddEndpointFilter<ResultToHttpEndpointFilter>();
	}

	/// <summary>
	/// Explicitly adds the Cirreum Result-to-HTTP endpoint filter to this route group.
	/// </summary>
	/// <param name="builder">The route group builder.</param>
	/// <returns>The builder for chaining.</returns>
	/// <remarks>
	/// Use this when route groups are created outside of <see cref="MapApiEndpoints(IEndpointRouteBuilder, Action{RouteGroupBuilder})"/>
	/// and require Result-to-HTTP transformation.
	/// </remarks>
	public static RouteGroupBuilder WithResultFilter(this RouteGroupBuilder builder) {
		return builder.AddEndpointFilter<ResultToHttpEndpointFilter>();
	}
}