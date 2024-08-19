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

using static Cave.Form1;
using static Cave.Form1.Globals;
using static Cave.MathF;
using static Cave.Sprites;
using static Cave.Structures;
using static Cave.Nests;
using static Cave.Entities;
using static Cave.Files;
using static Cave.Plants;
using static Cave.Screens;
using static Cave.Chunks;
using static Cave.Players;
using System.Diagnostics;


namespace Cave
{
    public partial class Form1 : Form
    {
        public const float div32 = 0.03125f;
        public const float _1On255 = 0.00393f;
        public const float _1On17 = 0.0588f;
        public class Globals
        {
            public static bool loadStructuresYesOrNo = true;
            public static bool spawnPlantsAndEntities = false;

            public static int ChunkLength = 4;
            public static int UnloadedChunksAmount = 8;

            public static Bitmap black32Bitmap = new Bitmap(32, 32);
            public static Bitmap transBlue32Bitmap = new Bitmap(32, 32);
            public static Chunk theFilledChunk;
            public static Color transparentColor = Color.FromArgb(0, 255, 255, 255);

            public static Random rand = new Random();

            public static bool[] arrowKeysState = { false, false, false, false }; //
            public static bool digPress = false;
            public static bool[] placePress = { false, false };
            public static bool[] zoomPress = { false, false };
            public static bool[] inventoryChangePress = { false, false };
            public static bool pausePress = false;
            public static bool fastForward = false;
            public static bool debugMode = false;
            public static bool craftPress = false;
            public static bool craftSelection = false;
            public static bool shiftPress = false;
            public static bool dimensionChangePress = false;
            public static bool dimensionSelection = false;
            public static int currentTargetDimension = 0;
            public static float lastZoom = 0;
            public static DateTime timeAtLauch;
            public static float timeElapsed = 0;

            public static int liquidSlideCount = 0;

            public static string currentDirectory;

            public static int currentStructureId = 0;
            public static int currentEntityId = 0;
            public static int currentPlantId = 0;
            public static int currentNestId = 0;
            public static int currentScreenId = 0;

            public static long worldSeed = 0;

            public static (int, int)[] squareModArray = new (int, int)[4] { (0, 0), (1, 0), (0, 1), (1, 1) };
            public static (int, int)[] bigSquareModArray = new (int, int)[9] { (0, 0), (1, 0), (2, 0), (0, 1), (1, 1), (2, 1), (0, 2), (1, 2), (2, 2) };
            public static (int, int)[] neighbourArray = new (int, int)[4] { (-1, 0), (1, 0), (0, 1), (0, -1) };
            public static (int, int)[] diagArray = new (int, int)[4] { (-1, 1), (1, 1), (1, -1), (-1, -1) };
            public static (int x, int y)[] directionPositionArray = new (int x, int y)[] { (-1, 0), (-1, 1), (0, 1), (1, 1), (1, 0), (1, -1), (0, -1), (-1, -1), };
            public static Dictionary<(int x, int y), int> directionPositionDictionary = new Dictionary<(int x, int y), int>
            {
                { (-1, 0), 0 },
                { (-1, 1), 1 },
                { (0, 1), 2 },
                { (1, 1), 3 },
                { (1, 0), 4},
                { (1, -1), 5 },
                { (0, -1), 6 },
                { (-1, -1), 7 },
            };
            public static Dictionary<(int type, int subType), int> entityStartingHp = new Dictionary<(int type, int subType), int>
            {
                { (0, 0), 2}, // Fairy
                { (0, 1), 4}, // ObsidianFairy
                { (0, 2), 2}, // FrostFairy
                { (0, 3), 5}, // SkeletonFairy
                { (1, 0), 1}, // Frog
                { (1, 1), 3}, // Carnal
                { (1, 2), 3}, // Skeletal
                { (2, 0), 1}, // Fish
                { (2, 1), 1}, // SkeletonFish
                { (3, 0), 1}, // HornetEgg 
                { (3, 1), 2}, // HornetLarva
                { (3, 2), 5}, // HornetCocoon
                { (3, 3), 3}, // Hornet
                { (4, 0), 4}, // Worm
                { (4, 1), 2}, // Nematode
                { (5, 0), 1}, // WaterSkipper
            };
            public static Dictionary<(int type, int subType), ((int type, int subType, int megaType) element, int count)> entityDrops = new Dictionary<(int type, int subType), ((int type, int subType, int megaType), int count)>
            {
                { (0, 0), ((-3, 0, 0), 1)}, // Fairy          --> Fairy Liquid
                { (0, 1), ((-3, 0, 0), 1)}, // ObsidianFairy  --> Fairy Liquid
                { (0, 2), ((-3, 0, 0), 1)}, // FrostFairy     --> Fairy Liquid
                { (0, 3), ((8, 1, 3), 1)},  // SkeletonFairy  --> Bone
                { (1, 0), ((8, 0, 3), 1)},  // Frog           --> Flesh
                { (1, 1), ((8, 0, 3), 1)},  // Carnal         --> Flesh
                { (1, 2), ((8, 1, 3), 1)},  // Skeletal       --> Bone
                { (2, 0), ((8, 0, 3), 1)},  // Fish           --> Flesh
                { (2, 1), ((8, 1, 3), 1)},  // SkeletonFish   --> Bone
                { (3, 0), ((8, 0, 3), 1)},  // HornetEgg      --> Flesh
                { (3, 1), ((8, 0, 3), 1)},  // HornetLarva    --> Flesh
                { (3, 2), ((8, 0, 3), 1)},  // HornetCocoon   --> Flesh
                { (3, 3), ((8, 0, 3), 1)},  // Hornet         --> Flesh
                { (4, 0), ((8, 0, 3), 1)},  // Worm           --> Flesh
                { (4, 1), ((8, 0, 3), 1)},  // Nematode       --> Flesh
                { (5, 0), ((8, 0, 3), 1)},  // WaterSkipper   --> Flesh
            };

