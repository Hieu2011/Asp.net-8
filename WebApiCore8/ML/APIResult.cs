using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML
{
    public class APIResult
    {
        private bool bolIsError = false;

        private int intStatusID = 0;

        private string strMessage = "";

        private string strMessageDetail = "";

        private object objResultObject = null;

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

        public int StatusID
        {
            get
            {
                return intStatusID;
            }
            set
            {
                intStatusID = value;
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

        public object ResultObject
        {
            get
            {
                return objResultObject;
            }
            set
            {
                objResultObject = value;
            }
        }

        public APIResult()
        {
        }

        public APIResult(ResultMessage objResultMessage)
        {
            bolIsError = objResultMessage.IsError;
            intStatusID = objResultMessage.ErrorType;
            strMessage = objResultMessage.Message;
            strMessageDetail = objResultMessage.MessageDetail;
        }

        public APIResult(int intStatusID, string strMessage, object objResultObject)
        {
            this.intStatusID = intStatusID;
            this.strMessage = strMessage;
            this.objResultObject = objResultObject;
        }

        public APIResult(bool bolIsError, int intStatusID, string strMessage, string strMessageDetail)
        {
            this.bolIsError = bolIsError;
            this.intStatusID = intStatusID;
            this.strMessage = strMessage;
            this.strMessageDetail = strMessageDetail;
        }

        public APIResult(bool bolIsError, ResultMessage.ErrorTypes enErrorTypes, string strMessage, string strMessageDetail)
        {
            this.bolIsError = bolIsError;
            intStatusID = (int)enErrorTypes;
            this.strMessage = strMessage;
            this.strMessageDetail = strMessageDetail;
        }

        public APIResult(object objResultObject)
        {
            this.ResultObject = objResultObject;
        }

        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
