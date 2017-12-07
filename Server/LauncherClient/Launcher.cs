﻿using LauncherClasses;
using LauncherClient.Owin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LauncherClient
{
    public partial class Launcher : Form
    {
        private ApiHost host;
        private GameCommand gc = new GameCommand();
        private int gameStartDelay = 30; // number of grace seconds on starting a game
        private int gameEndDelay = 5; // number of grace seconds for allowing a restart
        private int currStartDelay = 0;
        private int currEndDelay = 0;

        private string baseURL;
        private string computerKey;

        public Launcher()
        {
            InitializeComponent();

            host = new ApiHost();
            host.StartHost();

            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            baseURL = configuration.AppSettings.Settings["BaseURL"].Value;
            computerKey = configuration.AppSettings.Settings["ComputerKey"].Value;

            txtUrl.Text = baseURL;
            txtComputerKey.Text = computerKey;

            Hide();
            notifyIcon.Visible = true;
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
                if (currStartDelay > gameStartDelay)
                {
                    if (!gc.isGameRunning(LauncherInfo.game.exe))
                    {
                        currEndDelay++;
                    }
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
            configuration.AppSettings.Settings[key].Value = value;
            configuration.AppSettings.SectionInformation.ProtectSection(null);
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
            }
        }
    }
}
