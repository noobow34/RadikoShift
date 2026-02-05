using RadikoShift.EF;
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
        public static Task<List<Station>> GetStations(bool login)
        {
            return Task.Factory.StartNew(() =>
            {
                var xmlUrl = Define.Radiko.StationListFull;
                if (!login)
                {
                    // 地域判定をする
                    using var client = new HttpClient();
                    var text = client.GetStringAsync(Define.Radiko.AreaCheck).Result;
                    var m = Regex.Match(text, @"JP[0-9]+");
                    if (m.Success)
                    {
                        xmlUrl = Define.Radiko.StationListPref.Replace("[AREA]", m.Value);
                    }
                }
                
                var res = new List<Station>();
                var doc = XDocument.Load(xmlUrl);

                // 放送局一覧
                var sequence = 1;
                foreach (var stations in doc.Descendants("stations"))
                {
                    var regionId = stations.Attribute("region_id")?.Value ?? "";
                    var regionName = stations.Attribute("region_name")?.Value ?? "";
                    foreach (var station in stations.Descendants("station"))
                    {
                        var code = station.Descendants("id").First().Value;
                        var name = station.Descendants("name").First().Value;
                        var logo = station.Descendants("logo").FirstOrDefault()?.Value ?? "";
                        var areaId = station.Descendants("area_id").FirstOrDefault()?.Value ?? "";
                        var url = station.Descendants("href").First().Value;
                        res.Add(new Station
                        {
                            Id = $"{Define.Radiko.TypeName}_{code}",
                            RegionId = regionId,
                            RegionName = regionName,
                            Code = code,
                            Name = name,
                            Area = areaId,
                            DisplayOrder = sequence++
                        });
                    }
                }

                return res;
            });
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
                var doc = XDocument.Load(Define.Radiko.WeeklyTimeTable.Replace("[stationCode]", station.Code));

                return doc.Descendants("prog")
                    .Select(prog => new EF.Program()
                    {
                        Id = station.Code + prog.Attribute("ft")?.Value + prog.Attribute("to")?.Value,
                        StartTime = Utility.Text.StringToDate(prog.Attribute("ft")?.Value!),
                        EndTime = Utility.Text.StringToDate(prog.Attribute("to")?.Value!),
                        Title = prog.Element("title")?.Value.Trim(),
                        CastName = prog.Element("pfm")?.Value.Trim(),
                        Description = prog.Element("info")?.Value.Trim(),
                        StationId = station.Id,
                    })
                    .ToList();

            });
        }
    }
}
