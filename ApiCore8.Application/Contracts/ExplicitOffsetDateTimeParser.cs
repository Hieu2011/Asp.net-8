using System.Globalization;
using System.Text.RegularExpressions;

namespace ApiCore8.Application.Contracts
{
    /// <summary>
    /// Parse chuỗi thời gian client gửi lên cho các tham số kiểu "1 thời điểm cụ thể" (search theo
    /// ngày...), bắt buộc phải kèm offset múi giờ tường minh ("+07:00", "Z"...). Không dùng thẳng
    /// DateTimeOffset.TryParse/model binding mặc định vì khi thiếu offset, .NET âm thầm tự điền
    /// offset theo múi giờ hệ thống MÁY ĐANG CHẠY CODE (server) — dev máy set +7 thì ra đúng, deploy
    /// lên container Linux mặc định UTC thì ra sai lệch giờ mà không hề có exception nào báo.
    /// </summary>
    public static class ExplicitOffsetDateTimeParser
    {
        // Kết thúc chuỗi bằng "Z" hoặc "+HH:mm"/"-HH:mm" (có hoặc không dấu ":") mới coi là có offset.
        private static readonly Regex HasExplicitOffset = new(@"(Z|[+-]\d{2}:?\d{2})$", RegexOptions.Compiled);

        public static bool TryParse(string? value, out DateTimeOffset result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(value) || !HasExplicitOffset.IsMatch(value.Trim()))
            {
                return false;
            }

            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }
    }
}
