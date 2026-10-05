using AIPractice.Application.Abstractions;
using AIPractice.Application.Exceptions;
using static AIPractice.Application.Features.Users.Exceptions.BaseBusinessUserException;

namespace AIPractice.Application.Features.Users.CreateUser;

public class CreateUserValidator : BaseBusinessValidator
{
    private readonly CreateUserCommand _request;

    public CreateUserValidator(CreateUserCommand request)
    {
        _request = request;
    }

    public override async Task<List<BusinessException>> ValidateAsync()
    {
        var exceptions = new List<BusinessException>();

        if (string.IsNullOrWhiteSpace(_request.PhoneNumber))
        {
            exceptions.Add(new UserValidationException(message: "Invalid phone number"));
        }

        return exceptions;
    }
}