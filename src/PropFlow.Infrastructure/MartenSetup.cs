using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace PropFlow.Infrastructure;

public static class MartenSetup
{
    /// <summary>
    /// Registers Marten as the document store for all aggregates.
    /// Multi-tenancy: Marten's built-in conjoined tenancy maps each TenantId
    /// to the schema "tenant_{tenantId:N}" via the schema-per-tenant strategy.
    /// Finbuckle middleware sets the current tenant before each request.
    /// </summary>
    public static IServiceCollection AddMartenDocumentStore(
        this IServiceCollection services,
        string connectionString)
    {
        services
            .AddMarten(opts =>
            {
                opts.Connection(connectionString);
                opts.AutoCreateSchemaObjects = AutoCreate.All;

                // Enable conjoined multi-tenancy — each tenant gets its own schema.
                // Schema name resolved from ITenantContext at session creation time.
                opts.Policies.AllDocumentsAreMultiTenanted();
            })
            .UseLightweightSessions();

        return services;
    }
}