            public static Dictionary<int, int> costDict = new Dictionary<int, int>
            {
                { 0, 1 },       // air
                { -1, 3 },      // piss
                { -2, 3 },      // water
                { -3, 10 },     // fairy liquid
                { -4, 999999 }, // lava (cannot cross)
                { -5, 5 },      // honey
                { -6, 3 },      // blood
                { -7, 999999 }, // acid (cannot cross)
            };

            public static Dictionary<(int biome, int subBiome), bool> darkBiomes = new Dictionary<(int biome, int subBiome), bool>
            {
                { (9, 0), true }, // chandelier biome
            };

            public static Dictionary<(int biome, int subBiome), (int, int, int)> biomeDict = new Dictionary<(int biome, int subBiome), (int, int, int)>
            {
                { (-1, 0), (1200, -100, 1200) },                                               // undefined

                { (0, 0), (Color.Blue.R,Color.Blue.G,Color.Blue.B) },                          // cold biome
                { (0, 1), (Color.LightBlue.R,Color.LightBlue.G,Color.LightBlue.B) },           // frost biome

                { (1, 0), (Color.Fuchsia.R,Color.Fuchsia.G,Color.Fuchsia.B) },                 // acid biome

                { (2, 0), (Color.OrangeRed.R,Color.OrangeRed.G,Color.OrangeRed.B) },           // hot biome
                { (2, 1), (Color.OrangeRed.R + 90,Color.OrangeRed.G + 30,Color.OrangeRed.B) }, // lava ocean biome
                { (2, 2), (-100,-100,-100) },                                                  // obsidian biome...

                { (3, 0), (Color.Green.R,Color.Green.G,Color.Green.B)},                        // forest biome
                { (3, 1), (Color.Green.R,Color.Green.G + 40,Color.Green.B + 80)},              // flower forest biome
                                                                                               
                { (4, 0), (Color.GreenYellow.R,Color.GreenYellow.G,Color.GreenYellow.B) },     // toxic biome
                                                                                               
                { (5, 0), (Color.LightPink.R,Color.LightPink.G,Color.LightPink.B) },           // fairy biome !
                                                                                               
                { (8, 0), (Color.LightBlue.R,Color.LightBlue.G+60,Color.LightBlue.B+130) },    // ocean biome !
                                                                                               
                { (9, 0), (Color.Gray.R,Color.Gray.G,Color.Gray.B) },                          // stoplights and chandeliers biome !?!
                                                                                               
                { (10, 0), (Color.Red.R,Color.Red.G,Color.Red.B) },                            // flesh biome
                { (10, 1), (Color.Pink.R,Color.Pink.G,Color.Pink.B) },                         // flesh and bone biome
                { (10, 2), (Color.White.R,Color.White.G,Color.White.B) },                      // Bone biome...
                { (10, 3), (Color.DarkRed.R,Color.DarkRed.G,Color.DarkRed.B) },                // blood ocean
                { (10, 4), (Color.YellowGreen.R,Color.YellowGreen.G,Color.YellowGreen.B) },    // acid ocean
                
            };

