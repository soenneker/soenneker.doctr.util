using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.DocTr.Util.Abstract;
using Soenneker.Python.Util.Registrars;

namespace Soenneker.DocTr.Util.Registrars;

/// <summary>
/// A cross-platform docTR OCR utility for .NET using Python.
/// </summary>
public static class DocTrUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IDocTrUtil"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddDocTrUtilAsSingleton(this IServiceCollection services)
    {
        services.AddPythonUtilAsSingleton();
        services.TryAddSingleton<DocTrOptions>();
        services.TryAddSingleton<IDocTrUtil, DocTrUtil>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IDocTrUtil"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddDocTrUtilAsScoped(this IServiceCollection services)
    {
        services.AddPythonUtilAsScoped();
        services.TryAddSingleton<DocTrOptions>();
        services.TryAddScoped<IDocTrUtil, DocTrUtil>();

        return services;
    }
}
