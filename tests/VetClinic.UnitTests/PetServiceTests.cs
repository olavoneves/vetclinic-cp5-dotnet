using Moq;
using VetClinic.Application.Common;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Application.Services;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Errors;

namespace VetClinic.UnitTests;

public class PetServiceTests
{
    private readonly Mock<IPetRepository> _repository = new();
    private readonly PetService _service;

    public PetServiceTests()
    {
        _service = new PetService(_repository.Object);
    }

    private static PetRequest NovoRequest(int tutorId = 1, DateTime? nascimento = null) => new()
    {
        Nome = "Rex",
        Especie = "Cachorro",
        Raca = "Vira-lata",
        DataNascimento = nascimento ?? new DateTime(2020, 5, 10),
        TutorId = tutorId
    };

    [Fact]
    public async Task CreateAsync_ComDadosValidos_Retorna201()
    {
        var result = await _service.CreateAsync(NovoRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("Rex", result.Value!.Nome);
        _repository.Verify(r => r.AddAsync(It.Is<Pet>(p => p.TutorId == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ComTutorInexistente_PropagaNotFoundException()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Tutor.NaoEncontrado", "tutor não existe"));

        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(NovoRequest(tutorId: 999)));
    }

    [Fact]
    public async Task CreateAsync_ComNascimentoNoFuturo_LancaBusinessRuleException()
    {
        var request = NovoRequest(nascimento: DateTime.UtcNow.AddDays(10));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(request));

        Assert.Equal("Pet.DataNascimentoFutura", ex.Code);
        _repository.Verify(r => r.AddAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_QuandoNaoExiste_Retorna404()
    {
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Pet>.Failure(new Error("Pet.NaoEncontrado", "não existe"), 404));

        var result = await _service.GetByIdAsync(5);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_QuandoNaoExiste_Retorna404ENaoAtualiza()
    {
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Pet>.Failure(new Error("Pet.NaoEncontrado", "não existe"), 404));

        var result = await _service.UpdateAsync(5, NovoRequest());

        Assert.Equal(404, result.StatusCode);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_QuandoExiste_Retorna204()
    {
        _repository.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Pet>.Success(new Pet { Id = 3, Nome = "Mia", Especie = "Gato", TutorId = 1 }));

        var result = await _service.DeleteAsync(3);

        Assert.True(result.IsSuccess);
        Assert.Equal(204, result.StatusCode);
        _repository.Verify(r => r.DeleteAsync(It.Is<Pet>(p => p.Id == 3), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_LimitaPageSizeEm50EPassaFiltro()
    {
        _repository.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageResult<PetResponse>());

        await _service.GetPagedAsync(1, 1000, tutorId: 9);

        _repository.Verify(r => r.GetPagedAsync(1, 50, 9, It.IsAny<CancellationToken>()), Times.Once);
    }
}
