using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace Landoria.SagaCapture
{
    // Counts audio buffers rejected by the recorder without logging on Unity's audio thread.
    internal sealed class SagaCaptureAudioDiagnostics
    {
        private static SagaCaptureAudioDiagnostics _active;
        private static readonly PropertyInfo ChannelsProperty = AccessTools.Property(
            AccessTools.TypeByName("UnityRuntimeCameraRecorder.UnityAudioCapture"),
            "Channels");

        private readonly int _sampleRate;
        private long _submittedBytes;
        private long _wrappedDelegates;
        private long _acceptedBlocks;
        private long _rejectedBlocks;
        private long _rejectedBytes;
        private long _firstRejectedByte = -1;
        private long _lastRejectedByte = -1;
        private int _channels;
        private long _reportedRejectedBlocks;
        private float _nextReportAt;

        // Stores the Unity audio format for duration estimates.
        private SagaCaptureAudioDiagnostics(int sampleRate)
        {
            _sampleRate = sampleRate;
        }

        // Starts one recording's counters before the audio delegate is initialized.
        internal static SagaCaptureAudioDiagnostics Begin()
        {
            var diagnostics = new SagaCaptureAudioDiagnostics(AudioSettings.outputSampleRate);
            Interlocked.Exchange(ref _active, diagnostics);
            SagaCapturePlugin.Log.LogInfo(
                $"Audio diagnostics started: sampleRate={diagnostics._sampleRate} Hz.");
            return diagnostics;
        }

        // Writes the final counters and clears the active recording.
        internal static void End(SagaCaptureAudioDiagnostics diagnostics)
        {
            if (ReferenceEquals(Interlocked.CompareExchange(ref _active, null, diagnostics), diagnostics))
            {
                diagnostics.Report(true);
            }
        }

        // Reports new rejected blocks from Unity's main thread.
        internal void Poll()
        {
            if (Time.unscaledTime < _nextReportAt)
            {
                return;
            }

            _nextReportAt = Time.unscaledTime + 1f;
            Report(false);
        }

        // Writes a summary only when counts change or capture ends.
        private void Report(bool final)
        {
            long rejected = Interlocked.Read(ref _rejectedBlocks);
            if (!final && rejected == _reportedRejectedBlocks)
            {
                return;
            }

            long accepted = Interlocked.Read(ref _acceptedBlocks);
            long wrapped = Interlocked.Read(ref _wrappedDelegates);
            long rejectedBytes = Interlocked.Read(ref _rejectedBytes);
            long submittedBytes = Interlocked.Read(ref _submittedBytes);
            int channels = Volatile.Read(ref _channels);
            string positions = channels > 0 && _sampleRate > 0
                ? $"first={Seconds(Interlocked.Read(ref _firstRejectedByte), channels):F3}s, " +
                  $"last={Seconds(Interlocked.Read(ref _lastRejectedByte), channels):F3}s, " +
                  $"submittedDuration={Seconds(submittedBytes, channels):F3}s, " +
                  $"rejectedDuration={Seconds(rejectedBytes, channels):F3}s"
                : "position=unknown";
            string message = $"Audio diagnostics{(final ? " final" : "")}: " +
                $"wrappedDelegates={wrapped}, acceptedBlocks={accepted}, rejectedBlocks={rejected}, " +
                $"rejectedBytes={rejectedBytes}, channels={channels}, {positions}.";

            if (rejected > 0 || final && wrapped == 0)
            {
                SagaCapturePlugin.Log.LogWarning(message);
            }
            else if (final)
            {
                SagaCapturePlugin.Log.LogInfo(message);
            }

            _reportedRejectedBlocks = rejected;
        }

        // Converts interleaved float bytes to seconds at the captured sample rate.
        private double Seconds(long bytePosition, int channels)
        {
            return bytePosition < 0 ? -1 :
                (double)bytePosition / (sizeof(float) * channels * _sampleRate);
        }

        // The delegate still returns the original result to the recorder.
        internal static void Wrap(object audioCapture, ref Func<byte[], bool> writeAudio)
        {
            SagaCaptureAudioDiagnostics diagnostics = Volatile.Read(ref _active);
            if (diagnostics == null || writeAudio == null)
            {
                return;
            }

            Func<byte[], bool> original = writeAudio;
            Interlocked.Increment(ref diagnostics._wrappedDelegates);
            writeAudio = data =>
            {
                long start = Interlocked.Add(ref diagnostics._submittedBytes, data.Length) - data.Length;
                bool accepted = original(data);
                if (accepted)
                {
                    Interlocked.Increment(ref diagnostics._acceptedBlocks);
                }
                else
                {
                    diagnostics.RecordRejected(audioCapture, start, data.Length);
                }
                return accepted;
            };
        }

        // Records one failed write without logging on the audio thread.
        private void RecordRejected(object audioCapture, long start, int length)
        {
            int channels = ReadChannels(audioCapture);
            if (channels > 0)
            {
                Volatile.Write(ref _channels, channels);
            }
            if (Interlocked.Increment(ref _rejectedBlocks) == 1)
            {
                Interlocked.Exchange(ref _firstRejectedByte, start);
            }
            Interlocked.Exchange(ref _lastRejectedByte, start);
            Interlocked.Add(ref _rejectedBytes, length);
        }

        // Reads the channel count without risking an exception in Unity's audio callback.
        private static int ReadChannels(object audioCapture)
        {
            try
            {
                return ChannelsProperty != null
                    ? (int)ChannelsProperty.GetValue(audioCapture, null) : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
