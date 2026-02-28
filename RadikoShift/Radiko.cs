using RadikoShift.EF;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RadikoShift.Radio
{
    public class Radiko
    {

        private static CookieContainer? _cookieContainer;

        public static async Task<bool> Login(string email, string pass)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass)) return false;

            using (var handler = new HttpClientHandler() { UseCookies = true })
            {
                using var client = new HttpClient(handler);

                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        {"mail", email},
                        {"pass", pass}
                    });

                var res = await client.PostAsync(Define.Radiko.Login, content);
                var html = await res.Content.ReadAsStringAsync();
                _cookieContainer = handler.CookieContainer;

                res = await client.GetAsync(Define.Radiko.LoginCheck);
                var json = await res.Content.ReadAsStringAsync();
            }

            return true;
        }

        /// <summary>
        /// 放送局取得
        /// </summary>
        /// <returns></returns>
        public static async Task<List<Station>> GetStations(bool login)
        {
            var xmlUrl = Define.Radiko.StationListFull;

            if (!login)
            {
                using var client = new HttpClient();

                var text = await client.GetStringAsync(Define.Radiko.AreaCheck)
                                       .ConfigureAwait(false);

                var m = Regex.Match(text, @"JP[0-9]+");
                if (m.Success)
                {
                    xmlUrl = Define.Radiko.StationListPref.Replace("[AREA]", m.Value);
                }
            }

            var res = new List<Station>();

            // 非同期でXML読み込み
            using var stream = await new HttpClient()
                .GetStreamAsync(xmlUrl)
                .ConfigureAwait(false);

            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None)
                                     .ConfigureAwait(false);

            int stationOrder = 1;

            foreach (var stations in doc.Descendants("stations"))
            {
                var regionId = stations.Attribute("region_id")?.Value ?? "";
                var regionName = stations.Attribute("region_name")?.Value ?? "";

                foreach (var station in stations.Descendants("station"))
                {
                    var code = station.Descendants("id").First().Value;
                    var name = station.Descendants("name").First().Value;
                    var areaId = station.Descendants("area_id").FirstOrDefault()?.Value ?? "";

                    res.Add(new Station
                    {
                        Id = code,
                        RegionId = regionId,
                        RegionName = regionName,
                        Name = name,
                        AreaCode = areaId,
                        DisplayOrder = stationOrder++
                    });
                }
            }

            var sorted = res
                .GroupBy(s => s.Area)
                .OrderBy(g => g.Min(x => x.DisplayOrder))
                .SelectMany(g => g.OrderBy(x => x.DisplayOrder))
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].DisplayOrder = i + 1;
            }

            return sorted;
        }

        /// <summary>
        /// 番組表取得
        /// </summary>
        /// <param name="station"></param>
        /// <returns></returns>
        public static Task<List<EF.Program>> GetPrograms(Station station)
        {
            return Task.Factory.StartNew(() =>
            {
                var doc = XDocument.Load(Define.Radiko.WeeklyTimeTable.Replace("[stationCode]", station.Id));

                return doc.Descendants("prog")
                    .Select(prog => new EF.Program()
                    {
                        Id = station.Id + prog.Attribute("ft")?.Value + prog.Attribute("to")?.Value,
                        StartTime = DateTime.ParseExact(prog.Attribute("ft")?.Value!,"yyyyMMddHHmmss",CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal),
                        EndTime = DateTime.ParseExact(prog.Attribute("to")?.Value!, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal),
                        Title = prog.Element("title")?.Value.Trim(),
                        CastName = prog.Element("pfm")?.Value.Trim(),
                        Description = prog.Element("info")?.Value.Trim(),
                        StationId = station.Id,
                        ImageUrl = prog.Element("img")?.Value.Trim(),
                    })
                    .ToList();

            });
        }
    }
}
