using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VetClinic.Infrastructure.Persistence;

namespace VetClinic.FunctionalTests.Infrastructure;

/// <summary>
/// Base dos testes funcionais: cada teste cria dados com e-mail único (Guid)
/// e, ao final, remove tudo o que criou (pets caem junto por cascade).
/// </summary>
[Collection(ApiCollection.Name)]
public abstract class ApiTestBase : IAsyncLifetime
{
    protected readonly VetClinicApiFactory Factory;
    protected readonly HttpClient Client;
    private readonly List<string> _createdEmails = new();

    protected ApiTestBase(VetClinicApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected string UniqueEmail()
    {
        var email = $"func-{Guid.NewGuid():N}@teste.com";
        _createdEmails.Add(email);
        return email;
    }

    protected async Task<string> LoginAsync(string username, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("token").GetString()!;
    }

    protected async Task AuthenticateAsAdminAsync() => SetToken(await LoginAsync("admin", "admin123"));
    protected async Task AuthenticateAsUserAsync() => SetToken(await LoginAsync("user", "user123"));

    private void SetToken(string token) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    protected async Task<JsonElement> CreateTutorAsync(string? email = null, string cep = "01001-000")
    {
        var response = await Client.PostAsJsonAsync("/api/tutores", new
        {
            nome = $"Tutor Teste {Guid.NewGuid():N}"[..30],
            email = email ?? UniqueEmail(),
            cep
        });
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_createdEmails.Count == 0) return;

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VetClinicDbContext>();
        await db.Tutores.Where(t => _createdEmails.Contains(t.Email)).ExecuteDeleteAsync();
    }
}
