namespace RadikoShift
{
    public class Define
    {
        public class Config
        {
            public const string Common = "common";
            public const string EncodeSetting = "encode_setting";
        }

        public class Radiko
        {
            public const int EndSec = 30;
            
            /// <summary>
            /// Radiko
            /// </summary>
            public const string Home = "http://radiko.jp/";

            /// <summary>
            /// 週刊番組表
            /// </summary>
            public const string WeeklyTimeTable = "http://radiko.jp/v3/program/station/weekly/[stationCode].xml";

            /// <summary>
            /// 今日の番組表
            /// </summary>
            public const string DailyTimeTable = "http://radiko.jp/v2/api/program/today?area_id={area_id}";

            /// <summary>
            /// 今日の番組表(放送局指定)
            /// </summary>
            public const string TodayStationTimeTable =
                "http://radiko.jp/v2/api/program/station/today?station_id={station_id}";

            /// <summary>
            /// 地域判定用
            /// </summary>
            public const string AreaCheck = "http://radiko.jp/area/";

            /// <summary>
            /// ログインURL
            /// </summary>
            public const string Login = "https://radiko.jp/ap/member/webapi/member/login";

            /// <summary>
            /// ログインチェック
            /// </summary>
            public const string LoginCheck = "https://radiko.jp/ap/member/webapi/member/login/check";

            /// <summary>
            /// ログアウトURL
            /// </summary>
            public const string Logout = "http://radiko.jp/ap/member/webapi/member/logout";

            /// <summary>
            /// プレミアム登録URL
            /// </summary>
            public const string Regist = "https://radiko.jp/ap/member/regist/regist_mail_page?premium=1";

            /// <summary>
            /// 種別
            /// </summary>
            public const string TypeName = "radiko";

            /// <summary>
            /// 放送局画像
            /// </summary>
            public const string StationImage = "http://radiko.jp/station/logo/[CH]/logo_large.png";

            /// <summary>
            /// 放送局一覧(すべて)
            /// </summary>
            public const string StationListFull = "http://radiko.jp/v3/station/region/full.xml";

            
            /// <summary>
            /// 放送局一覧(都道府県ごと)
            /// </summary>
            public const string StationListPref = "http://radiko.jp/v3/station/list/[AREA].xml";

            public const string Thumbnail = "http://radiko.jp/station/logo/[CH]/logo_small.png";

            /// <summary>
            /// 認証URL1
            /// </summary>
            public const string Auth1 = "https://radiko.jp/v2/api/auth1";

            /// <summary>
            /// 認証URL2
            /// </summary>
            public const string Auth2 = "https://radiko.jp/v2/api/auth2";

            /// <summary>
            /// rtmpdumpの引数
            /// </summary>
            public const string RtmpdumpArgs =
                "-r \"rtmpe://f-radiko.smartstream.ne.jp/[CH]/_definst_\" -a \"[CH]/_definst_\" -W \"[SWF]\" -C S: -C S: -C S: -C S:[TOKEN] -y \"simul-stream.stream?ucid=null\"";

            /// <summary>
            /// common.js
            /// </summary>
            public const string CommonJs = "http://radiko.jp/apps/js/playerCommon.js";


            public const string TimeFreeM3U8 = "https://radiko.jp/v2/api/ts/playlist.m3u8?station_id=[CH]&ft=[FT]&to=[TO]";

            /// <summary>
            /// rtmpgwの引数
            /// </summary>
            public const string RtmpGwArgs =
                "-r \"rtmpe://f-radiko.smartstream.ne.jp/[CH]/_definst_\" -a \"[CH]/_definst_\" -W \"http://radiko.jp/apps/js/flash/myplayer-release.swf?station_id=[CH]\" --conn S: --conn S: --conn S: --conn S:[TOKEN] -y \"simul-stream.stream?ucid=null\" --live --device [IP] --sport [PORT]";

            public const string RawFFmpegArgs = "";

            /// <summary>
            /// stream
            /// </summary>
            public const string StationStream = "http://radiko.jp/v2/station/stream_smh_multi/[CH].xml";

            /// <summary>
            /// playlist.m3u8
            /// </summary>
            public const string PlayList =
                "http://f-radiko.smartstream.ne.jp/[CH]/_definst_/simul-stream.stream/playlist.m3u8";

            public const string RealTimeFfmpegArgs = "-headers \"X-Radiko-AuthToken: [TOKEN]\" -i \"http://f-radiko.smartstream.ne.jp/[CH]/_definst_/simul-stream.stream/playlist.m3u8\" -t [TIME] -metadata title=\"[TAG_TITLE]\" -metadata artist=\"[TAG_ARTIST]\" -metadata album=\"[TAG_ALBUM]\" -metadata genre=\"[TAG_GENRE]\" -metadata comment=\"[TAG_COMMENT]\" -bsf:a aac_adtstoasc -ar [SAMPLINGRATE] -ab [BITRATE] -vol [VOLUME]  \"[FILE]\"";

            public static readonly string TimeFreeFfmpegArgs = $"-i \"[M3U8]\" -metadata title=\"[TAG_TITLE]\" -metadata artist=\"[TAG_ARTIST]\" -metadata album=\"[TAG_ALBUM]\" -metadata genre=\"[TAG_GENRE]\" -metadata comment=\"[TAG_COMMENT]\" -bsf:a aac_adtstoasc -ar [SAMPLINGRATE] -ab [BITRATE] -vol [VOLUME]  \"[FILE]\"";

            
            //                var args = $"-headers 'X-Radiko-AuthToken: {t.Result}' -i '{url}' -t 00:00:30 -acodec copy test.aac";


        }
    }
}
