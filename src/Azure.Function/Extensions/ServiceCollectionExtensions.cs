using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Azure.Function.Configuration;
using Azure.Function.Services;
using Azure.Function.Providers.Storage;
using Azure.Function.Providers.Messaging;
using Azure.Function.Providers.Http;

namespace Azure.Function.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all application services, providers, and configurations to the DI container
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register strongly-typed configuration options
        services.AddApplicationConfiguration(configuration);

        // Register infrastructure providers
        services.AddInfrastructureProviders();

        // Register business services
        services.AddBusinessServices();


        return services;
    }

    /// <summary>
    /// Registers all strongly-typed configuration classes
    /// </summary>
    public static IServiceCollection AddApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Storage configuration - bind from Storage section and legacy connection strings
        services.Configure<StorageConfiguration>(options =>
        {
            // Bind the Storage section (for new multi-account configuration)
            configuration.GetSection("Storage").Bind(options);
            
            // Legacy connection strings for backward compatibility (only if not already configured)
            if (string.IsNullOrEmpty(options.SourceStorageConnection))
            {
                options.SourceStorageConnection = configuration.GetConnectionString("SourceStorageConnection") 
                    ?? configuration["SourceStorageConnection"] 
                    ?? throw new InvalidOperationException("SourceStorageConnection is required when not using new Storage configuration");
            }
            
            if (string.IsNullOrEmpty(options.DestinationStorageConnection))
            {
                options.DestinationStorageConnection = configuration.GetConnectionString("DestinationStorageConnection") 
                    ?? configuration["DestinationStorageConnection"] 
                    ?? throw new InvalidOperationException("DestinationStorageConnection is required when not using new Storage configuration");
            }
            
            if (string.IsNullOrEmpty(options.TableStorageConnection))
            {
                options.TableStorageConnection = configuration.GetConnectionString("TableStorageConnection") 
                    ?? configuration["TableStorageConnection"] 
                    ?? throw new InvalidOperationException("TableStorageConnection is required");
            }
        });

        // Service Bus configuration - bind from ServiceBus section and legacy connection strings
        services.Configure<ServiceBusConfiguration>(options =>
        {
            // Bind the ServiceBus section (for new managed identity configuration)
            configuration.GetSection("ServiceBus").Bind(options);
            
            // Legacy connection string for backward compatibility (only if not already configured)
            if (string.IsNullOrEmpty(options.ServiceBusConnection))
            {
                options.ServiceBusConnection = configuration.GetConnectionString("ServiceBusConnection") 
                    ?? configuration["ServiceBusConnection"] 
                    ?? throw new InvalidOperationException("ServiceBusConnection is required when not using new ServiceBus configuration");
            }
            
            // Set default topic names if not configured
            if (string.IsNullOrEmpty(options.StatusTopicName))
                options.StatusTopicName = "document-status-updates";
            if (string.IsNullOrEmpty(options.NotificationTopicName))
                options.NotificationTopicName = "document-notifications";
        });

        // External API configuration
        services.Configure<ApiConfiguration>(configuration.GetSection("ExternalApi"));

        // Monitoring configuration
        services.Configure<MonitoringConfiguration>(configuration.GetSection("Monitoring"));

        // Resilience configuration
        services.Configure<ResilienceConfiguration>(configuration.GetSection("Resilience"));

        return services;
    }

    /// <summary>
    /// Registers all infrastructure providers (Storage, Messaging, HTTP)
    /// </summary>
    public static IServiceCollection AddInfrastructureProviders(this IServiceCollection services)
    {
        // Storage providers - using factory pattern for multi-account support
        services.AddSingleton<IBlobStorageProviderFactory, BlobStorageProviderFactory>();
        services.AddScoped<ITableStorageProvider, TableStorageProvider>();

        // Messaging provider
        services.AddScoped<IServiceBusProvider, ServiceBusProvider>();

        // HTTP provider - simplified without HttpClient dependencies
        services.AddScoped<IHttpClientProvider, HttpClientProvider>();

        return services;
    }

    /// <summary>
    /// Registers all business services
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IDocumentHubService, DocumentHubService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }

}
