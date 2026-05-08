using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen_Music
{
    public class Album
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public DateTime Release_Date { get; set; }
        public string Cover_URL { get; set; }

        public override string ToString() => Title;
    }
}
