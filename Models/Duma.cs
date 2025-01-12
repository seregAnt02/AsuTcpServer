using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ChatServer.Models
{
    public class Duma
    {
        public int id { get; set; }
        public string guid { get; set; }
        public DateTime datetime { get; set; }
        public string macadress { get; set; }
        public string ipadress { get; set; }
        public int port { get; set; }        
        public string status { get; set; }
        public int number { get; set; }
        public string migration { get; set; }
        public int age { get; set; }
    }
}