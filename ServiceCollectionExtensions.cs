﻿using Microsoft.Extensions.Hosting;
using StackExchange.Metrics;
using StackExchange.Metrics.DependencyInjection;
#if NETCOREAPP
using StackExchange.Metrics.Infrastructure;
#endif

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Metrics collection extensions for <see cref="IServiceCollection" />.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures a <see cref="IMetricsCollector" /> and adds it to the service collection.
        /// </summary>
        /// <param name="services">
        /// An <see cref="IServiceCollection" /> to add the collector to.
        /// </param>
        public static IMetricsCollectorBuilder AddMetricsCollector(this IServiceCollection services)
        {
            var builder = new MetricsCollectorBuilder(services);

            services
                .AddSingleton<MetricsCollector>()
                .AddSingleton<IMetricsCollector>(s => s.GetService<MetricsCollector>())
                .AddSingleton<IHostedService>(s => s.GetService<MetricsCollector>())
                .AddSingleton<MetricSourceOptions>()
                .AddSingleton<MetricsCollectorOptions>(s => builder.Build(s));
#if NETCOREAPP
            services
                .AddSingleton<DiagnosticsCollector>()
                .AddSingleton<IDiagnosticsCollector>(s => s.GetService<DiagnosticsCollector>())
                .AddSingleton<IHostedService>(s => s.GetService<DiagnosticsCollector>());
#endif

            return builder;
        }
    }
}
