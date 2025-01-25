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
using static Cave.Files;
using static Cave.Plants;
using static Cave.Screens;
using static Cave.Chunks;
using static Cave.Players;
using static Cave.Particles;

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

            public ((int biome, int subBiome), int)[,][] biomeIndex;

            public (int type, int subType)[,] fillStates = new (int type, int subType)[32, 32];
            public (int, int, int)[,] baseColors;
            public Bitmap bitmap;

            public List<Entity> entityList = new List<Entity>();
            public Dictionary<int, Plant> plants = new Dictionary<int, Plant>();
            public List<Plant> exteriorPlantList = new List<Plant>();

            public int modificationCount = 0;
            public int unstableLiquidCount = 1;
            public bool entitiesAndPlantsSpawned = false;

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
                entitiesAndPlantsSpawned = chunkJson.spwnd;

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
                    if (explorationLevel == 1)
                    {
                        fogOfWar = chunkJson.fog;
                        fogBitmap = new Bitmap(32, 32);
                        for (int i = 0; i < 32; i++)
                        {
                            for (int j = 0; j < 32; j++)
                            {
                                if (!fogOfWar[i, j]) { setPixelButFaster(fogBitmap, (i, j), Color.Black); }
                            }
                        }
                    }
                    else { fogOfWar = null; }
                }

                if (!entitiesAndPlantsSpawned)
                {
                    screen.chunksToSpawnEntitiesIn[pos] = true;
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

                (int temp, int humi, int acid, int toxi, int mod1, int mod2)[,] tileValuesArray = determineAllBiomeValues();

                if (chunkJson == null) { generateTerrain(tileValuesArray); }   // If first loading only, generate terrain

                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        int darkness = 0;
                        foreach (((int biome, int subBiome), int) tupel in biomeIndex[i, j])
                        {
                            if (darkBiomes.ContainsKey(tupel.Item1))
                            {
                                darkness += (int)(tupel.Item2 * 0.3f);
                            }
                        }
                        darkness = Max(0, 255 - darkness);
                        Color colorToDraw = Color.FromArgb(255, darkness, darkness, darkness);
                        setPixelButFaster(lightBitmap, (i, j), colorToDraw);
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
            public (int temp, int humi, int acid, int toxi, int mod1, int mod2)[,] determineAllBiomeValues()
            {
                int[,,] biomeValues = new int[33, 33, 12];
                if (!screen.isMonoBiome)
                {
                    findNoiseValues(biomeValues, 0, 100, 512, 1028);    // big temp
                    findNoiseValues(biomeValues, 1, 101, 1024, 1028);   // small temp
                    findNoiseValues(biomeValues, 2, 102, 512, 1028);    // big humi
                    findNoiseValues(biomeValues, 3, 103, 1024, 1028);   // small humi
                    findNoiseValues(biomeValues, 4, 104, 512, 1028);    // big acid
                    findNoiseValues(biomeValues, 5, 105, 1024, 1028);   // small acid
                    findNoiseValues(biomeValues, 6, 106, 512, 1028);    // big toxi
                    findNoiseValues(biomeValues, 7, 107, 1024, 1028);   // small toxi
                    findNoiseValues(biomeValues, 8, 108, 512, 1028);    // big mod1
                    findNoiseValues(biomeValues, 9, 109, 1024, 1028);   // small mod1
                    findNoiseValues(biomeValues, 10, 110, 512, 1028);   // big mod2
                    findNoiseValues(biomeValues, 11, 111, 1024, 1028);  // small mod2
                }

                (int temp, int humi, int acid, int toxi, int mod1, int mod2)[,] tileValuesArray = new (int temp, int humi, int acid, int toxi, int mod1, int mod2)[32, 32];
                biomeIndex = new ((int biome, int subBiome), int)[32, 32][];
                baseColors = new (int, int, int)[32, 32];
                bitmap = new Bitmap(32, 32);

                for (int i = 0; i < 32; i += 1)
                {
                    for (int j = 0; j < 32; j += 1)
                    {
                        (int temp, int humi, int acid, int toxi, int mod1, int mod2) tileValues;
                        if (screen.isMonoBiome)
                        {
                            tileValues = makeTileBiomeValueArrayMonoBiome(screen.type);
                            biomeIndex[i, j] = new ((int biome, int subBiome), int)[] { (screen.type, 1000) };
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
            public void generateTerrain((int temp, int humi, int acid, int toxi, int mod1, int mod2)[,] tileValuesArray)
            {
                fillStates = new (int type, int subType)[32, 32];

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
                        // value1 = terrainValues[i, j, 1];
                        // value1 = 0;
                        int value2 = terrainValues[i, j, 2] + (int)(0.25 * terrainValues[i, j, 3]) - 32;
                        // value2 = terrainValues[i, j, 3];
                        // value2 = 128;
                        int temperature = tileValuesArray[i, j].temp;
                        int mod1 = (int)(tileValuesArray[i, j].mod1 * 0.25);
                        int mod2 = (int)(tileValuesArray[i, j].mod2 * 0.25);

                        int plateauPos = (int)(chunkSeed % 32);

                        float valueToBeAdded;
                        float value1modifier = 0;
                        float value2PREmodifier;
                        float value2modifier = 0;
                        float mod2divider = 1;
                        float foresto = 1;
                        float oceano = 0;
                        float oceanoSeeSaw = 0;

                        float mult;
                        foreach (((int biome, int subBiome), int) tupel in biomeIndex[i, j])
                        {
                            mult = tupel.Item2 * 0.001f;
                            if (tupel.Item1 == (1, 0))  // acid biome
                            {
                                value2modifier += -3 * mult * Max(sawBladeSeesaw(value1, 13), sawBladeSeesaw(value1, 11));
                            }
                            else if (tupel.Item1 == (3, 0) || tupel.Item1 == (9, 0))    // forest and chandelier biomes
                            {
                                foresto += mult;
                            }
                            else if (tupel.Item1 == (4, 0)) // toxic biome
                            {
                                float see1 = Sin(i + mod2 * 0.3f + 0.5f, 16);
                                float see2 = Sin(j + mod2 * 0.3f + 0.5f, 16);
                                valueToBeAdded = mult * Min(0, 20 * (see1 + see2) - 10);
                                value2modifier += valueToBeAdded;
                                value1modifier += valueToBeAdded + 2;
                            }
                            else if (tupel.Item1 == (2, 2)) // obsidian biome
                            {
                                float see1 = Obs((pos.Item1 * 32) % 64 + 64 + i + mod2 * 0.15f + 0.5f, 64);
                                float see2 = Obs((pos.Item2 * 32) % 64 + 64 + j + mod2 * 0.15f + 0.5f, 64);
                                if (false && (value2 < 50 || value2 > 200))
                                {
                                    value2PREmodifier = 300;
                                }
                                else
                                {
                                    value2PREmodifier = (Min(0, -40 * (see1 + see2) + 20) * 10 + value2 - 200);
                                }
                                value1modifier += 5 * mult;
                                value2modifier += mult * value2PREmodifier;
                                mod2divider += mult * 1.5f;
                            }
                            else if (tupel.Item1 == (8, 0) || tupel.Item1 == (2, 1) || tupel.Item1 == (10, 3) || tupel.Item1 == (10, 4))    // ocean biomes
                            {
                                oceano = Max(oceano, mult * 10); // To make separation between OCEAN biomes (like acid and blood). CHANGE THIS to make ocean biomes that can merge with one another (like idk cool water ocean and temperate water ocean idk)
                            }
                            else { value2modifier += mult * ((2 * value1) % 32); }
                        }

                        oceanoSeeSaw = Min(Seesaw((int)oceano, 8), 8 - oceano);
                        if (oceanoSeeSaw < 0)
                        {
                            oceanoSeeSaw = oceanoSeeSaw * oceanoSeeSaw * oceanoSeeSaw;
                        }
                        else { oceanoSeeSaw = oceanoSeeSaw * Abs(oceanoSeeSaw); }
                        oceano *= 10;
                        oceanoSeeSaw *= 10;

                        mod2 = (int)(mod2 / mod2divider);

                        (int type, int subType) elementToFillVoidWith;
                        if (biomeIndex[i, j][0].Item1 == (8, 0)) { elementToFillVoidWith = (-2, 0); } // ocean
                        else if (biomeIndex[i, j][0].Item1 == (2, 1)) { elementToFillVoidWith = (-4, 0); } // lava ocean
                        else if (biomeIndex[i, j][0].Item1 == (10, 3)) { elementToFillVoidWith = (-6, 0); } // blood ocean
                        else if (biomeIndex[i, j][0].Item1 == (10, 4)) { elementToFillVoidWith = (-7, 0); } // acid ocean
                        else { elementToFillVoidWith = (0, 0); }

                        float score1 = Min(value1 - (122 - mod2 * mod2 * foresto * 0.0003f + value1modifier + (int)(oceanoSeeSaw * 0.1f)), -value1 + (133 + mod2 * mod2 * foresto * 0.0003f - value1modifier - oceanoSeeSaw));
                        bool fillTest1 = score1 > 0;
                        float score2 = Max(value2 - (200 + value2modifier + oceano), -value2 + ((foresto - 1) * 75f - oceano));
                        bool fillTest2 = score2 > 0;
                        float plateauScore = Max(score1, score2) - (Abs(plateauPos - j) - 5) * 10;
                        //if (fillTest1 && fillTest2) { fillStates[i, j] = 4; }
                        //else if (fillTest1) { fillStates[i, j] = 3; }
                        //else if (fillTest2) { fillStates[i, j] = 2; }
                        if (((fillTest1 || fillTest2) && true) || (false && plateauScore >= 0)) { fillStates[i, j] = elementToFillVoidWith; }
                        else
                        {
                            Dictionary<(int type, int subType), float> dicto = findTransitions(biomeIndex[i, j], tileValuesArray[i, j]);
                            fillStates[i, j] = findMaterialToFillWith((terrainValues[i, j, 4], terrainValues[i, j, 5]), biomeIndex[i, j][0].Item1, dicto);
                        }
                        //if (rand.Next(500) != 0){ fillStates[i, j] = 1; }
                    }
                }
            }
            public (int type, int subType) findMaterialToFillWith((int, int) values, (int type, int subType) biome, Dictionary<(int type, int subType), float> dicto)
            {
                int value = (int)((values.Item1 + values.Item2) * 0.5f);
                if (biome == (10, 0)) // flesh
                {
                    return (4, 0);
                }
                else if (biome == (10, 1) || biome == (10, 3) || biome == (10, 4)) // flesh and bone (for acid and blood oceans too since they can have the transition)
                {
                    if (Max(0, (int)(Abs(Abs(values.Item1 - 1024) * 0.49f) + values.Item2 % 256)) >= 512 - dicto[biome] * 513)
                    {
                        return (4, 1);
                    }
                    return (4, 0);
                }
                else if (biome == (10, 2)) // bone
                {
                    return (4, 1);
                }

                if (value % 256 - (512 - (int)(value * 0.25f)) > 0 && Abs(values.Item1 - values.Item2) < 512 - (512 - (int)(value * 0.25f)))
                {
                    return (1, 1);
                }

                if (biome == (6, 0)) // mold
                {
                    if (Max(0, (int)(Abs(Abs(values.Item1 - 1024) * 0.49f))) >= 1024 - dicto[biome] * 1024) { return (5, 0); }
                }

                return (1, 0);
            }
            public void findTileColor(int i, int j)
            {
                int[] colorArray = { baseColors[i, j].Item1, baseColors[i, j].Item2, baseColors[i, j].Item3 };
                Color colorToSet;
                if (tileColors.ContainsKey(fillStates[i, j]))
                {
                    (int r, int g, int b, float mult) materialColor = tileColors[fillStates[i, j]];
                    for (int k = 0; k < 3; k++)
                    {
                        colorArray[k] = (int)(colorArray[k] * materialColor.mult);
                    };
                    int rando = 0;
                    if (fillStates[i, j] == (5, 0)) { rando = Abs((int)(LCGyNeg(LCGxPos(pos.x * 32 + i)%153 + LCGyPos(pos.y * 32 + j)%247) % 279)) % 40 - 20; }
                    colorArray[0] += (int)(materialColor.r * (1 - materialColor.mult)) + rando;
                    colorArray[1] += (int)(materialColor.g * (1 - materialColor.mult)) + rando;
                    colorArray[2] += (int)(materialColor.b * (1 - materialColor.mult)) + rando;
                    colorToSet = Color.FromArgb(ColorClamp(colorArray[0]), ColorClamp(colorArray[1]), ColorClamp(colorArray[2]));
                }
                else
                {
                    if ((i + j) % 2 == 0) { colorToSet = Color.Black; }
                    else { colorToSet = Color.FromArgb(255, 00, 255); }
                }
                setPixelButFaster(bitmap, (i, j), colorToSet);
            }
            public (int type, int subType) tileModification(int i, int j, (int type, int subType) newMaterial)
            {
                i = PosMod(i);
                j = PosMod(j);
                (int x, int y) posToModify = (i + pos.x * 32, j + pos.y * 32);
                (int type, int subType) previous = fillStates[i, j];
                fillStates[i, j] = newMaterial;
                findTileColor(i, j);
                testLiquidUnstableNonspecific(posToModify.x, posToModify.y);
                modificationCount += 1;
                checkForStructureAlteration(posToModify, newMaterial);
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
            public void spawnEntities()
            {
                Dictionary<(int x, int y), bool> forbiddenPositions = new Dictionary<(int x, int y), bool>();
                if (Globals.spawnEntities)
                {
                    Entity newEntity = new Entity(this);
                    if (!newEntity.isDeadAndShouldDisappear)
                    {
                        screen.activeEntities[newEntity.id] = newEntity;
                    }
                }
                if (spawnPlants)
                {
                    int smallPlantsToSpawn = 4;
                    int vinesToSpawn = 1;
                    int treesToSpawn = 0;
                    (int biome, int subBiome) mainBiome = biomeIndex[16, 16][0].Item1;
                    if (mainBiome == (3, 0))
                    {
                        smallPlantsToSpawn = 6;
                        vinesToSpawn = 3;
                        treesToSpawn = 2;
                    }
                    else if (mainBiome == (3, 1))
                    {
                        smallPlantsToSpawn = 16;
                        vinesToSpawn = 2;
                    }
                    else if (mainBiome == (8, 0))
                    {
                        vinesToSpawn = 4;
                    }
                    else if (mainBiome == (9, 0))
                    {
                        treesToSpawn = 1;
                    }

                    for (int i = 0; i < vinesToSpawn; i++)
                    {
                        ((int x, int y), bool valid) returnTuple = findCeilingPlantPosition(forbiddenPositions);
                        if (returnTuple.valid)
                        {
                            Plant newPlant = new Plant(this, 0, 1, returnTuple.Item1);
                            if (!newPlant.isDeadAndShouldDisappear)
                            {
                                screen.activePlants[newPlant.id] = newPlant;
                            }
                        }
                    }
                    for (int i = 0; i < treesToSpawn; i++)
                    {
                        ((int x, int y), bool valid) returnTuple = findGroundPlantPosition(forbiddenPositions);
                        if (returnTuple.valid)
                        {
                            Plant newPlant = new Plant(this, 1, 0, returnTuple.Item1);
                            if (!newPlant.isDeadAndShouldDisappear)
                            {
                                screen.activePlants[newPlant.id] = newPlant;
                            }
                        }
                    }
                    for (int i = 0; i < smallPlantsToSpawn; i++)
                    {
                        ((int x, int y), bool valid) returnTuple = findGroundPlantPosition(forbiddenPositions);
                        if (returnTuple.valid)
                        {
                            Plant newPlant = new Plant(this, 0, 0, returnTuple.Item1);
                            if (!newPlant.isDeadAndShouldDisappear)
                            {
                                screen.activePlants[newPlant.id] = newPlant;
                            }
                        }
                    }
                    entitiesAndPlantsSpawned = true;
                }
            }
            public ((int x, int y), bool valid) findGroundPlantPosition(Dictionary<(int x, int y), bool> forbiddenPositions)
            {
                int counto = 0;
                (int x, int y) chunkPos;
                (int x, int y) tileIndex;
                while (counto < 1000)
                {
                    int randX = rand.Next(32);
                    int randY = rand.Next(32);
                    if (forbiddenPositions.ContainsKey((randX, randY))) { }
                    else if (screen.loadedChunks.TryGetValue(pos, out Chunk chunkToTest))
                    {
                        if (chunkToTest.fillStates[randX, randY].type <= 0)
                        {
                            chunkPos = ChunkIdx(pos.x*32 + randX, pos.y*32 + randY - 1);
                            if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTesta))
                            {
                                tileIndex = PosMod((randX, randY - 1));
                                if (chunkToTesta.fillStates[tileIndex.x, tileIndex.y].type > 0)
                                {
                                    forbiddenPositions[(randX, randY)] = true;
                                    return ((pos.x*32 + randX, pos.y*32 + randY), true);
                                }
                            }
                        }
                    }
                    counto++;
                }
                return ((0, 0), false);
            }
            public ((int x, int y), bool valid) findCeilingPlantPosition(Dictionary<(int x, int y), bool> forbiddenPositions)
            {
                int counto = 0;
                (int x, int y) chunkPos;
                (int x, int y) tileIndex;
                while (counto < 1000)
                {
                    int randX = rand.Next(32);
                    int randY = rand.Next(32);
                    if (forbiddenPositions.ContainsKey((randX, randY))) { }
                    else if (screen.loadedChunks.TryGetValue(pos, out Chunk chunkToTest))
                    {
                        tileIndex = PosMod((randX, randY));
                        if (chunkToTest.fillStates[tileIndex.x, tileIndex.y].type <= 0)
                        {
                            chunkPos = ChunkIdx(pos.x*32 + randX, pos.y*32 + randY + 1);
                            if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTesta))
                            {
                                tileIndex = PosMod((randX, randY + 1));
                                if (chunkToTesta.fillStates[tileIndex.x, tileIndex.y].type > 0)
                                {
                                    forbiddenPositions[(randX, randY)] = true;
                                    return ((pos.x*32 + randX, pos.y*32 + randY), true);
                                }
                            }
                        }
                    }
                    counto++;
                }
                return ((0, 0), false);
            }
            public void moveLiquids()
            {
                if (unstableLiquidCount > 0) //here
                {
                    (int x, int y) chunkCoords = ChunkIdx(pos.Item1 * 32 - 32, pos.Item2 * 32);
                    Chunk leftChunk = screen.tryToGetChunk(chunkCoords);
                    chunkCoords = ChunkIdx(pos.Item1 * 32 - 32, pos.Item2 * 32 - 32);
                    Chunk bottomLeftChunk = screen.tryToGetChunk(chunkCoords);
                    chunkCoords = ChunkIdx(pos.Item1 * 32, pos.Item2 * 32 - 32);
                    Chunk bottomChunk = screen.tryToGetChunk(chunkCoords);
                    chunkCoords = ChunkIdx(pos.Item1 * 32 + 32, pos.Item2 * 32 - 32);
                    Chunk bottomRightChunk = screen.tryToGetChunk(chunkCoords);
                    chunkCoords = ChunkIdx(pos.Item1 * 32 + 32, pos.Item2 * 32);
                    Chunk rightChunk = screen.tryToGetChunk(chunkCoords);

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
            public bool moveOneLiquid(int i, int j, Chunk leftChunk, Chunk bottomLeftChunk, Chunk bottomChunk, Chunk bottomRightChunk, Chunk rightChunk)
            {
                Chunk leftTestPositionChunk;
                Chunk leftDiagTestPositionChunk;
                Chunk middleTestPositionChunk;
                Chunk rightTestPositionChunk;
                Chunk rightDiagTestPositionChunk;

                int jb = (j + 31) % 32;
                int il = (i + 31) % 32;
                int ir = (i + 1) % 32;

                if (j == 0)
                {
                    middleTestPositionChunk = bottomChunk;
                    if (i == 0)
                    {
                        leftTestPositionChunk = leftChunk;
                        leftDiagTestPositionChunk = bottomLeftChunk;
                        rightDiagTestPositionChunk = bottomChunk;
                        rightTestPositionChunk = this;
                    }
                    else if (i == 31)
                    {
                        leftTestPositionChunk = this;
                        leftDiagTestPositionChunk = bottomChunk;
                        rightDiagTestPositionChunk = bottomRightChunk;
                        rightTestPositionChunk = rightChunk;
                    }
                    else
                    {
                        leftTestPositionChunk = this;
                        leftDiagTestPositionChunk = bottomChunk;
                        rightDiagTestPositionChunk = bottomChunk;
                        rightTestPositionChunk = this;
                    }
                }
                else
                {
                    middleTestPositionChunk = this;
                    if (i == 0)
                    {
                        leftTestPositionChunk = leftChunk;
                        leftDiagTestPositionChunk = leftChunk;
                        rightDiagTestPositionChunk = this;
                        rightTestPositionChunk = this;
                    }
                    else if (i == 31)
                    {
                        leftTestPositionChunk = this;
                        leftDiagTestPositionChunk = this;
                        rightDiagTestPositionChunk = rightChunk;
                        rightTestPositionChunk = rightChunk;
                    }
                    else
                    {
                        leftTestPositionChunk = this;
                        leftDiagTestPositionChunk = this;
                        rightDiagTestPositionChunk = this;
                        rightTestPositionChunk = this;
                    }
                }

                (int type, int subType) material = fillStates[i, j];
                if (material.type < 0)
                {
                    if (middleTestPositionChunk.fillStates[i, jb].type == 0)
                    {
                        tileModification(i, j, (0, 0));
                        middleTestPositionChunk.tileModification(i, jb, material);
                        return true;
                    } // THIS ONE WAS FUCKING BUGGYYYYY BRUH
                    if ((i < 15 || middleTestPositionChunk.pos.Item1 < rightTestPositionChunk.pos.Item1) && (rightTestPositionChunk.fillStates[ir, j].type == 0 || middleTestPositionChunk.fillStates[i, jb].type < 0) && rightDiagTestPositionChunk.fillStates[ir, jb].type == 0)
                    {
                        tileModification(i, j, (0, 0));
                        rightDiagTestPositionChunk.tileModification(ir, jb, material);
                        return true;
                    } //this ONE WAS BUGGY
                    if ((rightTestPositionChunk.fillStates[ir, j].type == 0 || middleTestPositionChunk.fillStates[i, jb].type < 0) && rightDiagTestPositionChunk.fillStates[ir, jb].type < 0)
                    {
                        if (testLiquidPushRight(i, j))
                        {
                            return true;
                        }
                    }
                    if ((i > 0 || leftTestPositionChunk.pos.Item1 < middleTestPositionChunk.pos.Item1) && (leftTestPositionChunk.fillStates[il, j].type == 0 || middleTestPositionChunk.fillStates[i, jb].type < 0) && leftDiagTestPositionChunk.fillStates[il, jb].type == 0)
                    {
                        tileModification(i, j, (0, 0));
                        leftDiagTestPositionChunk.tileModification(il, jb, material);
                        return true;
                    } // THIS ONE WAS ALSO BUGGY
                    if ((leftTestPositionChunk.fillStates[il, j].type == 0 || middleTestPositionChunk.fillStates[i, jb].type < 0) && leftDiagTestPositionChunk.fillStates[il, jb].type < 0)
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
                (int, int) chunkCoords = ChunkIdx(absChunkX * 32, absChunkY * 32);
                Chunk chunkToTest = screen.tryToGetChunk(chunkCoords);

                List<(int x, int y)> posVisited = new List<(int x, int y)> { (absChunkX * 32 + iTested, absChunkY*32 + jTested) };
                (int x, int y) posToTest;

                int repeatCounter = 0;
                while (repeatCounter < 500)
                {
                    iTested++;
                    if (iTested > 31)
                    {
                        absChunkX++;
                        chunkCoords = ChunkIdx(absChunkX * 32, absChunkY * 32);
                        chunkToTest = screen.tryToGetChunk(chunkCoords);
                        iTested -= 32;
                        if (absChunkX >= screen.chunkX + screen.chunkResolution) { break; }
                    }
                    posToTest = (absChunkX * 32 + iTested, absChunkY * 32 + jTested);
                    (int type, int subType) material = chunkToTest.fillStates[iTested, jTested];
                    if (material.type > 0 || screen.liquidsThatCantGoRight.ContainsKey(posToTest)) { goto bumpedOnSolid; }
                    if (material.type == 0)
                    {
                        chunkToTest.tileModification(iTested, jTested, tileModification(i, j, (0, 0)));
                        return true;
                    }
                    posVisited.Add(posToTest);
                    liquidSlideCount++;
                    repeatCounter++;
                }
                return false;
            bumpedOnSolid:;
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
                (int, int) chunkCoords = ChunkIdx(absChunkX * 32, absChunkY * 32);
                Chunk chunkToTest = screen.tryToGetChunk(chunkCoords);

                List<(int x, int y)> posVisited = new List<(int x, int y)> { (absChunkX * 32 + iTested, absChunkY * 32 + jTested) };
                (int x, int y) posToTest;

                int repeatCounter = 0;
                while (repeatCounter < 500)
                {
                    iTested--;
                    if (iTested < 0)
                    {
                        absChunkX--;
                        chunkCoords = ChunkIdx(absChunkX * 32, absChunkY * 32);
                        chunkToTest = screen.tryToGetChunk(chunkCoords);
                        iTested += 32;
                        if (absChunkX < screen.chunkX) { break; }
                    }
                    posToTest = (absChunkX * 32 + iTested, absChunkY * 32 + jTested);
                    (int type, int subType) material = chunkToTest.fillStates[iTested, jTested];
                    if (material.type > 0 || screen.liquidsThatCantGoLeft.ContainsKey(posToTest)) { goto bumpedOnSolid; }
                    if (material.type == 0)
                    {
                        chunkToTest.tileModification(iTested, jTested, tileModification(i, j, (0, 0)));
                        return true;
                    }
                    posVisited.Add(posToTest);
                    liquidSlideCount++;
                    repeatCounter++;
                }
                return false;
            bumpedOnSolid:;
                foreach ((int x, int y) pos in posVisited)
                {
                    screen.liquidsThatCantGoLeft[pos] = true;
                }
                return false;
            }
            public void testLiquidUnstableNonspecific(int posX, int posY)
            {
                (int, int) chunkPos;
                Chunk chunkToTest;

                foreach ((int x, int y) mod in directionPositionArray)
                {
                    chunkPos = ChunkIdx(posX + mod.x, posY + mod.y);
                    if (screen.loadedChunks.ContainsKey(chunkPos))
                    {
                        chunkToTest = screen.loadedChunks[chunkPos];
                        if (chunkToTest.fillStates[PosMod(posX + mod.x), PosMod(posY + mod.y)].type <= 0)
                        {
                            chunkToTest.unstableLiquidCount++;
                            unstableLiquidCount++;
                        }
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
                    if (chunkToTest.fillStates[PosMod(posX + 1), PosMod(posY)].type < 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX - 1, posY + 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX - 1), PosMod(posY + 1)].type < 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX + 1, posY + 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX + 1), PosMod(posY + 1)].type < 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX - 1, posY);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    ;
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX - 1), PosMod(posY)].type < 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX, posY + 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX), PosMod(posY + 1)].type < 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX, posY - 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX), PosMod(posY - 1)].type < 0) // CHANGE THIS TOO FUCKER
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
                    if (chunkToTest.fillStates[PosMod(posX + 1), PosMod(posY)].type <= 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX - 1, posY - 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX - 1), PosMod(posY - 1)].type <= 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX + 1, posY - 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX + 1), PosMod(posY - 1)].type <= 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX - 1, posY);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX - 1), PosMod(posY)].type <= 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX, posY + 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX), PosMod(posY + 1)].type <= 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }

                chunkPos = ChunkIdx(posX, posY - 1);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(posX), PosMod(posY - 1)].type <= 0)
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
                        setPixelButFaster(bitmapToExport, (i, noiseValues.GetLength(1) - 1 - j), Color.FromArgb(value, value, value));
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
                    theFilledChunk.fillStates[i, j] = (1, 0);
                }
            }
        }
        public static Dictionary<(int type, int subType), float> findTransitions(((int biome, int subBiome), int)[] biomeArray, (int temp, int humi, int acid, int toxi, int mod1, int mod2) values)
        {
            Dictionary<(int type, int subType), float> dicto = new Dictionary<(int type, int subType), float>();
            foreach (((int type, int subType), int) key in biomeArray)
            {
                if (key.Item1 == (10, 1) || key.Item1 == (10, 3) || key.Item1 == (10, 4))
                {
                    dicto[key.Item1] = findTransition((10, 1), values);
                }
                else if (key.Item1 == (6, 0))
                {
                    dicto[key.Item1] = findTransition((6, 0), values);
                }
            }
            return dicto;
        }
        public static float findTransition((int type, int subType) biome, (int temp, int humi, int acid, int toxi, int mod1, int mod2) values)
        {
            if (biome == (10, 1) || biome == (10, 3) || biome == (10, 4))
            {
                return Clamp(0, (660 - values.humi) / 320f, 1);
            }
            else if (biome == (6, 0))
            {
                return Clamp(0, Min(500 - values.temp, values.humi - 500, values.acid - 500) / 320f, 1);
            }

            return 0;
        }
        public static (int temp, int humi, int acid, int toxi, int mod1, int mod2) makeTileBiomeValueArrayMonoBiome((int type, int subType) biome)
        {
            int temperature = biomeTypicalValues[biome].temp;
            int humidity = biomeTypicalValues[biome].humi;
            int acidity = biomeTypicalValues[biome].acid;
            int toxicity = biomeTypicalValues[biome].toxi;
            int mod1 = 0;
            int mod2 = 0;
            return (temperature, humidity, acidity, toxicity, mod1, mod2);
        }
        public static (int temp, int humi, int acid, int toxi, int mod1, int mod2) makeTileBiomeValueArray(int[,,] values, int posX, int posY)
        {
            int temperature = values[posX, posY, 0] + values[posX, posY, 1] - 512;
            int humidity = values[posX, posY, 2] + values[posX, posY, 3] - 512;
            int acidity = values[posX, posY, 4] + values[posX, posY, 5] - 512;
            int toxicity = values[posX, posY, 6] + values[posX, posY, 7] - 512;
            int mod1 = values[posX, posY, 8] + values[posX, posY, 9] - 512;
            int mod2 = values[posX, posY, 10] + values[posX, posY, 11] - 512;
            return (temperature, humidity, acidity, toxicity, mod1, mod2);
        }
        public static (int temp, int humi, int acid, int toxi, int mod1, int mod2) makeTileBiomeValueArray(int[] values, int posX, int posY)
        {
            int temperature = values[0];
            int humidity = values[1];
            int acidity = values[2];
            int toxicity = values[3];
            int mod1 = values[4];
            int mod2 = values[5];
            return (temperature, humidity, acidity, toxicity, mod1, mod2);
        }
        public static int testAddBiome(List<((int biome, int subBiome), int)> biomeList, (int biome, int subBiome) biomeToTest, int biomeness)
        {
            if (biomeness > 0)
            {
                biomeList.Add((biomeToTest, biomeness));
            }
            return biomeness;
        }
        public static int calculateBiome(int percentageFree, int valueToTest, (int min, int max) bounds, int transitionSpeed = 25) // transitionSpeed : the higher, the faster the transition
        {
            return (int)(Clamp(0, Min(valueToTest - bounds.min, bounds.max - valueToTest) * transitionSpeed, 1000) * percentageFree * 0.001f);
        }
        public static int calculateAndAddBiome(List<((int biome, int subBiome), int)> biomeList, (int biome, int subBiome) biomeToTest, int percentageFree, int valueToTest, (int min, int max) bounds, int transitionSpeed = 25) // transitionSpeed : the higher, the faster the transition
        {
            int biomeness = (int)(Clamp(0, Min(valueToTest - bounds.min, bounds.max - valueToTest) * transitionSpeed, 1000) * percentageFree * 0.001f);
            if (biomeness > 0)
            {
                biomeList.Add((biomeToTest, biomeness));
            }
            return biomeness;
        }
        public static ((int biome, int subBiome), int)[] findBiome((int, int) dimensionType, int[] values)
        {
            return findBiome(dimensionType, (values[0], values[1], values[2], values[3], values[4], values[5]));
        }
        public static ((int biome, int subBiome), int)[] findBiome((int, int) dimensionType, (int temp, int humi, int acid, int toxi, int mod1, int mod2) values)
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

                    int distTot = (biomeTypicalValues[i].range - (distTemp + distHumi + distAcid + distToxi));
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

                    percentageFree -= calculateAndAddBiome(listo, (6, 0), percentageFree, Min(500 - temperature, humidity - 500, acidity - 500), (0, 999999), 5);  // add mold

                    if (humidity - Abs((int)(0.4f * (temperature - 512))) > 720)
                    {
                        percentageFree -= calculateAndAddBiome(listo, (8, 0), percentageFree, humidity - Abs((int)(0.4f * (temperature - 512))), (720, 999999)); // ocean
                    }

                    if (percentageFree <= 0) { goto AfterTest; }

                    if (temperature > 720)
                    {
                        int hotness = calculateBiome(percentageFree, temperature, (720, 999999));
                        if (temperature > 1024)
                        {
                            int lavaness = calculateAndAddBiome(listo, (2, 1), hotness, temperature - Max(0, humidity - 512), (1024, 999999));
                            percentageFree -= lavaness;
                            hotness -= lavaness;
                        }
                        if (temperature > 840 && humidity > 600)
                        {
                            int obsidianess = calculateAndAddBiome(listo, (2, 2), hotness, Min(temperature - 840, humidity - 600), (0, 999999));
                            percentageFree -= obsidianess;
                            hotness -= obsidianess;
                        }
                        percentageFree -= testAddBiome(listo, (2, 0), hotness);
                    }

                    if (temperature < 440 && percentageFree > 0)
                    {
                        int coldness = calculateBiome(percentageFree, temperature, (-999999, 440));
                        if (temperature < 0)
                        {
                            int frostness = calculateAndAddBiome(listo, (0, 1), coldness, temperature, (-999999, 0));
                            percentageFree -= frostness;
                            coldness -= frostness;
                        }

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
                    testAddBiome(listo, (9, 0), percentageFree);
                }
                else if (dimensionType == (2, 0)) // type == 2, living dimension
                {
                    percentageFree -= calculateAndAddBiome(listo, (10, 4), percentageFree, acidity, (700, 999999)); // acid ocean
                    percentageFree -= calculateAndAddBiome(listo, (10, 3), percentageFree, temperature, (-999999, 400)); // blood ocean
                    percentageFree -= calculateAndAddBiome(listo, (10, 0), percentageFree, humidity, (660, 999999)); // flesh
                    percentageFree -= calculateAndAddBiome(listo, (10, 2), percentageFree, humidity, (-999999, 340)); // bone
                    testAddBiome(listo, (10, 1), percentageFree); // flesh and bone
                }
            }

            if (listo.Count == 0) { testAddBiome(listo, (-1, 0), percentageFree); }

        AfterTest:;

            SortByItem2(listo);
            ((int biome, int subBiome), int)[] arrayo = new ((int biome, int subBiome), int)[listo.Count];
            for (int i = 0; i < arrayo.Length; i++)
            {
                arrayo[i] = listo[i];
            }
            return arrayo;
        }
        public static int[] findBiomeColor(((int biome, int subBiome), int)[] arrayo)
        {
            int[] colorArray = { 0, 0, 0 };
            float mult;
            foreach (((int biome, int subBiome), int) tupel in arrayo)
            {
                mult = tupel.Item2 * 0.001f;

                (int, int, int) tupel2 = biomeDict[tupel.Item1];
                colorArray[0] += (int)(mult * tupel2.Item1);
                colorArray[1] += (int)(mult * tupel2.Item2);
                colorArray[2] += (int)(mult * tupel2.Item3);
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
