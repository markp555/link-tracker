using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace LinkTracker.Tests;
internal class ScrapperNoSchedulerFactory : ScrapperMockedTestFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            var quartzServices = services.Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType?.Name.Contains("Quartz") == true).ToList();
            foreach (var service in quartzServices)
            {
                services.Remove(service);
            }
        });
    }
}
