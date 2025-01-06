using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML
{
    public class ResultMessage
    {
        public enum ErrorTypes
        {
            No_Error,
            LoadInfo,
            Insert,
            Update,
            Delete,
            SearchData,
            GetData,
            ClientNotRegistered,
            InvalidPassowrd,
            AuthenticationHeaderNotExist,
            InvalidIV,
            TokenNotExist,
            TokenInvalid,
            TokenExpired,
            CheckData,
            AccessDataError,
            Others,
            UserDefine,
            UserMustReLogin,
            FileAccess,
            FileNotExists,
            NoNewAppVersion,
            PermissionDenied,
            InvalidDeviceID
        }

        private bool bolIsError = false;

        private int intErrorType;

        private string strMessage = string.Empty;

        private string strMessageDetail = string.Empty;

        public bool IsError
        {
            get
            {
                return bolIsError;
            }
            set
            {
                bolIsError = value;
            }
        }

        public int ErrorType
        {
            get
            {
                return intErrorType;
            }
            set
            {
                intErrorType = value;
            }
        }

        public string Message
        {
            get
            {
                return strMessage;
            }
            set
            {
                strMessage = value;
            }
        }

        public string MessageDetail
        {
            get
            {
                return strMessageDetail;
            }
            set
            {
                strMessageDetail = value;
            }
        }

        public ResultMessage()
        {
            bolIsError = false;
            intErrorType = 0;
            strMessage = "OK";
        }

        public ResultMessage(bool bolIsError, ErrorTypes enErrorTypes, string strMessage, string strMessageDetail)
        {
            this.bolIsError = bolIsError;
            intErrorType = (int)enErrorTypes;
            this.strMessage = strMessage;
            this.strMessageDetail = strMessageDetail;
        }

        public ResultMessage(bool bolIsError, int intErrorType, string strMessage)
        {
            this.bolIsError = bolIsError;
            this.intErrorType = intErrorType;
            this.strMessage = strMessage;
            strMessageDetail = "";
        }

        public ResultMessage(bool bolIsError, int intErrorType, string strMessage, string strMessageDetail)
        {
            this.bolIsError = bolIsError;
            this.intErrorType = intErrorType;
            this.strMessage = strMessage;
            this.strMessageDetail = strMessageDetail;
        }

        public ResultMessage(bool bolIsError, ErrorTypes enErrorTypes, string strMessage)
        {
            this.bolIsError = bolIsError;
            intErrorType = (int)enErrorTypes;
            this.strMessage = strMessage;
            strMessageDetail = "";
        }

        public ResultMessage(APIResult objAPIResult)
        {
            bolIsError = objAPIResult.IsError;
            intErrorType = objAPIResult.StatusID;
            strMessage = objAPIResult.Message;
            strMessageDetail = objAPIResult.MessageDetail;
        }
    }
}
