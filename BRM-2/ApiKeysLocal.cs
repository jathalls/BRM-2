namespace BRM_2;

/// <summary>
/// Local version of APIKeys which contains my personal API keys
/// </summary>
public static partial class APIKeys
{
    static APIKeys()
    {
       

        DarkSkyApiKey = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String("MGE5MjAzYzQ1YzAxNDYyMjUzMGU0MWQ5MGM0NzIxZjU="));


        VisualCrossingApiKey = "KMNSDWKHLFJNQKD7PP4RPN8LQ";

        OpenWeatherApiKey = "fe90d2222ed82508925fc7ac55efde17";

        What3WordsApiKey = "7RPQ7TW6";

        SyncfusionKey = "Ngo9BigBOggjHTQxAR8/V1JHaF5cWWdCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdlWXxceXRcRmJeV0J0XkFWYEo=";
    }
}
