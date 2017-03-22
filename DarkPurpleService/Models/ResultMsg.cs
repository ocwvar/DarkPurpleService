using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DarkPurpleService.Models
{
    public class ResultMsg<T>
    {
        public ResultMsg(bool isSuccess, string message , T inObject)
        {
            this.isSuccess = isSuccess;
            this.message = message;
            this.inObject = inObject;
        }

        public bool isSuccess { set; get; }

        public String message { set; get; }

        public T inObject { set; get; }
    }
}