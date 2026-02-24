using AgroSolutions.Identity.Domain.Entities;
using AgroSolutions.Identity.Domain.Interfaces;
using AgroSolutions.Identity.Infrastructure.Data;
using MongoDB.Driver;

namespace AgroSolutions.Identity.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly MongoDbContext _context;

    public UserRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        return await _context.Users
            .Find(u => u.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Find(u => u.Email == email.ToLowerInvariant())
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(User user)
    {
        user.Email = user.Email.ToLowerInvariant();
        await _context.Users.InsertOneAsync(user);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users
            .Find(u => u.Email == email.ToLowerInvariant())
            .AnyAsync();
    }
}
