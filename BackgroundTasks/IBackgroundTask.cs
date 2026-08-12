using System.Threading;
using System.Threading.Tasks;

namespace BackgroundTasks
{
	public interface IBackgroundTask
	{
		Task Start(CancellationToken ct);
	}
}
