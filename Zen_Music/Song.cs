using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen_Music
{
    public class Song
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public int Album_ID { get; set; }
        public int Duration_Sec { get; set; }
        public string File_URL { get; set; }
        public bool Is_Explicit { get; set; }

        public override string ToString() => Title;
    }
}
