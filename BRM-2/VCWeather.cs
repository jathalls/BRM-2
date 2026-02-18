namespace BRM_2;
    internal class VCWeather
    {
        public VCWeather() { }

        //public static WeatherClient client=new WeatherClient(APIKeys.OpenWeatherApiKey);

        /*
         * {
    "lat": 51.234,
    "lon": -0.234,
    "timezone": "Europe/London",
    "timezone_offset": 3600,
    "data": [
        {
            "dt": 1695600000,
            "sunrise": 1695621066,
            "sunset": 1695664466,
            "temp": 290.43,
            "feels_like": 290.15,
            "pressure": 1015,
            "humidity": 74,
            "dew_point": 285.76,
            "clouds": 0,
            "visibility": 10000,
            "wind_speed": 5.14,
            "wind_deg": 190,
            "weather": [
                {
                    "id": 800,
                    "main": "Clear",
                    "description": "clear sky",
                    "icon": "01n"
                }
            ]
        }
    ]
}*/
        /// <summary>
        /// https://api.openweathermap.org/data/3.0/onecall/timemachine?lat={lat}&lon={lon}&dt={time}&appid={API key}
        /// </summary>
        /// <param name="locationGPSLatitude"></param>
        /// <param name="locationGPSLongitude"></param>
        /// <param name="weatherDateTime"></param>
        /// <returns></returns>
        public static async Task<string?> GetWeatherHistoryAsync(double locationGPSLatitude, double locationGPSLongitude, DateTime weatherDateTime)
        {
            string? result = "";

            weatherDateTime = weatherDateTime.ToUniversalTime();
            long dt = ((DateTimeOffset)weatherDateTime).ToUnixTimeSeconds();

            ////Debug.WriteLine($"GWH:- {VCWeather.client.ApiKey}");
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://api.openweathermap.org/");
                    var callResult = await client.GetAsync($"data/3.0/onecall/timemachine?lat={locationGPSLatitude}&lon={locationGPSLongitude}" +
                        $"&dt={dt}&appid={APIKeys.OpenWeatherApiKey}&units=metric").ConfigureAwait(false);
                    //Debug.WriteLine(callResult.ToString());
                    if(callResult.IsSuccessStatusCode)
                    {
                        result=await callResult.Content.ReadAsStringAsync().ConfigureAwait(false);
                       // result=JsonConvert.DeserializeObject<string> (response??"");

                        /*
                         * {"lat":51.785,
                         * "lon":-0.2213,
                         * "timezone":"Europe/London",
                         * "timezone_offset":3600,
                         * "data":[{"dt":1565210490,
                         * "sunrise":1565152334,
                         * "sunset":1565206871,
                         * "temp":290.07,
                         * "feels_like":289.67,
                         * "pressure":1008,
                         * "humidity":71,
                         * "dew_point":284.78,
                         * "clouds":0,
                         * "visibility":10000,
                         * "wind_speed":5.1,
                         * "wind_deg":240,
                         * "weather":[{"id":800,
                         * "main":"Clear",
                         * "description":"clear sky",
                         * "icon":"01n"}]}]}

                        */
                    }
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.Message);
                return ("");
            }



            return (result);
        }

        public static string? GetWeatherHistory(double locationGPSLatitude, double locationGPSLongitude, DateTime weatherDateTime)
        {
            string result = "Weather by OpenWeather:- ";
            var res = GetWeatherHistoryAsync(locationGPSLatitude, locationGPSLongitude, weatherDateTime);
            string response = res?.Result??"";
            var weatherItems = new WeatherItems();
            if (!string.IsNullOrWhiteSpace(response))
            {
                response=response.Replace("[", string.Empty);
                response = response.Replace("]", string.Empty);
                response = response.Replace("{", string.Empty);
                response = response.Replace("}", string.Empty);
                response = response.Replace("\"", string.Empty);
                response = response.Replace("data:", string.Empty);
                var items = response.Split(",");
                foreach (var item in items) 
                {
                    if (item.Contains(':'))
                    {
                        var parts=item.Split(':');
                        if (parts.Length >= 2)
                        {
                            switch(parts[0])
                            {
                                case "lat":
                                    if (double.TryParse(parts[1],out double lat)) weatherItems.latitude = lat; break;
                                case "lon":
                                    if (double.TryParse(parts[1],out double longit))weatherItems.longitude = longit; break;
                                case "dt":
                                    if (long.TryParse(parts[1],out long unixdt))
                                    {
                                        
                                        var offset = DateTimeOffset.FromUnixTimeSeconds(unixdt);
                                        weatherItems.dateTime = offset.LocalDateTime;
                                    }
                                    break;
                                case "sunset":
                                    if (long.TryParse(parts[1], out long unixss))
                                    {

                                        var offset = DateTimeOffset.FromUnixTimeSeconds(unixss);
                                        weatherItems.sunset = offset.LocalDateTime;
                                    }
                                    break;
                                case "sunrise":
                                    if (long.TryParse(parts[1], out long unixsr))
                                    {

                                        var offset = DateTimeOffset.FromUnixTimeSeconds(unixsr);
                                        weatherItems.sunrise = offset.LocalDateTime;
                                    }
                                    break;
                                case "temp":
                                    if (double.TryParse(parts[1],out double temp)) weatherItems.temp = temp; break;
                                case "clouds":
                                    if (double.TryParse(parts[1], out double cloud)) weatherItems.clouds = cloud; break;
                                case "humidity":
                                    if (double.TryParse(parts[1], out double humidity)) weatherItems.humidity = humidity; break;
                                case "wind_speed":
                                    if (double.TryParse(parts[1], out double windspeed)) weatherItems.windSpeed = windspeed; break;
                                case "wind_dir":
                                    if (double.TryParse(parts[1], out double winddir)) weatherItems.windDir = winddir; break;
                                case "description":
                                    weatherItems.description = parts[1]; break;
                                default:break;
                            }
                        }
                    }
                }

            }

            result = $"{result}t={weatherItems.temp}, wind={weatherItems.windSpeed}, cloud={weatherItems.clouds}, desc={weatherItems.description} at {weatherItems.dateTime.ToShortTimeString()}";
            return (result);
        }
    }

    public class WeatherItems
    {
        public double latitude=200;
        public double longitude=-200;
        public DateTime dateTime = DateTime.MinValue;
        public DateTime sunset=DateTime.MinValue;
        public DateTime sunrise=DateTime.MinValue;
        public double temp=0.0d;
        public double humidity=0.0d;
            public double clouds = 0.0d;
        public double windSpeed=0.0d;
        public double windDir=0.0d;
        public string description="";

        public WeatherItems() { }


    }
	