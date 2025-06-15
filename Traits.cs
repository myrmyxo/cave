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
using static Cave.Traits;
using static Cave.Attacks;
using static Cave.Files;
using static Cave.Plants;
using static Cave.Screens;
using static Cave.Chunks;
using static Cave.Players;
using static Cave.Particles;
using static Cave.Dialogues;
using System.Diagnostics;

namespace Cave
{
    public class Traits
    {
        public class ColorRange
        {
            public (int v, int h, int s) r;
            public (int v, int h, int s) g;
            public (int v, int h, int s) b;
            public ColorRange((int v, int h, int s) red, (int v, int h, int s) green, (int v, int h, int s) blue)
            {
                r = red;
                g = green;
                b = blue;
            }
        }
        public static Dictionary<string, ColorRange> famousColorRanges = new Dictionary<string, ColorRange>
        {
            { "Obsidian", new ColorRange((30, 0, 0), (30, 0, 0), (40, 0, 10)) },
            { "ObsidianPollen", new ColorRange((220, 0, 0), (220, 0, 0), (230, 0, 10)) },
        };
        public class TileTraits
        {
            public (int type, int subType) type;
            public string name;

            public bool isSolid;
            public bool isAir;
            public bool isLiquid;
            public bool isAcidic;
            public bool isLava;         // H for hot
            public bool isTransformant; // Fairy liquid

            public bool isTransparent;
            public bool isSlippery;

            public int hardness;
            public int viscosity = 0;

            public ColorRange colorRange;
            public float biomeColorBlend;
            public bool isTextured;     // For mold for now
            public TileTraits(string namee, float biomeColorBlendToPut, ColorRange colRange, bool Air = false, bool Tex = false, bool L = false, bool H = false, bool A = false, bool T = false, bool S = false, bool Tr = false)
            {
                name = namee;
                colorRange = colRange;
                biomeColorBlend = biomeColorBlendToPut;
                isTextured = Tex;

                isAir = Air;
                isLiquid = L;
                isSolid = (isAir || isLiquid) ? false : true;

                isLava = H;
                isAcidic = A;
                isTransformant = T;
                isSlippery = S;
                isTransparent = Tr || isLiquid || isAir ;
            }
            public void setType((int type, int subType) typeToSet) { type = typeToSet; }
        }

        public static Dictionary<(int type, int subType), TileTraits> tileTraitsDict;
        public static TileTraits getTileTraits((int type, int subType) tileType) { return tileTraitsDict.ContainsKey(tileType) ? tileTraitsDict[tileType] : tileTraitsDict[(0, 0)]; }
        public static void makeTileTraitsDict()
        {
            tileTraitsDict = new Dictionary<(int type, int subType), TileTraits>()
            {
                { (-7, 0), new TileTraits("Acid", 0.2f,
                new ColorRange((120, 0, 0), (180, 0, 0), (60, 0, 0)),       L:true, A:true                              ) },

                { (-6, 0), new TileTraits("Blood", 0.2f,
                new ColorRange((100, 0, 0), (15, 0, 0), (25, 0, 0)),        L:true                                      ) },
                { (-6, 1), new TileTraits("Deoxygenated Blood", 0.2f,
                new ColorRange((65, 0, 0), (5, 0, 0), (35, 0, 0)),          L:true                                      ) },

                { (-5, 0), new TileTraits("Honey", 0.2f,
                new ColorRange((160, 0, 0), (120, 0, 0), (70, 0, 0)),       L:true                                      ) },

                { (-4, 0), new TileTraits("Lava", 0.05f,
                new ColorRange((255, 0, 0), (90, 0, 0), (0, 0, 0)),         L:true, H:true                              ) },

                { (-3, 0), new TileTraits("Fairy Liquid", 0.2f,
                new ColorRange((105, 0, 0), (80, 0, 0), (120, 0, 0)),       L:true, T:true                              ) },

                { (-2, -1), new TileTraits("Ice", 0.2f,
                new ColorRange((160, 0, 0), (160, 0, 0), (200, 0, 0)),      S:true, Tr:true                             ) },
                { (-2, 0), new TileTraits("Water", 0.2f,
                new ColorRange((80, 0, 0), (80, 0, 0), (120, 0, 0)),        L:true                                      ) },

                { (-1, 0), new TileTraits("Piss", 0.2f,                                                      
                new ColorRange((120, 0, 0), (120, 0, 0), (80, 0, 0)),       L:true                                      ) },


                { (0, 0), new TileTraits("Error/Air", 0.5f,
                new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0)),      Air:true                                    ) },


                { (1, 0), new TileTraits("Rock", 0.5f,
                new ColorRange((30, 0, 0), (30, 0, 0), (30, 0, 0))                                                      ) },
                { (1, 1), new TileTraits("Dense Rock", 0.2f,
                new ColorRange((10, 0, 0), (10, 0, 0), (10, 0, 0))                                                      ) },

                { (2, 0), new TileTraits("Dirt", 0.5f,
                new ColorRange((80, 0, 0), (60, 0, 0), (20, 0, 0))                                                      ) },

                { (3, 0), new TileTraits("Plant Matter", 0.35f,
                new ColorRange((10, 0, 0), (60, 0, 0), (30, 0, 0))                                                      ) },

                { (4, 0), new TileTraits("Flesh Tile", 0.2f,
                new ColorRange((135, 0, 0), (55, 0, 0), (55, 0, 0))                                                     ) },
                { (4, 1), new TileTraits("Bone Tile", 0.2f,
                new ColorRange((240, 0, 0), (230, 0, 0), (245, 0, 0))                                                   ) },

                { (5, 0), new TileTraits("Mold Tile", 0.1f,
                new ColorRange((50, 0, 0), (50, 0, 0), (100, 0, 0)),        Tex:true                                    ) },
            };

