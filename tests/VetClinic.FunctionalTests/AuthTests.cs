using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using VetClinic.FunctionalTests.Infrastructure;

namespace VetClinic.FunctionalTests;

public class AuthTests : ApiTestBase
{
    public AuthTests(VetClinicApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Login_ComAdminValido_RetornaTokenComRole()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "admin123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("token").GetString()));
        Assert.Equal("Admin", json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_ComSenhaErrada_Retorna401ProblemDetails()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "errada" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Auth.CredenciaisInvalidas", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetTutores_SemToken_Retorna401()
    {
        var response = await Client.GetAsync("/api/tutores");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostTutor_ComRoleUser_Retorna403()
    {
        await AuthenticateAsUserAsync();

        var response = await Client.PostAsJsonAsync("/api/tutores", new { nome = "Sem Permissão", email = UniqueEmail(), cep = "01001-000" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTutores_ComRoleUser_Retorna200()
    {
        await AuthenticateAsUserAsync();

        var response = await Client.GetAsync("/api/tutores");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
