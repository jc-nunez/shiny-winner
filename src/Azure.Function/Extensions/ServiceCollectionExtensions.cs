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
        // Storage configuration - simplified to use connection strings only
        services.Configure<StorageConfiguration>(options =>
        {
            // Get connection strings from various sources
            options.SourceStorageConnection = configuration.GetConnectionString("SourceStorageConnection") 
                ?? configuration["SourceStorageConnection"] 
                ?? throw new InvalidOperationException("SourceStorageConnection is required");
                
            options.DestinationStorageConnection = configuration.GetConnectionString("DestinationStorageConnection") 
                ?? configuration["DestinationStorageConnection"] 
                ?? throw new InvalidOperationException("DestinationStorageConnection is required");
                
            options.TableStorageConnection = configuration.GetConnectionString("TableStorageConnection") 
                ?? configuration["TableStorageConnection"] 
                ?? throw new InvalidOperationException("TableStorageConnection is required");
        });

        // Service Bus configuration - simplified to use connection string only
        services.Configure<ServiceBusConfiguration>(options =>
        {
            // Get connection string from various sources
            options.ServiceBusConnection = configuration.GetConnectionString("ServiceBusConnection") 
                ?? configuration["ServiceBusConnection"] 
                ?? throw new InvalidOperationException("ServiceBusConnection is required");
            
            // Set default topic names
            options.StatusTopicName = configuration["ServiceBus:StatusTopicName"] ?? "document-status-updates";
            options.NotificationTopicName = configuration["ServiceBus:NotificationTopicName"] ?? "document-notifications";
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
        
        // Table storage provider - simple direct registration
        services.AddScoped<ITableStorageProvider, TableStorageProvider>();
        
        // Domain-specific state services
        services.AddScoped<IDocumentExtractionRequestStateService, DocumentExtractionRequestStateService>();

        // Messaging provider
        services.AddScoped<IServiceBusProvider, ServiceBusProvider>();

        // HTTP provider
        services.AddScoped<IHttpClientProvider, HttpClientProvider>();

        return services;
    }

    /// <summary>
    /// Registers all business services
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IDocumentExtractionHubService, DocumentExtractionHubService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
