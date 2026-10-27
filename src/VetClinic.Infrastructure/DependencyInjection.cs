using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VetClinic.Application.Interfaces;
using VetClinic.Infrastructure.ExternalServices;
using VetClinic.Infrastructure.Persistence;
using VetClinic.Infrastructure.Repositories;

namespace VetClinic.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<VetClinicDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<ITutorRepository, TutorRepository>();
        services.AddScoped<IPetRepository, PetRepository>();

        // HttpClient tipado + resiliência padrão (retry, circuit breaker, timeouts).
        services.AddHttpClient<ICepService, ViaCepService>(client =>
            {
                client.BaseAddress = new Uri("https://viacep.com.br/ws/");
                client.Timeout = TimeSpan.FromSeconds(20);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(4);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(12);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            });

        return services;
    }
}
