using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Cave.Chunks;

namespace Cave
{
    public partial class Globals
    {
        public const float div32 = 0.03125f;
        public const float _1On255 = 0.00393f;
        public const float _1On17 = 0.0588f;

        public static bool devMode = true;
        public static bool loadStructuresYesOrNo = false;
        public static bool spawnNests = false;
        public static bool spawnEntitiesBool = false;
        public static bool spawnPlants = false;

        public static int chunkLoadMininumRadius = 8;

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
        public static bool craftPress = false;
        public static bool craftSelection = false;
        public static bool shiftPress = false;
        public static bool dimensionChangePress = false;
        public static bool dimensionSelection = false;
        public static int currentTargetDimension = 0;
        public static DateTime timeAtLauch;
        public static float timeElapsed = 0;

        public static int liquidSlideCount = 0;

        public static string currentDirectory;

        public static int currentStructureId = 0;
        public static int currentEntityId = 0;
        public static int currentPlantId = 0;
        public static int currentDimensionId = 0;

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

        // 0 is temperature, 1 is humidity, 2 is acidity, 3 is toxicity, 4 is terrain modifier1, 5 is terrain modifier 2
        public static Dictionary<(int biome, int subBiome), (int temp, int humi, int acid, int toxi, int range, int prio)> biomeTypicalValues = new Dictionary<(int biome, int subBiome), (int temp, int humi, int acid, int toxi, int range, int prio)>
        {
            { (-1, 0), (690, 690, 690, 690, 1000, 0)},  // undefined

            { (0, 0), (200, 320, 320, 512, 1000, 0) },  // cold biome
            { (0, 1), (-100, 320, 320, 512, 1000, 2) }, // frost biome

            { (1, 0), (200, 300, 800, 512, 1000, 0) },  // acid biome

            { (2, 0), (840, 512, 512, 512, 1000, 1) },  // hot biome
            { (2, 1), (1024, 512, 512, 512, 1000, 3) }, // lava ocean biome
            { (2, 2), (880, 880, 512, 512, 1000, 2) },  // obsidian biome...

            { (3, 0), (512, 720, 768, 340, 1000, 0) },  // forest biome
            { (3, 1), (512, 720, 256, 220, 1000, 0) },  // flower forest biome

            { (4, 0), (512, 280, 512, 680, 1000, 0) },  // toxic biome

            { (5, 0), (200, 840, 200, 320, 1000, 0) },  // fairy biome !
                
            { (6, 0), (200, 800, 800, 512, 1000, 0) },  // mold biome

            { (8, 0), (512, 960, 512, 512, 1000, 0) },  // ocean biome !

            { (9, 0), (320, 320, 240, 240, 1000, 0) },  // chandeliers biome !

            { (10, 0), (720, 512, 512, 512, 1000, 0) }, // Flesh biome !
            { (10, 1), (512, 360, 380, 512, 1000, 0) }, // Flesh and bone biome !
            { (10, 2), (320, 200, 256, 512, 1000, 0) }, // Bone biome...
            { (10, 3), (320, 880, 380, 360, 1000, 0) }, // Blood ocean biome !
            { (10, 4), (720, 600, 880, 880, 1000, 0) }, // Acid ocean biome !
        };

        public static Dictionary<(int type, int subType), (int type, int subType, int typeOfElement)> materialGatheringToolRequirement = new Dictionary<(int type, int subType), (int type, int subType, int typeOfElement)>()
        {   // For plants ! Not terrain !
            { (1, 1), (4, 0, 4) },  // Wood -> Axe
            { (11, 1), (4, 0, 4) }  // Metal -> Axe
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

            new ((int type, int subType, int megaType) material, int count)[] // Wood, MagicRock and Fairy liquid to WandMagic
            {
                ((1, 1, 3), -3),
                ((10, 0, 3), -1),
                ((-3, 0, 0), -2),
                ((3, 0, 4), 1)
            },
        };
    }
}
