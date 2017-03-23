using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DarkPurpleService.Models
{
    public class MusicFile
    {
        public string name { set; get; }

        public string ownerName { set; get; }

        public string musicURL { set; get; }

        public string coverURL { set; get; }
    }
}