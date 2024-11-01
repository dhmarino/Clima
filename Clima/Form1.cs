using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Device.Location;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Clima
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            var versiones = typeof(Form1).Assembly.GetName().Version;
            lblVersion.Text = "Versión " + versiones.ToString();
        }

        string temp;
        string feels_like;
        string temp_min;
        string temp_max;
        string pressure;
        string humidity;
        string sea_level;
        string grnd_level;
        string description = "Sin Datos";
        string icon = "unknown";
        double sunriseUnix = 1709386096;
        double sunsetUnix = 1709322241;
        DateTime sunrise;
        DateTime sunset;
        string ubicacion;
        int wind;
        int wind_direction;
        int visibilidad;
        int actualizacion = 0;
        int nubosidad;
        int counter = 0;
        bool vez = false;
        private Timer _timer;
        string latitud = "-34.6166786"; //@-34.6217612,-58.4311703,14z?
        string longitud = "-58.4274232";

        private void Form1_Load(object sender, EventArgs e)
        {
            
            // Carga inicial de los datos
            LoadData();

            // Crea un temporizador que se ejecutará cada segundo
            _timer = new Timer();
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            // Recarga los datos
            LoadData();
        }
        private void LoadData()
        {

            if (!vez)
            {
                ObtenerValores();
                vez = true;
            }

            counter++;

            pictureBoxClima.Image = Image.FromFile("C:\\Desarrollo\\02_Proyectos_C_Sharp\\Clima\\iconsApiWeather\\" + icon + ".png");
            pictureBoxClima.Refresh();
            label1.Text = temp + " °C";
            label1.Refresh();
            label2.Text = description.ToUpper();
            label2.Refresh();
            label3.Text = "ST " + feels_like + " °C";
            label3.Refresh();
            label4.Text = "Presion " + pressure + " Hpa";
            label4.Refresh();
            label5.Text = "Humedad " + humidity + " %";
            label5.Refresh();
            sunrise = UnixTimeStampToDateTime(sunriseUnix);
            label6.Text = "Salida del Sol: " + sunrise.ToShortTimeString();
            label6.Refresh();
            sunset = UnixTimeStampToDateTime(sunsetUnix);
            label7.Text = "Puesta del Sol: " + sunset.ToShortTimeString();
            label7.Refresh();
            label8.Text = ubicacion + " - " + actualizacion;
            label8.Refresh();
            string vientodel = "ND";
            if ((wind_direction >= 0 && wind_direction <= 23) || (wind_direction >= 337 && wind_direction <= 360)) vientodel = "Norte";
            if (wind_direction >= 24 && wind_direction <= 68) vientodel = "Noreste";
            if (wind_direction >= 69 && wind_direction <= 113) vientodel = "Este";
            if (wind_direction >= 114 && wind_direction <= 158) vientodel = "Sudeste";
            if (wind_direction >= 159 && wind_direction <= 203) vientodel = "Sur";
            if (wind_direction >= 204 && wind_direction <= 248) vientodel = "Sudoeste";
            if (wind_direction >= 249 && wind_direction <= 293) vientodel = "Oeste";
            if (wind_direction >= 294 && wind_direction <= 336) vientodel = "Noroeste";
            label9.Text = "Viento: " + wind.ToString() + " Km/h del sector " + vientodel;
            label9.Refresh();
            label10.Text = "Visibilidad: " + visibilidad / 1000 + " Km";
            label10.Refresh();
            label11.Text = "Nubosidad " + nubosidad.ToString() + " %";
            label11.Refresh();
            label12.Text = counter.ToString();
            label12.Refresh();
            timerActualizar.Enabled = true;
        }

        public async void ObtenerValores()
        {
            string location = await GetLocationByIP();

            //Obtenemos la Key de openweather desde nuestro archivo de seteos
            var json = File.ReadAllText("appsettings.json");
            var config = JObject.Parse(json);
            string key = config["API_KEY"].ToString();
            
            //Verificamos si hay proxi
            Uri testUri = new Uri("http://www.google.com");
            bool proxyRequired = IsProxyRequired(testUri);

            //Armamos el restClient segun si hay o no proxi
            RestClient client;
            if (proxyRequired)
            {
                var options = new RestClientOptions()
                {
                    Proxy = GetWebProxy()
                };
                client = new RestClient(options);
            }
            else
            {
                client = new RestClient();
            }
            
            //Ejecutamos el request
            var request = new RestRequest("https://api.openweathermap.org/data/2.5/weather?lat=" + latitud + "&lon=" + longitud + "&appid=" + key + "&units=metric&lang=es", Method.Get);

            var response = client.Execute(request);

            //Deserializamos la respuesta para poder trabajar con los datos obtenidos
            dynamic jsonData = JsonConvert.DeserializeObject(response.Content.ToString());

            //Accedemos al campo "documents" que tiene el pdf y a el campo shipmentTrackingNumber que tiene el numero de guia
            dynamic main = jsonData.main;
            
            temp = main.temp.ToString("#.#"); 
            feels_like = main.feels_like.ToString("#.#");
            temp_min = main.temp_min;
            temp_max = main.temp_max;
            pressure = main.pressure;
            humidity = main.humidity;
            sea_level = main.sea_level;
            grnd_level = main.grnd_level;
            description = jsonData.weather[0].description;
            icon = jsonData.weather[0].icon;
            sunriseUnix = Convert.ToDouble(jsonData.sys.sunrise);
            sunsetUnix = Convert.ToDouble(jsonData.sys.sunset);
            ubicacion = jsonData.name;
            wind = jsonData.wind.speed;
            wind_direction = jsonData.wind.deg;
            visibilidad = jsonData.visibility;
            nubosidad = jsonData.clouds.all;
            actualizacion++;
        }

        private async Task<string> GetLocation()
        {
            string location = await GetLocationByIP();
            MessageBox.Show(location, "Ubicación Aproximada");
            return location;
        }
        private async Task<string> GetLocationByIP()
        {
            try
            {
                //Verificamos si hay proxi
                Uri testUri = new Uri("http://www.google.com");
                bool proxyRequired = IsProxyRequired(testUri);

                HttpClientHandler httpClientHandler = new HttpClientHandler();
                if (proxyRequired)
                {
                    // Si hay proxy y no es excluido, configuramos el HttpClientHandler con el proxy
                    httpClientHandler.Proxy = new WebProxy("172.16.1.26:8080")
                    {
                        Credentials = CredentialCache.DefaultNetworkCredentials
                    };
                    httpClientHandler.UseProxy = true;
                }
                else
                {
                    // No se usa proxy
                    httpClientHandler.UseProxy = false;
                }

                //// Configuración del HttpClientHandler con el proxy
                //var httpClientHandler = new HttpClientHandler
                //{
                //    Proxy = GetWebProxy(),
                //    UseProxy = true
                //};

                using (HttpClient client = new HttpClient(httpClientHandler))
                {
                    // Hacer la solicitud al servicio de geolocalización
                    HttpResponseMessage response = await client.GetAsync("https://ipinfo.io/json");
                    response.EnsureSuccessStatusCode();

                    // Procesar la respuesta JSON
                    string json = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(json);

                    // Extraer información de ubicación
                    string ip = data["ip"].ToString();
                    string city = data["city"].ToString();
                    string region = data["region"].ToString();
                    string country = data["country"].ToString();
                    string loc = data["loc"].ToString();
                    
                    string[] coordinates = loc.Split(',');
                    latitud = coordinates[0];
                    longitud = coordinates[1];
                    
                    return $"IP: {ip}\nCiudad: {city}\nRegión: {region}\nPaís: {country}\nCoordenadas: {loc}";
                }
            }
            catch (Exception ex)
            {
                return $"Error obteniendo la ubicación: {ex.Message}";
            }
        }

        private void timerActualizar_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ObtenerValores();
        }
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
        private IWebProxy GetWebProxy()
        {
            //var proxyUrl = "http://proxy-name.companydomain.com:9090/";
            // First create a proxy object
            var builder = new UriBuilder("172.16.1.26:8080");
            var uri = builder.Uri;

            var proxy = new WebProxy()
            {
                //Address = new Uri(proxyUrl),
                Address = uri,
                BypassProxyOnLocal = false,
                //UseDefaultCredentials = true, // This uses: Credentials = CredentialCache.DefaultCredentials
                //* These creds are given to the proxy server, not the web server *
                Credentials = CredentialCache.DefaultNetworkCredentials
                //Credentials = new NetworkCredential("diego.marino", "Noviembre2024+-+")
            };
            return proxy;
        }

        public static bool IsProxyRequired(Uri uri)
        {
            IWebProxy proxy = WebRequest.GetSystemWebProxy();
            Uri proxyUri = proxy.GetProxy(uri);

            // Si la URI del proxy es diferente a la URI de destino, significa que se está usando un proxy
            return !proxyUri.Equals(uri);
        }
    }
}
