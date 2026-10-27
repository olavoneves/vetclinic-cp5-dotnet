using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using VetClinic.FunctionalTests.Infrastructure;

namespace VetClinic.FunctionalTests;

public class PetsTests : ApiTestBase
{
    public PetsTests(VetClinicApiFactory factory) : base(factory) { }

    [Fact]
    public async Task CriarPet_ComTutorInexistente_Retorna404ViaExceptionLancada()
    {
        await AuthenticateAsAdminAsync();

        var response = await Client.PostAsJsonAsync("/api/pets",
            new { nome = "Fantasma", especie = "Gato", dataNascimento = "2021-03-01", tutorId = 999999999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Tutor.NaoEncontrado", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CriarPet_ComNascimentoFuturo_Retorna422()
    {
        await AuthenticateAsAdminAsync();
        var tutor = await CreateTutorAsync();

        var response = await Client.PostAsJsonAsync("/api/pets", new
        {
            nome = "Futuro",
            especie = "Cachorro",
            dataNascimento = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd"),
            tutorId = tutor.GetProperty("id").GetInt32()
        });

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    [Fact]
    public async Task CriarPet_EListarPorTutor_RetornaPetComLinks()
    {
        await AuthenticateAsAdminAsync();
        var tutor = await CreateTutorAsync();
        var tutorId = tutor.GetProperty("id").GetInt32();

        var create = await Client.PostAsJsonAsync("/api/pets",
            new { nome = "Rex", especie = "Cachorro", raca = "Vira-lata", dataNascimento = "2020-05-10", tutorId });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var pet = await create.Content.ReadFromJsonAsync<JsonElement>();
        var rels = pet.GetProperty("links").EnumerateArray().Select(l => l.GetProperty("rel").GetString()).ToList();
        Assert.Contains("self", rels);
        Assert.Contains("tutor", rels);

        var list = await Client.GetAsync($"/api/pets?tutorId={tutorId}&pageNumber=1&pageSize=10");
        var page = await list.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, page.GetProperty("totalItems").GetInt32());
        Assert.Equal("Rex", page.GetProperty("items")[0].GetProperty("nome").GetString());
    }
}
