using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
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
using static Cave.Entities;
using static Cave.Files;
using static Cave.Plants;
using static Cave.Screens;
using static Cave.Chunks;
using static Cave.Players;


namespace Cave
{
    public partial class Form1 : Form
    {
        public const float div32 = 0.03125f;
        public class Globals
        {
            public static bool loadStructuresYesOrNo = true;

            public static int ChunkLength = 4;
            public static int UnloadedChunksAmount = 8;

            public static Bitmap black32Bitmap = new Bitmap(32, 32);
            public static Chunk theFilledChunk;

            public static Random rand = new Random();

            public static bool[] arrowKeysState = { false, false, false, false };
            public static bool digPress = false;
            public static bool[] placePress = { false, false };
            public static bool[] zoomPress = { false, false };
            public static bool[] inventoryChangePress = { false, false };
            public static bool pausePress = false;
            public static bool fastForward = false;
            public static bool debugMode = false;
            public static bool shiftPress = false;
            public static bool doShitPress = false;
            public static float lastZoom = 0;
            public static DateTime timeAtLauch;
            public static float timeElapsed = 0;

            public static string currentDirectory;

            public static int currentStructureId  = 0;
            public static int currentEntityId   = 0;
            public static int currentPlantId  = 0;
            public static int currentNestId = 0;

            public static long worldSeed = 0;

            public static (int, int)[] neighbourArray = new (int, int)[4] { (-1, 0), (1, 0), (0, 1), (0, -1) };
            public static (int, int)[] bubbleNeighbourArray = new (int, int)[8] { (-1, 0), (1, 0), (0, 1), (0, -1), (-2, 0), (2, 0), (0, 2), (0, -2) };
            public static (int, int)[] diagNeighbourArray = new (int, int)[4] { (-1, 1), (1, 1), (1, -1), (-1, -1) };

            public static Dictionary<int, int> costDict = new Dictionary<int, int>
            {
                { 0, 1 }, // air
                { -1, 3 }, // piss
                { -2, 3 }, // water
                { -3, 3 }, // fairy liquid
                { -4, 999999 }, // lava (cannot cross)
                { -5, 5 }, // honey
            };

            public static Dictionary<int, (int, int, int)> biomeDict = new Dictionary<int, (int, int, int)>
            {
                { 0, (Color.Blue.R,Color.Blue.G,Color.Blue.B) }, // cold biome
                { 1, (Color.Fuchsia.R,Color.Fuchsia.G,Color.Fuchsia.B) }, // acid biome
                { 2, (Color.OrangeRed.R,Color.OrangeRed.G,Color.OrangeRed.B) }, // hot biome
                { 3, (Color.Green.R,Color.Green.G,Color.Green.B)}, // plant biome
                { 4, (Color.GreenYellow.R,Color.GreenYellow.G,Color.GreenYellow.B) }, // toxic biome
                { 5, (Color.LightPink.R,Color.LightPink.G,Color.LightPink.B) }, // fairy biome !
                { 6, (-100,-100,-100) }, // obsidian biome...
                { 7, (Color.LightBlue.R,Color.LightBlue.G,Color.LightBlue.B) }, // frost biome
                { 8, (Color.LightBlue.R,Color.LightBlue.G+60,Color.LightBlue.B+130) }, // ocean biome !
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

            if (false)
            {
                turnPngIntoString("OverlayBackground");
                turnPngIntoString("Numbers");
                turnPngIntoString("BasicTile");

                turnPngIntoString("Fairy");
                turnPngIntoString("ObsidianFairy");
                turnPngIntoString("FrostFairy");
                turnPngIntoString("Frog");
                turnPngIntoString("Fish");
                turnPngIntoString("Hornet");

                turnPngIntoString("Piss");
                turnPngIntoString("Water");
                turnPngIntoString("FairyLiquid");
                turnPngIntoString("Lava");
                turnPngIntoString("Honey");

                turnPngIntoString("BasePlant");
                turnPngIntoString("Tree");
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
            }

            loadSpriteDictionaries();

            Screens.Screen mainScreen;
            SettingsJson settings;

            loadStructuresYesOrNo = true;

            bool updatePNG = false;
            int PNGsize = 120; // in chunks, 300 or more made it out of memory :( so put at 250 okok
            bool randomSeed = true;

            long seed = 123456;

            // cool ideas for later !
            // add kobolds. Add urchins in ocean biomes that can damage player (maybe) and eat the kelp. Add sharks that eat fish ?
            // add a dimension that is made ouf of pockets inside unbreakable terrain, a bit like an obsidian biome but scaled up.
            // add a dimension with CANDLE TREES (arbres chandeliers) that could be banger
            // make it possible to visit entities/players inventories lmfao

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
                seed = (long)rand.Next(1000000);
                int counto = rand.Next(1000);
                while (counto > 0)
                {
                    seed = LCGxPos(seed);
                    counto -= 1;
                }
            }
            worldSeed = seed;

