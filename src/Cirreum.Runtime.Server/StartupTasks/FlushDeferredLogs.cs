namespace Cirreum.Runtime.StartupTasks;

using Cirreum.Logging.Deferred;
using System.Threading.Tasks;

internal class FlushDeferredLogs(
	IHostApplicationLifetime lifetime,
	ILogger<FlushDeferredLogs> logger)
	: IStartupTask {

	public int Order => int.MaxValue;

	public ValueTask ExecuteAsync() {
		lifetime.ApplicationStarted.Register(this.HandleApplicationStarted);
		return ValueTask.CompletedTask;
	}

	private void HandleApplicationStarted() {
		logger.FlushDeferredLogs();
	}

}