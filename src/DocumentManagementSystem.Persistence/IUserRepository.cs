using DocumentManagementSystem.Domain.Entities;

namespace DocumentManagementSystem.Persistence;

public interface IUserRepository
{
    User? GetByNormalizedUsername(string normalizedUsername);
    bool ExistsByNormalizedUsername(string normalizedUsername);
    User Add(User user);
}