            // 0 is temperature, 1 is humidity, 2 is acidity, 3 is toxicity, 4 is terrain modifier1, 5 is terrain modifier 2
            public static Dictionary<(int biome, int subBiome), (int temp, int humi, int acid, int toxi, int range, int prio)> biomeTypicalValues = new Dictionary<(int biome, int subBiome), (int temp, int humi, int acid, int toxi, int range, int prio)>
            {
                { (-1, 0), (690, 690, 690, 690, 1000, 0)},  // undefined

                { (0, 0), (200, 320, 320, 512, 1000, 0) },  // cold biome
                { (0, 1), (-100, 320, 320, 512, 1000, 2) }, // frost biome

                { (1, 0), (200, 512, 800, 512, 1000, 0) },  // acid biome

                { (2, 0), (840, 512, 512, 512, 1000, 1) },  // hot biome
                { (2, 1), (1024, 512, 512, 512, 1000, 3) }, // lava ocean biome
                { (2, 2), (880, 880, 512, 512, 1000, 2) },  // obsidian biome...

                { (3, 0), (512, 720, 768, 340, 1000, 0) },  // forest biome
                { (3, 1), (512, 720, 256, 220, 1000, 0) },  // flower forest biome

                { (4, 0), (512, 280, 512, 680, 1000, 0) },  // toxic biome

                { (5, 0), (200, 840, 200, 320, 1000, 0) },  // fairy biome !

                { (8, 0), (512, 960, 512, 512, 1000, 0) },  // ocean biome !

                { (9, 0), (320, 320, 240, 240, 1000, 0) },  // chandeliers biome !

                { (10, 0), (720, 512, 512, 512, 1000, 0) }, // Flesh biome !
                { (10, 1), (512, 360, 380, 512, 1000, 0) }, // Flesh and bone biome !
                { (10, 2), (320, 200, 256, 512, 1000, 0) }, // Bone biome...
                { (10, 3), (320, 880, 380, 360, 1000, 0) }, // Blood ocean biome !
                { (10, 4), (720, 600, 880, 880, 1000, 0) }, // Acid ocean biome !

            };
            public static Dictionary<(int type, int subType), (int r, int g, int b, float mult)> materialColors = new Dictionary<(int type, int subType), (int r, int g, int b, float mult)>
            { // mult is in percent (0-100) : how much biome color is taken into account on the modifiying of the color shite.
                { (-7, 0), (120, 180, 60, 0.2f)}, // acid
                
                { (-6, 0), (100, 15, 25, 0.2f)},  // blood

                { (-5, 0), (160, 120, 70, 0.2f)}, // honey
                
                { (-4, 0), (255, 90, 0, 0.05f)},  // lava
                
                { (-3, 0), (105, 80, 120, 0.2f)}, // fairy liquid
                
                { (-2, 0), (80, 80, 120, 0.2f)},  // water
                
                { (-1, 0), (120, 120, 80, 0.2f)}, // piss
                
                { (0, 0), (140, 140, 140, 0.5f)}, // air lol

                { (1, 0), (30, 30, 30, 0.5f)},    // normal rock
                { (1, 1), (10, 10, 10, 0.2f)},    // dense rock... no... DANCE ROCK !! Yay ! Dance !! Luka Luka Night Fever !!
                
                { (2, 0), (80, 60, 20, 0.5f)},    // dirt
                
                { (3, 0), (10, 60, 30, 0.35f)},   // plant matter
                
                { (4, 0), (135, 55, 55, 0.2f)},   // flesh tile
                { (4, 1), (240, 230, 245, 0.2f)}, // bone tile
            };
            public static List<((int type, int subType, int megaType) material, int count)[]> craftRecipes = new List<((int type, int subType, int megaType) material, int count)[]>
            {
                new ((int type, int subType, int megaType) material, int count)[] // flesh to flesh tile
                {
                    ((8, 0, 3), -10),
                    ((4, 0, 0), 1),
                },
                new ((int type, int subType, int megaType) material, int count)[] // bone to bone tile
                {
                    ((8, 1, 3), -10),
                    ((4, 1, 0), 1),
                },

                new ((int type, int subType, int megaType) material, int count)[] // blood to water and flesh
                {
                    ((-6, 0, 0), -3),
                    ((-2, 0, 0), 3),
                    ((8, 0, 3), 1),
                },

                new ((int type, int subType, int megaType) material, int count)[] // fleshTile to blood
                {
                    ((4, 0, 0), -3),
                    ((-6, 0, 0), 1),
                },

                new ((int type, int subType, int megaType) material, int count)[] // INFINITE FLESH
                {
                    ((8, 0, 3), 100),
                },

                new ((int type, int subType, int megaType) material, int count)[] // Dense rock and Fairy liquid to MagicRock
                {
                    ((1, 1, 0), -1),
                    ((-3, 0, 0), -1),
                    ((10, 0, 3), 1)
                },

                new ((int type, int subType, int megaType) material, int count)[] // Wood, MagicRock and Fairy liquid to MagicWand
                {
                    ((1, 1, 3), -3),
                    ((10, 0, 3), -1),
                    ((-3, 0, 0), -2),
                    ((3, 0, 4), 1)
                },
            };

            public static string[] nameArray = new string[]
            {
                "ka",
                "ko",
                "ku",
                "ki",
                "ke",
                "ro",
                "ra",
                "re",
                "ru",
                "ri",
                "do",
                "da",
                "de",
                "du",
                "di",
                "va",
                "vo",
                "ve",
                "vu",
                "vi",
                "sa",
                "so",
                "se",
                "su",
                "si",
                "in",
                "on",
                "an",
                "en",
                "un",
            };

