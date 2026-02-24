using AgroSolutions.Identity.Domain.Entities;

namespace AgroSolutions.Identity.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task CreateAsync(User user);
    Task<bool> EmailExistsAsync(string email);
}
