namespace AgroSolutions.Property.API.DTOs;

// Requests
public record CreatePropertyRequest(string Nome, string Endereco);
public record UpdatePropertyRequest(string Nome, string Endereco);

// Responses
public record PropertyDto(
    string Id,
    string Nome,
    string Endereco,
    DateTime DataCadastro,
    List<PlotDto> Talhoes
);

public record PaginatedResponse<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);
