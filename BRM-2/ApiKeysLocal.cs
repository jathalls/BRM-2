namespace BRM_2;

/// <summary>
/// Local version of APIKeys which contains my personal API keys
/// </summary>
public static partial class APIKeys
{
    static APIKeys()
    {
        // BingMapsLicenseKey = System.Text.Encoding.UTF8.GetString(
        //     System.Convert.FromBase64String(
        //         "AhhVL9x6bq6w0NbyqlwjmXDh3Qd64GbWowQQlFzrqx0ChD1MvaLkMTDQxuh2bhzh"));
        // redundant as BinngMaps has been discontinued
        BingMapsLicenseKey = "AhhVL9x6bq6w0NbyqlwjmXDh3Qd64GbWowQQlFzrqx0ChD1MvaLkMTDQxuh2bhzh";

        //full non-commercial key under jathalls@gmail.com
        //SyncfusionKey = "Mzk3Nzc0MEAzMzMwMmUzMDJlMzAzYjMzMzAzYlRQZUxuaGppMzNoOXVkTmM3emM5aktNd09qZjdPcDgzY2lpWDdVS2ZTYVU9;Mzk3Nzc0MUAzMzMwMmUzMDJlMzAzYjMzMzAzYkw4QlhsQUEydlJEcmtZbHRsRGhQNHdPSjA2c3RtSVBMbS93VFU4ZVJzOTA9;Mzk3Nzc0MkAzMzMwMmUzMDJlMzAzYjMzMzAzYmR5TU9wbTV1TGdBY0hvc1o0bkVIVHQ3SzJWT0U1RkJFY2o0T0tsMHpkVjA9;Mzk3Nzc0M0AzMzMwMmUzMDJlMzAzYjMzMzAzYmRyaUhPREdMWGIvTW1heDJQOXNtblRBaW9BMXQ1Tmh4MnBhcWEyQ01JNm89;Mzk3Nzc0NEAzMzMwMmUzMDJlMzAzYjMzMzAzYmtWOCtOMHB1M1ZXS1VWUDljTHhQNUdoSytENTFQODlVYzhYRVE0ZG03OVU9";
        SyncfusionKey = "Ngo9BigBOggjHTQxAR8/V1JGaF5cX2dCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdlWX5ccXRcRWZZWU1xXENWYEs=";

        DarkSkyApiKey = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String("MGE5MjAzYzQ1YzAxNDYyMjUzMGU0MWQ5MGM0NzIxZjU="));


        VisualCrossingApiKey = "KMNSDWKHLFJNQKD7PP4RPN8LQ";

        OpenWeatherApiKey = "fe90d2222ed82508925fc7ac55efde17";

        What3WordsApiKey = "7RPQ7TW6";
	}
}