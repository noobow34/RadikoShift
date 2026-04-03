using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using RadikoShift.Data;

namespace RadikoShift.Radiko
{
    public class RadikoClient
    {
        /// <summary>
        /// ログインし、認証済み HttpClient を返す。
        /// メール・パスが空の場合は未ログイン状態の HttpClient を返す。
        /// </summary>
        public static async Task<HttpClient> CreateHttpClient(
            string email, string pass,
            DecompressionMethods decompression = DecompressionMethods.GZip | DecompressionMethods.Deflate)
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                UseCookies             = true,
                CookieContainer        = cookieContainer,
                AutomaticDecompression = decompression
            };
            var client = new HttpClient(handler);

            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(pass))
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "mail", email },
                    { "pass",  pass  }
                });
                var res = await client.PostAsync(RadikoUrls.Login, content);
                await res.Content.ReadAsStringAsync();

                res = await client.GetAsync(RadikoUrls.LoginCheck);
                await res.Content.ReadAsStringAsync();
            }

            return client;
        }

        /// <summary>放送局一覧を取得する</summary>
        public static async Task<List<Station>> GetStations(bool login, HttpClient httpClient)
        {
            var xmlUrl = RadikoUrls.StationListFull;

            if (!login)
            {
                var text = await httpClient.GetStringAsync(RadikoUrls.AreaCheck).ConfigureAwait(false);
                var m = Regex.Match(text, @"JP[0-9]+");
                if (m.Success)
                    xmlUrl = RadikoUrls.StationListPref.Replace("[AREA]", m.Value);
            }

            var res = new List<Station>();
            var stream = await httpClient.GetStreamAsync(xmlUrl).ConfigureAwait(false);
            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None).ConfigureAwait(false);

            int stationOrder = 1;
            foreach (var stations in doc.Descendants("stations"))
            {
                var regionId   = stations.Attribute("region_id")?.Value   ?? "";
                var regionName = stations.Attribute("region_name")?.Value ?? "";

                foreach (var station in stations.Descendants("station"))
                {
                    var code   = station.Descendants("id").FirstOrDefault()?.Value      ?? "";
                    var name   = station.Descendants("name").FirstOrDefault()?.Value    ?? "";
                    var areaId = station.Descendants("area_id").FirstOrDefault()?.Value ?? "";

                    if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
                        continue;

                    res.Add(new Station
                    {
                        Id           = code,
                        RegionId     = regionId,
                        RegionName   = regionName,
                        Name         = name,
                        AreaCode     = areaId,
                        DisplayOrder = stationOrder++
                    });
                }
            }

            var sorted = res
                .GroupBy(s => s.AreaCode)
                .OrderBy(g => g.Min(x => x.DisplayOrder))
                .SelectMany(g => g.OrderBy(x => x.DisplayOrder))
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
                sorted[i].DisplayOrder = i + 1;

            return sorted;
        }

        /// <summary>番組表を取得する</summary>
        public static async Task<List<Data.Program>> GetPrograms(Station station, HttpClient httpClient)
        {
            var stream = await httpClient.GetStreamAsync(
                RadikoUrls.WeeklyTimeTable.Replace("[stationCode]", station.Id));

            var doc = XDocument.Load(stream);

            return doc.Descendants("prog")
                .Select(prog => new Data.Program
                {
                    Id          = station.Id + prog.Attribute("ft")?.Value + prog.Attribute("to")?.Value,
                    StartTime   = DateTime.ParseExact(prog.Attribute("ft")?.Value!, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal),
                    EndTime     = DateTime.ParseExact(prog.Attribute("to")?.Value!, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal),
                    Title       = prog.Element("title")?.Value.Trim(),
                    CastName    = prog.Element("pfm")?.Value.Trim(),
                    Description = prog.Element("info")?.Value.Trim(),
                    StationId   = station.Id,
                    ImageUrl    = prog.Element("img")?.Value.Trim(),
                })
                .ToList();
        }
    }
}
