using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNS_Change_Propagator
{
    class Config
    {
        public string domain;
        public string user;
        public string password;
        public int updateInterval;
        public bool debug;
    }
}
