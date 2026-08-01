using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace FitBit.Api.Infrastructure;

/// <summary>
/// Drops a controller from the application model entirely. Used to make the
/// development-only endpoints not merely unreachable but absent outside
/// Development — an [Authorize] or an environment check inside each action would
/// still leave the routes registered.
/// </summary>
public sealed class RemoveControllerConvention : IApplicationModelConvention
{
    private readonly Type _controllerType;

    public RemoveControllerConvention(Type controllerType) => _controllerType = controllerType;

    public void Apply(ApplicationModel application)
    {
        var model = application.Controllers
            .FirstOrDefault(c => c.ControllerType.AsType() == _controllerType);

        if (model is not null) application.Controllers.Remove(model);
    }
}
