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
    public class Chunks
    {
        public class Fire
        {
            public Chunk chunk;
            public (int x, int y) pos;
            public bool isInvalidOnStartupOrGotKilled = false;
            public bool hasNotBeenInited = false;
            public int intensity;
            public int effectiveIntensity;
            public int propagationThreshold;
            public int propagationThresholdDiag;
            public int destructionThreshold;
            public List<PlantElement> affectedPlantElements;
            public TileTraits affectedTile;

            public Fire(FireJson fireJson, Chunk chunkToPut)
            {
                chunk = chunkToPut;
                pos = fireJson.p;
                isInvalidOnStartupOrGotKilled = false;
                hasNotBeenInited = true;
                intensity = fireJson.i;
                propagationThreshold = fireJson.pT;
                propagationThresholdDiag = fireJson.pTD;
                destructionThreshold = fireJson.dT;
            }
            public Fire((int x, int y) firePos, Chunk chunkToPut)
            {
                chunk = chunkToPut;
                pos = firePos;
                affectedTile = chunk.getTileContentInTHISChunk(firePos);
                if (!affectedTile.isAir && !affectedTile.isLava && affectedTile.flammability is null) { isInvalidOnStartupOrGotKilled = true; return; }
                affectedPlantElements = new List<PlantElement>();
                HashSet<MaterialTraits> affectedMaterials = new HashSet<MaterialTraits>();
                foreach (Plant plant in chunk.plants.Values)
                {
                    foreach ((PlantElement pE, MaterialTraits MT) tuple in plant.returnAllPlantElementsWhichMaterialsAtPos(firePos)) { affectedPlantElements.Add(tuple.pE); affectedMaterials.Add(tuple.MT); }
                }
                intensity = 0;
                if (affectedTile.flammability is null) { propagationThreshold = 999999; destructionThreshold = 0; }
                else
                {
                    propagationThreshold = affectedTile.flammability.Value.propagationThreshold;
                    destructionThreshold = affectedTile.flammability.Value.destructionThreshold;
                }
                foreach (MaterialTraits materialTraits in affectedMaterials)
                {
                    if (materialTraits.flammability is null) { continue; }
                    propagationThreshold = Min(propagationThreshold, materialTraits.flammability.Value.propagationThreshold);
                    destructionThreshold = Max(destructionThreshold, materialTraits.flammability.Value.destructionThreshold);
                }
                if (destructionThreshold <= 0) { isInvalidOnStartupOrGotKilled = true; return; }
                propagationThreshold = propagationThreshold + (int)(rand.Next((int)(propagationThreshold * 0.3f)) - (propagationThreshold * 0.1f));
                destructionThreshold = destructionThreshold + (int)(rand.Next((int)(destructionThreshold * 0.3f)) - (destructionThreshold * 0.1f));
                propagationThresholdDiag = propagationThreshold + rand.Next((int)(0.3f + propagationThreshold * 0.7f));
                new Attack(chunk.screen, null, (0, 0, 0, 0), pos, (0, 0), this);
            }

            public bool moveFire()   // false -> nothing happens, true -> fire dies
            {
                if (hasNotBeenInited) { init(); }
                intensity++;
                if (!affectedTile.isAir && !affectedTile.isLava && affectedTile.flammability is null) { intensity -= 11; }
                findEffectiveIntensity();
                if (intensity <= 0) { isInvalidOnStartupOrGotKilled = true; return true; }
                if (intensity >= propagationThreshold) { propagateFireOrtho(); }
                if (intensity >= propagationThresholdDiag) { propagateFireDiag(); }
                if (intensity >= destructionThreshold)
                {
                    propagateFireOrtho();
                    propagateFireDiag();
                    fireDestruction();
                    return true;
                }
                return false;
            }
            public void init()
            {
                affectedPlantElements = new List<PlantElement>();
                HashSet<MaterialTraits> affectedMaterials = new HashSet<MaterialTraits>();
                foreach (Plant plant in chunk.plants.Values)
                {
                    foreach ((PlantElement pE, MaterialTraits MT) tuple in plant.returnAllPlantElementsWhichMaterialsAtPos(pos)) { affectedPlantElements.Add(tuple.pE); affectedMaterials.Add(tuple.MT); }
                }
                affectedTile = chunk.getTileContentInTHISChunk(pos);
                new Attack(chunk.screen, null, (0, 0, 0, 0), pos, (0, 0), this);
            }
            public void findEffectiveIntensity()
            {
                effectiveIntensity = (int)(intensity * (2 - Min(1, destructionThreshold * 0.01f)));
                int diff = (int)(intensity - destructionThreshold * 0.85f);
                if (diff > 0) { effectiveIntensity -= diff * 6; }
            }
            public void propagateFireOrtho()
            {
                foreach ((int x, int y) mod in neighbourArray) { chunk.screen.firesToAdd.Add((pos.x + mod.x, pos.y + mod.y)); }
            }
            public void propagateFireDiag()
            {
                foreach ((int x, int y) mod in diagArray) { chunk.screen.firesToAdd.Add((pos.x + mod.x, pos.y + mod.y)); }
            }
            public void fireDestruction()
            {
                if (affectedTile.flammability != null && affectedTile.flammability.Value.destructionThreshold > 0) { chunk.tileModification(pos.x, pos.y, (0, 0)); }
                foreach (Plant plant in chunk.plants.Values) { plant.totalDestructionAtOnePos(pos, true); }
                isInvalidOnStartupOrGotKilled = true;
            }
        }

        public class Chunk
        {
            public Screens.Screen screen;

            public long chunkSeed;

            public (int x, int y) pos;
            public bool isImmuneToUnloading = true; // Immune to unloading on startup. Should fix shit I hope.
            public int framesSinceLastExtraGetting = 0;

            public ((int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2)[,] biomeValues,
            ((int x, int y) topLeft, (int x, int y) topRight, (int x, int y) bottomLeft, (int x, int y) bottomRight) derivative,
            (float baseScore1, float baseScore2, float separatorScore)[,] scoreArray,
            bool[] quartileFilledArray)? manyValues = null;

            public (BiomeTraits traits, int percentage)[,][] biomeIndex;
            public HashSet<BiomeTraits> allBiomesInTheChunk;

            public TileTraits[,] fillStates = new TileTraits[32, 32];
            public (int, int, int)[,] baseColors;
            public Bitmap bitmap;
            public Bitmap effectsBitmap;
            public Bitmap fireBitmap;

            public Dictionary<(int x, int y), Fire> fireDict = null;

            public List<Entity> entityList = new List<Entity>();
            public Dictionary<int, Plant> plants = new Dictionary<int, Plant>();
            public List<Plant> exteriorPlantList = new List<Plant>();

            public int modificationCount = 0;
            public int unstableLiquidCount = 1;
            public int maturity = 0;    // 0 -> terrain with base TFTs, 1 -> terrain with base TFTs and maturation TFTs, 2 -> terrain with base TFTs, maturation TFTs, and plants + entities generated
            // public HashSet<(int type, int subType)> tileTypesContainedOnGeneration = new HashSet<(int type, int subType)>();

            public int explorationLevel = 0; // set fog : 0 for not visible, 1 for cremebetweens, 2 for fully visible
            public bool[,] fogOfWar = null;
            public Bitmap fogBitmap = null;
            public Bitmap lightBitmap = new Bitmap(32, 32);
            public Chunk()
            {

            }
            public Chunk(Screens.Screen screenToPut, ChunkJson chunkJson)
            {
                screen = screenToPut;
                pos = chunkJson.pos;
                chunkSeed = chunkJson.seed;
                fillStates = ChunkJsonToChunkfillStates(chunkJson.fill1, chunkJson.fill2);
                maturity = chunkJson.mt;

                determineContents(chunkJson);
                if (chunkJson.f != null)
                {
                    fireDict = new Dictionary<(int x, int y), Fire>();
                    foreach (FireJson fireJson in chunkJson.f) { fireDict[fireJson.p] = new Fire(fireJson, this); }
                }
            }
            public Chunk(Screens.Screen screenToPut, (int x, int y) posToPut)
            {
                screen = screenToPut;
                pos = posToPut;

                determineContents(null);
            }
            public void promoteFromExtraToFullyLoaded(ChunkJson chunkJson)  // Can be used both for promotion and simple loading (careful dict displacement is not made by this function !)
            {
                // 3 Cases :
                // If loading (full) during the first loading, no Json, so Json not used
                // If loading (full) but not a first loading, the Json that was just loaded in LoadChunk will be used
                // If promoting from an extra loaded chunk, the Json used will have been retrieved from the game's files (must use the most up to date if entities were saved to it)
                if (chunkJson != null)  // If not on first loading (but full loading)
                {
                    foreach (int entityId in chunkJson.eLst) { entityList.Add(loadEntity(screen, entityId)); }
                    foreach (int plantId in chunkJson.pLst) { plants[plantId] = loadPlant(screen, plantId); }

                    explorationLevel = chunkJson.explLvl;
                }
                if (explorationLevel == 1)
                {
                    fogOfWar = chunkJson.fog;
                    if (fogOfWar == null) { fogOfWar = new bool[32, 32]; }  // It bugged for some reason due to chunks loading when nest loading extra chunk s ????
                    fogBitmap = new Bitmap(32, 32);
                    for (int i = 0; i < 32; i++)
                    {
                        for (int j = 0; j < 32; j++)
                        {
                            if (!fogOfWar[i, j]) { fogBitmap.SetPixel(i, j, Color.Black); }
                        }
                    }
                }
                else { fogOfWar = null; }

                if (maturity < 2) { screen.chunksToMature.Add(pos); }
                else
                {
                    for (int i = 0; i < 32; i++)
                    {
                        for (int j = 0; j < 32; j++)
                        {
                            findTileColor(i, j);
                        }
                    }
                }
            }
            public void demoteToExtra()
            {
                entityList = new List<Entity>();
                plants = new Dictionary<int, Plant>();
                fogOfWar = null;
                fogBitmap = null;
                framesSinceLastExtraGetting = 0;
            }
            public void determineContents(ChunkJson chunkJson)
            {
                chunkSeed = screen.getLCGValue((pos, 0), 32);

                (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2)[,] tileValuesArray = determineAllBiomeValues();

                if (chunkJson == null) // If first loading only, generate terrain
                {
                    (((int x, int y) topLeft, (int x, int y) topRight, (int x, int y) bottomLeft, (int x, int y) bottomRight) derivative, (float baseScore1, float baseScore2, float separatorScore)[,] scoreArray, bool[] quartileFilledArray) tupelo = generateTerrain(tileValuesArray);
                    manyValues = (tileValuesArray, tupelo.derivative, tupelo.scoreArray, tupelo.quartileFilledArray);
                }

                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        int darkness = 0;
                        foreach ((BiomeTraits traits, int percentage) tupel in biomeIndex[i, j])
                        {
                            if (tupel.traits.isDark) { darkness += (int)(tupel.percentage * 0.3f); }
                        }
                        darkness = Max(0, 255 - darkness);
                        Color colorToDraw = Color.FromArgb(255, darkness, darkness, darkness);
                        lightBitmap.SetPixel(i, j, colorToDraw);
                    }
                }
            }
            public (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2)[,] determineAllBiomeValues(bool updateTheTiles = true)
            {
                int[,,] biomeValues = new int[33, 33, 18];
                if (!screen.isMonoBiome)
                {
                    findNoiseValues(biomeValues, 0, 100, 512, 1636);    // small Temperature
                    findNoiseValues(biomeValues, 1, 101, 1024, 512);   // BIG Temperature
                    findNoiseValues(biomeValues, 2, 102, 512, 1636);    // small Humidity
                    findNoiseValues(biomeValues, 3, 103, 1024, 512);   // BIG Humidity
                    findNoiseValues(biomeValues, 4, 104, 512, 1636);    // small Acidity
                    findNoiseValues(biomeValues, 5, 105, 1024, 512);   // BIG Acidity
                    findNoiseValues(biomeValues, 6, 106, 512, 1636);    // small Toxicity
                    findNoiseValues(biomeValues, 7, 107, 1024, 512);   // BIG Toxicity
                    findNoiseValues(biomeValues, 8, 108, 512, 1636);    // small Salinity
                    findNoiseValues(biomeValues, 9, 109, 1024, 512);   // BIG Salinity
                    findNoiseValues(biomeValues, 10, 110, 512, 1636);   // small Illumination
                    findNoiseValues(biomeValues, 11, 111, 1024, 512);  // BIG Illumination
                    findNoiseValues(biomeValues, 12, 112, 512, 1636);   // small Oceanity
                    findNoiseValues(biomeValues, 13, 113, 1024, 512);  // BIG Oceanity
                }

                (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2)[,] tileValuesArray = new (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2)[32, 32];
                if (!updateTheTiles)    // If for chunk maturation for level 2, only get the tileValuesArray !! No need to do the rest (acutally it's important not too)
                {
                    if (screen.isMonoBiome)
                    {
                        (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2) tileValues = makeTileBiomeValueArrayMonoBiome(screen.type);
                        for (int i = 0; i < 32; i++) { for (int j = 0; j < 32; j++) { tileValuesArray[i, j] = tileValues; } }
                    }
                    else { for (int i = 0; i < 32; i++) { for (int j = 0; j < 32; j++) { tileValuesArray[i, j] = makeTileBiomeValueArray(biomeValues, i, j); } } }
                    return tileValuesArray;
                }

                allBiomesInTheChunk = new HashSet<BiomeTraits>();
                biomeIndex = new (BiomeTraits traits, int percentage)[32, 32][];
                baseColors = new (int, int, int)[32, 32];
                bitmap = new Bitmap(32, 32);
                effectsBitmap = new Bitmap(32, 32);

                if (screen.isMonoBiome)
                {
                    (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2) tileValues = makeTileBiomeValueArrayMonoBiome(screen.type);
                    (BiomeTraits traits, int percentage)[] biomeTraitArray = new (BiomeTraits traits, int percentage)[] { (getBiomeTraits(screen.type), 1000) };
                    int[] colorArray = findBiomeColor(biomeTraitArray);
                    (int, int, int) baseColorsTuple = (colorArray[0], colorArray[1], colorArray[2]);

                    allBiomesInTheChunk.Add(biomeTraitArray[0].traits);
                    for (int i = 0; i < 32; i++)
                    {
                        for (int j = 0; j < 32; j++)
                        {
                            biomeIndex[i, j] = biomeTraitArray;
                            baseColors[i, j] = baseColorsTuple;
                            tileValuesArray[i, j] = tileValues;
                        }
                    }
                    return tileValuesArray;
                }


                // Needed to be put alone to find the correct biome noise values AND still get the next optimization !
                for (int i = 0; i < 32; i++) { for (int j = 0; j < 32; j++) { tileValuesArray[i, j] = makeTileBiomeValueArray(biomeValues, i, j); } }


                // Optimization for Monobiome CHUNKS
                foreach ((int x, int y) mod in squareModArray) { biomeIndex[mod.x * 31, mod.y * 31] = findBiome(screen.type, tileValuesArray[mod.x * 31, mod.y * 31]); }

                if (biomeIndex[0, 0].Length == 1 && biomeIndex[31, 0].Length == 1 && biomeIndex[0, 31].Length == 1 && biomeIndex[31, 31].Length == 1 &&
                    biomeIndex[0, 0][0] == biomeIndex[31, 0][0] && biomeIndex[0, 0][0] == biomeIndex[0, 31][0] && biomeIndex[0, 0][0] == biomeIndex[31, 31][0])
                {
                    int[] colorArray = findBiomeColor(biomeIndex[0, 0]);
                    baseColors[0, 0] = (colorArray[0], colorArray[1], colorArray[2]);

                    allBiomesInTheChunk.Add(biomeIndex[0, 0][0].traits);
                    for (int i = 0; i < 32; i ++)
                    {
                        for (int j = 0; j < 32; j++)
                        {
                            biomeIndex[i, j] = biomeIndex[0, 0];
                            baseColors[i, j] = baseColors[0, 0];
                        }
                    }
                    return tileValuesArray;
                }

                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        biomeIndex[i, j] = findBiome(screen.type, tileValuesArray[i, j]);
                        foreach ((BiomeTraits traits, int percentage) item in biomeIndex[i, j]) { allBiomesInTheChunk.Add(item.traits); }

                        int[] colorArray = findBiomeColor(biomeIndex[i, j]);
                        baseColors[i, j] = (colorArray[0], colorArray[1], colorArray[2]);
                    }
                }

                return tileValuesArray;
            }
            public (float baseSeparatorScore, float rightScore, bool isLiquid, bool forceSolid)[,] voronoi(int[,,] terrainValues)
            {
                (float baseSeparatorScore, float rightScore, bool isLiquid, bool forceSolid)[,] stateArray = new (float baseSeparatorScore, float rightScore, bool isLiquid, bool forceSolid)[32, 32];

                ((int x, int y) pos, float ponderation)[] basePosArray = new ((int x, int y) pos, float distance)[25];

                (int x, int y) quartile = (Floor(pos.x, 8) / 8, pos.y);
                int counto = 0;
                foreach ((int x, int y) mod in bigSquareCenteredModArray)
                {
                    basePosArray[counto] = screen.getVoronoiValue((quartile.x + mod.x, quartile.y + mod.y), (256, 32));
                    counto++;
                }

                ((int x, int y) pos, float distance)[] posArray = new ((int x, int y) pos, float distance)[25];
                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        (int x, int y) realPos = (pos.x * 32 + i, pos.y * 32 + j);
                        ((int x, int y) pos, float ponderation) item;
                        for (int k = 0; k < 25; k++)
                        {
                            item = basePosArray[k];
                            posArray[k] = (item.pos, item.ponderation * item.ponderation * DistanceNotSqrted(item.pos, realPos, (1, 8)));
                        }

                        ((int x, int y) pos, float distance) closestPos = posArray[0];
                        ((int x, int y) pos, float distance) secondClosestPos = posArray[1];
                        for (int k = 0; k < 25; k++)
                        {
                            if (posArray[k].distance < closestPos.distance) { secondClosestPos = closestPos; closestPos = posArray[k]; }
                        }

                        closestPos = (closestPos.pos, (float)Math.Sqrt(closestPos.distance));
                        secondClosestPos = (secondClosestPos.pos, (float)Math.Sqrt(secondClosestPos.distance));

                        float baseSeparatorScore = secondClosestPos.distance - closestPos.distance;

                        int junctionScore = Max(0, realPos.y - Max(closestPos.pos.y, secondClosestPos.pos.y));
                        junctionScore *= -junctionScore * 4;
                        int heightDiff = realPos.y - closestPos.pos.y;

                        float juncScore1 = - Clamp(0, Max(0, heightDiff * 3), 3 * baseSeparatorScore);
                        bool juncScore2 = secondClosestPos.pos.y > closestPos.pos.y || baseSeparatorScore >= 20 + heightDiff * 5 ? false : true;

                        int depthScoreLOW = - 10 * Min(0, heightDiff + 6) - 10 * Min(0, heightDiff + 3);
                        int noiseScore = (int)(terrainValues[i, j, 1] * 0.05f);

                        float rightScore = juncScore2 ? baseSeparatorScore - 10 : noiseScore + junctionScore + depthScoreLOW + juncScore1 + Max(25, Max(Abs(closestPos.pos.x - realPos.x) * 0.25f, Abs(closestPos.pos.y - realPos.y)) + closestPos.distance * closestPos.distance * 0.003f);
                        stateArray[i, j] = (baseSeparatorScore, rightScore, !juncScore2 && realPos.y <= closestPos.pos.y && baseSeparatorScore > rightScore, baseSeparatorScore <= rightScore && rightScore - baseSeparatorScore < 30);
                        /*
                        if (baseSeparatorScore > rightScore)
                        {
                            if (realPos.y > closestPos.pos.y || baseSeparatorScore <= rightScore - juncScore2) { stateArray[i, j] = 0; }
                            else { stateArray[i, j] = 2; }
                        }
                        else { stateArray[i, j] = 1; }*/
                    }
                }
                return stateArray;
            }
            public (((int x, int y) topLeft, (int x, int y) topRight, (int x, int y) bottomLeft, (int x, int y) bottomRight) derivative, (float baseScore1, float baseScore2, float separatorScore)[,] scoreArray, bool[] quartileFilledArray)
                generateTerrain((int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2)[,] biomeValues, bool updateTheTiles = true)
            {
                if (updateTheTiles) { fillStates = new TileTraits[32, 32]; }

                // Find Perlin values when needed (chunk contains at least one biome that isn't mangrove)
                int[,,] terrainValues = new int[33, 33, 6];
                ((int x, int y) topLeft, (int x, int y) topRight, (int x, int y) bottomLeft, (int x, int y) bottomRight) derivative2 = ((0, 0), (0, 0), (0, 0), (0, 0));
                findNoiseValues(terrainValues, 1, 2, 16);   // small slither        // here bc it's used for voronoi noise shit as a slight terrain variation
                foreach (BiomeTraits biomeTraits in allBiomesInTheChunk)
                {
                    if (!biomeTraits.isVoronoiCave)
                    {
                        findNoiseValues(terrainValues, 0, 1, 64);   // big slither
                        findNoiseValues(terrainValues, 2, 3, 64);   // big bubble
                        findNoiseValues(terrainValues, 3, 4, 16);   // small bubble
                        
                        // derivative1 = findPerlinDerivative(terrainValues, (0, 1), (1, 0.25f));
                        derivative2 = findPerlinDerivative(terrainValues, (2, 3), (1, 0.25f));
                        
                        break;
                    }
                }

                // Find Voronoi values when needed (chunk contains mangrove)
                (float baseSeparatorScore, float rightScore, bool isLiquid, bool forceSolid)[,] voronoiStateArray = new (float baseSeparatorScore, float rightScore, bool isLiquid, bool forceSolid)[32, 32];
                foreach (BiomeTraits biomeTraits in allBiomesInTheChunk)
                {
                    if (biomeTraits.isVoronoiCave) { voronoiStateArray = voronoi(terrainValues); break; }
                }

                (float baseScore1, float baseScore2, float separatorScore)[,] scoreArray = new (float baseScore1, float baseScore2, float separatorScore)[32, 32];
                bool[] quartileFilledArray = new bool[4];

                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        BiomeTraits mainBiomeTraits = biomeIndex[i, j][0].traits;

                        int value1 = terrainValues[i, j, 0] + (int)(0.25 * terrainValues[i, j, 1]) - 32;
                        int value2 = terrainValues[i, j, 2] + (int)(0.25 * terrainValues[i, j, 3]) - 32;
                        int mod2 = (int)(biomeValues[i, j].mod2 * 0.25);

                        float mult;
                        float bound1 = 0;
                        float bound2 = 0;
                        float score1 = 0;
                        float score2 = 0;
                        float valueModifier;
                        Dictionary<(int separatorType, int connectionLayer), float> separatorDict = new Dictionary<(int separatorType, int connectionLayer), float>();
                        Dictionary<(int separatorType, int connectionLayer), float> antiSeparatorDict = new Dictionary<(int separatorType, int connectionLayer), float>();

                        float separatorScore = 0;
                        float antiSeparatorScore = 0;
                        float liquidVoronoiTransition = 0;
                        (int type, int subType)? liquidVoronoiTransitionType = null;

                        foreach ((BiomeTraits traits, int percentage) tupel in biomeIndex[i, j]) { if (tupel.traits.isVoronoiCave) { liquidVoronoiTransitionType = tupel.traits.lakeType; break; } }
                        if (liquidVoronoiTransitionType != null)
                        {
                            foreach ((BiomeTraits traits, int percentage) tupel in biomeIndex[i, j])
                            {
                                if (tupel.traits.isVoronoiCave && tupel.traits.lakeType == liquidVoronoiTransitionType.Value) { liquidVoronoiTransition += tupel.percentage * 0.001f; }
                                else if (tupel.traits.fillType == liquidVoronoiTransitionType.Value) { liquidVoronoiTransition += tupel.percentage * 0.001f; }
                            }
                        }

                        bool isVoronoiLiquidTransitionZone = liquidVoronoiTransition > 0.95f;
                        bool isVoronoiPool = false;

                        foreach ((BiomeTraits traits, int percentage) tupel in biomeIndex[i, j])
                        {
                            mult = tupel.percentage * 0.001f;

                            valueModifier = 0;
                            if (tupel.traits.isDegraded) { valueModifier += 3 * mult * Max(sawBladeSeesaw(value1, 13), sawBladeSeesaw(value2, 11)); }
                            if (tupel.traits.isSlimy) { valueModifier -= mult * Min(0, 20 * (Sin(i + mod2 * 0.3f + 0.5f, 16) + Sin(j + mod2 * 0.3f + 0.5f, 16)) - 10); }

                            if (tupel.traits.isVoronoiCave)
                            {
                                (float baseSeparatorScore, float rightScore, bool isLiquid, bool forceSolid) item = voronoiStateArray[i, j];
                                score1 += mult * 0.15f * item.baseSeparatorScore;
                                score2 += mult * 0.15f * item.baseSeparatorScore;
                                bound1 += mult * 0.15f * item.rightScore;
                                bound2 += mult * 0.15f * item.rightScore;
                                if (item.forceSolid && mult < 0.9f && mult > 0.3f) { score1 -= 100000; score2 -= 100000; }
                                if (item.isLiquid)
                                {
                                    isVoronoiPool = true;
                                    if (Abs(mult - 0.55f) < 0.2f) { separatorScore += 1000 * (0.2f - Abs(mult - 0.55f)); }
                                }
                            }
                            else
                            {
                                score1 += mult * (findFillScore(tupel.traits, tupel.traits.caveType.one, value1, (i, j), mod2) + findTextureScore(tupel.traits.textureType.one, value2) + valueModifier);   // Swapping is normal !
                                score2 += mult * (findFillScore(tupel.traits, tupel.traits.caveType.two, value2, (i, j), mod2) + findTextureScore(tupel.traits.textureType.two, value1) + valueModifier);   // Cause it needs to be an independant noise !
                            }

                            if (tupel.traits.separatorType != 0) { addOrIncrementDict(separatorDict, ((tupel.traits.separatorType, tupel.traits.connectionLayer), mult)); }
                            if (tupel.traits.antiSeparatorType != 0) { addOrIncrementDict(antiSeparatorDict, ((tupel.traits.antiSeparatorType, tupel.traits.connectionLayer), mult)); }
                        }

                        foreach ((int separatorType, int connectionLayer) tupel in separatorDict.Keys) { separatorScore += findSeparatorScore(tupel.separatorType, separatorDict[tupel]); }
                        foreach ((int separatorType, int connectionLayer) tupel in antiSeparatorDict.Keys) { antiSeparatorScore += findSeparatorScore(tupel.separatorType, antiSeparatorDict[tupel]); }
                        separatorScore = Max(0, separatorScore - (antiSeparatorScore < 10 ? 0 : antiSeparatorScore));  // tried to fix the frozen ocean biome border but uhhhh not sure it's gonna work
                        float separatorScore2 = Max(0, 1 - separatorScore * 0.001f);
                        if (isVoronoiLiquidTransitionZone && isVoronoiPool) { separatorScore = 0; score1 += 10000; score2 += 10000; }

                        scoreArray[i, j] = (
                            score1 * separatorScore2 - separatorScore - bound1 * separatorScore2,
                            score2 * separatorScore2 - separatorScore - bound2 * separatorScore2,
                            separatorScore);
                    }
                }

                if (updateTheTiles)
                {
                    for (int i = 0; i < 32; i++)
                    {
                        for (int j = 0; j < 32; j++)
                        {
                            BiomeTraits mainBiomeTraits = biomeIndex[i, j][0].traits;

                            if (scoreArray[i, j].baseScore1 > 0 || scoreArray[i, j].baseScore2 > 0)
                            {
                                if (mainBiomeTraits.isVoronoiCave && voronoiStateArray[i, j].isLiquid) { fillStates[i, j] = getTileTraits(mainBiomeTraits.lakeType); }
                                else { fillStates[i, j] = getTileTraits(mainBiomeTraits.fillType); }
                            }
                            else { fillStates[i, j] = getTileTraits(mainBiomeTraits.tileType); quartileFilledArray[(i > 16 ? 1 : 0) + (j > 16 ? 2 : 0)] = true; }
                        }
                    }
                    applyTerrainFeatures(biomeIndex, 0);
                }

                return (derivative2, scoreArray, quartileFilledArray);
            }
            public float findFillScore(BiomeTraits biomeTraits, int type, int value, (int x, int y) mod32pos, int mod2)
            {
                if (type == 0) { return -999999; }                                          // 0 - nothing
                if (type == 1) { return -Abs(value - 128) + 10 * biomeTraits.caveWidth; }   // 1 - normal slither caves
                if (type == 2) { return value - 210 + 10 * biomeTraits.caveWidth; }         // 2 - normal bubble caves
                if (type == 3) { return value - 138 + 10 * biomeTraits.caveWidth; }         // 3 - normal ocean 
                if (type == 4)                                                              // 4 - obsidian biome
                {
                    float see1 = Obs((pos.x * 32) % 64 + 64 + mod32pos.x + mod2 * 0.15f + 0.5f, 64);
                    float see2 = Obs((pos.y * 32) % 64 + 64 + mod32pos.y + mod2 * 0.15f + 0.5f, 64);
                    return 500 * (see1 + see2) - 250;
                }
                if (type == 5) { return Max(value - 200, 75 - value); }                     // 5 - forest biome
                if (type == 6)                                                              // 6 - plateaus
                {
                    int plateauPos = (int)(chunkSeed % 32);
                    float plateauScore = 160 - (Abs(plateauPos - mod32pos.y) - 5) * 10;
                    return plateauScore;
                }
                return -999999;
            }
            public float findTextureScore(int type, float value)
            {
                if (type == 0) { return 0; }                                                // 0 - nothing
                if (type == 1) { return (2 * value) % 32; }                                 // 1 - base texture
                return 0;
            }
            public float findSeparatorScore(int type, float mult)
            {
                if (type == 0) { return 0; }                                                // 0 - nothing
                if (type == 1) { float a = Min(mult, 1 - mult) * 50; return a * a; }        // 1 - ocean separator
                return 0;
            }
            public void applyTerrainFeatures((BiomeTraits traits, int percentage)[,][] biomeTraits, int maturityLevelToApply)
            {
                Dictionary<int, int[,]> terrainFeaturesNoiseDict = new Dictionary<int, int[,]>();
                foreach (BiomeTraits traits in allBiomesInTheChunk)
                {
                    if (traits.TFTArrays[maturityLevelToApply] is null) { continue; }
                    foreach (TerrainFeaturesTraits TFT in traits.TFTArrays[maturityLevelToApply])
                    {
                        if (TFT.makeNoiseMaps.one && !terrainFeaturesNoiseDict.ContainsKey(TFT.layer)) { terrainFeaturesNoiseDict[TFT.layer] = findNoiseValues(TFT.layer + 10000, TFT.noiseModulos.one, TFT.noiseValueRanges.one); }
                        if (TFT.makeNoiseMaps.two && !terrainFeaturesNoiseDict.ContainsKey(TFT.layer + 1)) { terrainFeaturesNoiseDict[TFT.layer + 1] = findNoiseValues(TFT.layer + 10001, TFT.noiseModulos.two, TFT.noiseValueRanges.two); }
                    }
                }
                if (terrainFeaturesNoiseDict.Count == 0) { return; }

                (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2)[,] biomeValues;
                ((int x, int y) topLeft, (int x, int y) topRight, (int x, int y) bottomLeft, (int x, int y) bottomRight) derivative;
                (float baseScore1, float baseScore2, float separatorScore)[,] scoreArray;
                bool[] quartileFilledArray;
                if (manyValues is null)
                {
                    biomeValues = determineAllBiomeValues(false);
                    (derivative, scoreArray, quartileFilledArray) = generateTerrain(biomeValues, false);
                    manyValues = (biomeValues, derivative, scoreArray, quartileFilledArray);
                }
                else { (biomeValues, derivative, scoreArray, quartileFilledArray) = manyValues.Value; }

                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        applyTerrainFeaturesOneTile(terrainFeaturesNoiseDict, maturityLevelToApply, biomeTraits[i, j], biomeValues[i, j], derivative, i, j, scoreArray[i, j], quartileFilledArray[(i > 16 ? 1 : 0) + (j > 16 ? 2 : 0)]);
                    }
                }
            }
            public bool applyTerrainFeaturesOneTile(Dictionary<int, int[,]> terrainFeaturesNoiseDict, int maturityLevelToApply, (BiomeTraits traits, int percentage)[] biomeTraits,
                (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2) biomeValues,
                ((int x, int y) topLeft, (int x, int y) topRight, (int x, int y) bottomLeft, (int x, int y) bottomRight) derivativeToPut,
                int i, int j, (float baseScore1, float baseScore2, float separatorScore) fillScore, bool quartileWasFilled, bool isMaturationTFTs = false)
            {
                Dictionary<TerrainFeaturesTraits, int> terrainFeatures = new Dictionary<TerrainFeaturesTraits, int>();
                List<TerrainFeaturesTraits> terrainFeaturesList = new List<TerrainFeaturesTraits>();
                foreach ((BiomeTraits traits, int percentage) tupel in biomeTraits)
                {
                    if (tupel.traits.TFTArrays[maturityLevelToApply] is null) { continue; }
                    foreach (TerrainFeaturesTraits trait in tupel.traits.TFTArrays[maturityLevelToApply])
                    {
                        if (!terrainFeatures.ContainsKey(trait)) { terrainFeaturesList.Add(trait); }
                        addOrIncrementDict(terrainFeatures, (trait, tupel.percentage));
                    }
                }
                if (terrainFeaturesList.Count == 0) { return false; }
                SortTerrainFeatureTraitsListByPriority(terrainFeaturesList);

                float baseScore = Max(fillScore.baseScore1, fillScore.baseScore2);
                foreach (TerrainFeaturesTraits tFT in terrainFeaturesList)
                {
                    if (tFT.needsQuartileFilled && !quartileWasFilled) { continue; }
                    TileTraits tileTraits = fillStates[i, j];
                    if (tileTraits.ignoreTileFeatures && !tFT.ignoreIgnore) { continue; }
                    if (!tFT.inAir && tileTraits.isAir) { continue; }
                    if (!tFT.inLiquid && tileTraits.isLiquid) { continue; }
                    if (!tFT.inSoil && tileTraits.isSolid) { continue; }

                    int noiseValue1 = tFT.makeNoiseMaps.one ? terrainFeaturesNoiseDict[tFT.layer][i, j] : 0;
                    int noiseValue2 = tFT.makeNoiseMaps.two ? terrainFeaturesNoiseDict[tFT.layer + 1][i, j] : 0;
                    float meanNoiseValue = (noiseValue1 + noiseValue2) * 0.5f;

                    float valueRequired = 0;
                    if (tFT.biomeEdgeReduction != null) { valueRequired += tFT.biomeEdgeReduction.Value.strength * 0.001f * Max(0, 1000 - tFT.biomeEdgeReduction.Value.threshold - terrainFeatures[tFT]); }
                    if (tFT.meanBasedValueRequired) { valueRequired += meanNoiseValue * 0.25f; }
                    if (tFT.isBiomeSystem)   // ugly asf, just to get it if the 2 above are false while still doing the 2 if the 2 are true
                    {
                        valueRequired += tFT.baseThreshold - Clamp(0, Min(
                        tFT.temperature is null ? 100000 : (tFT.temperature.Value.reverse ? -1 : 1) * (tFT.temperature.Value.threshold - biomeValues.temp),
                        tFT.humidity is null ? 100000 : (tFT.humidity.Value.reverse ? -1 : 1) * (tFT.humidity.Value.threshold - biomeValues.humi),
                        tFT.acidity is null ? 100000 : (tFT.acidity.Value.reverse ? -1 : 1) * (tFT.acidity.Value.threshold - biomeValues.acid),
                        tFT.toxicity is null ? 100000 : (tFT.toxicity.Value.reverse ? -1 : 1) * (tFT.toxicity.Value.threshold - biomeValues.toxi),
                        tFT.salinity is null ? 100000 : (tFT.salinity.Value.reverse ? -1 : 1) * (tFT.salinity.Value.threshold - biomeValues.sali),
                        tFT.illumination is null ? 100000 : (tFT.illumination.Value.reverse ? -1 : 1) * (tFT.illumination.Value.threshold - biomeValues.illu),
                        tFT.oceanity is null ? 100000 : (tFT.oceanity.Value.reverse ? -1 : 1) * (tFT.oceanity.Value.threshold - biomeValues.ocea)
                        ), 320) * tFT.biomeValuesScale * 0.003125f;
                    }

                    (int type, int subType) typeToFill = tFT.tileType;
                    float noiseValue;
                    if (tFT.transitionRules == 0)
                    {    // Temp !!!!
                        if (tFT.baseThreshold + meanNoiseValue % 256 + meanNoiseValue * 0.25f - 512 <= 0) { noiseValue = -999999; }
                        else { noiseValue = Abs(noiseValue1 - noiseValue2); }
                    }
                    else if (tFT.transitionRules == 1) // flesh and bone (for acid and blood oceans too since they can have the transition)
                    {
                        noiseValue = Max(0, (Abs(Abs(noiseValue1 - 1024) * 0.49f) + noiseValue2 % 256));
                    }
                    else if (tFT.transitionRules == 2) // mold
                    {
                        valueRequired += Min(500 - biomeValues.illu, biomeValues.humi - 500) + 0.1f * (biomeValues.acid + biomeValues.sali) - 0.2f * biomeValues.temp;
                        noiseValue = noiseValue1 + noiseValue2 - 1500 + (Max(fillScore.baseScore1, fillScore.baseScore2) * 100);
                    }
                    else if (tFT.transitionRules == 3) // salt terrain
                    {
                        noiseValue = noiseValue1 + noiseValue2 - 2048 + fillScore.baseScore2 * 25;
                    }
                    else if (tFT.transitionRules == 4) // salt filling
                    {
                        noiseValue = 1000;// -(int)fillScore.baseScore1;
                        valueRequired = fillScore.baseScore2 > 0 ? 999999 : 0;
                    }
                    else if (tFT.transitionRules == 5) // salt spikes
                    {
                        (int x, int y) derivative;
                        if (i > 15)
                        {
                            if (j > 15) { derivative = derivativeToPut.topRight; }
                            else { derivative = derivativeToPut.bottomRight; }
                        }
                        else
                        {
                            if (j > 15) { derivative = derivativeToPut.topLeft; }
                            else { derivative = derivativeToPut.bottomLeft; }
                        }
                        derivative = (derivative.y, -derivative.x);

                        int x = 3 * (i + pos.x * 32);
                        int y = 3 * (j + pos.y * 32);
                        int top = Abs(derivative.x * x + derivative.y * y);
                        int bottom = (int)Math.Sqrt(derivative.x * derivative.x + derivative.y * derivative.y);
                        int c = Seesaw(top / (bottom > 0 ? bottom : 1), 16);

                        noiseValue = Abs(4 * c) - 10 - (Max(fillScore.baseScore2, fillScore.separatorScore) + Max(0, noiseValue1 - 1200) * 0.125f);
                    }
                    else if (tFT.transitionRules == 6) // frost carving
                    {
                        noiseValue = Min(fillScore.baseScore1, fillScore.baseScore2 - 10);
                    }
                    else if (tFT.transitionRules == 7) // skin
                    {
                        valueRequired = tFT.baseThreshold;
                        noiseValue = 0;
                        foreach ((int x, int y) mod in neighbourArray) { if (!screen.getTileContent((pos.x * 32 + i + mod.x, pos.y * 32 + j + mod.y), false).isSolid) { noiseValue = noiseValue1; break; } }
                    }
                    else if (tFT.transitionRules == 8) // Dirt and Mud
                    {
                        valueRequired += tFT.baseThreshold;
                        noiseValue = noiseValue1;
                        TileTraits tileTested = screen.getTileContent((pos.x * 32 + i, pos.y * 32 + j - 1), false);
                        if (tileTested.isSolid)
                        {
                            bool isLiquid = false;
                            for (int k = 0; k < 4; k++)
                            {
                                tileTested = screen.getTileContent((pos.x * 32 + i, pos.y * 32 + j + k + 1), false);
                                if (tileTested.isSolid) { noiseValue += dirtScoreArray[k].invalid; }
                                else
                                {
                                    noiseValue += dirtScoreArray[k].valid;
                                    if (tileTested.isLiquid) { isLiquid = true; }
                                }

                                tileTested = screen.getTileContent((pos.x * 32 + i - 1, pos.y * 32 + j + k + 1), false);
                                if (tileTested.isSolid) { noiseValue += dirtScoreArray[k].invalid * 0.2f; }
                                else
                                {
                                    noiseValue += dirtScoreArray[k].valid * 0.2f;
                                    if (tileTested.isLiquid) { isLiquid = true; }
                                }

                                tileTested = screen.getTileContent((pos.x * 32 + i + 1, pos.y * 32 + j + k + 1), false);
                                if (tileTested.isSolid) { noiseValue += dirtScoreArray[k].invalid * 0.2f; }
                                else
                                {
                                    noiseValue += dirtScoreArray[k].valid * 0.2f;
                                    if (tileTested.isLiquid) { isLiquid = true; }
                                }
                            }

                            if (isLiquid) { typeToFill = (2, 1); }
                            else
                            {
                                for (int k = 1; k <= 3; k++)
                                {
                                    if (screen.getTileContent((pos.x * 32 + i - k, pos.y * 32 + j), false).isLiquid) { typeToFill = (2, 1); }
                                    else if (screen.getTileContent((pos.x * 32 + i + k, pos.y * 32 + j), false).isLiquid) { typeToFill = (2, 1); }
                                }
                            }
                        }
                        else { noiseValue -= 1000; }
                    }
                    else if (tFT.transitionRules == 9) // Litter
                    {
                        valueRequired += tFT.baseThreshold;
                        noiseValue = noiseValue1;
                        if (screen.getTileContent((pos.x * 32 + i, pos.y * 32 + j), false).type == (2, 0))
                        {
                            if (screen.getTileContent((pos.x * 32 + i, pos.y * 32 + j + 1), false).isAir) { noiseValue += 800; }
                            else { noiseValue -= 10000; }
                        }
                        else { noiseValue -= 10000; }
                    }
                    else { noiseValue = -999999; }

                    if (noiseValue >= valueRequired) { fillStates[i, j] = getTileTraits(typeToFill); return true; }
                }
                return false;
            }
            public void findTileColor(int i, int j)
            {
                int[] colorArray = { baseColors[i, j].Item1, baseColors[i, j].Item2, baseColors[i, j].Item3 };
                Color colorToSet;
                TileTraits traits = fillStates[i, j];

                (int r, int g, int b, float mult) materialColor = (traits.colorRange.r.v, traits.colorRange.g.v, traits.colorRange.b.v, traits.biomeColorBlend);
                for (int k = 0; k < 3; k++) { colorArray[k] = (int)(colorArray[k] * materialColor.mult); }

                int rando = traits.isTextured ? Abs((int)(LCGyNeg(LCGxPos(pos.x * 32 + i) % 153 + LCGyPos(pos.y * 32 + j) % 247) % 279)) % 40 - 20 : 0;
                colorArray[0] += (int)(materialColor.r * (1 - materialColor.mult)) + rando;
                colorArray[1] += (int)(materialColor.g * (1 - materialColor.mult)) + rando;
                colorArray[2] += (int)(materialColor.b * (1 - materialColor.mult)) + rando;
                colorToSet = Color.FromArgb(ColorClamp(colorArray[0]), ColorClamp(colorArray[1]), ColorClamp(colorArray[2]));

                if (traits.type == (0, -1))  // csgo missing texture effect when thing is not in the the tha            SOIL
                {
                    if ((i + j) % 2 == 0) { colorToSet = Color.Black; }
                    else { colorToSet = Color.FromArgb(180, 0, 180); }
                }
                else if (traits.type == (0, -2))  // csgo missing texture effect when thing is not in the the tha       LIQUID
                {
                    if ((i + j) % 2 == 0) { colorToSet = Color.FromArgb(60, 60, 60); }
                    else { colorToSet = Color.FromArgb(200, 60, 200); }
                }
                else if (traits.type == (0, -3))  // csgo missing texture effect when thing is not in the the tha       AIR
                {
                    if ((i + j) % 2 == 0) { colorToSet = Color.FromArgb(110, 110, 110); }
                    else { colorToSet = Color.FromArgb(255, 140, 255); }
                }
                bitmap.SetPixel(i, j, colorToSet);
                effectsBitmap.SetPixel(i, j, traits.isLiquid ? Color.FromArgb(80, ColorClamp(colorArray[0]), ColorClamp(colorArray[1]), ColorClamp(colorArray[2])) : Color.Transparent);
            }
            public TileTraits tileModification(int i, int j, (int type, int subType) newMaterial)
            {
                i = PosMod(i);
                j = PosMod(j);
                (int x, int y) posToModify = (i + pos.x * 32, j + pos.y * 32);
                TileTraits previous = fillStates[i, j];
                TileTraits newTraits = getTileTraits(newMaterial);
                fillStates[i, j] = newTraits;
                findTileColor(i, j);
                testLiquidUnstableNonspecific(posToModify.x, posToModify.y, newTraits);
                modificationCount += 1;
                checkForStructureAlteration(posToModify, newMaterial);
                return previous;
            }
            public TileTraits tileModification(int i, int j, TileTraits newTile)
            {
                i = PosMod(i);
                j = PosMod(j);
                (int x, int y) posToModify = (i + pos.x * 32, j + pos.y * 32);
                TileTraits previous = fillStates[i, j];
                fillStates[i, j] = newTile;
                findTileColor(i, j);
                testLiquidUnstableNonspecific(posToModify.x, posToModify.y, newTile);
                modificationCount += 1;
                checkForStructureAlteration(posToModify, newTile.type);
                return previous;
            }
            public TileTraits getTileContentInTHISChunk((int x, int y) posToGet)
            {
                return fillStates[PosMod(posToGet.x), PosMod(posToGet.y)];
            }
            public void checkForStructureAlteration((int x, int y) posToTest, (int type, int subType) newType)
            {
                foreach (Structure structure in screen.activeStructures.Values)
                {
                    if (structure.structureDict.ContainsKey(posToTest) && structure.structureDict[posToTest] != newType)
                    {
                        screen.game.structuresToRemove[structure.id] = structure;
                        continue;
                    }
                }
            }
            public void matureChunkToLevelOne(bool saveTheChunk = true)
            {
                applyTerrainFeatures(biomeIndex, 1);
                maturity = 1;
                if (saveTheChunk) { saveChunk(this); }
            }
            public void matureChunkToLevelTwo()
            {
                if (maturity < 1) { matureChunkToLevelOne(false); }
                applyTerrainFeatures(biomeIndex, 2);
                maturity = 2;
                manyValues = null;

                (HashSet<(int x, int y)> airTilesWithSoilUnder, HashSet<(int x, int y)> airTilesWithSoilNext, HashSet<(int x, int y)> airTilesWithSoilOver, HashSet<(int x, int y)> liquidTilesWithSoilUnder, HashSet<(int x, int y)> liquidTilesWithSoilNext, HashSet<(int x, int y)> liquidTilesWithSoilOver, HashSet<(int x, int y)> airTiles, HashSet<(int x, int y)> liquidTiles, HashSet<(int x, int y)> solidTiles) spawnLocations = getSpawnLocations();

                BiomeTraits mainBiomeTraits = biomeIndex[16, 16][0].traits;  // Middle of chunk
                if (spawnEntitiesBool) { spawnChunkEntities(mainBiomeTraits, spawnLocations); }
                if (spawnPlants) { spawnChunkPlants(mainBiomeTraits, spawnLocations); }

                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        findTileColor(i, j);
                    }
                }

                saveChunk(this);
            }
            public (HashSet<(int x, int y)> airTilesWithSoilUnder, HashSet<(int x, int y)> airTilesWithSoilNext, HashSet<(int x, int y)> airTilesWithSoilOver, HashSet<(int x, int y)> liquidTilesWithSoilUnder, HashSet<(int x, int y)> liquidTilesWithSoilNext, HashSet<(int x, int y)> liquidTilesWithSoilOver, HashSet<(int x, int y)> airTiles, HashSet<(int x, int y)> liquidTiles, HashSet<(int x, int y)> solidTiles) getSpawnLocations()
            {
                HashSet<(int x, int y)> airTilesWithSoilUnder = new HashSet<(int x, int y)>();
                HashSet<(int x, int y)> airTilesWithSoilNext = new HashSet<(int x, int y)>();
                HashSet<(int x, int y)> airTilesWithSoilOver = new HashSet<(int x, int y)>();
                HashSet<(int x, int y)> liquidTilesWithSoilUnder = new HashSet<(int x, int y)>();
                HashSet<(int x, int y)> liquidTilesWithSoilNext = new HashSet<(int x, int y)>();
                HashSet<(int x, int y)> liquidTilesWithSoilOver = new HashSet<(int x, int y)>();
                HashSet<(int x, int y)> airTiles = new HashSet<(int x, int y)>();
                HashSet<(int x, int y)> liquidTiles = new HashSet<(int x, int y)>();
                HashSet<(int x, int y)> solidTiles = new HashSet<(int x, int y)>();

                (int x, int y) randPos;
                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        randPos = (pos.x * 32 + i, pos.y * 32 + j);
                        TileTraits tileTraits = fillStates[i, j];

                        if (tileTraits.isSolid) { solidTiles.Add(randPos); }
                        else if (tileTraits.isAir)
                        {
                            airTiles.Add(randPos);
                            if (screen.getTileContent((randPos.x, randPos.y - 1)).isSolid) { airTilesWithSoilUnder.Add(randPos); }
                            if (screen.getTileContent((randPos.x - 1, randPos.y)).isSolid || screen.getTileContent((randPos.x + 1, randPos.y)).isSolid) { airTilesWithSoilNext.Add(randPos); }
                            if (screen.getTileContent((randPos.x, randPos.y + 1)).isSolid) { airTilesWithSoilOver.Add(randPos); }
                        }
                        else
                        {
                            liquidTiles.Add(randPos);
                            if (screen.getTileContent((randPos.x, randPos.y - 1)).isSolid) { liquidTilesWithSoilUnder.Add(randPos); }
                            if (screen.getTileContent((randPos.x - 1, randPos.y)).isSolid || screen.getTileContent((randPos.x + 1, randPos.y)).isSolid) { liquidTilesWithSoilNext.Add(randPos); }
                            if (screen.getTileContent((randPos.x, randPos.y + 1)).isSolid) { liquidTilesWithSoilOver.Add(randPos); }
                        }
                    }
                }

                return (airTilesWithSoilUnder, airTilesWithSoilNext, airTilesWithSoilOver, liquidTilesWithSoilUnder, liquidTilesWithSoilNext, liquidTilesWithSoilOver, airTiles, liquidTiles, solidTiles);
            }
            public void spawnChunkEntities(BiomeTraits mainBiomeTraits, (HashSet<(int x, int y)> airTilesWithSoilUnder, HashSet<(int x, int y)> airTilesWithSoilNext, HashSet<(int x, int y)> airTilesWithSoilOver, HashSet<(int x, int y)> liquidTilesWithSoilUnder, HashSet<(int x, int y)> liquidTilesWithSoilNext, HashSet<(int x, int y)> liquidTilesWithSoilOver, HashSet<(int x, int y)> airTiles, HashSet<(int x, int y)> liquidTiles, HashSet<(int x, int y)> solidTiles) spawnLocations)
            {
                HashSet<(int x, int y)> forbiddenPositions = new HashSet<(int x, int y)>();
                foreach (((int type, int subType) type, float percentage) in mainBiomeTraits.entitySpawnTypes)
                {
                    for (float i = (float)rand.NextDouble(); i < percentage * 0.01; i++)
                    {
                        EntityTraits traits = entityTraitsDict.ContainsKey(type) ? entityTraitsDict[type] : entityTraitsDict[(-1, 0)];

                        int forceChoose = -1;
                        if (traits.spawnsInMultipleStates)
                        {
                            if (traits.spawnsInSolid && traits.spawnsInAir)
                            {
                                if (traits.spawnsInLiquid) { forceChoose = rand.Next(3); }
                                else { forceChoose = rand.Next(2); }
                            }
                            else if (traits.spawnsInSolid) { forceChoose = rand.Next(2) * 2; }  // if it doesn't spawn in either Solid or Air, then it MUST spawn in Liquid so no need to test
                            else { forceChoose = rand.Next(2) + 1; }    // it spawns in Air and Liquid
                        }

                        (int x, int y) spawnPos;
                        if ((!traits.spawnsInMultipleStates && traits.spawnsInSolid) || forceChoose == 0)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                if (spawnLocations.solidTiles.Count == 0) { continue; }
                                spawnPos = getRandomItem(spawnLocations.solidTiles);
                                if (!forbiddenPositions.Contains(spawnPos) && traits.diggableTiles.Contains(screen.getTileContent(spawnPos).type)) { goto proceed; }
                            }
                        }
                        else if ((!traits.spawnsInMultipleStates && traits.spawnsInAir) || forceChoose == 1)
                        {
                            HashSet<(int x, int y)> setToUse;
                            if (traits.forceSpawnOnSolid) { setToUse = spawnLocations.airTilesWithSoilUnder; }
                            else { setToUse = spawnLocations.airTiles; }
                            if (setToUse.Count == 0) { continue; }

                            for (int j = 0; j < 10; j++)
                            {
                                spawnPos = getRandomItem(setToUse);
                                if (!forbiddenPositions.Contains(spawnPos) && (traits.tilesItCanSpawnOn is null || traits.tilesItCanSpawnOn.Contains(screen.getTileContent((spawnPos.x, spawnPos.y - 1)).type))) { goto proceed; }
                            }
                        }
                        else if ((!traits.spawnsInMultipleStates && traits.spawnsInLiquid) || forceChoose == 2)
                        {
                            if (spawnLocations.liquidTiles.Count == 0) { continue; }
                            for (int j = 0; j < 10; j++)
                            {
                                spawnPos = getRandomItem(spawnLocations.liquidTiles);
                                if (!forbiddenPositions.Contains(spawnPos)) { goto proceed; }
                            }
                        }
                        continue;

                    proceed:;
                        Entity newEntity = new Entity(this, spawnPos, type);
                        if (!newEntity.isDeadAndShouldDisappear) { screen.activeEntities[newEntity.id] = newEntity; forbiddenPositions.Add(spawnPos); }
                    }
                }
            }
            public void spawnChunkPlants(BiomeTraits mainBiomeTraits, (HashSet<(int x, int y)> airTilesWithSoilUnder, HashSet<(int x, int y)> airTilesWithSoilNext, HashSet<(int x, int y)> airTilesWithSoilOver, HashSet<(int x, int y)> liquidTilesWithSoilUnder, HashSet<(int x, int y)> liquidTilesWithSoilNext, HashSet<(int x, int y)> liquidTilesWithSoilOver, HashSet<(int x, int y)> airTiles, HashSet<(int x, int y)> liquidTiles, HashSet<(int x, int y)> solidTiles) spawnLocations)
            {
                HashSet<(int x, int y)> forbiddenPositions = new HashSet<(int x, int y)>();
                foreach (((int type, int subType) type, float percentage) in mainBiomeTraits.plantSpawnTypes)
                {
                    for (float i = (float)rand.NextDouble(); i < percentage * 0.01; i++)
                    {
                        testSpawnOneChunkPlant(type, forbiddenPositions, spawnLocations);
                    }
                }
            }
            public void testSpawnOneChunkPlant((int type, int subType) type, HashSet<(int x, int y)> forbiddenPositions, (HashSet<(int x, int y)> airTilesWithSoilUnder, HashSet<(int x, int y)> airTilesWithSoilNext, HashSet<(int x, int y)> airTilesWithSoilOver, HashSet<(int x, int y)> liquidTilesWithSoilUnder, HashSet<(int x, int y)> liquidTilesWithSoilNext, HashSet<(int x, int y)> liquidTilesWithSoilOver, HashSet<(int x, int y)> airTiles, HashSet<(int x, int y)> liquidTiles, HashSet<(int x, int y)> solidTiles) spawnLocations, ((int x, int y) motherPos, (int x, int y) range)? propagation = null)
            {
                PlantTraits traits = plantTraitsDict.ContainsKey(type) ? plantTraitsDict[type] : plantTraitsDict[(-1, 0)];

                bool forceChooseAir = false;
                if (traits.isAmphibious)    // This code bit makes it so amphibious plants have the same chance to spawn on each type of tile (like if there's 20% of ground tiles in liquid, there's 20% chance it's gonna spawn in a ground tile in liquid).
                {
                    int airAmount;
                    int liquidAmount;
                    if (traits.isGround) { airAmount = spawnLocations.airTilesWithSoilUnder.Count; liquidAmount = spawnLocations.liquidTilesWithSoilUnder.Count; }
                    else if (traits.isCeiling) { airAmount = spawnLocations.airTilesWithSoilOver.Count; liquidAmount = spawnLocations.liquidTilesWithSoilOver.Count; }
                    else if (traits.isSide) { airAmount = spawnLocations.airTilesWithSoilNext.Count; liquidAmount = spawnLocations.liquidTilesWithSoilNext.Count; }
                    else
                    {
                        airAmount = spawnLocations.airTilesWithSoilNext.Count + spawnLocations.airTilesWithSoilOver.Count + spawnLocations.airTilesWithSoilUnder.Count;
                        liquidAmount = spawnLocations.liquidTilesWithSoilNext.Count + spawnLocations.liquidTilesWithSoilOver.Count + spawnLocations.liquidTilesWithSoilUnder.Count;
                    }
                    if (rand.Next(airAmount + liquidAmount + 1) > liquidAmount) { forceChooseAir = true; }
                }

                (int x, int y) attachTileMod;
                bool testOtherSide = false;
                HashSet<(int x, int y)> setToUse;
                if ((!traits.isAmphibious && !traits.isWater) || forceChooseAir)
                {
                    if (traits.isEveryAttach)
                    {
                        int randy = rand.Next(4);   // So it spawns on 4 sides equally lol
                        if (randy == 0) { setToUse = spawnLocations.airTilesWithSoilUnder; attachTileMod = (0, -1); }
                        else if (randy == 1) { setToUse = spawnLocations.airTilesWithSoilOver; attachTileMod = (0, 1); }
                        else { setToUse = spawnLocations.airTilesWithSoilNext; attachTileMod = (1, 0); testOtherSide = true; }
                    }
                    else if (traits.isGround) { setToUse = spawnLocations.airTilesWithSoilUnder; attachTileMod = (0, -1); }
                    else if (traits.isCeiling) { setToUse = spawnLocations.airTilesWithSoilOver; attachTileMod = (0, 1); }
                    else if (traits.isSide) { setToUse = spawnLocations.airTilesWithSoilNext; attachTileMod = (1, 0); testOtherSide = true; }
                    else { setToUse = spawnLocations.airTiles; attachTileMod = (0, 0); }
                }
                else // if ((!traits.isAmphibious && traits.isWater) || !forceChooseAir)
                {
                    if (traits.isEveryAttach)
                    {
                        int randy = rand.Next(4);   // So it spawns on 4 sides equally lol
                        if (randy == 0) { setToUse = spawnLocations.liquidTilesWithSoilUnder; attachTileMod = (0, -1); }
                        else if (randy == 1) { setToUse = spawnLocations.liquidTilesWithSoilOver; attachTileMod = (0, 1); }
                        else { setToUse = spawnLocations.liquidTilesWithSoilNext; attachTileMod = (1, 0); testOtherSide = true; }
                    }
                    else if (traits.isGround) { setToUse = spawnLocations.liquidTilesWithSoilUnder; attachTileMod = (0, -1); }
                    else if (traits.isCeiling) { setToUse = spawnLocations.liquidTilesWithSoilOver; attachTileMod = (0, 1); }
                    else if (traits.isSide) { setToUse = spawnLocations.liquidTilesWithSoilNext; attachTileMod = (1, 0); testOtherSide = true; }
                    else { setToUse = spawnLocations.liquidTiles; attachTileMod = (0, 0); }
                }

                (int x, int y) spawnPos;
                if (setToUse.Count == 0) { return; }
                for (int j = 0; j < 10; j++)
                {
                    spawnPos = getRandomItem(setToUse);
                    if (propagation != null && (Abs(spawnPos.x - propagation.Value.motherPos.x) > propagation.Value.range.x || Abs(spawnPos.y - propagation.Value.motherPos.y) > propagation.Value.range.y)) { continue; }
                    if (traits.tileNeededClose != null)
                    {
                        for (int ii = -traits.tileNeededClose.Value.range.x; ii <= traits.tileNeededClose.Value.range.x; ii++)
                        {
                            for (int jj = -traits.tileNeededClose.Value.range.y; jj <= traits.tileNeededClose.Value.range.y; jj++)
                            {
                                if (screen.getTileContent((spawnPos.x + ii, spawnPos.y + jj)).type == traits.tileNeededClose.Value.tile) { goto tileNeededFound; }
                            }
                        }
                        continue;
                    }
                tileNeededFound:;

                    TileTraits tileTraits = screen.getTileContent((spawnPos.x + attachTileMod.x, spawnPos.y + attachTileMod.y));
                    if (forbiddenPositions.Contains(spawnPos) ||
                        ((traits.soilType is null ? screen.getTileContent((spawnPos.x + attachTileMod.x, spawnPos.y + attachTileMod.y)).isSterile : !traits.soilType.Contains(screen.getTileContent((spawnPos.x + attachTileMod.x, spawnPos.y + attachTileMod.y)).type)) &&
      (!testOtherSide || (traits.soilType is null ? screen.getTileContent((spawnPos.x - attachTileMod.x, spawnPos.y + attachTileMod.y)).isSterile : !traits.soilType.Contains(screen.getTileContent((spawnPos.x - attachTileMod.x, spawnPos.y + attachTileMod.y)).type)))))
                    { continue; }

                    Plant newPlant = new Plant(this, spawnPos, type);
                    if (newPlant.isDeadAndShouldDisappear) { continue; }
                    screen.activePlants[newPlant.id] = newPlant;
                    forbiddenPositions.Add(spawnPos);

                    if (propagation is null && traits.propagateOnSuccess != null)
                    {
                        for (int i = traits.propagateOnSuccess.Value.chance.baseValue + rand.Next(traits.propagateOnSuccess.Value.chance.variation + 1); i >= 0; i--)
                        {
                            testSpawnOneChunkPlant(type, forbiddenPositions, spawnLocations, (spawnPos, traits.propagateOnSuccess.Value.range));
                        }
                    }

                    return;
                }
            }
            public void moveLiquids()
            {
                if (unstableLiquidCount > 0) //here
                {
                    unstableLiquidCount = 0;

                    for (int j = 0; j < 32; j++)
                    {
                        for (int i = 0; i < 32; i++)
                        {
                            if (moveOneLiquid(i, j)) { unstableLiquidCount++; }
                        }
                    }
                }
            }
            public bool moveOneLiquid(int i, int j)
            {
                TileTraits traits = fillStates[i, j];
                if (!traits.isLiquid && !traits.isSandy) { return false; }

                (int x, int y) realPos = (pos.x * 32 + i, pos.y * 32 + j);
                TileTraits leftTile = screen.getTileContent((realPos.x - 1, realPos.y), onlyGetIfFullyLoaded:true);
                TileTraits leftDiagTile = screen.getTileContent((realPos.x - 1, realPos.y - 1), onlyGetIfFullyLoaded:true);
                TileTraits middleTile = screen.getTileContent((realPos.x, realPos.y - 1), onlyGetIfFullyLoaded:true);
                TileTraits rightTile = screen.getTileContent((realPos.x + 1, realPos.y), onlyGetIfFullyLoaded:true);
                TileTraits rightDiagTile = screen.getTileContent((realPos.x + 1, realPos.y - 1), onlyGetIfFullyLoaded:true);

                bool leftTileCrossable = leftTile.isAir || (traits.isSandy && leftTile.isLiquid);
                bool leftDiagTileCrossable = leftDiagTile.isAir || (traits.isSandy && leftDiagTile.isLiquid);
                bool middleTileCrossable = middleTile.isAir || (traits.isSandy && middleTile.isLiquid);
                bool rightTileCrossable = rightTile.isAir || (traits.isSandy && rightTile.isLiquid);
                bool rightDiagTileCrossable = rightDiagTile.isAir || (traits.isSandy && rightDiagTile.isLiquid);

                if (middleTileCrossable)
                {
                    tileModification(i, j, middleTile);
                    screen.setTileContent((realPos.x, realPos.y - 1), traits);
                    return true;
                }
                if (rightDiagTileCrossable && (rightTileCrossable || middleTileCrossable))
                {
                    tileModification(i, j, rightDiagTile);
                    screen.setTileContent((realPos.x + 1, realPos.y - 1), traits);
                    return true;
                }
                if (leftDiagTileCrossable && (leftTileCrossable || middleTileCrossable))
                {
                    tileModification(i, j, leftDiagTile);
                    screen.setTileContent((realPos.x - 1, realPos.y - 1), traits);
                    return true;
                }
                if (traits.isLiquid && (rightDiagTile.isLiquid && (rightTileCrossable || middleTileCrossable)))
                {
                    if (testLiquidPushRight(i, j)) { return true; }
                }
                if (traits.isLiquid && (leftDiagTile.isLiquid && (leftTileCrossable || middleTileCrossable)))
                {
                    if (testLiquidPushLeft(i, j)) { return true; }
                }
                return false;
            }
            public bool testLiquidPushRight(int i, int j)
            {
                int iTested = i;
                int jTested = j - 1;

                int absChunkX = pos.x;
                int absChunkY = pos.y;
                if (jTested < 0) { jTested += 32; absChunkY--; }
                Chunk chunkToTest = screen.getChunkFromChunkPos((absChunkX, absChunkY), false, true);    // Should alwats be loaded
                if (chunkToTest is null) { return false; }

                List<(int x, int y)> posVisited = new List<(int x, int y)> { (absChunkX * 32 + iTested, absChunkY*32 + jTested) };
                (int x, int y) posToTest;

                int repeatCounter = 0;
                while (repeatCounter < 500)
                {
                    iTested++;
                    if (iTested > 31)
                    {
                        absChunkX++;
                        iTested -= 32;
                        chunkToTest = screen.getChunkFromChunkPos((absChunkX, absChunkY), false, true);
                        if (chunkToTest is null) { break; }
                    }
                    posToTest = (absChunkX * 32 + iTested, absChunkY * 32 + jTested);
                    TileTraits traits = chunkToTest.fillStates[iTested, jTested];
                    if (traits.isSolid || screen.liquidsThatCantGoRight.Contains(posToTest)) { break; }
                    if (traits.isAir)
                    {
                        chunkToTest.tileModification(iTested, jTested, tileModification(i, j, (0, 0)));
                        return true;
                    }
                    posVisited.Add(posToTest);
                    liquidSlideCount++;
                    repeatCounter++;
                }
                foreach ((int x, int y) pos in posVisited) { screen.liquidsThatCantGoRight.Add(pos); }
                return false;
            }
            public bool testLiquidPushLeft(int i, int j)
            {
                int iTested = i;
                int jTested = j - 1;

                int absChunkX = pos.x;
                int absChunkY = pos.y;
                if (jTested < 0) { jTested += 32; absChunkY--; }
                Chunk chunkToTest = screen.getChunkFromChunkPos((absChunkX, absChunkY), false, true);    // Should alwats be loaded
                if (chunkToTest is null) { return false; }

                List<(int x, int y)> posVisited = new List<(int x, int y)> { (absChunkX * 32 + iTested, absChunkY * 32 + jTested) };
                (int x, int y) posToTest;

                int repeatCounter = 0;
                while (repeatCounter < 500)
                {
                    iTested--;
                    if (iTested < 0)
                    {
                        absChunkX--;
                        iTested += 32;
                        chunkToTest = screen.getChunkFromChunkPos((absChunkX, absChunkY), false, true);
                        if (chunkToTest is null) { break; }
                    }
                    posToTest = (absChunkX * 32 + iTested, absChunkY * 32 + jTested);
                    TileTraits traits = chunkToTest.fillStates[iTested, jTested];
                    if (traits.isSolid || screen.liquidsThatCantGoRight.Contains(posToTest)) { break; }
                    if (traits.isAir)
                    {
                        chunkToTest.tileModification(iTested, jTested, tileModification(i, j, (0, 0)));
                        return true;
                    }
                    posVisited.Add(posToTest);
                    liquidSlideCount++;
                    repeatCounter++;
                }
                foreach ((int x, int y) pos in posVisited) { screen.liquidsThatCantGoLeft.Add(pos); }
                return false;
            }
            public void testLiquidUnstableNonspecific(int posX, int posY, TileTraits newTile)
            {
                (int x, int y) posToTest;
                Chunk chunkToTest;

                foreach ((int x, int y) mod in directionPositionArray)
                {
                    posToTest = (posX + mod.x, posY + mod.y);
                    chunkToTest = screen.getChunkFromPixelPos(posToTest, onlyGetIfFullyLoaded: true);
                    if (chunkToTest is null) { continue; }
                    if (!chunkToTest.getTileContentInTHISChunk(posToTest).isSolid) { chunkToTest.unstableLiquidCount++; unstableLiquidCount++; }
                }

                if (newTile.isLiquid || newTile.isSandy)
                {
                    if (newTile.isLava) { tryAddFire((posX, posY)); }
                    else if (newTile.flammability is null && fireDict != null && fireDict.ContainsKey((posX, posY))) { fireDict[(posX, posY)].affectedTile = newTile; }
                }
            }
            public void applyRandomTileEffects()
            {
                (int x, int y) randPos = (rand.Next(32), rand.Next(32));
                TileTraits randTile = fillStates[randPos.x, randPos.y];
                if (randTile.isLava) { tryAddFire((pos.x * 32 + randPos.x + rand.Next(3) - 1, pos.y * 32 + randPos.y + rand.Next(3) - 1)); }
            }


            public void moveFires()
            {
                if (fireDict is null) { return; }
                HashSet<Fire> firesToRemove = new HashSet<Fire>();
                foreach (Fire fire in fireDict.Values)
                {
                    if (fire.isInvalidOnStartupOrGotKilled || fire.moveFire()) { firesToRemove.Add(fire); }
                }
                if (firesToRemove.Count == 0) { return; }
                foreach (Fire fire in firesToRemove) { fireDict.Remove(fire.pos); }
                if (fireDict.Count == 0) { fireDict = null; }
            }
            public void tryAddFire((int x, int y) posToAdd)
            {
                if (fireDict is null) { fireDict = new Dictionary<(int x, int y), Fire>(); }
                if (fireDict.ContainsKey(posToAdd)) { return; }
                Fire newFire = new Fire(posToAdd, this);
                if (newFire.isInvalidOnStartupOrGotKilled) { return; }
                fireDict[posToAdd] = newFire;
            }
            public void makeFireBitmaps()
            {
                if (fireDict is null) { fireBitmap = null; return; }
                fireBitmap = new Bitmap(32, 32);
                foreach ((int x, int y) firePos in fireDict.Keys) { fireBitmap.SetPixel(PosMod(firePos.x), PosMod(firePos.y), Color.FromArgb(Min(255, fireDict[firePos].effectiveIntensity * 10), 255, Min(255, 150 + fireDict[firePos].effectiveIntensity), Min(255, 50 + fireDict[firePos].effectiveIntensity * 2))); }
            }




            public MegaChunk getMegaChunk(bool isExtraGetting = false)
            {
                (int x, int y) pos = MegaChunkIdxFromChunkPos(this.pos);
                if (screen.megaChunks.ContainsKey(pos)) { return screen.megaChunks[pos]; }
                if (screen.extraLoadedMegaChunks.ContainsKey(pos))
                {
                    if (isExtraGetting) { return screen.extraLoadedMegaChunks[pos]; }
                    MegaChunk megaChunkToGet = screen.extraLoadedMegaChunks[pos];
                    screen.megaChunks[pos] = megaChunkToGet;
                    screen.extraLoadedMegaChunks.Remove(pos);
                    megaChunkToGet.promoteFromExtraToFullyLoaded();  // Upgrade the extraLoaded MegaChunk to a full MegaChunk, by loading all its contents and putting it in the other dict
                    return megaChunkToGet;
                }
                return loadMegaChunk(screen, pos, isExtraGetting);
            }




            public void createFogOfWar()
            {
                explorationLevel = 1;
                fogOfWar = new bool[32, 32];
                fogBitmap = new Bitmap(32, 32);
                for (int ii = 0; ii < 32; ii++)
                {
                    for (int jj = 0; jj < 32; jj++)
                    {
                        fogBitmap.SetPixel(ii, jj, Color.Black);
                    }
                }
            }
            public void updateFogOfWarOneTile(HashSet<Chunk> chunkDict, (int x, int y) posToTest)
            {
                if (explorationLevel == 2) { return; }
                if (explorationLevel == 0)
                {
                    createFogOfWar();
                    chunkDict.Add(this);
                }
                (int x, int y) tileIndex = PosMod(posToTest);
                if (!fogOfWar[tileIndex.x, tileIndex.y])
                {
                    fogOfWar[tileIndex.x, tileIndex.y] = true;
                    fogBitmap.SetPixel(tileIndex.x, tileIndex.y, Color.Transparent);
                    chunkDict.Add(this);
                }
            }
            public void updateFogOfWarFull()
            {
                bool setAsVisited = true;
                foreach (bool boolo in fogOfWar)
                {
                    if (!boolo)
                    {
                        setAsVisited = false;
                        fogBitmap.MakeTransparent(Color.White);
                        break;
                    }
                }
                if (setAsVisited)
                {
                    explorationLevel = 2;
                    fogOfWar = null;
                    fogBitmap = null;
                }
            }






            public int[,] findNoiseValues(int realLayer, int modulo, int noiseAmplitude = 256)  // noiseValues is int[32, 32, depends]   // layer is the one set in the array, realLayer is the one actually gotten   // Modulo is the resolution : 16 for small terrain noise, 64 for big, 1024 for biome.... for example
            {
                if (modulo < 32) { return findNoiseValuesQuartile(realLayer, modulo, noiseAmplitude); }

                int[,] noiseValues = new int[33, 33];
                (int x, int y) realPos = (pos.x * 32, pos.y * 32);
                int scale = Max(32, modulo) / 32;
                (int x, int y) posToGet = (ChunkIdx(realPos.x / scale), ChunkIdx(realPos.y / scale));

                (int x, int y) mod = PosMod((realPos.x, realPos.y), modulo);
                (int x, int y) modTopRight = PosMod((realPos.x + 31, realPos.y + 31), modulo);
                (int left, int right) preTopValues = (screen.getLCGValue(((posToGet.x, posToGet.y + 1), realLayer), noiseAmplitude), screen.getLCGValue(((posToGet.x + 1, posToGet.y + 1), realLayer), noiseAmplitude));
                (int left, int right) prebottomValues = (screen.getLCGValue((posToGet, realLayer), noiseAmplitude), screen.getLCGValue(((posToGet.x + 1, posToGet.y), realLayer), noiseAmplitude));
                (int left, int right) topValues = ((prebottomValues.left * (modulo - modTopRight.y) + preTopValues.left * modTopRight.y) / modulo, (prebottomValues.right * (modulo - modTopRight.y) + preTopValues.right * modTopRight.y) / modulo);
                (int left, int right) bottomValues = ((prebottomValues.left * (modulo - mod.y) + preTopValues.left * mod.y) / modulo, (prebottomValues.right * (modulo - mod.y) + preTopValues.right * mod.y) / modulo);
                for (int i = 0; i < 32; i++)
                {
                    mod = PosMod((realPos.x + i, realPos.y), modulo);
                    noiseValues[i, 0] = (bottomValues.left * (modulo - mod.x) + bottomValues.right * mod.x) / modulo;
                    noiseValues[i, 32] = (topValues.left * (modulo - mod.x) + topValues.right * mod.x) / modulo;  // ITS NORMAL THAT ITS mod.x in both lines. DONT CHANGEEEEEEEEeeeeeeeee (istg it's true) (ur getting the 2 x bands, THEN with mod.y it makes part of the 2... ITS NORMAL. DONT HCANGE IT. PLS)
                }
                for (int i = 0; i < 32; i++)
                {
                    for (int j = 1; j < 32; j++)
                    {
                        noiseValues[i, j] = (noiseValues[i, 0] * (32 - j) + noiseValues[i, 32] * j) / 32;
                    }
                }

                return noiseValues;
            }
            public int[,,] findNoiseValues(int[,,] noiseValues, int layer, int realLayer, int modulo, int noiseAmplitude = 256)  // noiseValues is int[32, 32, depends]   // layer is the one set in the array, realLayer is the one actually gotten   // Modulo is the resolution : 16 for small terrain noise, 64 for big, 1024 for biome.... for example
            {
                if (modulo < 32) { return findNoiseValuesQuartile(noiseValues, layer, realLayer, modulo, noiseAmplitude); }

                (int x, int y) realPos = (pos.x * 32, pos.y * 32);
                int scale = Max(32, modulo) / 32;
                (int x, int y) posToGet = (ChunkIdx(realPos.x / scale), ChunkIdx(realPos.y / scale));

                (int x, int y) mod = PosMod((realPos.x, realPos.y), modulo); 
                (int x, int y) modTopRight = PosMod((realPos.x + 32, realPos.y + 31), modulo);
                (int left, int right) preTopValues = (screen.getLCGValue(((posToGet.x, posToGet.y + 1), realLayer), noiseAmplitude), screen.getLCGValue(((posToGet.x + 1, posToGet.y + 1), realLayer), noiseAmplitude));
                (int left, int right) preBottomValues = (screen.getLCGValue((posToGet, realLayer), noiseAmplitude), screen.getLCGValue(((posToGet.x + 1, posToGet.y), realLayer), noiseAmplitude));
                (int left, int right) topValues = ((preBottomValues.left * (modulo - modTopRight.y) + preTopValues.left * modTopRight.y) / modulo, (preBottomValues.right * (modulo - modTopRight.y) + preTopValues.right * modTopRight.y) / modulo);
                (int left, int right) bottomValues = ((preBottomValues.left * (modulo - mod.y) + preTopValues.left * mod.y) / modulo, (preBottomValues.right * (modulo - mod.y) + preTopValues.right * mod.y) / modulo);
                for (int i = 0; i < 32; i++)
                {
                    mod = PosMod((realPos.x + i, realPos.y), modulo);
                    noiseValues[i, 0, layer] = (bottomValues.left * (modulo - mod.x) + bottomValues.right * mod.x) / modulo;
                    noiseValues[i, 32, layer] = (topValues.left * (modulo - mod.x) + topValues.right * mod.x) / modulo;  // ITS NORMAL THAT ITS mod.x in both lines. DONT CHANGEEEEEEEEeeeeeeeee (istg it's true) (ur getting the 2 x bands, THEN with mod.y it makes part of the 2... ITS NORMAL. DONT HCANGE IT. PLS)
                }
                for (int i = 0; i < 32; i++)
                {
                    for (int j = 1; j < 32; j++)
                    {
                        noiseValues[i, j, layer] = (noiseValues[i, 0, layer] * (32 - j) + noiseValues[i, 32, layer] * j) / 32;
                    }
                }

                // if (layer == 0) { exportNoiseMap(noiseValues, layer); }
                return noiseValues;
            }
            public int[,] findNoiseValuesQuartile(int realLayer, int modulo, int noiseAmplitude = 256)  // noiseValues is int[32, 32, depends]   // layer is the one set in the array, realLayer is the one actually gotten   // Modulo is the resolution : 16 for small terrain noise, 64 for big, 1024 for biome.... for example
            {
                int[,] noiseValues = new int[33, 33];
                (int x, int y) posToGet = ChunkIdx(pos.x * 64, pos.y * 64);
                (int x, int y) mod;
                foreach ((int x, int y) modo in bigSquareModArray)
                {
                    mod = (modo.x * 16, modo.y * 16);
                    noiseValues[mod.x, mod.y] = screen.getLCGValue(((posToGet.x + modo.x, posToGet.y + modo.y), realLayer), noiseAmplitude);
                }
                foreach ((int x, int y) modo in squareModArray)
                {
                    mod = (modo.x * 16, modo.y * 16);
                    for (int ii = 1; ii < 16; ii++)
                    {
                        int i = ii + mod.x;
                        int j = mod.y;
                        noiseValues[i, j] = (noiseValues[mod.x, j] * (16 - ii) + noiseValues[mod.x + 16, j] * ii) / 16;
                        noiseValues[i, j + 16] = (noiseValues[mod.x, j + 16] * (16 - ii) + noiseValues[mod.x + 16, j + 16] * ii) / 16;  // ITS NORMAL THAT ITS mod.x in both lines. DONT CHANGEEEEEEEEeeeeeeeee (istg it's true) (ur getting the 2 x bands, THEN with mod.y it makes part of the 2... ITS NORMAL. DONT HCANGE IT. PLS)
                    }
                }
                foreach ((int x, int y) modo in squareModArray)
                {
                    mod = (modo.x * 16, modo.y * 16);
                    for (int ii = 0; ii < 16; ii++)
                    {
                        for (int jj = 1; jj < 16; jj++)
                        {
                            int i = ii + mod.x;
                            int j = jj + mod.y;
                            noiseValues[i, j] = (noiseValues[i, mod.y] * (16 - jj) + noiseValues[i, 16 + mod.y] * jj) / 16;
                        }
                    }
                }

                // if (layer == 1) { exportNoiseMap(noiseValues, layer); }
                return noiseValues;
            }
            public int[,,] findNoiseValuesQuartile(int[,,] noiseValues, int layer, int realLayer, int modulo, int noiseAmplitude = 256)  // noiseValues is int[32, 32, depends]   // layer is the one set in the array, realLayer is the one actually gotten   // Modulo is the resolution : 16 for small terrain noise, 64 for big, 1024 for biome.... for example
            {
                (int x, int y) posToGet = ChunkIdx(pos.x * 64, pos.y * 64);
                (int x, int y) mod;
                foreach ((int x, int y) modo in bigSquareModArray)
                {
                    mod = (modo.x * 16, modo.y * 16);
                    noiseValues[mod.x, mod.y, layer] = screen.getLCGValue(((posToGet.x + modo.x, posToGet.y + modo.y), realLayer), noiseAmplitude);
                }
                foreach ((int x, int y) modo in squareModArray)
                {
                    mod = (modo.x * 16, modo.y * 16);
                    for (int ii = 1; ii < 16; ii++)
                    {
                        int i = ii + mod.x;
                        int j = mod.y;
                        noiseValues[i, j, layer] = (noiseValues[mod.x, j, layer] * (16 - ii) + noiseValues[mod.x + 16, j, layer] * ii) / 16;
                        noiseValues[i, j + 16, layer] = (noiseValues[mod.x, j + 16, layer] * (16 - ii) + noiseValues[mod.x + 16, j + 16, layer] * ii) / 16;  // ITS NORMAL THAT ITS mod.x in both lines. DONT CHANGEEEEEEEEeeeeeeeee (istg it's true) (ur getting the 2 x bands, THEN with mod.y it makes part of the 2... ITS NORMAL. DONT HCANGE IT. PLS)
                    }
                }
                foreach ((int x, int y) modo in squareModArray)
                {
                    mod = (modo.x * 16, modo.y * 16);
                    for (int ii = 0; ii < 16; ii++)
                    {
                        for (int jj = 1; jj < 16; jj++)
                        {
                            int i = ii + mod.x;
                            int j = jj + mod.y;
                            noiseValues[i, j, layer] = (noiseValues[i, mod.y, layer] * (16 - jj) + noiseValues[i, 16 + mod.y, layer] * jj) / 16;
                        }
                    }
                }

                // if (layer == 1) { exportNoiseMap(noiseValues, layer); }
                return noiseValues;
            }
            public ((int x, int y) topLeft, (int x, int y) topRight, (int x, int y) bottomLeft, (int x, int y) bottomRight) findPerlinDerivative(int[,,] noiseValues, (int one, int two) layers, (float one, float two) ponderation)
            {
                (int x, int y) topLeft;
                (int x, int y) topRight;
                (int x, int y) bottomLeft;
                (int x, int y) bottomRight;
                {
                    int xTopDerivative = noiseValues[0, 15, layers.one] - noiseValues[15, 15, layers.one];
                    int xBottomDerivative = noiseValues[0, 0, layers.one] - noiseValues[15, 0, layers.one];
                    int xDerivative = xTopDerivative + xBottomDerivative;
                    int yLeftDerivative = noiseValues[0, 0, layers.one] - noiseValues[0, 15, layers.one];
                    int yRightDerivative = noiseValues[15, 0, layers.one] - noiseValues[15, 15, layers.one];
                    int yDerivative = yLeftDerivative + yRightDerivative;

                    int xTopDerivative2 = noiseValues[0, 15, layers.two] - noiseValues[15, 15, layers.two];
                    int xBottomDerivative2 = noiseValues[0, 0, layers.two] - noiseValues[15, 0, layers.two];
                    int xDerivative2 = xTopDerivative2 + xBottomDerivative2;
                    int yLeftDerivative2 = noiseValues[0, 0, layers.two] - noiseValues[0, 15, layers.two];
                    int yRightDerivative2 = noiseValues[15, 0, layers.two] - noiseValues[15, 15, layers.two];
                    int yDerivative2 = yLeftDerivative2 + yRightDerivative2;

                    bottomLeft = ((int)(xDerivative * ponderation.two + xDerivative2 * ponderation.two), (int)(yDerivative * ponderation.two + yDerivative2 * ponderation.two));
                }
                {
                    int xTopDerivative = noiseValues[16, 15, layers.one] - noiseValues[31, 15, layers.one];
                    int xBottomDerivative = noiseValues[16, 0, layers.one] - noiseValues[31, 0, layers.one];
                    int xDerivative = xTopDerivative + xBottomDerivative;
                    int yLeftDerivative = noiseValues[16, 0, layers.one] - noiseValues[16, 15, layers.one];
                    int yRightDerivative = noiseValues[31, 0, layers.one] - noiseValues[31, 15, layers.one];
                    int yDerivative = yLeftDerivative + yRightDerivative;

                    int xTopDerivative2 = noiseValues[16, 15, layers.two] - noiseValues[31, 15, layers.two];
                    int xBottomDerivative2 = noiseValues[16, 0, layers.two] - noiseValues[31, 0, layers.two];
                    int xDerivative2 = xTopDerivative2 + xBottomDerivative2;
                    int yLeftDerivative2 = noiseValues[16, 0, layers.two] - noiseValues[16, 15, layers.two];
                    int yRightDerivative2 = noiseValues[31, 0, layers.two] - noiseValues[31, 15, layers.two];
                    int yDerivative2 = yLeftDerivative2 + yRightDerivative2;

                    bottomRight = ((int)(xDerivative * ponderation.two + xDerivative2 * ponderation.two), (int)(yDerivative * ponderation.two + yDerivative2 * ponderation.two));
                }
                {
                    int xTopDerivative = noiseValues[0, 31, layers.one] - noiseValues[15, 31, layers.one];
                    int xBottomDerivative = noiseValues[0, 16, layers.one] - noiseValues[15, 16, layers.one];
                    int xDerivative = xTopDerivative + xBottomDerivative;
                    int yLeftDerivative = noiseValues[0, 16, layers.one] - noiseValues[0, 31, layers.one];
                    int yRightDerivative = noiseValues[15, 16, layers.one] - noiseValues[15, 31, layers.one];
                    int yDerivative = yLeftDerivative + yRightDerivative;

                    int xTopDerivative2 = noiseValues[0, 31, layers.two] - noiseValues[15, 31, layers.two];
                    int xBottomDerivative2 = noiseValues[0, 16, layers.two] - noiseValues[15, 16, layers.two];
                    int xDerivative2 = xTopDerivative2 + xBottomDerivative2;
                    int yLeftDerivative2 = noiseValues[0, 16, layers.two] - noiseValues[0, 31, layers.two];
                    int yRightDerivative2 = noiseValues[15, 16, layers.two] - noiseValues[15, 31, layers.two];
                    int yDerivative2 = yLeftDerivative2 + yRightDerivative2;

                    topLeft = ((int)(xDerivative * ponderation.two + xDerivative2 * ponderation.two), (int)(yDerivative * ponderation.two + yDerivative2 * ponderation.two));
                }
                {
                    int xTopDerivative = noiseValues[16, 31, layers.one] - noiseValues[31, 31, layers.one];
                    int xBottomDerivative = noiseValues[16, 16, layers.one] - noiseValues[31, 16, layers.one];
                    int xDerivative = xTopDerivative + xBottomDerivative;
                    int yLeftDerivative = noiseValues[16, 16, layers.one] - noiseValues[16, 31, layers.one];
                    int yRightDerivative = noiseValues[31, 16, layers.one] - noiseValues[31, 31, layers.one];
                    int yDerivative = yLeftDerivative + yRightDerivative;

                    int xTopDerivative2 = noiseValues[16, 31, layers.two] - noiseValues[31, 31, layers.two];
                    int xBottomDerivative2 = noiseValues[16, 16, layers.two] - noiseValues[31, 16, layers.two];
                    int xDerivative2 = xTopDerivative2 + xBottomDerivative2;
                    int yLeftDerivative2 = noiseValues[16, 16, layers.two] - noiseValues[16, 31, layers.two];
                    int yRightDerivative2 = noiseValues[31, 16, layers.two] - noiseValues[31, 31, layers.two];
                    int yDerivative2 = yLeftDerivative2 + yRightDerivative2;

                    topRight = ((int)(xDerivative * ponderation.two + xDerivative2 * ponderation.two), (int)(yDerivative * ponderation.two + yDerivative2 * ponderation.two));
                }
                return (topLeft, topRight, bottomLeft, bottomRight);
            }
            public void exportNoiseMap(int[,,] noiseValues, int layer)  // Keep ! Exports the NoiseMap of Chunks. Useful.
            {
                Bitmap bitmapToExport = new Bitmap(noiseValues.GetLength(0), noiseValues.GetLength(1));
                int value;
                for (int i = 0; i < noiseValues.GetLength(0); i++)
                {
                    for (int j = 0; j < noiseValues.GetLength(1); j++)
                    {
                        value = noiseValues[i, j, layer];
                        bitmapToExport.SetPixel(i, noiseValues.GetLength(1) - 1 - j, Color.FromArgb(value, value, value));
                    }
                }
                bitmapToExport.Save($"{currentDirectory}\\CaveData\\{screen.game.seed}\\ChunkNoise\\x{pos.x}y{pos.y}.png");
            }
        }
        public static void makeTheFilledChunk()
        {
            theFilledChunk = new Chunk();
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    theFilledChunk.fillStates[i, j] = getTileTraits((1, 0));
                }
            }
        }
        public static (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2) makeTileBiomeValueArrayMonoBiome((int type, int subType) biome)
        {
            (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int range, int prio) values = biomeTypicalValues.ContainsKey(biome) ? biomeTypicalValues[biome] : biomeTypicalValues[(-1, 0)];
            int temperature = values.temp;
            int humidity = values.humi;
            int acidity = values.acid;
            int toxicity = values.toxi;
            int salinity = values.sali;
            int illumination = values.illu;
            int oceanity = values.ocea;
            int mod1 = 0;
            int mod2 = 0;
            return (temperature, humidity, acidity, toxicity, salinity, illumination, oceanity, mod1, mod2);
        }
        public static (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2) makeTileBiomeValueArray(int[,,] values, int posX, int posY)
        {
            int temperature = values[posX, posY, 0] + values[posX, posY, 1] - 512;
            int humidity = values[posX, posY, 2] + values[posX, posY, 3] - 512;
            int acidity = values[posX, posY, 4] + values[posX, posY, 5] - 512;
            int toxicity = values[posX, posY, 6] + values[posX, posY, 7] - 512;
            int salinity = values[posX, posY, 8] + values[posX, posY, 9] - 512;
            int illumination = values[posX, posY, 10] + values[posX, posY, 11] - 512;
            int oceanity = values[posX, posY, 12] + values[posX, posY, 13] - 512;
            int mod1 = values[posX, posY, 14] + values[posX, posY, 15] - 512;
            int mod2 = values[posX, posY, 16] + values[posX, posY, 17] - 512;
            return (temperature, humidity, acidity, toxicity, salinity, illumination, oceanity, mod1, mod2);
        }
        public static int testAddBiome(List<((int biome, int subBiome), int)> biomeList, (int biome, int subBiome) biomeToTest, int biomeness)
        {
            if (biomeness > 0) { biomeList.Add((biomeToTest, biomeness)); }
            return biomeness;
        }
        public static int calculateBiome(ref int percentageFree, int valueToTest, (int min, int max) bounds, int transitionSpeed = 25) // transitionSpeed : the higher, the faster the transition
        {
            int biomeness = (int)(Clamp(0, Min(valueToTest - bounds.min, bounds.max - valueToTest) * transitionSpeed, 1000) * percentageFree * 0.001f);
            percentageFree -= biomeness;
            return biomeness;
        }
        public static int calculateAndAddBiome(List<((int biome, int subBiome), int)> biomeList, (int biome, int subBiome) biomeToTest, ref int percentageFree, int valueToTest, (int min, int max) bounds, int transitionSpeed = 25) // transitionSpeed : the higher, the faster the transition
        {
            int biomeness = (int)(Clamp(0, Min(valueToTest - bounds.min, bounds.max - valueToTest) * transitionSpeed, 1000) * percentageFree * 0.001f);
            if (biomeness > 0) { biomeList.Add((biomeToTest, biomeness)); }
            percentageFree -= biomeness;
            return biomeness;
        }
        public static (BiomeTraits traits, int percentage)[] findBiome((int, int) dimensionType, int[] values)
        {
            return findBiome(dimensionType, (values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7], values[8]));
        }
        public static (BiomeTraits traits, int percentage)[] findBiome((int, int) dimensionType, (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2) values)
        {
            List<((int biome, int subBiome), int)> listo = new List<((int biome, int subBiome), int)>();
            int percentageFree = 1000;
            int currentInt;

            int temperature = values.temp;
            int humidity = values.humi;
            int acidity = values.acid;
            int toxicity = values.toxi;
            int salinity = values.sali;
            int illumination = values.illu;
            int oceanity = values.ocea;

            bool expensiveUglyBlending = false;
            if (expensiveUglyBlending) // distance shit that's slow asf and bad asf
            {
                //int cumScore = 0; // heehee !
                foreach ((int biome, int subBiome) i in biomeTypicalValues.Keys)
                {
                    int distTemp = Abs(temperature - biomeTypicalValues[i].temp);
                    int distHumi = Abs(humidity - biomeTypicalValues[i].humi);
                    int distAcid = Abs(acidity - biomeTypicalValues[i].acid);
                    int distToxi = Abs(toxicity - biomeTypicalValues[i].toxi);

                    int distTot = biomeTypicalValues[i].range - (distTemp + distHumi + distAcid + distToxi);
                    listo.Add((i, distTot));
                }
                int max = listo[0].Item2;
                for (int i = 1; i < listo.Count; i++)
                {
                    currentInt = listo[i].Item2;
                    if (currentInt > max) { max = currentInt; }
                }
                //max = Max(0, max - 100);
                max -= 100;
                int counto = 0;
                for (int i = listo.Count - 1; i >= 0; i--)
                {
                    listo[i] = (listo[i].Item1, Max(0, listo[i].Item2 - max));
                    if (listo[i].Item2 <= 0) { listo.RemoveAt(i); continue; }
                    counto += listo[i].Item2;
                }
                counto = Max(100, counto);
                for (int i = 0; i < listo.Count; i++) { listo[i] = (listo[i].Item1, listo[i].Item2 * 1000 / counto); }
                if (listo.Count == 0) { listo.Add(((-1, 0), 1000)); }

            }
            else    // The GOOD version of the biome shit
            {
                if (dimensionType == (0, 0)) // type == 1, normal dimension
                {
                    listo = new List<((int biome, int subBiome), int)>();

                    if (oceanity > 800)
                    {
                        int oceaness = calculateBiome(ref percentageFree, oceanity, (800, 999999));   // ocean
                        calculateAndAddBiome(listo, (8, 1), ref oceaness, temperature, (-999999, 0)); // Frozen ocean
                        int saltness = calculateBiome(ref oceaness, salinity, (512, 999999)); // Salt ocean;
                        calculateAndAddBiome(listo, (8, 2), ref saltness, illumination, (512, 999999)); // Algae ocean
                        testAddBiome(listo, (8, 3), saltness);
                        testAddBiome(listo, (8, 0), oceaness);
                    }

                    if (percentageFree <= 0) { goto AfterTest; }
                    calculateAndAddBiome(listo, (0, 1), ref percentageFree, temperature, (-999999, 0));   // add frost

                    if (temperature > 200 && temperature < 850)
                    {
                        if (illumination > 600)
                        {
                            int forestness = calculateBiome(ref percentageFree, Min(illumination - 600, temperature - 200, 850 - temperature), (0, 999999));    // normal forest

                            int wetlandness = calculateBiome(ref forestness, humidity, (700 - Max(0, oceanity - 512), 999999));
                            calculateAndAddBiome(listo, (3, 4), ref wetlandness, salinity, (512, 999999));    // mangrove
                            testAddBiome(listo, (3, 3), wetlandness);    // Add rest as swamp

                            calculateAndAddBiome(listo, (3, 1), ref forestness, temperature, (-999999, 400)); // conifer forest
                            calculateAndAddBiome(listo, (3, 2), ref forestness, temperature, (650, 999999));  // jungle
                            testAddBiome(listo, (3, 0), forestness);    // Add rest as forest

                            // -> if humidity is TOO LOW, no forests ? but deserts instead ?

                            // low humidity : baobab forest ?????? desert ???
                            // -> Garrigue when alcaline ?
                        }
                        if (percentageFree <= 0) { goto AfterTest; }
                        if (illumination > 350)
                        {
                            int prairieness = calculateBiome(ref percentageFree, Min(illumination - 350, temperature - 200, 850 - temperature), (0, 999999));
                            calculateAndAddBiome(listo, (2, 1), ref prairieness, humidity, (700 - Max(0, oceanity - 512), 999999));    // Marsh
                            testAddBiome(listo, (2, 0), prairieness);   // Add rest as flower forest (for now)
                        }
                    }

                    if (percentageFree <= 0) { goto AfterTest; }
                    if (temperature > 720)
                    {
                        int hotness = calculateBiome(ref percentageFree, temperature, (720, 999999));
                        calculateAndAddBiome(listo, (1, 2), ref hotness, Min(temperature - 920, humidity - 800), (0, 999999));   // obsidian
                        calculateAndAddBiome(listo, (1, 1), ref hotness, Min(temperature - 1040, 512 - oceanity), (0, 999999));   // lava ocean
                        testAddBiome(listo, (1, 0), hotness);
                    }

                    if (percentageFree <= 0) { goto AfterTest; }
                    if (temperature < 440)
                    {
                        int coldness = calculateBiome(ref percentageFree, temperature, (-999999, 440));
                        int savedColdness = calculateBiome(ref coldness, temperature, (-999999, 120));  // save coldness to have an ringe of ALWAYS cold biome around frost biomes
                        calculateAndAddBiome(listo, (0, 2), ref coldness, acidity, (700, 999999));  // acid
                        calculateAndAddBiome(listo, (5, 0), ref coldness, humidity - toxicity, (0, 999999));    // fairy
                        testAddBiome(listo, (0, 0), coldness + savedColdness);  // cold
                    }

                    calculateAndAddBiome(listo, (6, 0), ref percentageFree, Min(400 - illumination, humidity - 500) + (int)(0.1f * (acidity + salinity)) - (int)(0.2f * temperature), (0, 999999), 5);  // add mold
                    testAddBiome(listo, (4, 0), percentageFree);    // add slime
                }
                else if (dimensionType == (1, 0)) // type == 1, chandelier dimension
                {
                    calculateAndAddBiome(listo, (101, 0), ref percentageFree, oceanity, (720, 999999)); // Dark ocean...
                    calculateAndAddBiome(listo, (100, 0), ref percentageFree, temperature, (700, 999999)); // Lantern
                    calculateAndAddBiome(listo, (100, 2), ref percentageFree, temperature, (-999999, 300)); // Chandelier
                    testAddBiome(listo, (100, 1), percentageFree); // MixedLuminous
                }
                else if (dimensionType == (2, 0)) // type == 2, living dimension
                {
                    calculateAndAddBiome(listo, (202, 1), ref percentageFree, acidity, (950, 999999)); // acid ocean
                    calculateAndAddBiome(listo, (202, 0), ref percentageFree, oceanity, (-999999, 200)); // blood ocean
                    if (humidity > 450)
                    {
                        int fleshiness = calculateBiome(ref percentageFree, humidity, (450, 999999));
                        if (toxicity >= 700)
                        {
                            int hairiness = calculateBiome(ref fleshiness, toxicity, (700, 999999));
                            calculateAndAddBiome(listo, (200, 3), ref hairiness, temperature + acidity, (1024, 999999));    // hair forest
                            testAddBiome(listo, (200, 4), hairiness);    // long hair forest
                        }
                        calculateAndAddBiome(listo, (200, 1), ref fleshiness, toxicity, (-999999, 450)); // flesh forest;
                        testAddBiome(listo, (200, 0), fleshiness);   // add what's remaining as normal flesh
                    }
                    calculateAndAddBiome(listo, (201, 0), ref percentageFree, humidity, (-999999, 250)); // bone
                    testAddBiome(listo, (200, 2), percentageFree); // flesh and bone
                }
                else if (dimensionType == (-1, 0)) // type == -1, TEST wetlands dimension
                {
                    calculateAndAddBiome(listo, (2, 1), ref percentageFree, illumination, (650, 999999)); // Marsh
                    calculateAndAddBiome(listo, (3, 3), ref percentageFree, salinity, (512, 999999)); // Swamp
                    testAddBiome(listo, (3, 4), percentageFree); // Mangrove
                }
            }

            if (listo.Count == 0) { testAddBiome(listo, (-1, 0), percentageFree); }

        AfterTest:;

            SortByItem2(listo);
            (BiomeTraits traits, int percentage)[] arrayo = new (BiomeTraits traits, int percentage)[listo.Count];
            for (int i = 0; i < arrayo.Length; i++)
            {
                arrayo[i] = (getBiomeTraits(listo[i].Item1), listo[i].Item2);
            }
            return arrayo;
        }
        public static int[] findBiomeColor((BiomeTraits traits, int percentage)[] arrayo)
        {
            int[] colorArray = { 0, 0, 0 };
            float mult;
            foreach ((BiomeTraits traits, int percentage) tupel in arrayo)
            {
                mult = tupel.percentage * 0.001f;
                colorArray[0] += (int)(mult * tupel.traits.color.r);
                colorArray[1] += (int)(mult * tupel.traits.color.g);
                colorArray[2] += (int)(mult * tupel.traits.color.b);
            }
            for (int k = 0; k < 3; k++)
            {
                colorArray[k] = (int)(colorArray[k] * 0.3f);
                colorArray[k] += 20;
            }
            return colorArray;
        }
    }
}
