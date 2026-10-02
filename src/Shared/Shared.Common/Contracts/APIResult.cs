using Newtonsoft.Json;

namespace Shared.Common.Contracts;

/// <summary>Response envelope chung — mọi action controller trả <c>Task&lt;APIResult&gt;</c>.</summary>
public class APIResult
{
    public bool IsError { get; set; }
    public int StatusID { get; set; }
    public string Message { get; set; } = string.Empty;
    public string MessageDetail { get; set; } = string.Empty;
    public object? ResultObject { get; set; }

    public APIResult()
    {
    }

    /// <summary>Kết quả thành công.</summary>
    public APIResult(object? objResultObject)
    {
        ResultObject = objResultObject;
    }

    public APIResult(ResultMessage objResultMessage)
    {
        IsError = objResultMessage.IsError;
        StatusID = objResultMessage.ErrorType;
        Message = objResultMessage.Message;
        MessageDetail = objResultMessage.MessageDetail;
    }

    public APIResult(int intStatusID, string strMessage, object? objResultObject)
    {
        StatusID = intStatusID;
        Message = strMessage;
        ResultObject = objResultObject;
    }

    public APIResult(bool bolIsError, int intStatusID, string strMessage, string strMessageDetail)
    {
        IsError = bolIsError;
        StatusID = intStatusID;
        Message = strMessage;
        MessageDetail = strMessageDetail;
    }

    public APIResult(bool bolIsError, ResultMessage.ErrorTypes enmErrorType, string strMessage, string strMessageDetail)
        : this(bolIsError, (int)enmErrorType, strMessage, strMessageDetail)
    {
    }

    /// <summary>Kết quả lỗi theo mã lỗi enum riêng của từng service (VD AuthErrorCode).</summary>
    public static APIResult Error<TErrorCode>(TErrorCode enmErrorCode, string strMessage, string strMessageDetail = "")
        where TErrorCode : struct, Enum
        => new(true, Convert.ToInt32(enmErrorCode), strMessage, strMessageDetail);

    public string ToJsonString() => JsonConvert.SerializeObject(this);
}
