﻿namespace Plugin.Maui.Audio;

partial class AudioRecorder : IAudioRecorder
{
	public AudioRecorder(AudioRecorderOptions options)
	{
	}

	public bool CanRecordAudio => false;

	public bool IsRecording => false;

	public Task StartAsync(AudioRecordingOptions options) => Task.CompletedTask;

	public Task StartAsync(string filePath, AudioRecordingOptions options) => Task.CompletedTask;

	public Task StartAsync() => Task.CompletedTask;

	public Task StartAsync(string filePath) => Task.CompletedTask;

	public Task<IAudioSource> StopAsync(IStopRule? stopRule = null, CancellationToken cancellationToken = default) => Task.FromResult<IAudioSource>(new EmptyAudioSource());

	public byte[]? GetAudioDataChunk() => throw new NotImplementedException();
}