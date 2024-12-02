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

namespace Cave
{
    public class Chunks
    {
        public class Chunk
        {
            public Screens.Screen screen;

            public long chunkSeed;

            public (int x, int y) position;

            public ((int biome, int subBiome), int)[,][] biomeIndex;

            public (int type, int subType)[,] fillStates = new (int type, int subType)[32, 32];
            public (int, int, int)[,] baseColors;
            public Bitmap bitmap;

            public List<Entity> entityList = new List<Entity>();
            public List<Plant> plantList = new List<Plant>();
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
            public Chunk((int x, int y) posToPut, bool structureGenerated, Screens.Screen screenToPut)
            {
                screen = screenToPut;
                position = posToPut;

                if (!structureGenerated) { testIfMegaChunkGenerated(); }

                bool filePresent = testLoadChunk(structureGenerated);
                long chunkX = position.x * 2;
                long chunkY = position.y * 2;

                chunkSeed = findPlantSeed(chunkX, chunkY, screen, 0);
                long bigSeed = LCGxNeg(LCGz(LCGyPos(LCGxNeg(screen.seed))));
                long bigSeed2 = LCGxNeg(LCGz(LCGyPos(LCGxPos(bigSeed))));
                long bigSeed3 = LCGxNeg(LCGz(LCGyPos(LCGxNeg(bigSeed2))));

                (int x, int y) mod;
                (int x, int y) chunkRealPos = (position.x * 32, position.y * 32);

                // biome shit generation

                int layerStart = 0;
                if (screen.isMonoBiome) { layerStart = 4; }

                chunkX = Floor(position.x, 16) / 16;
                chunkY = Floor(position.y, 16) / 16;
                int[,] primaryBiomeValues = new int[6, 4];
                for (int i = layerStart; i < 6; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        mod = squareModArray[j];
                        primaryBiomeValues[i, j] = findPrimaryBiomeValue(chunkX + mod.x, chunkY + mod.y, screen.seed, i);
                    }
                }

                chunkX = ChunkIdx(position.Item1);
                chunkY = ChunkIdx(position.Item2);
                int[,] primaryBigBiomeValues = new int[6, 4];
                for (int i = layerStart; i < 6; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        mod = squareModArray[j];
                        primaryBigBiomeValues[i, j] = findPrimaryBiomeValue(chunkX + mod.x, chunkY + mod.y, bigSeed, i);
                    }
                }

                int[,,] secondaryBiomeValues = new int[32, 32, 6];
                int[,,] secondaryBigBiomeValues = new int[32, 32, 6];
                (int temp, int humi, int acid, int toxi)[,] tileValuesArray = new (int temp, int humi, int acid, int toxi)[32, 32];
                biomeIndex = new ((int biome, int subBiome), int)[32, 32][];
                baseColors = new (int, int, int)[32, 32];
                bitmap = new Bitmap(32, 32);


                int stepo = 1;
                bool doSecondMethodOfFindingBiomeValue = false;// debugMode2;
                if (doSecondMethodOfFindingBiomeValue) { stepo = 31; }

                for (int i = 0; i < 32; i += stepo)
                {
                    for (int j = 0; j < 32; j += stepo)
                    {
                        for (int k = layerStart; k < 6; k++)
                        {
                            secondaryBiomeValues[i, j, k] = findSecondaryBiomeValue(primaryBiomeValues, chunkRealPos.x + i, chunkRealPos.y + j, k);
                            secondaryBigBiomeValues[i, j, k] = findSecondaryBigBiomeValue(primaryBigBiomeValues, chunkRealPos.x + i, chunkRealPos.y + j, k);
                        }
                        (int temp, int humi, int acid, int toxi) tileValues;
                        if (screen.isMonoBiome) { tileValues = makeTileBiomeValueArrayMonoBiome(screen.type); }
                        else { tileValues = makeTileBiomeValueArray(secondaryBigBiomeValues, secondaryBiomeValues, i, j); }
                        tileValuesArray[i, j] = tileValues;
                        if (screen.isMonoBiome) { biomeIndex[i, j] = new ((int biome, int subBiome), int)[] { (screen.type, 1000) }; }
                        else { biomeIndex[i, j] = findBiome(this.screen.type, tileValues); }

                        int[] colorArray = findBiomeColor(biomeIndex[i, j]);
                        baseColors[i, j] = (colorArray[0], colorArray[1], colorArray[2]);
                    }
                }
                if (doSecondMethodOfFindingBiomeValue)
                {
                    for (int i = 0; i < 32; i += 1)
                    {
                        for (int j = 0; j < 32; j += 1)
                        {
                            if (screen.isMonoBiome) { biomeIndex[i, j] = new ((int biome, int subBiome), int)[] { (screen.type, 1000) }; }
                            else { biomeIndex[i, j] = findBiomeByMean(biomeIndex, i, j); }

                            int[] colorArray = findBiomeColor(biomeIndex[i, j]);
                            baseColors[i, j] = (colorArray[0], colorArray[1], colorArray[2]);
                        }
                    }
                }


