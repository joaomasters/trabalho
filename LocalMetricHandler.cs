﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Metrics.Infrastructure;

namespace StackExchange.Metrics.Handlers
{
    /// <summary>
    /// Represents metadata about a metric.
    /// </summary>
    public readonly struct LocalMetricMetadata
    {
        /// <summary>
        /// Constructs a new <see cref="LocalMetricMetadata" /> instance.
        /// </summary>
        /// <param name="metric">
        /// Name of a metric.
        /// </param>
        /// <param name="type">
        /// Type of the metric.
        /// </param>
        /// <param name="description">
        /// Descriptive text for the metric.
        /// </param>
        /// <param name="unit">
        /// Unit of the metric.
        /// </param>
        public LocalMetricMetadata(string metric, string type, string description, string unit)
        {
            Metric = metric;
            Type = type;
            Description = description;
            Unit = unit;
        }

        /// <summary>
        /// Gets the name of the metric.
        /// </summary>
        public string Metric { get; }
        /// <summary>
        /// Gets the type of a metric.
        /// </summary>
        public string Type { get; }
        /// <summary>
        /// Gets descriptive text for a metric.
        /// </summary>
        public string Description { get; }
        /// <summary>
        /// Gets the unit of a metric.
        /// </summary>
        public string Unit { get; }
    }

    /// <summary>
    /// An implementation of <see cref="IMetricHandler" /> that stores metrics locally
    /// so that they can be pulled via an API.
    /// </summary>
    public class LocalMetricHandler : IMetricHandler
    {
        readonly object _readingLock;
        readonly object _metadataLock;
        readonly Dictionary<MetricKey, MetricReading> _readings;
        readonly List<LocalMetricMetadata> _metadata;
        long _serializeCount;

        /// <summary>
        /// Constructs a new <see cref="LocalMetricMetadata" />.
        /// </summary>
        public LocalMetricHandler()
        {
            _readingLock = new object();
            _metadataLock = new object();
            _readings = new Dictionary<MetricKey, MetricReading>(MetricKeyComparer.Default);
            _metadata = new List<LocalMetricMetadata>();
        }

        /// <summary>
        /// Returns all the metadata that has been recorded.
        /// </summary>
        public IEnumerable<LocalMetricMetadata> GetMetadata()
        {
            lock (_metadataLock)
            {
                return _metadata.ToList();
            }
        }

        /// <summary>
        /// Returns a current snapshot of all metrics.
        /// </summary>
        /// <param name="reset">
        /// Indicates whether to reset the readings once they've been read.
        /// </param>
        public IEnumerable<MetricReading> GetReadings(bool reset = false)
        {
            lock (_readingLock)
            {
                var readings = _readings.Values.ToList();
                if (reset)
                {
                    _readings.Clear();
                }

                return readings;
            }
        }

        /// <inheritdoc />
        public IMetricReadingBatch BeginBatch() => new Batch(this);

        /// <inheritdoc />
        public ValueTask FlushAsync(TimeSpan delayBetweenRetries, int maxRetries, Action<AfterSendInfo> afterSend, Action<Exception> exceptionHandler)
        {
            var flushCount = Interlocked.Read(ref _serializeCount);
            if (flushCount > 0)
            {
                afterSend?.Invoke(
                    new AfterSendInfo
                    {
                        Duration = TimeSpan.Zero,
                        BytesWritten = 0
                    });

                Interlocked.Add(ref _serializeCount, -flushCount);
            }

            return default;
        }

        /// <inheritdoc />
        public void SerializeMetadata(IEnumerable<Metadata> metadata)
        {
            lock (_metadataLock)
            {
                _metadata.Clear();
                _metadata.AddRange(
                    metadata
                        .GroupBy(x => x.Metric)
                        .Select(
                            g => new LocalMetricMetadata(
                                metric: g.Key,
                                type: g.Where(x => x.Name == MetadataNames.Rate).Select(x => x.Value).FirstOrDefault(),
                                description: g.Where(x => x.Name == MetadataNames.Description).Select(x => x.Value).FirstOrDefault(),
                                unit: g.Where(x => x.Name == MetadataNames.Unit).Select(x => x.Value).FirstOrDefault()
                            )
                        )
                );
            }
        }

        /// <inheritdoc />
        public void SerializeMetric(in MetricReading reading)
        {
            lock (_readingLock)
            {
                var isCounter = reading.Type == MetricType.Counter || reading.Type == MetricType.CumulativeCounter;
                var key = new MetricKey(reading.Name, reading.Tags);
                if (isCounter && _readings.TryGetValue(key, out var existingReading))
                {
                    _readings[key] = existingReading.Update(reading.Value, reading.Timestamp);
                }
                else
                {
                    _readings[key] = reading;
                }

                Interlocked.Increment(ref _serializeCount);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        private class Batch : IMetricReadingBatch
        {
            private readonly LocalMetricHandler _handler;

            public Batch(LocalMetricHandler handler)
            {
                _handler = handler;
            }

            public long BytesWritten { get; private set; }
            public long MetricsWritten { get; private set; }

            /// <inheritdoc />
            public void Add(in MetricReading reading)
            {
                _handler.SerializeMetric(reading);
                MetricsWritten++;
                BytesWritten = 0;
            }
        }
    }
}
