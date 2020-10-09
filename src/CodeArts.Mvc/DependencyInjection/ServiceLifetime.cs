#if NET40 || NET45 || NET451 || NET452 || NET461

namespace CodeArts.Mvc.DependencyInjection
{
    /// <summary>
    /// Specifies the lifetime of a service in an <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// Specifies that a single instance of the service will be created.
        /// </summary>
        Singleton,
#if !NET40
        /// <summary>
        /// Specifies that a new instance of the service will be created for each scope.
        /// </summary>
        /// <remarks>
        /// In ASP.NET Core applications a scope is created around each server request.
        /// </remarks>
        Scoped,
#endif
        /// <summary>
        /// Specifies that a new instance of the service will be created every time it is requested.
        /// </summary>
        Transient
    }
}
#endif