using AgroSolutions.Identity.Domain.Entities;

namespace AgroSolutions.Identity.Domain.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
