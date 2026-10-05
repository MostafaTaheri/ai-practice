using AIPractice.Domain;
using AIPractice.Domain.Users;
using AIPractice.Infrastructure.UMS.Users;

namespace AIPractice.Infrastructure.Data;

public class ApplicationUnitOfWork(AIPracticeDbContext dbContext) : BaseUnitOfWork(dbContext), IAIPracticeUnitOfWork, IDisposable
{
    private readonly AIPracticeDbContext _dbContext = dbContext;
    private IUserRepository _userRepository;
    public IUserRepository Users => _userRepository ??= new UserRepository(_dbContext);
}