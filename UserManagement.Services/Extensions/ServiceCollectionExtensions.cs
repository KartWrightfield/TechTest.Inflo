using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Services.Implementations;
using UserManagement.Services.Interfaces;
using UserManagement.Services.Mapping;
using UserManagement.Services.Validation;

namespace UserManagement.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddAutoMapper(_ => { }, typeof(MappingProfile));
        services.AddValidatorsFromAssemblyContaining<UserInputViewModelValidator>();

        return services;
    }
}
