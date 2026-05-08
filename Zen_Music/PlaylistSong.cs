using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen_Music
{
    public class PlaylistSong
    {
        public int Playlist_ID { get; set; }
        public int Song_ID { get; set; }
        public DateTime Added_At { get; set; }
        public int Order_Index { get; set; }
    }
}
