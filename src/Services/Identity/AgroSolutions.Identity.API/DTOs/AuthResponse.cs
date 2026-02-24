namespace AgroSolutions.Identity.API.DTOs;

public record AuthResponse(string Token, int ExpiresIn, UserDto User);
