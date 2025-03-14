using Microsoft.Maui.Dispatching;
using System;
using System.Threading.Tasks;

namespace MauiMonitor
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            StartMonitoring();
        }

        private void StartMonitoring()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    UpdateSystemInfo();
                    await Task.Delay(5000); // Actualiza cada 5 segundos
                }
            });
        }

        private void UpdateSystemInfo()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
            #if ANDROID
                    CpuLabel.Text = $"CPU: {SystemMonitor.GetCpuUsage():F2} %";
                    RamLabel.Text = $"RAM Libre: {SystemMonitor.GetAvailableMemory()} MB";
                    TempLabel.Text = $"Temp. CPU: {SystemMonitor.GetCpuTemperature():F1} °C";
                    StorageLabel.Text = $"Almacenamiento: {SystemMonitor.GetFreeStorage()} GB";
            #endif
            });
        }


        private void OnRefreshClicked(object sender, EventArgs e)
        {
            UpdateSystemInfo();
        }

        private void OnEnableNotificationClicked(object sender, EventArgs e)
        {
#if ANDROID
            SystemMonitor.StartNotificationService();
#endif
        }
    }
}