                int[,,] secondaryFillValues = null;
                int[,,] secondaryBigFillValues = null;
                int[,] primaryFillValues = null;
                int[,] primaryBigFillValues = null;

                // terrain noise shit generation
                if (!filePresent)
                {
                    chunkX = position.x * 2;
                    chunkY = position.y * 2;
                    primaryFillValues = new int[4, 9];
                    for (int j = 0; j < 9; j++)
                    {
                        mod = bigSquareModArray[j];
                        primaryFillValues[0, j] = findPrimaryNoiseValue(chunkX + mod.x, chunkY + mod.y, screen.seed);
                        primaryFillValues[1, j] = findPrimaryNoiseValue(chunkX + mod.x, chunkY + mod.y, bigSeed);
                        primaryFillValues[2, j] = findPrimaryNoiseValue(chunkX + mod.x, chunkY + mod.y, bigSeed2, 2048);
                        primaryFillValues[3, j] = findPrimaryNoiseValue(chunkX + mod.x, chunkY + mod.y, bigSeed3, 2048);
                    }


                    chunkX = Floor(position.x, 2) / 2;
                    chunkY = Floor(position.y, 2) / 2;
                    primaryBigFillValues = new int[2, 4];
                    for (int j = 0; j < 4; j++)
                    {
                        mod = squareModArray[j];
                        primaryBigFillValues[0, j] = findPrimaryNoiseValue(chunkX + mod.x, chunkY + mod.y, bigSeed2);
                        primaryBigFillValues[1, j] = findPrimaryNoiseValue(chunkX + mod.x, chunkY + mod.y, bigSeed3);
                    }

                    secondaryFillValues = new int[4, 32, 32];
                    secondaryBigFillValues = new int[2, 32, 32];
                    fillStates = new (int type, int subType)[32, 32];
                }

                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        if (!filePresent)
                        {
                            secondaryFillValues[0, i, j] = findSecondaryNoiseValue(primaryFillValues, chunkRealPos.x + i, chunkRealPos.y + j, 0);
                            secondaryBigFillValues[0, i, j] = findSecondaryBigNoiseValue(primaryBigFillValues, chunkRealPos.x + i, chunkRealPos.y + j, 0);
                            int value1 = secondaryBigFillValues[0, i, j] + (int)(0.25 * secondaryFillValues[0, i, j]) - 32;
                            //value1 = secondaryFillValues[0, i, j];
                            //value1 = 0;
                            secondaryFillValues[1, i, j] = findSecondaryNoiseValue(primaryFillValues, chunkRealPos.x + i, chunkRealPos.y + j, 1);
                            secondaryBigFillValues[1, i, j] = findSecondaryBigNoiseValue(primaryBigFillValues, chunkRealPos.x + i, chunkRealPos.y + j, 1);
                            int value2 = secondaryBigFillValues[1, i, j] + (int)(0.25 * secondaryFillValues[1, i, j]) - 32;
                            //value2 = secondaryBigFillValues[1, i, j];
                            //value2 = 128;
                            int temperature = secondaryBiomeValues[i, j, 0];
                            int mod1 = (int)(secondaryBiomeValues[i, j, 4] * 0.25);
                            int mod2 = (int)(secondaryBiomeValues[i, j, 5] * 0.25);

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
                                if (tupel.Item1 == (1, 0))
                                {
                                    value2modifier += -3 * mult * Max(sawBladeSeesaw(value1, 13), sawBladeSeesaw(value1, 11));
                                }
                                else if (tupel.Item1 == (3, 0) || tupel.Item1 == (9, 0))
                                {
                                    foresto += mult;
                                }
                                else if (tupel.Item1 == (4, 0))
                                {
                                    float see1 = Sin(i + mod2 * 0.3f + 0.5f, 16);
                                    float see2 = Sin(j + mod2 * 0.3f + 0.5f, 16);
                                    valueToBeAdded = mult * Min(0, 20 * (see1 + see2) - 10);
                                    value2modifier += valueToBeAdded;
                                    value1modifier += valueToBeAdded + 2;
                                }
                                else if (tupel.Item1 == (2, 2))
                                {
                                    float see1 = Obs((position.Item1 * 32) % 64 + 64 + i + mod2 * 0.15f + 0.5f, 64);
                                    float see2 = Obs((position.Item2 * 32) % 64 + 64 + j + mod2 * 0.15f + 0.5f, 64);
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
                                else if (tupel.Item1 == (8, 0) || tupel.Item1 == (2, 1) || tupel.Item1 == (10, 3) || tupel.Item1 == (10, 4))
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
                            if (((fillTest1 || fillTest2 ) && true) || ( false && plateauScore >= 0)) { fillStates[i, j] = elementToFillVoidWith; }
                            else
                            {
                                secondaryFillValues[2, i, j] = findSecondaryNoiseValue(primaryFillValues, chunkRealPos.x + i, chunkRealPos.y + j, 2);
                                secondaryFillValues[3, i, j] = findSecondaryNoiseValue(primaryFillValues, chunkRealPos.x + i, chunkRealPos.y + j, 3);
                                Dictionary<(int type, int subType), float> dicto = findTransitions(biomeIndex[i, j], tileValuesArray[i, j]);
                                fillStates[i, j] = findMaterialToFillWith((secondaryFillValues[2, i, j], secondaryFillValues[3, i, j]), biomeIndex[i, j][0].Item1, dicto);
                            }
                            //if (rand.Next(500) != 0){ fillStates[i, j] = 1; }
                        }

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


                if (!structureGenerated && !entitiesAndPlantsSpawned)
                {
                    screen.chunksToSpawnEntitiesIn[position] = true;
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
            public void testIfMegaChunkGenerated()
            {
                (int x, int y) megaChunkPos = MegaChunkIdx(position);
                if (screen.megaChunks.ContainsKey(megaChunkPos))
                {
                    return;
                }
                if (System.IO.File.Exists($"{currentDirectory}\\CaveData\\{screen.game.seed}\\ChunkData\\{screen.id}\\{megaChunkPos.Item1}.{megaChunkPos.Item2}.json"))
                {
                    return;
                }
                screen.createStructures(megaChunkPos.x, megaChunkPos.y);
            }
            public bool testLoadChunk(bool structureGenerated)
            {
                if (structureGenerated)
                {
                    if (System.IO.File.Exists($"{currentDirectory}\\CaveData\\{screen.game.seed}\\ChunkData\\{screen.id}\\{position.Item1}.{position.Item2}.json"))
                    {
                        loadChunk(this, false);
                        return true;
                    }
                }
                else if (System.IO.File.Exists($"{currentDirectory}\\CaveData\\{screen.game.seed}\\ChunkData\\{screen.id}\\{position.Item1}.{position.Item2}.json"))
                {
                    loadChunk(this, true);
                    return true;
                }
                return false;
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
                    if (fillStates[i, j] == (5, 0)) { rando = Abs((int)(LCGyNeg(LCGxPos(position.x * 32 + i)%153 + LCGyPos(position.y * 32 + j)%247) % 279)) % 40 - 20; }
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
                (int x, int y) posToModify = (i + position.x * 32, j + position.y * 32);
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
                            Plant newPlant = new Plant(this, 0, 3, returnTuple.Item1);
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
                    else if (screen.loadedChunks.TryGetValue(position, out Chunk chunkToTest))
                    {
                        if (chunkToTest.fillStates[randX, randY].type <= 0)
                        {
                            chunkPos = ChunkIdx(position.x*32 + randX, position.y*32 + randY - 1);
                            if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTesta))
                            {
                                tileIndex = PosMod((randX, randY - 1));
                                if (chunkToTesta.fillStates[tileIndex.x, tileIndex.y].type > 0)
                                {
                                    forbiddenPositions[(randX, randY)] = true;
                                    return ((position.x*32 + randX, position.y*32 + randY), true);
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
                    else if (screen.loadedChunks.TryGetValue(position, out Chunk chunkToTest))
                    {
                        tileIndex = PosMod((randX, randY));
                        if (chunkToTest.fillStates[tileIndex.x, tileIndex.y].type <= 0)
                        {
                            chunkPos = ChunkIdx(position.x*32 + randX, position.y*32 + randY + 1);
                            if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTesta))
                            {
                                tileIndex = PosMod((randX, randY + 1));
                                if (chunkToTesta.fillStates[tileIndex.x, tileIndex.y].type > 0)
                                {
                                    forbiddenPositions[(randX, randY)] = true;
                                    return ((position.x*32 + randX, position.y*32 + randY), true);
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
                    (int x, int y) chunkCoords = ChunkIdx(position.Item1 * 32 - 32, position.Item2 * 32);
                    Chunk leftChunk = screen.tryToGetChunk(chunkCoords);
                    chunkCoords = ChunkIdx(position.Item1 * 32 - 32, position.Item2 * 32 - 32);
                    Chunk bottomLeftChunk = screen.tryToGetChunk(chunkCoords);
                    chunkCoords = ChunkIdx(position.Item1 * 32, position.Item2 * 32 - 32);
                    Chunk bottomChunk = screen.tryToGetChunk(chunkCoords);
                    chunkCoords = ChunkIdx(position.Item1 * 32 + 32, position.Item2 * 32 - 32);
                    Chunk bottomRightChunk = screen.tryToGetChunk(chunkCoords);
                    chunkCoords = ChunkIdx(position.Item1 * 32 + 32, position.Item2 * 32);
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
                    if ((i < 15 || middleTestPositionChunk.position.Item1 < rightTestPositionChunk.position.Item1) && (rightTestPositionChunk.fillStates[ir, j].type == 0 || middleTestPositionChunk.fillStates[i, jb].type < 0) && rightDiagTestPositionChunk.fillStates[ir, jb].type == 0)
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
                    if ((i > 0 || leftTestPositionChunk.position.Item1 < middleTestPositionChunk.position.Item1) && (leftTestPositionChunk.fillStates[il, j].type == 0 || middleTestPositionChunk.fillStates[i, jb].type < 0) && leftDiagTestPositionChunk.fillStates[il, jb].type == 0)
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

                int absChunkX = position.Item1;
                int absChunkY = position.Item2;
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

                int absChunkX = position.Item1;
                int absChunkY = position.Item2;
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
            return (seedZ + seedX + seedY) / 3;
            //return ((int)(seedX%512)-256, (int)(seedY%512)-256);
        }
        public static int findPrimaryNoiseValue(long posX, long posY, long seed, int maxValue = 256)
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
            return (int)((seedZ + seedX + seedY) % maxValue);
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
        public static int findSecondaryNoiseValue(int[,] values, int posX, int posY, int layer) // posX/Y is real pos and not %32 !!! same for all findSecondary functions
        {
            int modulo = 16;
            int modX = PosMod(posX, modulo);
            int modY = PosMod(posY, modulo);

            int quartile = 0;
            if (PosMod(posX) >= 16) { quartile += 1; }
            if (PosMod(posY) >= 16) { quartile += 3; }

            int fX1 = values[layer, 0 + quartile] * (modulo - modX) + values[layer, 1 + quartile] * modX;
            int fX2 = values[layer, 3 + quartile] * (modulo - modX) + values[layer, 4 + quartile] * modX; // ITS NORMAL THAT ITS modX DONT CHANGEEEEEEEEeeeeeeeee (istg it's true)
            int fY = fX1 * (modulo - modY) + fX2 * modY;
            return fY / (modulo * modulo);
        }
        public static int findSecondaryBigNoiseValue(int[,] values, int posX, int posY, int layer)
        {
            int modulo = 64;
            int modX = PosMod(posX, modulo);
            int modY = PosMod(posY, modulo);
            int fX1 = values[layer, 0] * (modulo - modX) + values[layer, 1] * modX;
            int fX2 = values[layer, 2] * (modulo - modX) + values[layer, 3] * modX; // ITS NORMAL THAT ITS modX DONT CHANGEEEEEEEEeeeeeeeee (istg it's true)
            int fY = fX1 * (modulo - modY) + fX2 * modY;
            return fY / (modulo * modulo);
        }
        public static int findSecondaryBiomeValue(int[,] values, int posX, int posY, int layer)
        {
            int modulo = 512;
            int modX = PosMod(posX, modulo);
            int modY = PosMod(posY, modulo);
            int fX1 = values[layer, 0] * (modulo - modX) + values[layer, 1] * modX;
            int fX2 = values[layer, 2] * (modulo - modX) + values[layer, 3] * modX; // ITS NORMAL THAT ITS modX DONT CHANGEEEEEEEEeeeeeeeee (istg it's true)
            int fY = fX1 * (modulo - modY) + fX2 * modY;
            return fY / (modulo * modulo);
        }
        public static int findSecondaryBigBiomeValue(int[,] values, int posX, int posY, int layer)
        {
            int modulo = 1024;
            int modX = PosMod(posX, modulo);
            int modY = PosMod(posY, modulo);
            int fX1 = values[layer, 0] * (modulo - modX) + values[layer, 1] * modX;
            int fX2 = values[layer, 2] * (modulo - modX) + values[layer, 3] * modX; // ITS NORMAL THAT ITS modX DONT CHANGEEEEEEEEeeeeeeeee (istg it's true)
            int fY = fX1 * (modulo - modY) + fX2 * modY;
            return fY / (modulo * modulo);
        }
        public static ((int biome, int subBiome), int)[] findBiomeByMean(((int biome, int subBiome), int)[,][] biomeIndex, int posX, int posY) // posX/Y is %32 and not real pos
        {
            float step = 0.03226f; // 1/31
            float pondX2 = posX * step;
            float pondX = 1 - pondX2;
            float pondY2 = posY * step;
            float pondY = 1 - pondY2;
            Dictionary<(int x, int y), float> ponderation = new Dictionary<(int x, int y), float>
            {
                {(0, 0), pondX*pondY},
                {(1, 0), pondX2*pondY},
                {(0, 1), pondX*pondY2},
                {(1, 1), pondX2*pondY2}
            };
            Dictionary<(int biome, int subBiome), int> dicto = new Dictionary<(int biome, int subBiome), int>();

            foreach ((int x, int y) pos in squareModArray)
            {
                foreach (((int biome, int subBiome), int) biome in biomeIndex[pos.x * 31, pos.y * 31])
                {
                    if (!dicto.ContainsKey(biome.Item1)) { dicto[biome.Item1] = (int)(biome.Item2 * ponderation[pos]); }
                    else { dicto[biome.Item1] += (int)(biome.Item2 * ponderation[pos]); }
                }
            }

            List<((int biome, int subBiome), int)> listo = new List<((int biome, int subBiome), int)>();
            foreach (KeyValuePair<(int biome, int subBiome), int> pair in dicto) { listo.Add((pair.Key, pair.Value)); }
            SortByItem2(listo);
            ((int biome, int subBiome), int)[] arrayo = new ((int biome, int subBiome), int)[listo.Count];
            for (int i = 0; i < arrayo.Length; i++)
            {
                arrayo[i] = listo[i];
            }
            return arrayo;
        }
        public static Dictionary<(int type, int subType), float> findTransitions(((int biome, int subBiome), int)[] biomeArray, (int temp, int humi, int acid, int toxi) values)
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
        public static float findTransition((int type, int subType) biome, (int temp, int humi, int acid, int toxi) values)
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
        public static (int temp, int humi, int acid, int toxi) makeTileBiomeValueArrayMonoBiome((int type, int subType) biome)
        {
            int temperature = biomeTypicalValues[biome].temp;
            int humidity = biomeTypicalValues[biome].humi;
            int acidity = biomeTypicalValues[biome].acid;
            int toxicity = biomeTypicalValues[biome].toxi;
            return (temperature, humidity, acidity, toxicity);
        }
        public static (int temp, int humi, int acid, int toxi) makeTileBiomeValueArray(int[,,] bigValues, int[,,] values, int posX, int posY)
        {
            int temperature = bigValues[posX, posY, 0] + values[posX, posY, 0] - 512;
            int humidity = bigValues[posX, posY, 1] + values[posX, posY, 1] - 512;
            int acidity = bigValues[posX, posY, 2] + values[posX, posY, 2] - 512;
            int toxicity = bigValues[posX, posY, 3] + values[posX, posY, 3] - 512;
            return (temperature, humidity, acidity, toxicity);
        }
        public static (int temp, int humi, int acid, int toxi) makeTileBiomeValueArray(int[] values, int posX, int posY)
        {
            int temperature = values[0];
            int humidity = values[1];
            int acidity = values[2];
            int toxicity = values[3];
            return (temperature, humidity, acidity, toxicity);
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
            return findBiome(dimensionType, (values[0], values[1], values[2], values[3]));
        }
        public static ((int biome, int subBiome), int)[] findBiome((int, int) dimensionType, (int temp, int humi, int acid, int toxi) values)
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

            if (0 > 0) // distance shit that's slow asf and bad asf
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

            } // distance shit that's slow asf and bad asf
            else if (dimensionType == (0, 0)) // type == 1, normal dimension
            {
                listo = new List<((int biome, int subBiome), int)>();

                percentageFree -= calculateAndAddBiome(listo, (6, 0), percentageFree, Min(500 - temperature, humidity - 500, acidity - 500), (0, 999999), 5);  // add mold

                if (humidity - Abs((int)(0.4f*(temperature - 512))) > 720)
                {
                    percentageFree -= calculateAndAddBiome(listo, (8, 0), percentageFree, humidity - Abs((int)(0.4f*(temperature - 512))), (720, 999999)); // ocean
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
                    percentageFree -= calculateAndAddBiome(listo, (3, 1), percentageFree, (500 - toxicity) + (int)(0.4f*(humidity - temperature)), (0, 999999));  // add flower forest
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
