namespace Cirreum.Runtime.SystemInitializers;

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

internal class InitializeDomainContext : ISystemInitializer {
	public ValueTask RunAsync(IServiceProvider serviceProvider) {
		var initializer = serviceProvider.GetRequiredService<IDomainContextInitializer>();
		initializer.Initialize();
		return ValueTask.CompletedTask;
	}
}