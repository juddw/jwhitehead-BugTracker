using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(jwhitehead_BugTracker.Startup))]
namespace jwhitehead_BugTracker
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