            public static Dictionary<(int type, int subType, int subSubType), string> structureNames = new Dictionary<(int type, int subType, int subSubType), string>
            {
                { (0, 0, 0), "lake" },
                { (1, 0, 0), "cube amalgam" },
                { (2, 0, 0), "sawblade" },
                { (2, 1, 0), "star" },
            };
        }
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            currentDirectory = System.IO.Directory.GetCurrentDirectory();

            makeTheFilledChunk();
            makeBlackBitmap();
            makeLightBitmaps();

            bool makePngStrings = true;
            if (makePngStrings)
            {
                turnPngIntoString("OverlayBackground");
                turnPngIntoString("Numbers");
                turnPngIntoString("LettersUp");
                turnPngIntoString("LettersLow");
                turnPngIntoString("LettersMin");
                turnPngIntoString("Arrows");
                turnPngIntoString("OperationSigns");

                turnPngIntoString("Fairy");
                turnPngIntoString("ObsidianFairy");
                turnPngIntoString("FrostFairy");
                turnPngIntoString("SkeletonFairy");
                turnPngIntoString("Frog");
                turnPngIntoString("Carnal");
                turnPngIntoString("Skeletal");
                turnPngIntoString("Fish");
                turnPngIntoString("SkeletonFish");
                turnPngIntoString("HornetEgg");
                turnPngIntoString("HornetLarva");
                turnPngIntoString("HornetCocoon");
                turnPngIntoString("Hornet");
                turnPngIntoString("Worm");
                turnPngIntoString("Nematode");
                turnPngIntoString("WaterSkipper");

                turnPngIntoString("Acid");
                turnPngIntoString("Blood");
                turnPngIntoString("Honey");
                turnPngIntoString("Lava");
                turnPngIntoString("FairyLiquid");
                turnPngIntoString("Water");
                turnPngIntoString("Piss");
                turnPngIntoString("BasicTile");
                turnPngIntoString("DenseRockTile");
                turnPngIntoString("FleshTile");
                turnPngIntoString("BoneTile");

                turnPngIntoString("BasePlant");
                turnPngIntoString("Candle");
                turnPngIntoString("Tulip");
                turnPngIntoString("Allium");
                turnPngIntoString("Tree");
                turnPngIntoString("ChandelierTree");
                turnPngIntoString("KelpUpwards");
                turnPngIntoString("KelpDownwards");
                turnPngIntoString("ObsidianPlant");
                turnPngIntoString("Mushroom");
                turnPngIntoString("Vines");
                turnPngIntoString("ObsidianVines");

                turnPngIntoString("PlantMatter");
                turnPngIntoString("Wood");
                turnPngIntoString("Kelp");
                turnPngIntoString("FlowerPetal");
                turnPngIntoString("Pollen");
                turnPngIntoString("MushroomCap");
                turnPngIntoString("MushroomStem");
                turnPngIntoString("Flesh");
                turnPngIntoString("Bone");
                turnPngIntoString("MagicRock");
                turnPngIntoString("Metal");
                turnPngIntoString("LightBulb");
                turnPngIntoString("Wax");

                turnPngIntoString("Sword");
                turnPngIntoString("Pickaxe");
                turnPngIntoString("Scythe");
                turnPngIntoString("MagicWand");

                turnPngIntoString("Fire");
            }

            loadSpriteDictionaries();

            Game game;

            loadStructuresYesOrNo = true;
            spawnPlantsAndEntities = true;

            bool randomSeed = true;

            long seed = 123456;

            // cool ideas for later !
            // add a dimension that is made ouf of pockets inside unbreakable terrain, a bit like an obsidian biome but scaled up.
            // add stoplight biomes not just candelier biome. and make candles have their own biome ?
            // make it possible to visit entities/players inventories lmfao
            // looping dimensions ???? Could be cool. And serve as TELEPORT HUBS ???
            // maybe depending on a parameter of the dimension, some living world dimensions would be more dead or not dead at all.
            // Lolitadimension ?? ? ? or CANDYDIMENSION ???? idk ? ? ? sugar cane trees would be poggers. Or a candy dimension with candies... yeah and uh idk a lolita biome and a super rare variant being a gothic lolita biome ??? idk wtf i'm on ngl
            // Whipped cream biome, chocolate biome... idk
            // Add a portal that is inside lava oceans; in a structure (obsidian city ?), that needs to be turned on with maybe liquid obsidian or oil, and teleports to like hell, or an obsidian dimension made ouf of only obsidian shit ?.
            // Amnesia spell that makes u forget parts of the map lol

