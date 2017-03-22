using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DarkPurpleService.Models
{
    public class User
    {
        private string name;
        private string message;

        public string Message
        {
            get
            {
                return message;
            }

            set
            {
                message = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }
    }
}