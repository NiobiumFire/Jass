using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(BelotWebApp.Startup))]
namespace BelotWebApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            app.MapSignalR();
        }
    }
}
