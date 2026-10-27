using Moq;
using VetClinic.Application.Common;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Application.Services;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Errors;

namespace VetClinic.UnitTests;

public class TutorServiceTests
{
    private readonly Mock<ITutorRepository> _repository = new();
    private readonly Mock<ICepService> _cepService = new();
    private readonly TutorService _service;

    private static readonly EnderecoDto EnderecoSe = new("01001-000", "Praça da Sé", "Sé", "São Paulo", "SP");

    public TutorServiceTests()
    {
        _service = new TutorService(_repository.Object, _cepService.Object);
    }

    private static TutorRequest NovoRequest() => new() { Nome = "Ana Souza", Email = "ANA@Email.com ", Cep = "01001-000" };

    [Fact]
    public async Task CreateAsync_ComCepValido_PreencheEnderecoERetorna201()
    {
        _cepService.Setup(c => c.GetAddressAsync("01001-000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<EnderecoDto>.Success(EnderecoSe));

        var result = await _service.CreateAsync(NovoRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("Praça da Sé", result.Value!.Logradouro);
        Assert.Equal("São Paulo", result.Value.Cidade);
        Assert.Equal("SP", result.Value.Uf);
        Assert.Equal("ana@email.com", result.Value.Email);
        _repository.Verify(r => r.AddAsync(It.Is<Tutor>(t => t.Bairro == "Sé"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_QuandoViaCepIndisponivel_Retorna503ENaoSalva()
    {
        _cepService.Setup(c => c.GetAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<EnderecoDto>.Failure(new Error("Cep.ServicoIndisponivel", "ViaCEP fora"), 503));

        var result = await _service.CreateAsync(NovoRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal(503, result.StatusCode);
        Assert.Equal("Cep.ServicoIndisponivel", result.Error!.Code);
        _repository.Verify(r => r.AddAsync(It.IsAny<Tutor>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ComCepInvalido_Retorna400()
    {
        _cepService.Setup(c => c.GetAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<EnderecoDto>.Failure(new Error("Cep.Invalido", "CEP inválido"), 400));

        var result = await _service.CreateAsync(NovoRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ComEmailDuplicado_PropagaConflictException()
    {
        _cepService.Setup(c => c.GetAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<EnderecoDto>.Success(EnderecoSe));
        _repository.Setup(r => r.AddAsync(It.IsAny<Tutor>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Tutor.EmailDuplicado", "duplicado"));

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(NovoRequest()));
        Assert.Equal("Tutor.EmailDuplicado", ex.Code);
    }

    [Fact]
    public async Task GetByIdAsync_QuandoNaoExiste_Retorna404()
    {
        _repository.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Tutor>.Failure(new Error("Tutor.NaoEncontrado", "não existe"), 404));

        var result = await _service.GetByIdAsync(42);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Tutor.NaoEncontrado", result.Error!.Code);
    }

    [Fact]
    public async Task GetByIdAsync_QuandoExiste_RetornaTutor()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Tutor>.Success(new Tutor { Id = 1, Nome = "Ana", Email = "ana@email.com", Cep = "01001-000" }));

        var result = await _service.GetByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("Ana", result.Value!.Nome);
    }

    [Fact]
    public async Task DeleteAsync_QuandoNaoExiste_Retorna404ENaoRemove()
    {
        _repository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Tutor>.Failure(new Error("Tutor.NaoEncontrado", "não existe"), 404));

        var result = await _service.DeleteAsync(7);

        Assert.Equal(404, result.StatusCode);
        _repository.Verify(r => r.DeleteAsync(It.IsAny<Tutor>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_QuandoExiste_AtualizaDadosEEndereco()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Tutor>.Success(new Tutor { Id = 1, Nome = "Antigo", Email = "a@a.com", Cep = "20040-020" }));
        _cepService.Setup(c => c.GetAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<EnderecoDto>.Success(EnderecoSe));

        var result = await _service.UpdateAsync(1, NovoRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("Ana Souza", result.Value!.Nome);
        Assert.Equal("01001-000", result.Value.Cep);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Tutor>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 0, 1, 10)]
    [InlineData(2, 500, 2, 50)]
    [InlineData(-3, 20, 1, 20)]
    public async Task GetPagedAsync_NormalizaPaginacao(int page, int size, int expectedPage, int expectedSize)
    {
        _repository.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageResult<TutorResponse>());

        await _service.GetPagedAsync(page, size);

        _repository.Verify(r => r.GetPagedAsync(expectedPage, expectedSize, It.IsAny<CancellationToken>()), Times.Once);
    }
}
