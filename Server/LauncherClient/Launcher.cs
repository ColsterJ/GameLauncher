using LauncherClasses;
using LauncherClient.Owin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using LauncherServerClasses;

namespace LauncherClient
{
    public partial class Launcher : Form
    {
        private ApiHost host;
        private GameCommand gc = new GameCommand();
        private int gameStartDelay = 120; // number of grace seconds on starting a game
        private int gameEndDelay = 15; // number of grace seconds for allowing a restart
        private int currStartDelay = 0;
        private int currEndDelay = 0;

        private string baseURL;
        private string computerKey;

        private Encryption encryption;

        public Launcher()
        {
            InitializeComponent();

            try
            {
                // This currently is designed to throw errors so we can
                // see what errors are common in production
                host = new ApiHost();
                host.StartHost();

                encryption = new Encryption("the machine key");

                Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                baseURL = configuration.AppSettings.Settings["BaseURL"].Value;
                computerKey = configuration.AppSettings.Settings["ComputerKey"].Value;
            }
            catch (Exception e)
            {
                if (e.InnerException.Message.Contains(":8099"))
                {
                    MessageBox.Show("Port 8099 is already in use. Perhapse the launcher is already running, or running as another user?");
                }
                else
                {
                    MessageBox.Show(e.ToString());
                }
                throw(e); // this gets eaten by the startup of the app but it does STOP the app

            }
            txtUrl.Text = baseURL;
            txtComputerKey.Text = computerKey;

            this.WindowState = FormWindowState.Minimized;
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.Visible = true;
            }
        }

        private void game_start_timer_Tick(object sender, EventArgs e)
        {
            currStartDelay++;
            

            if(LauncherInfo.gameIsNew)
            {
                if (LauncherInfo.game.status == "ok")
                {
                    notifyIcon.BalloonTipTitle = "Game Started";
                    notifyIcon.BalloonTipText = LauncherInfo.game.name;
                    notifyIcon.ShowBalloonTip(1000);
                }
                LauncherInfo.gameIsNew = false;
                currStartDelay = 0;
                if (LauncherInfo.isInstall)
                {
                    currStartDelay = -3600;
                    LauncherInfo.isInstall = false;
                }
                currEndDelay = 0;
                // set some kind of timeout
            }

            if (LauncherInfo.game != null)
            {
                if (!gc.isGameRunning(LauncherInfo.game.exe))
                {
                    if (currStartDelay > gameStartDelay)
                    {
                        currEndDelay++;
                    }
                }
                else
                {
                    currStartDelay = gameStartDelay;
                }

                if (currEndDelay > gameEndDelay)
                {
                    gc.CheckinUser($"{baseURL}/game/checkin", computerKey);
                    gc.StopSteam();
                    LauncherInfo.StopGame();
                    currEndDelay = 0;
                }
            }
        }

        public void SetConfigValue(string key, string value)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if(key == "Secret")
            {
                string encryptedValue = encryption.Encrypt(value);
                value = encryptedValue;
            }

            configuration.AppSettings.Settings[key].Value = value;
            //configuration.AppSettings.SectionInformation.ProtectSection(null);
            configuration.Save();

            ConfigurationManager.RefreshSection("appSettings");
            baseURL = configuration.AppSettings.Settings["BaseURL"].Value;
            computerKey = configuration.AppSettings.Settings["ComputerKey"].Value;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string set_computerKey = txtComputerKey.Text;
            string set_baseUrl = txtUrl.Text;
            SetConfigValue("ComputerKey", set_computerKey);
            SetConfigValue("BaseURL", set_baseUrl);
            // go get that secret key
            GameCommand gc = new GameCommand();

            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "computer_key", computerKey },
                { "current_time", DateTime.Now.ToString()},
                { "user", txtUser.Text },
                { "pass", txtPass.Text }
            };

            dynamic obj = gc.GetWebResponse($"{baseURL}/computers/getSecret", data);
            if (obj.status != null && obj.status == "ok")
            {
                string secret = obj.message;
                SetConfigValue("Secret", secret);
                MessageBox.Show("Settings Saved", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtUser.Text = "";
                txtPass.Text = "";
            }
            else
            {
                MessageBox.Show("Error getting settings","Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // ResetConfigMechanism();
        }

        private void ResetConfigMechanism()
        {
            typeof(ConfigurationManager)
                .GetField("s_initState", BindingFlags.NonPublic |
                                         BindingFlags.Static)
                .SetValue(null, 0);

            typeof(ConfigurationManager)
                .GetField("s_configSystem", BindingFlags.NonPublic |
                                            BindingFlags.Static)
                .SetValue(null, null);

            typeof(ConfigurationManager)
                .Assembly.GetTypes()
                .Where(x => x.FullName ==
                            "System.Configuration.ClientConfigPaths")
                .First()
                .GetField("s_current", BindingFlags.NonPublic |
                                       BindingFlags.Static)
                .SetValue(null, null);
        }
    }
}
