using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.CoreAudioApi;

namespace ShootingKeyboard.Services;

/// <summary>
/// Low-latency audio engine using NAudio DirectSound / Wasapi / WaveOut with pre-cached PCM samples
/// </summary>
public sealed class AudioEngineService : IAudioEngine
{
    private readonly ConcurrentDictionary<string, float[]> _sampleCache = new();
    private readonly ConcurrentDictionary<string, float> _soundVolumes = new();
    private IWavePlayer? _output;
    private MixingSampleProvider? _mixer;
    private float _masterVolume = 0.7f;
    private bool _muted = false;
    private bool _disposed = false;
    private readonly object _initLock = new();

    public AudioEngineService()
    {
        InitializeOutput();
    }

    private void InitializeOutput()
    {
        lock (_initLock)
        {
            if (_output != null && _mixer != null)
                return;

            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
            {
                ReadFully = true
            };

            // Strategy 1: DirectSoundOut (Universally supported across all Windows sound hardware, headsets, virtual drivers)
            try
            {
                var ds = new DirectSoundOut(40);
                ds.Init(_mixer);
                ds.Play();
                _output = ds;
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DirectSoundOut initialization failed: {ex.Message}");
            }

            // Strategy 2: WasapiOut (Shared mode with standard event sync)
            try
            {
                var wasapi = new WasapiOut(AudioClientShareMode.Shared, 50);
                wasapi.Init(_mixer);
                wasapi.Play();
                _output = wasapi;
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WasapiOut initialization failed: {ex.Message}");
            }

            // Strategy 3: WaveOutEvent (Universal fallback)
            try
            {
                var waveOut = new WaveOutEvent { DesiredLatency = 50 };
                waveOut.Init(_mixer);
                waveOut.Play();
                _output = waveOut;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WaveOutEvent initialization failed: {ex.Message}");
            }
        }
    }

    public bool LoadSound(string soundId, string filePath)
    {
        if (!File.Exists(filePath))
        {
            System.Diagnostics.Debug.WriteLine($"Sound file not found: {filePath}");
            return false;
        }

        try
        {
            using var reader = new AudioFileReader(filePath);
            var format = reader.WaveFormat;
            var buffer = new float[reader.Length / 4];
            var totalRead = 0;

            while (totalRead < buffer.Length)
            {
                var read = reader.Read(buffer, totalRead, buffer.Length - totalRead);
                if (read == 0)
                    break;
                totalRead += read;
            }

            if (totalRead < buffer.Length)
            {
                Array.Resize(ref buffer, totalRead);
            }

            // Convert to stereo if needed
            if (format.Channels == 1)
            {
                buffer = ConvertMonoToStereo(buffer);
            }
            else if (format.Channels > 2)
            {
                buffer = DownmixToStereo(buffer, format.Channels);
            }

            // Resample if needed
            if (format.SampleRate != 44100)
            {
                buffer = Resample(buffer, format.SampleRate, 44100);
            }

            _sampleCache[soundId] = buffer;
            _soundVolumes[soundId] = 1.0f;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load sound {soundId}: {ex.Message}");
            return false;
        }
    }

    public void LoadSoundPack(string basePath, IEnumerable<(string SoundId, string FileName, float Volume)> soundEntries)
    {
        foreach (var (soundId, fileName, volume) in soundEntries)
        {
            var fullPath = Path.Combine(basePath, fileName);
            if (LoadSound(soundId, fullPath))
            {
                _soundVolumes[soundId] = volume;
            }
        }
    }

    public void Play(string soundId, float volume = 1.0f)
    {
        PlayWithPitch(soundId, volume, 1.0f);
    }

    public void PlayWithPitch(string soundId, float volume, float pitch)
    {
        if (_disposed || _muted)
            return;

        if (_output == null || _mixer == null)
        {
            InitializeOutput();
        }

        if (_mixer == null)
            return;

        if (!_sampleCache.TryGetValue(soundId, out var samples))
            return;

        var effectiveVolume = _masterVolume * volume * _soundVolumes.GetValueOrDefault(soundId, 1.0f);
        if (effectiveVolume <= 0f)
            return;

        var provider = new CachedSampleProvider(samples, effectiveVolume, pitch);
        _mixer.AddMixerInput(provider);
    }

