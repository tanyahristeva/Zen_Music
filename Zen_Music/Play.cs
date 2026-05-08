using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen_Music
{
    public class Play
    {
        public int ID { get; set; }
        public int User_ID { get; set; }
        public int Song_ID { get; set; }
        public DateTime Played_At { get; set; }
        public int Play_Duration { get; set; }
    }
}
