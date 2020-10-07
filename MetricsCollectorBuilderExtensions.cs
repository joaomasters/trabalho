﻿using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Metrics;
using StackExchange.Metrics.Handlers;
using StackExchange.Metrics.Metrics;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IMetricsCollectorBuilder" />.
    /// </summary>
    public static class MetricsCollectorBuilderExtensions
    {

        /// <summary>
        /// Removes any <see cref="MetricSource"/> instances registered for the collector.
        /// </summary>
        public static IMetricsCollectorBuilder ClearSources(this IMetricsCollectorBuilder builder)
        {
            builder.Services.RemoveAll<MetricSource>();
            for (var i = builder.Services.Count - 1; i >= 0; i--)
            {
                if (builder.Services[i].ServiceType.IsSubclassOf(typeof(MetricSource)))
                {
                    builder.Services.RemoveAt(i);
                }
            }

            return builder;
        }

        /// <summary>
        /// Adds the default built-in <see cref="MetricSource" /> implementations to the collector.
        /// </summary>
        public static IMetricsCollectorBuilder AddDefaultSources(this IMetricsCollectorBuilder builder)
        {
            return builder.AddProcessMetricSource()
#if NETCOREAPP
                .AddAspNetMetricSource()
                .AddRuntimeMetricSource();
#else
                .AddGarbageCollectorMetricSource();
#endif
        }

        /// <summary>
        /// Adds a <see cref="ProcessMetricSource" /> to the collector.
        /// </summary>
        public static IMetricsCollectorBuilder AddProcessMetricSource(this IMetricsCollectorBuilder builder) => builder.AddSourceIfMissing<ProcessMetricSource>();

#if NETCOREAPP
        /// <summary>
        /// Adds a <see cref="RuntimeMetricSource" /> to the collector.
        /// </summary>
        public static IMetricsCollectorBuilder AddRuntimeMetricSource(this IMetricsCollectorBuilder builder) => builder.AddSourceIfMissing<RuntimeMetricSource>();

        /// <summary>
        /// Adds a <see cref="AspNetMetricSource" /> to the collector.
        /// </summary>
        public static IMetricsCollectorBuilder AddAspNetMetricSource(this IMetricsCollectorBuilder builder) => builder.AddSourceIfMissing<AspNetMetricSource>();
#else
        /// <summary>
        /// Adds a <see cref="GarbageCollectorMetricSource" /> to the collector.
        /// </summary>
        public static IMetricsCollectorBuilder AddGarbageCollectorMetricSource(this IMetricsCollectorBuilder builder) => builder.AddSource<GarbageCollectorMetricSource>();
#endif

        /// <summary>
        /// Adds a Bosun endpoint to the collector.
        /// </summary>
        public static IMetricsCollectorBuilder AddBosunEndpoint(this IMetricsCollectorBuilder builder, Uri baseUri, Action<BosunMetricHandler> configure = null)
        {
            var handler = new BosunMetricHandler(baseUri);
            configure?.Invoke(handler);
            return builder.AddEndpoint("Bosun", handler);
        }

        /// <summary>
        /// Adds a SignalFx endpoint to the collector.
        /// </summary>
        public static IMetricsCollectorBuilder AddSignalFxEndpoint(this IMetricsCollectorBuilder builder, Uri baseUri, Action<SignalFxMetricHandler> configure = null)
        {
            var handler = new SignalFxMetricHandler(baseUri);
            configure?.Invoke(handler);
            return builder.AddEndpoint("SignalFx", handler);
        }

        /// <summary>
        /// Adds a SignalFx endpoint to the collector.
        /// </summary>
        public static IMetricsCollectorBuilder AddSignalFxEndpoint(this IMetricsCollectorBuilder builder, Uri baseUri, string accessToken, Action<SignalFxMetricHandler> configure = null)
        {
            var handler = new SignalFxMetricHandler(baseUri, accessToken);
            configure?.Invoke(handler);
            return builder.AddEndpoint("SignalFx", handler);
        }

        /// <summary>
        /// Exceptions which occur on a background thread will be passed to the delegate specified here.
        /// </summary>
        public static IMetricsCollectorBuilder UseExceptionHandler(this IMetricsCollectorBuilder builder, Action<Exception> handler)
        {
            builder.Options.ExceptionHandler = handler;
            return builder;
        }

        /// <summary>
        /// Configures the default <see cref="MetricSourceOptions"/> used for <see cref="MetricSource"/> instances
        /// passed to the collector.
        /// </summary>
        public static IMetricsCollectorBuilder ConfigureSources(this IMetricsCollectorBuilder builder, Action<MetricSourceOptions> action)
        {
            var options = new MetricSourceOptions();
            action?.Invoke(options);
            return builder.ConfigureSources(options);
        }

        /// <summary>
        /// Configures the default <see cref="MetricSourceOptions"/> used for <see cref="MetricSource"/> instances
        /// passed to the collector.
        /// </summary>
        public static IMetricsCollectorBuilder ConfigureSources(this IMetricsCollectorBuilder builder, MetricSourceOptions options)
        {
            builder.Services.RemoveAll<MetricSourceOptions>();
            builder.Services.AddSingleton(options);
            return builder;
        }


        /// <summary>
        /// Registers an <see cref="MetricSource" /> for use with the collector.
        /// </summary>
        public static IMetricsCollectorBuilder AddSource<T>(this IMetricsCollectorBuilder builder) where T : MetricSource
        {
            builder.Services.AddSingleton<T>();
            builder.Services.AddSingleton<MetricSource>(s => s.GetService<T>());
            return builder;
        }

        /// <summary>
        /// Registers an <see cref="MetricSource" /> for use with the collector.
        /// </summary>
        public static IMetricsCollectorBuilder AddSource<T>(this IMetricsCollectorBuilder builder, T source) where T : MetricSource
        {
            builder.Services.AddSingleton<T>(source);
            builder.Services.AddSingleton<MetricSource>(s => s.GetService<T>());
            return builder;
        }

        private static IMetricsCollectorBuilder AddSourceIfMissing<T>(this IMetricsCollectorBuilder builder) where T : MetricSource
        {
            if (!builder.Services.Any(x => x.ServiceType == typeof(T)))
            {
                builder.AddSource<T>();
            }
            return builder;
        }
    }
}
