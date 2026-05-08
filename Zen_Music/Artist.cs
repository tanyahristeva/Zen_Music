using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen_Music
{
    public class Artist
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }
        public string Image_URL { get; set; }

        public override string ToString() => Name;
    }
}
