// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
	"Usage",
	"CA1816:Dispose methods should call SuppressFinalize",
	Justification = "This class is a simple wrapper that delegates disposal to the underlying WebApplication. " +
					"It does not have a finalizer and does not own any resources requiring finalization suppression.",
	Scope = "member",
	Target = "~M:Cirreum.Runtime.DomainApplication.DisposeAsync~System.Threading.Tasks.ValueTask")]
