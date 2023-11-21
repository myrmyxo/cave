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
using static Cave.Form1;
using static Cave.Form1.Globals;
using static Cave.MathF;
using static Cave.Sprites;
using static Cave.Structures;
using static Cave.Entities;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace Cave
{
    public partial class Form1 : Form
    {
        public class Globals
        {
            public static int ChunkLength = 6;
            public static int UnloadedChunksAmount = 6;

            public static Random rand = new Random();
            public static Player player;

            public static bool[] arrowKeysState = { false, false, false, false };
            public static bool digPress = false;
            public static bool[] placePress = { false, false };
            public static bool[] zoomPress = { false, false };
            public static bool[] inventoryChangePress = { false, false };
            public static bool shiftPress = false;
            public static float lastZoom = 0;
            public static DateTime timeAtLauch;
            public static float timeElapsed;

            public static string currentDirectory;

            public static float realCamPosX = 0;
            public static float realCamPosY = 0;
            public static int camPosX = 0;
            public static int camPosY = 0;
            public static int screenChunkX = 0;
            public static int screenChunkY = 0;

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
                { 7, (Color.LightBlue.R,Color.LightBlue.G,Color.LightBlue.B) }, // deep cold biome
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

            public (long, long) position;
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
            public int modificationCount = 0;
            public int liquidCount = 0;
            public bool isUnstable = false;

            public Bitmap bitmap;
            public int[,] plantFillStates;
            public Bitmap plantBitmap;
            public Chunk(long posX, long posY, long seed, bool structureGenerated, Screen screenToPut)
            {
                screen = screenToPut;
                long bigBiomeSeed = LCGxNeg(LCGz(LCGyPos(LCGxNeg(seed))));
                position = (posX, posY);
                long chunkX = (long)(Floor(posX, 2) * 0.5f);
                long chunkY = (long)(Floor(posY, 2) * 0.5f);
                int[,] fillValues =
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
                chunkSeed = findPlantSeed(chunkX, chunkY, screen, 0);
                primaryFillValues = fillValues;
                chunkX = Floor(posX, 16) / 16;
                chunkY = Floor(posY, 16) / 16;
                int[,] biomeValues =
                {
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, seed, 0),
                        findPrimaryBiomeValue(chunkX+1, chunkY, seed, 0),
                        findPrimaryBiomeValue(chunkX, chunkY+1, seed, 0),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, seed, 0)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, seed, 1),
                        findPrimaryBiomeValue(chunkX+1, chunkY, seed, 1),
                        findPrimaryBiomeValue(chunkX, chunkY+1, seed, 1),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, seed, 1)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, seed, 2),
                        findPrimaryBiomeValue(chunkX+1, chunkY, seed, 2),
                        findPrimaryBiomeValue(chunkX, chunkY+1, seed, 2),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, seed, 2)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, seed, 3),
                        findPrimaryBiomeValue(chunkX+1, chunkY, seed, 3),
                        findPrimaryBiomeValue(chunkX, chunkY+1, seed, 3),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, seed, 3)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, seed, 4),
                        findPrimaryBiomeValue(chunkX+1, chunkY, seed, 4),
                        findPrimaryBiomeValue(chunkX, chunkY+1, seed, 4),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, seed, 4)
                    },
                    {
                        findPrimaryBiomeValue(chunkX, chunkY, seed, 5),
                        findPrimaryBiomeValue(chunkX+1, chunkY, seed, 5),
                        findPrimaryBiomeValue(chunkX, chunkY+1, seed, 5),
                        findPrimaryBiomeValue(chunkX+1, chunkY+1, seed, 5)
                    }
                };
                primaryBiomeValues = biomeValues;

                chunkX = Floor(posX, 64) / 64;
                chunkY = Floor(posY, 64) / 64;
                int[,] bigBiomeValues =
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
                primaryBigBiomeValues = bigBiomeValues;
                secondaryFillValues = new int[2, 16, 16];
                secondaryBiomeValues = new int[16, 16, 6];
                secondaryBigBiomeValues = new int[16, 16, 6];
                biomeIndex = new (int, int)[16, 16][];
                fillStates = new int[16, 16];
                baseColors = new (int,int,int)[16, 16];
                colors = new Color[16, 16];
                bitmap = new Bitmap(16, 16);
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        int value;
                        for (int k = 0; k < 6; k++)
                        {
                            value = findSecondaryBiomeValue(this, i, j, k);
                            secondaryBiomeValues[i, j, k] = value;
                            value = findSecondaryBigBiomeValue(this, i, j, k);
                            secondaryBigBiomeValues[i, j, k] = value;
                        }
                        (int, int)[] valueTupleArray = findBiome(secondaryBiomeValues, secondaryBigBiomeValues, i, j);
                        biomeIndex[i, j] = valueTupleArray;
                        value = findSecondaryNoiseValue(this, i, j, 0);
                        secondaryFillValues[0, i, j] = value;
                        int value1 = value;
                        value = findSecondaryNoiseValue(this, i, j, 1);
                        secondaryFillValues[1, i, j] = value;
                        int value2 = value;
                        int temperature = secondaryBiomeValues[i, j, 0];
                        int mod1 = secondaryBiomeValues[i, j, 4];
                        int mod2 = secondaryBiomeValues[i, j, 5];

                        float valueToBeAdded;
                        float value1modifier = 0;
                        float value2PREmodifier;
                        float value2modifier = 0;
                        int[] colorArray = { 0, 0, 0 };
                        float mod2divider = 1;

                        float mult;
                        foreach ((int, int) tupel in biomeIndex[i, j])
                        {
                            mult = tupel.Item2 * 0.01f;
                            if (tupel.Item1 == 1)
                            {
                                value2modifier += -3 * mult * Max(sawBladeSeesaw(value1, 13), sawBladeSeesaw(value1, 11));
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
                                float see1 = Obs((posX * 16) % 64 + 64 + i + mod2 * 0.15f + 0.5f, 64);
                                float see2 = Obs((posY * 16) % 64 + 64 + j + mod2 * 0.15f + 0.5f, 64);
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
                            else { value2modifier += mult * (value1 % 16); }

                            (int, int, int) tupel2 = biomeDict[tupel.Item1];
                            colorArray[0] += (int)(mult * tupel2.Item1);
                            colorArray[1] += (int)(mult * tupel2.Item2);
                            colorArray[2] += (int)(mult * tupel2.Item3);
                        }

                        mod2 = (int)(mod2 / mod2divider);

                        if (value2 > 200 + value2modifier) { fillStates[i, j] = 0; }
                        else if (value1 > 122 - mod2 * mod2 * 0.0003f + value1modifier && value1 < 133 + mod2 * mod2 * 0.0003f - value1modifier) { fillStates[i, j] = 0; }
                        else { fillStates[i, j] = 1; }


                        for (int k = 0; k < 3; k++)
                        {
                            colorArray[k] = (int)(colorArray[k] * 0.15f);
                            colorArray[k] += 20;
                        }
                        baseColors[i, j] = (colorArray[0], colorArray[1], colorArray[2]);
                    }
                }
                if (structureGenerated)
                {
                    if (System.IO.File.Exists($"{currentDirectory}\\ChunkData\\{seed}\\{position.Item1}.{position.Item2}.txt"))
                    {
                        loadChunk(true);
                    }
                }
                else if (System.IO.File.Exists($"{currentDirectory}\\ChunkData\\{seed}\\{position.Item1}.{position.Item2}.txt"))
                {
                    loadChunk(false);
                }
                else { spawnEntities();}

                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        findTileColor(i, j);
                    }
                }
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
                colors[i, j] = Color.FromArgb(ColorClamp(colorArray[0]), ColorClamp(colorArray[1]), ColorClamp(colorArray[2]));
                bitmap.SetPixel(i, j, colors[i, j]);
            }
            public void spawnEntities()
            {
                Entity newEntity = new Entity(this, screen);
                if (!newEntity.isDeadAndShouldDisappear) { screen.activeEntities.Add(newEntity); }
                Plant newPlant = new Plant(chunkSeed, this, screen);
                if (!newEntity.isDeadAndShouldDisappear) { plantList.Add(newPlant); }
            }
            public void loadChunk(bool forceEntityPlantNotSpawning)
            {
                bool willSpawnEntities;
                using (StreamReader f = new StreamReader($"{currentDirectory}\\ChunkData\\{screen.seed}\\{position.Item1}.{position.Item2}.txt"))
                {
                    string line = f.ReadLine();
                    if (line != null && line.Length > 0 && line[line.Length - 1] == ';')
                    {
                        if (line[0] == '0')
                        {
                            willSpawnEntities = true;
                        }
                        else { willSpawnEntities = false; }
                    }
                    else { willSpawnEntities = false; }
                    line = f.ReadLine();
                    int idx = 0;
                    int length = 0;
                    if (line != null)
                    {
                        List<string> listo = new List<string>();
                        for (int i = 0; i < line.Length; i++)
                        {
                            if (line[i] == ';')
                            {
                                listo.Add(line.Substring(idx, length));
                                idx = i + 1;
                                length = -1;
                            }
                            length++;
                        }
                        for (int i = 0; i < listo.Count(); i+=6)
                        {
                            int posXt = Int32.Parse(listo[i]);
                            int posYt = Int32.Parse(listo[i + 1]);
                            int typet = Int32.Parse(listo[i + 2]);
                            int rt = Int32.Parse(listo[i + 3]);
                            int gt = Int32.Parse(listo[i + 4]);
                            int bt = Int32.Parse(listo[i + 5]);
                            screen.activeEntities.Add(new Entity(posXt, posYt, typet, rt, gt, bt, screen));
                        }
                    }

                    line = f.ReadLine();
                    idx = 0;
                    length = 0;
                    if (line != null)
                    {
                        List<string> listo = new List<string>();
                        for (int i = 0; i < line.Length; i++)
                        {
                            if (line[i] == ';')
                            {
                                listo.Add(line.Substring(idx, length));
                                idx = i + 1;
                                length = -1;
                            }
                            length++;
                        }
                        for (int i = 0; i < listo.Count(); i+=4)
                        {
                            int posXt = Int32.Parse(listo[i]);
                            int posYt = Int32.Parse(listo[i + 1]);
                            int typet = Int32.Parse(listo[i + 2]);
                            int seedt = Int32.Parse(listo[i + 3]);
                            screen.activePlants.Add(new Plant(posXt, posYt, typet, seedt, this, screen));
                        }
                    }

                    line = f.ReadLine();
                    idx = 0;
                    length = 0;
                    if (line[0] != 'x')
                    {
                        modificationCount = 1;
                        List<string> listo = new List<string>();
                        for (int i = 0; i < line.Length; i++)
                        {
                            if (line[i] == ';')
                            {
                                listo.Add(line.Substring(idx, length));
                                idx = i + 1;
                                length = -1;
                            }
                            length++;
                        }
                        for (int i = 0; i < 16; i++)
                        {
                            for (int j = 0; j < 16; j++)
                            {
                                fillStates[i, j] = Int32.Parse(listo[i * 16 + j]);
                                if (fillStates[i, j] < 0)
                                {
                                    liquidCount++;
                                }
                            }
                        }
                    }

                    if (willSpawnEntities && !forceEntityPlantNotSpawning) { spawnEntities(); }
                }
            }
            public void saveChunk(bool creaturesSpawned)
            {
                using (StreamWriter f = new StreamWriter($"{currentDirectory}\\ChunkData\\{screen.seed}\\{position.Item1}.{position.Item2}.txt", false))
                {
                    string stringo;
                    if (creaturesSpawned) { stringo = "1;\n"; }
                    else { stringo = "0;\n"; }
                    foreach (Entity entity in entityList)
                    {
                        stringo += entity.posX + ";" + entity.posY + ";";
                        stringo += entity.type + ";";
                        stringo += entity.color.R + ";" + entity.color.G + ";" + entity.color.B + ";";
                    }
                    stringo += "\n";
                    foreach (Plant plant in plantList)
                    {
                        stringo += plant.posX + ";" + plant.posY + ";";
                        stringo += plant.type + ";";
                        stringo += plant.seed + ";";
                    }
                    stringo += "\n";
                    if (modificationCount == 0)
                    {
                        stringo += "x\n";
                    }
                    else
                    {
                        foreach (int into in fillStates)
                        {
                            stringo += (Convert.ToInt32(into)).ToString() + ";";
                        }
                        stringo += "\n";
                    }
                    f.Write(stringo);
                }
            }
            public void moveLiquids()
            {
                (int, int) chunkCoords = screen.findChunkScreenRelativeIndex((int)position.Item1 * 16 - 16, (int)position.Item2 * 16);
                Chunk leftChunk = screen.loadedChunks[chunkCoords.Item1, chunkCoords.Item2];
                chunkCoords = screen.findChunkScreenRelativeIndex((int)position.Item1 * 16 - 16, (int)position.Item2 * 16 + 16);
                Chunk bottomLeftChunk = screen.loadedChunks[chunkCoords.Item1, chunkCoords.Item2];
                chunkCoords = screen.findChunkScreenRelativeIndex((int)position.Item1 * 16, (int)position.Item2 * 16 + 16);
                Chunk bottomChunk = screen.loadedChunks[chunkCoords.Item1, chunkCoords.Item2];
                chunkCoords = screen.findChunkScreenRelativeIndex((int)position.Item1 * 16 + 16, (int)position.Item2 * 16 + 16);
                Chunk bottomRightChunk = screen.loadedChunks[chunkCoords.Item1, chunkCoords.Item2];
                chunkCoords = screen.findChunkScreenRelativeIndex((int)position.Item1 * 16 + 16, (int)position.Item2 * 16);
                Chunk rightChunk = screen.loadedChunks[chunkCoords.Item1, chunkCoords.Item2];

                int jStart;
                if (this.position.Item2 > bottomChunk.position.Item2) { jStart = 14;} // if it is the on the lowest line of the chunks loaded, don't test for bottom row of pixels (teleportation issue).
                else { jStart = 15;}

                for (int j = jStart; j >= 0; j--)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        moveOneLiquid(i, j, leftChunk, bottomLeftChunk, bottomChunk, bottomRightChunk, rightChunk);
                    }
                }
            }
            public void moveOneLiquid(int i, int j, Chunk leftChunk, Chunk bottomLeftChunk, Chunk bottomChunk, Chunk bottomRightChunk, Chunk rightChunk)
            {
                Chunk leftTestPositionChunk;
                Chunk leftDiagTestPositionChunk;
                Chunk middleTestPositionChunk;
                Chunk rightTestPositionChunk;
                Chunk rightDiagTestPositionChunk;

                int jb = (j + 1) % 16;
                int il = (i + 15) % 16;
                int ir = (i + 1) % 16;

                if (j == 15)
                {
                    middleTestPositionChunk = bottomChunk;
                    if (i == 0)
                    {
                        leftTestPositionChunk = leftChunk;
                        leftDiagTestPositionChunk = bottomLeftChunk;
                        rightDiagTestPositionChunk = bottomChunk;
                        rightTestPositionChunk = this;
                    }
                    else if (i == 15)
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
                    else if (i == 15)
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

                if ((j < 15 || middleTestPositionChunk.position.Item2 > this.position.Item2) && fillStates[i, j] < 0)
                {
                    if (middleTestPositionChunk.fillStates[i, jb] == 0)
                    {
                        middleTestPositionChunk.fillStates[i, jb] = fillStates[i, j];
                        fillStates[i, j] = 0;
                        findTileColor(i, j);
                        middleTestPositionChunk.findTileColor(i, jb);
                        goto endOfTest;
                    } // THIS ONE WAS FUCKING BUGGYYYYY BRUH
                    if ((i < 15 || middleTestPositionChunk.position.Item1 < rightTestPositionChunk.position.Item1) && (rightTestPositionChunk.fillStates[ir, j] == 0 || middleTestPositionChunk.fillStates[i, jb] < 0) && rightDiagTestPositionChunk.fillStates[ir, jb] == 0)
                    {
                        rightDiagTestPositionChunk.fillStates[ir, jb] = fillStates[i, j];
                        fillStates[i, j] = 0;
                        findTileColor(i, j);
                        rightDiagTestPositionChunk.findTileColor(ir, jb);
                        goto endOfTest;
                    } //this ONE WAS BUGGY
                    if ((rightTestPositionChunk.fillStates[ir, j] == 0 || middleTestPositionChunk.fillStates[i, jb] < 0) && rightDiagTestPositionChunk.fillStates[ir, jb] < 0)
                    {
                        if (testLiquidPushRight(i, j)){ goto endOfTest; }
                    }
                    if ((i > 0 || leftTestPositionChunk.position.Item1 < middleTestPositionChunk.position.Item1) && (leftTestPositionChunk.fillStates[il, j] == 0 || middleTestPositionChunk.fillStates[i, jb] < 0) && leftDiagTestPositionChunk.fillStates[il, jb] == 0)
                    {
                        leftDiagTestPositionChunk.fillStates[il, jb] = fillStates[i, j];
                        fillStates[i, j] = 0;
                        findTileColor(i, j);
                        leftDiagTestPositionChunk.findTileColor(il, jb);
                        goto endOfTest;
                    } // THIS ONE WAS ALSO BUGGY
                    if ((leftTestPositionChunk.fillStates[il, j] == 0 || middleTestPositionChunk.fillStates[i, jb] < 0) && leftDiagTestPositionChunk.fillStates[il, jb] < 0)
                    {
                        if (testLiquidPushLeft(i, j)){ goto endOfTest; }
                    }

                    endOfTest:;
                }
            }
            bool testLiquidPushRight(int i, int j)
            {
                int iTested = i;
                int jTested = j+1;

                (int, int) AbsoluteChunkCoords = screen.findChunkAbsoluteIndex((int)position.Item1 * 16, (int)position.Item2 * 16);
                int absChunkX = AbsoluteChunkCoords.Item1;
                int absChunkY = AbsoluteChunkCoords.Item2;
                if (jTested >= 16) { jTested -= 16; absChunkY++; }
                (int, int) chunkCoords = screen.findChunkScreenRelativeIndex(absChunkX * 16, absChunkY * 16);
                Chunk chunkToTest = screen.loadedChunks[chunkCoords.Item1, chunkCoords.Item2];

                int repeatCounter = 0;
                while(repeatCounter < 500)
                {
                    iTested++;
                    if(iTested > 15)
                    {
                        absChunkX++;
                        chunkCoords = screen.findChunkScreenRelativeIndex(absChunkX * 16, absChunkY * 16);
                        chunkToTest = screen.loadedChunks[chunkCoords.Item1, chunkCoords.Item2];
                        iTested -= 16;
                    }
                    if (absChunkX >= screenChunkX + screen.chunkResolution)
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
                        return true;
                    }
                    repeatCounter++;
                }
                return false;
            }
            bool testLiquidPushLeft(int i, int j)
            {
                int iTested = i;
                int jTested = j+1;

                (int, int) AbsoluteChunkCoords = screen.findChunkAbsoluteIndex((int)position.Item1 * 16, (int)position.Item2 * 16);
                int absChunkX = AbsoluteChunkCoords.Item1;
                int absChunkY = AbsoluteChunkCoords.Item2;
                if (jTested >= 16) { jTested -= 16; absChunkY++; }
                (int, int) chunkCoords = screen.findChunkScreenRelativeIndex(absChunkX * 16, absChunkY * 16);
                Chunk chunkToTest = screen.loadedChunks[chunkCoords.Item1, chunkCoords.Item2];

                int repeatCounter = 0;
                while (repeatCounter < 500)
                {
                    iTested--;
                    if (iTested < 0)
                    {
                        absChunkX--;
                        chunkCoords = screen.findChunkScreenRelativeIndex(absChunkX * 16, absChunkY * 16);
                        chunkToTest = screen.loadedChunks[chunkCoords.Item1, chunkCoords.Item2];
                        iTested += 16;
                    }
                    if (absChunkX < screenChunkX)
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
                        return true;
                    }
                    repeatCounter++;
                }
                return false;
            }
        }
        public class Screen
        {
            public Chunk[,] loadedChunks;
            public List<long>[,] LCGCacheListMatrix;
            public int chunkResolution;
            public long seed;
            public int loadedChunkOffsetX;
            public int loadedChunkOffsetY;
            public bool isPngToBeExported;
            public Bitmap gameBitmap;
            public Bitmap overlayBitmap;
            public List<Player> playerList = new List<Player>();
            public List<Entity> activeEntities = new List<Entity>();
            public List<Entity> entitesToRemove = new List<Entity>();
            public List<Plant> activePlants = new List<Plant>();

            public Screen(long posX, long posY, int chunkResolutionToPut, long seedo, bool isPngToExport)
            {
                loadedChunkOffsetX = 0; //(((int)posX %chunkResolutionToPut) + chunkResolutionToPut) %chunkResolutionToPut;
                loadedChunkOffsetY = 0; //(((int)posY %chunkResolutionToPut) + chunkResolutionToPut) %chunkResolutionToPut;
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
                if (!Directory.Exists($"{currentDirectory}\\bitmapos"))
                {
                    Directory.CreateDirectory($"{currentDirectory}\\bitmapos");
                }
                LCGCacheInit();
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
            public void loadChunks(long posX, long posY, long seed)
            {
                loadedChunks = new Chunk[chunkResolution, chunkResolution];
                for (int i = 0; i < chunkResolution; i++)
                {
                    for (int j = 0; j < chunkResolution; j++)
                    {
                        loadedChunks[(i + chunkResolution) % chunkResolution, (j + chunkResolution) % chunkResolution] = new Chunk(posX + i, posY + j, seed, false, this);
                    }
                }
                if (isPngToBeExported) { gameBitmap = new Bitmap(16 * (chunkResolution - 1), 16 * (chunkResolution - 1)); }
                else { gameBitmap = new Bitmap(64 * (ChunkLength - 1), 64 * (ChunkLength - 1)); }
            }
            public void updateLoadedChunks(int posX, int posY, long seed, int screenSlideX, int screenSlideY)
            {

                // Gone ! Forever now ! it's. fucking. BACKKKKKK     !! ! !! GONE AGAIN fuck it's backkkk WOOOOOOOOOHOOOOOOOOOOOO BUG IS GONE !!! It's 4am !!!! FUCK !!!! PROBLEM !!!!! The update loaded chuncks is lagging 8 (7?) chunkcs behind the actual normal loading... but only in the updated dimension

                (int, int) chunkIndex;
                Chunk chunk;
                foreach (Chunk chunko in loadedChunks)
                {
                    chunko.entityList = new List<Entity>();
                    chunko.plantList = new List<Plant>();
                }
                while (activeEntities.Count() > 0)
                {
                    chunkIndex = findChunkScreenRelativeIndex(activeEntities[0].posX, activeEntities[0].posY);
                    chunk = loadedChunks[chunkIndex.Item1, chunkIndex.Item2];
                    chunk.entityList.Add(activeEntities[0]);
                    activeEntities.RemoveAt(0);
                }
                while (activePlants.Count() > 0)
                {
                    chunkIndex = findChunkScreenRelativeIndex(activePlants[0].posX, activePlants[0].posY);
                    chunk = loadedChunks[chunkIndex.Item1, chunkIndex.Item2];
                    chunk.plantList.Add(activePlants[0]);
                    activePlants.RemoveAt(0);
                }

                while (screenSlideX > 0)
                {
                    for (int j = 0; j < chunkResolution; j++)
                    {
                        loadedChunks[loadedChunkOffsetX, (loadedChunkOffsetY + j) % chunkResolution].saveChunk(true);
                        loadedChunks[loadedChunkOffsetX, (loadedChunkOffsetY + j) % chunkResolution] = new Chunk((posX - screenSlideX) + chunkResolution, (posY - screenSlideY) + j, seed, false, this);
                    }
                    loadedChunkOffsetX = (loadedChunkOffsetX + 1) % chunkResolution;
                    screenSlideX -= 1;
                }
                while (screenSlideX < 0)
                {
                    for (int j = 0; j < chunkResolution; j++)
                    {
                        loadedChunks[(loadedChunkOffsetX + chunkResolution - 1) % chunkResolution, (loadedChunkOffsetY + j) % chunkResolution].saveChunk(true);
                        loadedChunks[(loadedChunkOffsetX + chunkResolution - 1) % chunkResolution, (loadedChunkOffsetY + j) % chunkResolution] = new Chunk((posX - screenSlideX) - 1, (posY - screenSlideY) + j, seed, false, this);
                    }
                    loadedChunkOffsetX = (loadedChunkOffsetX + chunkResolution - 1) % chunkResolution;
                    screenSlideX += 1;
                }
                while (screenSlideY > 0)
                {
                    for (int i = 0; i < chunkResolution; i++)
                    {
                        loadedChunks[(loadedChunkOffsetX + i) % chunkResolution, loadedChunkOffsetY].saveChunk(true);
                        loadedChunks[(loadedChunkOffsetX + i) % chunkResolution, loadedChunkOffsetY] = new Chunk((posX - screenSlideX) + i, (posY - screenSlideY) + chunkResolution, seed, false, this);
                    }
                    loadedChunkOffsetY = (loadedChunkOffsetY + 1) % chunkResolution;
                    screenSlideY -= 1;
                }
                while (screenSlideY < 0)
                {
                    for (int i = 0; i < chunkResolution; i++)
                    {
                        loadedChunks[(loadedChunkOffsetX + i) % chunkResolution, (loadedChunkOffsetY + chunkResolution - 1) % chunkResolution].saveChunk(true);
                        loadedChunks[(loadedChunkOffsetX + i) % chunkResolution, (loadedChunkOffsetY + chunkResolution - 1) % chunkResolution] = new Chunk((posX - screenSlideX) + i, (posY - screenSlideY) - 1, seed, false, this);
                    }
                    loadedChunkOffsetY = (loadedChunkOffsetY + chunkResolution - 1) % chunkResolution;
                    screenSlideY += 1;
                }

                foreach (Chunk chunko in loadedChunks)
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
                int chunkPosX = Floor(pixelPosX, 16) / 16;
                int chunkPosY = Floor(pixelPosY, 16) / 16;
                return (chunkPosX, chunkPosY);
            }
            public (int, int) findChunkScreenRelativeIndex(int pixelPosX, int pixelPosY)
            {
                int chunkPosX = Floor(pixelPosX, 16) / 16;
                int chunkPosY = Floor(pixelPosY, 16) / 16;
                chunkPosX = ((chunkPosX % chunkResolution) + 2 * chunkResolution + 0 * loadedChunkOffsetX) % chunkResolution;
                chunkPosY = ((chunkPosY % chunkResolution) + 2 * chunkResolution + 0 * loadedChunkOffsetY) % chunkResolution;
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
                        Structure newStructure = new Structure(posX * 512 + 16 + (int)(seedX % 480), posY * 512 + 16 + (int)(seedY % 480), seedX, seedY, false, (posX, posY), this);
                        newStructure.drawStructure();
                        newStructure.imprintChunks();
                        newStructure.saveInFile();
                    }
                    long waterLakesAmount = 15 + (seedX + seedY) % 150;
                    for (int i = 0; i < waterLakesAmount; i++)
                    {
                        seedX = LCGyNeg(seedX); // on porpoise x    /\_/\
                        seedY = LCGxNeg(seedY); // and y switched  ( ^o^ )
                        Structure newStructure = new Structure(posX * 512 + 16 + (int)(seedX % 480), posY * 512 + 16 + (int)(seedY % 480), seedX, seedY, true, (posX, posY), this);
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
                        pixelPosX = ((i*16 + (-loadedChunkOffsetX + chunkResolution) * 16) % (chunkResolution * 16)) - ((camPosX % 16) + 16) % 16 - UnloadedChunksAmount * 16;
                        pixelPosY = ((j*16 + (-loadedChunkOffsetY + chunkResolution) * 16) % (chunkResolution * 16)) - ((camPosY % 16) + 16) % 16 - UnloadedChunksAmount * 16;

                        if (pixelPosX < -15 || pixelPosX >= (chunkResolution) * 16 || pixelPosY < -15 || pixelPosY >= (chunkResolution) * 16)
                        {
                            continue;
                        }

                        Color color = loadedChunks[i / 16, j / 16].colors[i % 16, j % 16];

                        /*using (var g = Graphics.FromImage(gameBitmap))
                        {
                            g.FillRectangle(new SolidBrush(color), (pixelPosX)*PNGmultiplicator, (pixelPosY)*PNGmultiplicator, PNGmultiplicator, PNGmultiplicator);
                        }*/

                        using (Graphics g = Graphics.FromImage(gameBitmap))
                        {
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                            g.DrawImage(loadedChunks[i, j].bitmap, pixelPosX*PNGmultiplicator, pixelPosY*PNGmultiplicator, 16*PNGmultiplicator, 16*PNGmultiplicator);
                        }
                    }
                }

                foreach (Plant plant in activePlants)
                {
                    pixelPosX = plant.posX - camPosX - UnloadedChunksAmount * 16;
                    pixelPosY = plant.posY - camPosY - UnloadedChunksAmount * 16;

                    if (pixelPosX >= 0 && pixelPosX < (chunkResolution - 1) * 16 && pixelPosY >= 0 && pixelPosY < (chunkResolution - 1) * 16)
                    {;
                        (int, int) chunkRelativePos = this.findChunkScreenRelativeIndex(plant.posX, plant.posY);
                        Chunk chunkToTest = this.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                        if ( true/*chunkToTest.fillStates[(plant.posX % 16 + 16) % 16, (plant.posY % 16 + 16) % 16] <= 0*/)
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
                    pixelPosX = entity.posX - camPosX - UnloadedChunksAmount * 16;
                    pixelPosY = entity.posY - camPosY - UnloadedChunksAmount * 16;

                    if (pixelPosX >= 0 && pixelPosX < (chunkResolution - 1) * 16 && pixelPosY >= 0 && pixelPosY < (chunkResolution - 1) * 16)
                    {
                        Color color = entity.color;
                        (int, int) chunkRelativePos = this.findChunkScreenRelativeIndex(entity.posX, entity.posY);
                        Chunk chunkToTest = this.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                        if (chunkToTest.fillStates[(entity.posX % 16 + 16) % 16, (entity.posY % 16 + 16) % 16] > 0)
                        {
                            color = Color.Red;
                        }
                        using (var g = Graphics.FromImage(gameBitmap))
                        {
                            g.FillRectangle(new SolidBrush(color), pixelPosX * PNGmultiplicator, pixelPosY * PNGmultiplicator, PNGmultiplicator, PNGmultiplicator);
                        }
                    }
                }

                pixelPosX = player.posX - camPosX - UnloadedChunksAmount * 16;
                pixelPosY = player.posY - camPosY - UnloadedChunksAmount * 16;

                if (pixelPosX >= 0 && pixelPosX < (chunkResolution - 1) * 16 && pixelPosY >= 0 && pixelPosY < (chunkResolution - 1) * 16)
                {
                    Color color = Color.Green;
                    (int, int) chunkRelativePos = this.findChunkScreenRelativeIndex(player.posX, player.posY);
                    Chunk chunkToTest = this.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                    if (chunkToTest.fillStates[(player.posX % 16 + 16) % 16, (player.posY % 16 + 16) % 16] > 0)
                    {
                        color = Color.Red;
                    }
                    using (var g = Graphics.FromImage(gameBitmap))
                    {
                        g.FillRectangle(new SolidBrush(color), pixelPosX * PNGmultiplicator, pixelPosY * PNGmultiplicator, PNGmultiplicator, PNGmultiplicator);
                    }
                }

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
                        gameBitmap = new Bitmap(64 * (ChunkLength - 1), 64 * (ChunkLength - 1));
                    }
                }
                else
                {
                    if (UnloadedChunksAmount > 1)
                    {
                        ChunkLength += 2;
                        UnloadedChunksAmount--;
                        gameBitmap = new Bitmap(64 * (ChunkLength - 1), 64 * (ChunkLength - 1));
                    }

                }
                lastZoom = timeElapsed;
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

            public Dictionary<(int index, bool isEntity), int> inventoryQuantities;
            public List<(int index, bool isEntity)> inventoryElements;
            public int inventoryCursor = 1;
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
                    int randX = rand.Next((ChunkLength - 1) * 16);
                    int randY = rand.Next((ChunkLength - 1) * 16);
                    Chunk randChunk = screen.loadedChunks[randX / 16, randY / 16];
                    if (randChunk.fillStates[randX % 16, randY % 16] == 0)
                    {
                        posX = randX;
                        realPosX = randX;
                        posY = randY;
                        realPosY = randY;
                        break;
                    }
                    counto++;
                }
                inventoryQuantities = new Dictionary<(int index, bool isEntity), int>
                {
                    {(0, true), -999 },
                    {(1, true), -999 },
                    {(2, true), -999 },
                    {(3, true), -999 },
                    {(-1, false), -999 }
                };
                inventoryElements = new List<(int index, bool isEntity)>
                {
                    (0, true),
                    (1, true),
                    (2, true),
                    (3, true),
                    (-1, false)
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
                        Dig(posX, posY + 1);
                    }
                    else if (arrowKeysState[3] && !arrowKeysState[2])
                    {
                        Dig(posX, posY - 1);
                    }
                }
                if ((placePress[0] || placePress[1]) && timeElapsed > timeAtLastPlace + 0.01f)
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
                        Place(posX, posY + 1);
                    }
                    else if (arrowKeysState[3] && !arrowKeysState[2])
                    {
                        Place(posX, posY - 1);
                    }
                }
                {
                    (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posX, posY);
                    if (screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2].fillStates[(posX % 16 + 32) % 16, (posY % 16 + 32) % 16] < 0)
                    {
                        speedX = speedX * 0.8f - Sign(speedX)*Sqrt(Max((int)speedX-1,0));
                        speedY = speedY * 0.8f - Sign(speedY)*Sqrt(Max((int)speedY-1,0));
                    }
                }
                /*while (Abs(toMoveX) > 0)
                {
                    (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posX + Sign(toMoveX), posY);
                    Chunk chunkToTest = screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                    if (chunkToTest.fillStates[(posX % 16 + 32 + Sign(toMoveX)) % 16, (posY % 16 + 32) % 16] <= 0)
                    {
                        posX += Sign(toMoveX);
                        realPosX += Sign(toMoveX);
                        toMoveX = Sign(toMoveX) * (Abs(toMoveX) - 1);
                    }
                    else
                    {
                        speedX = 0;
                        break;
                    }
                }
                while (Abs(toMoveY) > 0)
                {
                    (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posX, posY + Sign(toMoveY));
                    Chunk chunkToTest = screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                    if (chunkToTest.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 32 + Sign(toMoveY)) % 16] <= 0)
                    {
                        posY += Sign(toMoveY);
                        realPosY += Sign(toMoveY);
                        toMoveY = Sign(toMoveY) * (Abs(toMoveY) - 1);
                    }
                    else
                    {
                        speedY = 0;
                        break;
                    }
                }*/


                float toMoveX = speedX;
                float toMoveY = speedY;

                while (Abs(toMoveY) > 0)
                {
                    (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posX, posY + (int)Sign(toMoveY));
                    (int, int) chunkAbsolutePos = screen.findChunkAbsoluteIndex(posX, posY + (int)Sign(toMoveY));
                    Chunk chunkToTest = screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                    if (chunkToTest.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 32 + (int)Sign(toMoveY)) % 16] <= 0)
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
                while (Abs(toMoveX) > 0)
                {
                    (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posX + (int)Sign(toMoveX), posY);
                    (int, int) chunkAbsolutePos = screen.findChunkAbsoluteIndex(posX + (int)Sign(toMoveX), posY);
                    Chunk chunkToTest = screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                    if (chunkToTest.fillStates[(posX % 16 + 32 + (int)Sign(toMoveX)) % 16, (posY % 16 + 32) % 16] <= 0)
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
                (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posToDigX, posToDigY);
                Chunk chunkToDig = screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                int tileContent = chunkToDig.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16];
                if (tileContent != 0)
                {
                    (int index, bool isEntity)[] inventoryKeys = inventoryQuantities.Keys.ToArray();
                    for (int i = 0; i < inventoryKeys.Length; i++)
                    {
                        if (inventoryKeys[i].index == tileContent && !inventoryKeys[i].isEntity)
                        {
                            if (inventoryQuantities[(tileContent, false)] != -999)
                            {
                                inventoryQuantities[(tileContent, false)]++;
                            }
                            goto AfterTest;
                        }
                    }
                    // there was none of the thing present in the inventory already so gotta create it
                    inventoryQuantities.Add((tileContent, false), 1);
                    inventoryElements.Add((tileContent, false));
                    AfterTest:;
                    chunkToDig.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16] = 0;
                    chunkToDig.findTileColor((posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16);
                    chunkToDig.modificationCount += 1;
                    timeAtLastDig = timeElapsed;
                }
            }
            public void Place(int posToDigX, int posToDigY)
            {
                (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posToDigX, posToDigY);
                Chunk chunkToDig = screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                (int index, bool isEntity) tileContent = inventoryElements[inventoryCursor];
                int tileState = chunkToDig.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16];
                if (tileState == 0 || tileState < 0 && tileContent.isEntity)
                {
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
                    if(tileContent.isEntity)
                    {
                        Entity newEntity = new Entity(chunkToDig, (posToDigX, posToDigY), tileContent.index, screen);
                        screen.activeEntities.Add(newEntity);
                        timeAtLastPlace = timeElapsed;
                    }
                    else
                    {
                        chunkToDig.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16] = tileContent.index;
                        chunkToDig.findTileColor((posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16);
                        chunkToDig.modificationCount += 1;
                        timeAtLastPlace = timeElapsed;
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
                    (int index, bool isEntity) element = inventoryElements[inventoryCursor];
                    if (element.isEntity)
                    {
                        Sprites.drawSpriteOnCanvas(screen.overlayBitmap, entitySprites[element.index].bitmap, (340, 64), 4, true);
                    }
                    else
                    {
                        Sprites.drawSpriteOnCanvas(screen.overlayBitmap, compoundSprites[element.index].bitmap, (340, 64), 4, true);
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
            //RunGame(new Screen(0, 0, 0), rand);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            currentDirectory = System.IO.Directory.GetCurrentDirectory();

            turnPngIntoString("OverlayBackground");
            turnPngIntoString("Numbers");
            turnPngIntoString("BasicTile");
            turnPngIntoString("Fairy");
            turnPngIntoString("Frog");
            turnPngIntoString("Fish");
            turnPngIntoString("Hornet");
            turnPngIntoString("Piss");
            turnPngIntoString("Water");
            turnPngIntoString("FairyLiquid");
            turnPngIntoString("Lava");
            turnPngIntoString("Honey");

            loadSpriteDictionaries();

            Screen mainScreen;

            bool updatePNG = true;
            int PNGsize = 100; // in chunks
            bool randomSeed = true;

            long seed = 1809684240;

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
                int oldChunkRes;
                int rando = -10;
                camPosX = -50 * 16;
                camPosY = rando * 16;
                mainScreen = new Screen(-PNGsize / 2, -PNGsize / 2, PNGsize, seed, true);
                mainScreen.updateScreen();
                Bitmap bmp = mainScreen.gameBitmap;
                Bitmap bmp2 = new Bitmap(512, 512);

                mainScreen.updateScreen().Save($"{currentDirectory}\\cavee.png");
                bmp2.Save($"{currentDirectory}\\caveNoise.png");
            }

            mainScreen = new Screen(screenChunkX, screenChunkY, ChunkLength, seed, false);
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
            //return ((int)(seedX%512)-256, (int)(seedY%512)-256);
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
            int seedXY = (int)((2048 + seedX % 256 + seedY % 256) % 256);
            long seedZ = Abs(3 + posX + posY * 11);
            int z = seedXY;
            while (z > 0)
            {
                seedZ = LCGz(seedZ);
                z--;
            }
            return (int)((seedZ + seedXY) % 256);
            //return ((int)(seedX%512)-256, (int)(seedY%512)-256);
        }
        public static int findSecondaryNoiseValue(Chunk chunk, int varX, int varY, int layer)
        {
            int modulo = 32;
            int modX = (int)((chunk.position.Item1 * 16 + varX) % modulo + modulo) % modulo;
            int modY = (int)((chunk.position.Item2 * 16 + varY) % modulo + modulo) % modulo;
            int[,] values = chunk.primaryFillValues;
            int fX1 = values[layer, 0] * (modulo - modX) + values[layer, 1] * modX;
            int fX2 = values[layer, 2] * (modulo - modX) + values[layer, 3] * modX;
            int fY = fX1 * (modulo - modY) + fX2 * modY;
            return fY / (modulo * modulo);
        }
        public static int findSecondaryBiomeValue(Chunk chunk, int varX, int varY, int layer)
        {
            int modulo = 256;
            int modX = (int)((chunk.position.Item1 * 16 + varX) % modulo + modulo) % modulo;
            int modY = (int)((chunk.position.Item2 * 16 + varY) % modulo + modulo) % modulo;
            int[,] values = chunk.primaryBiomeValues;
            int fX1 = values[layer, 0] * (modulo - modX) + values[layer, 1] * modX;
            int fX2 = values[layer, 2] * (modulo - modX) + values[layer, 3] * modX;
            int fY = fX1 * (modulo - modY) + fX2 * modY;
            return fY / (modulo * modulo);
        }
        public static int findSecondaryBigBiomeValue(Chunk chunk, int varX, int varY, int layer)
        {
            int modulo = 1024;
            int modX = (int)((chunk.position.Item1 * 16 + varX) % modulo + modulo) % modulo;
            int modY = (int)((chunk.position.Item2 * 16 + varY) % modulo + modulo) % modulo;
            int[,] values = chunk.primaryBigBiomeValues;
            int fX1 = values[layer, 0] * (modulo - modX) + values[layer, 1] * modX;
            int fX2 = values[layer, 2] * (modulo - modX) + values[layer, 3] * modX;
            int fY = fX1 * (modulo - modY) + fX2 * modY;
            return fY / (modulo * modulo);
        }
        public static (int, int)[] findBiome(int[,,] values, int[,,] bigBiomeValues, int posX, int posY)
        {
            // arrite so... 0 is temperature, 1 is humidity, 2 is acidity, 3 is toxicity, 4 is terrain modifier1, 5 is terrain modifier 2
            int temperature = values[posX, posY, 0] + bigBiomeValues[posX, posY, 0]-128;
            int humidity = values[posX, posY, 1] + bigBiomeValues[posX, posY, 1]-128;
            int acidity = values[posX, posY, 2] + bigBiomeValues[posX, posY, 2]-128;
            int toxicity = values[posX, posY, 3] + bigBiomeValues[posX, posY, 3]-128;
            List<(int, int)> listo = new List<(int, int)>();
            int percentageFree = 100;

            if (temperature > 180)
            {
                int hotness = Min((temperature - 180) * 10, 100);
                if (temperature > 210 && humidity > 150)
                {
                    int minimo = Min(temperature - 210, humidity - 150);
                    int obsidianess = minimo * 4;
                    obsidianess = Min(obsidianess, 100);
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
            else if (temperature < 110)
            {
                int coldness = Min((110 - temperature) * 10, 100);
                if (temperature < 0)
                {
                    int bigColdness = (int)(Min((0 - temperature) * 10, 100) * coldness * 0.01f);
                    coldness -= bigColdness;
                    if (bigColdness > 0)
                    {
                        listo.Add((7, bigColdness));
                        percentageFree -= bigColdness;
                    }
                }
                int savedColdness = (int)(Max(0,(Min((30 - temperature) * 10, 100))));
                savedColdness = Min(savedColdness, coldness);
                coldness -= savedColdness;
                if (acidity < 110)
                {
                    int acidness = (int)(Min((110 - acidity) * 10, 100) * coldness * 0.01f);
                    coldness -= acidness;
                    listo.Add((1, acidness));
                    percentageFree -= acidness;
                }
                if (humidity > toxicity)
                {
                    int fairyness = (int)(Min((humidity - toxicity) * 10, 100) * coldness * 0.01f);
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

            if (percentageFree > 0)
            {
                int slimeness = (int)(Clamp((toxicity - humidity + 5) * 10, 0, 100) * percentageFree * 0.01f);
                int forestness = (int)(Clamp((humidity - toxicity + 5) * 10, 0, 100) * percentageFree * 0.01f);
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
            if (arrowKeysState[2]) { player.speedY += 0.5f; }
            if (arrowKeysState[3]) { player.speedY -= 1; }
            player.speedY += 0.5f;
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


            int posDiffX = player.posX - (camPosX + 8 * (screen.chunkResolution - 1)); //*2 is needed cause there's only *8 and not *16 before
            int posDiffY = player.posY - (camPosY + 8 * (screen.chunkResolution - 1));
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
            int oldChunkX = screenChunkX;
            int oldChunkY = screenChunkY;
            screenChunkX = Floor(camPosX, 16) / 16;
            screenChunkY = Floor(camPosY, 16) / 16;
            int chunkVariationX = screenChunkX - oldChunkX;
            int chunkVariationY = screenChunkY - oldChunkY;
            if (chunkVariationX != 0 || chunkVariationY != 0)
            {
                screen.updateLoadedChunks(screenChunkX, screenChunkY, screen.seed, chunkVariationX, chunkVariationY);
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
            int ii;
            int jj;
            for(int j = screen.chunkResolution - 1; j >= 0; j--)
            {
                for (int i = 0; i < screen.chunkResolution; i++)
                {
                    ii = (i + screen.loadedChunkOffsetX + screen.chunkResolution) % (screen.chunkResolution);
                    jj = (j + screen.loadedChunkOffsetY + screen.chunkResolution) % (screen.chunkResolution);
                    screen.loadedChunks[ii, jj].moveLiquids();
                }
            }
            gamePictureBox.Image = screen.updateScreen();
            gamePictureBox.Refresh();
            overlayPictureBox.Image = screen.overlayBitmap;
            Sprites.drawSpriteOnCanvas(screen.overlayBitmap, overlayBackground.bitmap, (0, 0), 4, false);
            player.drawInventory();
            overlayPictureBox.Refresh();
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
    }
}