namespace RadikoShift.Radiko
{
    /// <summary>
    /// Radiko API の URL 定数
    /// </summary>
    public static class RadikoUrls
    {
        /// <summary>週刊番組表</summary>
        public const string WeeklyTimeTable = "https://radiko.jp/v3/program/station/weekly/[stationCode].xml";

        /// <summary>地域判定用</summary>
        public const string AreaCheck = "http://radiko.jp/area/";

        /// <summary>ログイン</summary>
        public const string Login = "https://radiko.jp/ap/member/webapi/member/login";

        /// <summary>ログインチェック</summary>
        public const string LoginCheck = "https://radiko.jp/ap/member/webapi/member/login/check";

        /// <summary>放送局一覧（全国）</summary>
        public const string StationListFull = "https://radiko.jp/v3/station/region/full.xml";

        /// <summary>放送局一覧（都道府県ごと）</summary>
        public const string StationListPref = "https://radiko.jp/v3/station/list/[AREA].xml";
    }
}
