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

            public static bool[] arrowKeysState = { false, false, false, false };
            public static bool digPress = false;
            public static bool[] placePress = { false, false };
            public static bool[] zoomPress = { false, false };
            public static bool[] inventoryChangePress = { false, false };
            public static bool pausePress = false;
            public static bool fastForward = false;
            public static bool debugMode = false;
            public static bool debugMode2 = false;
            public static bool shiftPress = false;
            public static bool doShitPress = false;
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
            public static (int, int)[] bubbleNeighbourArray = new (int, int)[8] { (-1, 0), (1, 0), (0, 1), (0, -1), (-2, 0), (2, 0), (0, 2), (0, -2) };
            public static (int, int)[] diagNeighbourArray = new (int, int)[4] { (-1, 1), (1, 1), (1, -1), (-1, -1) };

            public static Dictionary<int, int> costDict = new Dictionary<int, int>
            {
                { 0, 1 }, // air
                { -1, 3 }, // piss
                { -2, 3 }, // water
                { -3, 10 }, // fairy liquid
                { -4, 999999 }, // lava (cannot cross)
                { -5, 5 }, // honey
                { -6, 3 }, // blood
                { -7, 999999 }, // acid (cannot cross)
            };

            public static Dictionary<(int biome, int subBiome), bool> darkBiomes = new Dictionary<(int biome, int subBiome), bool>
            {
                { (9, 0), true }, // chandelier biome
            };

            public static Dictionary<(int biome, int subBiome), (int, int, int)> biomeDict = new Dictionary<(int biome, int subBiome), (int, int, int)>
            {
                { (-1, 0), (1200, -100, 1200) }, // undefined

                { (0, 0), (Color.Blue.R,Color.Blue.G,Color.Blue.B) }, // cold biome
                { (0, 1), (Color.LightBlue.R,Color.LightBlue.G,Color.LightBlue.B) }, // frost biome

                { (1, 0), (Color.Fuchsia.R,Color.Fuchsia.G,Color.Fuchsia.B) }, // acid biome

                { (2, 0), (Color.OrangeRed.R,Color.OrangeRed.G,Color.OrangeRed.B) }, // hot biome
                { (2, 1), (Color.OrangeRed.R + 90,Color.OrangeRed.G + 30,Color.OrangeRed.B) }, // lava ocean biome
                { (2, 2), (-100,-100,-100) }, // obsidian biome...

                { (3, 0), (Color.Green.R,Color.Green.G,Color.Green.B)}, // forest biome
                { (3, 1), (Color.Green.R,Color.Green.G + 40,Color.Green.B + 80)}, // flower forest biome

                { (4, 0), (Color.GreenYellow.R,Color.GreenYellow.G,Color.GreenYellow.B) }, // toxic biome

                { (5, 0), (Color.LightPink.R,Color.LightPink.G,Color.LightPink.B) }, // fairy biome !

                { (8, 0), (Color.LightBlue.R,Color.LightBlue.G+60,Color.LightBlue.B+130) }, // ocean biome !

                { (9, 0), (Color.Gray.R,Color.Gray.G,Color.Gray.B) }, // stoplights and chandeliers biome !?!

                { (10, 0), (Color.Red.R,Color.Red.G,Color.Red.B) }, // flesh biome
                { (10, 1), (Color.Pink.R,Color.Pink.G,Color.Pink.B) }, // flesh and bone biome
                { (10, 2), (Color.DarkRed.R,Color.DarkRed.G,Color.DarkRed.B) }, // blood ocean
                { (10, 3), (Color.YellowGreen.R,Color.YellowGreen.G,Color.YellowGreen.B) }, // acid ocean
                
                { (11, 0), (Color.White.R,Color.White.G,Color.White.B) }, // Bone biome...
            };

            // 0 is temperature, 1 is humidity, 2 is acidity, 3 is toxicity, 4 is terrain modifier1, 5 is terrain modifier 2
            public static Dictionary<(int biome, int subBiome), (int temp, int humi, int acid, int toxi, int range, int prio)> biomeTypicalValues = new Dictionary<(int biome, int subBiome), (int temp, int humi, int acid, int toxi, int range, int prio)>
            {
                { (0, 0), (200, 320, 320, 512, 1000, 0) }, // cold biome
                { (0, 1), (-100, 320, 320, 512, 1000, 2) }, // frost biome

                { (1, 0), (200, 512, 800, 512, 1000, 0) }, // acid biome

                { (2, 0), (840, 512, 512, 512, 1000, 1) }, // hot biome
                { (2, 1), (1024, 512, 512, 512, 1000, 3) }, // lava ocean biome
                { (2, 2), (880, 880, 512, 512, 1000, 2) }, // obsidian biome...

                { (3, 0), (512, 720, 768, 340, 1000, 0) }, // forest biome
                { (3, 1), (512, 720, 256, 220, 1000, 0) }, // flower forest biome

                { (4, 0), (512, 280, 512, 680, 1000, 0) }, // toxic biome

                { (5, 0), (200, 840, 200, 320, 1000, 0) }, // fairy biome !

                { (8, 0), (512, 960, 512, 512, 1000, 0) }, // ocean biome !

                { (9, 0), (320, 320, 240, 240, 1000, 0) }, // chandeliers biome !

                { (10, 0), (720, 512, 512, 512, 1000, 0) }, // Flesh biome !
                { (10, 1), (512, 360, 380, 512, 1000, 0) }, // Flesh and bone biome !
                { (10, 2), (320, 880, 380, 360, 1000, 0) }, // Blood ocean biome !
                { (10, 3), (720, 600, 880, 880, 1000, 0) }, // Acid ocean biome !

                { (11, 0), (320, 200, 256, 512, 1000, 0) }, // Bone biome...

            };
            public static Dictionary<(int type, int subType), (int r, int g, int b, float mult)> materialColors = new Dictionary<(int type, int subType), (int r, int g, int b, float mult)>
            { // mult is in percent (0-100) : how much biome color is taken into account on the modifiying of the color shite.
                { (-7, 0), (120, 180, 60, 0.2f)}, // acid
                
                { (-6, 0), (100, 15, 25, 0.2f)}, // blood

                { (-5, 0), (160, 120, 70, 0.2f)}, // honey
                
                { (-4, 0), (255, 90, 0, 0.05f)}, // lava
                
                { (-3, 0), (105, 80, 120, 0.2f)}, // fairy liquid
                
                { (-2, 0), (80, 80, 120, 0.2f)}, // water
                
                { (-1, 0), (120, 120, 80, 0.2f)}, // piss
                
                { (0, 0), (140, 140, 140, 0.5f)}, // air lol

                { (1, 0), (30, 30, 30, 0.5f)}, // normal rock
                { (1, 1), (10, 10, 10, 0.2f)}, // dense rock... no... DANCE ROCK !! Yay ! Dance !! Luka Luka Night Fever !!
                
                { (2, 0), (80, 60, 20, 0.5f)}, // dirt
                
                { (3, 0), (10, 60, 30, 0.35f)}, // plant matter
                
                { (4, 0), (135, 55, 55, 0.2f)}, // flesh tile
                { (4, 1), (240, 230, 245, 0.2f)}, // bone tile
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
            
            public static string[] structureNames = new string[]
            {
                "cube amalgam",
                "sawblade",
                "star",
                "lake"
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

                turnPngIntoString("Fairy");
                turnPngIntoString("ObsidianFairy");
                turnPngIntoString("FrostFairy");
                turnPngIntoString("SkeletonFairy");
                turnPngIntoString("Frog");
                turnPngIntoString("Carnal");
                turnPngIntoString("Skeletal");
                turnPngIntoString("Fish");
                turnPngIntoString("SkeletonFish");
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
                turnPngIntoString("Flesh");
                turnPngIntoString("Bone");

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

                turnPngIntoString("Pollen");
                turnPngIntoString("PlantMatter");
                turnPngIntoString("FlowerPetal");
                turnPngIntoString("Wood");
                turnPngIntoString("Kelp");
                turnPngIntoString("MushroomCap");
                turnPngIntoString("MushroomStem");

                turnPngIntoString("Fire");
            }

            loadSpriteDictionaries();

            Game game;

            loadStructuresYesOrNo = true;
            spawnPlantsAndEntities = true;

            bool randomSeed = true;

            long seed = 123456;

            // cool ideas for later !
            // add kobolds. Add urchins in ocean biomes that can damage player (maybe) and eat the kelp. Add sharks that eat fish ? And add LITHOPEDIONS
            // add a dimension that is made ouf of pockets inside unbreakable terrain, a bit like an obsidian biome but scaled up.
            // add stoplight biomes not just candelier biome. and make candles have their own biome ?
            // make it possible to visit entities/players inventories lmfao
            // looping dimensions ???? Could be cool. And serve as TELEPORT HUBS ???
            // bone trees and shrubs... like ribs. Maybe a BONE dimension ! Or biome inside the living dimension... kinda good yesyes, a dead part of the living dimension.
            // maybe depending on a parameter of the dimension, some living world dimensions would be more dead or not dead at all.
            // Lolitadimension ?? ? ? or CANDYDIMENSION ???? idk ? ? ? sugar cane trees would be poggers. Or a candy dimension with candies... yeah and uh idk a lolita biome and a super rare variant being a gothic lolita biome ??? idk wtf i'm on ngl
            // Whipped cream biome, chocolate biome... idk
            // Add a portal that is inside lava oceans; in a structure (obsidian city ?), that needs to be turned on with maybe liquid obsidian or oil, and teleports to like hell, or an obsidian dimension made ouf of only obsidian shit ?.
            // Make it so fairies and other creatures have songs. Like maybe in a fairy village there's a village theme song that's procedurally generated. Idk. ANd they can teach u the song and u can sing it with instrument or voice idk.

            //
            // cool seeds !!!! DO NOT DELETE
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
            if (e.KeyCode == Keys.Right)
            {
                arrowKeysState[0] = true;
            }
            if (e.KeyCode == Keys.Left)
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
            if (e.KeyCode == Keys.K)
            {
                doShitPress = true;
            }
            if (e.KeyCode == Keys.M)
            {
                debugMode = !debugMode;
            }
            if (e.KeyCode == Keys.L)
            {
                debugMode2 = !debugMode2;
            }
            if ((Control.ModifierKeys & Keys.Shift) != 0)
            {
                shiftPress = true;
            }
        }
        private void KeyIsUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Right)
            {
                arrowKeysState[0] = false;
            }
            if (e.KeyCode == Keys.Left)
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
                screen.saveAllChunks();
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
        public static int PosMod(int value, int modulo)
        {
            return ((value % modulo) + modulo) % modulo;
        }
        public static float PosMod(float value, float modulo)
        {
            return ((value % modulo) + modulo) % modulo;
        }
        public static long PosMod(long value, long modulo)
        {
            return ((value % modulo) + modulo) % modulo;
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
        public static (int x, int y) GetChunkIndexFromTile((int x, int y) poso)
        {
            int posX = poso.x % 32;
            int posY = poso.y % 32;
            if (posX < 0) { posX += 32; }
            if (posY < 0) { posY += 32; }
            return (posX, posY);
        }
        public static (int x, int y) GetChunkIndexFromTile(int posoX, int posoY)
        {
            int posX = posoX % 32;
            int posY = posoY % 32;
            if (posX < 0) { posX += 32; }
            if (posY < 0) { posY += 32; }
            return (posX, posY);
        }
        public static int GetChunkIndexFromTile1D(int poso)
        {
            int pos = poso % 32;
            if (pos < 0) { pos += 32; }
            return pos;
        }
        public static int GetChunkIndexFromTile1D(int poso, int modulo)
        {
            int pos = poso % modulo;
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