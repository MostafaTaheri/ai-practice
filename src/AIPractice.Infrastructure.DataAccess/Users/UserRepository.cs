using Microsoft.EntityFrameworkCore;
using AIPractice.Domain.Users;
using AIPractice.Domain.Users.ValueObjects;
using AIPractice.Infrastructure.Data;

namespace AIPractice.Infrastructure.UMS.Users;

public class UserRepository : IUserRepository
{
    private readonly AIPracticeDbContext _context;

    public UserRepository(AIPracticeDbContext context)
    {
        _context = context;
    }

    public async Task<User> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByPhoneNumberAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }
}