            // Entities ideas !
            // add kobolds. Add urchins in ocean biomes that can damage player (maybe) and eat the kelp. Add sharks that eat fish ? And add LITHOPEDIONS
            // Make it so fairies and other creatures have songs. Like maybe in a fairy village there's a village theme song that's procedurally generated. Idk. ANd they can teach u the song and u can sing it with instrument or voice idk.
            // Add winged waterSkipper : when the population in a lake is too high, or food is too scarse, some old enough waterSkippers can become winged, and fly around to lakes with none or few waterSkippers/lots of food. Migration patterns ? idk

            // Plants ideas !
            // Tendril shits in living diomension
            // bone trees and shrubs... like ribs.

            // Lore ideas shit !
            // Carnals and Skeletals in the living dimension are at war. However, due to being made of flesh, only carnals can reproduce. So they end up killing all skeletals.
            // However they need to be at war to live, so they periodically decide, when there's not enough skeletals alive, to put like half of the tribe in ACID,
            // Which turns them into skeletals, these skeletals migrate to the bone biome, and then the war can start again lol. Periodic even that can be witnessed by the player maybe ?
            // Supah powerful wizards can create dimensions. Maybe dimensions that are super weird and shit can be justified this was LOL
            // Star people who have left star structures such as : "star" "big star" "bigbigstar" "bi gbigbigbi g star". Like maybe nests and shite made out of starz.

            //
            // cool seeds !!!! DO NOT DELETE      yeah actually since world gen keeps on changing they're fucking useless LMFAO
            // 		
            // 527503228 : spawn inside a giant obsidian biome !
            // 1115706211 : very cool spawn, with all the 7 current biomes types near and visitable and amazing looking caves (hahaha 7 different biomes... i'm old)
            // 947024425 : the biggest fucking obsidian biome i've ever seen. Not near the spawn, go FULL RIGHT, at around 130-140 chunks far right. What the actual fuck it's so big (that's what she said)
            // 2483676441 : what the ACTUAL fuck this obsidian biome is like 4 screens high ???????? holy fuck. Forest at the bottom.
            // 3496528327 : deep cold biome spawn
            // 3253271960 : another deep cold biome spawn
            // 1349831907 : enormous deep cold biome spawn
            // 3776667052 : yet again deep cold biome spawn
            // 1561562999 : onion san head... amazing
            // 1809684240 : go down, very very big water lake normally (don't forget to DELETE THE SAVE)
            // 2807443684 : the most FUCKING enormous OCEAN biome it is so fucking big... wtf
            // 3548078961 : giant fish in oceaon omggggg also banger terrain like wtf
            // 3452270044 : chill start frost inside ocean
            //

            if (randomSeed)
            {
                seed = rand.Next(1000000);
                int counto = rand.Next(1000);
                while (counto > 0)
                {
                    seed = LCGxPos(seed);
                    counto -= 1;
                }
            }
            worldSeed = seed;

            Files.createFolders(seed);

