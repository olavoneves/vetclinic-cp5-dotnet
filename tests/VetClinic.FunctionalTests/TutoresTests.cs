using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using VetClinic.FunctionalTests.Infrastructure;

namespace VetClinic.FunctionalTests;

public class TutoresTests : ApiTestBase
{
    public TutoresTests(VetClinicApiFactory factory) : base(factory) { }

    private static List<string> Rels(JsonElement element) =>
        element.GetProperty("links").EnumerateArray().Select(l => l.GetProperty("rel").GetString()!).ToList();

    [Fact]
    public async Task CriarTutor_ComAdmin_Retorna201ComEnderecoELinksHateoas()
    {
        await AuthenticateAsAdminAsync();
        var email = UniqueEmail();

        var response = await Client.PostAsJsonAsync("/api/tutores", new { nome = "Maria Teste", email, cep = "01001-000" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var tutor = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, tutor.GetProperty("email").GetString());
        Assert.Equal("Praça da Sé", tutor.GetProperty("logradouro").GetString()); // preenchido pelo (mock do) ViaCEP
        Assert.Equal("SP", tutor.GetProperty("uf").GetString());

        var rels = Rels(tutor);
        Assert.Contains("self", rels);
        Assert.Contains("update", rels);
        Assert.Contains("delete", rels);
        Assert.Contains("pets", rels);
    }

    [Fact]
    public async Task ListarTutores_Paginado_RetornaMetadadosELinksDePagina()
    {
        await AuthenticateAsAdminAsync();
        await CreateTutorAsync();
        await CreateTutorAsync();

        var response = await Client.GetAsync("/api/tutores?pageNumber=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, page.GetProperty("pageNumber").GetInt32());
        Assert.Equal(1, page.GetProperty("pageSize").GetInt32());
        Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.True(page.GetProperty("totalItems").GetInt32() >= 2);
        Assert.True(page.GetProperty("totalPages").GetInt32() >= 2);
        Assert.False(page.GetProperty("hasPrevious").GetBoolean());
        Assert.True(page.GetProperty("hasNext").GetBoolean());

        var rels = Rels(page);
        Assert.Contains("self", rels);
        Assert.Contains("first", rels);
        Assert.Contains("last", rels);
        Assert.Contains("next", rels);
        Assert.DoesNotContain("previous", rels);
    }

    [Fact]
    public async Task ListarTutores_ComPageSizeAcimaDe50_LimitaEm50()
    {
        await AuthenticateAsUserAsync();

        var response = await Client.GetAsync("/api/tutores?pageNumber=1&pageSize=500");

        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(50, page.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task BuscarTutor_Inexistente_Retorna404Customizado()
    {
        await AuthenticateAsUserAsync();

        var response = await Client.GetAsync("/api/tutores/999999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Tutor.NaoEncontrado", problem.GetProperty("code").GetString());
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task CriarTutor_ComEmailDuplicado_Retorna409()
    {
        await AuthenticateAsAdminAsync();
        var email = UniqueEmail();
        await CreateTutorAsync(email);

        var response = await Client.PostAsJsonAsync("/api/tutores", new { nome = "Duplicado", email, cep = "01001-000" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Tutor.EmailDuplicado", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CriarTutor_ComViaCepFora_Retorna503SemDerrubarApi()
    {
        await AuthenticateAsAdminAsync();

        var response = await Client.PostAsJsonAsync("/api/tutores",
            new { nome = "Sem CEP", email = UniqueEmail(), cep = VetClinicApiFactory.CepViaCepFora });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cep.ServicoIndisponivel", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AtualizarEDeletarTutor_FluxoCompleto()
    {
        await AuthenticateAsAdminAsync();
        var tutor = await CreateTutorAsync();
        var id = tutor.GetProperty("id").GetInt32();

        var update = await Client.PutAsJsonAsync($"/api/tutores/{id}", new { nome = "Nome Atualizado", email = UniqueEmail(), cep = "01001000" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Nome Atualizado", updated.GetProperty("nome").GetString());

        var delete = await Client.DeleteAsync($"/api/tutores/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var get = await Client.GetAsync($"/api/tutores/{id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }
}
