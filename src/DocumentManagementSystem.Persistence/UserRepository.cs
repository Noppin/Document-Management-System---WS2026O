using DocumentManagementSystem.Domain.Entities;

namespace DocumentManagementSystem.Persistence;

public sealed class UserRepository(DocumentManagementDbContext dbContext) : IUserRepository
{
    public User? GetByNormalizedUsername(string normalizedUsername) =>
        dbContext.Users.SingleOrDefault(user => user.NormalizedUsername == normalizedUsername);

    public bool ExistsByNormalizedUsername(string normalizedUsername) =>
        dbContext.Users.Any(user => user.NormalizedUsername == normalizedUsername);

    public User Add(User user)
    {
        dbContext.Users.Add(user);
        dbContext.SaveChanges();
        return user;
    }
}
