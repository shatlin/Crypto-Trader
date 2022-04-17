using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BinanceExchange.API;
using BinanceExchange.API.Client;
using BinanceExchange.API.Client.Interfaces;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Market;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using BinanceExchange.API.Models.Response.Error;
using BinanceExchange.API.Models.WebSocket;
using BinanceExchange.API.Utility;
using BinanceExchange.API.Websockets;
using log4net;
using Newtonsoft.Json;
using Trader.Models;
using WebSocketSharp;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Windows.Threading;
using System.Windows.Navigation;
using System.Windows.Shell;

namespace Trader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        public DispatcherTimer TraderTimer;
        double progress = 0;
        public MainWindow()
        {
            InitializeComponent();
            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            startup();
        }

        public void startup()
        {
            TraderTimer = new DispatcherTimer();
            TraderTimer.Tick += new EventHandler(TraderTimer_Tick);
            TraderTimer.Interval = new TimeSpan(0, 0, 2);

            var second = DateTime.Now.Second;

            while (second % 20 != 0)
            {
                Thread.Sleep(100);
                second = DateTime.Now.Second;
            }

            TraderTimer.Start();
        }

        private void TraderTimer_Tick(object sender, EventArgs e)
        {

            progress = progress <= 1 ? progress + 0.05 : 0;
            if (progress < 0.4)
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            if (progress >= 0.4 && progress < 0.8)
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
            else if (progress >= 0.8)
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
            TaskbarItemInfo.ProgressValue = progress;
        }

    }

}
