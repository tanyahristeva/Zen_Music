using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen_Music
{
    public class Like
    {
        public int User_ID { get; set; }
        public int Song_ID { get; set; }
        public DateTime Liked_At { get; set; }
    }
}
