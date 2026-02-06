namespace RadikoShift
{
    public class Define
    {
        public class Radiko
        {
            /// <summary>
            /// 週刊番組表
            /// </summary>
            public const string WeeklyTimeTable = "http://radiko.jp/v3/program/station/weekly/[stationCode].xml";

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
            /// 種別
            /// </summary>
            public const string TypeName = "radiko";

            /// <summary>
            /// 放送局一覧(すべて)
            /// </summary>
            public const string StationListFull = "http://radiko.jp/v3/station/region/full.xml";
            
            /// <summary>
            /// 放送局一覧(都道府県ごと)
            /// </summary>
            public const string StationListPref = "http://radiko.jp/v3/station/list/[AREA].xml";
        }
    }
}
