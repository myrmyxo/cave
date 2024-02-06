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

namespace Cave
{
    public partial class Form1 : Form
    {
        public class Globals
        {
            public static int ChunkLength = 4;
            public static int UnloadedChunksAmount = 4;

            public static Random rand = new Random();
            public static Player player;

            public static bool[] arrowKeysState = { false, false, false, false };
            public static bool digPress = false;
            public static bool[] placePress = { false, false };
            public static bool[] zoomPress = { false, false };
            public static bool[] inventoryChangePress = { false, false };
            public static bool pausePress = false;
            public static bool shiftPress = false;
            public static float lastZoom = 0;
            public static DateTime timeAtLauch;
            public static float timeElapsed;

            public static string currentDirectory;

            public static float realCamPosX = 0;
            public static float realCamPosY = 0;
            public static int camPosX = 0;
            public static int camPosY = 0;

            public static float accCamX = 0;
            public static float accCamY = 0;
            public static float speedCamX = 0;
            public static float speedCamY = 0;

            public static (int, int)[] directionArray = new (int, int)[4] { (-1, 0), (1, 0), (0, 1), (0, -1) };

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
        public class Chunk
        {
            public Screen screen;

            public long chunkSeed;

            public (int, int) position;
            public int[,] primaryFillValues;
            public int[,] primaryBiomeValues;
            public int[,] primaryBigBiomeValues; // the biome trends that are bigger than biomes
            public int[,,] secondaryFillValues;
            public int[,,] secondaryBiomeValues;
            public int[,,] secondaryBigBiomeValues;
            public (int, int)[,][] biomeIndex;
            public int[,] fillStates;
            public (int,int,int)[,] baseColors;
            public Color[,] colors;
            public List<Entity> entityList = new List<Entity>();
            public List<Plant> plantList = new List<Plant>();
            public List<Plant> exteriorPlantList = new List<Plant>();
            public int modificationCount = 0;
            public int unstableLiquidCount = 1;
            public bool entitiesAndPlantsSpawned = false;

