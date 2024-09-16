using System.Diagnostics;

namespace Plugin.Maui.Audio;

class SilenceIsDetectedStopRule : IStopRule
{
	bool readingsComplete;
	double noiseLevel;
	DateTime firstNoiseDetectedTime;
	DateTime lastSoundDetectedTime;
	bool soundDetected = false;

	readonly double silenceThreshold;
	readonly int silenceDuration;

	internal SilenceIsDetectedStopRule(double silenceThreshold, int silenceDuration)
	{
		this.silenceThreshold = silenceThreshold;
		this.silenceDuration = silenceDuration;
	}
	
	public async ValueTask<bool> EnforceStop(IAudioRecorder recorder, CancellationToken cancellationToken = default)
	{
		await DetectSilenceAsync(recorder, cancellationToken);
		
		return soundDetected;
	}
	
	async Task DetectSilenceAsync(IAudioRecorder recorder, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(silenceThreshold, 1);
		ArgumentOutOfRangeException.ThrowIfNegative(silenceDuration);

		readingsComplete = false;
		noiseLevel = 0;
		firstNoiseDetectedTime = default;
		lastSoundDetectedTime = default;

		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			await Task.Run(() =>
			{
#if WINDOWS
				audioFileStream = GetFileStream();
				audioChunkNumber = 1;
#endif
				while (recorder.IsRecording)
				{
					cancellationToken.ThrowIfCancellationRequested();
					byte[]? audioDataChunk = recorder.GetAudioDataChunk();

					if (audioDataChunk is byte[] audioData)
					{
						if (DetectSilence(audioData))
						{
							return;
						}
					}
				}
			}, cancellationToken);
		}
		catch(OperationCanceledException)
		{
			Debug.WriteLine("Detect silence canceled.");
			throw;
		}
		finally
		{
#if WINDOWS
			audioFileStream?.Dispose();
#endif
		}
	}

	bool DetectSilence(byte[] audioData)
	{
		double minimumNoiseLevel = 0.005;

		if (!readingsComplete)
		{
			readingsComplete = CheckIfReadingsComplete(audioData);
		}
		else if (noiseLevel == 0)
		{
			noiseLevel = CalculateNormalizedRMS(audioData);

			if (noiseLevel < minimumNoiseLevel)
			{
				noiseLevel = minimumNoiseLevel;
			}

			firstNoiseDetectedTime = DateTime.UtcNow;
		}
		else
		{
			double audioLevel = CalculateNormalizedRMS(audioData);

			if (audioLevel < noiseLevel && audioLevel > minimumNoiseLevel)
			{
				noiseLevel = audioLevel;
			}

			if (audioLevel <= silenceThreshold * noiseLevel)
			{
				if (lastSoundDetectedTime != default)
				{
					if ((DateTime.UtcNow - lastSoundDetectedTime).TotalMilliseconds >= silenceDuration)
					{
						Debug.WriteLine("Silence detected.");

						return true;
					}
				}
				else if ((DateTime.UtcNow - firstNoiseDetectedTime).TotalMilliseconds >= silenceDuration)
				{
					Debug.WriteLine("No sound detected.");

					return true;
				}
				
			}
			else
			{
				soundDetected = true;
				lastSoundDetectedTime = DateTime.UtcNow;
				Debug.WriteLine("Sound detected.");
			}
		}

		return false;
	}

	double CalculateNormalizedRMS(byte[] buffer)
	{
		double sampleSquareSum = 0;
		for (int i = 0; i < buffer.Length; i += 2)
		{
			short sample = BitConverter.ToInt16(buffer, i);
			sampleSquareSum += sample * sample;
		}

		double rootMeanSquare = Math.Sqrt(sampleSquareSum / (buffer.Length / 2));
		double normalizedRMS = rootMeanSquare / short.MaxValue;
		Debug.WriteLine($"RMS: {normalizedRMS} | Noise: {noiseLevel}");
		return normalizedRMS;
	}

	/// <summary>
	/// First sets of data after starting recoding are always zeros. They are followed by one incomplete set that have zeros at the beginning.
	/// Checking completeness of data is crucial beacuse the first complete audio data set is used to define background noise level.
	/// </summary>
	bool CheckIfReadingsComplete(byte[] data)
	{
		int sum = default;

		for (int i = 0; i < 100; i++)
		{
			sum += data[i];
		}

		return sum > 0;
	}
}

partial class When
{
	public static IStopRule SilenceIsDetected(double thresholdOf, int forDuration) => new SilenceIsDetectedStopRule(thresholdOf, forDuration);
}