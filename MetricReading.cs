﻿using System;
using System.Collections.Immutable;

namespace StackExchange.Metrics.Infrastructure
{
    /// <summary>
    /// Represents a reading from a metric.
    /// </summary>
    public readonly struct MetricReading
    {
        /// <summary>
        /// Constructs a new <see cref="MetricReading" />.
        /// </summary>
        /// <param name="name">
        /// Name of the metric.
        /// </param>
        /// <param name="type">
        /// Type of the metric.
        /// </param>
        /// <param name="value">
        /// Value of the metric.
        /// </param>
        /// <param name="tags">
        /// Dictionary of tags keyed by name.
        /// </param>
        /// <param name="timestamp">
        /// <see cref="DateTime"/> representing the time the metric was observed.
        /// </param>
        public MetricReading(string name, MetricType type, double value, ImmutableDictionary<string, string> tags, DateTime timestamp)
        {
            Name = name;
            Type = type;
            Value = value;
            Tags = tags;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Name of the metric.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Type of the metric.
        /// </summary>
        public MetricType Type { get; }
        /// <summary>
        /// Value of the metric.
        /// </summary>
        public double Value { get; }
        /// <summary>
        /// Tags associated with the metric.
        /// </summary>
        public ImmutableDictionary<string, string> Tags { get; }
        /// <summary>
        /// Timestamp that the metric was recorded at.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Updates the value and the timestamp of the metric.
        /// </summary>
        /// <param name="delta">
        /// Amount to increment the value.
        /// </param>
        /// <param name="timestamp">
        /// Timestamp for the update.
        /// </param>
        public MetricReading Update(double delta, DateTime timestamp)
        {
            return new MetricReading(Name, Type, Value + delta, Tags, timestamp);
        }
    }
}
