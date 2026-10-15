namespace Shared.Common.Exceptions;

/// <summary>Gắn/đọc ngữ cảnh vào Exception.Data — chỉ log 1 lần ở middleware/[LogApi] mà vẫn biết lỗi xảy ra ở store nào.</summary>
public static class ExceptionDataExtensions
{
    private const string StoreNameKey = "StoreName";

    public static void SetStoreName(this Exception exception, string storeName) => exception.Data[StoreNameKey] = storeName;

    /// <summary>Tìm cả trong InnerException (exception gốc có thể bị bọc lại ở tầng trên); không có thì trả chuỗi rỗng.</summary>
    public static string GetStoreName(this Exception exception)
    {
        for (Exception? objCurrent = exception; objCurrent != null; objCurrent = objCurrent.InnerException)
        {
            if (objCurrent.Data[StoreNameKey] is string strStoreName) return strStoreName;
        }
        return string.Empty;
    }
}
