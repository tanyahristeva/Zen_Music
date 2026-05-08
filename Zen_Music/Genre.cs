using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen_Music
{
    public class Genre
    {
        public int ID { get; set; }
        public string Name { get; set; }

        public override string ToString() => Name;
    }
}
