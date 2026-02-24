using AgroSolutions.Property.Domain.Entities;
using AgroSolutions.Property.Domain.Interfaces;
using AgroSolutions.Property.Infrastructure.Data;
using MongoDB.Driver;

namespace AgroSolutions.Property.Infrastructure.Repositories;

public class PropertyRepository : IPropertyRepository
{
    private readonly MongoDbContext _context;

    public PropertyRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<FarmProperty>> GetByOwnerAsync(string ownerId, int page, int pageSize)
    {
        return await _context.Properties
            .Find(p => p.ProprietarioId == ownerId && p.Ativo)
            .SortByDescending(p => p.DataCadastro)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByOwnerAsync(string ownerId)
    {
        return (int)await _context.Properties
            .CountDocumentsAsync(p => p.ProprietarioId == ownerId && p.Ativo);
    }

    public async Task<FarmProperty?> GetByIdAsync(string id)
    {
        return await _context.Properties
            .Find(p => p.Id == id && p.Ativo)
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(FarmProperty property)
    {
        await _context.Properties.InsertOneAsync(property);
    }

    public async Task UpdateAsync(FarmProperty property)
    {
        await _context.Properties.ReplaceOneAsync(p => p.Id == property.Id, property);
    }

    public async Task<bool> NameExistsForOwnerAsync(string ownerId, string nome, string? excludeId = null)
    {
        var filter = Builders<FarmProperty>.Filter.And(
            Builders<FarmProperty>.Filter.Eq(p => p.ProprietarioId, ownerId),
            Builders<FarmProperty>.Filter.Eq(p => p.Nome, nome),
            Builders<FarmProperty>.Filter.Eq(p => p.Ativo, true)
        );

        if (excludeId != null)
            filter &= Builders<FarmProperty>.Filter.Ne(p => p.Id, excludeId);

        return await _context.Properties.Find(filter).AnyAsync();
    }
}