            game = new Game(worldSeed);
            timer1.Tag = game;
            timeAtLauch = DateTime.Now;
        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            //Form1_Load(new object(), new EventArgs());
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            Game game = (Game)timer1.Tag;
            game.runGame(gamePictureBox, overlayPictureBox);
        }
        private void KeyIsDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left)
            {
                arrowKeysState[0] = true;
            }
            if (e.KeyCode == Keys.Right)
            {
                arrowKeysState[1] = true;
            }
            if (e.KeyCode == Keys.Down)
            {
                arrowKeysState[2] = true;
            }
            if (e.KeyCode == Keys.Up)
            {
                arrowKeysState[3] = true;
            }
            if (e.KeyCode == Keys.X)
            {
                digPress = true;
            }
            if (e.KeyCode == Keys.Z)
            {
                placePress[0] = true;
            }
            if (e.KeyCode == Keys.W)
            {
                placePress[1] = true;
            }
            if (e.KeyCode == Keys.S)
            {
                zoomPress[0] = true;
            }
            if (e.KeyCode == Keys.D)
            {
                zoomPress[1] = true;
            }
            if (e.KeyCode == Keys.C)
            {
                inventoryChangePress[0] = true;
            }
            if (e.KeyCode == Keys.V)
            {
                inventoryChangePress[1] = true;
            }
            if (e.KeyCode == Keys.P)
            {
                pausePress = true;
            }
            if (e.KeyCode == Keys.F)
            {
                fastForward = true;
            }
            if (e.KeyCode == Keys.K && !dimensionChangePress)
            {
                craftPress = true;
            }
            if (e.KeyCode == Keys.M)
            {
                debugMode = !debugMode;
            }
            if (e.KeyCode == Keys.L && !craftPress)
            {
                dimensionChangePress = true;
            }
            if ((Control.ModifierKeys & Keys.Shift) != 0)
            {
                shiftPress = true;
            }
        }
        private void KeyIsUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left)
            {
                arrowKeysState[0] = false;
            }
            if (e.KeyCode == Keys.Right)
            {
                arrowKeysState[1] = false;
            }
            if (e.KeyCode == Keys.Down)
            {
                arrowKeysState[2] = false;
            }
            if (e.KeyCode == Keys.Up)
            {
                arrowKeysState[3] = false;
            }
            if (e.KeyCode == Keys.X)
            {
                digPress = false;
            }
            if (e.KeyCode == Keys.Z)
            {
                placePress[0] = false;
            }
            if (e.KeyCode == Keys.W)
            {
                placePress[1] = false;
            }
            if (e.KeyCode == Keys.S)
            {
                zoomPress[0] = false;
            }
            if (e.KeyCode == Keys.D)
            {
                zoomPress[1] = false;
            }
            if (e.KeyCode == Keys.C)
            {
                inventoryChangePress[0] = false;
            }
            if (e.KeyCode == Keys.V)
            {
                inventoryChangePress[1] = false;
            }
            if (e.KeyCode == Keys.P)
            {
                pausePress = false;
            }
            if (e.KeyCode == Keys.F)
            {
                fastForward = false;
            }
            if (e.KeyCode == Keys.K)
            {

            }
            if (e.KeyCode == Keys.M)
            {

            }
            if (e.KeyCode == Keys.L)
            {

            }
            if ((Control.ModifierKeys & Keys.Shift) == 0)
            {
                shiftPress = false;
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Game game = (Game)timer1.Tag;
            foreach (Screens.Screen screen in game.loadedScreens.Values)
            {
                screen.putEntitiesAndPlantsInChunks();
                saveAllChunks(screen);
            }
        }
        public static void makeBiomeDiagram((int, int) dimensionType, (int, int) variablesToTest, (int, int) fixedValues)
        {
            Dictionary<int, string> dicto = new Dictionary<int, string>
            {
                { 0, "temperature" },
                { 1, "humidity" },
                { 2, "acidity" },
                { 3, "toxicity" }
            };
            (int, int) fixedValuesIdx = (0, 0);

            int[] values = new int[4];
            bool addZone = false;
            for (int i = 0; i < 4; i++)
            {
                if (variablesToTest.Item1 == i)
                {
                    values[i] = -1;
                }
                else if (variablesToTest.Item2 == i)
                {
                    values[i] = -1;
                }
                else if (!addZone)
                {
                    values[i] = fixedValues.Item1;
                    fixedValuesIdx = (i, fixedValuesIdx.Item2);
                    addZone = true;
                }
                else
                {
                    values[i] = fixedValues.Item2;
                    fixedValuesIdx = (fixedValuesIdx.Item1, i);
                }
            }
            Bitmap bitmap = new Bitmap(256, 256);
            Color colorToPut;
            for (int i = -64; i < 192; i++)
            {
                values[variablesToTest.Item1] = i*8;
                for (int j = -64; j < 192; j++)
                {
                    values[variablesToTest.Item2] = j*8;
                    ((int biome, int subBiome), int)[] biomeArray = findBiome(dimensionType, values);
                    //(int temp, int humi, int acid, int toxi) tileValues = makeTileBiomeValueArray(values, i, j);
                    if (biomeArray[0].Item1 == (-1, 0) && ((i/4)+(j/4))%2 == 1){ setPixelButFaster(bitmap, (i, 255 - j), Color.Black); continue; }
                    int[] colorArray = findBiomeColor(biomeArray);
                    colorToPut = Color.FromArgb(colorArray[0], colorArray[1], colorArray[2]);
                    setPixelButFaster(bitmap, (i+64, 191-j), colorToPut);
                }
            }
            bitmap.Save($"{currentDirectory}\\BiomeDiagrams\\biomeDiagram   {dicto[fixedValuesIdx.Item1]}={fixedValues.Item1}, {dicto[fixedValuesIdx.Item2]}={fixedValues.Item2}.png");
        }
    }
    public class MathF
    {
        public static long LCGxPos(long seed) // WARNING the 1073741824 is not 2^32 but it's 2^30 cause... lol
        {
            return ((long)(55797) * seed + (long)9973) % (long)4294967291;
        }
        public static long LCGxNeg(long seed)
        {
            return ((long)(12616645) * seed + (long)8123) % (long)4294967291;
        }
        public static long LCGyPos(long seed)
        {
            return ((long)(251253) * seed + (long)6763) % (long)4294967291;
        }
        public static long LCGyNeg(long seed)
        {
            return (long)((121525) * seed + (long)9109) % (long)4294967291;
        }
        public static long LCGz(long seed)
        {
            return (long)((121525) * seed + (long)6763) % (long)4294967291;
        }
        public static int LCGint1(int seed)
        {
            return Abs((121525 * seed + 6763) % 999983); // VERY SMALL HAVE TO REDO IT
        }
        public static int LCGint2(int seed)
        {
            return Abs((12616645 * seed + 8837) % 998947); // For some reason it DOES NOT WORK ??? The RANDOM is NOT wroking
        }

        public static void Sort(List<(int, int)> listo, bool sortByFirstInt)
        {
            if (sortByFirstInt)
            {
                int idx = 0;
                while (idx < listo.Count - 1)
                {
                    if (listo[idx + 1].Item1 > listo[idx].Item1 || (listo[idx + 1].Item1 == listo[idx].Item1 && listo[idx + 1].Item2 > listo[idx].Item2))
                    {
                        listo.Insert(idx, listo[idx + 1]);
                        listo.RemoveAt(idx + 2);
                        idx -= 2;
                    }
                    idx += 1;
                }
            }
            else
            {
                int idx = 0;
                while (idx < listo.Count - 1)
                {
                    if (listo[idx + 1].Item2 > listo[idx].Item2 || (listo[idx + 1].Item2 == listo[idx].Item2 && listo[idx + 1].Item1 > listo[idx].Item1))
                    {
                        listo.Insert(idx, listo[idx + 1]);
                        listo.RemoveAt(idx + 2);
                        idx -= 2;
                    }
                    idx = Max(0, idx + 1);
                }
            }
        }

        public static void SortByItem2(List<((int biome, int subBiome), int)> listo)
        {
            int idx = 0;
            while (idx < listo.Count - 1)
            {
                if (listo[idx + 1].Item2 > listo[idx].Item2 || (listo[idx + 1].Item2 == listo[idx].Item2 && listo[idx + 1].Item1.biome > listo[idx].Item1.biome))
                {
                    listo.Insert(idx, listo[idx + 1]);
                    listo.RemoveAt(idx + 2);
                    idx -= 2;
                }
                idx = Max(0, idx + 1);
            }
        }

        public static float Max(float a, float b)
        {
            if (a > b) { return a; }
            return b;
        }
        public static int Max(int a, int b)
        {
            if (a > b) { return a; }
            return b;
        }
        public static byte Max(byte a, byte b)
        {
            if (a > b) { return a; }
            return b;
        }
        public static float Min(float a, float b)
        {
            if (a < b) { return a; }
            return b;
        }
        public static int Min(int a, int b)
        {
            if (a < b) { return a; }
            return b;
        }
        public static byte Min(byte a, byte b)
        {
            if (a < b) { return a; }
            return b;
        }
        public static float Clamp(float min, float value, float max)
        {
            if (value > max) { return max; }
            if (value < min) { return min; }
            return value;
        }
        public static int Clamp(int min, int value, int max)
        {
            if (value > max) { return max; }
            if (value < min) { return min; }
            return value;
        }
        public static int ColorClamp(int value)
        {
            if (value > 255) { return 255; }
            if (value < 0) { return 0; }
            return value;
        }
        public static int Floor(int value, int modulo)
        {
            return value - (((value % modulo) + modulo) % modulo);
        }
        public static float Floor(float value, float modulo)
        {
            return value - (((value % modulo) + modulo) % modulo);
        }
        public static long Floor(long value, long modulo)
        {
            return value - (((value % modulo) + modulo) % modulo);
        }
        public static (int, int) MegaChunkIdx((int x, int y) pos) // PUT CHUNKPOS inside ! NOT tilePos !
        {
            int chunkPosX = Floor(pos.x, 16) / 16;
            int chunkPosY = Floor(pos.y, 16) / 16;
            return (chunkPosX, chunkPosY);
        }
        public static (int, int) ChunkIdx(int pixelPosX, int pixelPosY)
        {
            int chunkPosX = Floor(pixelPosX, 32) / 32;
            int chunkPosY = Floor(pixelPosY, 32) / 32;
            return (chunkPosX, chunkPosY);
        }
        public static (int, int) ChunkIdx((int x, int y) pos)
        {
            int chunkPosX = Floor(pos.x, 32) / 32;
            int chunkPosY = Floor(pos.y, 32) / 32;
            return (chunkPosX, chunkPosY);
        }
        public static int ChunkIdx(int pos)
        {
            return Floor(pos, 32) / 32;
        }
        public static (int, int) StructChunkIdx(int pixelPosX, int pixelPosY)
        {
            int chunkPosX = Floor(pixelPosX, 512) / 512;
            int chunkPosY = Floor(pixelPosY, 512) / 512;
            return (chunkPosX, chunkPosY);
        }
        public static (int, int) StructChunkIdx((int x, int y) pos)
        {
            int chunkPosX = Floor(pos.x, 512) / 512;
            int chunkPosY = Floor(pos.y, 512) / 512;
            return (chunkPosX, chunkPosY);
        }
        public static int StructChunkIdx(int pos)
        {
            return Floor(pos, 512) / 512;
        }
        public static int Sign(int a)
        {
            if (a >= 0) { return 1; }
            return -1;
        }
        public static int Sign(float a)
        {
            if (a >= 0) { return 1; }
            return -1;
        }
        public static float Abs(float a)
        {
            if (a >= 0) { return a; }
            return -a;
        }
        public static int Abs(int a)
        {
            if (a >= 0) { return a; }
            return -a;
        }
        public static long Abs(long a)
        {
            if (a >= 0) { return a; }
            return -a;
        }
        public static int Sqrt(int n)
        {
            int sq = 1;
            while (sq < n / sq)
            {
                sq++;
            }
            if (sq > n / sq) return sq - 1;
            return sq;
        }

        //star see saw is the function used to make the*... circular blades
        public static int sawBladeSeesaw(int n, int mod)
        {
            n = ((n % mod) + n) % mod; // additional "+ n" that has falsifies the seesaw (frequency*2) but we'll leave it for sawblades for now lol
            int n2 = n % (mod / 2);
            if (n == n2) { return n; }
            return n - n2;
        }
        public static int Seesaw(int n, int mod)
        {
            n = n % mod;
            int n2 = n % (mod / 2);
            if (n == n2) { return n; }
            return n - n2*2;
        }
        public static float Seesaw(float n, float mod)
        {
            n = n % mod;
            float n2 = n % (mod*0.5f);
            if (n == n2) { return n; }
            return n - n2 * 2;
        }
        public static float sawBladeSeesaw(float n, float mod)
        {
            n = ((n % mod) + n) % mod; // additional "+ n" that has falsifies the seesaw (frequency*2) but we'll leave it for sawblades for now lol
            float n2 = n % (mod / 2);
            if (n == n2) { return n; }
            return n - n2*2;
        }
        public static float Obseesaw(float n, float mod)
        {
            n = ((n % mod) + n) % mod;
            float n2 = n % (mod / 2);
            if (n * 3 > mod && n * 3 < mod * 2) { return mod * 0.33f; }
            if (n == n2) { return n; }
            return n - n2 * 2;
        }
        public static float Sin(float n, float period)
        {
            n = sawBladeSeesaw(n, period);
            return (n * n) / (period * period * 0.25f);
        }
        public static float Obs(float n, float period)
        {
            n = Obseesaw(n, period);
            return (n * n) / (period * period * 0.25f);
        }
        public static (int x, int y) PosMod((int x, int y) poso, int mod = 32)
        {
            int posX = poso.x % mod;
            int posY = poso.y % mod;
            if (posX < 0) { posX += mod; }
            if (posY < 0) { posY += mod; }
            return (posX, posY);
        }
        public static int PosMod(int poso, int modulo = 32)
        {
            int pos = poso % modulo;
            if (pos < 0) { pos += modulo; }
            return pos;
        }
        public static float PosMod(float poso, float modulo = 32)
        {
            float pos = poso % modulo;
            if (pos < 0) { pos += modulo; }
            return pos;
        }
        public static int getBound(List<(int x, int y)> listo, bool testY, bool testMax)
        {
            if (listo.Count == 0) { return 0; }
            int result;
            if (testY)
            {
                result = listo[0].y;
                foreach ((int x, int y) pos in listo)
                {
                    if ((testMax && pos.y > result) || (!testMax && pos.y < result))
                    {
                        result = pos.y;
                    }
                }
            }
            else
            {
                result = listo[0].x;
                foreach ((int x, int y) pos in listo)
                {
                    if ((testMax && pos.x > result) || (!testMax && pos.x < result))
                    {
                        result = pos.x;
                    }
                }
            }
            return result;
        }
        public static int manhattanDistance((int x, int y) pos1, (int x, int y) pos2)
        {
            return Abs(pos1.x - pos2.x) + Abs(pos1.y - pos2.y);
        }
        public static float distance((int x, int y) pos1, (int x, int y) pos2)
        {
            int distX = (pos1.x - pos2.x);
            int distY = (pos1.y - pos2.y);
            return Sqrt(distX*distX + distY*distY);
        }


        // functions to randomize shit
        public static List<T> shuffleList<T>(List<T> listo)
        {
            int posToSwitch;
            for (int i = listo.Count-1; i >= 0; i--)
            {
                posToSwitch = rand.Next(i+1);
                var elementHeld = listo[i];
                listo[i] = listo[posToSwitch];
                listo[posToSwitch] = elementHeld;
            }
            for (int i = listo.Count - 1; i >= 0; i--)
            {
                posToSwitch = rand.Next(i + 1);
                var elementHeld = listo[i];
                listo[i] = listo[posToSwitch];
                listo[posToSwitch] = elementHeld;
            } // do it twice because yes ! So element can be at its original position maybe ?
            return listo;
        }
    }
}