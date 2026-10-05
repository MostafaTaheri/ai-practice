using AIPractice.Domain.Abstractions;
using AIPractice.Domain.Users;

namespace AIPractice.Domain;

/// <summary>
/// Application-level unit-of-work marker.
/// Inherits all transactional operations from <see cref="IUnitOfWork"/>.
/// </summary>
public interface IAIPracticeUnitOfWork : IUnitOfWork
{
    IUserRepository Users { get; }
}
