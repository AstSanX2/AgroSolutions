using AgroSolutions.Property.Domain.Entities;

namespace AgroSolutions.Property.Domain.Interfaces;

public interface IPropertyRepository
{
    Task<List<FarmProperty>> GetByOwnerAsync(string ownerId, int page, int pageSize);
    Task<int> CountByOwnerAsync(string ownerId);
    Task<FarmProperty?> GetByIdAsync(string id);
    Task CreateAsync(FarmProperty property);
    Task UpdateAsync(FarmProperty property);
    Task<bool> NameExistsForOwnerAsync(string ownerId, string nome, string? excludeId = null);
    Task<List<FarmProperty>> GetAllActiveAsync();
}
