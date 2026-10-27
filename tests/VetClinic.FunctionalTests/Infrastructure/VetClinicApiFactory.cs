using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using VetClinic.Application.Common;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;

namespace VetClinic.FunctionalTests.Infrastructure;

/// <summary>
/// Sobe a API real contra o MESMO SQL Server do container Docker (sem SQLite/InMemory).
/// Apenas o ICepService é substituído por um mock, para não depender da internet.
/// </summary>
public class VetClinicApiFactory : WebApplicationFactory<Program>
{
    /// <summary>CEP que o mock trata como "ViaCEP fora do ar" (503).</summary>
    public const string CepViaCepFora = "00000-000";

    public VetClinicApiFactory()
    {
        // Variáveis de ambiente são lidas pelo WebApplication.CreateBuilder antes do Program usar a configuração.
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__Default")))
            Environment.SetEnvironmentVariable("ConnectionStrings__Default", TestSettings.BuildConnectionString());

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Jwt__Key")))
            Environment.SetEnvironmentVariable("Jwt__Key", "chave-jwt-usada-somente-nos-testes-funcionais-0123456789");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            var cepMock = new Mock<ICepService>();
            cepMock
                .Setup(c => c.GetAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string cep, CancellationToken _) => cep == CepViaCepFora
                    ? OperationResult<EnderecoDto>.Failure(new Error("Cep.ServicoIndisponivel", "ViaCEP indisponível (mock)."), 503)
                    : OperationResult<EnderecoDto>.Success(new EnderecoDto(cep, "Praça da Sé", "Sé", "São Paulo", "SP")));

            services.RemoveAll<ICepService>();
            services.AddSingleton(cepMock.Object);
        });
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<VetClinicApiFactory>
{
    public const string Name = "api";
}
