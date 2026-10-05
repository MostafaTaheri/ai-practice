using AIPractice.Application.Features.Users.CreateUser;
using AIPractice.Domain.Users;

namespace AIPractice.Application.Users.Backoffice.CreateUser;

public interface ICreateUserService
{
    /// <summary>
    /// Creates a new user with the given phone number in both the database and Keycloak.
    /// The user is created with PendingVerification status.
    /// </summary>
    /// <param name="command">The command containing user creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created user entity</returns>
    Task<User> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default);
}
