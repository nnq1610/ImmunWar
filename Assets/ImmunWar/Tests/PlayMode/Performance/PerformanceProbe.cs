using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace ImmunWar.Tests.PlayMode.Performance
{
    public sealed class PerformanceProbe : IDisposable
    {
        private ProfilerRecorder _mainThread;
        private ProfilerRecorder _gcAllocated;
        private readonly List<double> _frameMilliseconds = new List<double>();
        public IReadOnlyList<double> FrameMilliseconds => _frameMilliseconds;

        public void Start()
        {
            _frameMilliseconds.Clear();
            _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            _gcAllocated = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
        }

        public void Sample()
        {
            if (_mainThread.Valid && _mainThread.Count > 0) _frameMilliseconds.Add(_mainThread.LastValue / 1_000_000d);
        }

        public long LastAllocatedBytes => _gcAllocated.Valid && _gcAllocated.Count > 0 ? _gcAllocated.LastValue : -1;

        public double Percentile(double percentile)
        {
            if (_frameMilliseconds.Count == 0) return 0d;
            var values = _frameMilliseconds.ToArray();
            Array.Sort(values);
            var rank = Math.Max(1, (int)Math.Ceiling(percentile * values.Length));
            return values[Math.Min(values.Length - 1, rank - 1)];
        }

        public void Dispose() { _mainThread.Dispose(); _gcAllocated.Dispose(); }
    }
}

