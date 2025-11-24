namespace Cirreum.Runtime;

sealed class DomainEnvironment(
	string applicationName,
	string environmentName,
	DomainRuntimeType runtimeType
) : IDomainEnvironment {
	public string ApplicationName { get; } = applicationName;
	public string EnvironmentName { get; } = environmentName;
	public DomainRuntimeType RuntimeType { get; } = runtimeType;
}