    public void SetMasterVolume(float volume)
    {
        _masterVolume = Math.Clamp(volume, 0f, 1f);
    }

    public float GetMasterVolume() => _masterVolume;

    public void SetMuted(bool muted)
    {
        _muted = muted;
    }

    public bool IsSoundLoaded(string soundId) => _sampleCache.ContainsKey(soundId);

    public IReadOnlyCollection<string> GetLoadedSoundIds() => _sampleCache.Keys.ToList();

    public void UnloadSound(string soundId)
    {
        _sampleCache.TryRemove(soundId, out _);
        _soundVolumes.TryRemove(soundId, out _);
    }

    public void UnloadAllSounds()
    {
        _sampleCache.Clear();
        _soundVolumes.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _output?.Stop();
            _output?.Dispose();
        }
        catch { }

        _output = null;
        _mixer = null;
        UnloadAllSounds();
    }

    #region Audio Processing Helpers

    private static float[] ConvertMonoToStereo(float[] mono)
    {
        var stereo = new float[mono.Length * 2];
        for (int i = 0; i < mono.Length; i++)
        {
            stereo[i * 2] = mono[i];
            stereo[i * 2 + 1] = mono[i];
        }
        return stereo;
    }

    private static float[] DownmixToStereo(float[] multi, int channels)
    {
        var frameCount = multi.Length / channels;
        var stereo = new float[frameCount * 2];

        for (int i = 0; i < frameCount; i++)
        {
            float left = 0, right = 0;
            for (int c = 0; c < channels; c++)
            {
                var sample = multi[i * channels + c];
                if (c == 0 || c % 2 == 0)
                    left += sample;
                else
                    right += sample;
            }
            stereo[i * 2] = left / Math.Max(1, (channels + 1) / 2);
            stereo[i * 2 + 1] = right / Math.Max(1, channels / 2);
        }

        return stereo;
    }

    private static float[] Resample(float[] input, int fromRate, int toRate)
    {
        if (fromRate == toRate)
            return input;

        var ratio = (double)toRate / fromRate;
        var outputLength = (int)(input.Length * ratio / 2) * 2; // Keep even for stereo
        var output = new float[outputLength];

        for (int i = 0; i < outputLength; i += 2)
        {
            var srcPos = i / ratio;
            var srcIndex = (int)srcPos;
            var frac = srcPos - srcIndex;

            // Linear interpolation for left channel
            var left1 = srcIndex < input.Length ? input[srcIndex] : 0;
            var left2 = srcIndex + 2 < input.Length ? input[srcIndex + 2] : 0;
            output[i] = left1 + (left2 - left1) * (float)frac;

            // Linear interpolation for right channel
            var right1 = srcIndex + 1 < input.Length ? input[srcIndex + 1] : 0;
            var right2 = srcIndex + 3 < input.Length ? input[srcIndex + 3] : 0;
            output[i + 1] = right1 + (right2 - right1) * (float)frac;
        }

        return output;
    }

    #endregion

    /// <summary>
    /// Sample provider that reads from pre-cached PCM data with volume and pitch control
    /// </summary>
    private sealed class CachedSampleProvider : ISampleProvider
    {
        private readonly float[] _samples;
        private readonly float _volume;
        private readonly float _pitch;
        private int _position = 0;
        private readonly float _pitchStep;

        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

        public CachedSampleProvider(float[] samples, float volume, float pitch)
        {
            _samples = samples;
            _volume = volume;
            _pitch = pitch;
            _pitchStep = Math.Clamp(pitch, 0.2f, 4.0f);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var samplesToRead = count;
            var actualRead = 0;

            while (actualRead < samplesToRead && _position < _samples.Length)
            {
                var srcIndex = (int)(_position * _pitchStep);
                if (srcIndex >= _samples.Length - 1)
                    break;

                // Linear interpolation for pitch
                var frac = _position * _pitchStep - srcIndex;
                buffer[offset + actualRead] = (_samples[srcIndex] * (1 - (float)frac) + _samples[srcIndex + 1] * (float)frac) * _volume;

                _position++;
                actualRead++;
            }

            return actualRead;
        }
    }
}