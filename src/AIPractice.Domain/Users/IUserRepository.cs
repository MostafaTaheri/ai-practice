using AIPractice.Domain.Users.ValueObjects;

namespace AIPractice.Domain.Users;

public interface IUserRepository
{
    Task<User> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPhoneNumberAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
