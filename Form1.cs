using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace Cave
{
    public partial class Form1 : Form
    {
        public class Globals
        {
            public static int ChunkLength = 6;
            public static int UnloadedChunksAmount = 2;

            public static Random rand = new Random();
            public static Player player = new Player();

            public static bool[] arrowKeysState = { false, false, false, false };
            public static bool digPress = false;
            public static bool[] placePress = { false, false };
            public static bool[] zoomPress = { false, false };
            public static bool shiftPress = false;
            public static float lastZoom = 0;
            public static DateTime timeAtLauch;
            public static float timeElapsed;

            public static string currentDirectory;

            public static float realCamPosX = 0;
            public static float realCamPosY = 0;
            public static int camPosX = 0;
            public static int camPosY = 0;
            public static int chunkX = 0;
            public static int chunkY = 0;

            public static float accCamX = 0;
            public static float accCamY = 0;
            public static float speedCamX = 0;
            public static float speedCamY = 0;

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
        }
        public class CaveSystem
        {
            public long seed;
        }
        public class Chunk
        {
            public Screen screen;

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
            public int modificationCount = 0;
            public int liquidCount = 0;
            public bool isUnstable = false;
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

                }
                else if (System.IO.File.Exists($"{currentDirectory}\\ChunkData\\{seed}\\{position.Item1}.{position.Item2}.txt"))
                {
                    loadChunk(screen);
                }
                else { spawnEntites(screen); }

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
                    colorArray[0] = (int)(colorArray[0]*0.8f) + 60;
                    colorArray[1] = (int)(colorArray[1]*0.8f) + 60;
                    colorArray[2] = (int)(colorArray[2]*0.8f) + 100;
                }
                colors[i, j] = Color.FromArgb(ColorClamp(colorArray[0]), ColorClamp(colorArray[1]), ColorClamp(colorArray[2]));
            }
            public void spawnEntites(Screen screen)
            {
                if (secondaryFillValues[0, 0, 0] % 23 > -15)
                {
                    Entity newEntity = new Entity(this);
                    screen.activeEntites.Add(newEntity);
                }
            }
            public void loadChunk(Screen screen)
            {
                using (StreamReader f = new StreamReader($"{currentDirectory}\\ChunkData\\{screen.seed}\\{position.Item1}.{position.Item2}.txt"))
                {
                    string line = f.ReadLine();
                    if (line != null && line.Length > 0 && line[line.Length - 1] == ';')
                    {
                        if (line[0] == '0')
                        {
                            spawnEntites(screen);
                        }
                    }
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
                        for (int i = 0; i < listo.Count() / 6; i++)
                        {
                            int posXt = Int32.Parse(listo[i * 6]);
                            int posYt = Int32.Parse(listo[i * 6 + 1]);
                            int typet = Int32.Parse(listo[i * 6 + 2]);
                            int rt = Int32.Parse(listo[i * 6 + 3]);
                            int gt = Int32.Parse(listo[i * 6 + 4]);
                            int bt = Int32.Parse(listo[i * 6 + 5]);
                            screen.activeEntites.Add(new Entity(posXt, posYt, typet, rt, gt, bt));
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
                }
            }
            public void saveChunk(Screen screen)
            {
                using (StreamWriter f = new StreamWriter($"{currentDirectory}\\ChunkData\\{screen.seed}\\{position.Item1}.{position.Item2}.txt", false))
                {
                    string stringo = "1;\n";
                    foreach (Entity entity in entityList)
                    {
                        stringo += entity.posX + ";" + entity.posY + ";";
                        stringo += entity.type + ";";
                        stringo += entity.color.R + ";" + entity.color.G + ";" + entity.color.B + ";";
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
            public void moveLiquids(Screen screen)
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

                for (int j = 15; j >= 0; j--)
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
                Chunk middleTestPositionChunk;
                Chunk rightTestPositionChunk;

                int jb = (j + 1) % 16;
                int il = (i + 15) % 16;
                int ir = (i + 1) % 16;

                if (j == 15)
                {
                    middleTestPositionChunk = bottomChunk;
                    if (i == 0)
                    {
                        leftTestPositionChunk = bottomLeftChunk;
                        rightTestPositionChunk = bottomChunk;
                    }
                    else if (i == 15)
                    {
                        leftTestPositionChunk = bottomChunk;
                        rightTestPositionChunk = bottomRightChunk;
                    }
                    else
                    {
                        leftTestPositionChunk = bottomChunk;
                        rightTestPositionChunk = bottomChunk;
                    }
                }
                else
                {
                    middleTestPositionChunk = this;
                    if (i == 0)
                    {
                        leftTestPositionChunk = leftChunk;
                        rightTestPositionChunk = this;
                    }
                    else if (i == 15)
                    {
                        leftTestPositionChunk = this;
                        rightTestPositionChunk = rightChunk;
                    }
                    else
                    {
                        leftTestPositionChunk = this;
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
                        goto endOfTest;
                    }
                    if (rightTestPositionChunk.fillStates[ir, jb] == 0)
                    {
                        rightTestPositionChunk.fillStates[ir, jb] = fillStates[i, j];
                        fillStates[i, j] = 0;
                        findTileColor(i, j);
                        rightTestPositionChunk.findTileColor(ir, jb);
                        goto endOfTest;
                    }
                    if (rightTestPositionChunk.fillStates[ir, jb] < 0)
                    {
                        if (testLiquidPushRight(i, j)){ goto endOfTest; }
                    }
                    if (leftTestPositionChunk.fillStates[il, jb] == 0)
                    {
                        leftTestPositionChunk.fillStates[il, jb] = fillStates[i, j];
                        fillStates[i, j] = 0;
                        findTileColor(i, j);
                        leftTestPositionChunk.findTileColor(il, jb);
                        goto endOfTest;
                    }
                    if (leftTestPositionChunk.fillStates[il, jb] < 0)
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

                (int, int) chunkCoords = screen.findChunkScreenRelativeIndex((int)position.Item1 * 16, (int)position.Item2 * 16);
                int chunkX = chunkCoords.Item1;
                int chunkY = chunkCoords.Item2;
                if (jTested >= 16) { jTested = 0; chunkY = (chunkY+1)%screen.chunkResolution; }
                Chunk chunkToTest = screen.loadedChunks[chunkX, chunkY];

                int repeatCounter = 0;
                while(repeatCounter < 500)
                {
                    iTested++;
                    if(iTested > 15)
                    {
                        chunkX++;
                        chunkCoords = screen.findChunkScreenRelativeIndex(chunkX*16, chunkY*16);
                        chunkToTest = screen.loadedChunks[chunkCoords.Item1, chunkCoords.Item2];
                        iTested -= 16;
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

                (int, int) chunkCoords = screen.findChunkScreenRelativeIndex((int)position.Item1 * 16, (int)position.Item2 * 16);
                int chunkX = chunkCoords.Item1;
                int chunkY = chunkCoords.Item2;
                if (jTested >= 16) { jTested = 0; chunkY = (chunkY + 1) % screen.chunkResolution; }
                Chunk chunkToTest = screen.loadedChunks[chunkX, chunkY];

                int repeatCounter = 0;
                while (repeatCounter < 500)
                {
                    iTested--;
                    if (iTested < 0)
                    {
                        chunkX--;
                        chunkCoords = screen.findChunkScreenRelativeIndex(chunkX * 16, chunkY * 16);
                        chunkToTest = screen.loadedChunks[chunkCoords.Item1, chunkCoords.Item2];
                        iTested += 16;
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
            public Bitmap bitmap;
            public List<Player> playerList = new List<Player>();
            public List<Entity> activeEntites = new List<Entity>();
            public List<Entity> entitesToRemove = new List<Entity>();

            public Screen(long posX, long posY, int chunkResolutionToPut, long seedo, bool isPngToExport)
            {
                loadedChunkOffsetX = 0; //(((int)posX %chunkResolutionToPut) + chunkResolutionToPut) %chunkResolutionToPut;
                loadedChunkOffsetY = 0; //(((int)posY %chunkResolutionToPut) + chunkResolutionToPut) %chunkResolutionToPut;
                seed = seedo;
                isPngToBeExported = isPngToExport;
                playerList = new List<Player>();
                activeEntites = new List<Entity>();
                chunkResolution = chunkResolutionToPut+UnloadedChunksAmount*2; // invisible chunks of the sides/top/bottom
                if (!Directory.Exists($"{currentDirectory}\\ChunkData\\{seed}"))
                {
                    Directory.CreateDirectory($"{currentDirectory}\\ChunkData\\{seed}");
                }
                if (!Directory.Exists($"{currentDirectory}\\StructureData\\{seed}"))
                {
                    Directory.CreateDirectory($"{currentDirectory}\\StructureData\\{seed}");
                }
                LCGCacheInit();
                checkStructuresPlayerSpawn(player);
                loadChunks(posX, posY, seed);

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
                        loadedChunks[(i + chunkResolution) % chunkResolution, (j + chunkResolution) % chunkResolution] = new Chunk(posX + i, posY + j, seed, false, this);
                    }
                }
                if (isPngToBeExported) { bitmap = new Bitmap(16 * (ChunkLength - 1), 16 * (ChunkLength - 1)); }
                else { bitmap = new Bitmap(64 * (ChunkLength - 1), 64 * (ChunkLength - 1)); }
            }
            public void updateLoadedChunks(int posX, int posY, long seed, int screenSlideX, int screenSlideY)
            {

                // Gone ! Forever now ! it's. fucking. BACKKKKKK     !! ! !! GONE AGAIN fuck it's backkkk WOOOOOOOOOHOOOOOOOOOOOO BUG IS GONE !!! It's 4am !!!! FUCK !!!! PROBLEM !!!!! The update loaded chuncks is lagging 8 (7?) chunkcs behind the actual normal loading... but only in the updated dimension

                (int, int) chunkIndex;
                Chunk chunk;
                foreach (Chunk chunko in loadedChunks)
                {
                    chunko.entityList = new List<Entity>();
                }
                while (activeEntites.Count() > 0)
                {
                    chunkIndex = findChunkScreenRelativeIndex(activeEntites[0].posX, activeEntites[0].posY);
                    chunk = loadedChunks[chunkIndex.Item1, chunkIndex.Item2];
                    chunk.entityList.Add(activeEntites[0]);
                    activeEntites.RemoveAt(0);
                }

                while (screenSlideX > 0)
                {
                    for (int j = 0; j < chunkResolution; j++)
                    {
                        loadedChunks[loadedChunkOffsetX, (loadedChunkOffsetY + j) % chunkResolution].saveChunk(this);
                        loadedChunks[loadedChunkOffsetX, (loadedChunkOffsetY + j) % chunkResolution] = new Chunk((posX - screenSlideX) + chunkResolution, (posY - screenSlideY) + j, seed, false, this);
                    }
                    loadedChunkOffsetX = (loadedChunkOffsetX + 1) % chunkResolution;
                    screenSlideX -= 1;
                }
                while (screenSlideX < 0)
                {
                    for (int j = 0; j < chunkResolution; j++)
                    {
                        loadedChunks[(loadedChunkOffsetX + chunkResolution - 1) % chunkResolution, (loadedChunkOffsetY + j) % chunkResolution].saveChunk(this);
                        loadedChunks[(loadedChunkOffsetX + chunkResolution - 1) % chunkResolution, (loadedChunkOffsetY + j) % chunkResolution] = new Chunk((posX - screenSlideX) - 1, (posY - screenSlideY) + j, seed, false, this);
                    }
                    loadedChunkOffsetX = (loadedChunkOffsetX + chunkResolution - 1) % chunkResolution;
                    screenSlideX += 1;
                }
                while (screenSlideY > 0)
                {
                    for (int i = 0; i < chunkResolution; i++)
                    {
                        loadedChunks[(loadedChunkOffsetX + i) % chunkResolution, loadedChunkOffsetY].saveChunk(this);
                        loadedChunks[(loadedChunkOffsetX + i) % chunkResolution, loadedChunkOffsetY] = new Chunk((posX - screenSlideX) + i, (posY - screenSlideY) + chunkResolution, seed, false, this);
                    }
                    loadedChunkOffsetY = (loadedChunkOffsetY + 1) % chunkResolution;
                    screenSlideY -= 1;
                }
                while (screenSlideY < 0)
                {
                    for (int i = 0; i < chunkResolution; i++)
                    {
                        loadedChunks[(loadedChunkOffsetX + i) % chunkResolution, (loadedChunkOffsetY + chunkResolution - 1) % chunkResolution].saveChunk(this);
                        loadedChunks[(loadedChunkOffsetX + i) % chunkResolution, (loadedChunkOffsetY + chunkResolution - 1) % chunkResolution] = new Chunk((posX - screenSlideX) + i, (posY - screenSlideY) - 1, seed, false, this);
                    }
                    loadedChunkOffsetY = (loadedChunkOffsetY + chunkResolution - 1) % chunkResolution;
                    screenSlideY += 1;
                }

                foreach (Chunk chunko in loadedChunks)
                {
                    foreach (Entity entity in chunko.entityList)
                    {
                        activeEntites.Add(entity);
                    }
                    chunko.entityList = new List<Entity>();
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
                    long structuresAmount = (seedX + seedY) % 10 + 1;
                    for (int i = 0; i < structuresAmount; i++)
                    {
                        seedX = LCGyPos(seedX); // on porpoise x    /\_/\
                        seedY = LCGxPos(seedY); // and y switched  ( ^o^ )
                        Structure newStructure = new Structure(posX * 1024 + 32 + (int)(seedX % 960), posY * 1024 + 32 + (int)(seedY % 960), seedX, seedY, false, this);
                        newStructure.drawStructure();
                        newStructure.imprintChunks();
                        newStructure.saveInFile();
                    }
                    long waterLakesAmount = (seedX + seedY) % 30 + 10;
                    for (int i = 0; i < structuresAmount; i++)
                    {
                        seedX = LCGyPos(seedX); // on porpoise x    /\_/\
                        seedY = LCGxPos(seedY); // and y switched  ( ^o^ )
                        Structure newStructure = new Structure(posX * 1024 + 32 + (int)(seedX % 960), posY * 1024 + 32 + (int)(seedY % 960), seedX, seedY, true, this);
                        newStructure.drawLake();
                        newStructure.imprintChunks();
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

                for (int i = 0; i < chunkResolution*16; i++)
                {
                    for (int j = 0; j < chunkResolution*16; j++)
                    {
                        pixelPosX = ((i + (-loadedChunkOffsetX + chunkResolution) * 16) % (chunkResolution * 16)) - ((camPosX % 16) + 16) % 16 - UnloadedChunksAmount * 16;
                        pixelPosY = ((j + (-loadedChunkOffsetY + chunkResolution) * 16) % (chunkResolution * 16)) - ((camPosY % 16) + 16) % 16 - UnloadedChunksAmount * 16;

                        if (pixelPosX < 0 || pixelPosX >= (chunkResolution - 1) * 16 || pixelPosY < 0 || pixelPosY >= (chunkResolution - 1) * 16)
                        {
                            continue;
                        }

                        Color color = loadedChunks[i / 16, j / 16].colors[i % 16, j % 16];

                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.FillRectangle(new SolidBrush(color), (pixelPosX)*PNGmultiplicator, (pixelPosY)*PNGmultiplicator, PNGmultiplicator, PNGmultiplicator);
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
                    using (var g = Graphics.FromImage(bitmap))
                    {
                        g.FillRectangle(new SolidBrush(color), pixelPosX * PNGmultiplicator, pixelPosY * PNGmultiplicator, PNGmultiplicator, PNGmultiplicator);
                    }
                }

                foreach (Entity entity in activeEntites)
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
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.FillRectangle(new SolidBrush(color), pixelPosX * PNGmultiplicator, pixelPosY * PNGmultiplicator, PNGmultiplicator, PNGmultiplicator);
                        }
                    }
                }

                /*for (int i2 = 16; i2 < 48; i2++)
                {
                    for (int j2 = 16; j2 < 48; j2++)
                    {
                        //this was debug for the offset location, flemme to fixer lol
                        //bitmap.SetPixel((i2 +(-loadedChunkOffsetX + ChunkLength) * 64) % (ChunkLength * 64), (j2 + (-loadedChunkOffsetY + ChunkLength) * 64) % (ChunkLength * 64), Color.Green);
                    }
                }*/
                return bitmap;
            }
            public void zoom(bool isZooming)
            {
                if(isZooming)
                {
                    if(ChunkLength > 2)
                    {
                        ChunkLength -= 2;
                        UnloadedChunksAmount++;
                        bitmap = new Bitmap(64 * (ChunkLength - 1), 64 * (ChunkLength - 1));
                    }
                }
                else
                {
                    if (UnloadedChunksAmount > 1)
                    {
                        ChunkLength += 2;
                        UnloadedChunksAmount--;
                        bitmap = new Bitmap(64 * (ChunkLength - 1), 64 * (ChunkLength - 1));
                    }

                }
                lastZoom = timeElapsed;
            }
        }
        public class Structure
        {
            public string name;
            public int type;
            public Screen screen;
            public long seedX;
            public long seedY;
            public (int, int) centerPos;
            public (int, int) centerChunkPos;
            public (int, int) size;
            public (int, int, int, int) chunkBounds; // (Start X, end X not included, Start Y, end Y not included)
            public int[,][,] structureArray;
            public Structure(int posX, int posY, long seedXToPut, long seedYToPut, bool isLake, Screen screenToPut)
            {
                seedX = seedXToPut;
                seedY = seedYToPut;
                screen = screenToPut;
                centerPos = (posX, posY);
                centerChunkPos = (Floor(posX, 16) / 16, Floor(posY, 16) / 16);

                if(isLake)
                {
                    // waterLake
                    {
                        type = 3;
                        int sizeX = 0;
                        int sizeY = 0;
                        int posoX = centerChunkPos.Item1 - Floor(sizeX, 2) / 2;
                        int posoY = centerChunkPos.Item2 - Floor(sizeY, 2) / 2;
                        size = (sizeX, sizeY);
                        chunkBounds = (posoX, posoX + sizeX, posoY, posoY + sizeY);
                    }
                }
                else
                {
                    long seedo = (seedX / 2 + seedY / 2) % 79461537;
                    if (Abs(seedo) % 200 < 50) // cubeAmalgam
                    {
                        type = 0;
                        int sizeX = (int)(seedX % 5) + 1;
                        int sizeY = (int)(seedY % 5) + 1;
                        int posoX = centerChunkPos.Item1 - Floor(sizeX, 2) / 2;
                        int posoY = centerChunkPos.Item2 - Floor(sizeY, 2) / 2;
                        size = (sizeX, sizeY);
                        chunkBounds = (posoX, posoX + sizeX, posoY, posoY + sizeY);
                    }
                    else if (Abs(seedo) % 200 < 150)// circularBlade
                    {
                        type = 1;
                        int sizeX = (int)(seedX % 5) + 1;
                        int posoX = centerChunkPos.Item1 - Floor(sizeX, 2) / 2;
                        int posoY = centerChunkPos.Item2 - Floor(sizeX, 2) / 2;
                        size = (sizeX, sizeX);
                        chunkBounds = (posoX, posoX + sizeX, posoY, posoY + sizeX);
                    }
                    else // star 
                    {
                        type = 2;
                        int sizeX = (int)(seedX % 5) + 1;
                        int posoX = centerChunkPos.Item1 - Floor(sizeX, 2) / 2;
                        int posoY = centerChunkPos.Item2 - Floor(sizeX, 2) / 2;
                        size = (sizeX, sizeX);
                        chunkBounds = (posoX, posoX + sizeX, posoY, posoY + sizeX);
                    }
                }
            }
            public void drawLake()
            {
                List<List<int>>[,] listArray = new List<List<int>>[2, 2];
                int minX = 10;
                int maxX = 10;
                int minY = 10;
                int maxY = 10;
                listArray[0, 0] = new List<List<int>>();
                listArray[1, 0] = new List<List<int>>();
                listArray[0, 1] = new List<List<int>>();
                listArray[1, 1] = new List<List<int>>();
                for(int i = 0; i < 10; i++)
                {
                    listArray[0, 0].Add(new List<int>());
                    listArray[0, 1].Add(new List<int>());
                    listArray[1, 0].Add(new List<int>());
                    listArray[1, 1].Add(new List<int>());
                    for (int j = 0; j < 10; j++)
                    {
                        listArray[0, 0][i].Add(-999);
                        listArray[1, 0][i].Add(-999);
                        listArray[0, 1][i].Add(-999);
                        listArray[1, 1][i].Add(-999);
                    }
                }
                int posX = chunkBounds.Item1;
                int posY = chunkBounds.Item3;
                long seedo = (seedX / 2 + seedY / 2) % 79461537;
                // find a tile in the base chunk, try going down till ground is found, if not found soon enough cancel
                // if bottom found check if it's at the bottom or not, try to find the bottom for a bit, if not cancel.
                // when it is found, fill up layer by layer using brute force pathfinding.
                // when it stops going up and goes down, look around for a bit, while putting all new tiles in a buffer. If it's too much down, delete the buffer, return, if not, continue filling.
                // from this, make the actual structure map and return. Good luck to me.
            }
            public void drawStructure()
            {
                structureArray = new int[size.Item1, size.Item2][,];
                for (int i = 0; i < size.Item1; i++)
                {
                    for (int j = 0; j < size.Item2; j++)
                    {
                        structureArray[i, j] = new int[16, 16];
                        for (int k = 0; k < 16; k++)
                        {
                            for (int l = 0; l < 16; l++)
                            {
                                structureArray[i, j][k, l] = -999;
                            }
                        }
                    }
                }

                if (type == 0) { cubeAmalgam(); }
                else if (type == 1) { circularBlade(); }
                else if (type == 2) { star(); }
                else if (type == 3) { waterLake(); }
            }
            public void cubeAmalgam()
            {
                int squaresToDig = (int)(seedX % (10 + (size.Item1 * size.Item2))) + (int)(size.Item1 * size.Item2 * 0.2f) + 1;
                long seedoX = seedX;
                long seedoY = seedY;
                for (int gu = 0; gu < squaresToDig; gu++)
                {
                    seedoX = LCGxNeg(seedoX);
                    seedoY = LCGyNeg(seedoY);
                    int sizo = (int)((LCGxNeg(seedoY)) % 7 + 7) % 7 + 1;
                    int relativeCenterX = (int)(sizo + seedoX % (size.Item1 * 16 - 2 * sizo));
                    int relativeCenterY = (int)(sizo + seedoY % (size.Item2 * 16 - 2 * sizo));
                    for (int i = 1 - sizo; i < sizo; i++)
                    {
                        for (int j = -sizo; j <= sizo; j++)
                        {
                            int ii = relativeCenterX + i;
                            int jj = relativeCenterY + j;
                            structureArray[ii / 16, jj / 16][ii % 16, jj % 16] = 0;
                        }
                    }
                    for (int i = -sizo; i <= sizo; i += 2 * sizo)
                    {
                        for (int j = -sizo; j <= sizo; j++)
                        {
                            int ii = relativeCenterX + i;
                            int jj = relativeCenterY + j;
                            structureArray[ii / 16, jj / 16][ii % 16, jj % 16] = 1;
                        }
                    }
                    for (int i = -sizo; i <= sizo; i++)
                    {
                        for (int j = -sizo; j <= sizo; j += 2 * sizo)
                        {
                            int ii = relativeCenterX + i;
                            int jj = relativeCenterY + j;
                            structureArray[ii / 16, jj / 16][ii % 16, jj % 16] = 1;
                        }
                    }
                }
            }
            public void circularBlade()
            {
                long seedoX = seedX;
                long seedoY = seedY;

                int angleOfShape = (int)LCGz(seedoX + seedoY) % 360;
                (int,int) centerCoords = (size.Item1*8, size.Item2*8);

                for (int i = 0; i < size.Item1*16; i++)
                {
                    for (int j = 0; j < size.Item2*16; j++)
                    {
                        int posToCenterX = i - centerCoords.Item1;
                        int posToCenterY = j - centerCoords.Item2;
                        int angleMod = (int)(Math.Atan2(posToCenterY,posToCenterX)*180/Math.PI);
                        int angle = (360 + angleOfShape - angleMod)%360;
                        float distance = (float)Math.Sqrt(posToCenterX*posToCenterX+posToCenterY*posToCenterY);

                        float sizo = (size.Item1*(8 - sawBladeSeesaw(angle, 72)*0.1f));

                        if(distance < sizo)
                        {
                            structureArray[i / 16, j / 16][i % 16, j % 16] = 0;


                            //outline

                            int newi = i - 1;
                            int newj = j;
                            if (newi>=0 && newi<size.Item1*16 && newj>=0 && newj<size.Item1*16 && structureArray[newi / 16, newj / 16][newi % 16, newj % 16] == -999)
                            {
                                structureArray[newi / 16, newj / 16][newi % 16, newj % 16] = 1;
                            }
                            newi = i + 1;
                            newj = j;
                            if (newi>=0 && newi<size.Item1*16 && newj>=0 && newj<size.Item1*16 && structureArray[newi / 16, newj / 16][newi % 16, newj % 16] == -999)
                            {
                                structureArray[newi / 16, newj / 16][newi % 16, newj % 16] = 1;
                            }
                            newi = i;
                            newj = j - 1;
                            if (newi>=0 && newi<size.Item1*16 && newj>=0 && newj<size.Item1*16 && structureArray[newi / 16, newj / 16][newi % 16, newj % 16] == -999)
                            {
                                structureArray[newi / 16, newj / 16][newi % 16, newj % 16] = 1;
                            }
                            newi = i;
                            newj = j + 1;
                            if (newi>=0 && newi<size.Item1*16 && newj>=0 && newj<size.Item1*16 && structureArray[newi / 16, newj / 16][newi % 16, newj % 16] == -999)
                            {
                                structureArray[newi / 16, newj / 16][newi % 16, newj % 16] = 1;
                            }
                        }

                    }
                }
                (int,int) tupelo = (centerCoords.Item1-1, centerCoords.Item2-1);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 2;
                tupelo = (centerCoords.Item1, centerCoords.Item2-1);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 0;
                tupelo = (centerCoords.Item1+1, centerCoords.Item2-1);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 2;
                tupelo = (centerCoords.Item1-1, centerCoords.Item2);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 0;
                tupelo = (centerCoords.Item1, centerCoords.Item2);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 1;
                tupelo = (centerCoords.Item1+1, centerCoords.Item2);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 0;
                tupelo = (centerCoords.Item1-1, centerCoords.Item2+1);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 2;
                tupelo = (centerCoords.Item1, centerCoords.Item2+1);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 0;
                tupelo = (centerCoords.Item1+1, centerCoords.Item2+1);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 2;
            }
            public void star()
            {
                long seedoX = seedX;
                long seedoY = seedY;

                int angleOfShape = (int)LCGz(seedoX + seedoY) % 360;
                (int, int) centerCoords = (size.Item1 * 8, size.Item2 * 8);

                for (int i = 0; i < size.Item1 * 16; i++)
                {
                    for (int j = 0; j < size.Item2 * 16; j++)
                    {
                        int posToCenterX = i - centerCoords.Item1;
                        int posToCenterY = j - centerCoords.Item2;
                        int angleMod = (int)(Math.Atan2(posToCenterY, posToCenterX) * 180 / Math.PI);
                        int angle = (3600 + angleOfShape - angleMod) % 360;
                        float distance = (float)Math.Sqrt(posToCenterX * posToCenterX + posToCenterY * posToCenterY);

                        float sizo = (size.Item1 * (8 - Seesaw(angle, 72) * 0.1f));

                        if (distance < sizo)
                        {
                            structureArray[i / 16, j / 16][i % 16, j % 16] = 0;


                            //outline

                            int newi = i - 1;
                            int newj = j;
                            if (newi >= 0 && newi < size.Item1 * 16 && newj >= 0 && newj < size.Item1 * 16 && structureArray[newi / 16, newj / 16][newi % 16, newj % 16] == -999)
                            {
                                structureArray[newi / 16, newj / 16][newi % 16, newj % 16] = 1;
                            }
                            newi = i + 1;
                            newj = j;
                            if (newi >= 0 && newi < size.Item1 * 16 && newj >= 0 && newj < size.Item1 * 16 && structureArray[newi / 16, newj / 16][newi % 16, newj % 16] == -999)
                            {
                                structureArray[newi / 16, newj / 16][newi % 16, newj % 16] = 1;
                            }
                            newi = i;
                            newj = j - 1;
                            if (newi >= 0 && newi < size.Item1 * 16 && newj >= 0 && newj < size.Item1 * 16 && structureArray[newi / 16, newj / 16][newi % 16, newj % 16] == -999)
                            {
                                structureArray[newi / 16, newj / 16][newi % 16, newj % 16] = 1;
                            }
                            newi = i;
                            newj = j + 1;
                            if (newi >= 0 && newi < size.Item1 * 16 && newj >= 0 && newj < size.Item1 * 16 && structureArray[newi / 16, newj / 16][newi % 16, newj % 16] == -999)
                            {
                                structureArray[newi / 16, newj / 16][newi % 16, newj % 16] = 1;
                            }
                        }

                    }
                }
            }
            public void waterLake()
            {

            }
            public void imprintChunks()
            {
                for (int i = 0; i < size.Item1; i++)
                {
                    for (int j = 0; j < size.Item2; j++)
                    {
                        int ii = chunkBounds.Item1 + i;
                        int jj = chunkBounds.Item3 + j;
                        Chunk chunko = new Chunk(ii, jj, screen.seed, true, screen);
                        for (int k = 0; k < 16; k++)
                        {
                            for (int l = 0; l < 16; l++)
                            {
                                if (structureArray[i, j][k, l] != -999)
                                {
                                    chunko.fillStates[k, l] = structureArray[i, j][k, l];
                                }
                            }
                        }
                        chunko.modificationCount = 1;
                        chunko.saveChunk(screen);
                    }
                }
            }
            public void saveInFile()
            {
                //need to DO
            }
        }
        public class Player
        {
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
            public (int, int) findIntPos(float positionX, float positionY)
            {
                return ((int)Floor(positionX, 1), (int)Floor(positionY, 1));
            }
            public void placePlayer(Screen screen)
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
                }
            }
            public void movePlayer(Screen screen)
            {
                (int, int) newPos = findIntPos(realPosX + speedX, realPosY + speedY);
                int toMoveX = newPos.Item1 - posX;
                int toMoveY = newPos.Item2 - posY;
                if (digPress && timeElapsed > timeAtLastDig + 0.5f)
                {
                    if (arrowKeysState[0] && !arrowKeysState[1])
                    {
                        Dig(posX + 1, posY, screen);
                    }
                    else if (arrowKeysState[1] && !arrowKeysState[0])
                    {
                        Dig(posX - 1, posY, screen);
                    }
                    else if (arrowKeysState[2] && !arrowKeysState[3])
                    {
                        Dig(posX, posY + 1, screen);
                    }
                    else if (arrowKeysState[3] && !arrowKeysState[2])
                    {
                        Dig(posX, posY - 1, screen);
                    }
                }
                if ((placePress[0] || placePress[1]) && timeElapsed > timeAtLastPlace + 0.01f)
                {
                    if (arrowKeysState[0] && !arrowKeysState[1])
                    {
                        Place(posX + 1, posY, screen);
                    }
                    else if (arrowKeysState[1] && !arrowKeysState[0])
                    {
                        Place(posX - 1, posY, screen);
                    }
                    else if (arrowKeysState[2] && !arrowKeysState[3])
                    {
                        Place(posX, posY + 1, screen);
                    }
                    else if (arrowKeysState[3] && !arrowKeysState[2])
                    {
                        Place(posX, posY - 1, screen);
                    }
                }
                {
                    (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posX, posY);
                    if (screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2].fillStates[(posX % 16 + 32) % 16, (posY % 16 + 32) % 16] < 0)
                    {
                        speedX = (int)(speedX * 0.5f - Sqrt(Max((int)speedX-1,0)) + 0.6f);
                        speedY = (int)(speedY * 0.5f - Sqrt(Max((int)speedY-1,0)) + 0.6f);
                    }
                }
                while (Abs(toMoveX) > 0)
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
                }
            }
            public bool CheckStructurePosChange()
            {
                (int, int) oldStructurePos = (structureX, structureY);
                structureX = Floor(posX, 1024) / 1024;
                structureY = Floor(posY, 1024) / 1024;
                if (oldStructurePos == (structureX, structureY)) { return false; }
                return true;

            }
            public void Dig(int posToDigX, int posToDigY, Screen screen)
            {
                (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posToDigX, posToDigY);
                Chunk chunkToDig = screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                if (chunkToDig.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16] != 0)
                {
                    chunkToDig.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16] = 0;
                    chunkToDig.findTileColor((posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16);
                    chunkToDig.modificationCount += 1;
                    timeAtLastDig = timeElapsed;
                }
            }
            public void Place(int posToDigX, int posToDigY, Screen screen)
            {
                (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posToDigX, posToDigY);
                Chunk chunkToDig = screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                if (chunkToDig.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16] == 0)
                {
                    chunkToDig.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16] = -1;
                    chunkToDig.findTileColor((posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16);
                    chunkToDig.modificationCount += 1;
                    timeAtLastPlace = timeElapsed;
                }
            }
        }
        public class Entity
        {
            public int type; // 0 = fairy , 1 = frog
            public float realPosX = 0;
            public float realPosY = 0;
            public int posX = 0;
            public int posY = 0;
            public float speedX = 0;
            public float speedY = 0;
            public Color color;

            public Color findColor(Chunk chunk)
            {
                int hueVar = rand.Next(101) - 50;
                int shadeVar = rand.Next(61) - 30;
                int biome = chunk.biomeIndex[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16][0].Item1;
                if (biome == 5)
                {
                    type = 0;
                    return Color.FromArgb(130 + hueVar + shadeVar, 130 - hueVar + shadeVar, 210 + shadeVar);
                }
                else if (biome == 6)
                {
                    type = 0;
                    return Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar);
                }
                else if (biome == 7)
                {
                    type = 0;
                    hueVar = Abs((int)(hueVar * 0.4f));
                    shadeVar = Abs(shadeVar);
                    return Color.FromArgb(255 - hueVar - shadeVar, 255 - hueVar - shadeVar, 255 - shadeVar);
                }
                type = 1;
                return Color.FromArgb(90 + hueVar + shadeVar, 210 + shadeVar, 110 - hueVar + shadeVar);
            }
            public Entity(int posXt, int posYt, int typet, int rt, int gt, int bt)
            {
                realPosX = posXt;
                posX = posXt;
                realPosY = posYt;
                posY = posYt;
                type = typet;
                color = Color.FromArgb(rt, gt, bt);
            }
            public Entity(Chunk chunk)
            {
                placeEntity(chunk);
                color = findColor(chunk);
            }
            public (int, int) findIntPos(float positionX, float positionY)
            {
                return ((int)Floor(positionX, 1), (int)Floor(positionY, 1));
            }
            public void placeEntity(Chunk chunk)
            {
                int counto = 0;
                while (counto < 10000)
                {
                    int randX = rand.Next(16);
                    int randY = rand.Next(16);
                    if (chunk.fillStates[randX, randY] == 0)
                    {
                        posX = (int)chunk.position.Item1 * 16 + randX;
                        realPosX = posX;
                        posY = (int)chunk.position.Item2 * 16 + randY;
                        realPosY = posY;
                        break;
                    }
                    counto += 1;
                }
            }
            public void moveEntity(Screen screen)
            {
                if (type == 0)
                {
                    speedX += rand.Next(3) - 1;
                    speedY += rand.Next(3) - 1;
                }
                else if (type == 1)
                {
                    speedY += 1;
                    (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posX, posY + 1); // +1 cause coordinates are inverted lol
                    Chunk chunkToTest = screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                    if (chunkToTest.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 33) % 16] > 0)
                    {
                        speedX = Sign(speedX) * (Max(0, Abs(speedX) * (0.75f) - 0.25f));
                        if (rand.NextDouble() > 0.05f)
                        {
                            speedX += rand.Next(11) - 5;
                            speedY += rand.Next(11) - 5;
                        }
                    }
                }
                {
                    (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posX, posY);
                    if (screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2].fillStates[(posX % 16 + 32) % 16, (posY % 16 + 32) % 16] < 0)
                    {
                        speedX = (int)(speedX * 0.5f - Sqrt(Max((int)speedX - 1, 0)) + 0.6f);
                        speedY = (int)(speedY * 0.5f - Sqrt(Max((int)speedY - 1, 0)) + 0.6f);
                    }
                }
                (int, int) newPos = findIntPos(realPosX + speedX, realPosY + speedY);
                int toMoveX = newPos.Item1 - posX;
                int toMoveY = newPos.Item2 - posY;
                while (Abs(toMoveY) > 0)
                {
                    (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posX, posY + Sign(toMoveY));
                    (int, int) chunkAbsolutePos = screen.findChunkAbsoluteIndex(posX, posY + Sign(toMoveY));
                    Chunk chunkToTest = screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                    if (chunkAbsolutePos.Item2 < chunkY || chunkAbsolutePos.Item2 >= chunkY + screen.chunkResolution)
                    {
                        posY += Sign(toMoveY);
                        saveEntity(screen);
                        screen.entitesToRemove.Add(this);
                        return;
                    }
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
                }
                while (Abs(toMoveX) > 0)
                {
                    (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posX + Sign(toMoveX), posY);
                    (int, int) chunkAbsolutePos = screen.findChunkAbsoluteIndex(posX + Sign(toMoveX), posY);
                    Chunk chunkToTest = screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                    if (chunkAbsolutePos.Item1 < chunkX || chunkAbsolutePos.Item1 >= chunkX + screen.chunkResolution)
                    {
                        posX += Sign(toMoveX);
                        saveEntity(screen);
                        screen.entitesToRemove.Add(this);
                        return;
                    }
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
            }
            public void saveEntity(Screen screen)
            {
                (int, int) position = (Floor(posX, 16) / 16, Floor(posY, 16) / 16);
                if (System.IO.File.Exists($"{currentDirectory}\\ChunkData\\{screen.seed}\\{position.Item1}.{position.Item2}.txt"))
                {
                    List<String> lines = new List<String>();
                    using (StreamReader f = new StreamReader($"{currentDirectory}\\ChunkData\\{screen.seed}\\{position.Item1}.{position.Item2}.txt"))
                    {
                        while (!f.EndOfStream)
                        {
                            lines.Add(f.ReadLine());
                        }
                    }

                    string entityText = "";
                    entityText += posX + ";" + posY + ";";
                    entityText += type + ";";
                    entityText += color.R + ";" + color.G + ";" + color.B + ";";
                    lines[1] += entityText;

                    System.IO.File.WriteAllLines($"{currentDirectory}\\ChunkData\\{screen.seed}\\{position.Item1}.{position.Item2}.txt", lines);
                }
                else
                {
                    using (StreamWriter f = new StreamWriter($"{currentDirectory}\\ChunkData\\{screen.seed}\\{position.Item1}.{position.Item2}.txt", false))
                    {
                        string stringo = "0;\n";
                        stringo += posX + ";" + posY + ";";
                        stringo += type + ";";
                        stringo += color.R + ";" + color.G + ";" + color.B + ";";
                        stringo += "\nx\n";
                        f.Write(stringo);
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

            Screen mainScreen;

            bool updatePNG = false;
            int PNGsize = 50; // in chunks
            bool randomSeed = true;

            long seed = 3253271960;

            //
            // cool seeds !!!! DO NOT DELETE
            // 		
            // 527503228 : spawn inside a giant obsidian biome !
            // 1115706211 : very cool spawn, with all the 7 current biomes types near and visitable and amazing looking caves
            // 947024425 : the biggest fucking obsidian biome i've ever seen. Not near the spawn, go FULL RIGHT, at around 130-140 chunks far right. What the actual fuck it's so big (that's what she said)
            // 3496528327 : deep cold biome spawn
            // 3253271960 : another deep cold biome spawn
            // 1561562999 : onion san head... amazing
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
                int rando = -10;
                camPosX = -50 * 16;
                camPosY = rando * 16;
                mainScreen = new Screen(-PNGsize / 2, -PNGsize / 2, PNGsize, seed, true);
                mainScreen.updateScreen();
                Bitmap bmp = mainScreen.bitmap;
                Bitmap bmp2 = new Bitmap(512, 512);

                mainScreen.updateScreen().Save($"{currentDirectory}\\cavee.png");
                bmp2.Save($"{currentDirectory}\\caveNoise.png");
            }

            mainScreen = new Screen(chunkX, chunkY, ChunkLength, seed, false);
            player.placePlayer(mainScreen);
            mainScreen.playerList = new List<Player> { player };
            mainScreen.activeEntites = new List<Entity>();
            camPosX = player.posX - (int)(ChunkLength * 0.5f);
            camPosY = player.posY - (int)(ChunkLength * 0.5f);
            timer1.Tag = mainScreen;
            timeAtLauch = DateTime.Now;
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
            timeElapsed = (float)((DateTime.Now - timeAtLauch).TotalSeconds);
            accCamX = 0;
            accCamY = 0;
            player.speedX = Sign(player.speedX) * (Max(0, Abs(player.speedX) * (0.75f) - 0.25f));
            player.speedY = Sign(player.speedY) * (Max(0, Abs(player.speedY) * (0.75f) - 0.25f));
            if (arrowKeysState[0]) { player.speedX += 1; }
            if (arrowKeysState[1]) { player.speedX -= 1; }
            if (arrowKeysState[2]) { player.speedY += 1; }
            if (arrowKeysState[3]) { player.speedY -= 2; }
            player.speedY += 1;
            if (shiftPress)
            {
                player.speedX = Sign(player.speedX) * (Max(0, Abs(player.speedX) * (0.5f) - 1.2f));
                player.speedY = Sign(player.speedY) * (Max(0, Abs(player.speedY) * (0.5f) - 1.2f));
            }
            foreach (Player playor in screen.playerList)
            {
                playor.movePlayer(screen);
                screen.checkStructures(playor);
            }
            screen.entitesToRemove = new List<Entity>();
            foreach (Entity entity in screen.activeEntites)
            {
                entity.moveEntity(screen);
            }
            foreach (Entity entity in screen.entitesToRemove)
            {
                screen.activeEntites.Remove(entity);
            }
            int ii;
            int jj;
            for(int j = screen.chunkResolution - 2; j >= 0; j--)
            {
                for (int i = 1; i < screen.chunkResolution - 1; i++)
                {
                    ii = (i + screen.loadedChunkOffsetX + screen.chunkResolution) % (screen.chunkResolution);
                    jj = (j + screen.loadedChunkOffsetY + screen.chunkResolution) % (screen.chunkResolution);
                    screen.loadedChunks[ii, jj].moveLiquids(screen);
                }
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
            int oldChunkX = chunkX;
            int oldChunkY = chunkY;
            chunkX = Floor(camPosX, 16) / 16;
            chunkY = Floor(camPosY, 16) / 16;
            int chunkVariationX = chunkX - oldChunkX;
            int chunkVariationY = chunkY - oldChunkY;
            if (chunkVariationX != 0 || chunkVariationY != 0)
            {
                screen.updateLoadedChunks(chunkX, chunkY, screen.seed, chunkVariationX, chunkVariationY);
            }
            pictureBox1.Image = screen.updateScreen();
            pictureBox1.Refresh();
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
            if (listo.Count >= 3)
            {
                string stringo = "";
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