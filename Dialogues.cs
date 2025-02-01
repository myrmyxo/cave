using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using static Cave.Form1;
using static Cave.Globals;
using static Cave.MathF;
using static Cave.Sprites;
using static Cave.Structures;
using static Cave.Nests;
using static Cave.Entities;
using static Cave.EntityBehaviors;
using static Cave.Attacks;
using static Cave.Files;
using static Cave.Plants;
using static Cave.Screens;
using static Cave.Chunks;
using static Cave.Players;
using static Cave.Particles;
using static Cave.Dialogues;

namespace Cave
{
    public partial class Globals
    {
        public Dictionary<((int type, int subType) type, int state), string> sentences = new Dictionary<((int type, int subType) type, int state), string>()
        {
            { ((0, 0), 0), "Hi !"},
            { ((0, 1), 0), "HII !"},
            { ((0, 2), 0), "H-Hkhkhk-Hi !"},
            { ((0, 3), 0), "Klonkhi !"},
            { ((1, 0), 0), "Ribbit !"},
            { ((1, 1), 0), "Sprotch !"},
            { ((1, 2), 0), "Klank !"},
            { ((2, 0), 0), "Blub !"},
            { ((2, 1), 0), "Blonk !"},
            { ((3, 0), 0), "..."},
            { ((3, 1), 0), "Ab ! Ab !"},
            { ((3, 2), 0), "...?"},
            { ((3, 3), 0), "ZZZZZZZZZ !"},
            { ((4, 0), 0), "Slush !"},
            { ((4, 1), 0), "Slirk !"},
            { ((5, 0), 0), "Snyoooom !"},
            { ((6, 1), 0), "Gobli Goblou !"},
        };
        public Dictionary<string, OneSprite> stringSprites = new Dictionary<string, OneSprite>
        {
            { "Test", new OneSprite("Numbers", false) },
        };
    }
    public class Dialogues
    {
        public class Dialogue
        {

        }
    }
}
