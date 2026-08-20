namespace Cirreum.Runtime;

/// <summary>
/// Defines default values used by the domain runtime antiforgery infrastructure.
/// </summary>
public static class DomainAntiforgeryDefaults {

	/// <summary>
	/// The route prefix for Cirreum-provided antiforgery endpoints.
	/// </summary>
	public const string Prefix = "/_cirreum";

	/// <summary>
	/// The default request header used to supply the antiforgery token.
	/// </summary>
	public const string HeaderName = "X-CSRF-TOKEN";

	/// <summary>
	/// The default route for retrieving an antiforgery request token.
	/// </summary>
	public const string TokenEndpoint = $"{Prefix}/antiforgery/token";

}