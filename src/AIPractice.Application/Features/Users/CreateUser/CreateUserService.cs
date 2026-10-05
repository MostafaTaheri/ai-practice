using AIPractice.Application.Exceptions;
using AIPractice.Application.Features.Users.CreateUser;
using AIPractice.Domain;
using AIPractice.Domain.Users;
using AIPractice.Domain.Users.ValueObjects;
using static AIPractice.Application.Features.Users.Exceptions.BaseBusinessUserException;

namespace AIPractice.Application.Users.Backoffice.CreateUser;

public class CreateUserService : ICreateUserService
{
    private readonly IAIPracticeUnitOfWork _unitOfWork;

    public CreateUserService(IAIPracticeUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<User> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        await new CreateUserValidator(command).AssertValidationAsync();

        var phoneNumber = PhoneNumber.Create(command.PhoneNumber);
        var user = User.Register(phoneNumber);

         try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw new UserValidationException("an error occoured");
        }

        return user;
    }
}
