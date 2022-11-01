using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.Management;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;
using System.Timers;

namespace DNS_Change_Propagator
{
    public partial class Service1 : ServiceBase
    {
        private static readonly string serviceName = "DNSPropagatorService";
        private static readonly HttpClient client = new HttpClient();
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        Config config;
        string lastIp = "0.0.0.0";

        Timer timer = new Timer();

        public Service1()
        {
            config = LoadConfig();

            if (config == null)
            {
                throw new Exception("Couldn't initialize config!");
            }

            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = config.updateInterval;
            timer.Enabled = true;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var currentIp = GetExternalIP();

            if (!lastIp.StartsWith(currentIp))
            {
                _log.Info($"Updating IP to {currentIp}... ");

                bool success = UpdateGoogleDNS(config.domain, config.user, config.password, currentIp);

                if (success)
                {
                    _log.Info("Success!");
                    lastIp = currentIp;
                }
                else
                {
                    _log.Info("Failure, trying again next round.");
                }

            }
        }

        protected override void OnStop()
        {
            base.OnStop();
        }

        string GetExternalIP()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "IP Change Propagator v1.0");

            var result = new Ipify() { ip = "0.0.0.0" };

            try
            {
                // alternative could be https://domains.google.com/checkip
                var streamTask = client.GetStringAsync("https://api.ipify.org?format=json");

                result = JsonConvert.DeserializeObject<Ipify>(streamTask.Result);
            }
            catch (Exception e)
            {
                _log.Error("Request went wrong!", e);
            }

            return result.ip;
        }

        bool UpdateGoogleDNS(string hostname, string user, string password, string ip)
        {
            var success = false;

            string url = $"https://domains.google.com/nic/update?hostname={hostname}&myip={ip}";
            string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                                           .GetBytes($"{user}:{password}"));

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "IP Change Propagator v1.0");
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {encoded}");

            try
            {
                var streamTask = client.GetStringAsync(url);

                string result = streamTask.Result.ToLower();

                if (config.debug)
                    _log.Debug(result);

                success = result.Contains("good") || result.Contains("nochg");
            }
            catch (Exception e)
            {
                _log.Error("Request weng wrong!", e);
            }

            return success;
        }

        static Config LoadConfig()
        {
            Config config = null;

            try
            {
                using (ManagementObject wmiService = new ManagementObject("Win32_Service.Name='" + serviceName + "'"))
                {
                    wmiService.Get();

                    string basePath = Path.GetDirectoryName(wmiService["PathName"].ToString().Trim('"'));

                    using (StreamReader r = new StreamReader(basePath + "\\config.json"))
                    {
                        string json = r.ReadToEnd();
                        config = JsonConvert.DeserializeObject<Config>(json);
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error(e);
            }

            return config;
        }
    }
}
