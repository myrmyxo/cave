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
        public class Chunk
        {
            public Screens.Screen screen;

            public long chunkSeed;

            public (int x, int y) pos;
            public bool isImmuneToUnloading = true; // Immune to unloading on startup. Should fix shit I hope.
            public int framesSinceLastExtraGetting = 0;

            public (BiomeTraits traits, int percentage)[,][] biomeIndex;
            public BiomeTraits traits;

            public TileTraits[,] fillStates = new TileTraits[32, 32];
            public (int, int, int)[,] baseColors;
            public Bitmap bitmap;
            public Bitmap effectsBitmap;

            public List<Entity> entityList = new List<Entity>();
            public Dictionary<int, Plant> plants = new Dictionary<int, Plant>();
            public List<Plant> exteriorPlantList = new List<Plant>();

            public int modificationCount = 0;
            public int unstableLiquidCount = 1;
            public bool isMature = false;

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
                isMature = chunkJson.mt;

                determineContents(chunkJson);
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

                if (!isMature) { screen.chunksToMature[pos] = true; }
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

                if (chunkJson == null) { generateTerrain(tileValuesArray); }   // If first loading only, generate terrain

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

                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        findTileColor(i, j);
                    }
                }
            }
            public (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2)[,] determineAllBiomeValues()
            {
                int[,,] biomeValues = new int[33, 33, 18];
                if (!screen.isMonoBiome)
                {
                    findNoiseValues(biomeValues, 0, 100, 512, 1028);    // BIG   Temperature
                    findNoiseValues(biomeValues, 1, 101, 1024, 1028);   // small Temperature
                    findNoiseValues(biomeValues, 2, 102, 512, 1028);    // BIG   Humidity
                    findNoiseValues(biomeValues, 3, 103, 1024, 1028);   // small Humidity
                    findNoiseValues(biomeValues, 4, 104, 512, 1028);    // BIG   Acidity
                    findNoiseValues(biomeValues, 5, 105, 1024, 1028);   // small Acidity
                    findNoiseValues(biomeValues, 6, 106, 512, 1028);    // BIG   Toxicity
                    findNoiseValues(biomeValues, 7, 107, 1024, 1028);   // small Toxicity
                    findNoiseValues(biomeValues, 8, 108, 512, 1028);    // BIG   Salinity
                    findNoiseValues(biomeValues, 9, 109, 1024, 1028);   // small Salinity
                    findNoiseValues(biomeValues, 10, 110, 512, 1028);   // BIG   Illumination
                    findNoiseValues(biomeValues, 11, 111, 1024, 1028);  // small Illumination
                    findNoiseValues(biomeValues, 12, 108, 512, 1028);   // BIG   Oceanity
                    findNoiseValues(biomeValues, 13, 109, 1024, 1028);  // small Oceanity

                    findNoiseValues(biomeValues, 14, 108, 512, 1028);   // BIG   mod1
                    findNoiseValues(biomeValues, 15, 109, 1024, 1028);  // small mod1
                    findNoiseValues(biomeValues, 16, 110, 512, 1028);   // BIG   mod2
                    findNoiseValues(biomeValues, 17, 111, 1024, 1028);  // small mod2
                }

                (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2)[,] tileValuesArray = new (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2)[32, 32];
                biomeIndex = new (BiomeTraits traits, int percentage)[32, 32][];
                baseColors = new (int, int, int)[32, 32];
                bitmap = new Bitmap(32, 32);
                effectsBitmap = new Bitmap(32, 32);

                for (int i = 0; i < 32; i += 1)
                {
                    for (int j = 0; j < 32; j += 1)
                    {
                        (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2) tileValues;
                        if (screen.isMonoBiome)
                        {
                            tileValues = makeTileBiomeValueArrayMonoBiome(screen.type);
                            biomeIndex[i, j] = new (BiomeTraits traits, int percentage)[] { (getBiomeTraits(screen.type), 1000) };
                        }
                        else
                        {
                            tileValues = makeTileBiomeValueArray(biomeValues, i, j);
                            biomeIndex[i, j] = findBiome(screen.type, tileValues);
                        }
                        tileValuesArray[i, j] = tileValues;

                        int[] colorArray = findBiomeColor(biomeIndex[i, j]);
                        baseColors[i, j] = (colorArray[0], colorArray[1], colorArray[2]);
                    }
                }

                return tileValuesArray;
            }
            public void generateTerrain((int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2)[,] tileValuesArray)
            {
                fillStates = new TileTraits[32, 32];

                int[,,] terrainValues = new int[33, 33, 6];
                findNoiseValues(terrainValues, 0, 1, 64);           // big slither
                findNoiseValuesQuartile(terrainValues, 1, 2);       // small slither
                findNoiseValues(terrainValues, 2, 3, 64);           // big bubble
                findNoiseValuesQuartile(terrainValues, 3, 4);       // small bubble
                findNoiseValuesQuartile(terrainValues, 4, 5, 2048); // Stuff for minerals (dense rock), not efficient here since it should be one measured for nonfilled tiles, but whatever
                findNoiseValuesQuartile(terrainValues, 5, 6, 2048); // Stuff for minerals (dense rock), not efficient here since it should be one measured for nonfilled tiles, but whatever

                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        int value1 = terrainValues[i, j, 0] + (int)(0.25 * terrainValues[i, j, 1]) - 32;
                        int value2 = terrainValues[i, j, 2] + (int)(0.25 * terrainValues[i, j, 3]) - 32;
                        int mod2 = (int)(tileValuesArray[i, j].mod2 * 0.25);

                        BiomeTraits mainBiomeTraits = biomeIndex[i, j][0].Item1;

                        float mult;
                        float score1 = 0;
                        float score2 = 0;
                        float valueModifier;
                        float separatorScore = 0;
                        float antiSeparatorScore = 0;
                        foreach ((BiomeTraits traits, int percentage) tupel in biomeIndex[i, j])
                        {
                            mult = tupel.percentage * 0.001f;

                            valueModifier = 0;
                            if (tupel.traits.isDegraded) { valueModifier += 3 * mult * Max(sawBladeSeesaw(value1, 13), sawBladeSeesaw(value2, 11)); }
                            if (tupel.traits.isSlimy) { valueModifier -= mult * Min(0, 20 * (Sin(i + mod2 * 0.3f + 0.5f, 16) + Sin(j + mod2 * 0.3f + 0.5f, 16)) - 10); }

                            score1 += mult * (findFillScore(tupel.traits, tupel.traits.caveType.one, value1, (i, j), mod2) + findTextureScore(tupel.traits.textureType.one, value2) + valueModifier); // Swapping is normal !
                            score2 += mult * (findFillScore(tupel.traits, tupel.traits.caveType.two, value2, (i, j), mod2) + findTextureScore(tupel.traits.textureType.two, value1) + valueModifier); // Cause it needs to be an independant noise !
                            if (tupel.traits.separatorType != 0) { separatorScore += findSeparatorScore(tupel.traits.separatorType, mult); }
                            if (tupel.traits.antiSeparatorType != 0) { antiSeparatorScore += findSeparatorScore(tupel.traits.antiSeparatorType, mult); }
                        }

                        separatorScore = Max(0, separatorScore - antiSeparatorScore);
                        bool carveTest1 = score1 * (1 - separatorScore * 0.001f) - separatorScore > 0;
                        bool carveTest2 = score2 * (1 - separatorScore * 0.001f) - separatorScore > 0;

                        if (carveTest1 || carveTest2) { fillStates[i, j] = getTileTraits(mainBiomeTraits.fillType); }
                        else { fillStates[i, j] = getTileTraits(findMaterialToFillWith(tileValuesArray[i, j], (terrainValues[i, j, 4], terrainValues[i, j, 5]), biomeIndex[i, j])); }
                    }
                }
            }
            public float findFillScore(BiomeTraits biomeTraits, int type, int value, (int x, int y) mod32pos, int mod2)
            {
                if (type == 0) { return -999999; }                                          // 0 - nothing
                if (type == 1) { return -Abs(value - 128) + 10 * biomeTraits.caveWidth; }   // 1 - normal slither caves
                if (type == 2) { return value - 210 + 10 * biomeTraits.caveWidth; }         // 2 - normal bubble caves
                if (type == 3) { return value - 138 + 10 * biomeTraits.caveWidth; }         // 3 - normal ocean 
                if (type == 4)                                                              // 4 - obsidian biome
                {
                    float see1 = Obs((pos.Item1 * 32) % 64 + 64 + mod32pos.x + mod2 * 0.15f + 0.5f, 64);
                    float see2 = Obs((pos.Item2 * 32) % 64 + 64 + mod32pos.y + mod2 * 0.15f + 0.5f, 64);
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
                if (type == 1) { float a = (0.5f - Abs(mult - 0.5f)) * 30; return a * a; }  // 1 - ocean separator
                return 0;
            }
            public (int type, int subType) findMaterialToFillWith((int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2) biomeValues, (int, int) values, (BiomeTraits traits, int percentage)[] biomeTraits)
            {
                Dictionary<TileTransitionTraits, int> transitionDict = new Dictionary<TileTransitionTraits, int>();
                foreach ((BiomeTraits traits, int percentage) tupel in biomeTraits)
                {
                    if (tupel.traits.tileTransitionTraitsArray is null) { continue; }
                    foreach (TileTransitionTraits trait in tupel.traits.tileTransitionTraitsArray) { addOrIncrementDict(transitionDict, (trait, tupel.percentage)); }
                }
                if (transitionDict.Count == 0) { return biomeTraits[0].traits.tileType; }

                float meanValue = (values.Item1 + values.Item2) * 0.5f;
                foreach (TileTransitionTraits tTT in transitionDict.Keys)
                {
                    float valueRequired;
                    if (tTT.meanBasedValueRequired) { valueRequired = meanValue * 0.25f; }
                    else
                    {
                        valueRequired = tTT.baseThreshold - Clamp(0, Min(
                        tTT.temperature is null ? 100000 : (tTT.temperature.Value.reverse ? -1 : 1) * (tTT.temperature.Value.threshold - biomeValues.temp),
                        tTT.humidity is null ? 100000 : (tTT.humidity.Value.reverse ? -1 : 1) * (tTT.humidity.Value.threshold - biomeValues.humi),
                        tTT.acidity is null ? 100000 : (tTT.acidity.Value.reverse ? -1 : 1) * (tTT.acidity.Value.threshold - biomeValues.acid),
                        tTT.toxicity is null ? 100000 : (tTT.toxicity.Value.reverse ? -1 : 1) * (tTT.toxicity.Value.threshold - biomeValues.toxi),
                        tTT.salinity is null ? 100000 : (tTT.salinity.Value.reverse ? -1 : 1) * (tTT.salinity.Value.threshold - biomeValues.toxi),
                        tTT.illumination is null ? 100000 : (tTT.illumination.Value.reverse ? -1 : 1) * (tTT.illumination.Value.threshold - biomeValues.toxi),
                        tTT.oceanity is null ? 100000 : (tTT.oceanity.Value.reverse ? -1 : 1) * (tTT.oceanity.Value.threshold - biomeValues.toxi)
                        ) / 320f, 1) * tTT.biomeValuesScale;
                    }

                    int noiseValue;
                    if (tTT.transitionRules == 0)
                    {    // Temp !!!!
                        if (tTT.baseThreshold + meanValue % 256 + meanValue * 0.25f - 512 <= 0) { noiseValue = -999999; }
                        else { noiseValue = Abs(values.Item1 - values.Item2); }
                    }
                    else if (tTT.transitionRules == 1) // flesh and bone (for acid and blood oceans too since they can have the transition)
                    {
                        noiseValue = Max(0, (int)(Abs(Abs(values.Item1 - 1024) * 0.49f) + values.Item2 % 256));
                    }
                    else if (tTT.transitionRules == 2) // mold
                    {
                        noiseValue = Max(0, (int)(Abs(Abs(values.Item1 - 1024) * 0.49f)));
                    }
                    else { noiseValue = -999999; }

                    if (noiseValue >= valueRequired) { return tTT.tileType; }
                }
                return biomeTraits[0].traits.tileType;
            }
            public void findTileColor(int i, int j)
            {
                int[] colorArray = { baseColors[i, j].Item1, baseColors[i, j].Item2, baseColors[i, j].Item3 };
                Color colorToSet;
                TileTraits traits = fillStates[i, j];

                (int r, int g, int b, float mult) materialColor = (traits.colorRange.r.v, traits.colorRange.g.v, traits.colorRange.b.v, traits.biomeColorBlend);
                for (int k = 0; k < 3; k++)
                {
                    colorArray[k] = (int)(colorArray[k] * materialColor.mult);
                };
                int rando = traits.isTextured ? Abs((int)(LCGyNeg(LCGxPos(pos.x * 32 + i) % 153 + LCGyPos(pos.y * 32 + j) % 247) % 279)) % 40 - 20 : 0;
                colorArray[0] += (int)(materialColor.r * (1 - materialColor.mult)) + rando;
                colorArray[1] += (int)(materialColor.g * (1 - materialColor.mult)) + rando;
                colorArray[2] += (int)(materialColor.b * (1 - materialColor.mult)) + rando;
                colorToSet = Color.FromArgb(ColorClamp(colorArray[0]), ColorClamp(colorArray[1]), ColorClamp(colorArray[2]));

                if (false)  // This was for the csgo missing texture effect when thing is not in the the tha
                {
                    if ((i + j) % 2 == 0) { colorToSet = Color.Black; }
                    else { colorToSet = Color.FromArgb(255, 00, 255); }
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
                fillStates[i, j] = getTileTraits(newMaterial);
                findTileColor(i, j);
                testLiquidUnstableNonspecific(posToModify.x, posToModify.y);
                modificationCount += 1;
                checkForStructureAlteration(posToModify, newMaterial);
                return previous;
            }
            public TileTraits tileModification(int i, int j, TileTraits newMaterial)
            {
                i = PosMod(i);
                j = PosMod(j);
                (int x, int y) posToModify = (i + pos.x * 32, j + pos.y * 32);
                TileTraits previous = fillStates[i, j];
                fillStates[i, j] = newMaterial;
                findTileColor(i, j);
                testLiquidUnstableNonspecific(posToModify.x, posToModify.y);
                modificationCount += 1;
                checkForStructureAlteration(posToModify, newMaterial.type);
                return previous;
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
            public void spawnOneEntities(float spawnRate, ((int type, int subType) type, float percentage)[] spawnTypes, Dictionary<(int x, int y), bool> forbiddenPositions)
            {
                for (float i = (float)rand.NextDouble(); i < spawnRate; i++)
                {
                    float rando = ((float)rand.NextDouble()) * 100;
                    foreach (((int type, int subType) type, float percentage) tupelo in spawnTypes)
                    {
                        if (rando > tupelo.percentage) { rando -= tupelo.percentage; continue; }
                        EntityTraits traits = entityTraitsDict.ContainsKey(tupelo.type) ? entityTraitsDict[tupelo.type] : entityTraitsDict[(-1, 0)];
                        ((int x, int y) pos, bool valid) returnTuple = findSuitablePosition(forbiddenPositions, true, traits.isSwimming, false, false, traits.isDigging, traits.isJesus);
                        if (!returnTuple.valid) { break; }
                        Entity newEntity = new Entity(this, tupelo.type, returnTuple.pos);
                        if (!newEntity.isDeadAndShouldDisappear) { screen.activeEntities[newEntity.id] = newEntity; }
                    }
                }
            }
            public void spawnOnePlants(float spawnRate, ((int type, int subType) type, float percentage)[] spawnTypes, Dictionary<(int x, int y), bool> forbiddenPositions)
            {
                for (float i = (float)rand.NextDouble(); i < spawnRate; i++)
                {
                    float rando = ((float)rand.NextDouble()) * 100;
                    int tries = 0;
                    foreach (((int type, int subType) type, float percentage) tupelo in spawnTypes)
                    {
                        if (rando > tupelo.percentage) { rando -= tupelo.percentage; continue; }
                        PlantTraits traits = plantTraitsDict.ContainsKey(tupelo.type) ? plantTraitsDict[tupelo.type] : plantTraitsDict[(-1, 0)];
                    plantInvalidTryAgain:;
                        ((int x, int y) pos, bool valid) returnTuple = findSuitablePosition(forbiddenPositions, false, traits.isWater, traits.isCeiling, traits.isSide, soilType: traits.soilType, isEveryAttach: traits.isEveryAttach);
                        if (!returnTuple.valid) { break; }
                        Plant newPlant = new Plant(this, returnTuple.pos, tupelo.type);
                        while (newPlant.isDeadAndShouldDisappear)
                        {
                            if (tries > 10) { goto finalFail; }
                            tries++;
                            if (newPlant.traits.initFailType != null) { newPlant = new Plant(this, returnTuple.pos, newPlant.traits.initFailType.Value); }
                            else { goto plantInvalidTryAgain; }
                        }
                        screen.activePlants[newPlant.id] = newPlant;
                    finalFail:;
                    }
                }
            }
            public void matureChunk()
            {
                BiomeTraits traits = biomeIndex[16, 16][0].traits;  // Middle of chunk

                bool modified = replaceSurfaceTiles();

                if (spawnEntitiesBool)
                {
                    Dictionary<(int x, int y), bool> forbiddenPositions = new Dictionary<(int x, int y), bool>();
                    spawnOneEntities(traits.entityBaseSpawnRate, traits.entityBaseSpawnTypes, forbiddenPositions);
                    spawnOneEntities(traits.entityGroundSpawnRate, traits.entityGroundSpawnTypes, forbiddenPositions);
                    spawnOneEntities(traits.entityWaterSpawnRate, traits.entityWaterSpawnTypes, forbiddenPositions);
                    spawnOneEntities(traits.entityJesusSpawnRate, traits.entityJesusSpawnTypes, forbiddenPositions);
                }

                if (spawnPlants)
                {
                    Dictionary<(int x, int y), bool> forbiddenPositions = new Dictionary<(int x, int y), bool>();
                    spawnOnePlants(traits.plantGroundSpawnRate, traits.plantGroundSpawnTypes, forbiddenPositions);
                    spawnOnePlants(traits.plantCeilingSpawnRate, traits.plantCeilingSpawnTypes, forbiddenPositions);
                    spawnOnePlants(traits.plantSideSpawnRate, traits.plantSideSpawnTypes, forbiddenPositions);
                    spawnOnePlants(traits.plantTreeSpawnRate, traits.plantTreeSpawnTypes, forbiddenPositions);
                    spawnOnePlants(traits.plantWaterGroundSpawnRate, traits.plantWaterGroundSpawnTypes, forbiddenPositions);
                    spawnOnePlants(traits.plantWaterCeilingSpawnRate, traits.plantWaterCeilingSpawnTypes, forbiddenPositions);
                    spawnOnePlants(traits.plantWaterSideSpawnRate, traits.plantWaterSideSpawnTypes, forbiddenPositions);
                    spawnOnePlants(traits.plantEveryAttachSpawnRate, traits.plantEveryAttachSpawnTypes, forbiddenPositions);
                }
                isMature = true;
                if (modified) { saveChunk(this); }
            }
            public bool replaceSurfaceTiles()
            {
                List<((int i, int j) pos, BiomeTraits traits)> tilesToTryReplace = new List<((int i, int j) pos, BiomeTraits traits)>();

                BiomeTraits biomeTraits;
                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        if (!fillStates[i, j].isSolid) { continue; }
                        biomeTraits = biomeIndex[i, j][0].traits;
                        if (biomeTraits.surfaceMaterial is null) { continue; }
                        foreach ((int x, int y) mod in neighbourArray)
                        {
                            if (!screen.getTileContent((pos.x * 32 + i + mod.x, pos.y * 32 + j + mod.y)).isSolid) { tilesToTryReplace.Add(((i, j), biomeTraits)); break; }
                        }
                    }
                }
                if (tilesToTryReplace.Count == 0) { return false; }

                int[] noiseValues = new int[4] {
                    (int)(LCGxy((pos, 95), screen.seed) % 101),
                    (int)(LCGxy(((pos.x + 1, pos.y), 95), screen.seed) % 101),
                    (int)(LCGxy(((pos.x, pos.y + 1), 95), screen.seed) % 101),
                    (int)(LCGxy(((pos.x + 1, pos.y + 1), 95), screen.seed) % 101)
                };

                bool modified = false;
                foreach (((int i, int j) pos, BiomeTraits traits) item in tilesToTryReplace)
                {
                    if ((noiseValues[0] * (32 - item.pos.i) + noiseValues[1] * item.pos.i) * (32 - item.pos.j) +
                        (noiseValues[2] * (32 - item.pos.i) + noiseValues[3] * item.pos.i) * item.pos.j
                        > item.traits.surfaceMaterial.Value.chance * 1024) { continue; }
                    fillStates[item.pos.i, item.pos.j] = getTileTraits(item.traits.surfaceMaterial.Value.type); findTileColor(item.pos.i, item.pos.j);
                    modified = true;
                }
                return modified;
            }
            public ((int x, int y), bool valid) findSuitablePosition(Dictionary<(int x, int y), bool> forbiddenPositions, bool isEntity, bool isWater, bool isCeiling = false, bool isSide = false, bool isDigging = false, bool isJesus = false, (int type, int subType)[] soilType = null, bool isEveryAttach = false)
            {
                int counto = 0;
                (int x, int y) posToTest;
                (int x, int y) randPos;
                while (counto < 100)
                {
                    counto++;
                    randPos = (pos.x * 32 + rand.Next(32), pos.y * 32 + rand.Next(32));
                    if (forbiddenPositions.ContainsKey(randPos)) { continue; }
                    TileTraits tileTraits = fillStates[PosMod(randPos.x), PosMod(randPos.y)];
                    if (tileTraits.isSolid != isDigging) { forbiddenPositions[randPos] = true; continue; }
                    if (tileTraits.isAir == (isWater && !isJesus) && !isDigging) { continue; };    // If water plant and liquid tile, yay, if normal plant and empty tile, yay, else no
                    if (isEntity && !isJesus) { goto success; }   // For entities, no need to test if ceiling or ground shit

                    if (isEveryAttach) { (int x, int y) mod = neighbourArray[rand.Next(4)]; posToTest = (randPos.x + mod.x, randPos.y + mod.y); }
                    else if (isSide) { posToTest = (randPos.x + (isSide && !isCeiling ? (counto % 2) * 2 - 1 : 0), randPos.y); }
                    else { posToTest = (randPos.x, randPos.y + (isCeiling ? 1 : -1)); }
                    tileTraits = screen.getTileContent(posToTest);

                    if (soilType != null && !soilType.Contains(tileTraits.type)) { continue; }
                    if (isJesus)
                    {
                        if (!tileTraits.isLiquid) { continue ; }
                    }
                    else if (!tileTraits.isSolid) { continue; }
                    goto success;
                }
                return ((0, 0), false);
            success:;
                forbiddenPositions[randPos] = true; // if spawn succeeded, prevent spawning in the same tile
                return (randPos, true);
            }
            public void moveLiquids()
            {
                if (unstableLiquidCount > 0) //here
                {
                    Chunk leftChunk = screen.getChunkFromChunkPos((pos.Item1 - 1, pos.Item2), false, true) ?? theFilledChunk;
                    Chunk bottomLeftChunk = screen.getChunkFromChunkPos((pos.Item1 - 1, pos.Item2 - 1), false, true) ?? theFilledChunk;
                    Chunk bottomChunk = screen.getChunkFromChunkPos((pos.Item1, pos.Item2 - 1), false, true) ?? theFilledChunk;
                    Chunk bottomRightChunk = screen.getChunkFromChunkPos((pos.Item1 + 1, pos.Item2 - 1), false, true) ?? theFilledChunk;
                    Chunk rightChunk = screen.getChunkFromChunkPos((pos.Item1 + 1, pos.Item2), false, true) ?? theFilledChunk;

                    unstableLiquidCount = 0;

                    for (int j = 0; j < 32; j++)
                    {
                        for (int i = 0; i < 32; i++)
                        {
                            if (moveOneLiquid(i, j, leftChunk, bottomLeftChunk, bottomChunk, bottomRightChunk, rightChunk))
                            {
                                unstableLiquidCount++;
                            }
                        }
                    }
                }
            }
            public bool moveOneLiquid(int i, int j, Chunk leftChunkParam, Chunk bottomLeftChunkParam, Chunk bottomChunkParam, Chunk bottomRightChunkParam, Chunk rightChunkParam)
            {
                Chunk leftChunk;
                Chunk leftDiagChunk;
                Chunk middleChunk;
                Chunk rightChunk;
                Chunk rightDiagChunk;

                int jb = (j + 31) % 32;
                int il = (i + 31) % 32;
                int ir = (i + 1) % 32;

                if (j == 0)
                {
                    middleChunk = bottomChunkParam;
                    if (i == 0)
                    {
                        leftChunk = leftChunkParam;
                        leftDiagChunk = bottomLeftChunkParam;
                        rightDiagChunk = bottomChunkParam;
                        rightChunk = this;
                    }
                    else if (i == 31)
                    {
                        leftChunk = this;
                        leftDiagChunk = bottomChunkParam;
                        rightDiagChunk = bottomRightChunkParam;
                        rightChunk = rightChunkParam;
                    }
                    else
                    {
                        leftChunk = this;
                        leftDiagChunk = bottomChunkParam;
                        rightDiagChunk = bottomChunkParam;
                        rightChunk = this;
                    }
                }
                else
                {
                    middleChunk = this;
                    if (i == 0)
                    {
                        leftChunk = leftChunkParam;
                        leftDiagChunk = leftChunkParam;
                        rightDiagChunk = this;
                        rightChunk = this;
                    }
                    else if (i == 31)
                    {
                        leftChunk = this;
                        leftDiagChunk = this;
                        rightDiagChunk = rightChunkParam;
                        rightChunk = rightChunkParam;
                    }
                    else
                    {
                        leftChunk = this;
                        leftDiagChunk = this;
                        rightDiagChunk = this;
                        rightChunk = this;
                    }
                }

                TileTraits traits = fillStates[i, j];
                if (traits.isLiquid)
                {
                    if (middleChunk.fillStates[i, jb].isAir)
                    {
                        tileModification(i, j, (0, 0));
                        middleChunk.tileModification(i, jb, traits);
                        return true;
                    } // THIS ONE WAS FUCKING BUGGYYYYY BRUH
                    if ((i < 15 || middleChunk.pos.Item1 < rightChunk.pos.Item1) && (rightChunk.fillStates[ir, j].isAir || middleChunk.fillStates[i, jb].isLiquid) && rightDiagChunk.fillStates[ir, jb].isAir)
                    {
                        tileModification(i, j, (0, 0));
                        rightDiagChunk.tileModification(ir, jb, traits);
                        return true;
                    } //this ONE WAS BUGGY
                    if ((rightChunk.fillStates[ir, j].isAir || middleChunk.fillStates[i, jb].isLiquid) && rightDiagChunk.fillStates[ir, jb].isLiquid)
                    {
                        if (testLiquidPushRight(i, j))
                        {
                            return true;
                        }
                    }
                    if ((i > 0 || leftChunk.pos.Item1 < middleChunk.pos.Item1) && (leftChunk.fillStates[il, j].isAir || middleChunk.fillStates[i, jb].isLiquid) && leftDiagChunk.fillStates[il, jb].isAir)
                    {
                        tileModification(i, j, (0, 0));
                        leftDiagChunk.tileModification(il, jb, traits);
                        return true;
                    } // THIS ONE WAS ALSO BUGGY
                    if ((leftChunk.fillStates[il, j].isAir || middleChunk.fillStates[i, jb].isLiquid) && leftDiagChunk.fillStates[il, jb].isLiquid)
                    {
                        if (testLiquidPushLeft(i, j))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            public bool testLiquidPushRight(int i, int j)
            {
                int iTested = i;
                int jTested = j - 1;

                int absChunkX = pos.Item1;
                int absChunkY = pos.Item2;
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
                    if (traits.isSolid || screen.liquidsThatCantGoRight.ContainsKey(posToTest)) { break; }
                    if (traits.isAir)
                    {
                        chunkToTest.tileModification(iTested, jTested, tileModification(i, j, (0, 0)));
                        return true;
                    }
                    posVisited.Add(posToTest);
                    liquidSlideCount++;
                    repeatCounter++;
                }
                foreach ((int x, int y) pos in posVisited)
                {
                    screen.liquidsThatCantGoRight[pos] = true;
                }
                return false;
            }
            public bool testLiquidPushLeft(int i, int j)
            {
                int iTested = i;
                int jTested = j - 1;

                int absChunkX = pos.Item1;
                int absChunkY = pos.Item2;
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
                    if (traits.isSolid || screen.liquidsThatCantGoRight.ContainsKey(posToTest)) { break; }
                    if (traits.isAir)
                    {
                        chunkToTest.tileModification(iTested, jTested, tileModification(i, j, (0, 0)));
                        return true;
                    }
                    posVisited.Add(posToTest);
                    liquidSlideCount++;
                    repeatCounter++;
                }
                foreach ((int x, int y) pos in posVisited)
                {
                    screen.liquidsThatCantGoLeft[pos] = true;
                }
                return false;
            }
            public void testLiquidUnstableNonspecific(int posX, int posY)
            {
                Chunk chunkToTest;
                foreach ((int x, int y) mod in directionPositionArray)
                {
                    chunkToTest = screen.getChunkFromPixelPos((posX + mod.x, posY + mod.y));
                    if (!chunkToTest.fillStates[PosMod(posX + mod.x), PosMod(posY + mod.y)].isSolid)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }
            }
            public void testLiquidUnstableAir(int posX, int posY)
            {
                (int, int) chunkPos;
                Chunk chunkToTest;

                chunkPos = ChunkIdx(posX + 1, posY);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX + 1), PosMod(posY)].isLiquid)
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX - 1, posY + 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX - 1), PosMod(posY + 1)].isLiquid)
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX + 1, posY + 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX + 1), PosMod(posY + 1)].isLiquid)
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX - 1, posY);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX - 1), PosMod(posY)].isLiquid)
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX, posY + 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX), PosMod(posY + 1)].isLiquid)
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX, posY - 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX), PosMod(posY - 1)].isLiquid) // CHANGE THIS TOO FUCKER
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }
            }
            public void testLiquidUnstableLiquid(int posX, int posY)
            {
                (int, int) chunkPos;
                Chunk chunkToTest;

                chunkPos = ChunkIdx(posX + 1, posY);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (!chunkToTest.fillStates[PosMod(posX + 1), PosMod(posY)].isSolid)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX - 1, posY - 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (!chunkToTest.fillStates[PosMod(posX - 1), PosMod(posY - 1)].isSolid)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX + 1, posY - 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (!chunkToTest.fillStates[PosMod(posX + 1), PosMod(posY - 1)].isSolid)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX - 1, posY);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (!chunkToTest.fillStates[PosMod(posX - 1), PosMod(posY)].isSolid)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX, posY + 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (!chunkToTest.fillStates[PosMod(posX), PosMod(posY + 1)].isSolid)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX, posY - 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (!chunkToTest.fillStates[PosMod(posX), PosMod(posY - 1)].isSolid)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }
            }
            public MegaChunk getMegaChunk(bool isExtraGetting = false)
            {
                (int x, int y) pos = MegaChunkIdxFromChunkPos(this.pos);
                if (screen.megaChunks.ContainsKey(pos)) { return screen.megaChunks[pos]; }
                if (screen.extraLoadedMegaChunks.ContainsKey(pos))
                {
                    if (isExtraGetting) { return screen.extraLoadedMegaChunks[pos]; }
                    MegaChunk megaChunkToGet = screen.extraLoadedMegaChunks[pos];
                    megaChunkToGet.loadAllStuffInIt();  // Upgrade the extraLoaded MegaChunk to a full MegaChunk, by loading all its contents and putting it in the other dict
                    screen.megaChunks[pos] = megaChunkToGet;
                    screen.extraLoadedMegaChunks.Remove(pos);
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
            public void updateFogOfWarOneTile(Dictionary<Chunk, bool> chunkDict, (int x, int y) posToTest)
            {
                if (explorationLevel == 2) { return; }
                if (explorationLevel == 0)
                {
                    createFogOfWar();
                    chunkDict[this] = true;
                }
                (int x, int y) tileIndex = PosMod(posToTest);
                if (!fogOfWar[tileIndex.x, tileIndex.y])
                {
                    fogOfWar[tileIndex.x, tileIndex.y] = true;
                    fogBitmap.SetPixel(tileIndex.x, tileIndex.y, Color.Transparent);
                    chunkDict[this] = true;
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







            public int[,,] findNoiseValues(int[,,] noiseValues, int layer, int realLayer, int modulo, int noiseAmplitude = 256)  // noiseValues is int[32, 32, depends]   // layer is the one set in the array, realLayer is the one actually gotten   // Modulo is the resolution : 16 for small terrain noise, 64 for big, 1024 for biome.... for example
            {
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
            public int[,,] findNoiseValuesQuartile(int[,,] noiseValues, int layer, int realLayer, int noiseAmplitude = 256)  // noiseValues is int[32, 32, depends]   // layer is the one set in the array, realLayer is the one actually gotten   // Modulo is the resolution : 16 for small terrain noise, 64 for big, 1024 for biome.... for example
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
            int salinity = values.toxi;
            int illumination = values.toxi;
            int oceanity = values.toxi;
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
        public static int calculateBiome(int percentageFree, int valueToTest, (int min, int max) bounds, int transitionSpeed = 25) // transitionSpeed : the higher, the faster the transition
        {
            return (int)(Clamp(0, Min(valueToTest - bounds.min, bounds.max - valueToTest) * transitionSpeed, 1000) * percentageFree * 0.001f);
        }
        public static int calculateAndAddBiome(List<((int biome, int subBiome), int)> biomeList, (int biome, int subBiome) biomeToTest, int percentageFree, int valueToTest, (int min, int max) bounds, int transitionSpeed = 25) // transitionSpeed : the higher, the faster the transition
        {
            int biomeness = (int)(Clamp(0, Min(valueToTest - bounds.min, bounds.max - valueToTest) * transitionSpeed, 1000) * percentageFree * 0.001f);
            if (biomeness > 0) { biomeList.Add((biomeToTest, biomeness)); }
            return biomeness;
        }
        public static (BiomeTraits traits, int percentage)[] findBiome((int, int) dimensionType, int[] values)
        {
            return findBiome(dimensionType, (values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7], values[8]));
        }
        public static (BiomeTraits traits, int percentage)[] findBiome((int, int) dimensionType, (int temp, int humi, int acid, int toxi, int sali, int illu, int ocea, int mod1, int mod2) values)
        {
            //return new (int, int)[]{ (8, 1000) }; // use this to force a biome for debug (infite biome)


            // arrite so... 0 is temperature, 1 is humidity, 2 is acidity, 3 is toxicity, 4 is terrain modifier1, 5 is terrain modifier 2
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
                    if (currentInt > max)
                    {
                        max = currentInt;
                    }
                }
                //max = Max(0, max - 100);
                max -= 100;
                int counto = 0;
                for (int i = listo.Count - 1; i >= 0; i--)
                {
                    listo[i] = (listo[i].Item1, Max(0, listo[i].Item2 - max));
                    if (listo[i].Item2 <= 0)
                    {
                        listo.RemoveAt(i);
                        continue;
                    }
                    counto += listo[i].Item2;
                }
                counto = Max(100, counto);
                for (int i = 0; i < listo.Count; i++)
                {
                    listo[i] = (listo[i].Item1, listo[i].Item2 * 1000 / counto);
                }
                if (listo.Count == 0)
                {
                    listo.Add(((-1, 0), 1000));
                }

            }
            else    // The GOOD version of the biome shit
            {
                if (dimensionType == (0, 0)) // type == 1, normal dimension
                {
                    listo = new List<((int biome, int subBiome), int)>();

                    if (oceanity > 720)
                    {
                        int oceaness = calculateBiome(percentageFree, oceanity, (720, 999999));   // ocean
                        int oceanToAdd = oceaness - calculateAndAddBiome(listo, (8, 1), oceaness, temperature, (-999999, 0)); // Frozen ocean
                        testAddBiome(listo, (8, 0), oceanToAdd);
                        percentageFree -= oceaness;
                    }

                    percentageFree -= calculateAndAddBiome(listo, (0, 1), percentageFree, temperature, (-999999, 0));   // add frost
                    percentageFree -= calculateAndAddBiome(listo, (6, 0), percentageFree, Min(500 - temperature, humidity - 500, acidity - 500), (0, 999999), 5);  // add mold

                    if (percentageFree <= 0) { goto AfterTest; }

                    if (temperature > 720)
                    {
                        int hotness = calculateBiome(percentageFree, temperature, (720, 999999));
                        if (temperature > 840 && humidity > 600)
                        {
                            int obsidianess = calculateAndAddBiome(listo, (2, 2), hotness, Min(temperature - 840, humidity - 600), (0, 999999));
                            percentageFree -= obsidianess;
                            hotness -= obsidianess;
                        }
                        if (temperature > 920 && oceanity < 512)
                        {
                            int lavaness = calculateAndAddBiome(listo, (2, 1), hotness, Min(temperature - 920, 512 - oceanity), (0, 999999));
                            percentageFree -= lavaness;
                            hotness -= lavaness;
                        }
                        percentageFree -= testAddBiome(listo, (2, 0), hotness);
                    }

                    if (temperature < 440 && percentageFree > 0)
                    {
                        int coldness = calculateBiome(percentageFree, temperature, (-999999, 440));
                        int savedColdness = calculateBiome(coldness, temperature, (-999999, 120));
                        coldness -= savedColdness;

                        if (acidity > 700)
                        {
                            int acidness = calculateAndAddBiome(listo, (1, 0), coldness, acidity, (700, 999999));
                            percentageFree -= acidness;
                            coldness -= acidness;
                        }
                        if (humidity > toxicity)
                        {
                            int fairyness = calculateAndAddBiome(listo, (5, 0), coldness, humidity - toxicity, (0, 999999));
                            percentageFree -= fairyness;
                            coldness -= fairyness;
                        }

                        coldness += savedColdness;
                        percentageFree -= testAddBiome(listo, (0, 0), coldness);
                    }

                    if (percentageFree > 0)
                    {
                        percentageFree -= calculateAndAddBiome(listo, (4, 0), percentageFree, toxicity, (715, 999999));  // add slime
                        percentageFree -= calculateAndAddBiome(listo, (3, 1), percentageFree, (500 - toxicity) + (int)(0.4f * (humidity - temperature)), (0, 999999));  // add flower forest
                        testAddBiome(listo, (3, 0), percentageFree); // add what's remaining as forest
                    }
                }
                else if (dimensionType == (1, 0)) // type == 1, chandelier dimension
                {
                    percentageFree -= calculateAndAddBiome(listo, (11, 0), percentageFree, oceanity, (720, 999999)); // Dark ocean...
                    percentageFree -= calculateAndAddBiome(listo, (10, 0), percentageFree, temperature, (700, 999999)); // Lantern
                    percentageFree -= calculateAndAddBiome(listo, (10, 2), percentageFree, temperature, (-999999, 300)); // Chandelier
                    testAddBiome(listo, (10, 1), percentageFree); // MixedLuminous
                }
                else if (dimensionType == (2, 0)) // type == 2, living dimension
                {
                    percentageFree -= calculateAndAddBiome(listo, (22, 1), percentageFree, acidity, (800, 999999)); // acid ocean
                    percentageFree -= calculateAndAddBiome(listo, (22, 0), percentageFree, temperature, (-999999, 300)); // blood ocean
                    if (humidity > 500)
                    {
                        int fleshiness = calculateBiome(percentageFree, humidity, (500, 999999));
                        int hairiness = 0;
                        if (toxicity >= 700)
                        {
                            hairiness = calculateBiome(fleshiness, toxicity, (700, 999999));
                            int longHairToAdd = hairiness - calculateAndAddBiome(listo, (20, 3), hairiness, temperature + acidity, (1024, 999999)); // hair forest
                            testAddBiome(listo, (20, 4), longHairToAdd); // long hair forest
                            fleshiness -= hairiness;
                        }
                        int forestness = calculateAndAddBiome(listo, (20, 1), fleshiness, toxicity, (-999999, 350)); // flesh forest
                        fleshiness -= forestness;
                        percentageFree -= forestness + hairiness;
                        percentageFree -= testAddBiome(listo, (20, 0), fleshiness); // add what's remaining as normal flesh
                    }
                    percentageFree -= calculateAndAddBiome(listo, (21, 0), percentageFree, humidity, (-999999, 300)); // bone
                    testAddBiome(listo, (20, 2), percentageFree); // flesh and bone
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
                mult = tupel.Item2 * 0.001f;
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
