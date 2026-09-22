using System.Reflection;
using FluentValidation;
using FSEdu.Application.Features.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace FSEdu.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        // Auto-discount rule evaluator (no abstraction — concrete service)
        services.AddScoped<DiscountEvaluator>();

        return services;
    }
}
