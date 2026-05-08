using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen_Music
{
    public class Playlist
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Creator_ID { get; set; }
        public bool Is_Public { get; set; }
        public string Cover_URL { get; set; }
        public DateTime Created_At { get; set; }

        public override string ToString() => Name;
    }
}