            foreach ((int type, int subType) typeToSet in tileTraitsDict.Keys) { tileTraitsDict[typeToSet].setType(typeToSet); }
        }





        public class MaterialTraits
        {
            public string name;
            public (int type, int subType, int megaType)? toolGatheringRequirement;
            public ColorRange colorRange;
            //public float biomeColorBlend;
            public MaterialTraits(string namee, (int type, int subType, int megaType)? tool = null, ColorRange col = null)
            {
                name = namee;
                toolGatheringRequirement = tool;
                colorRange = col;
                // biomeColorBlend = biomeColorBlendToPut;
            }
        }

        public static Dictionary<(int type, int subType), MaterialTraits> materialTraitsDict;
        public static MaterialTraits getMaterialTraits((int type, int subType) materialType)
        {
            return materialTraitsDict.ContainsKey(materialType) ? materialTraitsDict[materialType] : materialTraitsDict[(0, 0)];
        }
        public static void makeMaterialTraitsDict()
        {
            materialTraitsDict = new Dictionary<(int type, int subType), MaterialTraits>()
            {
                { (0, 0), new MaterialTraits("Error/Air", 
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },

                { (1, 0), new MaterialTraits("Plant Matter",
                col:new ColorRange((50, 0, 30), (170, 50, 30), (50, 0, 30))                                             ) },
                { (1, 1), new MaterialTraits("Wood",                        tool:(4, 0, 4),
                col:new ColorRange((140, 20, 30), (140, 20, 30), (50, 0, 30))                                           ) },
                { (1, 2), new MaterialTraits("Kelp",
                col:new ColorRange((30, 0, 30), (90, 50, 30), (140, -50, 30))                                           ) },
                { (1, 3), new MaterialTraits("Obsidian Plant Matter",
                col:famousColorRanges["Obsidian"]                                                                       ) },

                { (2, 0), new MaterialTraits("Petal",
                col:new ColorRange((170, 20, 30), (120, 0, 30), (150, -20, 30))                                         ) },
                { (2, 1), new MaterialTraits("Pollen",
                col:new ColorRange((170, -10, 30), (170, 10, 30), (50, 0, 30))                                          ) },

                { (3, 0), new MaterialTraits("Mushroom Stem",
                col:new ColorRange((180, 0, 30), (160, 0, 30), (165, 0, 30))                                            ) },
                { (3, 1), new MaterialTraits("Mushroom Cap",
                col:new ColorRange((140, 0, -30), (120, 50, 0), (170, -50, 0))                                          ) },
                { (3, 2), new MaterialTraits("Mold",
                col:new ColorRange((50, 0, 30), (50, 0, 30), (100, 0, 30))                                              ) },

                { (8, 0), new MaterialTraits("Flesh",
                col:new ColorRange((135, 20, 20), (55, 0, 20), (55, 0, 20))                                             ) },
                { (8, 1), new MaterialTraits("Bone",
                col:new ColorRange((210, 0, 20), (210, 0, 20), (190, 20, 20))                                           ) },
                { (8, 2), new MaterialTraits("Hair",
                col:new ColorRange((65, 20, 20), (40, 0, 20), (25, -20, 20))                                            ) },
                { (8, 3), new MaterialTraits("Fat",
                col:new ColorRange((200, 0, 0), (200, 0, 0), (170, 0, 0))                                               ) },

                { (10, 0), new MaterialTraits("Magic Rock",
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },

                { (11, 0), new MaterialTraits("Metal",                      tool:(4, 0, 4),
                col:new ColorRange((40, 0, 10), (40, 0, 10), (60, 0, 10))                                               ) },
                { (11, 1), new MaterialTraits("Lightbulb",
                col:new ColorRange((230, 0, 10), (230, 0, 10), (120, 0, 10))                                            ) },

                { (12, 0), new MaterialTraits("Wax",
                col:new ColorRange((210, 0, 10), (210, 0, 10), (200, 0, 10))                                            ) },
            };
        }






        public class EntityTraits
        {
            public string name;
            public int startingHp;
            public ((int type, int subType, int megaType) element, int count) drops;

            public ColorRange colorRange;
            public (Color color, float period)? wingTraits;
            public int? lightRadius;
            public (int baseLength, int variation)? length;
            public (int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, (int a, int r, int g, int b) value)? color)[] tailMap;

            // Characteristics (automatically derived from Behaviors)
            public bool isFlying;
            public bool isSwimming;
            public bool isDigging;
            public bool isJesus;
            public bool isCliming;

            // Behaviors
            public int inWaterBehavior;     // -> 0: nothing, 1: float upwards, 2: move randomly in water
            public int onWaterBehavior;     // -> 0: nothing, 1: skip, 2: drift towards land
            public int inAirBehavior;       // -> 0: nothing, 1: fly randomly, 2: random drift
            public int onGroundBehavior;    // -> 0: nothing, 1: random jump, 2: move around, 3: dig down
            public int inGroundBehavior;    // -> 0: nothing, 1: random jump, 2: dig around, 3: teleport, 4: dig tile
            public int onPlantBehavior;     // -> 0: fallOut, 1: random movement

            public float swimSpeed;
            public float swimMaxSpeed;

            public (float x, float y) jumpStrength;
            public float jumpChance;
            public EntityTraits(string namee, int hp, ((int type, int subType, int megaType) element, int count) drps,
                ColorRange colRange, (Color color, float period)? wT = null, int? lR = null, (int baseLength, int variation)? L = null,
                (int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, (int a, int r, int g, int b) value)? color)[] tM = null,
                int iW = 0, int oW = 0, int iA = 0, int oG = 0, int iG = 0, int oP = 0,
                float sS = 0.1f, float sMS = 0.5f, (float x, float y)? jS = null, float jC = 0)
            {
                name = namee;
                startingHp = hp;
                drops = drps;

                colorRange = colRange;
                wingTraits = wT;
                lightRadius = lR;
                length = L;
                tailMap = tM;

                isFlying = iA == 1 ? true : false;
                isSwimming = iW == 2 ? true : false;
                isDigging = iG == 2 ? true : false;
                isJesus = oW == 1 ? true : false;
                isCliming = oP != 0 ? true : false;

                inWaterBehavior = iW;
                onWaterBehavior = oW;
                inAirBehavior = iA;
                inGroundBehavior = iG;
                onGroundBehavior = oG;
                onPlantBehavior = oP;

                swimSpeed = sS;
                swimMaxSpeed = sMS;

                jumpStrength = jS ?? (1, 1);
                jumpChance = jC;
            }
        }
        public static Dictionary<(int type, int subType), EntityTraits> entityTraitsDict;
        public static void makeEntityTraitsDict()
        {
            entityTraitsDict = new Dictionary<(int type, int subType), EntityTraits>()
            {  // R              G               B     ->     (Color, hue, shade)
                { (-1, 0), new EntityTraits("Error",       69420, ((11, 1, 3), 1),      //  --> Light Bulb          ERROR ! This is the missing type value. Not Fairy.
                new ColorRange((130, 50, 30), (130, -50, 30), (210, 50, 30)),
                iW:1, oG:1, iG:2) },

                { (0, 0), new EntityTraits("Fairy",           4,  ((-3, 0, 0), 1),      //  --> Fairy Liquid
                new ColorRange((130, 50, 30), (130, -50, 30), (210, 0, 30)), lR:7, wT:(Color.FromArgb(50, 220, 220, 200), 0.02165f),
                iW:1, iA:1, iG:3) },                                                                        
                { (0, 1), new EntityTraits("ObsidianFairy",   10, ((-3, 0, 0), 1),      //  --> Fairy Liquid
                new ColorRange((30, 0, 30), (30, 0, 30), (30, 0, 30)), lR:7, wT:(Color.FromArgb(50, 0, 0, 0), 0.02165f),
                iW:1, iA:1, iG:3) },
                { (0, 2), new EntityTraits("FrostFairy",      4 , ((-3, 0, 0), 1),      //  --> Fairy Liquid
                new ColorRange((200, 25, 30), (200, 25, 30), (225, 0, 30)), lR:7, wT:(Color.FromArgb(50, 255, 255, 255), 0.02165f),
                iW:1, iA:1, iG:3) },
                { (0, 3), new EntityTraits("SkeletonFairy",   15, ((8, 1, 3), 1),       //  --> Bone
                new ColorRange((210, 0, 20), (210, 0, 20), (190, 20, 20)), lR:7, wT:(Color.FromArgb(50, 230, 230, 230), 0.02165f),
                iW:1, iA:1, iG:3) },

                { (1, 0), new EntityTraits("Frog",            2,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((90, 50, 30), (210, 50, 30), (110, -50, 30)),
                iW:1, oW:2, iA:2, oG:1, iG:1, jS:(2, 2.5f), jC:0.1f) },
                { (1, 1), new EntityTraits("Carnal",          7,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((135, 0, 30), (55, 30, 30), (55, 30, 30)),
                iW:1, oW:2, iA:2, oG:1, iG:1, jS:(2, 2.5f), jC:0.1f) },
                { (1, 2), new EntityTraits("Skeletal",        7,  ((8, 1, 3), 1),       //  --> Bone
                new ColorRange((210, 0, 20), (210, 0, 20), (190, 20, 20)),
                iW:1, oW:2, iA:2, oG:1, iG:1, jS:(2, 2.5f), jC:0.1f) },

                { (2, 0), new EntityTraits("Fish",            2,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((190, 0, 30), (80, -50, 30), (80, 50, 30)),
                iW:2, oG:1, iG:1, jC:0.01f) },
                { (2, 1), new EntityTraits("SkeletonFish",    2,  ((8, 1, 3), 1),       //  --> Bone
                new ColorRange((210, 0, 20), (210, 0, 20), (190, 20, 20)),
                iW:2, oG:1, iG:1, jC:0.01f) },

                { (3, 0), new EntityTraits("HornetEgg",       2,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((205, 10, 30), (205, 10, 30), (235, 0, 30)),                                              
                iW:1) },
                { (3, 1), new EntityTraits("HornetLarva",     3,  ((8, 0, 3), 1),       //  --> Flesh   
                new ColorRange((180, 10, 30), (180, 10, 30), (160, 0, 30)),                             
                iW:1, oW:2, iA:2, oG:1, iG:1, jC:0.01f) },                                                              
                { (3, 2), new EntityTraits("HornetCocoon",    20, ((8, 0, 3), 1),       //  --> Flesh   
                new ColorRange((120, 10, 30), (120, 10, 30), (20, 0, 20)),                              
                iW:1) },
                { (3, 3), new EntityTraits("Hornet",          6,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((190, 10, 30), (190, 10, 30), (80, 0, 30)), wT:(Color.FromArgb(50, 220, 220, 200), 0.02165f),
                iW:1, iA:1, iG:4) },

                { (4, 0), new EntityTraits("Worm",            7,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((210, 0, 30), (140, 20, 30), (140, 20, 30)), L:(2, 4),
                iW:1, oW:2, iA:2, oG:3, iG:2) },
                { (4, 1), new EntityTraits("Nematode",        3,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((210, -20, 30), (210, 20, 30), (235, 0, 30)), L:(2, 8),
                iW:2, oW:2, iA:2, oG:3, iG:2) },

                { (5, 0), new EntityTraits("WaterSkipper",    3,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((110, 0, 30), (110, 0, 30), (140, 20, 30)),
                iW:1, oW:1, iA:2, oG:1, iG:1, jC:0.05f) },

                { (6, 0), new EntityTraits("Goblin",          3,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((80, 50, 30), (175, 50, 30), (80, 50, 30)),
                iW:1, oW:2, iA:2, oG:1, iG:1, jS:(1, 4), jC:0.05f) },

                { (7, 0), new EntityTraits("Louse",           2,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((160, -10, 30), (180, 10, 30), (200, 10, 30)),
                iW:1, oW:2, iA:2, oG:4, iG:1, oP:1, jS:(1, 1.5f), jC:0.1f) },

                { (8, 0), new EntityTraits("Shark",           5,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((110, 0, 30), (110, 0, 30), (140, 20, 30)), L:(4, 1), tM:new (int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, (int a, int r, int g, int b) value)? color)[]{ (1, false, false, 0, (0, 1), (true, (0, -15, -15, -20))), (1, false, false, 0, (0, 2), (true, (0, -15, -15, -20))),     (0, true, true, 1, (1, 0), (true, (0, -15, -15, -20))), (0, true, true, 7, (1, 0), (true, (0, -15, -15, -20))) },
                iW:2, oG:1, iG:1, jC:0.01f) },

                { (9, 0), new EntityTraits("WaterDog",       15,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((150, 0, 30), (120, 10, 30), (90, 20, 30)), L:(13, 7), tM:new (int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, (int a, int r, int g, int b) value)? color)[]{ (2, false, false, 0, (0, 1), (true, (0, -20, -20, -15))), (2, false, false, 0, (0, 2), (true, (0, -20, -20, -15))),    (3, false, false, 0, (0, -1), (true, (0, -20, -20, -15))),    (5, false, false, 0, (0, 1), (true, (0, -20, -20, -15))), (5, false, false, 0, (0, 2), (true, (0, -20, -20, -15))),    (6, false, false, 0, (0, -1), (true, (0, -20, -20, -15))),    (8, false, false, 0, (0, 1), (true, (0, -20, -20, -15))),    (11, false, false, 0, (0, 1), (true, (0, -20, -20, -15))) },
                iW:2, oG:1, iG:1, jC:0.01f) },
            };
        }


         




        



        public class PlantStructureFrame
        {
            public Dictionary<(int x, int y), (int type, int subType)> elementDict;
            public PlantStructureFrame(Dictionary<(int type, int subType), (int x, int y)[]> dict = null)
            {
                elementDict = new Dictionary<(int type, int subType), (int x, int y)>();
                if (dict is null) { return; }
                foreach ((int type, int subType) key in dict.Keys)
                {
                    foreach ((int x, int y) pos in dict[key]) { elementDict[pos] = key; }
                }
            }
        }

        public static Dictionary<string, PlantStructureFrame> plantStructureFramesDict;
        public static void makePlantStructureFramesDict()
        {
            plantStructureFramesDict = new Dictionary<string, PlantStructureFrame>()
            {
                { "Error", new PlantStructureFrame(                                                                  ) },

                { "TulipFlower1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "TulipFlower2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0), (0, 1) } } }
                ) },
                { "TulipFlower3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0), (0, 1), (0, 2) } } }
                ) },
                { "TulipFlower4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0), (-1, 1), (0, 1), (1, 1), (0, 2) } } }
                ) },
                { "TulipFlower5", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0), (-1, 1), (0, 1), (1, 1), (-1, 2), (1, 2) } } }
                ) },

                { "AlliumFlower1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "AlliumFlower2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0), (-1, 1), (0, 1), (1, 1) } } }
                ) },
                { "AlliumFlower3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0), (-1, 1), (0, 1), (1, 1), (-1, 2), (0, 2), (1, 2) } } }
                ) },
                { "AlliumFlower4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0), (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1), (-2, 2), (-1, 2), (0, 2), (1, 2), (2, 2), (-1, 3), (0, 3), (1, 3) } } }
                ) },



                { "TreeLeaves1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "TreeLeaves2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, -1), (-1, 0), (0, 0), (1, 0), (0, 1) } } }
                ) },
                { "TreeLeaves3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-1, -1), (0, -1), (1, -1), (-1, 0), (0, 0), (1, 0), (-1, 1), (0, 1), (1, 1) } } }
                ) },
                { "TreeLeaves4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-1, -2), (0, -2), (1, -2), (-2, -1), (-1, -1), (0, -1), (1, -1), (2, -1), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1), (-1, 2), (0, 2), (1, 2) } } }
                ) },

                { "Lantern1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 1), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "Lantern2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 1), new (int x, int y)[] { (0, 0) } },  { (11, 0), new (int x, int y)[] { (0, 1) } } }
                ) },
                { "Lantern3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 1), new (int x, int y)[] { (0, 0) } },  { (11, 0), new (int x, int y)[] { (-1, 1), (0, 1), (1, 1) } } }
                ) },
                { "Lantern4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 1), new (int x, int y)[] { (0, 0) } },  { (11, 0), new (int x, int y)[] { (-1, 1), (0, 1), (1, 1), (0, 2) } } }
                ) },
                { "Lantern5", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 1), new (int x, int y)[] { (0, 0) } },  { (11, 0), new (int x, int y)[] { (-1, -1), (0, -1), (1, -1), (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1), (0, 2) } } }
                ) },

                { "CandleHolder1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "CandleHolder2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 0), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0) } } }
                ) },


                { "MushroomCap1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (3, 1), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0) } } }
                ) },
                { "MushroomCap2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (3, 1), new (int x, int y)[] { (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0) } } }
                ) },
                { "MushroomCap3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (3, 1), new (int x, int y)[] { (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (1, 1), (-1, 1), (0, 1), (1, 1) } } }
                ) },



                { "PlusFlower1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "PlusFlower2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -1), (-1, 0), (0, 0), (1, 0), (0, 1) } } }
                ) },
                { "PlusFlower3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -1), (-1, 0), (1, 0), (0, 1) } },     { (2, 1), new (int x, int y)[] { (0, 0) } } }
                ) },

                { "CrossFlower1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "CrossFlower2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, -1), (-1, 1), (0, 0), (1, -1), (1, 1) } } }
                ) },
                { "CrossFlower3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, -1), (-1, 1), (1, -1), (1, 1) } },   { (2, 1), new (int x, int y)[] { (0, 0) } } }
                ) },

                { "BigFlower1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "BigFlower2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -1), (-1, 0), (0, 0), (1, 0), (0, 1) } } }
                ) },
                { "BigFlower3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -2), (0, -1), (-2, 0), (-1, 0), (0, 0), (2, 0), (1, 0), (0, 2), (0, 1), (1, 1), (-1, 1), (1, -1), (-1, -1), } } }
                ) },
                { "BigFlower4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -3), (0, -2), (0, -1), (-3, 0), (-2, 0), (-1, 0), (0, 0), (3, 0), (2, 0), (1, 0), (0, 3), (0, 2), (0, 1),     (2, 2), (1, 1), (-2, 2), (-1, 1), (2, -2), (1, -1), (-2, -2), (-1, -1), } } }
                ) },
                { "BigFlower5", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -3), (0, -2), (0, -1), (-3, 0), (-2, 0), (-1, 0), (3, 0), (2, 0), (1, 0), (0, 3), (0, 2), (0, 1),     (2, 2), (1, 1), (-2, 2), (-1, 1), (2, -2), (1, -1), (-2, -2), (-1, -1), } }, { (2, 1), new (int x, int y)[] { (0, 0) } } }
                ) },

                { "ReedFlower1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "ReedFlower2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0), (0, 1) } } }
                ) },
                { "ReedFlower3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0), (0, 1) } }, { (2, 1), new (int x, int y)[] { (0, 2) } } }
                ) },
            };
        }



        public class PlantGrowthRules
        {
            public (int maxLevel, int range) maxGrowth;
            public bool offsetMaxGrowthVariation;
            public (float baseValue, float variation) growthSpeedVariationFactor;

            public (int type, int subType) materalToFillWith;
            public (int type, int subType)? tileContentNeededToGrow;
            public ((int x, int y, bool stopGrowth)[] left, (int x, int y, bool stopGrowth)[] right, (int x, int y, bool stopGrowth)[] down, (int x, int y, bool stopGrowth)[] up)? hindrancePreventionPositions;

            public ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] childrenOnGrowthStart;
            public ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] childrenOnGrowthEnd;
            public ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] childArray;
            public int childOffset;
            public bool mirrorTwinChildren;
            public bool loopChild;

            public ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped)? startDirection;
            public ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] directionGrowthArray;
            public int dGOffset;
            public bool rotationalDG;
            public bool loopDG;

            public ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] growthPosModArray;
            public int pMOffset;
            public bool loopPM;

            public bool preventGaps;
            public bool isMold;
            public PlantGrowthRules((int type, int subType) t, (int type, int subType)? tCNTG = null, (int frame, int range)? mG = null, bool oMGV = false, (float baseValue, float variation)? gSVF = null,
                ((int x, int y, bool stopGrowth)[] left, (int x, int y, bool stopGrowth)[] right, (int x, int y, bool stopGrowth)[] down, (int x, int y, bool stopGrowth)[] up)? hPP = null,
                ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] cOGS = null,
                ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] cOGE = null,
                ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] C = null, int cO = 0, bool lC = false, bool mTC = false,    // O O OOOO I. WANT A HEN-
                ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped)? sD = null,
                ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] DG = null, int dGO = 0, bool rDG = false, bool lDG = false,
                ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] PM = null, int pMO = 0, bool lPM = false, bool pG = true, bool M = false)
            {
                maxGrowth = mG ?? (5, 0);
                offsetMaxGrowthVariation = oMGV;
                growthSpeedVariationFactor = gSVF ?? (1, 0);

                materalToFillWith = t;
                tileContentNeededToGrow = tCNTG;
                hindrancePreventionPositions = hPP;

                childrenOnGrowthStart = cOGS;
                childrenOnGrowthEnd = cOGE;
                childArray = C;
                childOffset = cO;
                mirrorTwinChildren = mTC;
                loopChild = lC;

                startDirection = sD;
                directionGrowthArray = DG;
                dGOffset = dGO;
                rotationalDG = rDG;
                loopDG = lDG;

                growthPosModArray = PM;
                pMOffset = pMO;
                loopPM = lPM;

                preventGaps = pG;
                isMold = M;
            }
        }
        public static Dictionary<string, ((int x, int y, bool stopGrowth)[] left, (int x, int y, bool stopGrowth)[] right, (int x, int y, bool stopGrowth)[] down, (int x, int y, bool stopGrowth)[] up)> fHPP = new Dictionary<string, ((int x, int y, bool stopGrowth)[] left, (int x, int y, bool stopGrowth)[] right, (int x, int y, bool stopGrowth)[] down, (int x, int y, bool stopGrowth)[] up)>
        {
            { "Leaves", (new (int x, int y, bool stopGrowth)[]{ (-1, -2, false), (-2, -1, true), (-2, 0, true), (-2, 1, true), (-1, 2, false) }, new (int x, int y, bool stopGrowth)[]{ (1, -2, false), (2, -1, true), (2, 0, true), (2, 1, true), (1, 2, false) }, new (int x, int y, bool stopGrowth)[]{ (-2, -1, false), (-1, -2, true), (0, -2, true), (1, -2, true), (2, -1, false) }, new (int x, int y, bool stopGrowth)[]{ (-2, 1, false), (-1, 2, true), (0, 2, true), (1, 2, true), (2, 1, false) }) },
            { "Fire", (new (int x, int y, bool stopGrowth)[]{ (-1, 1, true), (-1, 2, true), (-1, 3, true), (-1, 4, true) }, new (int x, int y, bool stopGrowth)[]{ (1, 1, true), (1, 2, true), (1, 3, true), (1, 4, true) }, new (int x, int y, bool stopGrowth)[]{ (-1, 0, true), (0, 0, true), (1, 0, true) }, new (int x, int y, bool stopGrowth)[]{ (-1, 4, true), (0, 4, true), (1, 4, true) }) },
            
            { "Up1Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 1, true) }) },
            { "Up2Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 2, true) }) },
            { "Up3Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 3, true) }) },
            { "Up3GapX3Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (-1, 3, true), (0, 3, true), (1, 3, true) }) },
            { "Up4Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 4, true) }) },

            { "Down1Gap", (null, null, new (int x, int y, bool stopGrowth)[]{ (0, -1, true) }, null) },
            { "Down2Gap", (null, null, new (int x, int y, bool stopGrowth)[]{ (0, -2, true) }, null) },
            { "Down3Gap", (null, null, new (int x, int y, bool stopGrowth)[]{ (0, -3, true) }, null) },
            { "Down4Gap", (null, null, new (int x, int y, bool stopGrowth)[]{ (0, -4, true) }, null) },
        };
        public class PlantElementTraits
        {
            public string name;
            public bool isRegenerative;
            public (int maxLevel, int range) maxGrowth;
            public ((int x, int y) pos, (bool x, bool y) flip)? isSticky;

            public bool isClimbable;

            public OneAnimation animation;
            public ((int frame, int range) changeFrame, PlantStructureFrame frame)[] frames;
            public ((int type, int subType, int subSubType) plantElement, (int x, int y) offset, int chance)? deathChild;
            public PlantGrowthRules plantGrowthRules;

            public ((int x, int y) pos, (bool x, bool y) baseDirectionFlip)[] requiredEmptyTiles;
            public ((int x, int y) pos, (int type, int subType) type, (bool x, bool y) baseDirectionFlip)[] specificRequiredEmptyTiles;

            public bool forceLightAtPos;
            public int lightRadius;
            public (int type, int subType) lightElement;
            public (int type, int subType)[] colorOverrideArray;    // not used YET (will be used if individual plantElements of the same plant need to have different colors (like idk a flower is blue, another is yellow... or different leaf colors in the same tree...)
            
            public HashSet<(int type, int subType)> materialsPresent;
            public PlantElementTraits(string namee, ((int x, int y) pos, (bool x, bool y) flip)? stick = null, (int maxLevel, int range)? fMG = null, OneAnimation anm = null,
                ((int frame, int range) changeFrame, PlantStructureFrame frame)[] framez = null, ((int type, int subType, int subSubType) plantElement,
                (int x, int y) offset, int chance)? dC = null, PlantGrowthRules pGR = null, ((int x, int y) pos, (bool x, bool y) baseDirectionFlip)[] rET = null,
                ((int x, int y) pos, (int type, int subType) type, (bool x, bool y) baseDirectionFlip)[] sRET = null, (int type, int subType)[] cOverride = null,
                bool isReg = false, bool fLAP = false, int lR = 0, (int type, int subType)? lE = null, bool iC = false)
            {
                name = namee;
                isRegenerative = isReg;
                isSticky = stick;
                isClimbable = iC;
                animation = anm;
                frames = framez;
                deathChild = dC;
                colorOverrideArray = cOverride;
                plantGrowthRules = pGR;
                forceLightAtPos = fLAP;
                lightElement = lE ?? (0, 0);
                lightRadius = lR;
                requiredEmptyTiles = rET;
                specificRequiredEmptyTiles = sRET;

                materialsPresent = new HashSet<(int type, int subType)>();
                if (plantGrowthRules != null)
                {
                    maxGrowth = fMG ?? plantGrowthRules.maxGrowth;  // fMG is forceMaxGrowth
                    materialsPresent.Add(plantGrowthRules.materalToFillWith);
                }
                else if (frames != null)
                {
                    maxGrowth = fMG ?? (framez.Length, 0);  // fMG is forceMaxGrowth
                    foreach (((int frame, int range) changeFrame, PlantStructureFrame frame) frame in frames)
                    {
                        foreach ((int type, int subType) material in frame.frame.elementDict.Values)
                        {
                            materialsPresent.Add(material);
                        }
                    }
                }
                else { maxGrowth = fMG ?? (5, 0); }     // fMG is forceMaxGrowth
            }
        }
        public static ((int frame, int range) changeFrame, PlantStructureFrame frame)[] makeStructureFrameArray((int frame, int range)[] frameChange = null, params string[] args)
        {
            ((int frame, int range) changeFrame, PlantStructureFrame frame)[] arrayo = new ((int frame, int range) changeFrame, PlantStructureFrame frame)[args.Length];
            if (frameChange is null) { for (int i = 0; i < args.Length; i++) { arrayo[i] = ((1, 0), plantStructureFramesDict.ContainsKey(args[i]) ? plantStructureFramesDict[args[i]] : plantStructureFramesDict["Error"]); } }
            else if (frameChange.Length != args.Length) { throw new Exception("Error in creation of StructureFrameArray ! Frame array and frameChange array had different lengths !"); }
            else { for (int i = 0; i < args.Length; i++) { arrayo[i] = (frameChange[i], plantStructureFramesDict.ContainsKey(args[i]) ? plantStructureFramesDict[args[i]] : plantStructureFramesDict["Error"]); } }
            return arrayo;
        }
        public static PlantElementTraits getPlantElementTraits((int type, int subType, int subSubType) plantType) { return plantElementTraitsDict.ContainsKey(plantType) ? plantElementTraitsDict[plantType] : plantElementTraitsDict[(-1, 0, 0)]; }

        public static Dictionary<(int type, int subType, int subSubType), PlantElementTraits> plantElementTraitsDict;
        public static void makePlantElementTraitsDict()
        {
            plantElementTraitsDict = new Dictionary<(int type, int subType, int subSubType), PlantElementTraits>()
            {
                { (-1, 0, 0), new PlantElementTraits("Error") },



                // Stems and trunks (procedural generation based on rules), subSubType -> 0 (like (1, 4, _0_))
                
                { (0, 0, 0), new PlantElementTraits("BaseStem",
                pGR:new PlantGrowthRules(t:(1, 0), mG:(2, 4), hPP:fHPP["Up1Gap"],
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, true), (2, 1), 100) },
                    lPM:true
                )) },
                { (0, 1, 0), new PlantElementTraits("TulipStem", rET:(from number in Enumerable.Range(0, 6) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 0), mG:(2, 3), hPP:fHPP["Up3GapX3Gap"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((0, 1, 1), (0, 0), 0, 0, (1, 0), 100) }
                )) },
                { (0, 2, 0), new PlantElementTraits("AlliumStem", rET:(from number in Enumerable.Range(0, 7) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 0), mG:(3, 2), hPP:fHPP["Up4Gap"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((0, 2, 1), (0, 0), 0, 0, (1, 1), 100) }
                )) },
                { (0, 3, 0), new PlantElementTraits("BigFlowerStem", rET:(from number in Enumerable.Range(0, 12) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 0), mG:(8, 4), hPP:fHPP["Up4Gap"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((0, 3, 1), (0, 0), 0, 0, (1, 1), 100) }
                )) },

                { (1, 0, 0), new PlantElementTraits("BaseTrunk", rET:(from number in Enumerable.Range(0, 20) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 1), mG:(15, 35), hPP:fHPP["Leaves"],
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((1, 0, 1), (0, 0), 0, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 0, -1), (0, 0), 0, 0, (3, 6), 100) },
                    lC:true,
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, true), (4, 8), 100) },
                    lPM:true
                )) },

                { (2, 0, 0), new PlantElementTraits("Kelp", rET:(from number in Enumerable.Range(0, 5) select ((number % 2, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 2), mG:(3, 8), tCNTG:(-2, 0),
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (1, 0), 100), ((-1, 0), (true, false, false), (1, 0), 100) },
                    lPM:true
                )) },
                { (2, 1, 0), new PlantElementTraits("KelpCeiling", rET:(from number in Enumerable.Range(0, 5) select ((number % 2, -number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 2), mG:(3, 8), tCNTG:(-2, 0),
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (1, 0), 100), ((-1, 0), (true, false, false), (1, 0), 100) },
                    lPM:true
                )) },
                { (2, 2, 0), new PlantElementTraits("ReedStem", rET:(from number in Enumerable.Range(0, 5) select ((0, number), (true, false))).ToArray(), sRET:new ((int x, int y) pos, (int type, int subType) type, (bool x, bool y) baseDirectionFlip)[] { ((0, 3), (0, 0), (true, false)) },
                pGR:new PlantGrowthRules(t:(1, 0), mG:(4, 4), hPP:fHPP["Up3Gap"],
                    // PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (1, 0)), ((-1, 0), (true, false, false), (1, 0)) }
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((2, 2, 1), (0, 0), 0, 0, (1, 1), 100) }
                )) },

                { (3, 0, 0), new PlantElementTraits("ObsidianStem", rET:(from number in Enumerable.Range(0, 3) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 3), mG:(1, 3), hPP:fHPP["Up1Gap"],
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((3, 0, -1), (-1, 0), 0, 0, 100), ((3, 0, -1), (1, 0), 0, 0, 100) }
                )) },

                { (4, 0, 0), new PlantElementTraits("MushroomStem", rET:(from number in Enumerable.Range(0, 4) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(3, 0), mG:(2, 4), hPP:fHPP["Up2Gap"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((4, 0, 1), (0, 0), 0, 0, (1, 1), 100) }
                )) },
                { (4, 1, 0), new PlantElementTraits("Mold",
                pGR:new PlantGrowthRules(t:(3, 2), mG:(50, 950), M:true
                )) },

                { (5, 0, 0), new PlantElementTraits("Vine", rET:(from number in Enumerable.Range(0, 15) select ((0, -number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 0), mG:(10, 50),
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((0, 0, 1), (0, 0), 0, 0, (3, 4), 100) },
                    lC:true,
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (2, 0), 100), ((-1, 0), (true, false, false), (2, 0), 100) },
                    lPM:true
                )) },
                { (5, 1, 0), new PlantElementTraits("ObsidianVine", rET:(from number in Enumerable.Range(0, 8) select ((0, -number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 3), mG:(6, 14),
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((0, 0, 1), (0, 0), 0, 0, (3, 3), 100) },
                    lC:true,
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (2, 0), 100), ((-1, 0), (true, false, false), (2, 0), 100) },
                    lPM:true
                )) },



                { (7, 0, 0), new PlantElementTraits("FleshVine", rET:(from number in Enumerable.Range(0, 5) select ((0, -number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(8, 0), mG:(4, 6),
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (2, 0), 100), ((-1, 0), (true, false, false), (2, 0), 100) },
                    lPM:true
                )) },
                { (7, 1, 0), new PlantElementTraits("FleshTendril", rET:(from number in Enumerable.Range(0, 5) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(8, 0), mG:(4, 6),
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (2, 0), 100), ((-1, 0), (true, false, false), (2, 0), 100) },
                    lPM:true
                )) },
                { (7, 2, 0), new PlantElementTraits("FleshTrunk1", rET:(from number in Enumerable.Range(0, 5) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(8, 0), mG:(2, 3),
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((7, 2, -1), (-1, 1), 1, 0, 100), ((7, 2, -1), (1, 1), 1, 0, 100) }
                )) },
                { (7, 3, 0), new PlantElementTraits("FleshTrunk2", rET:(from number in Enumerable.Range(0, 5) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(8, 0), mG:(2, 3),
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((7, 3, -1), (-1, 1), 1, 0, 100), ((7, 3, -1), (1, 1), 1, 0, 100) }
                )) },

                { (8, 0, 0), new PlantElementTraits("BoneStalactite", rET:(from number in Enumerable.Range(0, 4) select ((0, -number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(8, 1), mG:(2, 4), hPP:fHPP["Down2Gap"],
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((8, 0, -1), (-1, 0), 0, 0, 100), ((8, 0, -1), (1, 0), 0, 0, 100) }
                )) },
                { (8, 1, 0), new PlantElementTraits("BoneStalagmite", rET:(from number in Enumerable.Range(0, 4) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(8, 1), mG:(2, 4), hPP:fHPP["Up2Gap"],
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((8, 0, -1), (-1, 0), 0, 0, 100), ((8, 0, -1), (1, 0), 0, 0, 100) }
                )) },

                { (9, 0, 0), new PlantElementTraits("HairBody", iC:true,
                pGR:new PlantGrowthRules(t:(8, 2), mG:(8, 22),
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, true, false), (1, 0), 66), ((-1, 0), (true, true, false), (1, 0), 34) },
                    lDG:true, rDG:true, dGO:1
                )) },{ (9, 1, 0), new PlantElementTraits("HairLong", iC:true,
                pGR:new PlantGrowthRules(t:(8, 2), mG:(12, 88), gSVF:(0.75f, 2),
                    PM:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (1, 1), 100), ((-1, 0), (true, false, false), (3, 2), 100), ((-1, 0), (true, false, false), (1, 1), 100), ((1, 0), (true, false, false), (3, 2), 100) },
                    lPM:true
                )) },



                { (20, 0, 0), new PlantElementTraits("LanternTreeTrunk", rET:(from number in Enumerable.Range(0, 25) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(11, 0), mG:(20, 30), hPP:fHPP["Leaves"],
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((20, 0, 1), (0, 0), 0, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((20, 0, -1), (0, 0), 0, 0, (2, 5), 100) },
                    lC:true
                )) },
                { (20, 1, 0), new PlantElementTraits("LanternVine", rET:(from number in Enumerable.Range(0, 10) select ((0, -number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(11, 0), mG:(3, 5), mTC:true,
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((20, 1, -1), (-1, -1), 1, 0, 100), ((20, 1, -1), (1, -1), 1, 0, 100) }
                )) },
                { (20, 2, 0), new PlantElementTraits("SideLantern", rET:(from number in Enumerable.Range(0, 5) select ((0, number - 2), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(11, 0), mG:(8, 10), oMGV:true,
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((20, 0, 1), (-1, -1), 1, 0, 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 1), (true, true, false), (2, 2), 100), ((0, 1), (true, true, false), (1, 1), 100) }
                )) },

                { (21, 0, 0), new PlantElementTraits("WaxStem", rET:(from number in Enumerable.Range(0, 6) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(12, 0), mG:(2, 4), hPP:fHPP["Fire"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((21, 0, 1), (0, 0), 0, 0, (1, 0), 100) }
                )) },
                { (21, 1, 0), new PlantElementTraits("ChandelierStem", rET:(from number in Enumerable.Range(0, 10) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(11, 0), mG:(2, 2), hPP:fHPP["Fire"],
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((21, 1, 1), (0, 0), 0, 0, 100) },
                    lC:true
                )) },
                { (21, 2, 0), new PlantElementTraits("CandelabrumTrunk", rET:(from number in Enumerable.Range(0, 25) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(11, 0), mG:(20, 30), hPP:fHPP["Fire"],
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((21, 1, 1), (0, 0), 0, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((21, 2, -1), (0, 0), 0, 0, (2, 5), 100) },
                    lC:true
                )) },
                







                // Branches     subSubType -> -x (like (1, 4, _-2_))

                { (1, 0, -1), new PlantElementTraits("BaseBranch",
                pGR:new PlantGrowthRules(t:(1, 1), mG:(7, 12), sD:((1, 1), (true, false, true)), hPP:fHPP["Leaves"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 0, 1), (0, 0), 0, 0, (1, 1), 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, 1), (true, false, true), (2, 2), 100) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, true), (4, 4), 100) },
                    lPM:true
                )) },

                { (3, 0, -1), new PlantElementTraits("ObsidianBranch",
                pGR:new PlantGrowthRules(t:(1, 3), mG:(2, 1), hPP:fHPP["Up1Gap"]
                )) },

                { (7, 2, -1), new PlantElementTraits("FleshBranch1-1",
                pGR:new PlantGrowthRules(t:(8, 0), mG:(7, 10),
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((7, 2, -2), (0, 0), 2, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((7, 2, -2), (0, 0), 2, 0.5f, (2, 1), 100) },
                    lC:true,
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, 1), (false, false, false), (2, 4), 100) },
                    lPM:true,
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (1, 1), 100) }
                )) },
                { (7, 2, -2), new PlantElementTraits("FleshBranch1-2", rET:new ((int x, int y) pos, (bool x, bool y) baseDirectionFlip)[]{ ((1, 1), (true, false)), ((1, 2), (true, false)), ((1, 3), (true, false)), ((1, 4), (true, false)), ((1, 5), (true, false)), ((1, 6), (true, false)), ((1, 7), (true, false)), ((1, 8), (true, false)), ((1, 9), (true, false)), },
                pGR:new PlantGrowthRules(t:(8, 0), mG:(6, 4), sD:((0, 1), (false, false, false)), hPP:fHPP["Up1Gap"],
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, 1), (true, false, false), (2, 0), 100) }
                )) },

                { (7, 3, -1), new PlantElementTraits("FleshBranch2-1",
                pGR:new PlantGrowthRules(t:(8, 0), mG:(11, 10),
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((7, 3, -2), (0, 0), 2, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((7, 3, -2), (0, 0), 2, 0, (2, 1), 100) },
                    cO:8, lC:true,
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (3, 2), 100),       ((0, 1), (true, false, false), (5, 2), 100),   ((0, -1), (true, false, false), (4, 2), 100), ((0, -1), (true, false, false), (2, 2), 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, 1), (true, false, false), (1, 0), 100), ((1, 1), (true, false, false), (5, 2), 100), ((1, 0), (true, false, false), (1, 1), 100) }
                )) },
                { (7, 3, -2), new PlantElementTraits("FleshBranch2-2", rET:new ((int x, int y) pos, (bool x, bool y) baseDirectionFlip)[]{ ((1, -1), (true, false)), ((1, -2), (true, false)) },
                pGR:new PlantGrowthRules(t:(8, 0), mG:(5, 3), sD:((0, -1), (false, false, false)), hPP:fHPP["Down1Gap"],
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, -1), (true, false, false), (2, 0), 100) }
                )) },

                { (8, 0, -1), new PlantElementTraits("BoneBranch",
                pGR:new PlantGrowthRules(t:(8, 1), mG:(1, 0)
                )) },

                { (20, 0, -1), new PlantElementTraits("LanternTreeBranch",
                pGR:new PlantGrowthRules(t:(11, 0), mG:(8, 8), sD:((1, 0), (true, false, true)), hPP:fHPP["Leaves"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((20, 0, 1), (0, 0), 0, 0, (1, 1), 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 1), (true, false, false), (3, 7), 100), ((0, 1), (false, false, false), (1, 1), 100) }
                )) },
                { (20, 1, -1), new PlantElementTraits("LanternVineBranch",
                pGR:new PlantGrowthRules(t:(11, 0), mG:(10, 3), hPP:fHPP["Leaves"],
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((20, 0, 1), (0, 0), 2, 0, 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (2, 1), 100), ((1, 0), (true, false, false), (1, 0), 100), ((1, 1), (true, false, false), (1, 0), 100), ((1, 0), (true, false, false), (1, 0), 100), ((1, -1), (true, false, false), (1, 1), 100), ((0, -1), (true, false, false), (2, 0), 100) }
                )) },

                { (21, 0, -1), new PlantElementTraits("WaxFruitShit", stick:((0, 1), (false, false)),    // Same as WaxStem except that it sticks (so acts as a fruit/branch ??)
                pGR:new PlantGrowthRules(t:(12, 0), mG:(2, 4),
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((21, 0, 1), (0, 0), 0, 0, (1, 0), 100) }
                )) },
                { (21, 2, -1), new PlantElementTraits("CandelabrumBranch",
                pGR:new PlantGrowthRules(t:(11, 0), mG:(8, 8), sD:((1, 0), (true, false, true)), hPP:fHPP["Fire"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((21, 1, 1), (0, 0), 0, 0, (1, 1), 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 1), (true, false, false), (3, 7), 100), ((0, 1), (false, false, false), (1, 1), 100) }
                )) },







                // Flowers and shit (generation based on fixed frames)   subSubType -> +x (like (1, 4, _3_))
                
                //(0, 0, x) are elements shared between many plants. Since (0, 0, 0) is just normal grass it has no flowers and branches
                { (0, 0, 1), new PlantElementTraits("PlusFlower",
                framez:makeStructureFrameArray(null, "PlusFlower1", "PlusFlower2", "PlusFlower3")
                ) },
                { (0, 0, 2), new PlantElementTraits("CrossFlower",
                framez:makeStructureFrameArray(null, "CrossFlower1", "CrossFlower2", "CrossFlower3")
                ) },

                { (0, 1, 1), new PlantElementTraits("TulipFlower", stick:((0, 1), (false, false)),
                framez:makeStructureFrameArray(null, "TulipFlower1", "TulipFlower2", "TulipFlower3", "TulipFlower4", "TulipFlower5")
                ) },
                { (0, 2, 1), new PlantElementTraits("AlliumFlower", stick:((0, 1), (false, false)),
                framez:makeStructureFrameArray(null, "AlliumFlower1", "AlliumFlower2", "AlliumFlower3", "AlliumFlower4")
                ) },
                { (0, 3, 1), new PlantElementTraits("BigFlower", stick:((0, 1), (false, false)),
                framez:makeStructureFrameArray(null, "BigFlower1", "BigFlower2", "BigFlower3", "BigFlower4", "BigFlower5")
                ) },

                { (1, 0, 1), new PlantElementTraits("TreeLeaves", stick:((0, 0), (false, false)),
                framez:makeStructureFrameArray(null, "TreeLeaves1", "TreeLeaves2", "TreeLeaves3", "TreeLeaves4")
                ) },

                { (2, 2, 1), new PlantElementTraits("ReedFlower", stick:((0, 1), (false, false)),
                framez:makeStructureFrameArray(null, "ReedFlower1", "ReedFlower2", "ReedFlower3")
                ) },

                { (4, 0, 1), new PlantElementTraits("MushroomCap", stick:((0, 1), (false, false)),
                framez:makeStructureFrameArray(new (int value, int range)[]{ (1, 0), (2, 3), (1, 3) }, "MushroomCap1", "MushroomCap2", "MushroomCap3")
                ) },

                { (20, 0, 1), new PlantElementTraits("Lantern", stick:((0, 0), (false, false)), lE:(11, 1), lR:13,
                framez:makeStructureFrameArray(null, "Lantern1", "Lantern2", "Lantern3", "Lantern4", "Lantern5")
                ) },

                { (21, 0, 1), new PlantElementTraits("CandleFlower", stick:((0, 1), (false, false)), fLAP:true, anm:fireAnimation, lE:(11, 1), lR:9) },
                { (21, 1, 1), new PlantElementTraits("CandleHolder", stick:((0, 1), (false, false)), dC:((21, 0, -1), (0, 0), 100),  // dC is WaxFruitShit
                framez:makeStructureFrameArray(null, "CandleHolder1", "CandleHolder2")
                ) },
            };
        }














        public class PlantTraits
        {
            public string name;

            public bool isTree;
            public bool isCeiling;
            public bool isSide;
            public bool isEveryAttach;
            public bool isWater;
            public bool isLuminous;
            public bool isClimbable;

            public (int type, int subType, int subSubType) plantElementType;
            public (int type, int subType)? initFailType;

            public (int type, int subType)? soilType;
            public int minGrowthForValidity;

            public ((int type, int subType) type, ColorRange colorRange)[] colorOverrideArray;
            public PlantTraits(string namee, (int type, int subType, int subSubType)? t = null, (int type, int subType)? iFT = null, (int type, int subType)? sT = null, int mGFV = 1,
                ((int type, int subType) type, ColorRange colorRange)[] cOverride = null,
                bool T = false, bool C = false, bool S = false, bool EA = false, bool W = false, bool lum = false, bool cl = false)
            {
                name = namee;
                isTree = T;
                isCeiling = C;
                isSide = S;
                isEveryAttach = EA;
                isWater = W;
                soilType = sT;
                isLuminous = lum;
                isClimbable  = cl;
                colorOverrideArray = cOverride;
                minGrowthForValidity = mGFV;
                plantElementType = t ?? (-1, 0, 0);
                initFailType = iFT;
            }
        }

        public static Dictionary<(int type, int subType), PlantTraits> plantTraitsDict;
        public static void makePlantTraitsDict()
        {
            plantTraitsDict = new Dictionary<(int type, int subType), PlantTraits>()
            {
                { (-1, 0), new PlantTraits("Error",
                t:(-1, 0, 0)) },

                { (0, 0), new PlantTraits("BasePlant",
                t:(0, 0, 0)) },
                { (0, 1), new PlantTraits("Tulip",
                t:(0, 1, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((2, 0), new ColorRange((220, 0, 30), (110, -50, 30), (130, 50, 30))) }) },
                { (0, 2), new PlantTraits("Allium",
                t:(0, 2, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((2, 0), new ColorRange((140, 0, 30), (80, 50, 30), (220, 0, 30))) }) },
                { (0, 3), new PlantTraits("BigFlower",
                t:(0, 3, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((2, 0), new ColorRange((170, 0, 30), (80, 50, 30), (190, 0, 30))) }) },

                { (1, 0), new PlantTraits("Tree",                                   T:true,
                t:(1, 0, 0)) },

                { (2, 0), new PlantTraits("KelpUpwards",                                    W:true,
                t:(2, 0, 0), iFT:(2, 2)) },
                { (2, 1), new PlantTraits("KelpDownwards",                          C:true, W:true,
                t:(2, 1, 0)) },
                { (2, 2), new PlantTraits("Reed",                                   W:true,
                t:(2, 2, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((2, 0), new ColorRange((120, 30, 30), (40, 0, 20), (20, -10, 10))), ((2, 1), new ColorRange((235, 0, 10), (225, 5, 10), (190, 15, 10))) }) },

                { (3, 0), new PlantTraits("ObsidianPlant",
                t:(3, 0, 0)) },

                { (4, 0), new PlantTraits("Mushroom",
                t:(4, 0, 0)) },
                { (4, 1), new PlantTraits("Mold",
                t:(4, 1, 0)) },

                { (5, 0), new PlantTraits("Vine",                                   C:true,
                t:(5, 0, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), new ColorRange((50, 0, 30), (120, 50, 30), (50, 0, 30))) } ) },
                { (5, 1), new PlantTraits("ObsidianVine",                           C:true,
                t:(5, 1, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((2, 0), famousColorRanges["Obsidian"]), ((2, 1), famousColorRanges["ObsidianPollen"]) } ) },

                { (7, 0), new PlantTraits("FleshVine",                              C:true,
                t:(7, 0, 0), mGFV:4, sT:(4, 0)) },
                { (7, 1), new PlantTraits("FleshTendril",
                t:(7, 1, 0), mGFV:4, sT:(4, 0)) },
                { (7, 2), new PlantTraits("FleshTree1",                             T:true,
                t:(7, 2, 0), mGFV:1, sT:(4, 0)) },
                { (7, 3), new PlantTraits("FleshTree2",                             T:true,
                t:(7, 3, 0), mGFV:1, sT:(4, 0)) },

                { (8, 0), new PlantTraits("BoneStalactite",                         C:true,
                t:(8, 0, 0), mGFV:4, sT:(4, 1)) },
                { (8, 1), new PlantTraits("BoneStalagmite",
                t:(8, 1, 0), mGFV:4, sT:(4, 1)) },

                { (9, 0), new PlantTraits("Body Hair",                              EA:true, cl:true,
                t:(9, 0, 0), mGFV:4, sT:(4, 0)) },
                { (9, 1), new PlantTraits("Long Hair",                              C:true, cl:true,
                t:(9, 1, 0), mGFV:4, sT:(4, 0)) },

                { (20, 0), new PlantTraits("LanternTree",                           T:true, lum:true,
                t:(20, 0, 0)) },
                { (20, 1), new PlantTraits("LanternVine",                           C:true, lum:true,
                t:(20, 1, 0)) },
                { (20, 2), new PlantTraits("SideLantern",                           S:true, lum:true,
                t:(20, 2, 0)) },
                { (21, 0), new PlantTraits("Candle",                                        lum:true,
                t:(21, 0, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((11, 1), new ColorRange((200, 0, 10), (120, 0, 10), (40, 0, 10))) }) },
                { (21, 1), new PlantTraits("Chandelier",                                    lum:true,
                t:(21, 1, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((11, 1), new ColorRange((200, 0, 10), (120, 0, 10), (40, 0, 10))) }) },
                { (21, 2), new PlantTraits("Candelabrum",                           T:true, lum:true,
                t:(21, 2, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((11, 1), new ColorRange((200, 0, 10), (120, 0, 10), (40, 0, 10))) }) },
            };
        }







        public class TileTransitionTraits
        {
            public (int type, int subType) tileType;

            public int transitionRules;
            public bool meanBasedValueRequired;

            public int baseThreshold;

            public (int threshold, bool reverse)? temperature;
            public (int threshold, bool reverse)? humidity;
            public (int threshold, bool reverse)? acidity;
            public (int threshold, bool reverse)? toxicity;
            public int biomeValuesScale;
            public TileTransitionTraits((int type, int subType) tT, int tR, bool mBVR = false, int bT = 512, (int threshold, bool reverse)? T = null, (int threshold, bool reverse)? H = null, (int threshold, bool reverse)? A = null, (int threshold, bool reverse)? TX = null, int bVS = 512)
            {
                tileType = tT;

                transitionRules = tR;
                meanBasedValueRequired = mBVR;

                baseThreshold = bT;

                temperature = T;
                humidity = H;
                acidity = A;
                toxicity = TX;
                biomeValuesScale = bVS;
            }
        }
        
        public static Dictionary<string, TileTransitionTraits> famousTTT = new Dictionary<string, TileTransitionTraits>
        {
            { "HardRock", new TileTransitionTraits((1, 1), 0, mBVR:true, bT:0) },    // This one is particular but uuuuuuuuuhHHHHHHHHHH
            { "Bone", new TileTransitionTraits((4, 1), 1, bT:512, H:(500, false), bVS:1024) },
            { "Mold", new TileTransitionTraits((5, 0), 2, bT:1024, T:(500, false), H:(500, true), A:(500, true), bVS:1024) },
        };




        public class BiomeTraits        // -> Additional spawn attempts ? Like for modding idfk, on top of existing ones... idk uirehqdmsoijq
        {
            public (int type, int subType) type;
            public string name;
            public int difficulty = 1;
            public (int r, int g, int b) color;

            public (int type, int subType) fillType;
            public (int type, int subType) tileType;
            public (int type, int subType) lakeType;

            public (int one, int two) caveType;
            public (int one, int two) textureType;
            public int separatorType;
            public int antiSeparatorType;
            public float caveWidth;

            public TileTransitionTraits[] tileTransitionTraitsArray;

            public bool isDark;
            public bool isSlimy;
            public bool isDegraded;

            public float entityBaseSpawnRate;
            public float entityGroundSpawnRate;
            public float entityWaterSpawnRate;
            public float entityJesusSpawnRate;

            public float plantEveryAttachSpawnRate;
            public float plantGroundSpawnRate;
            public float plantTreeSpawnRate;
            public float plantCeilingSpawnRate;
            public float plantSideSpawnRate;
            public float plantWaterGroundSpawnRate;
            public float plantWaterCeilingSpawnRate;
            public float plantWaterSideSpawnRate;

            public ((int type, int subType) type, float percentage)[] entityBaseSpawnTypes;
            public ((int type, int subType) type, float percentage)[] entityGroundSpawnTypes;
            public ((int type, int subType) type, float percentage)[] entityWaterSpawnTypes;
            public ((int type, int subType) type, float percentage)[] entityJesusSpawnTypes;

            public ((int type, int subType) type, float percentage)[] plantEveryAttachSpawnTypes;
            public ((int type, int subType) type, float percentage)[] plantGroundSpawnTypes;
            public ((int type, int subType) type, float percentage)[] plantTreeSpawnTypes;
            public ((int type, int subType) type, float percentage)[] plantCeilingSpawnTypes;
            public ((int type, int subType) type, float percentage)[] plantSideSpawnTypes;
            public ((int type, int subType) type, float percentage)[] plantWaterGroundSpawnTypes;
            public ((int type, int subType) type, float percentage)[] plantWaterCeilingSpawnTypes;
            public ((int type, int subType) type, float percentage)[] plantWaterSideSpawnTypes;

            public BiomeTraits(string namee, (int r, int g, int b) colorToPut, float[] spawnRates, ((int type, int subType) type, float percentage)[] entityTypes, ((int type, int subType) type, float percentage)[] plantTypes,
                (int one, int two)? cT = null, (int one, int two)? txT = null, int sT = 0, int aST = 0, float cW = 1, TileTransitionTraits[] tTT = null,
                (int type, int subType)? fT = null, (int type, int subType)? tT = null, (int type, int subType)? lT = null,
                bool S = false, bool Dg = false, bool Da = false)
            {
                name = namee;
                color = colorToPut;

                isDark = Da;
                isSlimy = S;
                isDegraded = Dg;

                fillType = fT ?? (0, 0);
                tileType = tT ?? (1, 0);
                lakeType = lT ?? (-2, 0);

                caveType = cT ?? (1, 2);
                textureType = txT ?? (0, 1);
                separatorType = sT;
                antiSeparatorType = aST;
                caveWidth = cW;

                tileTransitionTraitsArray = tTT;

                EntityTraits entityTraits;
                List<((int type, int subType) type, float percentage)> entityBaseSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> entityGroundSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> entityWaterSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> entityJesusSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                foreach (((int type, int subType) type, float percentage) tupelo in entityTypes)
                {
                    if (!entityTraitsDict.ContainsKey(tupelo.type)) { continue; }
                    entityTraits = entityTraitsDict[tupelo.type];
                    if (entityTraits.isJesus) { entityJesusSpawnTypesList.Add(tupelo); }
                    else if (entityTraits.isDigging) { entityGroundSpawnTypesList.Add(tupelo); }
                    else if (entityTraits.isSwimming) { entityWaterSpawnTypesList.Add(tupelo); }
                    else { entityBaseSpawnTypesList.Add(tupelo); }
                }
                entityBaseSpawnTypes = entityBaseSpawnTypesList.ToArray();
                entityGroundSpawnTypes = entityGroundSpawnTypesList.ToArray();
                entityWaterSpawnTypes = entityWaterSpawnTypesList.ToArray();
                entityJesusSpawnTypes = entityJesusSpawnTypesList.ToArray();

                PlantTraits plantTraits;
                List<((int type, int subType) type, float percentage)> plantEveryAttachSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> plantGroundSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> plantTreeSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> plantCeilingSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> plantSideSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> plantWaterGroundSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> plantWaterCeilingSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> plantWaterSideSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                foreach (((int type, int subType) type, float percentage) tupelo in plantTypes)
                {
                    if (!plantTraitsDict.ContainsKey(tupelo.type)) { continue; }
                    plantTraits = plantTraitsDict[tupelo.type];
                    if (plantTraits.isEveryAttach) { plantEveryAttachSpawnTypesList.Add(tupelo); }
                    else if (plantTraits.isTree) { plantTreeSpawnTypesList.Add(tupelo); }
                    else if (plantTraits.isWater)
                    {
                        if (plantTraits.isCeiling) { plantWaterCeilingSpawnTypesList.Add(tupelo); }
                        else if (plantTraits.isSide) { plantWaterSideSpawnTypesList.Add(tupelo); }
                        else { plantWaterGroundSpawnTypesList.Add(tupelo); }
                    }
                    else
                    {
                        if (plantTraits.isCeiling) { plantCeilingSpawnTypesList.Add(tupelo); }
                        else if (plantTraits.isSide) { plantSideSpawnTypesList.Add(tupelo); }
                        else { plantGroundSpawnTypesList.Add(tupelo); }
                    }
                }
                plantEveryAttachSpawnTypes  = plantEveryAttachSpawnTypesList.ToArray();
                plantGroundSpawnTypes  = plantGroundSpawnTypesList.ToArray();
                plantTreeSpawnTypes = plantTreeSpawnTypesList.ToArray();
                plantCeilingSpawnTypes = plantCeilingSpawnTypesList.ToArray();
                plantSideSpawnTypes = plantSideSpawnTypesList.ToArray();
                plantWaterGroundSpawnTypes = plantWaterGroundSpawnTypesList.ToArray();
                plantWaterCeilingSpawnTypes = plantWaterCeilingSpawnTypesList.ToArray();
                plantWaterSideSpawnTypes = plantWaterSideSpawnTypesList.ToArray();


                entityBaseSpawnRate = entityBaseSpawnTypes.Length == 0 ? 0 : spawnRates[0];
                entityGroundSpawnRate = entityGroundSpawnTypes.Length == 0 ? 0 : spawnRates[1];
                entityWaterSpawnRate = entityWaterSpawnTypes.Length == 0 ? 0 : spawnRates[2];
                entityJesusSpawnRate = entityJesusSpawnTypes.Length == 0 ? 0 : spawnRates[3];

                plantEveryAttachSpawnRate = plantEveryAttachSpawnTypes.Length == 0 ? 0 : spawnRates[4];
                plantGroundSpawnRate = plantGroundSpawnTypes.Length == 0 ? 0 : spawnRates[5];
                plantTreeSpawnRate = plantTreeSpawnTypes.Length == 0 ? 0 : spawnRates[6];
                plantCeilingSpawnRate = plantCeilingSpawnTypes.Length == 0 ? 0 : spawnRates[7];
                plantSideSpawnRate = plantSideSpawnTypes.Length == 0 ? 0 : spawnRates[8];
                plantWaterGroundSpawnRate = plantWaterGroundSpawnTypes.Length == 0 ? 0 : spawnRates[9];
                plantWaterCeilingSpawnRate = plantWaterCeilingSpawnTypes.Length == 0 ? 0 : spawnRates[10];
                plantWaterSideSpawnRate = plantWaterSideSpawnTypes.Length == 0 ? 0 : spawnRates[11];
            }
            public void setType((int type, int subType) typeToSet) { type = typeToSet; }
        }
        public static BiomeTraits getBiomeTraits((int type, int subType) biomeType) { return biomeTraitsDict.ContainsKey(biomeType) ? biomeTraitsDict[biomeType] : biomeTraitsDict[(-1, 0)]; }

        public static Dictionary<(int type, int subType), BiomeTraits> biomeTraitsDict;
        public static void makeBiomeTraitsDict()
        {
            biomeTraitsDict = new Dictionary<(int type, int subType), BiomeTraits>()
            {   //      -E- C  G  W  J      -P- G  T  C  S  WG WC WS  
                { (-1, 0), new BiomeTraits("Error",                 (1200, -100, 1200),
                new float[]{0, 0, 0, 0,         0, 0, 0, 0, 0, 0, 0, 0},
                new ((int type, int subType) type, float percentage)[]{ },
                new ((int type, int subType) type, float percentage)[]{ }
                ) },

                { (0, 0),  new BiomeTraits("Cold",                  (Color.Blue.R, Color.Blue.G, Color.Blue.B),     // -> put smaller spawn rates for this one ? Since cold. And nothing for frost
                new float[]{1, 0.25f, 2, 2,     0, 4, 1, 2, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), }
                ) },                                                 // Base           Vine           Kelp           CeilingKelp
                { (0, 1),  new BiomeTraits("Frost",                 (Color.LightBlue.R, Color.LightBlue.G, Color.LightBlue.B),
                new float[]{1, 0.25f, 2, 2,     0, 4, 1, 2, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((0, 2), 100), },
                new ((int type, int subType) type, float percentage)[]{ },
                lT:(-2, -1)) },                                      // Nothing lol
                { (1, 0),  new BiomeTraits("Acid",                  (Color.Fuchsia.R, Color.Fuchsia.G, Color.Fuchsia.B),
                new float[]{1, 0.25f, 2, 2,     0, 4, 1, 2, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((4, 0), 100), ((2, 0), 100),  ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                Dg:true) },                                          // Base           Vine           Kelp           CeilingKelp

                { (2, 0),  new BiomeTraits("Hot",                   (Color.OrangeRed.R, Color.OrangeRed.G, Color.OrangeRed.B),
                new float[]{1, 0.25f, 2, 2,     0, 4, 1, 2, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((2, 0), 100), ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                lT:(-4, 0)) },                                        // Base           Vine           Kelp           CeilingKelp
                { (2, 1),  new BiomeTraits("Lava Ocean",            (Color.OrangeRed.R + 90, Color.OrangeRed.G + 30, Color.OrangeRed.B),
                new float[]{1, 0.25f, 2, 2,     0, 4, 1, 2, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((2, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                cT:(0, 3), txT:(0, 0), sT:1, fT:(-4, 0)) },          // Base           Vine           Kelp           CeilingKelp
                { (2, 2),  new BiomeTraits("Obsidian",              (-100, -100, -100),
                new float[]{1, 0.25f, 2, 2,     0, 4, 1, 2, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((0, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((3, 0), 100), ((5, 1), 100), ((2, 0), 100), ((2, 1), 100), },
                cT:(1, 4), txT:(0, 0), cW:0.3f) },                   // ObsidianPlant  Vine           Kelp           CeilingKelp

                { (3, 0),  new BiomeTraits("Forest",                (Color.Green.R, Color.Green.G, Color.Green.B),       // finish forest flowers shite
                new float[]{1, 0.25f, 2, 2,     0, 6, 1, 2, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((1, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                cT:(1, 5), txT:(0, 0)) },                            // Base           Tree           Vine           Kelp           CeilingKelp
                { (3, 1),  new BiomeTraits("Flower Forest",         (Color.Green.R, Color.Green.G + 40, Color.Green.B + 80),
                new float[]{1, 0.25f, 2, 2,    0, 16, 1, 3, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 10), ((0, 1), 20), ((0, 2), 20), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                txT:(0, 0)) },                                       // Base          Tulip         Allium        Vine           Kelp           CeilingKelp

                { (4, 0),  new BiomeTraits("Toxic",                 (Color.GreenYellow.R, Color.GreenYellow.G, Color.GreenYellow.B),
                new float[]{1, 0.25f, 2, 2,     0, 4, 1, 2, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                txT:(0, 0), S:true) },                                           // Base           Vine           Kelp           CeilingKelp

                { (5, 0),  new BiomeTraits("Fairy",                 (Color.LightPink.R, Color.LightPink.G, Color.LightPink.B),
                new float[]{1, 0.25f, 2, 2,     0, 4, 1, 2, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((4, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                lT:(-3, 0)) },                                       // Mushroom       Vine           Kelp           CeilingKelp

                { (6, 0),  new BiomeTraits("Mold",                  (Color.DarkBlue.R, Color.DarkBlue.G + 20, Color.DarkBlue.B + 40),
                new float[]{1, 0.25f, 2, 2,     4, 1, 2, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((4, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((4, 1), 100), },
                txT:(0, 0), tTT:new TileTransitionTraits[]{ famousTTT["Mold"] }) }, // Mold

                { (8, 0),  new BiomeTraits("Ocean",                 (Color.LightBlue.R, Color.LightBlue.G + 60, Color.LightBlue.B + 130),
                new float[]{1, 0.25f, 3, 6,     0, 4, 1, 2, 0, 8, 8, 0},
                new ((int type, int subType) type, float percentage)[]{ ((2, 0), 95), ((8, 0), 4), ((9, 0), 1), ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((2, 0), 100), ((2, 1), 100), },
                cT:(0, 3), txT:(0, 0), fT:(-2, 0), sT:1) },          // Kelp           CeilingKelp
                { (8, 1),  new BiomeTraits("Frozen Ocean",          (Color.LightBlue.R + 60, Color.LightBlue.G + 90, Color.LightBlue.B + 150),
                new float[]{1, 0.25f, 3, 6,     0, 4, 1, 2, 0, 8, 8, 0},
                new ((int type, int subType) type, float percentage)[]{ ((2, 0), 100), ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((2, 0), 100), ((2, 1), 100), },
                cT:(0, 3), txT:(0, 0), fT:(-2, -1), lT:(-2, -1), aST:1) }, // Kelp     CeilingKelp


                //      -E- C  G  W  J      -P- E  G  T  C  S  WG WC WS  
                { (9, 0),  new BiomeTraits("Lanterns",                (Color.Gray.R - 50, Color.Gray.G - 10, Color.Gray.B + 40),
                new float[]{1, 0.25f, 2, 1,     0, 4, 1, 2, 2, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((0, 2), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((20, 0), 100), ((20, 1), 100), ((20, 2), 100) },
                cT:(1, 5), txT:(0, 0), Da:true) },                   // LanternTree     LanternVine     LanternSide
                { (9, 1),  new BiomeTraits("MixedLuminous",           (Color.Gray.R, Color.Gray.G, Color.Gray.B),
                new float[]{1, 0.25f, 2, 1,     0, 4, 1, 1, 1, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((0, 2), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((21, 0), 50), ((21, 1), 50), ((20, 1), 100), ((20, 2), 100) },
                Da:true) },                                          // Candle         Chandelier     LanternVine      LanternSide
                { (9, 2),  new BiomeTraits("Chandeliers",             (Color.Gray.R + 50, Color.Gray.G + 10, Color.Gray.B - 40),
                new float[]{1, 0.25f, 2, 1,     0, 4, 1, 1, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((0, 2), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((21, 0), 50), ((21, 1), 50), ((21, 2), 100), },
                cT:(1, 5), txT:(0, 0), Da:true) },                   // Candle         Chandelier     Candelabrum 


                //      -E- C  G  W  J      -P- E  G  T  C  S  WG WC WS  
                { (10, 0), new BiomeTraits("Flesh",                 (Color.Red.R, Color.Red.G, Color.Red.B),
                new float[]{1, 1, 2, 1,         0, 4, 1, 4, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((1, 1), 100), ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((7, 0), 100), ((7, 1), 100) },
                lT:(-7, 0), tT:(4, 0)) },                            // Flesh Vine     Flesh Tendril
                { (10, 1), new BiomeTraits("FleshForest",           (Color.DarkRed.R + 20, Color.DarkRed.G - 20, Color.DarkRed.B - 20),
                new float[]{1, 1, 2, 1,         0, 3, 1, 3, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((1, 1), 100), ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((7, 0), 100), ((7, 1), 100), ((7, 2), 50), ((7, 3), 50) },
                cT:(1, 5), txT:(0, 0), lT:(-7, 0), tT:(4, 0)) },     // Flesh Vine     Flesh Tendril  Flesh Tree 1   Flesh Tree 2
                { (10, 2), new BiomeTraits("Flesh and Bone",        (Color.Pink.R, Color.Pink.G, Color.Pink.B),
                new float[]{1, 1, 2, 1,         0, 4, 1, 4, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((1, 1), 50),  ((1, 2), 50),  ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((7, 0), 75),  ((7, 1), 75),  ((8, 0), 25),  ((8, 1), 25) },
                lT:(-6, 0), tT:(4, 0),                               // Flesh Vine     Flesh Tendril  Bone Stalagmi  Bone Stalactite
                tTT:new TileTransitionTraits[] { famousTTT["Bone"] }) },
                { (10, 3), new BiomeTraits("Body Hair Forest",      (Color.DarkRed.R - 20, Color.DarkRed.G - 50, Color.DarkRed.B - 70),
                new float[]{1, 1, 2, 1,        10, 4, 1, 4, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((7, 0), 100),  ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((9, 0), 100) },
                cT:(1, 0), lT:(-6, 0), tT:(4, 0), cW:2.5f) },        // Body Hair
                { (10, 4), new BiomeTraits("Long Hair Forest",      (Color.DarkRed.R - 20, Color.DarkRed.G - 50, Color.DarkRed.B - 70),
                new float[]{1, 1, 2, 1,         0, 4, 1, 10, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((7, 0), 100),  ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((9, 1), 100) },
                cT:(1, 0), lT:(-6, 0), tT:(4, 0), cW:2.5f) },        // Long Hair

                { (11, 0), new BiomeTraits("Bone",                  (Color.White.R, Color.White.G, Color.White.B),
                new float[]{1, 1, 2, 1,         0, 1, 1, 1, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((1, 2), 100), ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((8, 0), 100), ((8, 1), 100) },
                lT:(-6, 0), tT:(4, 1)) },                            // Bone Stalagmi  Bone Stalactite

                { (12, 0), new BiomeTraits("Blood Ocean",           (Color.DarkRed.R, Color.DarkRed.G, Color.DarkRed.B),
                new float[]{1, 1, 2, 1,         0, 4, 1, 2, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ },
                cT:(0, 3), txT:(0, 0), sT:1, tT:(4, 0), fT:(-6, 0),
                tTT:new TileTransitionTraits[]{ famousTTT["Bone"] }) },
                { (12, 1), new BiomeTraits("Acid Ocean",            (Color.YellowGreen.R, Color.YellowGreen.G, Color.YellowGreen.B),
                new float[]{1, 1, 1, 1,         0, 4, 1, 2, 0, 4, 4, 0},
                new ((int type, int subType) type, float percentage)[]{ ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ },
                cT:(0, 3), txT:(0, 0), sT:1, tT:(4, 0), fT:(-7, 0),
                tTT:new TileTransitionTraits[] { famousTTT["Bone"] }) },
            };

            foreach ((int type, int subType) typeToSet in biomeTraitsDict.Keys) { biomeTraitsDict[typeToSet].setType(typeToSet); }

            foreach ((int type, int subType) key in biomeTraitsDict.Keys)
            {
                BiomeTraits bT = biomeTraitsDict[key];
                if (bT.tileType == (1, 0))
                {
                    if (bT.tileTransitionTraitsArray is null) { bT.tileTransitionTraitsArray = new TileTransitionTraits[] { famousTTT["HardRock"] }; }
                    else
                    {
                        TileTransitionTraits[] tTTA = new TileTransitionTraits[bT.tileTransitionTraitsArray.Length + 1];
                        for (int i = 0; i < bT.tileTransitionTraitsArray.Length; i++) { tTTA[i + 1] = bT.tileTransitionTraitsArray[i]; }
                        tTTA[0] = famousTTT["HardRock"];
                        bT.tileTransitionTraitsArray = tTTA;
                    }
                }
            }
        }





        public class AttackTraits
        {
            public string name;
            public float damage;
            public float manaCost;
            public bool isHitting;
            public bool isTerrainDigging;
            public bool isPlantDigging;
            public bool isAbortable;
            public bool isEntityBound;
            public (int type, int subType)? targetMaterial;

            public (int v, int h, int s) r;
            public (int v, int h, int s) g;
            public (int v, int h, int s) b;
            public AttackTraits(string namee, float d = 0, float m = 0, bool H = false, bool T = false, bool P = false, bool A = false, bool B = false, (int type, int subType)? tM = null)
            {
                name = namee;
                damage = d;
                manaCost = m;
                isHitting = H;
                isTerrainDigging = T;
                isPlantDigging = P;
                isAbortable = A;
                isEntityBound = B;
                targetMaterial = tM;
            }
        }

        public static Dictionary<(int type, int subType, int subSubType, int megaType), AttackTraits> attackTraitsDict;
        public static void makeAttackTraitsDict()
        {
            attackTraitsDict = new Dictionary<(int type, int subType, int subSubType, int megaType), AttackTraits>()
            {
                { (-1, 0, 0, 0), new AttackTraits("Error"                                                                               ) },
                                                                                                                                    
                { (0, 0, 0, 4), new AttackTraits("Sword",               d:1,            H:true, B:true                                  ) },
                { (1, 0, 0, 4), new AttackTraits("Pickaxe",             d:0.5f,         H:true, B:true, T:true,         A:true          ) },
                { (2, 0, 0, 4), new AttackTraits("Scythe",              d:0.75f,        H:true, B:true,         P:true                  ) },
                { (4, 0, 0, 4), new AttackTraits("Axe",                 d:0.5f,         H:true, B:true,         P:true, A:true          ) },
                                                                                                                                    
                { (3, 0, 0, 4), new AttackTraits("Magic Wand",                  m:5,            B:true                                  ) },
                { (3, 0, 1, 4), new AttackTraits("Magic Bullet"                                                                         ) },
                { (3, 1, 0, 4), new AttackTraits("Carnal Wand",                 m:25,           B:true                                  ) },
                { (3, 1, 1, 4), new AttackTraits("Carnal Bullet",                       H:true                                          ) },
                { (3, 2, 0, 4), new AttackTraits("Floral Wand",                 m:100,          B:true                                  ) },
                { (3, 2, 1, 4), new AttackTraits("Floral Bullet"                                                                        ) },
                { (3, 2, 2, 4), new AttackTraits("Floral Bullet 2"                                                                      ) },
                { (3, 3, 0, 4), new AttackTraits("Teleport Wand",               m:10,           B:true                                  ) },
                { (3, 3, 1, 4), new AttackTraits("Teleport bullet"                                                                      ) },
                { (3, 4, 0, 4), new AttackTraits("Dig Wand",                    m:15,           B:true                                  ) },
                { (3, 4, 1, 4), new AttackTraits("Dig bullet",                                          T:true                          ) },
                { (3, 4, 2, 4), new AttackTraits("Dig bullet 2",                                        T:true                          ) },
                { (3, 4, 3, 4), new AttackTraits("Dig bullet 3",                                        T:true                          ) },

                { (6, 0, 0, 5), new AttackTraits("Goblin Hand",         d:0.25f,        H:true, B:true, T:true, P:true, A:true          ) },
                                                                                                                                    
                { (3, 0, 0, 5), new AttackTraits("Hornet Warning",      d:0.05f,        H:true, B:true                                  ) },
                { (3, 1, 0, 5), new AttackTraits("Hornet Mandibles",    d:1,            H:true, B:true, T:true, P:true,        tM:(2, 1)) },
                { (3, 2, 0, 5), new AttackTraits("Hornet Sting",        d:0.65f,        H:true, B:true                                  ) },
            };
        }
    }
}
