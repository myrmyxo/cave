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


namespace Cave
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            currentDirectory = System.IO.Directory.GetCurrentDirectory();

            makeBlackBitmap();
            makeLightBitmaps();

            bool makePngStrings = true;
            if (makePngStrings)
            {
                turnPngIntoString("Error");

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
                turnPngIntoString("Goblin");

                turnPngIntoString("Acid");
                turnPngIntoString("DeoxygenatedBlood");
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
                turnPngIntoString("MoldTile");

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
                turnPngIntoString("FleshVine");
                turnPngIntoString("FleshTendril");
                turnPngIntoString("BoneStalactite");
                turnPngIntoString("BoneStalagmite");

                turnPngIntoString("PlantMatter");
                turnPngIntoString("ObsidianPlantMatter");
                turnPngIntoString("Wood");
                turnPngIntoString("Kelp");
                turnPngIntoString("FlowerPetal");
                turnPngIntoString("Pollen");
                turnPngIntoString("MushroomCap");
                turnPngIntoString("MushroomStem");
                turnPngIntoString("Mold");
                turnPngIntoString("Flesh");
                turnPngIntoString("Bone");
                turnPngIntoString("MagicRock");
                turnPngIntoString("Metal");
                turnPngIntoString("LightBulb");
                turnPngIntoString("Wax");

                turnPngIntoString("Sword");
                turnPngIntoString("Pickaxe");
                turnPngIntoString("Scythe");
                turnPngIntoString("Axe");
                turnPngIntoString("WandMagic");
                turnPngIntoString("WandCarnal");
                turnPngIntoString("WandFloral");

                turnPngIntoString("GoblinHand");
                turnPngIntoString("Stinger");
                turnPngIntoString("Mandibles");

                turnPngIntoString("FairyPortrait");
                turnPngIntoString("GoblinPortrait");
                turnPngIntoString("HornetPortrait");

                turnPngIntoString("Fire");
                turnPngIntoString("LivingPortal");
            }

            loadSpriteDictionaries();

            makeTileTraitsDict();
            makeMaterialTraitsDict();
            makeAttackTraitsDict();
            makeEntityTraitsDict();
            makePlantStructureFramesDict();
            makePlantElementTraitsDict();
            makePlantTraitsDict();
            makeBiomeTraitsDict();

            makeTheFilledChunk();

            //      --- - TO DO LIST - ---
            // Make nests not appear in certain biomes.
            // Fix plants spawning. Make megaChunks priority loading (to stop overlaps and weird generation).
            // Player fairy transfo when in fairy liquid. Craft tools. Add ICE. Auto sorting in inventory. Blood breathing ? Extract o2 from blood.
            // Optimize/functionalize lake maker function
            // EntityCemetary and PlantCemetary folders, putting the files of dead Entities/Plants there
            // Hornet nests -> search for point of interests in plants should take place with SPIRAL function
            // Update tryGrowth stuff in plants to fix bugs and make growth more intelligently
            // Optimization of biome getting -> if all 4 corners of a chunk are Monobiome of the same biome, no need to computer all the other ones inside ! thank you noiseposti.ng 
            // Hornets : 3 types of attack. Warning sting (first attack, to tell to fuk off, scares creatures off the den, second attack, actual stings that deal poison, third attack, mandible slash that can cause bleeding)
            // Message from player character portrait (LMFAO LIKE IF THERE WAS ONE) : "I feel like i'm very much not wanted here..." to tell player that hornet did a warning attack. "I should really get out before they get angry" on the second one.
            // They a loud BUZZ and every hornet aggroes on the player
            // GoHome function for hornets. They go to main room/random room in the nest
            // Upside down trees ! And other plants like that !
            // Fix the fucking mold... or make it interesting. Make that, on mold conversion, it Digs ALL tiles in plants present. As an ATTACK like all diggings will be made.
            // The uh... menu... and uh text... and uh dialogues... and uh villages... and uhhhhhhh make an actual fucking game uhhh
            // In the debugging put extraLoadedMegachunks as another color than the red. Ig.
            // Upgrade plants spawning : if Maxgrowth returns a growth level that is too small, it FAILS, and tries again with another position
            // nornet nests disappear when they empty
            // Hornet larvae climbing up to the ceiling to go pupate ?
            // Bubble effects in water ?
            // Fishing rod
            // Flower that is a 8 star, with petals being ortho + diag of length of 2
            
            // - - - Le Evil Bugz... - - -
            // Raycast : In diagonal can bypass if 2*2 oxxo, and when faraway sometimes even passes through 1 line thick full 1D walls... wtf
            // 30/01/2025 Once, a broken honey storage room was made, as the tunnel that lead to it made it have a leak mid height (tunnel dug in the bordel...)
            // Nest making rooms SUPER fucking far away from spawn
            // Prevent waterSkippers for yeeting themselves out of pools where there is nothing on the sides of a lake to prevent them from doing so
            // When worms are on solid ground, like in air on top of a solid... they stop moving lol
            // An ocean biome was leaking out in a slime biome ??? the fuck
            // When making portal, it FUCKING EATS THE PLANTS AROUND IT ????????? LIKE DUDE ??? STOP ???

            // cool ideas for later !
            // make global using thing because it's RAD... IT DOES NOT FUCKING WORK because not right version guhhh
            // add a dimension that is made ouf of pockets inside unbreakable terrain, a bit like an obsidian biome but scaled up.
            // add stoplight biomes not just candelier biome. and make candles have their own biome ?
            // make it possible to visit entities/players inventories lmfao
            // looping dimensions ???? Could be cool. And serve as TELEPORT HUBS ???
            // maybe depending on a parameter of the dimension, some living world dimensions would be more dead or not dead at all.
            // Lolitadimension ?? ? ? or CANDYDIMENSION ???? idk ? ? ? sugar cane trees would be poggers. Or a candy dimension with candies... yeah and uh idk a lolita biome and a super rare variant being a gothic lolita biome ??? idk wtf i'm on ngl
            // Whipped cream biome, chocolate biome... idk
            // Add a portal that is inside lava oceans; in a structure (obsidian city ?), that needs to be turned on with maybe liquid obsidian or oil, and teleports to like hell, or an obsidian dimension made ouf of only obsidian shit ?.
            // Amnesia spell that makes u forget parts of the map lol
            // Make it so lakes that are big enough (megaLakes) have stuff similar to ocean in them. Like idk mermaids or other shite ? Since they're huge and are like mini oceans.
            // Make player die ! Amulet that when in inventory protects player from lava, and acid. And breathing ! And hp and food ! And the rest !!
            // Idea : affuter blades ? Maybe they can get rusty ? Multiple species of mushroom (move mold to 0 ? or its whole thing prolly better).
            // When multiple players, movePlayer them in random order. So no player gets a real advantage sorta ig.
            // Different fonts for different personalities ? Or tones of speaking ?

            // Biome shit
            // Sometimes Lava lakes in obsidian biomes, but rare -> player can still die if not careful
            // Ocean biome -> in some patches, have the normal cave system thing get added on top, so that there are kinds of small caves and shit in the ocean biome too (but only in some parts)
            // Salt biomes ?

            // Entities ideas !
            // add kobolds. Add urchins in ocean biomes that can damage player (maybe) and eat the kelp. Add sharks that eat fish ? And add LITHOPEDIONS
            // Make it so fairies and other creatures have songs. Like maybe in a fairy village there's a village theme song that's procedurally generated. Idk. ANd they can teach u the song and u can sing it with instrument or voice idk.
            // Add winged waterSkipper : when the population in a lake is too high, or food is too scarse, some old enough waterSkippers can become winged, and fly around to lakes with none or few waterSkippers/lots of food. Migration patterns ? idk
            // add tribes of snowmen ! lmao
            // ADD SPIDERS !! That can put strings and you can get stuck into them ? Webs ? And you get eARTER ? ?
            // Civilisations of undead. That are not necessarly evil or eat the player (maybe multiple kinds of zombies ? civilisations of cannibalistic zombies that eat those whose brain deteriorates too much ? Like a second death ?)
            // Bat/Pterosaur like creatures that can dive in lake to catch fish (or just surface and they catch water skippers) like they glide and shit. But like they're big.
            // Special worms that pollinate obsidian plants in obsidian biomes
            // Vulture bees ??
            // Add rats with a tail ? Rat swarms ?

            // Plants ideas !
            // Tendril shits in living diomension
            // bone trees and shrubs... like ribs.
            // Orchid�e like plants ?
            // Ephemerophytes
            // Rice ? In salt biomes ?
            // Roots for trees, that can grow out the soil under the plant and be exposed if there's a cave under them
            // Branching wax plants ?

            // Lore ideas shit !
            // Carnals and Skeletals in the living dimension are at war. However, due to being made of flesh, only carnals can reproduce. So they end up killing all skeletals.
            // However they need to be at war to live, so they periodically decide, when there's not enough skeletals alive, to put like half of the tribe in ACID,
            // Which turns them into skeletals, these skeletals migrate to the bone biome, and then the war can start again lol. Periodic even that can be witnessed by the player maybe ?
            // Supah powerful wizards can create dimensions. Maybe dimensions that are super weird and shit can be justified this was LOL
            // Star people who have left star structures such as : "star" "big star" "bigbigstar" "bi gbigbigbi g star". Like maybe nests and shite made out of starz.
            // Day and night cycle (that... doesn't exist yet lmfao) due to huge influx of light from another dimension. Like in all points of the dimension photons just happen to be transferred during the day, but it stops during the nigh
            // Same reason for the seasons(?) and shit, but over a long period of time
            // In living dimension, bone plants are acutally bone tumors ? And flesh plants parasitic ?? idk

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

            Game game = new Game();
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
            if (e.KeyCode == Keys.Left) { arrowKeysState[0] = true; }
            if (e.KeyCode == Keys.Right) { arrowKeysState[1] = true; }
            if (e.KeyCode == Keys.Down) { arrowKeysState[2] = true; }
            if (e.KeyCode == Keys.Up) { arrowKeysState[3] = true; }
            if (e.KeyCode == Keys.X) { digPress = true; }
            if (e.KeyCode == Keys.Z) { placePress[0] = true; }
            if (e.KeyCode == Keys.W) { placePress[1] = true; }
            if (e.KeyCode == Keys.S) { zoomPress[0] = true; }
            if (e.KeyCode == Keys.D) { zoomPress[1] = true; }
            if (e.KeyCode == Keys.C) { inventoryChangePress[0] = true; }
            if (e.KeyCode == Keys.V) { inventoryChangePress[1] = true; }
            if (e.KeyCode == Keys.P) { pausePress = true; }
            if (e.KeyCode == Keys.F) { fastForward = true; }
            if (e.KeyCode == Keys.K && !dimensionChangePress) { craftPress = true; }
            if (e.KeyCode == Keys.M) { debugMode = !debugMode; }
            if (e.KeyCode == Keys.L && !craftPress) { dimensionChangePress = true; }
            if ((Control.ModifierKeys & Keys.Shift) != 0) { shiftPress = true; }
        }
        private void KeyIsUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) { arrowKeysState[0] = false; }
            if (e.KeyCode == Keys.Right) { arrowKeysState[1] = false; }
            if (e.KeyCode == Keys.Down) { arrowKeysState[2] = false; }
            if (e.KeyCode == Keys.Up) { arrowKeysState[3] = false; }
            if (e.KeyCode == Keys.X) { digPress = false; }
            if (e.KeyCode == Keys.Z) { placePress[0] = false; }
            if (e.KeyCode == Keys.W) { placePress[1] = false; }
            if (e.KeyCode == Keys.S) { zoomPress[0] = false; }
            if (e.KeyCode == Keys.D) { zoomPress[1] = false; }
            if (e.KeyCode == Keys.C) { inventoryChangePress[0] = false; }
            if (e.KeyCode == Keys.V) { inventoryChangePress[1] = false; }
            if (e.KeyCode == Keys.P) { pausePress = false; }
            if (e.KeyCode == Keys.F) { fastForward = false; }
            if (e.KeyCode == Keys.K) { }
            if (e.KeyCode == Keys.M) { }
            if (e.KeyCode == Keys.L) { }
            if ((Control.ModifierKeys & Keys.Shift) == 0) { shiftPress = false; }
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
        public static long cash((int x, int y, int z) pos, long seed)  // cash stands for chaos hash :D. Thank you soooo much bakkaa on stackoverflow
        {
            long h = seed + pos.x * 374761393 + pos.y * 668265263 + pos.z * 39079; // all constants are prime
            h = (h ^ (h >> 13)) * 1274126177;
            return Abs(h ^ (h >> 16));
        }
        public static long cashInt((int x, int y, int z) pos, long seed)  // cash stands for chaos hash :D. Thank you soooo much bakkaa on stackoverflow
        {
            long h = seed + pos.x * 374761393 + pos.y * 668265263 + pos.z * 39079; // all constants are prime
            h = (h ^ (h >> 13)) * 1274126177;
            return Abs((int)(h ^ (h >> 16)));
        }
        public static long XorShift(long seed)      // Doesn't work...
        {
            ulong seedo = (ulong)seed;
            seedo ^= (seedo << 21);
            seedo ^= (seedo >> 35);
            seedo ^= (seedo << 4);
            return (long)seedo;
        }
        public static long LCGxy(((int x, int y) pos, int layer) pos, long seed)
        {
            return cash((pos.pos.x, pos.pos.y, pos.layer), seed);

            // This thing here is to test random number generators
            /*
            Dictionary<long, long> numbersPassed = new Dictionary<long, long>();
            long current = 0;
            long i = 0;
            int rep = 0;
            while (true)
            {
                current = cash((i, pos.pos.y, pos.layer), seed);
                if (numbersPassed.ContainsKey(current) || rep > 1280)
                {
                    Dictionary<int, int> dicto = new Dictionary<int, int>();
                    for (int j = 0; j < 256; j++) { dicto[j] = 0; }
                    foreach (int num in numbersPassed.Values) { dicto[num]++; }
                    dicto = dicto;
                    int a = 0;
                }
                numbersPassed[current] = current % 256;
                i++;
                rep++;
            }
            */
        }
        public static long LCGxPos(long seed) // WARNING the 1073741824 is not 2^32 but it's 2^30 cause... lol
        {
            return (758267 * seed + 281641) % 4294967291;
        }
        public static long LCGxNeg(long seed)
        {
            return (337651 * seed + 502553) % 4294967291;
        }
        public static long LCGyPos(long seed)
        {
            return (834959 * seed + 545437) % 4294967291;
        }
        public static long LCGyNeg(long seed)
        {
            return (921677 * seed + 766177) % 4294967291;
        }
        public static long LCGz(long seed)
        {
            return (152953 * seed + 845003) % 4294967291;
        }
        public static int LCGint1(int seed)
        {
            return Abs((91 * seed + 6763) % 999983); // VERY SMALL HAVE TO REDO IT
        }
        public static int LCGint2(int seed)
        {
            return Abs((126161 * seed + 8837) % 998947); // For some reason it DOES NOT WORK ??? The RANDOM is NOT wroking
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
        public static T getRandomItem<T>(List<T> collection)
        {
            return collection[rand.Next(collection.Count)];
        }
        public static T getRandomKey<T>(Dictionary<T, T> collection)
        {
            return collection.Keys.ToList()[rand.Next(collection.Count)];
        }
        public static T getRandomValue<T>(Dictionary<T, T> collection)
        {
            return collection.Values.ToList()[rand.Next(collection.Count)];
        }
        public static void insertIntoListSorted<T>(List<(T t, int cost)> l, (T t, int cost) e, bool priorityToSmallerValues = true)
        {
            for (int i = 0; i < l.Count; i++) { if (priorityToSmallerValues ? (e.cost < l[i].cost) : (e.cost > l[i].cost)) { l.Insert(i, e); return; } }
            l.Add(e);
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
        public static int Min(params int[] values)
        {
            int mini = values[0];
            foreach (int value in values) { if (value < mini) { mini = value; } }
            return mini;
        }
        public static byte Min(byte a, byte b)
        {
            if (a < b) { return a; }
            return b;
        }
        public static int MaxAbs(int a, int b)
        {
            if (Abs(a) > Abs(b)) { return a; }
            return b;
        }
        public static float MaxAbs(float a, float b)
        {
            if (Abs(a) > Abs(b)) { return a; }
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
        public static int RoundUp(int value, int modulo)
        {
            if (value % modulo == 0) { return value; }
            return Floor(value, modulo) + modulo;
        }
        public static float RoundUp(float value, float modulo)
        {
            if (value % modulo == 0) { return value; }
            return Floor(value, modulo) + modulo;
        }
        public static long RoundUp(long value, long modulo)
        {
            if (value % modulo == 0) { return value; }
            return Floor(value, modulo) + modulo;
        }
        public static (int x, int y) ChunkIdx(int pixelPosX, int pixelPosY)
        {
            int chunkPosX = Floor(pixelPosX, 32) / 32;
            int chunkPosY = Floor(pixelPosY, 32) / 32;
            return (chunkPosX, chunkPosY);
        }
        public static (int x, int y) ChunkIdx((int x, int y) pos)
        {
            int chunkPosX = Floor(pos.x, 32) / 32;
            int chunkPosY = Floor(pos.y, 32) / 32;
            return (chunkPosX, chunkPosY);
        }
        public static int ChunkIdx(int pos)
        {
            return Floor(pos, 32) / 32;
        }
        public static (int x, int y) MegaChunkIdxFromChunkPos((int x, int y) pos)
        {
            int chunkPosX = Floor(pos.x, 16) / 16;
            int chunkPosY = Floor(pos.y, 16) / 16;
            return (chunkPosX, chunkPosY);
        }
        public static (int x, int y) MegaChunkIdxFromPixelPos((int x, int y) pos)
        {
            int chunkPosX = Floor(pos.x, 512) / 512;
            int chunkPosY = Floor(pos.y, 512) / 512;
            return (chunkPosX, chunkPosY);
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
        public static int SignZero(int a)
        {
            if (a == 0) { return 0; }
            if (a > 0) { return 1; }
            return -1;
        }
        public static int SignZero(float a)
        {
            if (a == 0) { return 0; }
            if (a > 0) { return 1; }
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
        public static float Distance((int x, int y) pos1, (int x, int y) pos2)
        {
            int distX = (pos1.x - pos2.x);
            int distY = (pos1.y - pos2.y);
            return Sqrt(distX*distX + distY*distY);
        }

        // Coordinate manipulation stuff
        public static (int x, int y) spiralProgression((int x, int y) pos, int rotation = 0, int flipNum = 0)  // From a pos tuple, returns the next one in a clockwise spiral progression centered on (0, 0)
        {
            flip(pos, flipNum);
            rotate(pos, rotation);
            if (pos.y > Abs(pos.x)) { pos = (pos.x + 1, pos.y); }
            else if (pos.x <= 0 && Abs(pos.x) >= Abs(pos.y)) { pos = (pos.x, pos.y + 1); }
            else if (pos.y < 0 && Abs(pos.y) >= Abs(pos.x)) { pos = (pos.x - 1, pos.y); }
            else { pos = (pos.x, pos.y - 1); }
            rotate(pos, -rotation);
            flip(pos, flipNum);
            return pos;
        }
        public static (int x, int y) rotate((int x, int y) pos, int rotation) // 0 : nothing, +1 -> + 90�� (so -1/3, -2/2, -3,1 do the same)
        {
            rotation = PosMod(rotation, 4);
            return ((rotation % 2 == 0 ? pos.x : pos.y) * (rotation >= 2 ? -1 : 1), (rotation % 2 == 0 ? pos.y : pos.x) * (rotation == 1 || rotation == 2 ? -1 : 1));
        }
        public static (int x, int y) flip((int x, int y) pos, int flip) // 0 : nothing, 1 : vertical, 2 : horizontal, 3 : both
        {
            return (pos.x * (flip >= 2 ? -1 : 1), pos.y * (flip % 2 == 1 ? -1 : 1));
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