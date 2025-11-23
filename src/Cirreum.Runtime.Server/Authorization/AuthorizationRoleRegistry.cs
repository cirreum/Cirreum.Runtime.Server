namespace Cirreum.Runtime.Authorization;

using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

internal sealed class AuthorizationRoleRegistry(
	ILogger<AuthorizationRoleRegistry> logger)
	: AuthorizationRoleRegistryBase(logger), IAutoInitialize {

	/// <inheritdoc/>
	public ValueTask InitializeAsync() {
		return this.DefaultInitializationAsync();
	}

}