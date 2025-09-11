using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Azure.Data.Tables;
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
        
        // Generic table storage factory and provider
        services.AddSingleton<ITableStorageFactory, TableStorageFactory>();
        services.AddScoped<ITableStorageProvider>(sp => 
            sp.GetRequiredService<ITableStorageFactory>().GetProvider("DocumentRequests"));
        
        // Domain-specific state services (using generic repository internally)
        services.AddScoped<IDocumentExtractionRequestStateService, DocumentExtractionRequestStateService>();

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
        services.AddScoped<IDocumentExtractionHubService, DocumentExtractionHubService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}

/// <summary>
/// Generic service registration extensions for any domain
/// </summary>
public static class GenericServiceCollectionExtensions
{
    /// <summary>
    /// Adds a generic repository for any entity type
    /// </summary>
    public static IServiceCollection AddRepository<TEntity>(
        this IServiceCollection services, 
        string? tableName = null, 
        string partitionKey = "DefaultPartition",
        ServiceLifetime lifetime = ServiceLifetime.Scoped) 
        where TEntity : class, ITableEntity, new()
    {
        var serviceDescriptor = new ServiceDescriptor(
            typeof(IRepository<TEntity>),
            sp =>
            {
                var factory = sp.GetRequiredService<ITableStorageFactory>();
                return string.IsNullOrEmpty(tableName) 
                    ? factory.GetRepository<TEntity>(partitionKey)
                    : factory.GetRepository<TEntity>(tableName, partitionKey);
            },
            lifetime);

        services.Add(serviceDescriptor);
        return services;
    }

    /// <summary>
    /// Adds repositories for multiple entity types
    /// </summary>
    public static IServiceCollection AddRepositories<T1, T2>(
        this IServiceCollection services,
        string partitionKey = "DefaultPartition",
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where T1 : class, ITableEntity, new()
        where T2 : class, ITableEntity, new()
    {
        services.AddRepository<T1>(partitionKey: partitionKey, lifetime: lifetime);
        services.AddRepository<T2>(partitionKey: partitionKey, lifetime: lifetime);
        
        return services;
    }

    /// <summary>
    /// Adds typed repositories with fluent configuration
    /// </summary>
    public static IServiceCollection AddTypedRepositories(this IServiceCollection services, 
        Action<RepositoryRegistrationBuilder> configure)
    {
        var builder = new RepositoryRegistrationBuilder(services);
        configure(builder);
        return services;
    }
}

/// <summary>
/// Builder for repository registrations with fluent API
/// </summary>
public class RepositoryRegistrationBuilder
{
    private readonly IServiceCollection _services;

    internal RepositoryRegistrationBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Registers a repository for an entity type
    /// </summary>
    public RepositoryRegistrationBuilder AddRepository<TEntity>(
        string? tableName = null,
        string partitionKey = "DefaultPartition",
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TEntity : class, ITableEntity, new()
    {
        _services.AddRepository<TEntity>(tableName, partitionKey, lifetime);
        return this;
    }

    /// <summary>
    /// Registers multiple repositories with the same configuration
    /// </summary>
    public RepositoryRegistrationBuilder AddRepositories<T1, T2>(
        string partitionKey = "DefaultPartition",
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where T1 : class, ITableEntity, new()
        where T2 : class, ITableEntity, new()
    {
        AddRepository<T1>(partitionKey: partitionKey, lifetime: lifetime);
        AddRepository<T2>(partitionKey: partitionKey, lifetime: lifetime);
        return this;
    }
}

/// <summary>
/// Domain-specific extensions using generic infrastructure
/// </summary>
public static class DomainSpecificServiceCollectionExtensions
{
    /// <summary>
    /// Adds document processing repositories using the generic infrastructure
    /// </summary>
    public static IServiceCollection AddDocumentProcessingRepositories(this IServiceCollection services)
    {
        return services.AddTypedRepositories(builder =>
            builder.AddRepository<Models.RequestTrackingEntity>(
                tableName: "DocumentRequests",
                partitionKey: "DocumentRequests"));
    }
}
