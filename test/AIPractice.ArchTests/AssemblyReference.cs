using System.Reflection;
using AIPractice.Application.Abstractions;
using AIPractice.Api.Controllers;
using AIPractice.Domain.Abstractions;
using AIPractice.Infrastructure.Data;

namespace AIPractice.ArchTests;

internal static class AssemblyReference
{
    public static readonly Assembly Domain = typeof(Entity<>).Assembly;
    public static readonly Assembly Application = typeof(BaseBusinessValidator).Assembly;
    public static readonly Assembly Infrastructure = typeof(ApplicationUnitOfWork).Assembly;
    public static readonly Assembly Api = typeof(UserController).Assembly;
}
