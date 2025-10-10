﻿using System;
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

namespace Cave
{
    public class Traits
    {
        public class ColorRange
        {
            public (int v, int h, int s) r;
            public (int v, int h, int s) g;
            public (int v, int h, int s) b;
            public ColorRange((int v, int h, int s) red, (int v, int h, int s) green, (int v, int h, int s) blue) { r = red; g = green; b = blue; }
        }
        public static Dictionary<string, ColorRange> famousColorRanges = new Dictionary<string, ColorRange>
        {
            { "Obsidian", new ColorRange((30, 0, 0), (30, 0, 0), (40, 0, 10)) },
            { "ObsidianPollen", new ColorRange((220, 0, 0), (220, 0, 0), (230, 0, 10)) },
            { "IceStem", new ColorRange((175, 10, 15), (175, 10, 15), (215, -10, 15)) },
            { "IcePetal", new ColorRange((195, 10, 10), (195, 10, 10), (235, -10, 10)) },
            { "IcePollen", new ColorRange((235, 10, 10), (230, 0, 10), (255, 0, 0)) },
        };
        public class TileTraits
        {
            public (int type, int subType) type;
            public string name;

            public bool isAir;
            public bool isLiquid;
            public bool isSolid;

            public bool isLava;         // H for hot
            public bool isAcidic;
            public bool isTransformant; // Fairy liquid
            public bool isSlippery;

            public bool isSterile;
            public bool ignoreTileFeatures;

            public int hardness;
            public int viscosity = 0;

            public ColorRange colorRange;
            public float biomeColorBlend;
            public bool isTextured;     // For mold and salt crystals
            public bool isTransparent;
            public TileTraits(string namee, ColorRange cR = null, bool Air = false, bool Liq = false, bool L = false, bool A = false, bool T = false, bool S = false, bool St = false, bool iTF = false, float bCB = 0.1f, bool Tex = false, bool Tr = false)
            {
                name = namee;

                isAir = Air;
                isLiquid = Liq;
                isSolid = !(isAir || isLiquid);

                isLava = L;
                isAcidic = A;
                isTransformant = T;
                isSlippery = S;

                isSterile = St;
                ignoreTileFeatures = iTF;

                colorRange = cR;
                biomeColorBlend = bCB;
                isTextured = Tex;
                isTransparent = Tr || isLiquid || isAir;
            }
            public void setType((int type, int subType) typeToSet) { type = typeToSet; }
        }

