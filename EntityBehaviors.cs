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
    public class EntityBehaviors
    {
        public class EntityBehavior
        {
            public string name;
            public int startingHp;
            public ((int type, int subType, int megaType) element, int count) drops;
            public bool isFlying;
            public bool isSwimming;
            public bool isDigging;
            public bool isJesus;
            public (int v, int h, int s) r;
            public (int v, int h, int s) g;
            public (int v, int h, int s) b;
            public EntityBehavior(string namee, int hp, ((int type, int subType, int megaType) element, int count) drps, (int v, int h, int s) red, (int v, int h, int s) green, (int v, int h, int s) blue, bool F = false, bool S = false, bool D = false, bool J = false)
            {
                name = namee;
                startingHp = hp;
                drops = drps;
                isFlying = F;
                isSwimming = S;
                isDigging = D;
                isJesus = J;
                r = red;
                g = green;
                b = blue;
            }
        }
        public static void makeEntityBehaviorDict()
        {
            entityBehaviorDict = new Dictionary<(int type, int subType), EntityBehavior>()
            {     // Color,   hue,    shade
                { (-1, 0), new EntityBehavior("Error",       69420, ((11, 1, 3), 1),    //  --> Light Bulb          ERROR ! This is the missing type value. Not Fairy.
                  (130, 50, 30), (130, -50, 30), (210, 50, 30)                                        ) },
                { (0, 0), new EntityBehavior("Fairy",           4,  ((-3, 0, 0), 1),    //  --> Fairy Liquid
                  (130, 50, 30), (130, -50, 30), (210, 0, 30),                       F:true          ) },
                { (0, 1), new EntityBehavior("ObsidianFairy",   10, ((-3, 0, 0), 1),    //  --> Fairy Liquid
                  (30, 0, 30), (30, 0, 30), (30, 0, 30),                             F:true          ) },
                { (0, 2), new EntityBehavior("FrostFairy",      4 , ((-3, 0, 0), 1),    //  --> Fairy Liquid
                  (200, 25, 30), (200, 25, 30), (225, 0, 30),                        F:true          ) },
                { (0, 3), new EntityBehavior("SkeletonFairy",   15, ((8, 1, 3), 1),     //  --> Bone
                  (210, 0, 20), (210, 0, 20), (190, 20, 20),                         F:true          ) },
                { (1, 0), new EntityBehavior("Frog",            2,  ((8, 0, 3), 1),     //  --> Flesh
                  (90, 50, 30), (210, 50, 30), (110, -50, 30)                                        ) },
                { (1, 1), new EntityBehavior("Carnal",          7,  ((8, 0, 3), 1),     //  --> Flesh
                  (135, 0, 30), (55, 30, 30), (55, 30, 30)                                           ) },
                { (1, 2), new EntityBehavior("Skeletal",        7,  ((8, 1, 3), 1),     //  --> Bone
                  (210, 0, 20), (210, 0, 20), (190, 20, 20)                                          ) },
                { (2, 0), new EntityBehavior("Fish",            2,  ((8, 0, 3), 1),     //  --> Flesh
                  (190, 0, 30), (80, -50, 30), (80, 50, 30),                         S:true          ) },
                { (2, 1), new EntityBehavior("SkeletonFish",    2,  ((8, 1, 3), 1),     //  --> Bone
                  (210, 0, 20), (210, 0, 20), (190, 20, 20),                         S:true          ) },
                { (3, 0), new EntityBehavior("HornetEgg",       2,  ((8, 0, 3), 1),     //  --> Flesh
                  (205, 10, 30), (205, 10, 30), (235, 0, 30)                                         ) },
                { (3, 1), new EntityBehavior("HornetLarva",     3,  ((8, 0, 3), 1),     //  --> Flesh
                  (180, 10, 30), (180, 10, 30), (160, 0, 30)                                         ) },
                { (3, 2), new EntityBehavior("HornetCocoon",    20, ((8, 0, 3), 1),     //  --> Flesh
                  (120, 10, 30), (120, 10, 30), (20, 0, 20)                                          ) },
                { (3, 3), new EntityBehavior("Hornet",          6,  ((8, 0, 3), 1),     //  --> Flesh
                  (190, 10, 30), (190, 10, 30), (80, 0, 30),                         F:true          ) },
                { (4, 0), new EntityBehavior("Worm",            7,  ((8, 0, 3), 1),     //  --> Flesh
                  (210, 0, 30), (140, 20, 30), (140, 20, 30),                        D:true          ) },
                { (4, 1), new EntityBehavior("Nematode",        3,  ((8, 0, 3), 1),     //  --> Flesh
                  (210, -20, 30), (210, 20, 30), (235, 0, 30),                       S:true, D:true  ) },
                { (5, 0), new EntityBehavior("WaterSkipper",    3,  ((8, 0, 3), 1),     //  --> Flesh
                  (110, 0, 30), (110, 0, 30), (140, 20, 30),                         S:true, J:true  ) },
                { (6, 0), new EntityBehavior("Goblin",          3,  ((8, 0, 3), 1),     //  --> Flesh
                  (80, 50, 30), (175, 50, 30), (80, 50, 30)                                          ) },
            };
        }
        public static Dictionary<(int type, int subType), EntityBehavior> entityBehaviorDict;
    }
}
