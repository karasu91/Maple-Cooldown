using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MapleCooldown
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class UI
    {
        public int imageSize { get; set; }
        public int windowPosX { get; set; }
        public int windowPosY { get; set; }
        public string font { get; set; } = "Arial";
        public int fontSize { get; set; } = 20;
        public string resetKey { get; set; } = "Escape";
    }

    public class RootUI
    {
        public UI UI { get; set; }
    }
}