        public static Dictionary<(int type, int subType), TileTraits> tileTraitsDict;
        public static TileTraits getTileTraits((int type, int subType) tileType) { return tileTraitsDict.ContainsKey(tileType) ? tileTraitsDict[tileType] : tileTraitsDict[(0, 0)]; }
        public static void makeTileTraitsDict()
        {
            tileTraitsDict = new Dictionary<(int type, int subType), TileTraits>()
            {
                { (-8, 0), new TileTraits("Slime", bCB:0.2f,
                cR:new ColorRange((145, 0, 0), (175, 0, 0), (115, 0, 0)),         Liq:true                                  ) },

                { (-7, 0), new TileTraits("Acid", bCB:0.2f,
                cR:new ColorRange((120, 0, 0), (180, 0, 0), (60, 0, 0)),          Liq:true, A:true                          ) },

                { (-6, 0), new TileTraits("Blood", bCB:0.2f,
                cR:new ColorRange((100, 0, 0), (15, 0, 0), (25, 0, 0)),           Liq:true                                  ) },
                { (-6, 1), new TileTraits("Deoxygenated Blood", bCB:0.2f,
                cR:new ColorRange((65, 0, 0), (5, 0, 0), (35, 0, 0)),             Liq:true                                  ) },

                { (-5, 0), new TileTraits("Honey", bCB:0.2f,
                cR:new ColorRange((160, 0, 0), (120, 0, 0), (70, 0, 0)),          Liq:true                                  ) },

                { (-4, 0), new TileTraits("Lava", bCB:0.05f,
                cR:new ColorRange((255, 0, 0), (90, 0, 0), (0, 0, 0)),            Liq:true, L:true                          ) },

                { (-3, 0), new TileTraits("Fairy Liquid", bCB:0.2f,
                cR:new ColorRange((105, 0, 0), (80, 0, 0), (120, 0, 0)),          Liq:true, T:true                          ) },

                { (-2, -1), new TileTraits("Ice", bCB:0.2f,
                cR:new ColorRange((160, 0, 0), (160, 0, 0), (200, 0, 0)),          Tr:true, S:true, St:true, iTF:true       ) },
                { (-2, 0), new TileTraits("Water", bCB:0.2f,
                cR:new ColorRange((80, 0, 0), (80, 0, 0), (120, 0, 0)),           Liq:true                                  ) },
                { (-2, 2), new TileTraits("SaltyWater", bCB:0.2f,
                cR:new ColorRange((100, 0, 0), (100, 0, 0), (110, 0, 0)),         Liq:true                                  ) },

                { (-1, 0), new TileTraits("Piss", bCB:0.2f,
                cR:new ColorRange((120, 0, 0), (120, 0, 0), (80, 0, 0)),          Liq:true                                  ) },


                { (0, -3), new TileTraits("Airror", bCB:0.5f,
                cR:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0)),         Air:true                                  ) },
                { (0, -2), new TileTraits("Erroil", bCB:0.5f,
                cR:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0)),         Liq:true                                  ) },
                { (0, -1), new TileTraits("Errore", bCB:0.5f,
                cR:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                                    ) },
                { (0, 0), new TileTraits("Air", bCB:0.5f,
                cR:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0)),         Air:true                                  ) },


                { (1, 0), new TileTraits("Rock", bCB:0.5f,
                cR:new ColorRange((30, 0, 0), (30, 0, 0), (30, 0, 0))                                                       ) },
                { (1, 1), new TileTraits("Dense Rock", bCB:0.2f,
                cR:new ColorRange((10, 0, 0), (10, 0, 0), (10, 0, 0))                                                       ) },

                { (2, 0), new TileTraits("Dirt", bCB:0.5f,
                cR:new ColorRange((80, 0, 0), (60, 0, 0), (20, 0, 0))                                                       ) },

                { (3, 0), new TileTraits("Plant Matter", bCB:0.35f,
                cR:new ColorRange((10, 0, 0), (60, 0, 0), (30, 0, 0))                                                       ) },

                { (4, 0), new TileTraits("Flesh Tile", bCB:0.2f,
                cR:new ColorRange((135, 0, 0), (55, 0, 0), (55, 0, 0))                                                      ) },
                { (4, 1), new TileTraits("Bone Tile", bCB:0.2f,
                cR:new ColorRange((240, 0, 0), (230, 0, 0), (245, 0, 0))                                                    ) },
                { (4, 2), new TileTraits("Skin Tile", bCB:0.2f,
                cR:new ColorRange((200, 0, 0), (150, 0, 0), (130, 0, 0))                                                    ) },

                { (5, 0), new TileTraits("Mold Tile", bCB:0.1f,
                cR:new ColorRange((50, 0, 0), (50, 0, 0), (100, 0, 0)),           Tex:true                                  ) },

                { (6, 0), new TileTraits("Salt Tile", bCB:0.1f,
                cR:new ColorRange((170, 0, 0), (120, 0, 0), (140, 0, 0)),         Tex:true, Tr:true, St:true                ) },
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
                { (8, 3), new MaterialTraits("Skin",
                col:new ColorRange((240, 0, 0), (210, 0, 0), (190, 0, 0))                                               ) },

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

            // Cosmetic stuff
            public ColorRange colorRange;
            public int? lightRadius;
            public (int baseLength, int variation)? length;
            public float? tailAxisRigidity;
            public bool transparentTail;
            public (int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color)[] tailMap;
            public (int type, (int x, int y) pos, float period, float turningSpeed, (bool isVariation, (int a, int r, int g, int b) value) color)? wingTraits;

            // Collisions shit
            public ((int x, int y)[] up, (int x, int y)[] side, (int x, int y)[] down) collisionPoints;

            // Behavior Characteristics (automatically derived from Behaviors)
            public bool isFlying;
            public bool isSwimming;
            public bool isDigging;
            public bool isJesus;
            public bool isCliming;

            // Spawn conditions
            public bool spawnsInAir;
            public bool spawnsInLiquid;
            public bool spawnsInSolid;
            public bool forceSpawnOnSolid;
            public HashSet<(int type, int subType)> tilesItCanSpawnOn;

            // Behaviors
            public int inWaterBehavior;     // -> 0: nothing, 1: float upwards, 2: move randomly in water, 3:drift towards land
            public int onWaterBehavior;     // -> 0: nothing, 1: skip, 2: drift towards land
            public int inAirBehavior;       // -> 0: nothing, 1: fly randomly, 2: random drift
            public int onGroundBehavior;    // -> 0: nothing, 1: random jump, 2: move around, 3: dig down
            public int inGroundBehavior;    // -> 0: nothing, 1: random jump, 2: dig around, 3: teleport, 4: dig tile
            public int onPlantBehavior;     // -> 0: fallOut, 1: random movement

            // Characteristics
            public float swimSpeed;
            public float swimMaxSpeed;
            public float movementChance;
            public (float x, float y) jumpStrength;
            public float jumpChance;
            public int goHomeDistance;
            public HashSet<(int type, int subType)> diggableTiles;
            public EntityTraits(string namee, int hp, ((int type, int subType, int megaType) element, int count) drps,
                ColorRange colRange, int? lR = null, (int baseLength, int variation)? L = null, float? tAR = null, bool tT = false,
                (int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color)[] tM = null,
                ((bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color, (int segment, bool fromEnd, (int x, int y) pos)[] array)[] tM2 = null,
                (int type, (int x, int y) pos, float period, float turningSpeed, (bool isVariation, (int a, int r, int g, int b) value) color)? wT = null,
                ((int x, int y)[] up, (int x, int y)[] side, (int x, int y)[] down)? cP = null, HashSet<(int type, int subType)> tICSO = null,
                int iW = 0, int oW = 0, int iA = 0, int oG = 0, int iG = 0, int oP = 0,
                bool fIF = false, bool fIS = false, bool fID = false, bool fIJ = false, bool fIC = false,
                float sS = 0.1f, float sMS = 0.5f, (float x, float y)? jS = null, float jC = 0, float mC = 0, int gHD = 50,
                HashSet<(int type, int subType)> dT = null)
            {
                name = namee;
                startingHp = hp;
                drops = drps;

                colorRange = colRange;
                lightRadius = lR;
                length = L;
                tailAxisRigidity = tAR;
                transparentTail = tT;
                tailMap = tM;
                if (tM2 != null)
                {
                    List<(int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color)> newTailMap = new List<(int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color)>();
                    foreach (((bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color, (int segment, bool fromEnd, (int x, int y) pos)[] array) subArray in tM2)
                    {
                        foreach ((int segment, bool fromEnd, (int x, int y) pos) element in subArray.array) { newTailMap.Add((element.segment, element.fromEnd, false, 0, element.pos, subArray.color)); }
                    }
                    if (tailMap != null) { foreach ((int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color) element in tailMap) { newTailMap.Add(element); } }
                    tailMap = newTailMap.ToArray();
                }
                wingTraits = wT;

                if (cP is null) { collisionPoints = (doubleZeroArray, doubleZeroArray, doubleZeroArray); }
                else { collisionPoints = cP.Value; }

                isFlying = fIF || iA > 0 ? true : false;
                isSwimming = fIS || iW == 2 ? true : false;
                isDigging = fID || iG == 2 ? true : false;
                isJesus = fIJ || oW == 1 ? true : false;
                isCliming = fIC || oP != 0 ? true : false;

                spawnsInAir = isFlying;
                spawnsInLiquid = isSwimming;
                spawnsInSolid = isDigging;
                if (!(spawnsInLiquid || spawnsInSolid)) { spawnsInAir = true; }
                if (!isFlying && spawnsInAir && !isJesus && !isCliming) { forceSpawnOnSolid = true; }
                tilesItCanSpawnOn = tICSO;

                inWaterBehavior = iW;
                onWaterBehavior = oW;
                inAirBehavior = iA;
                inGroundBehavior = iG;
                onGroundBehavior = oG;
                onPlantBehavior = oP;

                swimSpeed = sS;
                swimMaxSpeed = sMS;

                movementChance = mC;

                jumpStrength = jS ?? (1, 1);
                jumpChance = jC;

                goHomeDistance = gHD;
                diggableTiles = dT ?? new HashSet<(int type, int subType)>();
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
                new ColorRange((130, 50, 30), (130, -50, 30), (210, 0, 30)), lR:7, wT:(0, (0, 0), 0.02165f, 0.75f, (false, (50, 220, 220, 200))),
                iW:1, iA:1, iG:3) },                                                                        
                { (0, 1), new EntityTraits("ObsidianFairy",   10, ((-3, 0, 0), 1),      //  --> Fairy Liquid
                new ColorRange((30, 0, 30), (30, 0, 30), (30, 0, 30)), lR:7, wT:(0, (0, 0), 0.02165f, 0.75f, (false, (50, 0, 0, 0))),
                iW:1, iA:1, iG:3) },
                { (0, 2), new EntityTraits("FrostFairy",      4 , ((-3, 0, 0), 1),      //  --> Fairy Liquid
                new ColorRange((200, 25, 30), (200, 25, 30), (225, 0, 30)), lR:7, wT:(0, (0, 0), 0.02165f, 0.75f, (false, (50, 255, 255, 255))),
                iW:1, iA:1, iG:3) },
                { (0, 3), new EntityTraits("SkeletonFairy",   15, ((8, 1, 3), 1),       //  --> Bone
                new ColorRange((210, 0, 20), (210, 0, 20), (190, 20, 20)), lR:7, wT:(0, (0, 0), 0.02165f, 0.75f, (false, (50, 230, 230, 230))),
                iW:1, iA:1, iG:3) },

                { (1, 0), new EntityTraits("Frog",            2,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((90, 50, 30), (210, 50, 30), (110, -50, 30)),
                iW:1, oW:2, iA:0, oG:1, iG:1, jS:(2, 2.5f), jC:0.1f) },
                { (1, 1), new EntityTraits("Carnal",          7,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((135, 0, 30), (55, 30, 30), (55, 30, 30)),
                iW:1, oW:2, iA:0, oG:1, iG:1, jS:(2, 2.5f), jC:0.1f) },
                { (1, 2), new EntityTraits("Skeletal",        7,  ((8, 1, 3), 1),       //  --> Bone
                new ColorRange((210, 0, 20), (210, 0, 20), (190, 20, 20)),
                iW:1, oW:2, iA:0, oG:1, iG:1, jS:(2, 2.5f), jC:0.1f) },

                { (2, 0), new EntityTraits("Fish",            2,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((190, 0, 30), (80, -50, 30), (80, 50, 30)), wT:(1, (0, 0), 0.01865f, 0.05f, (true, (-205, 50, 50, 50))),
                iW:2, oG:1, iG:1, jC:0.01f) },
                { (2, 1), new EntityTraits("SkeletonFish",    2,  ((8, 1, 3), 1),       //  --> Bone
                new ColorRange((210, 0, 20), (210, 0, 20), (190, 20, 20)),
                iW:2, oG:1, iG:1, jC:0.01f) },
                { (2, 2), new EntityTraits("Pufferfish",      2,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((190, 0, 30), (80, -50, 30), (80, 50, 30)),
                iW:2, oG:1, iG:1, jC:0.01f) },

                { (3, 0), new EntityTraits("HornetEgg",       2,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((205, 10, 30), (205, 10, 30), (235, 0, 30)),                                              
                iW:1) },
                { (3, 1), new EntityTraits("HornetLarva",     3,  ((8, 0, 3), 1),       //  --> Flesh   
                new ColorRange((180, 10, 30), (180, 10, 30), (160, 0, 30)),                             
                iW:1, oW:2, iA:0, oG:1, iG:1, jC:0.01f) },                                                              
                { (3, 2), new EntityTraits("HornetCocoon",    20, ((8, 0, 3), 1),       //  --> Flesh   
                new ColorRange((120, 10, 30), (120, 10, 30), (20, 0, 20)),                              
                iW:1) },
                { (3, 3), new EntityTraits("Hornet",          6,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((190, 10, 30), (190, 10, 30), (80, 0, 30)), wT:(0, (0, 0), 0.02165f, 0.75f, (false, (50, 220, 220, 200))),
                iW:1, iA:1, iG:4) },

                { (4, 0), new EntityTraits("Worm",            7,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((210, 0, 30), (140, 20, 30), (140, 20, 30)), L:(2, 4),
                dT:new HashSet<(int type, int subType)>{ (1, 0), (2, 0), (3, 2) },
                iW:3, oW:2, iA:0, oG:3, iG:2) },
                { (4, 1), new EntityTraits("Nematode",        3,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((210, -20, 30), (210, 20, 30), (235, 0, 30)), L:(2, 8),
                dT:new HashSet<(int type, int subType)>{ (4, 0), (4, 1), (4, 2) },
                iW:2, oW:2, iA:0, oG:3, iG:2) },
                { (4, 2), new EntityTraits("Salt Worm",       10, ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((170, -5, 5), (120, 5, 5), (140, 5, 5)), L:(3, 3),
                tM:new (int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color)[]
                {
                    (0, false, true, 1,  (1, 0), (true, null, (0, -15, -15, -20))), (0, false, true, 1,  (2, 0), (true, null, (-128, -30, -30, -35))),
                    (0, false, true, -1, (1, 0), (true, null, (0, -15, -15, -20))), (0, false, true, -1, (2, 0), (true, null, (-128, -30, -30, -35)))
                },
                dT:new HashSet<(int type, int subType)>{ (6, 0) },
                iW:2, oW:2, iA:0, oG:3, iG:2) },
                { (4, 3), new EntityTraits("Ice Worm",        7,  ((8, 0, 3), 1),       //  --> Flesh
                famousColorRanges["IceStem"], L:(4, 8),
                tM:new (int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color)[]
                {
                    (1, false, true, 1,  (1, 0), (true, null, (0, 25, 25, 15))), (1, false, true, 1,  (2, 0), (true, null, (-64, 35, 35, 25))),
                    (1, false, true, -1, (1, 0), (true, null, (0, 25, 25, 15))), (1, false, true, -1, (2, 0), (true, null, (-64, 35, 35, 25))),
                    (1, false, true, 2,  (1, 0), (true, null, (0, 25, 25, 15))), (1, false, true, 2,  (2, 0), (true, null, (-64, 35, 35, 25))),
                    (1, false, true, -2, (1, 0), (true, null, (0, 25, 25, 15))), (1, false, true, -2, (2, 0), (true, null, (-64, 35, 35, 25)))
                },
                dT:new HashSet<(int type, int subType)>{ (-2, -1) },
                iW:2, oW:2, iA:0, oG:3, iG:2) },
                { (4, 4), new EntityTraits("Icealt Worm",     15, ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((145, 20, 15), (120, 0, 15), (190, -20, 15)), L:(8, 16),
                tM:new (int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color)[]
                {
                    (1,  false, true, 1,  (1, 0), (true, null, (0, 35, 25, 25))), (1,  false, true, 1,  (2, 0), (true, null, (0, 70, 50, 50))),
                    (2,  false, true, -1, (1, 0), (true, null, (0, 35, 25, 25))), (2,  false, true, -1, (2, 0), (true, null, (0, 70, 50, 50))),
                    (3,  false, true, 1,  (1, 0), (true, null, (0, 35, 25, 25))), (3,  false, true, 1,  (2, 0), (true, null, (0, 70, 50, 50))),
                    (5,  false, true, -1, (1, 0), (true, null, (0, 35, 25, 25))), (5,  false, true, -1, (2, 0), (true, null, (0, 70, 50, 50))),
                    (7,  false, true, 1,  (1, 0), (true, null, (0, 35, 25, 25))), (7,  false, true, 1,  (2, 0), (true, null, (0, 70, 50, 50))),
                    (9,  false, true, -1, (1, 0), (true, null, (0, 35, 25, 25))), (9,  false, true, -1, (2, 0), (true, null, (0, 70, 50, 50))),
                    (11, false, true, 1,  (1, 0), (true, null, (0, 35, 25, 25))), (11, false, true, 1,  (2, 0), (true, null, (0, 70, 50, 50))),
                    (13, false, true, -1, (1, 0), (true, null, (0, 35, 25, 25))), (13, false, true, -1, (2, 0), (true, null, (0, 70, 50, 50))),
                    (15, false, true, 1,  (1, 0), (true, null, (0, 35, 25, 25))), (15, false, true, 1,  (2, 0), (true, null, (0, 70, 50, 50))),
                    (17, false, true, -1, (1, 0), (true, null, (0, 35, 25, 25))), (17, false, true, -1, (2, 0), (true, null, (0, 70, 50, 50))),
                    (19, false, true, 1,  (1, 0), (true, null, (0, 35, 25, 25))), (19, false, true, 1,  (2, 0), (true, null, (0, 70, 50, 50))),
                    (21, false, true, -1, (1, 0), (true, null, (0, 35, 25, 25))), (21, false, true, -1, (2, 0), (true, null, (0, 70, 50, 50))),
                    (23, false, true, 1,  (1, 0), (true, null, (0, 35, 25, 25))), (23, false, true, 1,  (2, 0), (true, null, (0, 70, 50, 50))),
                    (0,  true,  true, 0,  (1, 0), (true, null, (0, 35, 25, 25))), (0,  true,  true, 0,  (2, 0), (true, null, (0, 70, 50, 50))),
                },
                dT:new HashSet<(int type, int subType)>{ (6, 0), (-2, -1) },
                iW:2, oW:2, iA:0, oG:3, iG:2) },

                { (5, 0), new EntityTraits("WaterSkipper",    3,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((110, 0, 30), (110, 0, 30), (140, 20, 30)),
                iW:1, oW:1, iA:0, oG:1, iG:1, jC:0.05f, tICSO:new HashSet<(int type, int subType)>{ (-2, 0) }) },

                { (6, 0), new EntityTraits("Goblin",          3,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((80, 50, 30), (175, 50, 30), (80, 50, 30)),
                iW:1, oW:2, iA:0, oG:1, iG:1, jS:(1, 4), jC:0.05f) },

                { (7, 0), new EntityTraits("Louse",           2,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((160, -10, 30), (180, 10, 30), (200, 10, 30)),
                iW:1, oW:2, iA:0, oG:4, iG:1, oP:1, jS:(1, 1.5f), jC:0.1f) },

                { (8, 0), new EntityTraits("Shark",           5,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((110, 0, 30), (110, 0, 30), (140, 20, 30)), L:(4, 1),
                tM:new (int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color)[]
                {
                    (2, false, false, 0, (0, 1), (true, null, (0, -15, -15, -20))), (2, false, false, 0, (0, 2), (true, null, (0, -15, -15, -20))),
                    (0, true, true, 1, (1, 0), (true, null, (0, -8, -8, -13))),
                    (0, true, true, 7, (1, 0), (true, null, (0, -8, -8, -13)))
                },
                iW:2, oG:1, iG:1, jC:0.01f, tAR:2.5f) },
                { (8, 1), new EntityTraits("Anglerfish",      5,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((80, 20, 20), (55, 0, 20), (35, -10, 20)), L:(9, 0),
                tM: new (int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color)[]
                {   // light part, then eye
                    (2, false, false, 0, (5, 3), (false, 4, (255, 255, 255, 255))), (2, false, false, 0, (0, 2), (false, null, (255, 230, 230, 230)))
                },
                tM2:new ((bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color, (int segment, bool fromEnd, (int x, int y) pos)[] array)[]
                {
                    ((true, null, (0, 0, 0, 0)), new (int segment, bool fromEnd, (int x, int y) pos)[]
                    {
                        (0, false, (1, -2)),
                        (0, false, (0, -3)), (0, false, (0, -2)), (0, false, (0, 2)),
                        (1, false, (0, -3)), (1, false, (0, -2)), (1, false, (0, 1)), (1, false, (0, 2)), (1, false, (0, 3)),
                        (2, false, (0, -3)), (2, false, (0, -2)), (2, false, (0, -1)), (2, false, (0, 0)), (2, false, (0, 1)), (2, false, (0, 2)), (2, false, (0, 3)),
                        (3, false, (0, -3)), (3, false, (0, -2)), (3, false, (0, -1)), (3, false, (0, 0)), (3, false, (0, 1)), (3, false, (0, 2)), (3, false, (0, 3)),
                        (4, false, (0, -3)), (4, false, (0, -2)), (4, false, (0, -1)), (4, false, (0, 0)), (4, false, (0, 1)), (4, false, (0, 2)), (4, false, (0, 3)),
                        (5, false, (0, -2)), (5, false, (0, -1)), (5, false, (0, 0)), (5, false, (0, 1)), (5, false, (0, 2)),
                        (6, false, (0, -1)), (6, false, (0, 0)), (6, false, (0, 1)),

                        (2, false, (0, 4)), (2, false, (-1, 5)), (2, false, (-2, 5)), (2, false, (-2, 6)), (2, false, (-1, 7)), (2, false, (0, 7)), (2, false, (1, 7)), (2, false, (2, 7)), (2, false, (3, 6)), (2, false, (4, 5)), (2, false, (5, 4))
                    }),

                    ((true, null, (0, 70, 75, 80)), new (int segment, bool fromEnd, (int x, int y) pos)[]
                    {
                        (0, false, (-1, 0)), (0, false, (0, -1)), (0, false, (1, 1)), (0, false, (2, -1)), // Teeth

                        (5, false, (-1, 3)), (5, false, (0, 3)),
                        (5, false, (-1, -3)), (5, false, (0, -3)),

                        (7, false, (0, 1)), (7, false, (0, 0)), (7, false, (0, -1)),
                        (7, false, (-1, 1)), (7, false, (-1, 2)), (7, false, (-1, 0)), (7, false, (-1, -1)), (7, false, (-1, -2)),
                    })
                }, 
                tAR:3.1f, tT:true,
                cP:(new (int x, int y)[] { (-6, 5), (-5, 6), (-4, 7), (-3, 7), (-2, 7), (-1, 7), (0, 7), (1, 6), (2, 5), (3, 4) },
                    new (int x, int y)[] { (0, -3), (1, -2), (2, -1), (2, 0), (2, 1), (2, 2), (3, 3), (3, 4), (2, 5), (1, 6), (0, 7) },
                    new (int x, int y)[] { (-6, -1), (-5, -2), (-4, -3), (-3, -3), (-2, -3), (-1, -3), (0, -3), (1, -2), (2, -1) }), 
                iW:2, oG:1, iG:1, jC:0.01f) },

                { (9, 0), new EntityTraits("WaterDog",       15,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((150, 0, 30), (120, 10, 30), (90, 20, 30)), L:(13, 7),
                tM:new (int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color)[]
                {
                    (2, false, false, 0, (0, 1),  (true, null, (0, -20, -20, -15))), (2, false, false, 0, (0, 2), (true, null, (0, -20, -20, -15))),
                    (3, false, false, 0, (0, -1), (true, null, (0, -20, -20, -15))),
                    (5, false, false, 0, (0, 1),  (true, null, (0, -20, -20, -15))), (5, false, false, 0, (0, 2), (true, null, (0, -20, -20, -15))),
                    (6, false, false, 0, (0, -1), (true, null, (0, -20, -20, -15))),
                    (8, false, false, 0, (0, 1),  (true, null, (0, -20, -20, -15))),
                    (11, false, false, 0, (0, 1), (true, null, (0, -20, -20, -15)))
                },
                iW:2, oG:1, iG:1, jC:0.01f) },

                { (10, 0), new EntityTraits("Dragonfly",      2,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((50, -30, 20), (70, 30, 25), (110, 15, 30)), L:(2, 0), wT:(0, (0, 0), 0.007132f, 1.5f, (false, (50, 220, 220, 200))),
                iW:1, iA:2, iG:3, tAR:2, mC:0.02f, fIJ:true, tICSO:new HashSet<(int type, int subType)>{ (-2, 0), (-2, 2) }) },
            };
        }


         




        



        public class PlantStructureFrame
        {
            public (bool x, bool y) directionalFlip;
            public Dictionary<(int x, int y), (int type, int subType)> elementDict;
            public PlantStructureFrame(Dictionary<(int type, int subType), (int x, int y)[]> dict = null, (bool x, bool y)? dF = null)
            {
                elementDict = new Dictionary<(int type, int subType), (int x, int y)>();
                if (dict is null) { return; }
                foreach ((int type, int subType) key in dict.Keys)
                {
                    foreach ((int x, int y) pos in dict[key]) { elementDict[pos] = key; }
                }
                directionalFlip = dF ?? (true, false);
            }
        }

        public static Dictionary<string, PlantStructureFrame> plantStructureFramesDict;
        public static void makePlantStructureFramesDict()
        {
            plantStructureFramesDict = new Dictionary<string, PlantStructureFrame>()
            {
                { "Error", new PlantStructureFrame() },



                { "PlusFlower-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "PlusFlower-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -1), (-1, 0), (0, 0), (1, 0), (0, 1) } } }
                ) },
                { "PlusFlower-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -1), (-1, 0), (1, 0), (0, 1) } },     { (2, 1), new (int x, int y)[] { (0, 0) } } }
                ) },

                { "CrossFlower-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "CrossFlower-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, -1), (-1, 1), (0, 0), (1, -1), (1, 1) } } }
                ) },
                { "CrossFlower-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, -1), (-1, 1), (1, -1), (1, 1) } },   { (2, 1), new (int x, int y)[] { (0, 0) } } }
                ) },

                { "BigFlower-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "BigFlower-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -1), (-1, 0), (0, 0), (1, 0), (0, 1) } } }
                ) },
                { "BigFlower-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -2), (0, -1), (-2, 0), (-1, 0), (0, 0), (2, 0), (1, 0), (0, 2), (0, 1), (1, 1), (-1, 1), (1, -1), (-1, -1), } } }
                ) },
                { "BigFlower-4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -3), (0, -2), (0, -1), (-3, 0), (-2, 0), (-1, 0), (0, 0), (3, 0), (2, 0), (1, 0), (0, 3), (0, 2), (0, 1),     (2, 2), (1, 1), (-2, 2), (-1, 1), (2, -2), (1, -1), (-2, -2), (-1, -1), } } }
                ) },
                { "BigFlower-5", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -3), (0, -2), (0, -1), (-3, 0), (-2, 0), (-1, 0), (3, 0), (2, 0), (1, 0), (0, 3), (0, 2), (0, 1),     (2, 2), (1, 1), (-2, 2), (-1, 1), (2, -2), (1, -1), (-2, -2), (-1, -1), } }, { (2, 1), new (int x, int y)[] { (0, 0) } } }
                ) },


                { "TulipFlower-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "TulipFlower-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0), (0, 1) } } }
                ) },
                { "TulipFlower-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0), (0, 1), (0, 2) } } }
                ) },
                { "TulipFlower-4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0), (-1, 1), (0, 1), (1, 1), (0, 2) } } }
                ) },
                { "TulipFlower-5", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0), (-1, 1), (0, 1), (1, 1), (-1, 2), (1, 2) } } }
                ) },

                { "AlliumFlower-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "AlliumFlower-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0), (-1, 1), (0, 1), (1, 1) } } }
                ) },
                { "AlliumFlower-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0), (-1, 1), (0, 1), (1, 1), (-1, 2), (0, 2), (1, 2) } } }
                ) },
                { "AlliumFlower-4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0), (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1), (-2, 2), (-1, 2), (0, 2), (1, 2), (2, 2), (-1, 3), (0, 3), (1, 3) } } }
                ) },



                { "TreeLeaves-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "TreeLeaves-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, -1), (-1, 0), (0, 0), (1, 0), (0, 1) } } }
                ) },
                { "TreeLeaves-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-1, -1), (0, -1), (1, -1), (-1, 0), (0, 0), (1, 0), (-1, 1), (0, 1), (1, 1) } } }
                ) },
                { "TreeLeaves-4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-1, -2), (0, -2), (1, -2), (-2, -1), (-1, -1), (0, -1), (1, -1), (2, -1), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1), (-1, 2), (0, 2), (1, 2) } } }
                ) },

                { "FirLeaves-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "FirLeaves-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-1, 0), (0, 0), (0, 1), (1, 1) } } }
                ) },
                { "FirLeaves-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-2, 0), (-1, 0), (0, 0), (1, 0), (-1, 1), (0, 1), (1, 1), (2, 1) } } }
                ) },
                { "FirLeaves-4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (-1, 1), (0, 1), (1, 1), (2, 1), (3, 1) } } }
                ) },
                { "FirLeaves-5", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, -1), (1, -1), (2, -1), (3, -1), (4, -1),  (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0),  (-4, 1), (-3, 1), (-2, 1), (-1, 1), (0, 1) } } }
                ) },

                { "FirLeavesTop-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "FirLeavesTop-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, -1),  (-1, 0), (0, 0), (1, 0),  (1, 1) } } }
                ) },
                { "FirLeavesTop-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0),  (0, 1), } } }
                ) },
                { "FirLeavesTop-4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-2, -1), (-1, -1), (0, -1), (1, -1), (2, -2),  (2, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0),  (0, 1) } } }
                ) },
                { "FirLeavesTop-5", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-4, -1), (-3, -1), (-2, -1), (-1, -1), (0, -1), (1, -1), (2, -1), (3, -1), (4, -1),  (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0),  (0, 1) } } }
                ) },

                { "JungleLeaves-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "JungleLeaves-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0) } } }
                ) },
                { "JungleLeaves-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0),  (-1, 1), (0, 1), (1, 1) } } }
                ) },
                { "JungleLeaves-4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-1, -1), (0, -1), (1, -1),  (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0),  (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1) } } }
                ) },
                { "JungleLeaves-5", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-2, -1), (-1, -1), (0, -1), (1, -1), (2, -1),  (-4, 0), (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0), (4, 0),  (-3, 1), (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1), (3, 1),  (-1, 2), (0, 2), (1, 2) } } }
                ) },

                { "MangroveLeaves-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "MangroveLeaves-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, 0), (0, 1) } } }
                ) },
                { "MangroveLeaves-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0),  (-1, 1), (0, 1), (1, 1) } } }
                ) },
                { "MangroveLeaves-4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0),  (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1),  (-2, 2), (-1, 2), (0, 2), (1, 2), (2, 2) } } }
                ) },
                { "MangroveLeaves-5", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0),  (-3, 1), (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1), (3, 1),  (-3, 2), (-2, 2), (-1, 2), (0, 2), (1, 2), (2, 2), (3, 2),  (-2, 3), (-1, 3), (0, 3), (1, 3), (2, 3) } } }
                ) },



                { "ReedFlower-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "ReedFlower-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0), (0, 1) } } }
                ) },
                { "ReedFlower-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0), (0, 1) } }, { (2, 1), new (int x, int y)[] { (0, 2) } } }
                ) },

                { "BulbousAlgaeLeaves-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "BulbousAlgaeLeaves-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, -1), (-1, 0), (0, 0), (1, 0), (0, 1) } } }
                ) },
                { "BulbousAlgaeLeaves-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, -2), (-1, -1), (0, -1), (-1, 0), (0, 0), (1, 0), (0, 1), (1, 1), (0, 2) } } }
                ) },
                { "BulbousAlgaeLeaves-4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, -3), (-1, -2), (0, -2), (-1, -1), (0, -1), (1, -1), (-1, 0), (0, 0), (1, 0), (-1, 1), (0, 1), (1, 1), (0, 2), (1, 2), (0, 3) } } }
                ) },



                { "MushroomCap-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (3, 1), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0) } } }
                ) },
                { "MushroomCap-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (3, 1), new (int x, int y)[] { (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0) } } }
                ) },
                { "MushroomCap-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (3, 1), new (int x, int y)[] { (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (1, 1), (-1, 1), (0, 1), (1, 1) } } }
                ) },



                { "IceTromel-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "IceTromel-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -1), (-1, 0), (0, 0), (1, 0), (0, 1) } } }
                ) },
                { "IceTromel-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -2), (0, -1), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (0, 1), (0, 2) } } }
                ) },
                { "IceTromel-4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, -3), (1, -3), (0, -2), (0, -1),  (-1, 3), (1, 3), (0, 2), (0, 1),  (-3, -1), (-3, 1), (-2, 0), (-1, 0),  (3, -1), (3, 1), (2, 0), (1, 0),  (0, 0) } } }
                ) },
                { "IceTromel-5", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, -3), (1, -3), (0, -2), (0, -1),  (-1, 3), (1, 3), (0, 2), (0, 1),  (-3, -1), (-3, 1), (-2, 0), (-1, 0),  (3, -1), (3, 1), (2, 0), (1, 0) } }, { (2, 1), new (int x, int y)[] { (0, 0) } } }
                ) },

                { "IceKital-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "IceKital-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, -1), (-1, 1), (0, 0), (1, -1), (1, 1) } } }
                ) },
                { "IceKital-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-2, -2), (-2, 2), (-2, 2), (2, 2),  (-1, -1), (-1, 0), (-1, 1), (0, 1), (1, 1), (1, 0), (1, -1), (0, -1),  (0, 0) } } }
                ) },
                { "IceKital-4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-3, -3), (3, -3), (-3, 3), (3, 3),  (-2, -2), (-1, -2), (-2, -1),  (2, -2), (1, -2), (2, -1),  (-2, 2), (-1, 2), (-2, 1),  (2, 2), (1, 2), (2, 1),  (-1, -1), (-1, 0), (-1, 1), (0, 1), (1, 1), (1, 0), (1, -1), (0, -1),  (0, 0) } } }
                ) },
                { "IceKital-5", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-3, -3), (3, -3), (-3, 3), (3, 3),  (-2, -2), (-1, -2), (-2, -1),  (2, -2), (1, -2), (2, -1),  (-2, 2), (-1, 2), (-2, 1),  (2, 2), (1, 2), (2, 1),  (-1, -1), (-1, 0), (-1, 1), (0, 1), (1, 1), (1, 0), (1, -1), (0, -1) } }, { (2, 1), new (int x, int y)[] { (0, 0) } } }
                ) },

                { "IceFlokan-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "IceFlokan-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -1), (-1, 0), (0, 0), (1, 0), (0, 1) } } }
                ) },
                { "IceFlokan-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, -1), (0, -1), (1, -1),  (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0),  (-1, 1), (0, 1), (1, 1) } } }
                ) },
                { "IceFlokan-4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -2),  (-2, -1), (-1, -1), (0, -1), (1, -1), (2, -1),  (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0),  (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1),  (0, 2) } } }
                ) },
                { "IceFlokan-5", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -3),  (-3, -2), (0, -2), (3, -2),  (-2, -1), (-1, -1), (0, -1), (1, -1), (2, -1),  (-4, 0), (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0), (4, 0),  (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1),  (-3, 2), (0, 2), (3, 2),  (0, 3) } } }
                ) },
                { "IceFlokan-6", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, -4), (1, -4),  (-3, -3), (0, -3), (3, -3),  (-4, -2), (-3, -2), (-1, -2), (0, -2), (1, -2), (4, -2), (3, -2),  (-2, -1), (-1, -1), (0, -1), (1, -1), (2, -1),  (-5, 0), (-4, 0), (-3, 0), (-2, 0), (-1, 0), (1, 0), (2, 0), (3, 0), (4, 0), (5, 0),  (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1),  (-4, 2), (-3, 2), (-1, 2), (0, 2), (1, 2), (3, 2), (4, 2),  (-3, 3), (0, 3), (3, 3),  (-1, 4), (1, 4) } }, { (2, 1), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "IceFlokan-7", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (-1, -4), (1, -4),  (-3, -3), (0, -3), (3, -3),  (-4, -2), (-3, -2), (-1, -2), (0, -2), (1, -2), (4, -2), (3, -2),  (-2, -1), (-1, -1), (1, -1), (2, -1),  (-5, 0), (-4, 0), (-3, 0), (-2, 0), (2, 0), (3, 0), (4, 0), (5, 0),  (-2, 1), (-1, 1), (1, 1), (2, 1),  (-4, 2), (-3, 2), (-1, 2), (0, 2), (1, 2), (3, 2), (4, 2),  (-3, 3), (0, 3), (3, 3),  (-1, 4), (1, 4) } }, { (2, 1), new (int x, int y)[] { (0, -1), (-1, 0), (0, 0), (1, 0), (0, 1) } } }
                ) },

                { "IceOctam-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "IceOctam-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -1),  (-1, 0), (0, 0), (1, 0),  (0, 1) } } }
                ) },
                { "IceOctam-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -2),  (0, -1), (1, -1),  (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0),  (-1, 1), (0, 1),  (0, 2) } } }
                ) },
                { "IceOctam-4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -3),  (0, -2),  (-1, -1), (0, -1), (1, -1),  (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0),  (-1, 1), (0, 1), (1, 1),  (0, 2),  (0, 3) } } }
                ) },
                { "IceOctam-5", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -4),  (-1, -3), (0, -3), (1, -3),  (0, -2),  (-1, -1), (0, -1), (1, -1),  (-4, 0), (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0), (4, 0),  (-1, 1), (0, 1), (1, 1),  (0, 2),  (-1, 3), (0, 3), (1, 3),  (0, 4) } } }
                ) },
                { "IceOctam-6", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -5),  (-1, -4), (1, -4),  (0, -3),  (-2, -2), (0, -2), (2, -2),  (-4, -1), (-1, -1), (0, -1), (1, -1), (4, -1),  (-5, 0), (-4, 0), (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0), (4, 0), (5, 0),  (-4, 1), (-1, 1), (0, 1), (1, 1), (4, 1),  (-2, 2), (0, 2), (2, 2),  (0, 3),  (-1, 4), (1, 4),  (0, 5) } } }
                ) },
                { "IceOctam-7", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -6),  (-1, -5), (1, -5),  (0, -4),  (-3, -3), (-2, -3), (0, -3), (2, -3), (3, -3),  (-2, -2), (0, -2), (2, -2),  (-4, -1), (-1, -1), (0, -1), (1, -1), (4, -1),  (-5, 0), (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0), (5, 0),  (-4, 1), (-1, 1), (0, 1), (1, 1), (4, 1),  (-2, 2), (0, 2), (2, 2),  (-3, 3), (-2, 3), (0, 3), (2, 3), (3, 3),  (0, 4),  (-1, 5), (1, 5),  (0, 6) } } }
                ) },
                { "IceOctam-8", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -7),  (-1, -6), (1, -6),  (0, -5),  (-4, -4), (-3, -4), (0, -4), (3, -4), (4, -4),  (-3, -3), (-2, -3), (0, -3), (2, -3), (3, -3),  (-2, -2), (0, -2), (2, -2),  (-5, -1), (-4, -1), (-1, -1), (0, -1), (1, -1), (4, -1), (5, -1),  (-6, 0), (-5, 0), (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0), (5, 0), (6, 0),  (-5, 1), (-4, 1), (-1, 1), (0, 1), (1, 1), (4, 1), (5, 1),  (-2, 2), (0, 2), (2, 2),  (-3, 3), (-2, 3), (0, 3), (2, 3), (3, 3),  (-4, 4), (-3, 4), (0, 4), (3, 4), (4, 4),  (0, 5),  (-1, 6), (1, 6),  (0, 7) } } }
                ) },
                { "IceOctam-9", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -8),  (-1, -7), (0, -7), (1, -7),  (-1, -6), (1, -6),  (-4, -5), (-3, -5), (0, -5), (3, -5), (4, -5),  (-4, -4), (-2, -4), (0, -4), (2, -4), (4, -4),  (-3, -3), (-2, -3), (0, -3), (2, -3), (3, -3),  (-2, -2), (0, -2), (2, -2),  (-5, -1), (-4, -1), (-1, -1), (0, -1), (1, -1), (4, -1), (5, -1),  (-6, 0), (-5, 0), (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0), (5, 0), (6, 0),  (-5, 1), (-4, 1), (-1, 1), (0, 1), (1, 1), (4, 1), (5, 1),  (-2, 2), (0, 2), (2, 2),  (-3, 3), (-2, 3), (0, 3), (2, 3), (3, 3),  (-4, 4), (-2, 4), (0, 4), (2, 4), (4, 4),  (-4, 5), (-3, 5), (0, 5), (3, 5), (4, 5),  (-1, 6), (1, 6),  (-1, 7), (0, 7), (1, 7),  (0, 8) } } }
                ) },
                { "IceOctam-10", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (2, 0), new (int x, int y)[] { (0, -8),  (-1, -7), (0, -7), (1, -7),  (-1, -6), (1, -6),  (-4, -5), (-3, -5), (0, -5), (3, -5), (4, -5),  (-4, -4), (-2, -4), (0, -4), (2, -4), (4, -4),  (-3, -3), (-2, -3), (0, -3), (2, -3), (3, -3),  (-2, -2), (0, -2), (2, -2),  (-5, -1), (-4, -1), (-1, -1), (0, -1), (1, -1), (4, -1), (5, -1),  (-6, 0), (-5, 0), (-3, 0), (-2, 0), (-1, 0), (1, 0), (2, 0), (3, 0), (5, 0), (6, 0),  (-5, 1), (-4, 1), (-1, 1), (0, 1), (1, 1), (4, 1), (5, 1),  (-2, 2), (0, 2), (2, 2),  (-3, 3), (-2, 3), (0, 3), (2, 3), (3, 3),  (-4, 4), (-2, 4), (0, 4), (2, 4), (4, 4),  (-4, 5), (-3, 5), (0, 5), (3, 5), (4, 5),  (-1, 6), (1, 6),  (-1, 7), (0, 7), (1, 7),  (0, 8) } }, { (2, 1), new (int x, int y)[] { (0, 0) } } }
                ) },





                { "Lantern-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 1), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "Lantern-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 1), new (int x, int y)[] { (0, 0) } },  { (11, 0), new (int x, int y)[] { (0, 1) } } }
                ) },
                { "Lantern-3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 1), new (int x, int y)[] { (0, 0) } },  { (11, 0), new (int x, int y)[] { (-1, 1), (0, 1), (1, 1) } } }
                ) },
                { "Lantern-4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 1), new (int x, int y)[] { (0, 0) } },  { (11, 0), new (int x, int y)[] { (-1, 1), (0, 1), (1, 1), (0, 2) } } }
                ) },
                { "Lantern-5", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 1), new (int x, int y)[] { (0, 0) } },  { (11, 0), new (int x, int y)[] { (-1, -1), (0, -1), (1, -1), (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1), (0, 2) } } }
                ) },

                { "CandleHolder-1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 0), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "CandleHolder-2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 0), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0) } } }
                ) },

            };
        }



        public class PlantGrowthRules
        {
            public (int maxLevel, int range) maxGrowth;
            public (float step, bool fromEnd)? maxGrowthParentRelatedVariation;
            public bool offsetMaxGrowthVariation;
            public (float baseValue, float variation) growthSpeedVariationFactor;

            public (int type, int subType) materalToFillWith;
            public (int type, int subType)[] tileContentNeededToGrow;
            public ((int x, int y, bool stopGrowth)[] left, (int x, int y, bool stopGrowth)[] right, (int x, int y, bool stopGrowth)[] down, (int x, int y, bool stopGrowth)[] up)? hindrancePreventionPositions;

            public ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped)? elementWidening;

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
            public PlantGrowthRules((int type, int subType) t, (int type, int subType)[] tCNTG = null, (int frame, int range)? mG = null, (float step, bool fromEnd)? mGPRV = null, bool oMGV = false, (float baseValue, float variation)? gSVF = null,
                ((int x, int y, bool stopGrowth)[] left, (int x, int y, bool stopGrowth)[] right, (int x, int y, bool stopGrowth)[] down, (int x, int y, bool stopGrowth)[] up)? hPP = null,
                ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped)? eW = null,
                ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] cOGS = null,
                ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] cOGE = null,
                ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] C = null, int cO = 0, bool lC = false, bool mTC = false,    // O O OOOO I. WANT A HEN-
                ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped)? sD = null,
                ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] DG = null, int dGO = 0, bool rDG = false, bool lDG = false,
                ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] PM = null, int pMO = 0, bool lPM = false, bool pG = true, bool M = false)
            {
                maxGrowth = mG ?? (5, 0);
                maxGrowthParentRelatedVariation = mGPRV;
                offsetMaxGrowthVariation = oMGV;
                growthSpeedVariationFactor = gSVF ?? (1, 0);

                materalToFillWith = t;
                tileContentNeededToGrow = tCNTG;
                hindrancePreventionPositions = hPP;

                elementWidening = eW;

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
            { "LeavesMangrove", (new (int x, int y, bool stopGrowth)[]{ (-1, 0, true), (-3, 1, true), (-3, 2, true), (-2, 3, true) }, new (int x, int y, bool stopGrowth)[]{ (1, 0, true), (3, 1, true), (3, 2, true), (2, 3, true) }, new (int x, int y, bool stopGrowth)[]{ (-3, 1, false), (-2, 1, true), (-1, 0, true), (0, 0, true), (1, 0, true), (2, 1, true), (3, 1, false) }, new (int x, int y, bool stopGrowth)[]{ (-3, 2, false), (-2, 3, true), (-1, 3, true), (0, 3, true), (1, 3, true), (2, 3, true), (3, 2, false) }) },
            { "Fire", (new (int x, int y, bool stopGrowth)[]{ (-1, 1, true), (-1, 2, true), (-1, 3, true), (-1, 4, true) }, new (int x, int y, bool stopGrowth)[]{ (1, 1, true), (1, 2, true), (1, 3, true), (1, 4, true) }, new (int x, int y, bool stopGrowth)[]{ (-1, 0, true), (0, 0, true), (1, 0, true) }, new (int x, int y, bool stopGrowth)[]{ (-1, 4, true), (0, 4, true), (1, 4, true) }) },

            { "Side1Gap", (new (int x, int y, bool stopGrowth)[]{ (-1, 0, true) }, new (int x, int y, bool stopGrowth)[]{ (1, 0, true) }, null, null) },

            { "Up1Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 1, true) }) },
            { "Up2Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 2, true) }) },
            { "Up3Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 3, true) }) },
            { "Up3GapX3Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (-1, 3, true), (0, 3, true), (1, 3, true) }) },
            { "Up4Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 4, true) }) },
            { "Up5Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 5, true) }) },
            { "Up6Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 6, true) }) },
            { "Up7Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 7, true) }) },
            { "Up8Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 8, true) }) },
            { "Up9Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 9, true) }) },
            { "Up10Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 10, true) }) },
            { "Up11Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 11, true) }) },
            { "Up12Gap", (null, null, null, new (int x, int y, bool stopGrowth)[]{ (0, 12, true) }) },

            { "Y12GapX7Gap", (new (int x, int y, bool stopGrowth)[]{ (-8, 0, true) }, new (int x, int y, bool stopGrowth)[]{ (8, 0, true) }, new (int x, int y, bool stopGrowth)[]{ (0, -12, true) }, new (int x, int y, bool stopGrowth)[]{ (0, 12, true) }) },

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
            public (float step, bool fromEnd)? maxGrowthParentRelatedVariation;
            public ((int x, int y) pos, (bool x, bool y) flip)? isSticky;

            public bool isClimbable;

            public OneAnimation animation;
            public ((int frame, int range) changeFrame, PlantStructureFrame frame)[] frames;
            public ((int type, int subType, int subSubType) plantElement, (int x, int y) offset, int chance)? deathChild;
            public PlantGrowthRules plantGrowthRules;
            public (int type, int subType, int subSubType)? transitionToOtherPlantElementOnGrowthEnd;

            public ((int x, int y) pos, (bool x, bool y) baseDirectionFlip)[] requiredEmptyTiles;
            public ((int x, int y) pos, (int type, int subType) type, (bool x, bool y) baseDirectionFlip)[] specificRequiredEmptyTiles;

            public bool forceLightAtPos;
            public int lightRadius;
            public (int type, int subType) lightElement;
            public ((int type, int subType) type, ColorRange colorRange)[] colorOverrideArray;  // If specific PlantElement has different color from the one in the whole Plant. If colorRange is null, it will take the colorRange of the motherPlant, but with a different range due to see (so variations of color in different leaves for example)

            public HashSet<(int type, int subType)> materialsPresent;
            public PlantElementTraits(string namee, ((int x, int y) pos, (bool x, bool y) flip)? stick = null, (int maxLevel, int range)? fMG = null, (float step, bool fromEnd)? mGPRV = null, OneAnimation anm = null,
                ((int frame, int range) changeFrame, PlantStructureFrame frame)[] framez = null, ((int type, int subType, int subSubType) plantElement,
                (int x, int y) offset, int chance)? dC = null, PlantGrowthRules pGR = null, ((int x, int y) pos, (bool x, bool y) baseDirectionFlip)[] rET = null,
                ((int x, int y) pos, (int type, int subType) type, (bool x, bool y) baseDirectionFlip)[] sRET = null, ((int type, int subType) type, ColorRange colorRange)[] cOverride = null,
                bool isReg = false, bool fLAP = false, int lR = 0, (int type, int subType)? lE = null, bool iC = false, (int type, int subType, int subSubType)? tTOPEOGE = null)
            {
                name = namee;
                isRegenerative = isReg;
                isSticky = stick;

                isClimbable = iC;

                animation = anm;
                frames = framez;
                deathChild = dC;
                plantGrowthRules = pGR;
                transitionToOtherPlantElementOnGrowthEnd = tTOPEOGE;

                requiredEmptyTiles = rET;
                specificRequiredEmptyTiles = sRET;

                forceLightAtPos = fLAP;
                lightRadius = lR;
                lightElement = lE ?? (0, 0);
                colorOverrideArray = cOverride;

                materialsPresent = new HashSet<(int type, int subType)>();

                if (plantGrowthRules != null)
                {
                    maxGrowth = fMG ?? plantGrowthRules.maxGrowth;  // fMG is forceMaxGrowth
                    maxGrowthParentRelatedVariation = mGPRV ?? plantGrowthRules.maxGrowthParentRelatedVariation;
                    materialsPresent.Add(plantGrowthRules.materalToFillWith);
                }
                else if (frames != null)
                {
                    maxGrowth = fMG ?? (framez.Length, 0);  // fMG is forceMaxGrowth
                    maxGrowthParentRelatedVariation = mGPRV;
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
                { (1, 1, 0), new PlantElementTraits("FirTrunk", rET:(from number in Enumerable.Range(0, 20) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 1), mG:(25, 15), hPP:fHPP["Leaves"],
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((1, 1, 3), (0, 0), 0, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 1, -1), (-1, 0), 1, 0, (2, 0), 100), ((1, 1, -1), (1, 0), 1, 0, (2, 0), 100) },
                    cO:4, lC:true,
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, true), (4, 8), 100) },
                    lPM:true
                )) },
                { (1, 2, 0), new PlantElementTraits("JungleTrunk", rET:(from number in Enumerable.Range(0, 20) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 1), mG:(14, 11), hPP:fHPP["Leaves"],
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((1, 2, 1), (0, 0), 0, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 2, -1), (-1, 0), 1, 0, (3, 2), 85), ((1, 2, -1), (1, 0), 1, 0, (3, 2), 85) },
                    cO:10, lC:true,
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, true), (4, 8), 100) },
                    lPM:true
                )) },

                { (1, 3, 0), new PlantElementTraits("MangroveTrunk", rET:(from number in Enumerable.Range(0, 20) select ((0, number), (true, false))).ToArray(), sRET:new ((int x, int y) pos, (int type, int subType) type, (bool x, bool y) baseDirectionFlip)[] { ((0, 15), (0, 0), (true, false)) },
                pGR:new PlantGrowthRules(t:(1, 1), mG:(10, 5), hPP:fHPP["LeavesMangrove"],
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((1, 3, 1), (0, 0), 0, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 3, -2), (-1, 0), 1, 0, (1, 1), 90), ((1, 3, -2), (1, 0), 1, 0, (1, 0), 90) },
                    lC:true,
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, true), (4, 8), 50) },
                    lPM:true
                ), tTOPEOGE:(1, 3, -1)) },
                { (1, 3, -1), new PlantElementTraits("MangroveTrunkSecondPart",
                pGR:new PlantGrowthRules(t:(1, 1), mG:(12, 8), hPP:fHPP["LeavesMangrove"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 3, -4), (-1, 0), 1, 0, (1, 2), 66), ((1, 3, -4), (1, 0), 1, 0, (1, 2), 66) },
                    cO:3, lC:true
                )) },

                { (1, 4, 0), new PlantElementTraits("WeepingWillowTrunk", rET:(from number in Enumerable.Range(0, 20) select ((0, number), (true, false))).Concat(from number in Enumerable.Range(7, 12) select ((4, number), (true, false))).Concat(from number in Enumerable.Range(7, 12) select ((-4, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 1), mG:(16, 3), eW:((-1, 0), (true, false, false)),
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 4, -2), (1, 0), 1, 0, (3, 2), 85), ((1, 4, -2), (-1, 0), 1, 0, (2, 1), 85) },
                    cO:7,
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 1), (true, false, false), (6, 3), 100), ((0, 1), (true, false, false), (4, 2), 100) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (3, 2), 100), ((1, 0), (true, false, false), (2, 0), 100), ((1, 0), (true, false, false), (2, 0), 100) }
                    ), tTOPEOGE:(1, 4, -1)) },
                { (1, 4, -1), new PlantElementTraits("WeepingWillowTrunkSecondPart",
                pGR:new PlantGrowthRules(t:(1, 1), mG:(16, 4),
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((1, 4, -3), (1, 1), 1, 0, 100)  },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 4, -3), (1, 1), 1, 0, (1, 1), 90), ((1, 4, -3), (-1, 1), 1, 0, (1, 1), 90)  },
                    lC:true, cO:5,
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((-1, 1), (true, false, false), (9, 4), 100), ((-1, 0), (true, false, false), (2, 1), 100) }
                )) },
                { (1, 5, 0), new PlantElementTraits("CheeringWillowTrunk", rET:(from number in Enumerable.Range(0, 20) select ((0, number), (true, false))).Concat(from number in Enumerable.Range(7, 12) select ((4, number), (true, false))).Concat(from number in Enumerable.Range(7, 12) select ((-4, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 1), mG:(16, 3), eW:((-1, 0), (true, false, false)),
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 5, -2), (1, 0), 1, 0, (3, 2), 85), ((1, 5, -2), (-1, 0), 1, 0, (2, 1), 85) },
                    cO:7,
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 1), (true, false, false), (6, 3), 100), ((0, 1), (true, false, false), (4, 2), 100) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (3, 2), 100), ((1, 0), (true, false, false), (2, 0), 100), ((1, 0), (true, false, false), (2, 0), 100) }
                    ), tTOPEOGE:(1, 5, -1)) },
                { (1, 5, -1), new PlantElementTraits("CheeringWillowTrunkSecondPart",
                pGR:new PlantGrowthRules(t:(1, 1), mG:(16, 4),
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((1, 5, -3), (1, 1), 1, 0, 100)  },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 5, -3), (1, 1), 1, 0, (1, 1), 90), ((1, 5, -3), (-1, 1), 1, 0, (1, 1), 90)  },
                    lC:true, cO:5,
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((-1, 1), (true, false, false), (9, 4), 100), ((-1, 0), (true, false, false), (2, 1), 100) }
                )) },

                { (2, 0, 0), new PlantElementTraits("Kelp", rET:(from number in Enumerable.Range(0, 5) select ((number % 2, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 2), mG:(3, 8), tCNTG:new (int type, int subType)[]{ (-2, 0), (-2, 2) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (1, 0), 100), ((-1, 0), (true, false, false), (1, 0), 100) },
                    lPM:true
                )) },
                { (2, 1, 0), new PlantElementTraits("KelpCeiling", rET:(from number in Enumerable.Range(0, 5) select ((number % 2, -number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 2), mG:(3, 8), tCNTG:new (int type, int subType)[]{ (-2, 0), (-2, 2) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (1, 0), 100), ((-1, 0), (true, false, false), (1, 0), 100) },
                    lPM:true
                )) },
                { (2, 2, 0), new PlantElementTraits("ReedStem", rET:(from number in Enumerable.Range(0, 5) select ((0, number), (true, false))).ToArray(), sRET:new ((int x, int y) pos, (int type, int subType) type, (bool x, bool y) baseDirectionFlip)[] { ((0, 3), (0, 0), (true, false)) },
                pGR:new PlantGrowthRules(t:(1, 0), mG:(4, 4), hPP:fHPP["Up3Gap"],
                    // PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (1, 0)), ((-1, 0), (true, false, false), (1, 0)) }
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((2, 2, 1), (0, 0), 0, 0, (1, 1), 100) }
                )) },
                { (2, 3, 0), new PlantElementTraits("Algae 1 Stem", rET:(from number in Enumerable.Range(0, 5) select ((number % 2, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 0), mG:(4, 3), tCNTG:new (int type, int subType)[]{ (-2, 2) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (1, 1), 65) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((2, 3, -1), (-1, 0), 1, 0, (1, 1), 90), ((2, 3, -1), (1, 0), 1, 0, (1, 1), 90) },
                    lC:true
                )) },
                { (2, 4, 0), new PlantElementTraits("Algae Bulbous", rET:(from number in Enumerable.Range(0, 5) select ((number % 2, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 0), mG:(10, 32), hPP:fHPP["Up12Gap"], tCNTG:new (int type, int subType)[]{ (-2, 2) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (2, 4), 65), ((-1, 0), (true, false, false), (2, 4), 35) },
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((2, 4, -1), (0, 0), 0, 0, 100) }
                )) },
                { (2, 5, 0), new PlantElementTraits("Algae Ceiling 1", rET:(from number in Enumerable.Range(0, 5) select ((number % 2, -number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 0), mG:(3, 8), tCNTG:new(int type, int subType)[] {(-2, 2) }
                    //PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (1, 0), 100), ((-1, 0), (true, false, false), (1, 0), 100) },
                    //lPM:true
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
                { (5, 2, 0), new PlantElementTraits("IceVine", rET:(from number in Enumerable.Range(0, 20) select ((0, -number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 0), mG:(15, 85),
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((0, 0, 1), (0, 0), 0, 0, (3, 4), 100) },
                    lC:true,
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (2, 0), 100), ((-1, 0), (true, false, false), (2, 0), 100) },
                    lPM:true
                )) },

                { (6, 0, 0), new PlantElementTraits("IceGrass", rET:(from number in Enumerable.Range(0, 12) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 0), mG:(1, 5), hPP:fHPP["Up1Gap"],
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, true), (2, 1), 25) }
                )) },
                { (6, 1, 0), new PlantElementTraits("IceBruticStem", rET:(from number in Enumerable.Range(0, 6) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 0), mG:(4, 5), hPP:fHPP["Up1Gap"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((0, 0, 4), (0, 0), 0, 0, (0, 0), 100), ((6, 1, -1), (-1, 0), 1, 0, (3, 1), 100), ((6, 1, -1), (1, 0), 1, 0, (1, 1), 75), ((6, 1, -1), (-1, 0), 1, 0, (1, 1), 50) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, true), (2, 2), 33) }
                )) },
                { (6, 10, 0), new PlantElementTraits("IceTromelStem", rET:(from number in Enumerable.Range(0, 12) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 0), mG:(9, 4), hPP:fHPP["Up5Gap"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((6, 10, 1), (0, 0), 0, 0, (1, 1), 100) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (2, 0), 100) },
                    lPM:true, pMO:4
                )) },
                { (6, 11, 0), new PlantElementTraits("IceKitalStem", rET:(from number in Enumerable.Range(0, 12) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 0), mG:(6, 3), hPP:fHPP["Up4Gap"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((6, 11, 1), (0, 0), 0, 0, (1, 1), 100) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (3, 1), 100) },
                    lPM:true, pMO:2
                )) },
                { (6, 20, 0), new PlantElementTraits("IceFlokanStem", rET:(from number in Enumerable.Range(0, 20) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 0), mG:(16, 4), hPP:fHPP["Up4Gap"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((6, 20, 1), (0, 0), 0, 0, (1, 1), 100) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (4, 0), 100) },
                    lPM:true, pMO:4
                )) },
                { (6, 21, 0), new PlantElementTraits("IceOctamStem", rET:(from number in Enumerable.Range(0, 20) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(1, 0), mG:(26, 12), hPP:fHPP["Y12GapX7Gap"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((6, 21, 1), (0, 0), 0, 0, (1, 1), 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, true, false), (19, 0), 100), ((1, 0), (true, true, false), (3, 1), 100), ((1, 0), (true, true, false), (2, 1), 100), ((1, 0), (true, true, false), (2, 1), 100) },
                    rDG:true,
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (6, 0), 100) },
                    lPM:true, pMO:3
                )) },



                { (10, 0, 0), new PlantElementTraits("LanternTreeTrunk", rET:(from number in Enumerable.Range(0, 25) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(11, 0), mG:(20, 30), hPP:fHPP["Leaves"],
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((10, 0, 1), (0, 0), 0, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((10, 0, -1), (0, 0), 0, 0, (2, 5), 100) },
                    lC:true
                )) },
                { (10, 1, 0), new PlantElementTraits("LanternVine", rET:(from number in Enumerable.Range(0, 10) select ((0, -number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(11, 0), mG:(3, 5), mTC:true,
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((10, 1, -1), (-1, -1), 1, 0, 100), ((10, 1, -1), (1, -1), 1, 0, 100) }
                )) },
                { (10, 2, 0), new PlantElementTraits("SideLantern", rET:(from number in Enumerable.Range(0, 5) select ((0, number - 2), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(11, 0), mG:(8, 10), oMGV:true,
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((10, 0, 1), (-1, -1), 1, 0, 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 1), (true, true, false), (2, 2), 100), ((0, 1), (true, true, false), (1, 1), 100) }
                )) },

                { (11, 0, 0), new PlantElementTraits("WaxStem", rET:(from number in Enumerable.Range(0, 6) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(12, 0), mG:(2, 4), hPP:fHPP["Fire"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((11, 0, 1), (0, 0), 0, 0, (1, 0), 100) }
                )) },
                { (11, 1, 0), new PlantElementTraits("ChandelierStem", rET:(from number in Enumerable.Range(0, 10) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(11, 0), mG:(2, 2), hPP:fHPP["Fire"],
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((11, 1, 1), (0, 0), 0, 0, 100) },
                    lC:true
                )) },
                { (11, 2, 0), new PlantElementTraits("CandelabrumTrunk", rET:(from number in Enumerable.Range(0, 25) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(11, 0), mG:(20, 30), hPP:fHPP["Fire"],
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((11, 1, 1), (0, 0), 0, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((11, 2, -1), (0, 0), 0, 0, (2, 5), 100) },
                    lC:true
                )) },



                { (20, 0, 0), new PlantElementTraits("FleshVine", rET:(from number in Enumerable.Range(0, 5) select ((0, -number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(8, 0), mG:(4, 6),
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (2, 0), 100), ((-1, 0), (true, false, false), (2, 0), 100) },
                    lPM:true
                )) },
                { (20, 1, 0), new PlantElementTraits("FleshTendril", rET:(from number in Enumerable.Range(0, 5) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(8, 0), mG:(4, 6),
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (2, 0), 100), ((-1, 0), (true, false, false), (2, 0), 100) },
                    lPM:true
                )) },
                { (20, 2, 0), new PlantElementTraits("FleshTrunk1", rET:(from number in Enumerable.Range(0, 5) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(8, 0), mG:(2, 3),
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((20, 2, -1), (-1, 1), 1, 0, 100), ((20, 2, -1), (1, 1), 1, 0, 100) }
                )) },
                { (20, 3, 0), new PlantElementTraits("FleshTrunk2", rET:(from number in Enumerable.Range(0, 5) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(8, 0), mG:(2, 3),
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((20, 3, -1), (-1, 1), 1, 0, 100), ((20, 3, -1), (1, 1), 1, 0, 100) }
                )) },

                { (21, 0, 0), new PlantElementTraits("BoneStalactite", rET:(from number in Enumerable.Range(0, 4) select ((0, -number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(8, 1), mG:(2, 4), hPP:fHPP["Down2Gap"],
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((21, 0, -1), (-1, 0), 0, 0, 100), ((21, 0, -1), (1, 0), 0, 0, 100) }
                )) },
                { (21, 1, 0), new PlantElementTraits("BoneStalagmite", rET:(from number in Enumerable.Range(0, 4) select ((0, number), (true, false))).ToArray(),
                pGR:new PlantGrowthRules(t:(8, 1), mG:(2, 4), hPP:fHPP["Up2Gap"],
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((21, 0, -1), (-1, 0), 0, 0, 100), ((21, 0, -1), (1, 0), 0, 0, 100) }
                )) },

                { (22, 0, 0), new PlantElementTraits("HairBody", iC:true,
                pGR:new PlantGrowthRules(t:(8, 2), mG:(8, 22),
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, true, false), (1, 0), 66), ((-1, 0), (true, true, false), (1, 0), 34) },
                    lDG:true, rDG:true, dGO:1
                )) },
                { (22, 1, 0), new PlantElementTraits("HairLong", iC:true,
                pGR:new PlantGrowthRules(t:(8, 2), mG:(12, 88), gSVF:(0.75f, 2),
                    PM:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (1, 1), 100), ((-1, 0), (true, false, false), (3, 2), 100), ((-1, 0), (true, false, false), (1, 1), 100), ((1, 0), (true, false, false), (3, 2), 100) },
                    lPM:true
                )) },
                







                // Branches     subSubType -> -x (like (1, 4, _-2_))

                { (1, 0, -1), new PlantElementTraits("BaseBranch",
                pGR:new PlantGrowthRules(t:(1, 1), mG:(7, 12), sD:((1, 1), (true, false, true)), hPP:fHPP["Leaves"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 0, 1), (0, 0), 0, 0, (1, 1), 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, 1), (true, false, true), (2, 2), 100) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, true), (4, 4), 100) },
                    lPM:true
                )) },
                { (1, 1, -1), new PlantElementTraits("FirBranch",
                pGR:new PlantGrowthRules(t:(1, 1), mG:(1, 1), mGPRV:(0.2f, true), hPP:fHPP["Leaves"],
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((1, 1, 1), (0, 0), 2, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 1, 2), (0, 0), 0, 2, (1, 1), 100) },
                    // DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, 1), (true, false, true), (2, 2), 100) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, -1), (true, false, true), (2, 3), 75) },
                    lPM:true
                )) },
                { (1, 2, -1), new PlantElementTraits("JungleBranch",
                pGR:new PlantGrowthRules(t:(1, 1), mG:(6, 2), sD:((1, 0), (true, false, true)), hPP:fHPP["Leaves"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 2, 1), (0, 0), 0, 0, (1, 1), 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 1), (true, false, false), (2, 2), 100), ((0, 1), (true, false, false), (2, 0), 100) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, 1), (true, false, true), (2, 0), 45) }
                )) },

                { (1, 3, -2), new PlantElementTraits("MangroveRoot",
                pGR:new PlantGrowthRules(t:(1, 1), mG:(20, 10), gSVF:(0.65f, 1.35f),
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, -1), (true, false, false), (2, 2), 100), ((0, -1), (true, false, false), (2, 0), 100) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, -1), (true, false, true), (2, 0), 45) }
                )) },
                { (1, 3, -3), new PlantElementTraits("MangroveRoot2",   // might not be used idk, the spiky ones coming out the ground
                pGR:new PlantGrowthRules(t:(1, 1), mG:(7, 12),
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 3, 1), (0, 0), 0, 0, (1, 1), 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, 1), (true, false, true), (2, 2), 100) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, true), (4, 4), 100) },
                    lPM:true
                )) },
                { (1, 3, -4), new PlantElementTraits("MangroveBranch",
                pGR:new PlantGrowthRules(t:(1, 1), mG:(6, 6), hPP:fHPP["LeavesMangrove"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 3, 1), (0, 0), 0, 0, (1, 1), 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 1), (true, false, false), (3, 3), 100), ((0, 1), (true, false, false), (2, 1), 100) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, 1), (true, false, true), (3, 0), 45) }
                )) },

                { (1, 4, -2), new PlantElementTraits("WeepingWillowBranch",
                pGR:new PlantGrowthRules(t:(1, 1), mG:(14, 4),
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((1, 4, -3), (1, 1), 1, 0, 100)  },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 4, -3), (1, 1), 1, 0, (1, 1), 90), ((1, 4, -3), (-1, 1), 1, 0, (1, 1), 90)  },
                    lC:true, cO:3,
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 1), (true, false, false), (3, 2), 100), ((0, 1), (true, false, false), (2, 1), 100), ((1, 1), (true, false, false), (5, 1), 100), ((1, 0), (true, false, false), (2, 1), 100) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, 1), (true, false, true), (3, 0), 45) }
                )) },
                { (1, 4, -3), new PlantElementTraits("WeepingWillowBranchWeeping",
                pGR:new PlantGrowthRules(t:(1, 1), mG:(8, 4),
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((1, 4, 1), (0, 1), 0, 0, 100) },
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((1, 4, 1), (0, 1), 0, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 4, 1), (0, 1), 0, 0, (1, 0), 90) },
                    lC:true,
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (0, 2), 100), ((1, -1), (true, false, false), (5, 2), 100) }
                )) },
                { (1, 5, -2), new PlantElementTraits("CheeringWillowBranch",
                pGR:new PlantGrowthRules(t:(1, 1), mG:(14, 4),
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((1, 5, -3), (1, 1), 1, 0, 100)  },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 5, -3), (1, 1), 1, 0, (1, 1), 90), ((1, 5, -3), (-1, 1), 1, 0, (1, 1), 90)  },
                    lC:true, cO:3,
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 1), (true, false, false), (3, 2), 100), ((0, 1), (true, false, false), (2, 1), 100), ((1, 1), (true, false, false), (5, 1), 100), ((1, 0), (true, false, false), (2, 1), 100) },
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, 1), (true, false, true), (3, 0), 45) }
                )) },
                { (1, 5, -3), new PlantElementTraits("CheeringWillowBranchCheering",
                pGR:new PlantGrowthRules(t:(1, 1), mG:(8, 4),
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((1, 5, 1), (0, -1), 0, 0, 100) },
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((1, 5, 1), (0, -1), 0, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((1, 5, 1), (0, -1), 0, 0, (1, 0), 90) },
                    lC:true,
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (0, 2), 100), ((1, 1), (true, false, false), (3, 3), 100) }
                )) },

                { (2, 3, -1), new PlantElementTraits("Algae 1 branch",
                pGR:new PlantGrowthRules(t:(1, 0), mG:(4, 2), hPP:fHPP["Up1Gap"],
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 1), (true, false, false), (1, 1), 100), ((0, 1), (false, false, false), (1, 1), 100) }
                )) },
                { (2, 4, -1), new PlantElementTraits("Algae Bulbous mother branch",
                pGR:new PlantGrowthRules(t:(1, 0), mG:(6, 6), hPP:fHPP["Up6Gap"],
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((2, 4, 1), (0, 0), 0, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((2, 4, -2), (-1, 1), 1, 0, (1, 0), 100), ((2, 4, -2), (1, 1), 1, 0, (1, 0), 100) },
                    lC:true
                )) },
                { (2, 4, -2), new PlantElementTraits("Algae Bulbous child branch",
                pGR:new PlantGrowthRules(t:(1, 0), mG:(1, 3), hPP:fHPP["Up4Gap"],
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((2, 4, 1), (0, 0), 0, 0, 100) }
                )) },

                { (3, 0, -1), new PlantElementTraits("ObsidianBranch",
                pGR:new PlantGrowthRules(t:(1, 3), mG:(2, 1), hPP:fHPP["Up1Gap"]
                )) },

                { (6, 1, -1), new PlantElementTraits("IceBruticBranch",
                pGR:new PlantGrowthRules(t:(1, 0), mG:(1, 1), hPP:fHPP["Side1Gap"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((0, 0, 4), (0, 0), 0, 0, (0, 0), 100) }
                    // PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, true), (2, 2), 33) }
                )) },


                { (10, 0, -1), new PlantElementTraits("LanternTreeBranch",
                pGR:new PlantGrowthRules(t:(11, 0), mG:(8, 8), sD:((1, 0), (true, false, true)), hPP:fHPP["Leaves"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((10, 0, 1), (0, 0), 0, 0, (1, 1), 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 1), (true, false, false), (3, 7), 100), ((0, 1), (false, false, false), (1, 1), 100) }
                )) },
                { (10, 1, -1), new PlantElementTraits("LanternVineBranch",
                pGR:new PlantGrowthRules(t:(11, 0), mG:(10, 3), hPP:fHPP["Leaves"],
                    cOGS:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((10, 0, 1), (0, 0), 2, 0, 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (2, 1), 100), ((1, 0), (true, false, false), (1, 0), 100), ((1, 1), (true, false, false), (1, 0), 100), ((1, 0), (true, false, false), (1, 0), 100), ((1, -1), (true, false, false), (1, 1), 100), ((0, -1), (true, false, false), (2, 0), 100) }
                )) },

                { (11, 0, -1), new PlantElementTraits("WaxFruitShit", stick:((0, 1), (false, false)),    // Same as WaxStem except that it sticks (so acts as a fruit/branch ??)
                pGR:new PlantGrowthRules(t:(12, 0), mG:(2, 4),
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((11, 0, 1), (0, 0), 0, 0, (1, 0), 100) }
                )) },
                { (11, 2, -1), new PlantElementTraits("CandelabrumBranch",
                pGR:new PlantGrowthRules(t:(11, 0), mG:(8, 8), sD:((1, 0), (true, false, true)), hPP:fHPP["Fire"],
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((11, 1, 1), (0, 0), 0, 0, (1, 1), 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 1), (true, false, false), (3, 7), 100), ((0, 1), (false, false, false), (1, 1), 100) }
                )) },


                { (20, 2, -1), new PlantElementTraits("FleshBranch1-1",
                pGR:new PlantGrowthRules(t:(8, 0), mG:(7, 10),
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((20, 2, -2), (0, 0), 2, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((20, 2, -2), (0, 0), 2, 0.5f, (2, 1), 100) },
                    lC:true,
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, 1), (false, false, false), (2, 4), 100) },
                    lPM:true,
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (1, 1), 100) }
                )) },
                { (20, 2, -2), new PlantElementTraits("FleshBranch1-2", rET:new ((int x, int y) pos, (bool x, bool y) baseDirectionFlip)[]{ ((1, 1), (true, false)), ((1, 2), (true, false)), ((1, 3), (true, false)), ((1, 4), (true, false)), ((1, 5), (true, false)), ((1, 6), (true, false)), ((1, 7), (true, false)), ((1, 8), (true, false)), ((1, 9), (true, false)), },
                pGR:new PlantGrowthRules(t:(8, 0), mG:(6, 4), sD:((0, 1), (false, false, false)), hPP:fHPP["Up1Gap"],
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, 1), (true, false, false), (2, 0), 100) }
                )) },

                { (20, 3, -1), new PlantElementTraits("FleshBranch2-1",
                pGR:new PlantGrowthRules(t:(8, 0), mG:(11, 10),
                    cOGE:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance)[] { ((20, 3, -2), (0, 0), 2, 0, 100) },
                    C:new ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance)[] { ((20, 3, -2), (0, 0), 2, 0, (2, 1), 100) },
                    cO:8, lC:true,
                    PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (3, 2), 100),       ((0, 1), (true, false, false), (5, 2), 100),   ((0, -1), (true, false, false), (4, 2), 100), ((0, -1), (true, false, false), (2, 2), 100) },
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, 1), (true, false, false), (1, 0), 100), ((1, 1), (true, false, false), (5, 2), 100), ((1, 0), (true, false, false), (1, 1), 100) }
                )) },
                { (20, 3, -2), new PlantElementTraits("FleshBranch2-2", rET:new ((int x, int y) pos, (bool x, bool y) baseDirectionFlip)[]{ ((1, -1), (true, false)), ((1, -2), (true, false)) },
                pGR:new PlantGrowthRules(t:(8, 0), mG:(5, 3), sD:((0, -1), (false, false, false)), hPP:fHPP["Down1Gap"],
                    DG:new ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((0, -1), (true, false, false), (2, 0), 100) }
                )) },

                { (21, 0, -1), new PlantElementTraits("BoneBranch",
                pGR:new PlantGrowthRules(t:(8, 1), mG:(1, 0)
                )) },







                // Flowers and shit (generation based on fixed frames)   subSubType -> +x (like (1, 4, _3_))
                
                //(0, 0, x) are elements shared between many plants. Since (0, 0, 0) is just normal grass it has no flowers and branches
                { (0, 0, 1), new PlantElementTraits("PlusFlower",
                framez:makeStructureFrameArray(null, "PlusFlower-1", "PlusFlower-2", "PlusFlower-3")
                ) },
                { (0, 0, 2), new PlantElementTraits("PlusFlowerSticky", stick:((0, 0), (false, false)),
                framez:makeStructureFrameArray(null, "PlusFlower-1", "PlusFlower-2", "PlusFlower-3")
                ) },
                { (0, 0, 3), new PlantElementTraits("CrossFlower",
                framez:makeStructureFrameArray(null, "CrossFlower-1", "CrossFlower-2", "CrossFlower-3")
                ) },
                { (0, 0, 4), new PlantElementTraits("CrossFlowerSticky", stick:((0, 0), (false, false)),
                framez:makeStructureFrameArray(null, "CrossFlower-1", "CrossFlower-2", "CrossFlower-3")
                ) },

                { (0, 1, 1), new PlantElementTraits("TulipFlower", stick:((0, 1), (false, false)),
                framez:makeStructureFrameArray(null, "TulipFlower-1", "TulipFlower-2", "TulipFlower-3", "TulipFlower-4", "TulipFlower-5")
                ) },
                { (0, 2, 1), new PlantElementTraits("AlliumFlower", stick:((0, 1), (false, false)),
                framez:makeStructureFrameArray(null, "AlliumFlower-1", "AlliumFlower-2", "AlliumFlower-3", "AlliumFlower-4")
                ) },
                { (0, 3, 1), new PlantElementTraits("BigFlower", stick:((0, 1), (false, false)),
                framez:makeStructureFrameArray(null, "BigFlower-1", "BigFlower-2", "BigFlower-3", "BigFlower-4", "BigFlower-5")
                ) },

                { (1, 0, 1), new PlantElementTraits("TreeLeaves", stick:((0, 0), (false, false)),
                framez:makeStructureFrameArray(null, "TreeLeaves-1", "TreeLeaves-2", "TreeLeaves-3", "TreeLeaves-4")
                ) },
                { (1, 1, 1), new PlantElementTraits("FirLeaves", stick:((0, 0), (false, false)),
                framez:makeStructureFrameArray(null, "FirLeaves-1", "FirLeaves-2", "FirLeaves-3", "FirLeaves-4", "FirLeaves-5")
                ) },
                { (1, 1, 2), new PlantElementTraits("FirLeavesNotSticky",
                framez:makeStructureFrameArray(null, "FirLeaves-1", "FirLeaves-2", "FirLeaves-3", "FirLeaves-4", "FirLeaves-5")
                ) },
                { (1, 1, 3), new PlantElementTraits("FirLeavesTop", stick:((0, 0), (false, false)),
                framez:makeStructureFrameArray(null, "FirLeavesTop-1", "FirLeavesTop-2", "FirLeavesTop-3", "FirLeavesTop-4", "FirLeavesTop-5")
                ) },
                { (1, 2, 1), new PlantElementTraits("JungleLeaves", stick:((0, 0), (false, false)),
                framez:makeStructureFrameArray(null, "JungleLeaves-1", "JungleLeaves-2", "JungleLeaves-3", "JungleLeaves-4", "JungleLeaves-5")
                ) },
                { (1, 3, 1), new PlantElementTraits("MangroveLeaves", stick:((0, 0), (false, false)),
                framez:makeStructureFrameArray(null, "MangroveLeaves-1", "MangroveLeaves-2", "MangroveLeaves-3", "MangroveLeaves-4", "MangroveLeaves-5")
                ) },
                { (1, 4, 1), new PlantElementTraits("WeepingWillowLeaves",
                pGR:new PlantGrowthRules(t:(1, 0), sD:((0, -1), (true, false, false)), mG:(1, 5), mGPRV:(0.35f, true)
                    //PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (1, 0), 100), ((-1, 0), (true, false, false), (1, 0), 100) },
                    //lPM:true
                ), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), null) }
                ) },
                { (1, 5, 1), new PlantElementTraits("CheeringWillowLeaves",
                pGR:new PlantGrowthRules(t:(1, 0), sD:((0, 1), (true, false, false)), mG:(1, 5), mGPRV:(0.35f, true)
                    //PM:new ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance)[] { ((1, 0), (true, false, false), (1, 0), 100), ((-1, 0), (true, false, false), (1, 0), 100) },
                    //lPM:true
                ), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), null) }
                ) },

                { (6, 10, 1), new PlantElementTraits("IceTromel", stick:((0, 1), (false, false)),
                framez:makeStructureFrameArray(null, "IceTromel-1", "IceTromel-2", "IceTromel-3", "IceTromel-4", "IceTromel-5")
                ) },
                { (6, 11, 1), new PlantElementTraits("IceKital", stick:((0, 1), (false, false)),
                framez:makeStructureFrameArray(null, "IceKital-1", "IceKital-2", "IceKital-3", "IceKital-4", "IceKital-5")
                ) },
                { (6, 20, 1), new PlantElementTraits("IceFlokan", stick:((0, 1), (false, false)),
                framez:makeStructureFrameArray(null, "IceFlokan-1", "IceFlokan-2", "IceFlokan-3", "IceFlokan-4", "IceFlokan-5", "IceFlokan-6", "IceFlokan-7")
                ) },
                { (6, 21, 1), new PlantElementTraits("IceOctam", stick:((0, 1), (false, false)),
                framez:makeStructureFrameArray(null, "IceOctam-1", "IceOctam-2", "IceOctam-3", "IceOctam-4", "IceOctam-5", "IceOctam-6", "IceOctam-7", "IceOctam-8", "IceOctam-9", "IceOctam-10")
                ) },

                { (2, 2, 1), new PlantElementTraits("ReedFlower", stick:((0, 1), (false, false)),
                framez:makeStructureFrameArray(null, "ReedFlower-1", "ReedFlower2-", "ReedFlower3-")
                ) },
                { (2, 4, 1), new PlantElementTraits("BulbousAlgaeLeaves", stick:((0, 0), (false, false)),
                framez:makeStructureFrameArray(null, "BulbousAlgaeLeaves-1", "BulbousAlgaeLeaves-2", "BulbousAlgaeLeaves-3", "BulbousAlgaeLeaves-4"),
                cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), null) }
                ) },

                { (4, 0, 1), new PlantElementTraits("MushroomCap", stick:((0, 1), (false, false)),
                framez:makeStructureFrameArray(new (int value, int range)[]{ (1, 0), (2, 3), (1, 3) }, "MushroomCap-1", "MushroomCap-2", "MushroomCap-3")
                ) },

                { (10, 0, 1), new PlantElementTraits("Lantern", stick:((0, 0), (false, false)), lE:(11, 1), lR:13,
                framez:makeStructureFrameArray(null, "Lantern-1", "Lantern-2", "Lantern-3", "Lantern-4", "Lantern-5")
                ) },

                { (11, 0, 1), new PlantElementTraits("CandleFlower", stick:((0, 1), (false, false)), fLAP:true, anm:fireAnimation, lE:(11, 1), lR:9) },
                { (11, 1, 1), new PlantElementTraits("CandleHolder", stick:((0, 1), (false, false)), dC:((11, 0, -1), (0, 0), 100),  // dC is WaxFruitShit
                framez:makeStructureFrameArray(null, "CandleHolder-1", "CandleHolder-2")
                ) },
            };
        }














        public class PlantTraits
        {
            public string name;

            public (int type, int subType, int subSubType) plantElementType;
            public (int type, int subType)? initFailType;
            public int minGrowthForValidity;

            public HashSet<(int type, int subType)> soilType;
            public ((int type, int subType) tile, (int x, int y) range)? tileNeededClose;

            public (int r, int g, int b)? fullPlantShade;
            public bool doesFullPlantShadeOverride;
            public ((int type, int subType) type, ColorRange colorRange)[] colorOverrideArray;

            public bool isTree;
            public bool isCeiling;
            public bool isSide;
            public bool isEveryAttach;
            public bool isWater;
            public bool isLuminous;
            public bool isClimbable;

            public ((int baseValue, int variation) chance, (int x, int y) range)? propagateOnSuccess;
            public PlantTraits(string namee, (int type, int subType, int subSubType)? t = null, (int type, int subType)? iFT = null, int mGFV = 1,
                HashSet<(int type, int subType)> sT = null, ((int type, int subType) tile, (int x, int y) range)? tNC = null,
                ((int type, int subType) type, ColorRange colorRange)[] cOverride = null, (int r, int g, int b)? fPS = null, bool dFPSO = false,
                bool T = false, bool C = false, bool S = false, bool EA = false, bool W = false, bool lum = false, bool cl = false, ((int baseValue, int variation) chance, (int x, int y) range)? pOS = null)
            {
                name = namee;

                plantElementType = t ?? (-1, 0, 0);
                initFailType = iFT;
                minGrowthForValidity = mGFV;

                soilType = sT;
                tileNeededClose = tNC;

                colorOverrideArray = cOverride;
                fullPlantShade = fPS;
                doesFullPlantShadeOverride = fullPlantShade != null && dFPSO;

                isTree = T;
                isCeiling = C;
                isSide = S;
                isEveryAttach = EA;
                isWater = W;
                isLuminous = lum;
                isClimbable  = cl;

                propagateOnSuccess = pOS;
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
                { (1, 1), new PlantTraits("Fir",                                    T:true,
                t:(1, 1, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), new ColorRange((10, 0, 20), (75, -15, 20), (55, 15, 20))), ((1, 1), new ColorRange((80, -10, 20), (55, 10, 20), (35, -10, 20))) }) },
                { (1, 2), new PlantTraits("Jungle Tree",                            T:true,
                t:(1, 2, 0)) },
                { (1, 3), new PlantTraits("Mangrove Tree",                          T:true, W:true,
                t:(1, 3, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 1), new ColorRange((115, -10, 20), (85, 10, 20), (65, -10, 20))) }) },
                { (1, 4), new PlantTraits("Weeping Willow",                         T:true, tNC:((-2, 0), (3, 3)),
                t:(1, 4, 0), fPS:(35, 35, 35), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), new ColorRange((60, -10, 20), (145, 20, 20), (80, -15, 20))), ((1, 1), new ColorRange((100, -10, 15), (85, 5, 15), (80, 10, 15))) }) },
                { (1, 5), new PlantTraits("Cheering Willow",                        T:true, tNC:((-3, 0), (3, 3)),
                t:(1, 5, 0), fPS:(50, 50, 50), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), new ColorRange((220, -10, 20), (140, 20, 20), (180, -15, 20))), ((1, 1), new ColorRange((100, -10, 15), (85, 5, 15), (80, 10, 15))) }) },


                { (2, 0), new PlantTraits("KelpUpwards",                                    W:true,
                t:(2, 0, 0), iFT:(2, 2)) },
                { (2, 1), new PlantTraits("KelpDownwards",                          C:true, W:true,
                t:(2, 1, 0)) },
                { (2, 2), new PlantTraits("Reed",                                           W:true,
                t:(2, 2, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((2, 0), new ColorRange((120, 30, 30), (40, 0, 20), (20, -10, 10))), ((2, 1), new ColorRange((235, 0, 10), (225, 5, 10), (190, 15, 10))) }) },
                { (2, 3), new PlantTraits("Algae 1",                                        W:true,
                t:(2, 3, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), new ColorRange((65, 0, 20), (110, -10, 20), (50, 30, 20))) }) },
                { (2, 4), new PlantTraits("Algae Bulbous",                                  W:true,
                t:(2, 4, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), new ColorRange((45, 0, 10), (80, 10, 10), (30, 20, 10))) }) },
                { (2, 5), new PlantTraits("Algae Ceiling 1",                        C:true, W:true,
                t:(2, 5, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), new ColorRange((65, 0, 20), (110, -10, 20), (50, 30, 20))) }) },

                { (3, 0), new PlantTraits("ObsidianPlant",
                t:(3, 0, 0)) },

                { (4, 0), new PlantTraits("Mushroom",
                t:(4, 0, 0)) },
                { (4, 1), new PlantTraits("Mold",                                  EA:true,
                t:(4, 1, 0)) },

                { (5, 0), new PlantTraits("Vine",                                   C:true,
                t:(5, 0, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), new ColorRange((50, 0, 30), (120, 50, 30), (50, 0, 30))) }) },
                { (5, 1), new PlantTraits("ObsidianVine",                           C:true,
                t:(5, 1, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((2, 0), famousColorRanges["Obsidian"]), ((2, 1), famousColorRanges["ObsidianPollen"]) }) },
                { (5, 2), new PlantTraits("IceVine",                                C:true,
                t:(5, 2, 0), sT:new HashSet<(int type, int subType)> { (-2, -1) },
                cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), famousColorRanges["IceStem"]), ((2, 0), famousColorRanges["IcePetal"]), ((2, 1), famousColorRanges["IcePollen"]) }) },

                { (6, 0), new PlantTraits("IceGrass",
                t:(6, 0, 0), pOS:((7, 17), (15, 10)), sT:new HashSet<(int type, int subType)> { (-2, -1) },
                cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), famousColorRanges["IceStem"]), ((2, 0), famousColorRanges["IcePetal"]), ((2, 1), famousColorRanges["IcePollen"]) }) },
                { (6, 1), new PlantTraits("IceBrutic",
                t:(6, 1, 0), pOS:((0, 4), (8, 5)), sT:new HashSet<(int type, int subType)> { (-2, -1) },
                cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), famousColorRanges["IceStem"]), ((2, 0), famousColorRanges["IcePetal"]), ((2, 1), famousColorRanges["IcePollen"]) }) },
                { (6, 10), new PlantTraits("IceTromel",
                t:(6, 10, 0), sT:new HashSet<(int type, int subType)> { (-2, -1) },
                cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), famousColorRanges["IceStem"]), ((2, 0), famousColorRanges["IcePetal"]), ((2, 1), famousColorRanges["IcePollen"]) }) },
                { (6, 11), new PlantTraits("IceKital",
                t:(6, 11, 0), sT:new HashSet<(int type, int subType)> { (-2, -1) },
                cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), famousColorRanges["IceStem"]), ((2, 0), famousColorRanges["IcePetal"]), ((2, 1), famousColorRanges["IcePollen"]) }) },
                { (6, 20), new PlantTraits("IceFlokan",
                t:(6, 20, 0), sT:new HashSet<(int type, int subType)> { (-2, -1) },
                cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), famousColorRanges["IceStem"]), ((2, 0), famousColorRanges["IcePetal"]), ((2, 1), famousColorRanges["IcePollen"]) }) },
                { (6, 21), new PlantTraits("IceOctam",
                t:(6, 21, 0), sT:new HashSet<(int type, int subType)> { (-2, -1) },
                cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((1, 0), famousColorRanges["IceStem"]), ((2, 0), famousColorRanges["IcePetal"]), ((2, 1), famousColorRanges["IcePollen"]) }) },


                { (10, 0), new PlantTraits("LanternTree",                           T:true, lum:true,
                t:(10, 0, 0)) },
                { (10, 1), new PlantTraits("LanternVine",                           C:true, lum:true,
                t:(10, 1, 0)) },
                { (10, 2), new PlantTraits("SideLantern",                           S:true, lum:true,
                t:(10, 2, 0)) },

                { (11, 0), new PlantTraits("Candle",                                        lum:true,
                t:(11, 0, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((11, 1), new ColorRange((200, 0, 10), (120, 0, 10), (40, 0, 10))) }) },
                { (11, 1), new PlantTraits("Chandelier",                                    lum:true,
                t:(11, 1, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((11, 1), new ColorRange((200, 0, 10), (120, 0, 10), (40, 0, 10))) }) },
                { (11, 2), new PlantTraits("Candelabrum",                           T:true, lum:true,
                t:(11, 2, 0), cOverride:new ((int type, int subType) type, ColorRange colorRange)[]{ ((11, 1), new ColorRange((200, 0, 10), (120, 0, 10), (40, 0, 10))) }) },


                { (20, 0), new PlantTraits("FleshVine",                             C:true,
                t:(20, 0, 0), mGFV:4, sT:new HashSet<(int type, int subType)> { (4, 0), (4, 2) }) },
                { (20, 1), new PlantTraits("FleshTendril",
                t:(20, 1, 0), mGFV:4, sT:new HashSet<(int type, int subType)> { (4, 0), (4, 2) }) },
                { (20, 2), new PlantTraits("FleshTree1",                            T:true,
                t:(20, 2, 0), mGFV:1, sT:new HashSet<(int type, int subType)> { (4, 0), (4, 2) }) },
                { (20, 3), new PlantTraits("FleshTree2",                            T:true,
                t:(20, 3, 0), mGFV:1, sT:new HashSet<(int type, int subType)> { (4, 0), (4, 2) }) },


                { (21, 0), new PlantTraits("BoneStalactite",                        C:true,
                t:(21, 0, 0), mGFV:4, sT:new HashSet<(int type, int subType)> { (4, 1) }) },
                { (21, 1), new PlantTraits("BoneStalagmite",
                t:(21, 1, 0), mGFV:4, sT:new HashSet<(int type, int subType)> { (4, 1) }) },

                { (22, 0), new PlantTraits("Body Hair",                            EA:true, cl:true,
                t:(22, 0, 0), mGFV:4, sT:new HashSet<(int type, int subType)> { (4, 0), (4, 2) }) },
                { (22, 1), new PlantTraits("Long Hair",                             C:true, cl:true,
                t:(22, 1, 0), mGFV:4, sT:new HashSet<(int type, int subType)> { (4, 0), (4, 2) }) },
            };
        }






        public class TerrainFeaturesTraits
        {
            public (int type, int subType) tileType;
            public int layer;

            public int transitionRules;
            public bool meanBasedValueRequired;

            public bool inSoil;
            public bool inLiquid;
            public bool inAir;
            public bool needsQuartileFilled;
            public bool ignoreIgnore;

            public int baseThreshold;
            public (int one, int two) noiseModulos;
            public (bool one, bool two) makeNoiseMaps;

            public bool isBiomeSystem;
            public (int threshold, bool reverse)? temperature;
            public (int threshold, bool reverse)? humidity;
            public (int threshold, bool reverse)? acidity;
            public (int threshold, bool reverse)? toxicity;
            public (int threshold, bool reverse)? salinity;
            public (int threshold, bool reverse)? illumination;
            public (int threshold, bool reverse)? oceanity;
            public int biomeValuesScale;
            public (int strength, int threshold)? biomeEdgeReduction;
            public TerrainFeaturesTraits((int type, int subType) tT, int tR, bool mBVR = false, int bT = 512, (int? one, int? two)? nM = null,
                (int threshold, bool reverse)? T = null, (int threshold, bool reverse)? H = null, (int threshold, bool reverse)? A = null,
                (int threshold, bool reverse)? TX = null, (int threshold, bool reverse)? S = null, (int threshold, bool reverse)? I = null,
                (int threshold, bool reverse)? O = null, int bVS = 512, (int strength, int threshold)? bER = null, bool fBS = false, bool iS = false, bool iL = false, bool iA = false, bool nQF = false, bool iI = false)
            {
                tileType = tT;

                transitionRules = tR;
                meanBasedValueRequired = mBVR;

                inSoil = iS;
                inLiquid = iL;
                inAir = iA;
                needsQuartileFilled = nQF;
                ignoreIgnore = iI;

                baseThreshold = bT;
                noiseModulos = nM is null ? (16, 16) : (nM.Value.one ?? 16, nM.Value.two ?? 16);
                makeNoiseMaps = nM is null ? (true, true) : (nM.Value.one != null, nM.Value.two != null);

                if (fBS || T != null || H != null || A != null || TX != null || S != null || I != null || O != null) { isBiomeSystem = true; }
                else { isBiomeSystem = false; }
                temperature = T;
                humidity = H;
                acidity = A;
                toxicity = TX;
                salinity = S;
                illumination = I;
                oceanity = O;
                biomeValuesScale = bVS;
                biomeEdgeReduction = bER;
            }
        }
        public static Dictionary<string, TerrainFeaturesTraits> famousTFT;
        public static void makeFamousTerrainFeaturesTraitsDict()
        {
            famousTFT = new Dictionary<string, TerrainFeaturesTraits>
            {
                { "HardRock", new TerrainFeaturesTraits((1, 1), 0, iS:true, mBVR:true, bT:0) },
                { "Bone", new TerrainFeaturesTraits((4, 1), 1, iS:true, bT:512, H:(500, false), bVS:1024) },
                { "Mold", new TerrainFeaturesTraits((5, 0), 2, iS:true, bT:1024, bER:(2000, 0), bVS:1024, nM:(64, 16)) },
                { "Salt Terrain", new TerrainFeaturesTraits((6, 0), 3, iS:true, bT:0, bER:(10000, 700), S:(650, true), nM:(256, 64)) },
                { "Salt Filling", new TerrainFeaturesTraits((6, 0), 4, iL:true, bT:0, fBS:true, bER:(10000, 700), nM:(null, null)) },
                { "Salt Spikes", new TerrainFeaturesTraits((6, 0), 5, iL:true, bT:0, bER:(10000, 700), nQF:true) },
                { "Frost Carving", new TerrainFeaturesTraits((0, 0), 6, iS:true, bT:0, bER:(100, 0), nM:(null, null), iI:true) },
            };
            int counto = 0;
            foreach (TerrainFeaturesTraits tTT in famousTFT.Values) { tTT.layer = counto * 2; counto++; }
        }







        public class BiomeTraits        // -> Additional spawn attempts ? Like for modding idfk, on top of existing ones... idk uirehqdmsoijq
        {
            public (int type, int subType) type;
            public string name;
            public int difficulty = 1;
            public (int r, int g, int b) color;

            public (int type, int subType) fillType;
            public (int type, int subType) tileType;
            public ((int type, int subType) type, int chance)? surfaceMaterial;

            public (int type, int subType) lakeType;
            public (int minHeight, int minTiles, int maxTiles) lakeSize;
            public bool invertedLakes;

            public (int one, int two) caveType;
            public bool isVoronoiCave;
            public (int one, int two) textureType;
            public int connectionLayer;
            public int separatorType;
            public int antiSeparatorType;
            public float caveWidth;

            public TerrainFeaturesTraits[] terrainFeaturesTraitsArray;

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
            public float plantWaterTreeSpawnRate;
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
            public ((int type, int subType) type, float percentage)[] plantWaterTreeSpawnTypes;
            public ((int type, int subType) type, float percentage)[] plantWaterCeilingSpawnTypes;
            public ((int type, int subType) type, float percentage)[] plantWaterSideSpawnTypes;

            public ((int type, int subType) type, int percentage)[] extraPlantsSpawning;

            public BiomeTraits(string namee, (int r, int g, int b) colorToPut, float[] spawnRates, ((int type, int subType) type, float percentage)[] entityTypes, ((int type, int subType) type, float percentage)[] plantTypes, ((int type, int subType) type, int percentage)[] ePS = null,
                (int one, int two)? cT = null, (int one, int two)? txT = null, int cL = 0, int sT = 0, int aST = 0, float cW = 1, TerrainFeaturesTraits[] tFT = null,
                (int type, int subType)? fT = null, (int type, int subType)? tT = null, ((int type, int subType) type, int chance)? sM = null, (int type, int subType)? lT = null, (int minHeight, int minTiles, int maxTiles)? lS = null,
                bool S = false, bool Dg = false, bool Da = false)
            {
                name = namee;
                color = colorToPut;

                isDark = Da;
                isSlimy = S;
                isDegraded = Dg;

                fillType = fT ?? (0, 0);
                tileType = tT ?? (1, 0);
                surfaceMaterial = sM;


                lakeType = lT ?? (-2, 0);
                lakeSize = lS ?? (2, 6, 1234);
                invertedLakes = lakeType == (0, 0);

                caveType = cT ?? (1, 2);
                isVoronoiCave = caveType.one == 7 || caveType.two == 7;
                textureType = txT ?? (0, 1);
                connectionLayer = cL;
                separatorType = sT;
                antiSeparatorType = aST;
                caveWidth = cW;

                terrainFeaturesTraitsArray = tFT;

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
                List<((int type, int subType) type, float percentage)> plantWaterTreeSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> plantWaterCeilingSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> plantWaterSideSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                foreach (((int type, int subType) type, float percentage) tupelo in plantTypes)
                {
                    if (!plantTraitsDict.ContainsKey(tupelo.type)) { continue; }
                    plantTraits = plantTraitsDict[tupelo.type];
                    if (plantTraits.isEveryAttach) { plantEveryAttachSpawnTypesList.Add(tupelo); }
                    else if (plantTraits.isWater)
                    {
                        if (plantTraits.isCeiling) { plantWaterCeilingSpawnTypesList.Add(tupelo); }
                        else if (plantTraits.isTree) { plantWaterTreeSpawnTypesList.Add(tupelo); }
                        else if (plantTraits.isSide) { plantWaterSideSpawnTypesList.Add(tupelo); }
                        else { plantWaterGroundSpawnTypesList.Add(tupelo); }
                    }
                    else
                    {
                        if (plantTraits.isCeiling) { plantCeilingSpawnTypesList.Add(tupelo); }
                        else if (plantTraits.isTree) { plantTreeSpawnTypesList.Add(tupelo); }
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
                plantWaterTreeSpawnTypes = plantWaterTreeSpawnTypesList.ToArray();
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
                plantWaterTreeSpawnRate = plantWaterTreeSpawnTypes.Length == 0 ? 0 : spawnRates[10];
                plantWaterCeilingSpawnRate = plantWaterCeilingSpawnTypes.Length == 0 ? 0 : spawnRates[11];
                plantWaterSideSpawnRate = plantWaterSideSpawnTypes.Length == 0 ? 0 : spawnRates[12];

                extraPlantsSpawning = ePS;
            }
            public void setType((int type, int subType) typeToSet) { type = typeToSet; }
        }
        public static BiomeTraits getBiomeTraits((int type, int subType) biomeType) { return biomeTraitsDict.ContainsKey(biomeType) ? biomeTraitsDict[biomeType] : biomeTraitsDict[(-1, 0)]; }

        public static Dictionary<(int type, int subType), BiomeTraits> biomeTraitsDict;
        public static void makeBiomeTraitsDict()
        {
            ((int type, int subType) type, int percentage)[] WILLOW = new ((int type, int subType) type, int percentage)[] { ((1, 4), 100) };
            ((int type, int subType) type, int percentage)[] CHEERINGWILLOW = new ((int type, int subType) type, int percentage)[] { ((1, 5), 100) };

            biomeTraitsDict = new Dictionary<(int type, int subType), BiomeTraits>()
            {   //      -E- C  G  W  J   -P- E  G  T  C  S  WG WT WC WS
                { (-1, 0), new BiomeTraits("Error",                 (1200, -100, 1200),
                new float[]{0, 0, 0, 0,      0, 0, 0, 0, 0, 0, 0, 0, 0},
                new ((int type, int subType) type, float percentage)[]{ },
                new ((int type, int subType) type, float percentage)[]{ },
                tT:(0, -1), lT:(0, -2), fT:(0, -3)) },

                { (0, 0),  new BiomeTraits("Cold",                  (Color.Blue.R, Color.Blue.G, Color.Blue.B),     // -> put smaller spawn rates for this one ? Since cold. And nothing for frost
                new float[]{1, 0.25f, 2, 2,  0, 4, 1, 2, 0, 4, 0, 4, 0}, // Frog       Worm           Fish           WaterSkipper   Dragonfly
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 75), ((10, 0), 25), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), }
                ) },                                                 // Base           Vine           Kelp           CeilingKelp
                { (0, 1),  new BiomeTraits("Frost",                 (Color.LightBlue.R, Color.LightBlue.G, Color.LightBlue.B),
                new float[]{1, 0.25f, 2, 2,  0, 4, 1, 2, 0, 4, 0, 4, 0}, // Frost Fairy
                new ((int type, int subType) type, float percentage)[]{ ((0, 2), 100), },
                new ((int type, int subType) type, float percentage)[]{ },
                lT:(-2, -1)) },                                      // Nothing lol
                { (1, 0),  new BiomeTraits("Acid",                  (Color.Fuchsia.R, Color.Fuchsia.G, Color.Fuchsia.B),
                new float[]{1, 0.25f, 2, 2,  0, 4, 1, 2, 0, 4, 0, 4, 0}, // Worm       Fish           WaterSkipper   Dragonfly
                new ((int type, int subType) type, float percentage)[]{ ((4, 0), 100), ((2, 0), 100), ((5, 0), 75), ((10, 0), 25), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                Dg:true) },                                          // Base           Vine           Kelp           CeilingKelp
                //      -E- C  G  W  J   -P- E  G  T  C  S  WG WT WC WS
                { (2, 0),  new BiomeTraits("Hot",                   (Color.OrangeRed.R, Color.OrangeRed.G, Color.OrangeRed.B),
                new float[]{1, 0.25f, 2, 2,  0, 4, 1, 2, 0, 4, 0, 4, 0}, // Frog       Fish           WaterSkipper   Dragonfly
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((2, 0), 100), ((5, 0), 75), ((10, 0), 25), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                lT:(-4, 0)) },                                       // Base           Vine           Kelp           CeilingKelp
                { (2, 1),  new BiomeTraits("Lava Ocean",            (Color.OrangeRed.R + 90, Color.OrangeRed.G + 30, Color.OrangeRed.B),
                new float[]{1, 0.25f, 2, 2,  0, 4, 1, 2, 0, 4, 0, 4, 0}, // Fish
                new ((int type, int subType) type, float percentage)[]{ ((2, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                lT:(0, 0), lS:(3, 50, 3000),                         // Base           Vine           Kelp           CeilingKelp
                cT:(0, 3), txT:(0, 0), sT:1, fT:(-4, 0)) },
                { (2, 2),  new BiomeTraits("Obsidian",              (-100, -100, -100),
                new float[]{1, 0.25f, 2, 2,  0, 4, 1, 2, 0, 4, 0, 4, 0}, // Obsidian Fairy
                new ((int type, int subType) type, float percentage)[]{ ((0, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((3, 0), 100), ((5, 1), 100), ((2, 0), 100), ((2, 1), 100), },
                cT:(1, 4), txT:(0, 0), cW:0.3f) },                   // ObsidianPlant  Vine           Kelp           CeilingKelp
                //      -E- C  G  W  J   -P- E  G  T  C  S  WG WT WC WS
                { (3, 0),  new BiomeTraits("Forest",                (Color.Green.R, Color.Green.G, Color.Green.B),
                new float[]{1, 0.25f, 2, 2,  0, 6, 2, 2, 0, 4, 0, 4, 0}, // Frog       Worm           Fish           WaterSkipper   Dragonfly
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 75), ((10, 0), 25), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((1, 0), 96),  ((1, 1), 4),   ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                cT:(1, 5), txT:(0, 0),                               // Base           Tree           Fir            Vine           Kelp           CeilingKelp
                ePS:WILLOW) },
                { (3, 1),  new BiomeTraits("Flower Forest",         (Color.Green.R, Color.Green.G + 40, Color.Green.B + 80),
                new float[]{1, 0.25f, 2, 2, 0, 16, 1, 3, 0, 4, 0, 4, 0}, // Frog       Worm           Fish           WaterSkipper   Dragonfly
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 75), ((10, 0), 25), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 10), ((0, 1), 20), ((0, 2), 20), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                txT:(0, 0),                                          // Base          Tulip         Allium        Vine           Kelp           CeilingKelp
                ePS:WILLOW) },
                { (3, 2),  new BiomeTraits("Conifer Forest",        (Color.Green.R - 120, Color.Green.G - 40, Color.Green.B + 40),
                new float[]{1, 0.25f, 2, 2,  0, 6, 3, 2, 0, 4, 0, 4, 0}, // Frog       Worm           Fish           WaterSkipper   Dragonfly
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 75), ((10, 0), 25), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((1, 1), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                cT:(1, 5), txT:(0, 0),                               // Base           Fir            Vine           Kelp           CeilingKelp
                ePS:WILLOW) },
                { (3, 3),  new BiomeTraits("Jungle",                (Color.Green.R + 80, Color.Green.G + 160, Color.Green.B + 40),
                new float[]{1, 0.25f, 2, 2,  0, 6, 4, 2, 0, 4, 0, 4, 0}, // Frog       Worm           Fish           WaterSkipper   Dragonfly
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 75), ((10, 0), 25), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((1, 2), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                cT:(1, 5), txT:(0, 0),                               // Base           JungleTree     Vine           Kelp           CeilingKelp
                ePS:WILLOW) },
                { (3, 4),  new BiomeTraits("Mangrove",                (Color.DarkSeaGreen.R + 20, Color.DarkSeaGreen.G + 60, Color.DarkSeaGreen.B + 30),
                new float[]{1, 0.25f, 2, 2,  0, 6, 1, 2, 0, 4, 3, 4, 0}, // Frog       Worm           Fish           WaterSkipper   Dragonfly
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 75), ((10, 0), 25), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((1, 3), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                cT:(7, 7), lT:(-2, 2), txT:(0, 0)) },                // Base           MangroveTree   Vine           Kelp           CeilingKelp

                { (4, 0),  new BiomeTraits("Toxic",                 (Color.GreenYellow.R, Color.GreenYellow.G, Color.GreenYellow.B),
                new float[]{1, 0.25f, 2, 2,  0, 4, 1, 2, 0, 4, 0, 4, 0}, // Frog       Worm           Fish           WaterSkipper   Dragonfly
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 75), ((10, 0), 25), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                lT:(-8, 0), txT:(0, 0), S:true,                      // Base           Vine           Kelp           CeilingKelp
                ePS:WILLOW) },

                { (5, 0),  new BiomeTraits("Fairy",                 (Color.LightPink.R, Color.LightPink.G, Color.LightPink.B),
                new float[]{1, 0.25f, 2, 2,  0, 4, 1, 2, 0, 4, 0, 4, 0}, // Fairy       Worm          Fish           WaterSkipper   Dragonfly
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 100), ((10, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((4, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), },
                lT:(-3, 0), lS:(2, 6, 100),                          // Mushroom       Vine           Kelp           CeilingKelp
                ePS:CHEERINGWILLOW) },

                { (6, 0),  new BiomeTraits("Mold",                  (Color.DarkBlue.R, Color.DarkBlue.G + 20, Color.DarkBlue.B + 40),
                new float[]{1, 0.25f, 2, 2,  1, 1, 2, 0, 4, 4, 0, 0}, // Worm
                new ((int type, int subType) type, float percentage)[]{ ((4, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((4, 1), 100), },
                txT:(0, 0), tFT:new TerrainFeaturesTraits[]{ famousTFT["Mold"] }) }, // Mold
                //      -E- C  G  W  J   -P- E  G  T  C  S  WG WT WC WS
                { (8, 0),  new BiomeTraits("Ocean",                 (Color.LightBlue.R, Color.LightBlue.G + 60, Color.LightBlue.B + 130),
                new float[]{1, 0.25f, 3, 6,  0, 4, 1, 2, 0, 8, 0, 8, 0}, // Fish      Shark           Waterdog        WaterSkipper   Dragonfly
                new ((int type, int subType) type, float percentage)[]{ ((2, 0), 95), ((8, 0), 4.5f), ((9, 0), 0.5f), ((5, 0), 75), ((10, 0), 25), },
                new ((int type, int subType) type, float percentage)[]{ ((2, 0), 100), ((2, 1), 100), },
                lT:(0, 0), lS:(3, 50, 3000),                         // Kelp          CeilingKelp
                cT:(0, 3), txT:(0, 0), fT:(-2, 0), sT:1) },
                { (8, 1),  new BiomeTraits("Frozen Ocean",          (Color.LightBlue.R + 60, Color.LightBlue.G + 90, Color.LightBlue.B + 150),
                new float[]{1, 0.25f, 3, 6,  0, 2, 1, 2, 0, 8, 0, 8, 0}, // Frost Fairy  Ice Worm    Icealt Worm
                new ((int type, int subType) type, float percentage)[]{ ((0, 2), 100), ((4, 3), 99), ((4, 4), 1) },
                new ((int type, int subType) type, float percentage)[]{ ((6, 0), 60), ((6, 1), 15), ((6, 10), 8), ((6, 11), 10), ((6, 20), 5), ((6, 21), 2), ((5, 2), 100) },
                lT:(0, 0), lS:(3, 50, 3000),                         // IceGrass      IceBrutic     IceTromel     IceKital       IceFlokan     IceOctam      IceVines
                cT:(3, 3), txT:(0, 0), fT:(-2, -1), aST:1, tFT:new TerrainFeaturesTraits[]{ famousTFT["Frost Carving"] }) },
                { (8, 2),  new BiomeTraits("Algae Ocean",           (Color.DarkSeaGreen.R, Color.DarkSeaGreen.G, Color.DarkSeaGreen.B),
                new float[]{1, 0.25f, 15, 6, 0, 4, 1, 2, 0, 8, 0, 8, 0}, // Fish      Shark           Waterdog        WaterSkipper   Dragonfly
                new ((int type, int subType) type, float percentage)[]{ ((2, 0), 99), ((8, 0), 0.9f),  ((9, 0), 0.1f), ((5, 0), 75), ((10, 0), 25), },
                new ((int type, int subType) type, float percentage)[]{ ((2, 3), 85), ((2, 4), 15), ((2, 5), 100), },
                lT:(0, 0), lS:(3, 50, 3000),                         // Algae 1       Algae Bulbous Algae Ceiling 1
                cT:(0, 3), txT:(0, 0), fT:(-2, 2), sT:1, cL:1) },
                { (8, 3),  new BiomeTraits("Salt Ocean",            (Color.DeepPink.R, Color.DeepPink.G, Color.DeepPink.B),
                new float[]{1, 0.25f, 15, 6, 0, 4, 1, 2, 0, 8, 0, 8, 0}, // Salt Worm Icealt Worm
                new ((int type, int subType) type, float percentage)[]{ ((4, 2), 99), ((4, 4), 1)},
                new ((int type, int subType) type, float percentage)[]{ },
                lT:(-2, 2), lS:(3, 50, 3000), tFT:new TerrainFeaturesTraits[]{ famousTFT["Salt Terrain"], famousTFT["Salt Filling"], famousTFT["Salt Spikes"] },
                cT:(1, 3), txT:(0, 0), fT:(-2, 2), sT:1, cL:1) },


                //      -E- C  G  W  J   -P- E  G  T  C  S  WG WT WC WS  
                { (10, 0), new BiomeTraits("Lanterns",             (Color.Gray.R - 50, Color.Gray.G - 10, Color.Gray.B + 40),
                new float[]{1, 0.25f, 2, 1,  0, 4, 1, 2, 2, 4, 0, 4, 0}, // Frost Fairy
                new ((int type, int subType) type, float percentage)[]{ ((0, 2), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((10, 0), 100), ((10, 1), 100), ((10, 2), 100) },
                cT:(1, 5), txT:(0, 0), Da:true) },                   // LanternTree     LanternVine     LanternSide
                { (10, 1), new BiomeTraits("MixedLuminous",        (Color.Gray.R, Color.Gray.G, Color.Gray.B),
                new float[]{1, 0.25f, 2, 1,  0, 4, 1, 1, 1, 4, 0, 4, 0}, // Frost Fairy
                new ((int type, int subType) type, float percentage)[]{ ((0, 2), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((11, 0), 50), ((12, 1), 50), ((10, 1), 100), ((10, 2), 100) },
                Da:true) },                                          // Candle         Chandelier     LanternVine     LanternSide
                { (10, 2), new BiomeTraits("Chandeliers",          (Color.Gray.R + 50, Color.Gray.G + 10, Color.Gray.B - 40),
                new float[]{1, 0.25f, 2, 1,  0, 4, 1, 1, 0, 4, 0, 4, 0}, // Frost Fairy
                new ((int type, int subType) type, float percentage)[]{ ((0, 2), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((11, 0), 50), ((11, 1), 50), ((11, 2), 100), },
                cT:(1, 5), txT:(0, 0), Da:true) },                   // Candle         Chandelier     Candelabrum 
                { (11, 0), new BiomeTraits("Dark Ocean",            (Color.DarkSlateBlue.R, Color.DarkSlateBlue.G, Color.DarkSlateBlue.B),
                new float[]{1, 0.25f, 3, 6,  0, 4, 1, 2, 0, 8, 0, 8, 0}, // Fish      Shark           Anglerfish   Waterdog        WaterSkipper   Dragonfly
                new ((int type, int subType) type, float percentage)[]{ ((2, 0), 95), ((8, 0), 3.5f), ((8, 1), 1), ((9, 0), 0.5f), ((5, 0), 75), ((10, 0), 25), },
                new ((int type, int subType) type, float percentage)[]{ ((2, 0), 100), ((2, 1), 100), },
                cT:(0, 3), txT:(0, 0), fT:(-2, 0), sT:1, Da:true) }, // Kelp           CeilingKelp


                //      -E- C  G  W  J   -P- E  G  T  C  S  WG WT WC WS  
                { (20, 0), new BiomeTraits("Flesh",                 (Color.Red.R, Color.Red.G, Color.Red.B),
                new float[]{1, 1, 2, 1,      0, 4, 1, 4, 0, 4, 0, 4, 0}, // Carnal     Nematode
                new ((int type, int subType) type, float percentage)[]{ ((1, 1), 100), ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((20, 0), 100), ((20, 1), 100) },
                lT:(-7, 0), tT:(4, 0), sM:((4, 2), 40)) },           // Flesh Vine      Flesh Tendril
                { (20, 1), new BiomeTraits("FleshForest",           (Color.DarkRed.R + 20, Color.DarkRed.G - 20, Color.DarkRed.B - 20),
                new float[]{1, 1, 2, 1,      0, 3, 1, 3, 0, 4, 0, 4, 0}, // Carnal     Nematode
                new ((int type, int subType) type, float percentage)[]{ ((1, 1), 100), ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((20, 0), 100), ((20, 1), 100), ((20, 2), 50), ((20, 3), 50) },
                cT:(1, 5), txT:(0, 0), lT:(-7, 0), tT:(4, 0)) },     // Flesh Vine      Flesh Tendril   Flesh Tree 1   Flesh Tree 2
                { (20, 2), new BiomeTraits("Flesh and Bone",        (Color.Pink.R, Color.Pink.G, Color.Pink.B),
                new float[]{1, 1, 2, 1,      0, 4, 1, 4, 0, 4, 0, 4, 0}, // Carnal     Skeletal       Nematode
                new ((int type, int subType) type, float percentage)[]{ ((1, 1), 50),  ((1, 2), 50),  ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((20, 0), 75), ((20, 1), 75), ((21, 0), 25), ((21, 1), 25) },
                lT:(-6, 0), tT:(4, 0),                               // Flesh Vine     Flesh Tendril  Bone Stalagmi  Bone Stalactite
                tFT:new TerrainFeaturesTraits[] { famousTFT["Bone"] }) },
                { (20, 3), new BiomeTraits("Body Hair Forest",      (Color.DarkRed.R - 20, Color.DarkRed.G - 50, Color.DarkRed.B - 70),
                new float[]{1, 1, 2, 1,     10, 4, 1, 4, 0, 4, 0, 4, 0}, // Louse       Nematode
                new ((int type, int subType) type, float percentage)[]{ ((7, 0), 100),  ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{           ((22, 0), 100) },
                cT:(1, 0), lT:(-6, 0), tT:(4, 0), sM:((4, 2), 90), cW:2.5f) }, // Body Hair
                { (20, 4), new BiomeTraits("Long Hair Forest",      (Color.DarkRed.R - 20, Color.DarkRed.G - 50, Color.DarkRed.B - 70),
                new float[]{1, 1, 2, 1,      0, 4, 1, 10, 0, 4, 0, 4, 0}, // Louse      Nematode
                new ((int type, int subType) type, float percentage)[]{ ((7, 0), 100),  ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{           ((22, 1), 100) },
                cT:(1, 0), lT:(-6, 0), tT:(4, 0), sM:((4, 2), 75), cW:2.5f) }, // Long Hair
                //      -E- C  G  W  J   -P- E  G  T  C  S  WG WT WC WS
                { (21, 0), new BiomeTraits("Bone",                  (Color.White.R, Color.White.G, Color.White.B),
                new float[]{1, 1, 2, 1,      0, 1, 1, 1, 0, 4, 0, 4, 0}, // Skeletal   Nematode
                new ((int type, int subType) type, float percentage)[]{ ((1, 2), 100), ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((21, 0), 100), ((21, 1), 100) },
                lT:(-6, 0), tT:(4, 1)) },                            // Bone Stalagmite Bone Stalactite

                { (22, 0), new BiomeTraits("Blood Ocean",           (Color.DarkRed.R, Color.DarkRed.G, Color.DarkRed.B),
                new float[]{1, 1, 2, 1,      0, 4, 1, 2, 0, 4, 0, 4, 0}, // Nematode
                new ((int type, int subType) type, float percentage)[]{ ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ },
                cT:(0, 3), txT:(0, 0), sT:1, tT:(4, 0), fT:(-6, 0),
                tFT:new TerrainFeaturesTraits[]{ famousTFT["Bone"] }) },
                { (22, 1), new BiomeTraits("Acid Ocean",            (Color.YellowGreen.R, Color.YellowGreen.G, Color.YellowGreen.B),
                new float[]{1, 1, 1, 1,      0, 4, 1, 2, 0, 4, 0, 4, 0}, // Nematode
                new ((int type, int subType) type, float percentage)[]{ ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ },
                cT:(0, 3), txT:(0, 0), sT:1, tT:(4, 0), fT:(-7, 0), cL:1,
                tFT:new TerrainFeaturesTraits[] { famousTFT["Bone"] }) },
            };

            foreach ((int type, int subType) typeToSet in biomeTraitsDict.Keys) { biomeTraitsDict[typeToSet].setType(typeToSet); }

            foreach ((int type, int subType) key in biomeTraitsDict.Keys)
            {
                BiomeTraits bT = biomeTraitsDict[key];
                if (bT.tileType == (1, 0))
                {
                    if (bT.terrainFeaturesTraitsArray is null) { bT.terrainFeaturesTraitsArray = new TerrainFeaturesTraits[] { famousTFT["HardRock"] }; }
                    else
                    {
                        TerrainFeaturesTraits[] tFTA = new TerrainFeaturesTraits[bT.terrainFeaturesTraitsArray.Length + 1];
                        for (int i = 0; i < bT.terrainFeaturesTraitsArray.Length; i++) { tFTA[i + 1] = bT.terrainFeaturesTraitsArray[i]; }
                        tFTA[0] = famousTFT["HardRock"];
                        bT.terrainFeaturesTraitsArray = tFTA;
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
            public bool isTerrainPlacing;
            public bool isPlantDigging;
            public bool isAbortable;
            public bool isEntityBound;
            public (int type, int subType)? targetMaterial;

            public (int v, int h, int s) r;
            public (int v, int h, int s) g;
            public (int v, int h, int s) b;
            public AttackTraits(string namee, float d = 0, float m = 0, bool H = false, bool T = false, bool tP = false, bool P = false, bool A = false, bool B = false, (int type, int subType)? tM = null)
            {
                name = namee;
                damage = d;
                manaCost = m;
                isHitting = H;
                isTerrainDigging = T;
                isTerrainPlacing = tP;
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
                { (3, 5, 0, 4), new AttackTraits("Place Wand",                  m:15,           B:true                                  ) },
                { (3, 5, 1, 4), new AttackTraits("Place bullet",                                       tP:true                          ) },
                { (3, 5, 2, 4), new AttackTraits("Place bullet 2",                                     tP:true                          ) },
                { (3, 5, 3, 4), new AttackTraits("Place bullet 3",                                     tP:true                          ) },

                { (6, 0, 0, 5), new AttackTraits("Goblin Hand",         d:0.25f,        H:true, B:true, T:true, P:true, A:true          ) },
                                                                                                                                    
                { (3, 0, 0, 5), new AttackTraits("Hornet Warning",      d:0.05f,        H:true, B:true                                  ) },
                { (3, 1, 0, 5), new AttackTraits("Hornet Mandibles",    d:1,            H:true, B:true, T:true, P:true,        tM:(2, 1)) },
                { (3, 2, 0, 5), new AttackTraits("Hornet Sting",        d:0.65f,        H:true, B:true                                  ) },
            };
        }
    }
}
