using Android.App;
using Android.Content;
using Android.OS;
using Java.IO;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MauiMonitor
{
    public static class SystemMonitor
    {
        public static float GetCpuUsage()
        {
            try
            {
                var reader = new RandomAccessFile("/proc/stat", "r");
                string[] load = reader.ReadLine().Split(' ');

                long idle1 = long.Parse(load[4]);
                long cpu1 = long.Parse(load[2]) + long.Parse(load[3]) + long.Parse(load[4]) + long.Parse(load[5]) +
                            long.Parse(load[6]) + long.Parse(load[7]) + long.Parse(load[8]);

                reader.Close();
                System.Threading.Thread.Sleep(360);

                reader = new RandomAccessFile("/proc/stat", "r");
                load = reader.ReadLine().Split(' ');

                long idle2 = long.Parse(load[4]);
                long cpu2 = long.Parse(load[2]) + long.Parse(load[3]) + long.Parse(load[4]) + long.Parse(load[5]) +
                            long.Parse(load[6]) + long.Parse(load[7]) + long.Parse(load[8]);

                reader.Close();
                return (cpu2 - cpu1) * 100f / (cpu2 + idle2 - cpu1 - idle1);
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        public static long GetAvailableMemory()
        {
            ActivityManager activityManager = (ActivityManager)Android.App.Application.Context.GetSystemService(Context.ActivityService);
            ActivityManager.MemoryInfo memoryInfo = new ActivityManager.MemoryInfo();
            activityManager.GetMemoryInfo(memoryInfo);
            return memoryInfo.AvailMem / (1024 * 1024);
        }

        public static float GetCpuTemperature()
        {
            try
            {
                string[] paths = new string[]
                {
            "/sys/class/thermal/thermal_zone0/temp",
            "/sys/class/thermal/thermal_zone1/temp",
            "/sys/class/thermal/thermal_zone2/temp",
            "/sys/devices/virtual/thermal/thermal_zone0/temp"
                };

                foreach (var path in paths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        string temp = System.IO.File.ReadAllText(path);
                        return float.Parse(temp) / 1000;
                    }
                }

                System.Console.WriteLine("Ninguna de las rutas de temperatura fue encontrada.");
                return -1;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error al obtener la temperatura: {ex.Message}");
                return -1;
            }
        }


        public static long GetFreeStorage()
        {
            StatFs stat = new StatFs(Android.App.Application.Context.GetExternalFilesDir(null).AbsolutePath);
            long bytesAvailable = (long)stat.AvailableBlocksLong * stat.BlockSizeLong;
            return bytesAvailable / (1024 * 1024 * 1024);
        }

        public static void StartNotificationService()
        {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(NotificationService));
            context.StartService(intent);
        }
    }

    [Service]
    public class NotificationService : Service
    {
        const int NOTIFICATION_ID = 1001;

        public override IBinder OnBind(Intent intent) => null;

        public override void OnCreate()
        {
            base.OnCreate();
            StartForeground(NOTIFICATION_ID, BuildNotification());
            Task.Run(async () =>
            {
                while (true)
                {
                    var notification = BuildNotification();
                    var manager = (NotificationManager)GetSystemService(NotificationService);
                    manager.Notify(NOTIFICATION_ID, notification);
                    await Task.Delay(5000); // Actualización cada 5 segundos
                }
            });
        }

        private Notification BuildNotification()
        {
            var context = Android.App.Application.Context;
            var channelId = "monitor_channel";
            var manager = (NotificationManager)context.GetSystemService(NotificationService);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(channelId, "Monitor", NotificationImportance.Low);
                manager.CreateNotificationChannel(channel);
            }

            // Obtener la temperatura del CPU
            float cpuTemperature = SystemMonitor.GetCpuTemperature();

            // Construir la notificación, ahora sin la batería, pero con la temperatura del CPU
            var notification = new Notification.Builder(context, channelId)
                .SetContentTitle("Uso del Sistema")
                .SetContentText($"CPU: {SystemMonitor.GetCpuUsage():F2}%, RAM: {SystemMonitor.GetAvailableMemory()}MB, Temp. CPU: {cpuTemperature}°C")
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .Build();

            return notification;
        }
    }
}
