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
    public class Chunks
    {
        public class Chunk
        {
            public Screens.Screen screen;

            public long chunkSeed;

            public (int x, int y) position;

            public int[,] primaryFillValues;
            public int[,] primaryBigFillValues;
            public int[,] primaryBiomeValues;
            public int[,] primaryBigBiomeValues; // the biome trends that are bigger than biomes
            public int[,,] secondaryFillValues;
            public int[,,] secondaryBigFillValues;
            public int[,,] secondaryBiomeValues;
            public int[,,] secondaryBigBiomeValues;
            public (int, int)[,][] biomeIndex;

            public int[,] fillStates = new int[32, 32];
            public (int, int, int)[,] baseColors;
            public Color[,] colors;
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

                bool filePresent = testLoadChunk(structureGenerated);
                long chunkX = position.x * 2;
                long chunkY = position.y * 2;

                chunkSeed = findPlantSeed(chunkX, chunkY, screen, 0);
                long bigSeed = LCGxNeg(LCGz(LCGyPos(LCGxNeg(screen.seed))));
                long bigSeed2 = LCGxNeg(LCGz(LCGyPos(LCGxPos(bigSeed))));
                long bigSeed3 = LCGxNeg(LCGz(LCGyPos(LCGxNeg(bigSeed2))));

                (int x, int y) mod;


                // biome shit generation

                int layerStart = 0;
                if (screen.isMonoBiome) { layerStart = 4; }

                chunkX = Floor(position.x, 16) / 16;
                chunkY = Floor(position.y, 16) / 16;
                primaryBiomeValues = new int[6,4];
                for (int i = layerStart; i < 6; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        mod = squareModArray[j];
                        primaryBiomeValues[i, j] = findPrimaryBiomeValue(chunkX + mod.x, chunkY + mod.y, screen.seed, i);
                    }
                }

                chunkX = Floor(position.Item1, 32) / 32;
                chunkY = Floor(position.Item2, 32) / 32;
                primaryBigBiomeValues = new int[6, 4];
                for (int i = layerStart; i < 6; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        mod = squareModArray[j];
                        primaryBigBiomeValues[i, j] = findPrimaryBiomeValue(chunkX + mod.x, chunkY + mod.y, bigSeed, i);
                    }
                }

                secondaryBiomeValues = new int[32, 32, 6];
                secondaryBigBiomeValues = new int[32, 32, 6];
                biomeIndex = new (int, int)[32, 32][];
                baseColors = new (int, int, int)[32, 32];
                colors = new Color[32, 32];
                bitmap = new Bitmap(32, 32);

                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        for (int k = layerStart; k < 6; k++)
                        {
                            secondaryBiomeValues[i, j, k] = findSecondaryBiomeValue(this, i, j, k);
                            secondaryBigBiomeValues[i, j, k] = findSecondaryBigBiomeValue(this, i, j, k);
                        }
                        if (screen.isMonoBiome) { biomeIndex[i, j] = new (int, int)[] { (screen.type, 1000) }; }
                        else { biomeIndex[i, j] = findBiome(this, i, j); }

                        int[] colorArray = findBiomeColor(biomeIndex[i,j]);
                        baseColors[i, j] = (colorArray[0], colorArray[1], colorArray[2]);
                    }
                }




                // terrain noise shit generation
                if (!filePresent)
                {
                    chunkX = position.x * 2;
                    chunkY = position.y * 2;
                    primaryFillValues = new int[2, 9];
                    for (int j = 0; j < 9; j++)
                    {
                        mod = bigSquareModArray[j];
                        primaryFillValues[0, j] = findPrimaryNoiseValue(chunkX + mod.x, chunkY + mod.y, screen.seed);
                        primaryFillValues[1, j] = findPrimaryNoiseValue(chunkX + mod.x, chunkY + mod.y, bigSeed);
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

                    secondaryFillValues = new int[2, 32, 32];
                    secondaryBigFillValues = new int[2, 32, 32];
                    fillStates = new int[32, 32];
                }

                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        if (!filePresent)
                        {
                            secondaryFillValues[0, i, j] = findSecondaryNoiseValue(this, i, j, 0);
                            secondaryBigFillValues[0, i, j] = findSecondaryBigNoiseValue(this, i, j, 0);
                            int value1 = secondaryBigFillValues[0, i, j] + (int)(0.25 * secondaryFillValues[0, i, j]) - 32;
                            //value1 = secondaryFillValues[0, i, j];
                            //value1 = 0;
                            secondaryFillValues[1, i, j] = findSecondaryNoiseValue(this, i, j, 1);
                            secondaryBigFillValues[1, i, j] = findSecondaryBigNoiseValue(this, i, j, 1);
                            int value2 = secondaryBigFillValues[1, i, j] + (int)(0.25 * secondaryFillValues[1, i, j]) - 32;
                            //value2 = secondaryBigFillValues[1, i, j];
                            //value2 = 128;
                            int temperature = secondaryBiomeValues[i, j, 0];
                            int mod1 = (int)(secondaryBiomeValues[i, j, 4] * 0.25);
                            int mod2 = (int)(secondaryBiomeValues[i, j, 5] * 0.25);

                            float valueToBeAdded;
                            float value1modifier = 0;
                            float value2PREmodifier;
                            float value2modifier = 0;
                            float mod2divider = 1;
                            float foresto = 1;
                            float oceano = 0;
                            float oceanoSeeSaw = 0;

                            float mult;
                            foreach ((int, int) tupel in biomeIndex[i, j])
                            {
                                mult = tupel.Item2 * 0.001f;
                                if (tupel.Item1 == 1)
                                {
                                    value2modifier += -3 * mult * Max(sawBladeSeesaw(value1, 13), sawBladeSeesaw(value1, 11));
                                }
                                else if (tupel.Item1 == 3 || tupel.Item1 == 9)
                                {
                                    foresto += mult;
                                }
                                else if (tupel.Item1 == 4)
                                {
                                    float see1 = Sin(i + mod2 * 0.3f + 0.5f, 16);
                                    float see2 = Sin(j + mod2 * 0.3f + 0.5f, 16);
                                    valueToBeAdded = mult * Min(0, 20 * (see1 + see2) - 10);
                                    value2modifier += valueToBeAdded;
                                    value1modifier += valueToBeAdded + 2;
                                }
                                else if (tupel.Item1 == 6)
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
                                else if (tupel.Item1 == 8)
                                {
                                    oceano = mult * 10;
                                    oceanoSeeSaw = Min(Seesaw((int)oceano, 8), 8 - oceano);
                                    if (oceanoSeeSaw < 0)
                                    {
                                        oceanoSeeSaw = oceanoSeeSaw * oceanoSeeSaw * oceanoSeeSaw;
                                    }
                                    else { oceanoSeeSaw = oceanoSeeSaw * Abs(oceanoSeeSaw); }
                                    oceano *= 10;
                                    oceanoSeeSaw *= 10;
                                }
                                else { value2modifier += mult * ((2 * value1) % 32); }
                            }

                            mod2 = (int)(mod2 / mod2divider);

                            int elementToFillVoidWith;
                            if (biomeIndex[i, j][0].Item1 == 8) { elementToFillVoidWith = -2; }
                            else { elementToFillVoidWith = 0; }

                            bool fillTest1 = value1 > 122 - mod2 * mod2 * foresto * 0.0003f + value1modifier + (int)(oceanoSeeSaw * 0.1f) && value1 < 133 + mod2 * mod2 * foresto * 0.0003f - value1modifier - oceanoSeeSaw;
                            bool fillTest2 = value2 > 200 + value2modifier + oceano || value2 < (foresto - 1) * 75f - oceano;
                            //if (fillTest1 && fillTest2) { fillStates[i, j] = 4; }
                            //else if (fillTest1) { fillStates[i, j] = 3; }
                            //else if (fillTest2) { fillStates[i, j] = 2; }
                            if (fillTest1 || fillTest2) { fillStates[i, j] = elementToFillVoidWith; }
                            else { fillStates[i, j] = 1; }
                            //fillStates[i, j] = 1;
                        }

                        int darkness = 0;
                        foreach ((int, int) tupel in biomeIndex[i, j])
                        {
                            if (darkBiomes.ContainsKey(tupel.Item1))
                            {
                                darkness += (int)(tupel.Item2*0.3f);
                            }
                        }
                        Color colorToDraw = Color.FromArgb(Max(0, 255-darkness), 255, 255, 255);
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
                if (fillStates[i, j] == 0)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        colorArray[k] += 70;
                    };
                }
                else if (fillStates[i, j] == 2)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        colorArray[k] = (int)(colorArray[k] * 0.5f) + 120;
                    };
                }
                else if (fillStates[i, j] == 3)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        colorArray[k] = (int)(colorArray[k] * 0.5f) + 120;
                    };
                    colorArray[0] += 50;
                    colorArray[1] += 50;
                }
                else if (fillStates[i, j] == 4)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        colorArray[k] = (int)(colorArray[k] * 0.5f) + 120;
                    };
                    colorArray[0] += 140;
                    colorArray[1] += 60;
                    colorArray[1] += 30;
                }
                else if (fillStates[i, j] == -1)
                {
                    colorArray[0] = (int)(colorArray[0] * 0.8f) + 100;
                    colorArray[1] = (int)(colorArray[1] * 0.8f) + 100;
                    colorArray[2] = (int)(colorArray[2] * 0.8f) + 60;
                }
                else if (fillStates[i, j] == -2)
                {
                    colorArray[0] = (int)(colorArray[0] * 0.8f) + 60;
                    colorArray[1] = (int)(colorArray[1] * 0.8f) + 60;
                    colorArray[2] = (int)(colorArray[2] * 0.8f) + 100;
                }
                else if (fillStates[i, j] == -3)
                {
                    colorArray[0] = (int)(colorArray[0] * 0.8f) + 85;
                    colorArray[1] = (int)(colorArray[1] * 0.8f) + 60;
                    colorArray[2] = (int)(colorArray[2] * 0.8f) + 100;
                }
                else if (fillStates[i, j] == -4)
                {
                    colorArray[0] = (int)(colorArray[0] * 0.8f) + 145;
                    colorArray[1] = (int)(colorArray[1] * 0.8f) + 30;
                    colorArray[2] = (int)(colorArray[2] * 0.8f) + 60;
                }
                else if (fillStates[i, j] == -5)
                {
                    colorArray[0] = (int)(colorArray[0] * 0.8f) + 140;
                    colorArray[1] = (int)(colorArray[1] * 0.8f) + 120;
                    colorArray[2] = (int)(colorArray[2] * 0.8f) + 70;
                }
                colors[i, j] = Color.FromArgb(ColorClamp(colorArray[0]), ColorClamp(colorArray[1]), ColorClamp(colorArray[2]));
                setPixelButFaster(bitmap, (i, j), colors[i, j]);
            }
            public void spawnEntities()
            {
                if (spawnPlantsAndEntities)
                {
                    Entity newEntity = new Entity(this);
                    if (!newEntity.isDeadAndShouldDisappear)
                    {
                        screen.activeEntities[newEntity.id] = newEntity;
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        Plant newPlanto = new Plant(this, 0);
                        if (!newPlanto.isDeadAndShouldDisappear)
                        {
                            screen.activePlants[newPlanto.id] = newPlanto;
                        }
                    }
                    Plant newPlant = new Plant(this, 1);
                    if (!newPlant.isDeadAndShouldDisappear)
                    {
                        screen.activePlants[newPlant.id] = newPlant;
                    }
                    entitiesAndPlantsSpawned = true;
                }
            }
            public void moveLiquids()
            {
                if (unstableLiquidCount > 0) //here
                {
                    (int x, int y) chunkCoords = screen.findChunkAbsoluteIndex(position.Item1 * 32 - 32, position.Item2 * 32);
                    Chunk leftChunk = screen.tryToGetChunk(chunkCoords);
                    chunkCoords = screen.findChunkAbsoluteIndex(position.Item1 * 32 - 32, position.Item2 * 32 - 32);
                    Chunk bottomLeftChunk = screen.tryToGetChunk(chunkCoords);
                    chunkCoords = screen.findChunkAbsoluteIndex(position.Item1 * 32, position.Item2 * 32 - 32);
                    Chunk bottomChunk = screen.tryToGetChunk(chunkCoords);
                    chunkCoords = screen.findChunkAbsoluteIndex(position.Item1 * 32 + 32, position.Item2 * 32 - 32);
                    Chunk bottomRightChunk = screen.tryToGetChunk(chunkCoords);
                    chunkCoords = screen.findChunkAbsoluteIndex(position.Item1 * 32 + 32, position.Item2 * 32);
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

                if (fillStates[i, j] < 0)
                {
                    if (middleTestPositionChunk.fillStates[i, jb] == 0)
                    {
                        middleTestPositionChunk.fillStates[i, jb] = fillStates[i, j];
                        fillStates[i, j] = 0;
                        findTileColor(i, j);
                        middleTestPositionChunk.findTileColor(i, jb);
                        testLiquidUnstableLiquid(middleTestPositionChunk.position.Item1 * 32 + i, middleTestPositionChunk.position.Item1 * 32 + jb);
                        testLiquidUnstableAir(position.Item1 * 32 + i, position.Item1 * 32 + j);
                        unstableLiquidCount++;
                        return true;
                    } // THIS ONE WAS FUCKING BUGGYYYYY BRUH
                    if ((i < 15 || middleTestPositionChunk.position.Item1 < rightTestPositionChunk.position.Item1) && (rightTestPositionChunk.fillStates[ir, j] == 0 || middleTestPositionChunk.fillStates[i, jb] < 0) && rightDiagTestPositionChunk.fillStates[ir, jb] == 0)
                    {
                        rightDiagTestPositionChunk.fillStates[ir, jb] = fillStates[i, j];
                        fillStates[i, j] = 0;
                        findTileColor(i, j);
                        rightDiagTestPositionChunk.findTileColor(ir, jb);
                        testLiquidUnstableLiquid(rightDiagTestPositionChunk.position.Item1 * 32 + ir, rightDiagTestPositionChunk.position.Item1 * 32 + jb);
                        testLiquidUnstableAir(position.Item1 * 32 + i, position.Item1 * 32 + j);
                        unstableLiquidCount++;
                        return true;
                    } //this ONE WAS BUGGY
                    if ((rightTestPositionChunk.fillStates[ir, j] == 0 || middleTestPositionChunk.fillStates[i, jb] < 0) && rightDiagTestPositionChunk.fillStates[ir, jb] < 0)
                    {
                        if (testLiquidPushRight(i, j))
                        {
                            return true;
                        }
                    }
                    if ((i > 0 || leftTestPositionChunk.position.Item1 < middleTestPositionChunk.position.Item1) && (leftTestPositionChunk.fillStates[il, j] == 0 || middleTestPositionChunk.fillStates[i, jb] < 0) && leftDiagTestPositionChunk.fillStates[il, jb] == 0)
                    {
                        leftDiagTestPositionChunk.fillStates[il, jb] = fillStates[i, j];
                        fillStates[i, j] = 0;
                        findTileColor(i, j);
                        leftDiagTestPositionChunk.findTileColor(il, jb);
                        testLiquidUnstableLiquid(leftDiagTestPositionChunk.position.Item1 * 32 + il, leftDiagTestPositionChunk.position.Item1 * 32 + jb);
                        testLiquidUnstableAir(position.Item1 * 32 + i, position.Item1 * 32 + j);
                        return true;
                    } // THIS ONE WAS ALSO BUGGY
                    if ((leftTestPositionChunk.fillStates[il, j] == 0 || middleTestPositionChunk.fillStates[i, jb] < 0) && leftDiagTestPositionChunk.fillStates[il, jb] < 0)
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
                (int, int) chunkCoords = screen.findChunkAbsoluteIndex(absChunkX * 32, absChunkY * 32);
                Chunk chunkToTest = screen.tryToGetChunk(chunkCoords);

                int repeatCounter = 0;
                while (repeatCounter < 500)
                {
                    iTested++;
                    if (iTested > 31)
                    {
                        absChunkX++;
                        chunkCoords = screen.findChunkAbsoluteIndex(absChunkX * 32, absChunkY * 32);
                        chunkToTest = screen.tryToGetChunk(chunkCoords);
                        iTested -= 32;
                    }
                    if (absChunkX >= screen.chunkX + screen.chunkResolution)
                    {
                        return false;
                    }
                    if (chunkToTest.fillStates[iTested, jTested] > 0)
                    {
                        return false;
                    }
                    if (chunkToTest.fillStates[iTested, jTested] == 0)
                    {
                        chunkToTest.fillStates[iTested, jTested] = fillStates[i, j];
                        fillStates[i, j] = 0;
                        findTileColor(i, j);
                        chunkToTest.findTileColor(iTested, jTested);
                        chunkToTest.testLiquidUnstableLiquid(chunkToTest.position.Item1 * 32 + iTested, chunkToTest.position.Item1 * 32 + jTested);
                        testLiquidUnstableAir(position.Item1 * 32 + iTested, position.Item1 * 32 + jTested);
                        return true;
                    }
                    repeatCounter++;
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
                (int, int) chunkCoords = screen.findChunkAbsoluteIndex(absChunkX * 32, absChunkY * 32);
                Chunk chunkToTest = screen.tryToGetChunk(chunkCoords);

                int repeatCounter = 0;
                while (repeatCounter < 500)
                {
                    iTested--;
                    if (iTested < 0)
                    {
                        absChunkX--;
                        chunkCoords = screen.findChunkAbsoluteIndex(absChunkX * 32, absChunkY * 32);
                        chunkToTest = screen.tryToGetChunk(chunkCoords);
                        iTested += 32;
                    }
                    if (absChunkX < screen.chunkX)
                    {
                        return false;
                    }
                    if (chunkToTest.fillStates[iTested, jTested] > 0)
                    {
                        return false;
                    }
                    if (chunkToTest.fillStates[iTested, jTested] == 0)
                    {
                        chunkToTest.fillStates[iTested, jTested] = fillStates[i, j];
                        fillStates[i, j] = 0;
                        findTileColor(i, j);
                        chunkToTest.findTileColor(iTested, jTested);
                        chunkToTest.testLiquidUnstableLiquid(chunkToTest.position.Item1 * 32 + iTested, chunkToTest.position.Item1 * 32 + jTested);
                        testLiquidUnstableAir(position.Item1 * 32 + iTested, position.Item1 * 32 + jTested);
                        return true;
                    }
                    repeatCounter++;
                }
                return false;
            }
            public void testLiquidUnstableAir(int posX, int posY)
            {
                (int, int) chunkPos;
                Chunk chunkToTest;

                chunkPos = screen.findChunkAbsoluteIndex(posX + 1, posY);
                if (chunkPos.Item1 >= screen.chunkX && chunkPos.Item1 < screen.chunkX + screen.chunkResolution && chunkPos.Item2 >= screen.chunkY && chunkPos.Item2 < screen.chunkY + screen.chunkResolution)
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[((posX + 1) % 32 + 32) % 32, ((posY) % 32 + 32) % 32] < 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }

                chunkPos = screen.findChunkAbsoluteIndex(posX - 1, posY + 1);
                if (chunkPos.Item1 >= screen.chunkX && chunkPos.Item1 < screen.chunkX + screen.chunkResolution && chunkPos.Item2 >= screen.chunkY && chunkPos.Item2 < screen.chunkY + screen.chunkResolution)
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[((posX - 1) % 32 + 32) % 32, ((posY + 1) % 32 + 32) % 32] < 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }

                chunkPos = screen.findChunkAbsoluteIndex(posX + 1, posY + 1);
                if (chunkPos.Item1 >= screen.chunkX && chunkPos.Item1 < screen.chunkX + screen.chunkResolution && chunkPos.Item2 >= screen.chunkY && chunkPos.Item2 < screen.chunkY + screen.chunkResolution)
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[((posX + 1) % 32 + 32) % 32, ((posY + 1) % 32 + 32) % 32] < 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }

                chunkPos = screen.findChunkAbsoluteIndex(posX - 1, posY);
                if (chunkPos.Item1 >= screen.chunkX && chunkPos.Item1 < screen.chunkX + screen.chunkResolution && chunkPos.Item2 >= screen.chunkY && chunkPos.Item2 < screen.chunkY + screen.chunkResolution)
                {
                    ;
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[((posX - 1) % 32 + 32) % 32, (posY % 32 + 32) % 32] < 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }

                chunkPos = screen.findChunkAbsoluteIndex(posX, posY + 1);
                if (chunkPos.Item1 >= screen.chunkX && chunkPos.Item1 < screen.chunkX + screen.chunkResolution && chunkPos.Item2 >= screen.chunkY && chunkPos.Item2 < screen.chunkY + screen.chunkResolution)
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[((posX) % 32 + 32) % 32, ((posY + 1) % 32 + 32) % 32] < 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }

                chunkPos = screen.findChunkAbsoluteIndex(posX, posY - 1);
                if (chunkPos.Item1 >= screen.chunkX && chunkPos.Item1 < screen.chunkX + screen.chunkResolution && chunkPos.Item2 >= screen.chunkY && chunkPos.Item2 < screen.chunkY + screen.chunkResolution)
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[((posX) % 32 + 32) % 32, ((posY - 1) % 32 + 32) % 32] < 0) // CHANGE THIS TOO FUCKER
                    {
                        chunkToTest.unstableLiquidCount++;
                    }
                }
            }
            public void testLiquidUnstableLiquid(int posX, int posY)
            {
                (int, int) chunkPos;
                Chunk chunkToTest;

                chunkPos = screen.findChunkAbsoluteIndex(posX + 1, posY);
                if (chunkPos.Item1 >= screen.chunkX && chunkPos.Item1 < screen.chunkX + screen.chunkResolution && chunkPos.Item2 >= screen.chunkY && chunkPos.Item2 < screen.chunkY + screen.chunkResolution)
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[((posX + 1) % 32 + 32) % 32, ((posY) % 32 + 32) % 32] <= 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }

                chunkPos = screen.findChunkAbsoluteIndex(posX - 1, posY - 1);
                if (chunkPos.Item1 >= screen.chunkX && chunkPos.Item1 < screen.chunkX + screen.chunkResolution && chunkPos.Item2 >= screen.chunkY && chunkPos.Item2 < screen.chunkY + screen.chunkResolution)
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[((posX - 1) % 32 + 32) % 32, ((posY - 1) % 32 + 32) % 32] <= 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }

                chunkPos = screen.findChunkAbsoluteIndex(posX + 1, posY - 1);
                if (chunkPos.Item1 >= screen.chunkX && chunkPos.Item1 < screen.chunkX + screen.chunkResolution && chunkPos.Item2 >= screen.chunkY && chunkPos.Item2 < screen.chunkY + screen.chunkResolution)
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[((posX + 1) % 32 + 32) % 32, ((posY - 1) % 32 + 32) % 32] <= 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }

                chunkPos = screen.findChunkAbsoluteIndex(posX - 1, posY);
                if (chunkPos.Item1 >= screen.chunkX && chunkPos.Item1 < screen.chunkX + screen.chunkResolution && chunkPos.Item2 >= screen.chunkY && chunkPos.Item2 < screen.chunkY + screen.chunkResolution)
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[((posX - 1) % 32 + 32) % 32, (posY % 32 + 32) % 32] <= 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }

                chunkPos = screen.findChunkAbsoluteIndex(posX, posY + 1);
                if (chunkPos.Item1 >= screen.chunkX && chunkPos.Item1 < screen.chunkX + screen.chunkResolution && chunkPos.Item2 >= screen.chunkY && chunkPos.Item2 < screen.chunkY + screen.chunkResolution)
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[((posX) % 32 + 32) % 32, ((posY + 1) % 32 + 32) % 32] <= 0)
                    {
                        chunkToTest.unstableLiquidCount++;
                        unstableLiquidCount++;
                    }
                }

                chunkPos = screen.findChunkAbsoluteIndex(posX, posY - 1);
                if (chunkPos.Item1 >= screen.chunkX && chunkPos.Item1 < screen.chunkX + screen.chunkResolution && chunkPos.Item2 >= screen.chunkY && chunkPos.Item2 < screen.chunkY + screen.chunkResolution)
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[((posX) % 32 + 32) % 32, ((posY - 1) % 32 + 32) % 32] <= 0)
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
                    theFilledChunk.fillStates[i, j] = 1;
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
        public static int findPrimaryNoiseValue(long posX, long posY, long seed)
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
            int[,] values = chunk.primaryFillValues;

            int quartile = 0;
            if (varX >= 16) { quartile += 1; }
            if (varY >= 16) { quartile += 3; }

            int fX1 = values[layer, 0 + quartile] * (modulo - modX) + values[layer, 1 + quartile] * modX;
            int fX2 = values[layer, 3 + quartile] * (modulo - modX) + values[layer, 4 + quartile] * modX; // ITS NORMAL THAT ITS modX DONT CHANGEEEEEEEEeeeeeeeee (istg it's true)
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
            int fX2 = values[layer, 2] * (modulo - modX) + values[layer, 3] * modX; // ITS NORMAL THAT ITS modX DONT CHANGEEEEEEEEeeeeeeeee (istg it's true)
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
            int fX2 = values[layer, 2] * (modulo - modX) + values[layer, 3] * modX; // ITS NORMAL THAT ITS modX DONT CHANGEEEEEEEEeeeeeeeee (istg it's true)
            int fY = fX1 * (modulo - modY) + fX2 * modY;
            return fY / (modulo * modulo);
        }
        public static int findSecondaryBigBiomeValue(Chunk chunk, int varX, int varY, int layer)
        {
            int modulo = 1024;
            int modX = GetChunkIndexFromTile1D(chunk.position.Item1 * 32 + varX, modulo);
            int modY = GetChunkIndexFromTile1D(chunk.position.Item2 * 32 + varY, modulo); // ITS NORMAL THAT ITS modX DONT CHANGEEEEEEEEeeeeeeeee (istg it's true)
            int[,] values = chunk.primaryBigBiomeValues;
            int fX1 = values[layer, 0] * (modulo - modX) + values[layer, 1] * modX;
            int fX2 = values[layer, 2] * (modulo - modX) + values[layer, 3] * modX;
            int fY = fX1 * (modulo - modY) + fX2 * modY;
            return fY / (modulo * modulo);
        }
        public static (int, int)[] findBiome(Chunk chunk, int posX, int posY)
        {
            int temperature = chunk.secondaryBiomeValues[posX, posY, 0] + chunk.secondaryBigBiomeValues[posX, posY, 0] - 512;
            int humidity = chunk.secondaryBiomeValues[posX, posY, 1] + chunk.secondaryBigBiomeValues[posX, posY, 1] - 512;
            int acidity = chunk.secondaryBiomeValues[posX, posY, 2] + chunk.secondaryBigBiomeValues[posX, posY, 2] - 512;
            int toxicity = chunk.secondaryBiomeValues[posX, posY, 3] + chunk.secondaryBigBiomeValues[posX, posY, 3] - 512;
            int[] arrayo = new int[4] { temperature, humidity, acidity, toxicity };
            return (findBiome(arrayo));
        }
        public static (int, int)[] findBiome(int[] values)
        {
            //return new (int, int)[]{ (8, 1000) }; // use this to force a biome for debug (infite biome)


            // arrite so... 0 is temperature, 1 is humidity, 2 is acidity, 3 is toxicity, 4 is terrain modifier1, 5 is terrain modifier 2
            List<(int, int)> listo = new List<(int, int)>();
            int percentageFree = 1000;
            (int, int) current;
            int currentInt;

            int temperature = values[0];
            int humidity = values[1];
            int acidity = values[2];
            int toxicity = values[3];

            if (false)
            {
                int cumScore = 0; // heehee !
                foreach (int i in biomeTypicalValues.Keys)
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
                    listo.Add((-1, 1000));
                }

            }
            else if (true) // type == 1
            {
                listo = new List<(int, int)>();
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
            }
            else if (true)
            {
                if (humidity > 512)
                {
                    int oceanness = Min((humidity - 720) * 25, 1000);
                    if (oceanness > 0)
                    {
                        listo.Add((9, oceanness));
                        percentageFree -= oceanness;
                    }
                }
                if (percentageFree > 0)
                {
                    listo.Add((0, percentageFree));
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
        public static int[] findBiomeColor((int, int)[] arrayo)
        {
            int[] colorArray = { 0, 0, 0 };
            float mult;
            foreach ((int, int) tupel in arrayo)
            {
                mult = tupel.Item2 * 0.001f;

                (int, int, int) tupel2 = biomeDict[tupel.Item1];
                colorArray[0] += (int)(mult * tupel2.Item1);
                colorArray[1] += (int)(mult * tupel2.Item2);
                colorArray[2] += (int)(mult * tupel2.Item3);
            }
            for (int k = 0; k < 3; k++)
            {
                colorArray[k] = (int)(colorArray[k] * 0.15f);
                colorArray[k] += 20;
            }
            return colorArray;
        }
    }
}