            public Bitmap bitmap;
            public Chunk((int, int) posToPut, bool structureGenerated, Screen screenToPut)
            {
                screen = screenToPut;
                position = posToPut;
                long chunkX = posToPut.Item1;
                long chunkY = posToPut.Item2;

                bool filePresent = testLoadChunk(structureGenerated);

                chunkSeed = findPlantSeed(chunkX, chunkY, screen, 0);

                if (!filePresent)
                {
                    primaryFillValues = new int[,]
                    {
                        {
                            findPrimaryNoiseValue(chunkX, chunkY, screen, 0),
                            findPrimaryNoiseValue(chunkX + 1, chunkY, screen, 0),
                            findPrimaryNoiseValue(chunkX, chunkY + 1, screen, 0),
                            findPrimaryNoiseValue(chunkX + 1, chunkY + 1, screen, 0)
                        },
                        {
                            findPrimaryNoiseValue(chunkX, chunkY, screen, 1),
                            findPrimaryNoiseValue(chunkX + 1, chunkY, screen, 1),
                            findPrimaryNoiseValue(chunkX, chunkY + 1, screen, 1),
                            findPrimaryNoiseValue(chunkX + 1, chunkY + 1, screen, 1)
                        }
                    };
                }
                chunkX = Floor(position.Item1, 8) / 8;
                chunkY = Floor(position.Item2, 8) / 8;
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
                long bigBiomeSeed = LCGxNeg(LCGz(LCGyPos(LCGxNeg(screen.seed))));
                primaryBigBiomeValues = new int[,]
                {
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, bigBiomeSeed, 0),
                        findPrimaryBiomeValue(chunkX+1, chunkY, bigBiomeSeed, 0),
                        findPrimaryBiomeValue(chunkX, chunkY+1, bigBiomeSeed, 0),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, bigBiomeSeed, 0)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, bigBiomeSeed, 1),
                        findPrimaryBiomeValue(chunkX+1, chunkY, bigBiomeSeed, 1),
                        findPrimaryBiomeValue(chunkX, chunkY+1, bigBiomeSeed, 1),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, bigBiomeSeed, 1)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, bigBiomeSeed, 2),
                        findPrimaryBiomeValue(chunkX+1, chunkY, bigBiomeSeed, 2),
                        findPrimaryBiomeValue(chunkX, chunkY+1, bigBiomeSeed, 2),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, bigBiomeSeed, 2)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, bigBiomeSeed, 3),
                        findPrimaryBiomeValue(chunkX+1, chunkY, bigBiomeSeed, 3),
                        findPrimaryBiomeValue(chunkX, chunkY+1, bigBiomeSeed, 3),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, bigBiomeSeed, 3)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, bigBiomeSeed, 4),
                        findPrimaryBiomeValue(chunkX+1, chunkY, bigBiomeSeed, 4),
                        findPrimaryBiomeValue(chunkX, chunkY+1, bigBiomeSeed, 4),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, bigBiomeSeed, 4)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, bigBiomeSeed, 5),
                        findPrimaryBiomeValue(chunkX+1, chunkY, bigBiomeSeed, 5),
                        findPrimaryBiomeValue(chunkX, chunkY+1, bigBiomeSeed, 5),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, bigBiomeSeed, 5)
                    }
                };
                secondaryBiomeValues = new int[32, 32, 6];
                secondaryBigBiomeValues = new int[32, 32, 6];
                biomeIndex = new (int, int)[32, 32][];
                baseColors = new (int,int,int)[32, 32];
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
                    fillStates = new int[32, 32];
                    for (int i = 0; i < 32; i++)
                    {
                        for (int j = 0; j < 32; j++)
                        {
                            secondaryFillValues[0, i, j] = findSecondaryNoiseValue(this, i, j, 0);
                            int value1 = secondaryFillValues[0, i, j];
                            secondaryFillValues[1, i, j] = findSecondaryNoiseValue(this, i, j, 1);
                            int value2 = secondaryFillValues[1, i, j];
                            int temperature = secondaryBiomeValues[i, j, 0];
                            int mod1 = (int)(secondaryBiomeValues[i, j, 4]*0.25);
                            int mod2 = (int)(secondaryBiomeValues[i, j, 5]*0.25);

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
                                    oceano = mult*10;
                                    oceanoSeeSaw = Min(Seesaw((int)oceano, 8), 8 - oceano);
                                    if (oceanoSeeSaw < 0)
                                    {
                                        oceanoSeeSaw = oceanoSeeSaw*oceanoSeeSaw*oceanoSeeSaw;
                                    }
                                    else { oceanoSeeSaw = oceanoSeeSaw*Abs(oceanoSeeSaw); }
                                    oceano *= 10;
                                    oceanoSeeSaw *= 10;
                                }
                                else { value2modifier += mult * (value1 % 16); }
                            }

                            mod2 = (int)(mod2 / mod2divider);

                            int elementToFillVoidWith;
                            if (biomeIndex[i, j][0].Item1 == 8) { elementToFillVoidWith = -2; }
                            else { elementToFillVoidWith = 0; }

                            if (value2 > 200 + value2modifier + oceano || value2 < (foresto-1)*75f - oceano) { fillStates[i, j] = elementToFillVoidWith; }
                            else if (value1 > 122 - mod2 * mod2 * foresto * 0.0003f + value1modifier + (int)(oceanoSeeSaw*0.1f) && value1 < 133 + mod2 * mod2 * foresto * 0.0003f - value1modifier - oceanoSeeSaw) { fillStates[i, j] = elementToFillVoidWith; }
                            else { fillStates[i, j] = 1; }
                            //fillStates[i, j] = 0;
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
                    spawnEntities();
                    entitiesAndPlantsSpawned = true;
                }
            }
            public bool testLoadChunk(bool structureGenerated)
            {
                if (structureGenerated)
                {
                    if (System.IO.File.Exists($"{currentDirectory}\\ChunkData\\{screen.seed}\\{position.Item1}.{position.Item2}.json"))
                    {
                        loadChunk(this, false);
                        return true;
                    }
                }
                else if (System.IO.File.Exists($"{currentDirectory}\\ChunkData\\{screen.seed}\\{position.Item1}.{position.Item2}.json"))
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
                        colorArray[k] = (int)(colorArray[k]*0.5f) + 120;
                    };
                }
                else if (fillStates[i, j] == -1)
                {
                    colorArray[0] = (int)(colorArray[0] * 0.8f) + 100;
                    colorArray[1] = (int)(colorArray[1] * 0.8f) + 100;
                    colorArray[2] = (int)(colorArray[2] * 0.8f) + 60;
                }
                else if (fillStates[i, j] == -2)
                {
                    colorArray[0] = (int)(colorArray[0]*0.8f) + 60;
                    colorArray[1] = (int)(colorArray[1]*0.8f) + 60;
                    colorArray[2] = (int)(colorArray[2]*0.8f) + 100;
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
                    screen.activeEntities.Add(newEntity);
                }
                Plant newPlant = new Plant(this);
                if (!newPlant.isDeadAndShouldDisappear)
                {
                    screen.activePlants.Add(newPlant);
                }

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
                        testLiquidUnstableLiquid(middleTestPositionChunk.position.Item1*32 + i, middleTestPositionChunk.position.Item1 * 32 + jb);
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
                int jTested = j-1;

                int absChunkX = position.Item1;
                int absChunkY = position.Item2;
                if (jTested < 0) { jTested += 32; absChunkY--; }
                (int, int) chunkCoords = screen.findChunkAbsoluteIndex(absChunkX * 32, absChunkY * 32);
                Chunk chunkToTest = screen.tryToGetChunk(chunkCoords);

                int repeatCounter = 0;
                while(repeatCounter < 500)
                {
                    iTested++;
                    if(iTested > 31)
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
                int jTested = j-1;

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
                {;
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
        public class Screen
        {
            public Chunk theFilledChunk;
            public Dictionary<(int, int), Chunk> loadedChunks;
            public Dictionary<(int, int), Chunk> extraLoadedChunks = new Dictionary<(int, int), Chunk>();
            public List<long>[,] LCGCacheListMatrix;
            public int chunkResolution;
            public long seed;
            public bool isPngToBeExported;
            public Bitmap gameBitmap;
            public Bitmap overlayBitmap;
            public List<Player> playerList = new List<Player>();
            public List<Entity> activeEntities = new List<Entity>();
            public List<Entity> entitesToRemove = new List<Entity>();
            public List<Plant> activePlants = new List<Plant>();
            public Dictionary<(int, int), List<Plant>> outOfBoundsPlants = new Dictionary<(int, int), List<Plant>>();
            public List<(int, int)> broadTestUnstableLiquidList = new List<(int, int)>();

            public bool initialLoadFinished = false;

            public int chunkX = 0;
            public int chunkY = 0;

            public Screen(int posX, int posY, int chunkResolutionToPut, long seedo, bool isPngToExport)
            {
                seed = seedo;
                isPngToBeExported = isPngToExport;
                playerList = new List<Player>();
                player = new Player(this);
                activeEntities = new List<Entity>();
                activePlants = new List<Plant>();
                chunkResolution = chunkResolutionToPut+UnloadedChunksAmount*2; // invisible chunks of the sides/top/bottom
                if (!Directory.Exists($"{currentDirectory}\\ChunkData\\{seed}"))
                {
                    Directory.CreateDirectory($"{currentDirectory}\\ChunkData\\{seed}");
                }
                if (!Directory.Exists($"{currentDirectory}\\StructureData\\{seed}"))
                {
                    Directory.CreateDirectory($"{currentDirectory}\\StructureData\\{seed}");
                }
                if (!Directory.Exists($"{currentDirectory}\\NestData\\{seed}"))
                {
                    Directory.CreateDirectory($"{currentDirectory}\\NestData\\{seed}");
                }
                if (!Directory.Exists($"{currentDirectory}\\bitmapos"))
                {
                    Directory.CreateDirectory($"{currentDirectory}\\bitmapos");
                }
                LCGCacheInit();
                makeTheFilledChunk();
                checkStructuresPlayerSpawn(player);
                loadChunks(posX, posY, seed);
                overlayBitmap = new Bitmap(512, 128);
            }
            public void LCGCacheInit()
            {
                LCGCacheListMatrix = new List<long>[2, 5];
                long longo;
                long longo2;
                for (int i = 0; i < 5; i++)
                {
                    LCGCacheListMatrix[0, i] = new List<long>();
                    LCGCacheListMatrix[1, i] = new List<long>();
                }
                longo = seed;
                longo2 = LCGz(seed);
                for (int j = 0; j < 10000; j+=50)
                {
                    LCGCacheListMatrix[0, 0].Add(longo);
                    LCGCacheListMatrix[1, 0].Add(longo2);
                    longo = LCGxPos(longo);
                    longo2 = LCGxPos(longo);
                }
                longo = seed;
                for (int j = 0; j < 10000; j+=50)
                {
                    LCGCacheListMatrix[0, 1].Add(longo);
                    LCGCacheListMatrix[1, 1].Add(longo2);
                    longo = LCGxNeg(longo);
                    longo2 = LCGxNeg(longo);
                }
                longo = seed;
                for (int j = 0; j < 10000; j+=50)
                {
                    LCGCacheListMatrix[0, 2].Add(longo);
                    LCGCacheListMatrix[1, 2].Add(longo2);
                    longo = LCGyPos(longo);
                    longo2 = LCGyPos(longo);
                }
                longo = seed;
                for (int j = 0; j < 10000; j+=50)
                {
                    if (j % 50 == 0)
                    {
                        LCGCacheListMatrix[0, 3].Add(longo);
                        LCGCacheListMatrix[1, 3].Add(longo2);
                    }
                    longo = LCGyNeg(longo);
                    longo2 = LCGyNeg(longo);
                }
                longo = seed;
                for (int j = 0; j < 10000; j+=50)
                {
                    if (j % 50 == 0)
                    {
                        LCGCacheListMatrix[0, 4].Add(longo);
                        LCGCacheListMatrix[1, 4].Add(longo2);
                    }
                    longo = LCGz(longo);
                    longo2 = LCGz(longo);
                }
            }
            public void loadChunks(int posX, int posY, long seed)
            {
                loadedChunks = new Dictionary<(int, int), Chunk>();
                for (int i = 0; i < chunkResolution; i++)
                {
                    for (int j = 0; j < chunkResolution; j++)
                    {
                        loadedChunks.Add((posX + i, posY + j), new Chunk((posX + i, posY + j), false, this));
                    }
                }
                if (isPngToBeExported) { gameBitmap = new Bitmap(32 * (chunkResolution - 1), 32 * (chunkResolution - 1)); }
                else { gameBitmap = new Bitmap(128 * (ChunkLength - 1), 128 * (ChunkLength - 1)); }
            }
            public void addPlantsToChunk(Chunk chunk)
            {
                if (outOfBoundsPlants.ContainsKey((chunk.position.Item1, chunk.position.Item2)))
                {
                    chunk.exteriorPlantList = outOfBoundsPlants[(chunk.position.Item1, chunk.position.Item2)];
                    outOfBoundsPlants.Remove((chunk.position.Item1, chunk.position.Item2));
                }
            }
            public void removePlantsFromChunk(Chunk chunk)
            {
                if(chunk.exteriorPlantList.Count > 0)
                {
                    outOfBoundsPlants.Add((chunk.position.Item1, chunk.position.Item2), new List<Plant>());
                }
                foreach(Plant plant in chunk.exteriorPlantList)
                {
                    outOfBoundsPlants[(chunk.position.Item1, chunk.position.Item2)].Add(plant);
                }
            }
            public void putEntitiesAndPlantsInChunks()
            {
                (int, int) chunkIndex;
                Chunk chunk;
                while (activeEntities.Count() > 0)
                {
                    chunkIndex = findChunkAbsoluteIndex(activeEntities[0].posX, activeEntities[0].posY);
                    chunk = loadedChunks[chunkIndex];
                    chunk.entityList.Add(activeEntities[0]);
                    activeEntities.RemoveAt(0);
                }
                while (activePlants.Count() > 0)
                {
                    chunkIndex = findChunkAbsoluteIndex(activePlants[0].posX, activePlants[0].posY);
                    chunk = loadedChunks[chunkIndex];
                    chunk.plantList.Add(activePlants[0]);
                    activePlants.RemoveAt(0);
                }
            }
            public void updateLoadedChunks(long seed, int screenSlideXtoPut, int screenSlideYtoPut)
            {
                int screenSlideX = screenSlideXtoPut;
                int screenSlideY = screenSlideYtoPut;

                // Okay I've changed shit to dictionary instead of array please don't bug bug please please please... . . Gone ! Forever now ! it's. fucking. BACKKKKKK     !! ! !! GONE AGAIN fuck it's backkkk WOOOOOOOOOHOOOOOOOOOOOO BUG IS GONE !!! It's 4am !!!! FUCK !!!! PROBLEM !!!!! The update loaded chuncks is lagging 8 (7?) chunkcs behind the actual normal loading... but only in the updated dimension

                Dictionary<(int, int), bool> chunksToAdd = new Dictionary<(int, int), bool>();
                Dictionary<(int, int), bool> chunksToDelete = new Dictionary<(int, int), bool>();

                int addo = -1;
                if (screenSlideX < 0) { addo = chunkResolution; }
                while (Abs(screenSlideX) > 0)
                {
                    for (int j = chunkY; j < chunkY + chunkResolution; j++)
                    {
                        chunksToDelete[(chunkX + addo + screenSlideX, j)] = true;
                        chunksToAdd[(chunkX + addo + Sign(screenSlideX)*chunkResolution + screenSlideX, j + screenSlideYtoPut)] = true;
                    }
                    screenSlideX = Sign(screenSlideX)*(Abs(screenSlideX)-1);
                }
                addo = -1;
                if (screenSlideY < 0) { addo = chunkResolution; }
                while (Abs(screenSlideY) > 0)
                {
                    for (int i = chunkX; i < chunkX + chunkResolution; i++)
                    {
                        chunksToDelete[(i, chunkY + addo + screenSlideY)] = true;
                        chunksToAdd[(i + screenSlideXtoPut, chunkY + addo + Sign(screenSlideY)*chunkResolution + screenSlideY)] = true;
                    }
                    screenSlideY = Sign(screenSlideY) * (Abs(screenSlideY) - 1);
                }
                chunkX += screenSlideXtoPut;
                chunkY += screenSlideYtoPut;


                foreach (Chunk chunko in loadedChunks.Values)
                {
                    chunko.entityList = new List<Entity>();
                    chunko.plantList = new List<Plant>();
                }
                putEntitiesAndPlantsInChunks();

                foreach((int, int) chunkPos in chunksToDelete.Keys)
                {
                    //removePlantsFromChunk(loadedChunks[chunkPos]);
                    Files.saveChunk(loadedChunks[chunkPos], true);
                    loadedChunks.Remove(chunkPos);
                }
                foreach ((int, int) chunkPos in chunksToAdd.Keys)
                {
                    loadedChunks.Add(chunkPos, new Chunk(chunkPos, false, this));
                    //addPlantsToChunk(loadedChunks[chunkPos]);
                    putEntitiesAndPlantsInChunks();
                }

                foreach (Chunk chunko in loadedChunks.Values)
                {
                    foreach (Entity entity in chunko.entityList)
                    {
                        activeEntities.Add(entity);
                    }
                    foreach (Plant plant in chunko.plantList)
                    {
                        activePlants.Add(plant);
                    }
                    chunko.entityList = new List<Entity>();
                    chunko.plantList = new List<Plant>();
                }
            }
            public (int, int) findChunkAbsoluteIndex(int pixelPosX, int pixelPosY)
            {
                int chunkPosX = Floor(pixelPosX, 32) / 32;
                int chunkPosY = Floor(pixelPosY, 32) / 32;
                return (chunkPosX, chunkPosY);
            }
            public void checkStructuresPlayerSpawn(Player player)
            {
                player.CheckStructurePosChange();
                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        createStructures(player.structureX + i, player.structureY + j);
                    }
                }
            }
            public void checkStructures(Player player)
            {
                (int, int) oldStructurePos = (player.structureX, player.structureY);
                if (player.CheckStructurePosChange())
                {
                    int changeX = player.structureX - oldStructurePos.Item1;
                    int changeY = player.structureY - oldStructurePos.Item2;
                    if (Abs(changeX) > 0)
                    {
                        createStructures(player.structureX + changeX, player.structureY + 1);
                        createStructures(player.structureX + changeX, player.structureY);
                        createStructures(player.structureX + changeX, player.structureY - 1);
                    }
                    if (Abs(changeY) > 0)
                    {
                        createStructures(player.structureX + 1, player.structureY + changeY);
                        createStructures(player.structureX, player.structureY + changeY);
                        createStructures(player.structureX - 1, player.structureY + changeY);
                    }
                }
            }
            public void createStructures(int posX, int posY)
            {
                if (!Directory.Exists($"{currentDirectory}\\StructureData\\{seed}\\{posX}.{posY}"))
                {
                    Directory.CreateDirectory($"{currentDirectory}\\StructureData\\{seed}\\{posX}.{posY}");
                    int x = posY % 10 + 15;
                    long seedX = seed + posX;
                    int y = posX % 10 + 15;
                    long seedY = seed + posY;
                    while (x > 0)
                    {
                        seedX = LCGxPos(seedX);
                        x--;
                    }
                    while (y > 0)
                    {
                        seedY = LCGyPos(seedY);
                        y--;
                    }
                    long structuresAmount = (seedX + seedY) % 3 + 1;
                    for (int i = 0; i < structuresAmount; i++)
                    {
                        seedX = LCGyPos(seedX); // on porpoise x    /\_/\
                        seedY = LCGxPos(seedY); // and y switched  ( ^o^ )
                        Structure newStructure = new Structure(posX * 512 + 32 + (int)(seedX % 480), posY * 512 + 32 + (int)(seedY % 480), seedX, seedY, false, (posX, posY), this);
                        newStructure.drawStructure();
                        newStructure.imprintChunks();
                        newStructure.saveInFile();
                    }
                    long waterLakesAmount = 15 + (seedX + seedY) % 150;
                    for (int i = 0; i < waterLakesAmount; i++)
                    {
                        seedX = LCGyNeg(seedX); // on porpoise x    /\_/\
                        seedY = LCGxNeg(seedY); // and y switched  ( ^o^ )
                        Structure newStructure = new Structure(posX * 512 + 32 + (int)(seedX % 480), posY * 512 + 32 + (int)(seedY % 480), seedX, seedY, true, (posX, posY), this);
                        newStructure.drawLakePapa();
                        newStructure.saveInFile();
                    }
                }
            }
            public Bitmap updateScreen()
            {
                int pixelPosX;
                int pixelPosY;

                int PNGmultiplicator = 4;
                if(isPngToBeExported) { PNGmultiplicator = 1; }

                for (int i = 0; i < chunkResolution; i++)
                {
                    for (int j = 0; j < chunkResolution; j++)
                    {
                        pixelPosX = i*32 - ((camPosX % 32) + 32) % 32 - UnloadedChunksAmount * 32;
                        pixelPosY = j*32 - ((camPosY % 32) + 32) % 32 - UnloadedChunksAmount * 32;

                        if (pixelPosX < -31 || pixelPosX >= (chunkResolution) * 32 || pixelPosY < -31 || pixelPosY >= (chunkResolution) * 32)
                        {
                            continue;
                        }
                        using (Graphics g = Graphics.FromImage(gameBitmap))
                        {
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                            g.DrawImage(loadedChunks[(chunkX + i, chunkY + j)].bitmap, pixelPosX*PNGmultiplicator, pixelPosY*PNGmultiplicator, 32*PNGmultiplicator, 32*PNGmultiplicator);
                        }
                    }
                }

                foreach (Plant plant in activePlants)
                {
                    pixelPosX = plant.posX - camPosX - UnloadedChunksAmount * 32;
                    pixelPosY = plant.posY - camPosY - UnloadedChunksAmount * 32;

                    if (pixelPosX >= 0 && pixelPosX < (chunkResolution - 1) * 32 && pixelPosY >= 0 && pixelPosY < (chunkResolution - 1) * 32)
                    {;
                        (int, int) chunkPos = findChunkAbsoluteIndex(plant.posX, plant.posY);
                        Chunk chunkToTest = loadedChunks[chunkPos];
                        if ( true/*chunkToTest.fillStates[(plant.posX % 32 + 32) % 32, (plant.posY % 32 + 32) % 32] <= 0*/)
                        {
                            using (Graphics g = Graphics.FromImage(gameBitmap))
                            {
                                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                                g.DrawImage(plant.bitmap, (pixelPosX + plant.posOffset[0]) * PNGmultiplicator, (pixelPosY + plant.posOffset[1]) * PNGmultiplicator, plant.bitmap.Width*PNGmultiplicator, plant.bitmap.Height*PNGmultiplicator);
                            }
                        }
                    }
                }

                foreach (Entity entity in activeEntities)
                {
                    pixelPosX = entity.posX - camPosX - UnloadedChunksAmount * 32;
                    pixelPosY = entity.posY - camPosY - UnloadedChunksAmount * 32;

                    if (pixelPosX >= 0 && pixelPosX < (chunkResolution - 1) * 32 && pixelPosY >= 0 && pixelPosY < (chunkResolution - 1) * 32)
                    {
                        Color color = entity.color;
                        (int, int) chunkPos = this.findChunkAbsoluteIndex(entity.posX, entity.posY);
                        Chunk chunkToTest = this.loadedChunks[chunkPos];
                        if (chunkToTest.fillStates[(entity.posX % 32 + 32) % 32, (entity.posY % 32 + 32) % 32] > 0)
                        {
                            color = Color.Red;
                        }
                        using (var g = Graphics.FromImage(gameBitmap))
                        {
                            g.FillRectangle(new SolidBrush(color), pixelPosX * PNGmultiplicator, pixelPosY * PNGmultiplicator, PNGmultiplicator, PNGmultiplicator);
                        }
                    }
                }
                foreach (Entity entity in activeEntities) // debug for paths
                {
                    foreach ((int x, int y) posToDrawAt in entity.pathToTarget)
                    {
                        pixelPosX = posToDrawAt.x - camPosX - UnloadedChunksAmount * 32;
                        pixelPosY = posToDrawAt.y - camPosY - UnloadedChunksAmount * 32;

                        if (pixelPosX >= 0 && pixelPosX < (chunkResolution - 1) * 32 && pixelPosY >= 0 && pixelPosY < (chunkResolution - 1) * 32)
                        {
                            Color color = entity.color;
                            using (var g = Graphics.FromImage(gameBitmap))
                            {
                                g.FillRectangle(new SolidBrush(color), pixelPosX * PNGmultiplicator, pixelPosY * PNGmultiplicator, PNGmultiplicator, PNGmultiplicator);
                            }
                        }
                    }
                }

                pixelPosX = player.posX - camPosX - UnloadedChunksAmount * 32;
                pixelPosY = player.posY - camPosY - UnloadedChunksAmount * 32;

                if (pixelPosX >= 0 && pixelPosX < (chunkResolution - 1) * 32 && pixelPosY >= 0 && pixelPosY < (chunkResolution - 1) * 32)
                {
                    Color color = Color.Green;
                    (int, int) chunkPos = this.findChunkAbsoluteIndex(player.posX, player.posY);
                    Chunk chunkToTest = this.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[(player.posX % 32 + 32) % 32, (player.posY % 32 + 32) % 32] > 0)
                    {
                        color = Color.Red;
                    }
                    using (var g = Graphics.FromImage(gameBitmap))
                    {
                        g.FillRectangle(new SolidBrush(color), pixelPosX * PNGmultiplicator, pixelPosY * PNGmultiplicator, PNGmultiplicator, PNGmultiplicator);
                    }
                }
                gameBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                return gameBitmap;
            }
            public void zoom(bool isZooming)
            {
                if(isZooming)
                {
                    if(ChunkLength > 2)
                    {
                        ChunkLength -= 2;
                        UnloadedChunksAmount++;
                        gameBitmap = new Bitmap(128 * (ChunkLength - 1), 128 * (ChunkLength - 1));
                    }
                }
                else
                {
                    if (UnloadedChunksAmount > 1)
                    {
                        ChunkLength += 2;
                        UnloadedChunksAmount--;
                        gameBitmap = new Bitmap(128 * (ChunkLength - 1), 128 * (ChunkLength - 1));
                    }

                }
                lastZoom = timeElapsed;
            }
            public void makeTheFilledChunk()
            {
                theFilledChunk = new Chunk((0, 0), true, this);
                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        theFilledChunk.fillStates[i,j] = 1;
                    }
                }
            }
            public Chunk tryToGetChunk((int, int) chunkCoords)
            {
                if (loadedChunks.TryGetValue(chunkCoords, out Chunk chunkToTest))
                {
                    return chunkToTest;
                }
                return theFilledChunk;
            }
        }
        public class Player
        {
            public Screen screen;

            public float realPosX = 0;
            public float realPosY = 0;
            public int posX = 0;
            public int posY = 0;
            public int structureX;
            public int structureY;
            public float speedX = 0;
            public float speedY = 0;

            public float timeAtLastDig = -9999;
            public float timeAtLastPlace = -9999;

            public Dictionary<(int index, int subType, int typeOfElement), int> inventoryQuantities;
            public List<(int index, int subType, int typeOfElement)> inventoryElements;
            public int inventoryCursor = 3;
            public Player (Screen screenToPut)
            {
                screen = screenToPut;
            }
            public (int, int) findIntPos(float positionX, float positionY)
            {
                return ((int)Floor(positionX, 1), (int)Floor(positionY, 1));
            }
            public void placePlayer()
            {
                int counto = 0;
                while (counto < 10000)
                {
                    int randX = rand.Next((ChunkLength - 1) * 32);
                    int randY = rand.Next((ChunkLength - 1) * 32);
                    Chunk randChunk = screen.loadedChunks[(randX / 32, randY / 32)];
                    if (randChunk.fillStates[randX % 32, randY % 32] == 0)
                    {
                        posX = randX;
                        realPosX = randX;
                        posY = randY;
                        realPosY = randY;
                        break;
                    }
                    counto++;
                }
                inventoryQuantities = new Dictionary<(int index, int subType, int typeOfElement), int>
                {
                    {(0, 0, 1), -999 },
                    {(0, 1, 1), -999 },
                    {(0, 2, 1), -999 },
                    {(1, 0, 1), -999 },
                    {(2, 0, 1), -999 },
                    {(3, 0, 1), -999 },
                    {(0, 0, 2), -999 },
                    {(1, 0, 2), -999 },
                    {(2, 0, 2), -999 },
                    {(2, 1, 2), -999 },
                    {(3, 0, 2), -999 },
                    {(4, 0, 2), -999 },
                    {(5, 0, 2), -999 },
                    {(5, 1, 2), -999 },
                    {(-1, 0, 0), -999 }
                };
                inventoryElements = new List<(int index, int subType, int typeOfElement)>
                {
                    (0, 0, 1),
                    (0, 1, 1),
                    (0, 2, 1),
                    (1, 0, 1),
                    (2, 0, 1),
                    (3, 0, 1),
                    (0, 0, 2),
                    (1, 0, 2),
                    (2, 0, 2),
                    (2, 1, 2),
                    (3, 0, 2),
                    (4, 0, 2),
                    (5, 0, 2),
                    (5, 1, 2),
                    (-1, 0, 0)
                };
        }
            public void movePlayer()
            {
                if (digPress && timeElapsed > timeAtLastDig + 0.2f)
                {
                    if (arrowKeysState[0] && !arrowKeysState[1])
                    {
                        Dig(posX + 1, posY);
                    }
                    else if (arrowKeysState[1] && !arrowKeysState[0])
                    {
                        Dig(posX - 1, posY);
                    }
                    else if (arrowKeysState[2] && !arrowKeysState[3])
                    {
                        Dig(posX, posY - 1);
                    }
                    else if (arrowKeysState[3] && !arrowKeysState[2])
                    {
                        Dig(posX, posY + 1);
                    }
                }
                if ((placePress[0] || placePress[1]) && ((inventoryElements[inventoryCursor].typeOfElement == 0 && timeElapsed > timeAtLastPlace + 0.01f) || (timeElapsed > timeAtLastPlace + 0.2f)))
                {
                    if (arrowKeysState[0] && !arrowKeysState[1])
                    {
                        Place(posX + 1, posY);
                    }
                    else if (arrowKeysState[1] && !arrowKeysState[0])
                    {
                        Place(posX - 1, posY);
                    }
                    else if (arrowKeysState[2] && !arrowKeysState[3])
                    {
                        Place(posX, posY - 1);
                    }
                    else if (arrowKeysState[3] && !arrowKeysState[2])
                    {
                        Place(posX, posY + 1);
                    }
                }
                {
                    (int, int) chunkPos = screen.findChunkAbsoluteIndex(posX, posY);
                    if (screen.loadedChunks[chunkPos].fillStates[(posX % 32 + 32) % 32, (posY % 32 + 32) % 32] < 0)
                    {
                        speedX = speedX * 0.8f - Sign(speedX)*Sqrt(Max((int)speedX-1,0));
                        speedY = speedY * 0.8f - Sign(speedY)*Sqrt(Max((int)speedY-1,0));
                    }
                }

                float toMoveX = speedX;
                float toMoveY = speedY;

                while (Abs(toMoveY) > 0)
                {
                    (int, int) chunkPos = screen.findChunkAbsoluteIndex(posX, posY + (int)Sign(toMoveY));
                    if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                    {
                        if (chunkToTest.fillStates[(posX % 32 + 32) % 32, (posY % 32 + 32 + (int)Sign(toMoveY)) % 32] <= 0)
                        {
                            if (Abs(toMoveY) >= 1)
                            {
                                posY += (int)Sign(toMoveY);
                                realPosY += Sign(toMoveY);
                                toMoveY = Sign(toMoveY) * (Abs(toMoveY) - 1);
                            }
                            else
                            {
                                realPosY += toMoveY;
                                posY = (int)Floor(realPosY, 1);
                                toMoveY = 0;
                            }
                        }
                        else
                        {
                            speedY = 0;
                            toMoveY = 0;
                            break;
                        }
                    }
                    else { break; }
                }
                while (Abs(toMoveX) > 0)
                {
                    (int, int) chunkPos = screen.findChunkAbsoluteIndex(posX + (int)Sign(toMoveX), posY);
                    if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                    {
                        if (chunkToTest.fillStates[(posX % 32 + 32 + (int)Sign(toMoveX)) % 32, (posY % 32 + 32) % 32] <= 0)
                        {
                            if (Abs(toMoveX) >= 1)
                            {
                                posX += (int)Sign(toMoveX);
                                realPosX += Sign(toMoveX);
                                toMoveX = Sign(toMoveX) * (Abs(toMoveX) - 1);
                            }
                            else
                            {
                                realPosX += toMoveX;
                                posX = (int)Floor(realPosX, 1);
                                toMoveX = 0;
                            }
                        }
                        else
                        {
                            speedX = 0;
                            toMoveX = 0;
                            break;
                        }
                    }
                    else { break; }
                }
            }
            public bool CheckStructurePosChange()
            {
                (int, int) oldStructurePos = (structureX, structureY);
                structureX = Floor(posX, 512) / 512;
                structureY = Floor(posY, 512) / 512;
                if (oldStructurePos == (structureX, structureY)) { return false; }
                return true;
            }
            public void Dig(int posToDigX, int posToDigY)
            {
                (int, int) chunkPos = screen.findChunkAbsoluteIndex(posToDigX, posToDigY);
                int value;
                foreach (Plant plant in screen.activePlants)
                {
                    value = plant.testDig(posToDigX, posToDigY);
                    if (value != 0)
                    {
                        (int index, int subType, int typeOfElement)[] inventoryKeys = inventoryQuantities.Keys.ToArray();
                        for (int i = 0; i < inventoryKeys.Length; i++)
                        {
                            if (inventoryKeys[i].index == value && inventoryKeys[i].typeOfElement == 3)
                            {
                                if (inventoryQuantities[(value, 0, 3)] != -999)
                                {
                                    inventoryQuantities[(value, 0, 3)]++;
                                }
                                goto AfterTest;
                            }
                        }
                        // there was none of the thing present in the inventory already so gotta create it
                        inventoryQuantities.Add((value, 0, 3), 1);
                        inventoryElements.Add((value, 0, 3));
                    AfterTest:;
                        timeAtLastDig = timeElapsed;
                        return;
                    }
                }
                if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest)) { return; }
                int tileContent = chunkToTest.fillStates[(posToDigX % 32 + 32) % 32, (posToDigY % 32 + 32) % 32];
                if (tileContent != 0)
                {
                    (int index, int subType, int typeOfElement)[] inventoryKeys = inventoryQuantities.Keys.ToArray();
                    for (int i = 0; i < inventoryKeys.Length; i++)
                    {
                        if (inventoryKeys[i].index == tileContent && inventoryKeys[i].typeOfElement == 0)
                        {
                            if (inventoryQuantities[(tileContent, 0, 0)] != -999)
                            {
                                inventoryQuantities[(tileContent, 0, 0)]++;
                            }
                            goto AfterTest;
                        }
                    }
                    // there was none of the thing present in the inventory already so gotta create it
                    inventoryQuantities.Add((tileContent, 0, 0), 1);
                    inventoryElements.Add((tileContent, 0, 0));
                    AfterTest:;
                    chunkToTest.fillStates[(posToDigX % 32 + 32) % 32, (posToDigY % 32 + 32) % 32] = 0;
                    chunkToTest.findTileColor((posToDigX % 32 + 32) % 32, (posToDigY % 32 + 32) % 32);
                    chunkToTest.testLiquidUnstableAir(posToDigX, posToDigY);
                    chunkToTest.modificationCount += 1;
                    timeAtLastDig = timeElapsed;
                }
            }
            public void Place(int posToDigX, int posToDigY)
            {
                (int, int) chunkPos = screen.findChunkAbsoluteIndex(posToDigX, posToDigY);
                if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest)) { return; }
                (int index, int subType, int typeOfElement) tileContent = inventoryElements[inventoryCursor];
                int tileState = chunkToTest.fillStates[(posToDigX % 32 + 32) % 32, (posToDigY % 32 + 32) % 32];
                if (tileState == 0 || tileState < 0 && tileContent.typeOfElement > 0 )
                {
                    if (tileContent.typeOfElement == 0)
                    {
                        chunkToTest.fillStates[(posToDigX % 32 + 32) % 32, (posToDigY % 32 + 32) % 32] = tileContent.index;
                        chunkToTest.findTileColor((posToDigX % 32 + 32) % 32, (posToDigY % 32 + 32) % 32);
                        chunkToTest.testLiquidUnstableLiquid(posToDigX, posToDigY);
                        chunkToTest.modificationCount += 1;
                        timeAtLastPlace = timeElapsed;
                    }
                    else if (tileContent.typeOfElement == 1)
                    {
                        Entity newEntity = new Entity(chunkToTest, (posToDigX, posToDigY), tileContent.index, tileContent.subType);
                        screen.activeEntities.Add(newEntity);
                        timeAtLastPlace = timeElapsed;
                    }
                    else if (tileContent.typeOfElement == 2)
                    {
                        Plant newPlant = new Plant(chunkToTest, (posToDigX, posToDigY), tileContent.index, tileContent.subType);
                        if (!newPlant.isDeadAndShouldDisappear) { screen.activePlants.Add(newPlant); }
                        timeAtLastPlace = timeElapsed;
                    }
                    else { return; }
                    if (inventoryQuantities[tileContent] != -999)
                    {
                        inventoryQuantities[tileContent]--;
                        if (inventoryQuantities[tileContent] <= 0)
                        {
                            inventoryQuantities.Remove(tileContent);
                            inventoryElements.Remove(tileContent);
                            moveInventoryCursor(0);
                        }
                    }
                }
            }
            public void moveInventoryCursor(int value)
            {
                int counto = inventoryElements.Count;
                if (counto == 0) { inventoryCursor = 0; }
                inventoryCursor = ((inventoryCursor + value)%counto + counto)%counto;
            }
            public void drawInventory()
            {
                if (inventoryElements.Count > 0)
                {
                    (int index, int subType, int typeOfElement) element = inventoryElements[inventoryCursor];
                    if (element.typeOfElement == 0)
                    {
                        Sprites.drawSpriteOnCanvas(screen.overlayBitmap, compoundSprites[element.index].bitmap, (340, 64), 4, true);
                    }
                    else if (element.typeOfElement == 1)
                    {
                        Sprites.drawSpriteOnCanvas(screen.overlayBitmap, entitySprites[(element.index, element.subType)].bitmap, (340, 64), 4, true);
                    }
                    else if (element.typeOfElement == 2)
                    {
                        Sprites.drawSpriteOnCanvas(screen.overlayBitmap, plantSprites[(element.index, element.subType)].bitmap, (340, 64), 4, true);
                    }
                    else if (element.typeOfElement == 3)
                    {
                        Sprites.drawSpriteOnCanvas(screen.overlayBitmap, materialSprites[(element.index, element.subType)].bitmap, (340, 64), 4, true);
                    }
                    int quantity = inventoryQuantities[element];
                    if(quantity == -999)
                    {
                        Sprites.drawSpriteOnCanvas(screen.overlayBitmap, numberSprites[10].bitmap, (408, 64), 4, true);
                    }
                    else
                    {
                        List<int> numberList = new List<int>();
                        for (int i = 0; quantity > 0; i++)
                        {
                            numberList.Insert(0, quantity%10);
                            quantity = quantity/10;
                        }
                        for (int i = 0; i < numberList.Count; i++)
                        {
                            Sprites.drawSpriteOnCanvas(screen.overlayBitmap, numberSprites[numberList[i]].bitmap, (408+i*32, 64), 4, true);
                        }
                    }
                }
            }
        }
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            currentDirectory = System.IO.Directory.GetCurrentDirectory();

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

            loadSpriteDictionaries();

            Screen mainScreen;

            bool updatePNG = false;
            int PNGsize = 50; // in chunks, 300 or more made it out of memory :( so put at 250 okok
            bool randomSeed = true;

            long seed = 3452270044;

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
            if (updatePNG)
            {
                int oldChunkLength = ChunkLength;
                int rando = -10;
                camPosX = -50 * 32;
                camPosY = rando * 32;
                ChunkLength = PNGsize;
                mainScreen = new Screen(0, 0, ChunkLength, seed, true);
                player.placePlayer();
                camPosX = player.posX - ChunkLength * 24;
                realCamPosX = camPosX;
                camPosY = player.posY - ChunkLength * 24;
                realCamPosY = camPosY;
                mainScreen.playerList = new List<Player> { player };
                timer1.Tag = mainScreen;
                timeAtLauch = DateTime.Now;

                timer1_Tick(new object(), new EventArgs());

                mainScreen.updateScreen().Save($"{currentDirectory}\\cavee.png");
                ChunkLength = oldChunkLength;
            }

            mainScreen = new Screen(0, 0, ChunkLength, seed, false);
            player.placePlayer();
            camPosX = player.posX-ChunkLength*24;
            realCamPosX = camPosX;
            camPosY = player.posY-ChunkLength*24;
            realCamPosY = camPosY;
            mainScreen.playerList = new List<Player> { player };
            timer1.Tag = mainScreen;
            timeAtLauch = DateTime.Now;
        }
        public static long findPlantSeed(long posX, long posY, Screen screen, int layer)
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
        public static int findPrimaryNoiseValue(long posX, long posY, Screen screen, int layer)
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
            int modulo = 32;
            int modX = GetChunkTileIndex1D(chunk.position.Item1 * 32 + varX, modulo);
            int modY = GetChunkTileIndex1D(chunk.position.Item2 * 32 + varY, modulo);
            int[,] values = chunk.primaryFillValues;
            int fX1 = values[layer, 0] * (modulo - modX) + values[layer, 1] * modX;
            int fX2 = values[layer, 2] * (modulo - modX) + values[layer, 3] * modX;
            int fY = fX1 * (modulo - modY) + fX2 * modY;
            return fY / (modulo * modulo);
        }
        public static int findSecondaryBiomeValue(Chunk chunk, int varX, int varY, int layer)
        {
            int modulo = 256;
            int modX = GetChunkTileIndex1D(chunk.position.Item1 * 32 + varX, modulo);
            int modY = GetChunkTileIndex1D(chunk.position.Item2 * 32 + varY, modulo);
            int[,] values = chunk.primaryBiomeValues;
            int fX1 = values[layer, 0] * (modulo - modX) + values[layer, 1] * modX;
            int fX2 = values[layer, 2] * (modulo - modX) + values[layer, 3] * modX;
            int fY = fX1 * (modulo - modY) + fX2 * modY;
            return fY / (modulo * modulo);
        }
        public static int findSecondaryBigBiomeValue(Chunk chunk, int varX, int varY, int layer)
        {
            int modulo = 1024;
            int modX = GetChunkTileIndex1D(chunk.position.Item1 * 32 + varX, modulo);
            int modY = GetChunkTileIndex1D(chunk.position.Item2 * 32 + varY, modulo);
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
            Screen screen = (Screen)timer1.Tag;
            if (!pausePress)
            {
                timeElapsed += 0.02f;
                screen.extraLoadedChunks.Clear(); // this will make many bugs
                screen.broadTestUnstableLiquidList = new List<(int, int)>();
                if (zoomPress[0] && timeElapsed > lastZoom + 0.25f) { screen.zoom(true); }
                if (zoomPress[1] && timeElapsed > lastZoom + 0.25f) { screen.zoom(false); }
                if (inventoryChangePress[0]) { inventoryChangePress[0] = false; player.moveInventoryCursor(-1); }
                if (inventoryChangePress[1]) { inventoryChangePress[1] = false; player.moveInventoryCursor(1); }
                timeElapsed = (float)((DateTime.Now - timeAtLauch).TotalSeconds);
                accCamX = 0;
                accCamY = 0;
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
                foreach (Player playor in screen.playerList)
                {
                    playor.movePlayer();
                    screen.checkStructures(playor);
                }


                int posDiffX = player.posX - (camPosX + 16 * (screen.chunkResolution - 1)); //*2 is needed cause there's only *8 and not *16 before
                int posDiffY = player.posY - (camPosY + 16 * (screen.chunkResolution - 1));
                accCamX = Sign(posDiffX) * Max(0, Sqrt(Abs(posDiffX)) - 2);
                accCamY = Sign(posDiffY) * Max(0, Sqrt(Abs(posDiffY)) - 2);
                if (accCamX == 0 || Sign(accCamX) != Sign(speedCamX))
                {
                    speedCamX = Sign(speedCamX) * (Max(Abs(speedCamX) - 1, 0));
                }
                if (accCamY == 0 || Sign(accCamY) != Sign(speedCamY))
                {
                    speedCamY = Sign(speedCamY) * (Max(Abs(speedCamY) - 1, 0));
                }
                speedCamX = Clamp(speedCamX + accCamX, -15f, 15f);
                speedCamY = Clamp(speedCamY + accCamY, -15f, 15f);
                realCamPosX += speedCamX;
                realCamPosY += speedCamY;
                camPosX = (int)(realCamPosX + 0.5f);
                camPosY = (int)(realCamPosY + 0.5f);
                int oldChunkX = screen.chunkX;
                int oldChunkY = screen.chunkY;
                int chunkVariationX = Floor(camPosX, 32) / 32 - oldChunkX;
                int chunkVariationY = Floor(camPosY, 32) / 32 - oldChunkY;
                if (chunkVariationX != 0 || chunkVariationY != 0)
                {
                    screen.updateLoadedChunks(screen.seed, chunkVariationX, chunkVariationY);
                }



                screen.entitesToRemove = new List<Entity>();
                foreach (Entity entity in screen.activeEntities)
                {
                    entity.testDigPlace();
                }
                foreach (Entity entity in screen.activeEntities)
                {
                    entity.moveEntity();
                }
                foreach (Entity entity in screen.entitesToRemove)
                {
                    screen.activeEntities.Remove(entity);
                }
                foreach (Plant plant in screen.activePlants)
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
            if ((Control.ModifierKeys & Keys.Shift) == 0)
            {
                shiftPress = false;
            }
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
        public static long Floor(long value, long modulo)
        {
            return value - (((value % modulo) + modulo) % modulo);
        }
        public static float Floor(float value, float modulo)
        {
            return value - (((value % modulo) + modulo) % modulo);
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
        public static (int x, int y) GetChunkTileIndex((int x, int y) poso, int modulo)
        {
            int posX = poso.x % modulo;
            int posY = poso.y % modulo;
            if (posX < 0) { posX += modulo; }
            if (posY < 0) { posY += modulo; }
            return (posX, posY);
        }
        public static (int x, int y) GetChunkTileIndex(int posoX, int posoY, int modulo)
        {
            int posX = posoX%modulo;
            int posY = posoY%modulo;
            if (posX < 0) { posX += modulo; }
            if (posY < 0) { posY += modulo; }
            return (posX, posY);
        }
        public static int GetChunkTileIndex1D(int poso, int modulo)
        {
            int pos = poso % modulo;
            if (pos < 0) { pos += modulo; }
            return pos;
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