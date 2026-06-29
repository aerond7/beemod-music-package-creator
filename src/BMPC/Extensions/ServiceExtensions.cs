using BMPC.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace BMPC.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddFactory<T>(this IServiceCollection services, Func<T> factory)
            where T : class
        {
            services.AddSingleton(factory);
            services.AddSingleton<IAbstractFactory<T>, AbstractFactory<T>>();

            return services;
        }

        public static IServiceCollection AddFormFactory<TForm>(this IServiceCollection services)
            where TForm : Window
        {
            services.AddTransient<TForm>();
            services.AddSingleton<Func<TForm>>(x => () => x.GetService<TForm>()!);
            services.AddSingleton<IAbstractFactory<TForm>, AbstractFactory<TForm>>();

            return services;
        }
    }
}
