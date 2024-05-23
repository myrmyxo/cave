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
    public class Chunks
    {
        public class Chunk
        {
            public Screens.Screen screen;

            public long chunkSeed;

            public (int x, int y) position;

            public int[,,] primaryFillValues;
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

            public int explorationLevel = 2; // 0 to not visible, 1 to partially visible, 2 to full visible. 
            public bool[,] fogOfWar = null;
            public Bitmap fogBitmap = null;
            public Chunk()
            {

            }
            public Chunk((int x, int y) posToPut, bool structureGenerated, Screens.Screen screenToPut)
            {
                screen = screenToPut;
                position = posToPut;
                long chunkX = position.x * 2;
                long chunkY = position.y * 2;

                bool filePresent = testLoadChunk(structureGenerated);

                chunkSeed = findPlantSeed(chunkX, chunkY, screen, 0);
                long bigSeed = LCGxNeg(LCGz(LCGyPos(LCGxNeg(screen.seed))));
                long bigSeed2 = LCGxNeg(LCGz(LCGyPos(LCGxPos(bigSeed))));
                long bigSeed3 = LCGxNeg(LCGz(LCGyPos(LCGxNeg(bigSeed2))));

                if (!filePresent)
                {
                    primaryFillValues = new int[4, 2, 4]
                    {
                        {
                            {
                                findPrimaryNoiseValue(chunkX, chunkY, screen.seed, 0),
                                findPrimaryNoiseValue(chunkX + 1, chunkY, screen.seed, 0),
                                findPrimaryNoiseValue(chunkX, chunkY + 1, screen.seed, 0),
                                findPrimaryNoiseValue(chunkX + 1, chunkY + 1, screen.seed, 0)
                            },
                            {
                                findPrimaryNoiseValue(chunkX, chunkY, bigSeed2, 1),
                                findPrimaryNoiseValue(chunkX + 1, chunkY, bigSeed2, 1),
                                findPrimaryNoiseValue(chunkX, chunkY + 1, bigSeed2, 1),
                                findPrimaryNoiseValue(chunkX + 1, chunkY + 1, bigSeed2, 1)
                            }
                        },
                        {
                            {
                                findPrimaryNoiseValue(chunkX + 1, chunkY, screen.seed, 0),
                                findPrimaryNoiseValue(chunkX + 2, chunkY, screen.seed, 0),
                                findPrimaryNoiseValue(chunkX + 1, chunkY + 1, screen.seed, 0),
                                findPrimaryNoiseValue(chunkX + 2, chunkY + 1, screen.seed, 0)
                            },
                            {
                                findPrimaryNoiseValue(chunkX + 1, chunkY, bigSeed2, 1),
                                findPrimaryNoiseValue(chunkX + 2, chunkY, bigSeed2, 1),
                                findPrimaryNoiseValue(chunkX + 1, chunkY + 1, bigSeed2, 1),
                                findPrimaryNoiseValue(chunkX + 2, chunkY + 1, bigSeed2, 1)
                            }
                        },
                        {
                            {
                                findPrimaryNoiseValue(chunkX, chunkY + 1, screen.seed, 0),
                                findPrimaryNoiseValue(chunkX + 1, chunkY + 1, screen.seed, 0),
                                findPrimaryNoiseValue(chunkX, chunkY + 2, screen.seed, 0),
                                findPrimaryNoiseValue(chunkX + 1, chunkY + 2, screen.seed, 0)
                            },
                            {
                                findPrimaryNoiseValue(chunkX, chunkY + 1, bigSeed2, 1),
                                findPrimaryNoiseValue(chunkX + 1, chunkY + 1, bigSeed2, 1),
                                findPrimaryNoiseValue(chunkX, chunkY + 2, bigSeed2, 1),
                                findPrimaryNoiseValue(chunkX + 1, chunkY + 2, bigSeed2, 1)
                            }
                        },
                        {
                            {
                                findPrimaryNoiseValue(chunkX + 1, chunkY + 1, screen.seed, 0),
                                findPrimaryNoiseValue(chunkX + 2, chunkY + 1, screen.seed, 0),
                                findPrimaryNoiseValue(chunkX + 1, chunkY + 2, screen.seed, 0),
                                findPrimaryNoiseValue(chunkX + 2, chunkY + 2, screen.seed, 0)
                            },
                            {
                                findPrimaryNoiseValue(chunkX + 1, chunkY + 1, bigSeed2, 1),
                                findPrimaryNoiseValue(chunkX + 2, chunkY + 1, bigSeed2, 1),
                                findPrimaryNoiseValue(chunkX + 1, chunkY + 2, bigSeed2, 1),
                                findPrimaryNoiseValue(chunkX + 2, chunkY + 2, bigSeed2, 1)
                            }
                        }
                    };
                }
                chunkX = Floor(position.x, 2) / 2;
                chunkY = Floor(position.y, 2) / 2;
                if (!filePresent)
                {
                    primaryBigFillValues = new int[,]
                    {
                        {
                            findPrimaryNoiseValue(chunkX, chunkY, bigSeed, 0),
                            findPrimaryNoiseValue(chunkX + 1, chunkY, bigSeed, 0),
                            findPrimaryNoiseValue(chunkX, chunkY + 1, bigSeed, 0),
                            findPrimaryNoiseValue(chunkX + 1, chunkY + 1, bigSeed, 0)
                        },
                        {
                            findPrimaryNoiseValue(chunkX, chunkY, bigSeed3, 1),
                            findPrimaryNoiseValue(chunkX + 1, chunkY, bigSeed3, 1),
                            findPrimaryNoiseValue(chunkX, chunkY + 1, bigSeed3, 1),
                            findPrimaryNoiseValue(chunkX + 1, chunkY + 1, bigSeed3, 1)
                        }
                    };
                }
                chunkX = Floor(position.x, 16) / 16;
                chunkY = Floor(position.y, 16) / 16;
                primaryBiomeValues = new int[,]
                {
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, screen.seed, 0),
                        findPrimaryBiomeValue(chunkX+1, chunkY, screen.seed, 0),
                        findPrimaryBiomeValue(chunkX, chunkY+1, screen.seed, 0),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, screen.seed, 0)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, screen.seed, 1),
                        findPrimaryBiomeValue(chunkX+1, chunkY, screen.seed, 1),
                        findPrimaryBiomeValue(chunkX, chunkY+1, screen.seed, 1),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, screen.seed, 1)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, screen.seed, 2),
                        findPrimaryBiomeValue(chunkX+1, chunkY, screen.seed, 2),
                        findPrimaryBiomeValue(chunkX, chunkY+1, screen.seed, 2),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, screen.seed, 2)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, screen.seed, 3),
                        findPrimaryBiomeValue(chunkX+1, chunkY, screen.seed, 3),
                        findPrimaryBiomeValue(chunkX, chunkY+1, screen.seed, 3),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, screen.seed, 3)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, screen.seed, 4),
                        findPrimaryBiomeValue(chunkX+1, chunkY, screen.seed, 4),
                        findPrimaryBiomeValue(chunkX, chunkY+1, screen.seed, 4),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, screen.seed, 4)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, screen.seed, 5),
                        findPrimaryBiomeValue(chunkX+1, chunkY, screen.seed, 5),
                        findPrimaryBiomeValue(chunkX, chunkY+1, screen.seed, 5),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, screen.seed, 5)
                    }
                };

                chunkX = Floor(position.Item1, 32) / 32;
                chunkY = Floor(position.Item2, 32) / 32;
                primaryBigBiomeValues = new int[,]
                {
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, bigSeed, 0),
                        findPrimaryBiomeValue(chunkX+1, chunkY, bigSeed, 0),
                        findPrimaryBiomeValue(chunkX, chunkY+1, bigSeed, 0),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, bigSeed, 0)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, bigSeed, 1),
                        findPrimaryBiomeValue(chunkX+1, chunkY, bigSeed, 1),
                        findPrimaryBiomeValue(chunkX, chunkY+1, bigSeed, 1),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, bigSeed, 1)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, bigSeed, 2),
                        findPrimaryBiomeValue(chunkX+1, chunkY, bigSeed, 2),
                        findPrimaryBiomeValue(chunkX, chunkY+1, bigSeed, 2),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, bigSeed, 2)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, bigSeed, 3),
                        findPrimaryBiomeValue(chunkX+1, chunkY, bigSeed, 3),
                        findPrimaryBiomeValue(chunkX, chunkY+1, bigSeed, 3),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, bigSeed, 3)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, bigSeed, 4),
                        findPrimaryBiomeValue(chunkX+1, chunkY, bigSeed, 4),
                        findPrimaryBiomeValue(chunkX, chunkY+1, bigSeed, 4),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, bigSeed, 4)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, bigSeed, 5),
                        findPrimaryBiomeValue(chunkX+1, chunkY, bigSeed, 5),
                        findPrimaryBiomeValue(chunkX, chunkY+1, bigSeed, 5),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, bigSeed, 5)
                    }
                };
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
                        for (int k = 0; k < 6; k++)
                        {
                            secondaryBiomeValues[i, j, k] = findSecondaryBiomeValue(this, i, j, k);
                            secondaryBigBiomeValues[i, j, k] = findSecondaryBigBiomeValue(this, i, j, k);
                        }
                        biomeIndex[i, j] = findBiome(secondaryBiomeValues, secondaryBigBiomeValues, i, j);

                        int[] colorArray = { 0, 0, 0 };
                        float mult;
                        foreach ((int, int) tupel in biomeIndex[i, j])
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
                        baseColors[i, j] = (colorArray[0], colorArray[1], colorArray[2]);
                    }
                }


                if (!filePresent)
                {
                    secondaryFillValues = new int[2, 32, 32];
                    secondaryBigFillValues = new int[2, 32, 32];
                    fillStates = new int[32, 32];
                    for (int i = 0; i < 32; i++)
                    {
                        for (int j = 0; j < 32; j++)
                        {
                            secondaryFillValues[0, i, j] = findSecondaryNoiseValue(this, i, j, 0);
                            secondaryBigFillValues[0, i, j] = findSecondaryBigNoiseValue(this, i, j, 0);
                            int value1 = secondaryBigFillValues[0, i, j] + (int)(0.25 * secondaryFillValues[0, i, j]) - 32;
                            //value1 = secondaryBigFillValues[0, i, j];
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
                                else if (tupel.Item1 == 3)
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
                                    float see1 = Obs((position.Item1 * 16) % 64 + 64 + i + mod2 * 0.15f + 0.5f, 64);
                                    float see2 = Obs((position.Item2 * 16) % 64 + 64 + j + mod2 * 0.15f + 0.5f, 64);
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
                    if (System.IO.File.Exists($"{currentDirectory}\\CaveData\\{screen.seed}\\ChunkData\\{position.Item1}.{position.Item2}.json"))
                    {
                        loadChunk(this, false);
                        return true;
                    }
                }
                else if (System.IO.File.Exists($"{currentDirectory}\\CaveData\\{screen.seed}\\ChunkData\\{position.Item1}.{position.Item2}.json"))
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
                bitmap.SetPixel(i, j, colors[i, j]);
            }
            public void spawnEntities()
            {
                Entity newEntity = new Entity(this);
                if (!newEntity.isDeadAndShouldDisappear)
                {
                    screen.activeEntities[newEntity.id] = newEntity;
                }
                for (int i = 0; i < 4; i++)
                {
                    Plant newPlant = new Plant(this);
                    if (!newPlant.isDeadAndShouldDisappear)
                    {
                        screen.activePlants[newPlant.id] = newPlant;
                    }
                }
                entitiesAndPlantsSpawned = true;
            }
            public void moveLiquids()
            {
                if (unstableLiquidCount > 0) //here
                {
                    (int, int) chunkCoords = screen.findChunkAbsoluteIndex(position.Item1 * 32 - 32, position.Item2 * 32);
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
    }
}