            Files.createFolders(seed);

            timeElapsed = 0;
            if (updatePNG)
            {
                int oldChunkLength = ChunkLength;
                ChunkLength = PNGsize;
                settings = tryLoadSettings(seed);
                if (settings != null) { timeElapsed = settings.time; }
                mainScreen = new Screens.Screen(ChunkLength, seed, true, settings);
                timer1.Tag = mainScreen;
                timeAtLauch = DateTime.Now;

                timer1_Tick(new object(), new EventArgs());

                mainScreen.updateScreen().Save($"{currentDirectory}\\CaveData\\cavee.png");
                ChunkLength = oldChunkLength;
            }

            settings = tryLoadSettings(seed);
            if (settings != null) { timeElapsed = settings.time; }
            mainScreen = new Screens.Screen(ChunkLength, seed, false, settings);
            timer1.Tag = mainScreen;
            timeAtLauch = DateTime.Now;
        }
        public static long findPlantSeed(long posX, long posY, Screens.Screen screen, int layer)
        {
            long x = posX;
            long y = posY;
            long seedX;
            if (x >= 0)
            {
                seedX = screen.LCGCacheListMatrix[layer, 0][(int)(x / 50)];
                x = x % 50;
                while (x > 0)
                {
                    seedX = LCGxPos(seedX);
                    x--;
                }
            }
            else
            {
                x = -x;
                seedX = screen.LCGCacheListMatrix[layer, 1][(int)(x / 50)];
                x = x % 50;
                while (x > 0)
                {
                    seedX = LCGxNeg(seedX);
                    x--;
                }
            }
            long seedY;
            if (y >= 0)
            {
                seedY = screen.LCGCacheListMatrix[layer, 2][(int)(y / 50)];
                y = y % 50;
                while (y > 0)
                {
                    seedY = LCGyPos(seedY);
                    y--;
                }
            }
            else
            {
                y = -y;
                seedY = screen.LCGCacheListMatrix[layer, 3][(int)(y / 50)];
                y = y % 50;
                while (y > 0)
                {
                    seedY = LCGyNeg(seedY);
                    y--;
                }
            }
            int z = (int)((256 + seedX % 256 + seedY % 256) % 256);
            long seedZ = screen.LCGCacheListMatrix[layer, 4][(int)(z / 50)];
            z = z % 50;
            while (z > 0)
            {
                seedZ = LCGz(seedZ);
                z--;
            }
            return (seedZ + seedX + seedY)/3;
            //return ((int)(seedX%512)-256, (int)(seedY%512)-256);
        }
        public static int findPrimaryNoiseValue(long posX, long posY, long seed, int layer)
        {
            long x = posX;
            long y = posY;
            long seedX = seed;
            if (x >= 0)
            {
                while (x > 0)
                {
                    seedX = LCGxPos(seedX);
                    x--;
                }
            }
            else
            {
                x = -x;
                while (x > 0)
                {
                    seedX = LCGxNeg(seedX);
                    x--;
                }
            }
            long seedY = seed;
            if (y >= 0)
            {
                while (y > 0)
                {
                    seedY = LCGyPos(seedY);
                    y--;
                }
            }
            else
            {
                y = -y;
                while (y > 0)
                {
                    seedY = LCGyNeg(seedY);
                    y--;
                }
            }
            int z = (int)((256 + seedX % 256 + seedY % 256) % 256);
            long seedZ = z;
            while (z > 0)
            {
                seedZ = LCGz(seedZ);
                z--;
            }
            return (int)((seedZ + seedX + seedY) % 256);
        }
        public static int findPrimaryNoiseValueCACHE(long posX, long posY, Screens.Screen screen, int layer)
        {
            long x = posX;
            long y = posY;
            long seedX;
            if (x >= 0)
            {
                seedX = screen.LCGCacheListMatrix[layer, 0][(int)(x / 50)];
                x = x % 50;
                while (x > 0)
                {
                    seedX = LCGxPos(seedX);
                    x--;
                }
            }
            else
            {
                x = -x;
                seedX = screen.LCGCacheListMatrix[layer, 1][(int)(x / 50)];
                x = x % 50;
                while (x > 0)
                {
                    seedX = LCGxNeg(seedX);
                    x--;
                }
            }
            long seedY;
            if (y >= 0)
            {
                seedY = screen.LCGCacheListMatrix[layer, 2][(int)(y / 50)];
                y = y % 50;
                while (y > 0)
                {
                    seedY = LCGyPos(seedY);
                    y--;
                }
            }
            else
            {
                y = -y;
                seedY = screen.LCGCacheListMatrix[layer, 3][(int)(y / 50)];
                y = y % 50;
                while (y > 0)
                {
                    seedY = LCGyNeg(seedY);
                    y--;
                }
            }
            int z = (int)((256 + seedX % 256 + seedY % 256) % 256);
            long seedZ = screen.LCGCacheListMatrix[layer, 4][(int)(z / 50)];
            z = z % 50;
            while (z > 0)
            {
                seedZ = LCGz(seedZ);
                z--;
            }
            return (int)((seedZ + seedX + seedY) % 256);
        }
        public static int findPrimaryBiomeValue(long posX, long posY, long seed, long layer)
        {
            long x = posX;
            long y = posY;
            int counto = 0;
            while (counto < 10 + layer * 10)
            {
                seed = LCGz(seed);
                counto += 1;
            }
            long seedX = seed;
            if (x >= 0)
            {
                while (x > 0)
                {
                    seedX = LCGyPos(seedX);
                    x--;
                }
            }
            else
            {
                x = -x;
                while (x > 0)
                {
                    seedX = LCGyNeg(seedX);
                    x--;
                }
            }
            long seedY = seed;
            if (y >= 0)
            {
                while (y > 0)
                {
                    seedY = LCGxPos(seedY);
                    y--;
                }
            }
            else
            {
                y = -y;
                while (y > 0)
                {
                    seedY = LCGxNeg(seedY);
                    y--;
                }
            }
            int seedXY = (int)((8192 + seedX % 1024 + seedY % 1024) % 1024);
            long seedZ = Abs(3 + posX + posY * 11);
            int z = seedXY;
            while (z > 0)
            {
                seedZ = LCGz(seedZ);
                z--;
            }
            return (int)((seedZ + seedXY) % 1024);
            //return ((int)(seedX%512)-256, (int)(seedY%512)-256);
        }
        public static int findSecondaryNoiseValue(Chunk chunk, int varX, int varY, int layer)
        {
            int modulo = 16;
            int modX = GetChunkIndexFromTile1D(chunk.position.Item1 * 16 + varX, modulo);
            int modY = GetChunkIndexFromTile1D(chunk.position.Item2 * 16 + varY, modulo);
            int[,,] values = chunk.primaryFillValues;

            int quartile = 0;
            if (varX >= 16) { quartile += 1; }
            if (varY >= 16) { quartile += 2; }

            int fX1 = values[quartile, layer, 0] * (modulo - modX) + values[quartile, layer, 1] * modX;
            int fX2 = values[quartile, layer, 2] * (modulo - modX) + values[quartile, layer, 3] * modX;
            int fY = fX1 * (modulo - modY) + fX2 * modY;
            return fY / (modulo * modulo);
        }
        public static int findSecondaryBigNoiseValue(Chunk chunk, int varX, int varY, int layer)
        {
            int modulo = 64;
            int modX = GetChunkIndexFromTile1D(chunk.position.Item1 * 32 + varX, modulo);
            int modY = GetChunkIndexFromTile1D(chunk.position.Item2 * 32 + varY, modulo);
            int[,] values = chunk.primaryBigFillValues;
            int fX1 = values[layer, 0] * (modulo - modX) + values[layer, 1] * modX;
            int fX2 = values[layer, 2] * (modulo - modX) + values[layer, 3] * modX;
            int fY = fX1 * (modulo - modY) + fX2 * modY;
            return fY / (modulo * modulo);
        }
        public static int findSecondaryBiomeValue(Chunk chunk, int varX, int varY, int layer)
        {
            int modulo = 512;
            int modX = GetChunkIndexFromTile1D(chunk.position.Item1 * 32 + varX, modulo);
            int modY = GetChunkIndexFromTile1D(chunk.position.Item2 * 32 + varY, modulo);
            int[,] values = chunk.primaryBiomeValues;
            int fX1 = values[layer, 0] * (modulo - modX) + values[layer, 1] * modX;
            int fX2 = values[layer, 2] * (modulo - modX) + values[layer, 3] * modX;
            int fY = fX1 * (modulo - modY) + fX2 * modY;
            return fY / (modulo * modulo);
        }
        public static int findSecondaryBigBiomeValue(Chunk chunk, int varX, int varY, int layer)
        {
            int modulo = 1024;
            int modX = GetChunkIndexFromTile1D(chunk.position.Item1 * 32 + varX, modulo);
            int modY = GetChunkIndexFromTile1D(chunk.position.Item2 * 32 + varY, modulo);
            int[,] values = chunk.primaryBigBiomeValues;
            int fX1 = values[layer, 0] * (modulo - modX) + values[layer, 1] * modX;
            int fX2 = values[layer, 2] * (modulo - modX) + values[layer, 3] * modX;
            int fY = fX1 * (modulo - modY) + fX2 * modY;
            return fY / (modulo * modulo);
        }
        public static (int, int)[] findBiome(int[,,] values, int[,,] bigBiomeValues, int posX, int posY)
        {
            //return new (int, int)[]{ (8, 1000) }; // use this to force a biome for debug (infite biome)


            // arrite so... 0 is temperature, 1 is humidity, 2 is acidity, 3 is toxicity, 4 is terrain modifier1, 5 is terrain modifier 2
            int temperature = values[posX, posY, 0] + bigBiomeValues[posX, posY, 0] - 512;
            int humidity = values[posX, posY, 1] + bigBiomeValues[posX, posY, 1] - 512;
            int acidity = values[posX, posY, 2] + bigBiomeValues[posX, posY, 2] - 512;
            int toxicity = values[posX, posY, 3] + bigBiomeValues[posX, posY, 3] - 512;
            List<(int, int)> listo = new List<(int, int)>();
            int percentageFree = 1000;

            if (humidity > 720)
            {
                int oceanness = Min((humidity - 720) * 25, 1000);
                if (oceanness > 0)
                {
                    listo.Add((8, oceanness));
                    percentageFree -= oceanness;
                }
            }


            if (percentageFree > 0)
            {
                if (temperature > 720)
                {
                    int hotness = (int)(Min((temperature - 720) * 25, 1000) * percentageFree * 0.001f);
                    if (temperature > 840 && humidity > 600)
                    {
                        int minimo = Min(temperature - 840, humidity - 600);
                        int obsidianess = minimo * 10;
                        obsidianess = (int)(Min(obsidianess, 1000) * percentageFree * 0.001f);
                        hotness -= obsidianess;
                        listo.Add((6, obsidianess));
                        percentageFree -= obsidianess;
                    }
                    if (hotness > 0)
                    {
                        listo.Add((2, hotness));
                        percentageFree -= hotness;
                    }
                }
                else if (temperature < 440)
                {
                    int coldness = (int)(Min((440 - temperature) * 10, 1000) * percentageFree * 0.001f);
                    if (temperature < 0)
                    {
                        int bigColdness = (int)(Min((0 - temperature) * 10, 1000) * coldness * 0.001f);
                        coldness -= bigColdness;
                        if (bigColdness > 0)
                        {
                            listo.Add((7, bigColdness));
                            percentageFree -= bigColdness;
                        }
                    }
                    int savedColdness = (int)(Max(0, (Min((120 - temperature) * 10, 1000))) * percentageFree * 0.001f);
                    savedColdness = Min(savedColdness, coldness);
                    coldness -= savedColdness;
                    if (acidity < 440)
                    {
                        int acidness = (int)(Min((440 - acidity) * 10, 1000) * coldness * 0.001f);
                        coldness -= acidness;
                        listo.Add((1, acidness));
                        percentageFree -= acidness;
                    }
                    if (humidity > toxicity)
                    {
                        int fairyness = (int)(Min((humidity - toxicity) * 10, 1000) * coldness * 0.001f);
                        coldness -= fairyness;
                        if (fairyness > 0)
                        {
                            listo.Add((5, fairyness));
                            percentageFree -= fairyness;
                        }
                    }
                    coldness += savedColdness;
                    if (coldness > 0)
                    {
                        listo.Add((0, coldness));
                        percentageFree -= coldness;
                    }
                }
            }

            if (percentageFree > 0)
            {
                int slimeness = (int)(Clamp((toxicity - humidity + 20) * 10, 0, 1000) * percentageFree * 0.001f);
                int forestness = (int)(Clamp((humidity - toxicity + 20) * 10, 0, 1000) * percentageFree * 0.001f);
                if (forestness > 0)
                {
                    listo.Add((3, forestness));
                    percentageFree -= forestness;
                }
                if (slimeness > 0)
                {
                    listo.Add((4, slimeness));
                    percentageFree -= slimeness;
                }
            }

            Sort(listo, false);
            (int, int)[] arrayo = new (int, int)[listo.Count];
            for (int i = 0; i < arrayo.Length; i++)
            {
                arrayo[i] = listo[i];
            }
            return arrayo;
        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            //Form1_Load(new object(), new EventArgs());
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            Screens.Screen screen = (Screens.Screen)timer1.Tag;
            Player player = screen.playerList[0];
            int framesFastForwarded = 0;
        LoopStart:;
            if (!pausePress)
            {
                timeElapsed += 0.02f;
                screen.extraLoadedChunks.Clear(); // this will make many bugs
                screen.broadTestUnstableLiquidList = new List<(int, int)>();
                if (zoomPress[0] && timeElapsed > lastZoom + 0.25f) { screen.zoom(true); }
                if (zoomPress[1] && timeElapsed > lastZoom + 0.25f) { screen.zoom(false); }
                if (inventoryChangePress[0]) { inventoryChangePress[0] = false; player.moveInventoryCursor(-1); }
                if (inventoryChangePress[1]) { inventoryChangePress[1] = false; player.moveInventoryCursor(1); }
                //timeElapsed = (float)((DateTime.Now - timeAtLauch).TotalSeconds); // what the FUCK is that ?????
                player.speedX = Sign(player.speedX) * (Max(0, Abs(player.speedX) * (0.85f) - 0.2f));
                player.speedY = Sign(player.speedY) * (Max(0, Abs(player.speedY) * (0.85f) - 0.2f));
                if (arrowKeysState[0]) { player.speedX += 0.5f; }
                if (arrowKeysState[1]) { player.speedX -= 0.5f; }
                if (arrowKeysState[2]) { player.speedY -= 0.5f; }
                if (arrowKeysState[3]) { player.speedY += 1; }
                player.speedY -= 0.5f;
                if (shiftPress)
                {
                    player.speedX = Sign(player.speedX) * (Max(0, Abs(player.speedX) * (0.75f) - 0.7f));
                    player.speedY = Sign(player.speedY) * (Max(0, Abs(player.speedY) * (0.75f) - 0.7f));
                }
                if (timeElapsed > 3 && screen.activeNests.Count > 0)
                {
                    Nest nestToTest = screen.activeNests.Values.ToArray()[rand.Next(screen.activeNests.Count)];
                    if (rand.Next(100) == 0) { nestToTest.isStable = false; }
                    if (!nestToTest.isStable && nestToTest.digErrands.Count == 0)
                    {
                        nestToTest.randomlyExtendNest();
                    }
                }
                foreach (Player playor in screen.playerList)
                {
                    playor.movePlayer();
                    screen.checkStructures(playor);
                }

                int posDiffX = player.posX - (player.camPosX + 16 * (screen.chunkResolution - 1)); //*2 is needed cause there's only *8 and not *16 before
                int posDiffY = player.posY - (player.camPosY + 16 * (screen.chunkResolution - 1));
                float accCamX = Sign(posDiffX) * Max(0, Sqrt(Abs(posDiffX)) - 2);
                float accCamY = Sign(posDiffY) * Max(0, Sqrt(Abs(posDiffY)) - 2);
                if (accCamX == 0 || Sign(accCamX) != Sign(player.speedCamX))
                {
                    player.speedCamX = Sign(player.speedCamX) * (Max(Abs(player.speedCamX) - 1, 0));
                }
                if (accCamY == 0 || Sign(accCamY) != Sign(player.speedCamY))
                {
                    player.speedCamY = Sign(player.speedCamY) * (Max(Abs(player.speedCamY) - 1, 0));
                }
                player.speedCamX = Clamp(player.speedCamX + accCamX, -15f, 15f);
                player.speedCamY = Clamp(player.speedCamY + accCamY, -15f, 15f);
                player.realCamPosX += player.speedCamX;
                player.realCamPosY += player.speedCamY;
                player.camPosX = (int)(player.realCamPosX + 0.5f);
                player.camPosY = (int)(player.realCamPosY + 0.5f);
                int oldChunkX = screen.chunkX;
                int oldChunkY = screen.chunkY;
                int chunkVariationX = Floor(player.camPosX, 32) / 32 - oldChunkX;
                int chunkVariationY = Floor(player.camPosY, 32) / 32 - oldChunkY;
                if (chunkVariationX != 0 || chunkVariationY != 0)
                {
                    screen.updateLoadedChunks(screen.seed, chunkVariationX, chunkVariationY);
                }
                screen.unloadFarawayChunks();
                screen.manageMegaChunks();
                if (doShitPress) { screen.testMegaChunksForBugs(); doShitPress = false; }

                foreach((int x, int y) pos in screen.chunksToSpawnEntitiesIn.Keys)
                {
                    if (screen.loadedChunks.ContainsKey(pos))
                    {
                        screen.loadedChunks[pos].spawnEntities();
                    }
                }
                screen.chunksToSpawnEntitiesIn = new Dictionary<(int x, int y), bool>();

                List<int> orphansToRemove = new List<int>();
                foreach (int entityId in screen.orphanEntities.Keys)    // add entities that were loaded when nests were not loaded if possible
                {
                    if (!screen.activeEntities.ContainsKey(entityId))
                    {
                        orphansToRemove.Add(entityId);
                        continue;
                    }
                    Entity entityToTest = screen.activeEntities[entityId];
                    if (screen.activeNests.ContainsKey(entityToTest.nestId))
                    {
                        entityToTest.nest = screen.activeNests[entityToTest.nestId];
                        orphansToRemove.Add(entityId);
                    }
                }
                foreach (int entityId in orphansToRemove)
                {
                    screen.orphanEntities.Remove(entityId);
                }


                screen.entitesToRemove = new Dictionary<int, Entity>();
                screen.entitesToAdd = new Dictionary<int, Entity>();
                foreach (Entity entity in screen.activeEntities.Values)
                {
                    entity.moveEntity();
                }
                foreach (Entity entity in screen.entitesToRemove.Values)
                {
                    screen.activeEntities.Remove(entity.id);
                }
                foreach (Entity entity in screen.entitesToAdd.Values)
                {
                    screen.activeEntities[entity.id] = entity;
                }
                foreach (Plant plant in screen.activePlants.Values)
                {
                    plant.testPlantGrowth();
                }
                for (int j = screen.chunkY + screen.chunkResolution - 1; j >= screen.chunkY; j--)
                {
                    for (int i = screen.chunkX; i < screen.chunkX + screen.chunkResolution; i++)
                    {
                        if (rand.Next(50) == 0) { screen.loadedChunks[(i, j)].unstableLiquidCount++; }
                        screen.loadedChunks[(i, j)].moveLiquids();
                    }
                }

                saveSettings(screen);

                if (fastForward && framesFastForwarded < 10)
                {
                    framesFastForwarded++;
                    goto LoopStart;
                }

                gamePictureBox.Image = screen.updateScreen();
                gamePictureBox.Refresh();
                overlayPictureBox.Image = screen.overlayBitmap;
                Sprites.drawSpriteOnCanvas(screen.overlayBitmap, overlayBackground.bitmap, (0, 0), 4, false);
                player.drawInventory();
                overlayPictureBox.Refresh();

            }
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
            if (e.KeyCode == Keys.M)
            {
                debugMode = !debugMode;
            }
            if (e.KeyCode == Keys.F)
            {
                fastForward = true;
            }
            if (e.KeyCode == Keys.K)
            {
                doShitPress = true;
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
            if (e.KeyCode == Keys.M)
            {

            }
            if (e.KeyCode == Keys.F)
            {
                fastForward = false;
            }
            if (e.KeyCode == Keys.K)
            {

            }
            if ((Control.ModifierKeys & Keys.Shift) == 0)
            {
                shiftPress = false;
            }
        }
        public static void makeTheFilledChunk()
        {
            theFilledChunk = new Chunk();
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    theFilledChunk.fillStates[i, j] = 1;
                }
            }
        }
        public static void makeBlackBitmap()
        {
            black32Bitmap = new Bitmap(32, 32);
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    black32Bitmap.SetPixel(i, j, Color.Black);
                }
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Screens.Screen screen = (Screens.Screen)timer1.Tag;
            screen.putEntitiesAndPlantsInChunks();
            screen.saveAllChunks();
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
        public static float Clamp(float value, float min, float max)
        {
            if (value > max) { return max; }
            if (value < min) { return min; }
            return value;
        }
        public static int Clamp(int value, int min, int max)
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
        public static float Sign(float a)
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