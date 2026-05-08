using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen_Music
{
    public class User
    {
        public int ID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password_Hash { get; set; }
        public string Password_Salt { get; set; }
        public string Avatar_URL { get; set; }
        public int Role_ID { get; set; }
        public DateTime Created_At { get; set; }

        public override string ToString() => Username;
    }
}
