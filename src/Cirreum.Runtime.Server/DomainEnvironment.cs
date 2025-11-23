namespace Cirreum.Runtime;


sealed class DomainEnvironment(
	IHostEnvironment hostEnvironment,
	IConfiguration configuration
) : IDomainEnvironment {
	public string ApplicationName => hostEnvironment.ApplicationName;
	public string EnvironmentName => hostEnvironment.EnvironmentName;
	public DomainRuntimeType RuntimeType { get; } = configuration.GetValue("Cirreum:Runtime", DomainRuntimeType.WebApi);
}