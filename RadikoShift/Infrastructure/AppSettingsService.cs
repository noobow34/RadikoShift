using RadikoShift.Data;

namespace RadikoShift.Infrastructure
{
    public class AppSettingsService
    {
        public const string KeyRefreshHour    = "RefreshHour";
        public const string KeyRefreshMinute  = "RefreshMinute";
        public const string KeyParallelCount  = "ParallelCount";

        public const int DefaultRefreshHour   = 6;
        public const int DefaultRefreshMinute = 0;
        public const int DefaultParallelCount = 10;

        private readonly ShiftContext _db;

        public AppSettingsService(ShiftContext db)
        {
            _db = db;
        }

        public int RefreshHour   => GetInt(KeyRefreshHour,   DefaultRefreshHour);
        public int RefreshMinute => GetInt(KeyRefreshMinute, DefaultRefreshMinute);
        public int ParallelCount => GetInt(KeyParallelCount, DefaultParallelCount);

        public async Task SetAsync(string key, string value)
        {
            var setting = await _db.AppSettings.FindAsync(key);
            if (setting == null)
                _db.AppSettings.Add(new AppSetting { Key = key, Value = value });
            else
                setting.Value = value;

            await _db.SaveChangesAsync();
        }

        private int GetInt(string key, int defaultValue)
        {
            var setting = _db.AppSettings.Find(key);
            if (setting != null && int.TryParse(setting.Value, out int v))
                return v;
            return defaultValue;
        }
    }
}
