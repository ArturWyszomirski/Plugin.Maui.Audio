namespace Plugin.Maui.Audio;

public class ImmediateStopRule : IStopRule
{
	public ValueTask<bool> EnforceStop(IAudioRecorder recorder, CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(true);
	}
}