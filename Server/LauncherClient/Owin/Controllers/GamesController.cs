﻿using LauncherClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Configuration;
using System.Web.Http.Results;
using LauncherServerClasses;
using System.Windows.Forms;

namespace LauncherClient.Owin.Controllers
{
    public class StatusMessage
    {
        public string message { get; set; } 
        public string status { get; set; }
    }

    public class GamesController : ApiController
    {
        private GameCommand gc = new GameCommand();
        private Encryption encryption;

        [HttpGet]
        public StatusMessage StartGame(int id, bool install = false)
        {
            StatusMessage returnMessage = new StatusMessage();

            string baseURL = ConfigurationManager.AppSettings["BaseURL"];
            string computerKey = ConfigurationManager.AppSettings["ComputerKey"];
            string secretKey = ConfigurationManager.AppSettings["Secret"];

            // We must decrypt the secret key using the machine key
            //machineKeyEncryption = new Encryption("the machine key");
            try
            {
                secretKey = MachineKeyEncryption.UnProtect(secretKey, $"Secret for computer {computerKey}");
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error: Unable to load settings. Contact an administrator.\n\n{e.Message}");
                throw;
            }

            // Then, we can Encrypt/Decrypt using that decrypted secret key
            encryption = new Encryption(secretKey); 

            string URL = $"{baseURL}/game/checkout/{id}";
            SteamGame game = gc.GetSteamLogin(id, URL, computerKey, "");
            returnMessage.message = game.message;
            returnMessage.status = game.status;

            if (game.status == "ok")
            {
                game.password = encryption.Decrypt(game.password);
                LauncherInfo.isInstall = install;
                LauncherInfo.StartGame(game);

                gc.StartSteam(LauncherInfo.game);
            }
            return returnMessage;
        }

        [HttpGet]
        public StatusMessage InstallGame(int id)
        {
            return StartGame(id, true);
        }

       [HttpGet]
        public string Test()
        {
            return "ok";
        }

        [HttpGet]
        public string Checkin()
        {
            string baseURL = ConfigurationManager.AppSettings["BaseURL"];
            string computerKey = ConfigurationManager.AppSettings["ComputerKey"];

            string URL = $"{baseURL}/game/checkin";
            gc.CheckinUser(URL, computerKey);
            LauncherInfo.StopGame();
            return "ok";
        }

        [HttpGet]

        public ComputerStatus Status()
        {
            var status = new ComputerStatus();
            if (LauncherInfo.game == null)
            {
                status.status = "ready";
                status.message = "Hi there";
            }
            else
            {
                status.status = "running";
                status.message = "Currently running a game";
                status.game = new SteamGame();
                status.game.name = LauncherInfo.game.name;
                status.game.id = LauncherInfo.game.id;
            }
            
            return status;
        }
    }
}
