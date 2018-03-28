using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LauncherClient.Owin
{
    public class ApiHost
    {
        public void StartHost()
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string baseurl = configuration.AppSettings.Settings["ApiURL"].Value; ;
            WebApp.Start<Startup>(baseurl);
        }
    }
}
