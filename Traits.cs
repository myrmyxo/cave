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
using System.Runtime.InteropServices;
using System.Windows.Forms.VisualStyles;

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
        public class TileTraits
        {
            public (int type, int subType) type;
            public string name;

            public bool isSolid;
            public bool isAir;
            public bool isLiquid;
            public bool isAcidic;
            public bool isLava; // H for hot
            public bool isTransformant; // Fairy liquid

            public int hardness;
            public int viscosity = 0;

            public ColorRange colorRange;
            public float biomeColorBlend;
            public bool isTextured; // For mold for now
            public TileTraits(string namee, float biomeColorBlendToPut, ColorRange colRange, bool Air = false, bool Tex = false, bool L = false, bool H = false, bool A = false, bool T = false)
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
            }
            public void setType((int type, int subType) typeToSet) { type = typeToSet; }
        }

        public static Dictionary<(int type, int subType), TileTraits> tileTraitsDict;
        public static TileTraits getTileTraits((int type, int subType) tileType)
        {
            return tileTraitsDict.ContainsKey(tileType) ? tileTraitsDict[tileType] : tileTraitsDict[(0, 0)];
        }
        public static void makeTileTraitsDict()
        {
            tileTraitsDict = new Dictionary<(int type, int subType), TileTraits>()
            {
                { (0, 0), new TileTraits("Error/Air", 0.5f,
                new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0)),      Air:true                                    ) },
                                                                                                                            
                                                                                                                            
                                                                                                                            
                { (-1, 0), new TileTraits("Piss", 0.2f,                                                      
                new ColorRange((120, 0, 0), (120, 0, 0), (80, 0, 0)),       L:true                                      ) },
                                                                                                                            
                { (-2, 0), new TileTraits("Water", 0.2f,
                new ColorRange((80, 0, 0), (80, 0, 0), (120, 0, 0)),        L:true                                      ) },

                { (-3, 0), new TileTraits("Fairy Liquid", 0.2f,
                new ColorRange((105, 0, 0), (80, 0, 0), (120, 0, 0)),       L:true, T:true                              ) },
                                                                                                                            
                { (-4, 0), new TileTraits("Lava", 0.05f,
                new ColorRange((255, 0, 0), (90, 0, 0), (0, 0, 0)),         L:true, H:true                              ) },

                { (-5, 0), new TileTraits("Honey", 0.2f,
                new ColorRange((160, 0, 0), (120, 0, 0), (70, 0, 0)),       L:true                                      ) },

                { (-6, 0), new TileTraits("Blood", 0.2f,
                new ColorRange((100, 0, 0), (15, 0, 0), (25, 0, 0)),        L:true                                      ) },
                { (-6, 1), new TileTraits("Deoxygenated Blood", 0.2f,
                new ColorRange((65, 0, 0), (5, 0, 0), (35, 0, 0)),          L:true                                      ) },

                { (-7, 0), new TileTraits("Acid", 0.2f,
                new ColorRange((120, 0, 0), (180, 0, 0), (60, 0, 0)),       L:true, A:true                              ) },



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
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },
                { (1, 1), new MaterialTraits("Wood",                        tool:(4, 0, 4),
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },
                { (1, 2), new MaterialTraits("Kelp",
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },

                { (2, 0), new MaterialTraits("Petal",
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },
                { (2, 1), new MaterialTraits("Pollen",
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },

                { (3, 0), new MaterialTraits("Mushroom Stem",
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },
                { (3, 1), new MaterialTraits("Mushroom Cap",
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },
                { (3, 2), new MaterialTraits("Mold",
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },

                { (8, 0), new MaterialTraits("Flesh",
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },
                { (8, 1), new MaterialTraits("Bone",
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },

                { (10, 0), new MaterialTraits("Magic Rock",
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },

                { (11, 0), new MaterialTraits("Metal",                      tool:(4, 0, 4),
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },
                { (11, 1), new MaterialTraits("Lightbulb",
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },

                { (12, 0), new MaterialTraits("Wax",
                col:new ColorRange((140, 0, 0), (140, 0, 0), (140, 0, 0))                                               ) },
            };
        }






        public class EntityTraits
        {
            public string name;

            public int startingHp;

            public ((int type, int subType, int megaType) element, int count) drops;

            public bool isFlying;
            public bool isSwimming;
            public bool isDigging;
            public bool isJesus;

            // Behaviors
            public int inWaterBehavior;     // -> 0: nothing, 1: float upwards, 2: move randomly in water
            public int onWaterBehavior;     // -> 0: nothing, 1: skip, 2: drift towards land
            public int inAirBehavior;       // -> 0: nothing, 1: fly randomly, 2: random drift
            public int onGroundBehavior;    // -> 0: nothing, 1: random jump, 2: move around, 3: dig down
            public int inGroundBehavior;    // -> 0: nothing, 1: random jump, 2: dig around, 3: teleport, 4: dig tile

            public float swimSpeed;
            public float swimMaxSpeed;

            public (float x, float y) jumpStrength;
            public float jumpChance;
            public float idleChance;
            public bool isNestEntity;

            public ColorRange colorRange;
            public EntityTraits(string namee, int hp, ((int type, int subType, int megaType) element, int count) drps, ColorRange colRange,
                int iW = 0, int oW = 0, int iA = 0, int oG = 0, int iG = 0,
                float sS = 0.1f, float sMS = 0.5f, (float x, float y)? jS = null, float jC = 0)
            {
                name = namee;
                startingHp = hp;
                drops = drps;

                isFlying = iA == 1 ? true : false;
                isSwimming = iW == 2 ? true : false;
                isDigging = iG == 2 ? true : false;
                isJesus = oW == 1 ? true : false;

                inWaterBehavior = iW;
                onWaterBehavior = oW;
                inAirBehavior = iA;
                inGroundBehavior = iG;
                onGroundBehavior = oG;

                swimSpeed = sS;
                swimMaxSpeed = sMS;

                jumpStrength = jS ?? (1, 1);
                jumpChance = jC;

                colorRange = colRange;
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
                new ColorRange((130, 50, 30), (130, -50, 30), (210, 0, 30)),
                iW:1, iA:1, iG:3) },                                                                        
                { (0, 1), new EntityTraits("ObsidianFairy",   10, ((-3, 0, 0), 1),      //  --> Fairy Liquid
                new ColorRange((30, 0, 30), (30, 0, 30), (30, 0, 30)),
                iW:1, iA:1, iG:3) },
                { (0, 2), new EntityTraits("FrostFairy",      4 , ((-3, 0, 0), 1),      //  --> Fairy Liquid
                new ColorRange((200, 25, 30), (200, 25, 30), (225, 0, 30)),
                iW:1, iA:1, iG:3) },
                { (0, 3), new EntityTraits("SkeletonFairy",   15, ((8, 1, 3), 1),       //  --> Bone
                new ColorRange((210, 0, 20), (210, 0, 20), (190, 20, 20)),
                iW:1, iA:1, iG:3) },

                { (1, 0), new EntityTraits("Frog",            2,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((90, 50, 30), (210, 50, 30), (110, -50, 30)),
                iW:1, oW:2, iA:2, oG:1, iG:1, jS:(2.5f, 2.5f), jC:0.1f) },
                { (1, 1), new EntityTraits("Carnal",          7,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((135, 0, 30), (55, 30, 30), (55, 30, 30)),
                iW:1, oW:2, iA:2, oG:1, iG:1, jS:(2.5f, 2.5f), jC:0.1f) },
                { (1, 2), new EntityTraits("Skeletal",        7,  ((8, 1, 3), 1),       //  --> Bone
                new ColorRange((210, 0, 20), (210, 0, 20), (190, 20, 20)),
                iW:1, oW:2, iA:2, oG:1, iG:1, jS:(2.5f, 2.5f), jC:0.1f) },

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
                new ColorRange((190, 10, 30), (190, 10, 30), (80, 0, 30)),
                iW:1, iA:1, iG:4) },

                { (4, 0), new EntityTraits("Worm",            7,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((210, 0, 30), (140, 20, 30), (140, 20, 30)),
                iW:1, oW:2, iA:2, oG:3, iG:2) },
                { (4, 1), new EntityTraits("Nematode",        3,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((210, -20, 30), (210, 20, 30), (235, 0, 30)),
                iW:2, oW:2, iA:2, oG:3, iG:2) },

                { (5, 0), new EntityTraits("WaterSkipper",    3,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((110, 0, 30), (110, 0, 30), (140, 20, 30)),
                iW:1, oW:1, iA:2, oG:1, iG:1, jS:(1, 1), jC:0.05f) },

                { (6, 0), new EntityTraits("Goblin",          3,  ((8, 0, 3), 1),       //  --> Flesh
                new ColorRange((80, 50, 30), (175, 50, 30), (80, 50, 30)),
                iW:1, oW:2, iA:2, oG:1, iG:1, jC:0.05f) },
            };
        }







        



        public class PlantStructureFrame
        {
            public Dictionary<(int type, int subType), (int x, int y)> elementDict;
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
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (0, 0), (-1, 1), (0, 1), (1, 1), (0, 2) } } }
                ) },
                { "TreeLeaves3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0), (-1, 1), (0, 1), (1, 1), (-1, 2), (0, 2), (1, 2) } } }
                ) },
                { "TreeLeaves4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (1, 0), new (int x, int y)[] { (-1, 0), (0, 0), (1, 0), (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1), (-2, 2), (-1, 2), (0, 2), (1, 2), (2, 2), (-2, 3), (-1, 3), (0, 3), (1, 3), (2, 3), (-1, 4), (0, 4), (1, 4) } } }
                ) },

                { "ChandelierTree1", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 1), new (int x, int y)[] { (0, 0) } } }
                ) },
                { "ChandelierTree2", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 1), new (int x, int y)[] { (0, 0) } },  { (11, 0), new (int x, int y)[] { (0, 1) } } }
                ) },
                { "ChandelierTree3", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 1), new (int x, int y)[] { (0, 0) } },  { (11, 0), new (int x, int y)[] { (-1, 1), (0, 1), (1, 1) } } }
                ) },
                { "ChandelierTree4", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 1), new (int x, int y)[] { (0, 0) } },  { (11, 0), new (int x, int y)[] { (-1, 1), (0, 1), (1, 1), (0, 2) } } }
                ) },
                { "ChandelierTree5", new PlantStructureFrame(
                dict:new Dictionary<(int type, int subType), (int x, int y)[]> { { (11, 1), new (int x, int y)[] { (0, 0) } },  { (11, 0), new (int x, int y)[] { (-1, -1), (0, -1), (1, -1), (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1), (0, 2) } } }
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
            };
        }
        public class PlantStructure
        {
            public string name;
            public bool isRegenerative;
            public int maxGrowth;
            public PlantStructureFrame[] frames;
            public (int value, int range)[] minimumScores;
            public PlantStructure(string namee, PlantStructureFrame[] framez, (int value, int range)[] minScores = null, bool isReg = false)
            {
                name = namee;
                isRegenerative = isReg;
                frames = framez;
                maxGrowth = framez is null ? 0 : framez.Length;
                if (minScores != null && minScores.Length != framez.Length) { throw new Exception("Error in creation of PlantStructure ! framez and minScores arrays had different lengths !"); }
                minimumScores = minScores;
            }
        }
        public static PlantStructureFrame[] makeStructureFrameArray(params string[] args)
        {
            PlantStructureFrame[] arrayo = new PlantStructureFrame[args.Length];
            for (int i = 0; i < args.Length; i++) { arrayo[i] = plantStructureFramesDict.ContainsKey(args[i]) ? plantStructureFramesDict[args[i]] : plantStructureFramesDict["Error"]; }
            return arrayo;
        }

        public static Dictionary<(int type, int subType, int subSubType), PlantStructure> plantStructuresDict;
        public static void makePlantStructuresDict()
        {
            plantStructuresDict = new Dictionary<(int type, int subType, int subSubType), PlantStructure>()
            {
                { (-1, 0, 0), new PlantStructure("Error", null                                                     ) },

                { (0, 2, 0), new PlantStructure("TulipFlower",               
                framez:makeStructureFrameArray("TulipFlower1", "TulipFlower2", "TulipFlower3", "TulipFlower4", "TulipFlower5")
                ) },
                { (0, 3, 0), new PlantStructure("AlliumFlower",
                framez:makeStructureFrameArray("AlliumFlower1", "AlliumFlower2", "AlliumFlower3", "AlliumFlower4")
                ) },

                { (1, 0, 0), new PlantStructure("TreeLeaves",
                framez:makeStructureFrameArray("TreeLeaves1", "TreeLeaves2", "TreeLeaves3", "TreeLeaves4")
                ) },
                { (1, 1, 0), new PlantStructure("ChandelierTree",
                framez:makeStructureFrameArray("ChandelierTree1", "ChandelierTree2", "ChandelierTree3", "ChandelierTree4", "ChandelierTree5")
                ) },

                { (4, 0, 0), new PlantStructure("MushroomCap",
                framez:makeStructureFrameArray("MushroomCap1", "MushroomCap2", "MushroomCap3"),
                minScores:new (int value, int range)[]{ (1, 0), (2, 5), (4, 5) }) },

                { (5, 0, 0), new PlantStructure("PlusFlower",
                framez:makeStructureFrameArray("PlusFlower1", "PlusFlower2", "PlusFlower3")
                ) },
                { (5, 0, 1), new PlantStructure("CrossFlower",
                framez:makeStructureFrameArray("CrossFlower1", "CrossFlower2", "CrossFlower3")
                ) },
            };
            plantStructuresDict[(5, 1, 0)] = plantStructuresDict[(5, 0, 0)];    // TEMP for obsidian vines ! Gonna be changed when plants are fully traited.
            plantStructuresDict[(5, 1, 1)] = plantStructuresDict[(5, 0, 1)];    // TEMP for obsidian vines ! Gonna be changed when plants are fully traited.
        }














        public class PlantTraits
        {
            public string name;
            public int startingHp;
            public ((int type, int subType, int megaType) element, int count) drops;
            public bool isTree;
            public bool isCeiling;
            public bool isWater;
            public (int min, int range) maxGrowth;
            public ColorRange colorRange;
            public PlantTraits(string namee, (int min, int range)? maxGrowthToPut = null, bool T = false, bool C = false, bool W = false)
            {
                name = namee;
                maxGrowth = maxGrowthToPut is null ? (-1, 0) : maxGrowthToPut.Value;
                isTree = T;
                isCeiling = C;
                isWater = W;
            }
        }

        public static Dictionary<(int type, int subType), PlantTraits> plantTraitsDict;
        public static void makePlantTraitsDict()
        {
            plantTraitsDict = new Dictionary<(int type, int subType), PlantTraits>()
            {
                { (-1, 0), new PlantTraits("Error"                                                          ) },

                { (0, 0), new PlantTraits("BasePlant",                                              (1, 5)  ) },
                { (0, 1), new PlantTraits("Candle",                                                 (1, 5)  ) },
                { (0, 2), new PlantTraits("Tulip",                                                  (2, 4)  ) },
                { (0, 3), new PlantTraits("Allium",                                                 (4, 3)  ) },
                
                { (1, 0), new PlantTraits("Tree",                                   T:true                  ) },
                { (1, 1), new PlantTraits("ChandelierTree",                         T:true                  ) },

                { (2, 0), new PlantTraits("KelpUpwards",                                    W:true          ) },
                { (2, 1), new PlantTraits("KelpDownwards",                          C:true, W:true          ) },

                { (3, 0), new PlantTraits("ObsidianPlant"                                                   ) },

                { (4, 0), new PlantTraits("Mushroom"                                                        ) },
                { (4, 1), new PlantTraits("Mold"                                                            ) },

                { (5, 0), new PlantTraits("Vines",                                  C:true                  ) },
                { (5, 1), new PlantTraits("ObsidianVines",                          C:true                  ) },
            };
        }
        public class BiomeTraits        // -> Additional spawn attempts ? Like for modding idfk, on top of existing ones... idk uirehqdmsoijq
        {
            public string name;
            public int difficulty = 1;
            public (int r, int g, int b) color;


            public float entityBaseSpawnRate;
            public float entityGroundSpawnRate;
            public float entityWaterSpawnRate;
            public float entityJesusSpawnRate;
            public float plantGroundSpawnRate;
            public float plantTreeSpawnRate;
            public float plantCeilingSpawnRate;
            public float plantWaterGroundSpawnRate;
            public float plantWaterCeilingSpawnRate;

            public ((int type, int subType) type, float percentage)[] entityBaseSpawnTypes;
            public ((int type, int subType) type, float percentage)[] entityGroundSpawnTypes;
            public ((int type, int subType) type, float percentage)[] entityWaterSpawnTypes;
            public ((int type, int subType) type, float percentage)[] entityJesusSpawnTypes;
            public ((int type, int subType) type, float percentage)[] plantGroundSpawnTypes;
            public ((int type, int subType) type, float percentage)[] plantTreeSpawnTypes;
            public ((int type, int subType) type, float percentage)[] plantCeilingSpawnTypes;
            public ((int type, int subType) type, float percentage)[] plantWaterGroundSpawnTypes;
            public ((int type, int subType) type, float percentage)[] plantWaterCeilingSpawnTypes;
            public bool isDark;

            // terrain generation shit also

            public BiomeTraits(string namee, (int r, int g, int b) colorToPut, float[] spawnRates, ((int type, int subType) type, float percentage)[] entityTypes, ((int type, int subType) type, float percentage)[] plantTypes, bool D = false)
            {
                name = namee;
                color = colorToPut;
                isDark = D;

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
                List<((int type, int subType) type, float percentage)> plantGroundSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> plantTreeSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> plantCeilingSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> plantWaterGroundSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                List<((int type, int subType) type, float percentage)> plantWaterCeilingSpawnTypesList = new List<((int type, int subType) type, float percentage)>();
                foreach (((int type, int subType) type, float percentage) tupelo in plantTypes)
                {
                    if (!plantTraitsDict.ContainsKey(tupelo.type)) { continue; }
                    plantTraits = plantTraitsDict[tupelo.type];
                    if (plantTraits.isTree) { plantTreeSpawnTypesList.Add(tupelo); }
                    else if (plantTraits.isWater)
                    {
                        if (plantTraits.isCeiling) { plantWaterCeilingSpawnTypesList.Add(tupelo); }
                        else { plantWaterGroundSpawnTypesList.Add(tupelo); }
                    }
                    else
                    {
                        if (plantTraits.isCeiling) { plantCeilingSpawnTypesList.Add(tupelo); }
                        else { plantGroundSpawnTypesList.Add(tupelo); }
                    }
                }
                plantGroundSpawnTypes  = plantGroundSpawnTypesList.ToArray();
                plantTreeSpawnTypes = plantTreeSpawnTypesList.ToArray();
                plantCeilingSpawnTypes = plantCeilingSpawnTypesList.ToArray();
                plantWaterGroundSpawnTypes = plantWaterGroundSpawnTypesList.ToArray();
                plantWaterCeilingSpawnTypes = plantWaterCeilingSpawnTypesList.ToArray();


                entityBaseSpawnRate = entityBaseSpawnTypes.Length == 0 ? 0 : spawnRates[0];
                entityGroundSpawnRate = entityGroundSpawnTypes.Length == 0 ? 0 : spawnRates[1];
                entityWaterSpawnRate = entityWaterSpawnTypes.Length == 0 ? 0 : spawnRates[2];
                entityJesusSpawnRate = entityJesusSpawnTypes.Length == 0 ? 0 : spawnRates[3];

                plantGroundSpawnRate = plantGroundSpawnTypes.Length == 0 ? 0 : spawnRates[4];
                plantTreeSpawnRate = plantTreeSpawnTypes.Length == 0 ? 0 : spawnRates[5];
                plantCeilingSpawnRate = plantCeilingSpawnTypes.Length == 0 ? 0 : spawnRates[6];
                plantWaterGroundSpawnRate = plantWaterGroundSpawnTypes.Length == 0 ? 0 : spawnRates[7];
                plantWaterCeilingSpawnRate = plantWaterCeilingSpawnTypes.Length == 0 ? 0 : spawnRates[8];
            }
        }

        public static Dictionary<(int type, int subType), BiomeTraits> biomeTraitsDict;
        public static void makeBiomeTraitsDict()
        {
            biomeTraitsDict = new Dictionary<(int type, int subType), BiomeTraits>()
            {   //      -E- C  G  W  J    -P- G  T  C WG WC  
                { (-1, 0), new BiomeTraits("Error",                 (1200, -100, 1200),
                new float[]{0, 0, 0, 0,       0, 0, 0, 0, 0},
                new ((int type, int subType) type, float percentage)[]{ },
                new ((int type, int subType) type, float percentage)[]{ }
                ) },

                { (0, 0),  new BiomeTraits("Cold",                  (Color.Blue.R, Color.Blue.G, Color.Blue.B),     // -> put smaller spawn rates for this one ? Since cold. And nothing for frost
                new float[]{1, 0.25f, 2, 2,       4, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), }
                ) },                                                 // Base           Vine           Kelp           CeilingKelp
                { (0, 1),  new BiomeTraits("Frost",                 (Color.LightBlue.R, Color.LightBlue.G, Color.LightBlue.B),
                new float[]{1, 0.25f, 2, 2,       4, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((0, 2), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), }
                ) },                                                 // Base           Vine           Kelp           CeilingKelp
                { (1, 0),  new BiomeTraits("Acid",                  (Color.Fuchsia.R, Color.Fuchsia.G, Color.Fuchsia.B),
                new float[]{1, 0.25f, 2, 2,       4, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((4, 0), 100), ((2, 0), 100),  ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), }
                ) },                                                 // Base           Vine           Kelp           CeilingKelp

                { (2, 0),  new BiomeTraits("Hot",                   (Color.OrangeRed.R, Color.OrangeRed.G, Color.OrangeRed.B),
                new float[]{1, 0.25f, 2, 2,       4, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((2, 0), 100), ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), }
                ) },                                                 // Base           Vine           Kelp           CeilingKelp
                { (2, 1),  new BiomeTraits("Lava Ocean",            (Color.OrangeRed.R + 90, Color.OrangeRed.G + 30, Color.OrangeRed.B),
                new float[]{1, 0.25f, 2, 2,       4, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((2, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), }
                ) },                                                 // Base           Vine           Kelp           CeilingKelp
                { (2, 2),  new BiomeTraits("Obsidian",              (-100, -100, -100),
                new float[]{1, 0.25f, 2, 2,       4, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((0, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((3, 0), 100), ((5, 1), 100), ((2, 0), 100), ((2, 1), 100), }
                ) },                                                 // ObsidianPlant  Vine           Kelp           CeilingKelp

                { (3, 0),  new BiomeTraits("Forest",                (Color.Green.R, Color.Green.G, Color.Green.B),       // finish forest flowers shite
                new float[]{1, 0.25f, 2, 2,        6, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((1, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), }
                ) },                                                 // Base           Tree           Vine           Kelp           CeilingKelp
                { (3, 1),  new BiomeTraits("Flower Forest",         (Color.Green.R, Color.Green.G + 40, Color.Green.B + 80),
                new float[]{1, 0.25f, 2, 2,       16, 1, 3, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 10), ((0, 2), 20), ((0, 3), 20), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), }
                ) },                                                 // Base          Tulip         Allium        Vine           Kelp           CeilingKelp

                { (4, 0),  new BiomeTraits("Toxic",                 (Color.GreenYellow.R, Color.GreenYellow.G, Color.GreenYellow.B),
                new float[]{1, 0.25f, 2, 2,       4, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((1, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), }
                ) },                                                 // Base           Vine           Kelp           CeilingKelp

                { (5, 0),  new BiomeTraits("Fairy",                 (Color.LightPink.R, Color.LightPink.G, Color.LightPink.B),
                new float[]{1, 0.25f, 2, 2,       4, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((0, 0), 100), ((4, 0), 100), ((2, 0), 100), ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((4, 0), 100), ((5, 0), 100), ((2, 0), 100), ((2, 1), 100), }
                ) },                                                 // Mushroom       Vine           Kelp           CeilingKelp

                { (6, 0),  new BiomeTraits("Mold",                  (Color.DarkBlue.R, Color.DarkBlue.G + 20, Color.DarkBlue.B + 40),
                new float[]{1, 0.25f, 2, 2,       4, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((4, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((4, 1), 100), }
                ) },                                                 // Mold

                { (8, 0),  new BiomeTraits("Ocean",                 (Color.LightBlue.R, Color.LightBlue.G + 60, Color.LightBlue.B + 130),
                new float[]{1, 0.25f, 3, 6,       4, 1, 2, 8, 8},
                new ((int type, int subType) type, float percentage)[]{ ((2, 0), 100), ((5, 0), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((2, 0), 100), ((2, 1), 100), }
                ) },                                                 // Kelp           CeilingKelp



                { (9, 0),  new BiomeTraits("Chandeliers",           (Color.Gray.R, Color.Gray.G, Color.Gray.B),
                new float[]{1, 0.25f, 2, 1,       4, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((0, 2), 100), },
                new ((int type, int subType) type, float percentage)[]{ ((0, 1), 100), ((1, 1), 100), },
                D:true) },                                           // Candle         ChandelierTree



                { (10, 0), new BiomeTraits("Flesh",                 (Color.Red.R, Color.Red.G, Color.Red.B),
                new float[]{1, 1, 2, 1,       4, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((1, 1), 100), ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ }
                ) },
                { (10, 1), new BiomeTraits("Flesh and Bone",        (Color.Pink.R, Color.Pink.G, Color.Pink.B),
                new float[]{1, 1, 2, 1,       4, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((1, 1), 50), ((1, 2), 50), ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ }
                ) },
                { (10, 2), new BiomeTraits("Bone",                  (Color.White.R, Color.White.G, Color.White.B),
                new float[]{1, 1, 2, 1,       4, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((1, 1), 100), ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ }
                ) },

                { (10, 3), new BiomeTraits("Blood Ocean",           (Color.DarkRed.R, Color.DarkRed.G, Color.DarkRed.B),
                new float[]{1, 1, 2, 1,       4, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ }
                ) },
                { (10, 4), new BiomeTraits("Acid Ocean",            (Color.YellowGreen.R, Color.YellowGreen.G, Color.YellowGreen.B),
                new float[]{1, 1, 1, 1,       4, 1, 2, 4, 4},
                new ((int type, int subType) type, float percentage)[]{ ((4, 1), 100), },
                new ((int type, int subType) type, float percentage)[]{ }
                ) },
            };
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


                { (6, 0, 0, 5), new AttackTraits("Goblin Hand",         d:0.25f,        H:true, B:true, T:true, P:true, A:true          ) },
                                                                                                                                    
                { (3, 0, 0, 5), new AttackTraits("Hornet Warning",      d:0.05f,        H:true, B:true                                  ) },
                { (3, 1, 0, 5), new AttackTraits("Hornet Mandibles",    d:1,            H:true, B:true, T:true, P:true,        tM:(2, 1)) },
                { (3, 2, 0, 5), new AttackTraits("Hornet Sting",        d:0.65f,        H:true, B:true                                  ) },
            };
        }
    }
}
