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
using System.Windows.Markup;
using System.Runtime.InteropServices;

namespace Cave
{
    public class Structures
    {
        public class Structure
        {
            public string name = "";
            public (int, int) structChunkPosition;
            public int type;
            public Form1.Screen screen;
            public long seedX;
            public long seedY;
            public (int, int) centerPos;
            public (int, int) centerChunkPos;
            public (int, int) size;
            public (int, int, int, int) chunkBounds; // (Start X, end X not included, Start Y, end Y not included)
            public int[,][,] structureArray;
            public Structure(int posX, int posY, long seedXToPut, long seedYToPut, bool isLake, (int, int) structChunkPositionToPut, Form1.Screen screenToPut)
            {
                seedX = seedXToPut;
                seedY = seedYToPut;
                screen = screenToPut;
                centerPos = (posX, posY);
                structChunkPosition = structChunkPositionToPut;
                centerChunkPos = (Floor(posX, 32) / 32, Floor(posY, 32) / 32);

                if (isLake)
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
            public void extendLakeArrays(int[] startingChunk, List<List<int>> tileList, List<List<Chunk>> chunkList, int x, int y)
            {
                if (x > 0)
                {
                    int lenX = chunkList.Count();
                    int lenY = chunkList[0].Count();

                    chunkList.Add(new List<Chunk>());

                    for (int j = 0; j < lenY; j++)
                    {
                        chunkList[lenX].Add(new Chunk((startingChunk[0] + lenX, startingChunk[1] + j), true, screen));
                    }

                    for (int i = 0; i < 32; i++)
                    {
                        tileList.Add(new List<int>());
                        for (int j = 0; j < lenY * 32; j++)
                        {
                            if(chunkList[lenX][j / 32].fillStates[i, j % 32] == 0)
                            {
                                tileList[lenX * 32 + i].Add(0);
                            }
                            else
                            {
                                tileList[lenX * 32 + i].Add(1);
                            }
                        }
                    }
                }
                else if (x < 0)
                {
                    startingChunk[0]--;

                    int lenX = chunkList.Count();
                    int lenY = chunkList[0].Count();

                    List<Chunk> listo = new List<Chunk>();

                    for (int j = 0; j < lenY; j++)
                    {
                        listo.Add(new Chunk((startingChunk[0], startingChunk[1] + j), true, screen));
                    }
                    chunkList.Insert(0, listo);

                    for (int i = 0; i < 32; i++)
                    {
                        tileList.Insert(i, new List<int>());
                        for (int j = 0; j < lenY * 32; j++)
                        {
                            if (chunkList[0][j / 32].fillStates[i, j % 32] == 0)
                            {
                                tileList[i].Add(0);
                            }
                            else
                            {
                                tileList[i].Add(1);
                            }
                        }
                    }
                }

                if (y > 0)
                {
                    int lenX = chunkList.Count();
                    int lenY = chunkList[0].Count();

                    for (int i = 0; i < lenX; i++)
                    {
                        chunkList[i].Add(new Chunk((startingChunk[0] + i, startingChunk[1] + lenY), true, screen));
                    }

                    for (int i = 0; i < lenX * 32; i++)
                    {
                        for (int j = 0; j < 32; j++)
                        {
                            if (chunkList[i / 32][lenY].fillStates[i % 32, j] == 0)
                            {
                                tileList[i].Add(0);
                            }
                            else
                            {
                                tileList[i].Add(1);
                            }
                        }
                    }
                }
                else if (y < 0)
                {
                    startingChunk[1]--;

                    int lenX = chunkList.Count();
                    int lenY = chunkList[0].Count();

                    for (int i = 0; i < lenX; i++)
                    {
                        chunkList[i].Insert(0, new Chunk((startingChunk[0] + i, startingChunk[1]), true, screen));
                    }

                    for (int i = 0; i < lenX * 32; i++)
                    {
                        for (int j = 0; j < 32; j++)
                        {
                            if (chunkList[i / 32][0].fillStates[i % 32, j] == 0)
                            {
                                tileList[i].Insert(j, 0);
                            }
                            else
                            {
                                tileList[i].Insert(j, 1);
                            }
                        }
                    }
                }
            }
            public void drawLake()
            {
                int[] startChunk = new int[2] { chunkBounds.Item1, chunkBounds.Item3 };
                long seedo = (seedX / 2 + seedY / 2) % 79461537;

                List<List<Chunk>> chunkList = new List<List<Chunk>>();
                chunkList.Add(new List<Chunk>());
                chunkList[0].Add(new Chunk((startChunk[0], startChunk[1]), true, screen));

                List<List<int>> tileList = new List<List<int>>();
                for (int i = 0; i < 32; i++)
                {
                    tileList.Add(new List<int>());
                    for (int j = 0; j < 32; j++)
                    {
                        tileList[i].Add(chunkList[0][0].fillStates[i, j]);
                    }
                }

                int[] testPos = new int[2] { (int)(seedo % 32), (int)((seedo / 32) % 32) };
                int[] testPosLeft;
                int[] testPosRight;
                bool[] hitWallArray = new bool[2] { false, false };

                int tilesToFill = Min((int)(seedo % 1009), (int)(seedo % 1277)) + 1;
                tilesToFill = 5000;
                int tilesFilled = 0;

                int liquidTypeToFill = -2;
                int numberOfSubLakes = 0; // the total number of mini lakes, connected or not, basically the amount of times the code passed through "searchForBottom"
                int numberOfConnectedSubLakes = 1; // the total number of mini lakes, that are touching the first minilake (basically those that are connected only, to not make separated lakes when saving and ignoring the non connected ones)

                // Loop 1 : find a tile in the base chunk, try going down till ground is found, if not found soon enough cancel
                // Loop 2 : bottom found : check if it's really the bottom or not (water can't possibly flow out by the sides), try to find the real bottom for a bit if not, if not foudn cancel.

                if (tileList[testPos[0]][testPos[1]] == 1) // if in the wall/ceiling abandon the lake
                {
                    goto abandonLake;
                }
            searchForBottom:; // if the lake was not fully filled, go back here to try and test if it's possible to fill it more
                numberOfSubLakes++;
                int repeatCounter = 0;
                while (repeatCounter < 100)
                {
                    if (tileList[testPos[0]][testPos[1]] != 0) // ground has been attained, will happen almost everytime
                    {
                        testPos[1] -= 1;
                        if (testPos[1] < 0)
                        {
                            extendLakeArrays(startChunk, tileList, chunkList, 0, -1);
                            testPos[1] += 32;
                        }
                        testPosLeft = new int[2] { testPos[0], testPos[1] };
                        testPosRight = new int[2] { testPos[0], testPos[1] };

                        while (!hitWallArray[0] || !hitWallArray[1]) // looop that sees if the ground attained is the lowest ground : checks on every side if water could fall lower
                        {
                            if (!hitWallArray[0])
                            {
                                testPosLeft[0] -= 1;
                                if (testPosLeft[0] < 0)
                                {
                                    extendLakeArrays(startChunk, tileList, chunkList, -1, 0);
                                    testPos[0] += 32;
                                    testPosLeft[0] += 32;
                                    testPosRight[0] += 32;
                                }
                                if (tileList[testPosLeft[0]][testPosLeft[1]] != 0)
                                {
                                    hitWallArray[0] = true;
                                    testPosLeft[0] += 1;
                                }
                                if (tileList[testPosLeft[0]][testPosLeft[1] + 1] == 0)
                                {
                                    testPos[0] = testPosLeft[0];
                                    testPos[1] = testPosLeft[1] + 1;
                                    break;
                                }
                            }
                            if (!hitWallArray[1])
                            {
                                testPosRight[0] += 1;
                                if (testPosRight[0] >= chunkList.Count * 32)
                                {
                                    extendLakeArrays(startChunk, tileList, chunkList, 1, 0);
                                }
                                if (tileList[testPosRight[0]][testPosRight[1]] != 0)
                                {
                                    hitWallArray[1] = true;
                                    testPosRight[0] -= 1;
                                }
                                if (tileList[testPosRight[0]][testPosRight[1] + 1] == 0)
                                {
                                    testPos[0] = testPosRight[0];
                                    testPos[1] = testPosRight[1] + 1;
                                    break;
                                }
                            }
                            repeatCounter++;
                        }
                        if (hitWallArray[0] && hitWallArray[1])
                        {
                            //yahoooooooo it's the bottom :)
                            goto outOfLoop;
                        }
                        //else if not real bottom : do nothing lol just needs to continue checking
                    }
                    else
                    {
                        testPos[1]++;
                        if (testPos[1] >= chunkList[0].Count * 32)
                        {
                            extendLakeArrays(startChunk, tileList, chunkList, 0, 1);
                        }
                    }
                    repeatCounter++;
                }

                if (tilesFilled == 0)  // the loop ran too much, too hard to find a place to put lake, abandon the shit
                {
                    goto abandonLake;
                }
                else
                {
                    goto fillAndSaveLake;
                }


                outOfLoop:;

                // Bottom has been found ! fill up layer by layer using brute force pathfinding.
                // find the type of lake (if it is in fairy biome it is fairy liquid etc).

                if (tilesFilled == 0) //only do that if lake hasn't started to be filled
                {
                    Chunk chunkToTest = chunkList[testPos[0] / 32][testPos[1] / 32];
                    (int, int) chunkTestPos = (testPos[0] % 32, testPos[1] % 32);

                    if (chunkToTest.biomeIndex[chunkTestPos.Item1, chunkTestPos.Item2][0].Item1 == 5)
                    {
                        liquidTypeToFill = -3;
                    }
                    if (chunkToTest.biomeIndex[chunkTestPos.Item1, chunkTestPos.Item2][0].Item1 == 2)
                    {
                        if (chunkToTest.secondaryBiomeValues[chunkTestPos.Item1, chunkTestPos.Item2, 0] + chunkToTest.secondaryBigBiomeValues[chunkTestPos.Item1, chunkTestPos.Item2, 0] - 128 + rand.Next(200) - 200 > 100)
                        {
                            liquidTypeToFill = -4;
                        }
                    }
                }

                List<(int, int)> tilesToAdd;
                testPos[1] += 1; // needed to counteract the first -= 1 just under
                while (tilesFilled < tilesToFill) // it can overfill, that's not a problem
                {
                    testPos[1] -= 1; // no need to check for out of bounds array cause the first test is always negative : at least 1 tile above loaded
                    if (testPos[1] < 0)
                    {
                        extendLakeArrays(startChunk, tileList, chunkList, 0, -1);
                        testPos[1] += 32;
                    }
                    if (tileList[testPos[0]][testPos[1]] != 0 && tileList[testPos[0]][testPos[1]] != -2)
                    {
                        goto fillAndSaveLake;
                    }
                    testPosLeft = new int[2] { testPos[0], testPos[1] };
                    testPosRight = new int[2] { testPos[0], testPos[1] };

                    tilesToAdd = new List<(int, int)>();
                    tilesToAdd.Add((testPos[0], testPos[1]));
                    hitWallArray = new bool[2] { false, false };

                    while (!hitWallArray[0] || !hitWallArray[1]) // looop that sees if the ground attained is the lowest ground : checks on every side if water could fall lower
                    {
                        if (!hitWallArray[0])
                        {
                            testPosLeft[0] -= 1;
                            if (testPosLeft[0] < 0)
                            {
                                extendLakeArrays(startChunk, tileList, chunkList, -1, 0);
                                for (int i = 0; i < tilesToAdd.Count; i++)
                                {
                                    tilesToAdd[i] = (tilesToAdd[i].Item1 + 32, tilesToAdd[i].Item2);
                                }
                                testPos[0] += 32;
                                testPosLeft[0] += 32;
                                testPosRight[0] += 32;
                            }
                            if (tileList[testPosLeft[0]][testPosLeft[1]] == 0 && tileList[testPosLeft[0]][testPosLeft[1] + 1] == 0)
                            {
                                testPos[0] = testPosLeft[0];
                                testPos[1] = testPosLeft[1] + 1;
                                goto searchForBottom;
                            }
                            if (tileList[testPosLeft[0]][testPosLeft[1]] != 0)
                            {
                                hitWallArray[0] = true;
                                testPosLeft[0] += 1;
                            }
                            else
                            {
                                tilesToAdd.Add((testPosLeft[0], testPosLeft[1]));
                            }
                        }
                        if (!hitWallArray[1])
                        {
                            testPosRight[0] += 1;
                            if (testPosRight[0] >= chunkList.Count * 32)
                            {
                                extendLakeArrays(startChunk, tileList, chunkList, 1, 0);
                            }
                            if (tileList[testPosRight[0]][testPosRight[1]] == 0 && tileList[testPosRight[0]][testPosRight[1] + 1] == 0)
                            {
                                testPos[0] = testPosRight[0];
                                testPos[1] = testPosRight[1] + 1;
                                goto searchForBottom;
                            }
                            if (tileList[testPosRight[0]][testPosRight[1]] != 0)
                            {
                                hitWallArray[1] = true;
                                testPosRight[0] -= 1;
                            }
                            else
                            {
                                tilesToAdd.Add((testPosRight[0], testPosRight[1]));
                            }
                        }
                        repeatCounter++;
                    }
                    if (hitWallArray[0] && hitWallArray[1])
                    {
                        // no water leakage, continue. Fill the array.
                        for (int i = 0; i < tilesToAdd.Count; i++)
                        {
                            tileList[tilesToAdd[i].Item1][tilesToAdd[i].Item2] = -numberOfSubLakes;
                        }
                        tilesFilled += tilesToAdd.Count;
                    }
                    else
                    {
                        goto searchForBottom; // do not update water array, as uhhh it'd overflow lol. No writing of the lil numbers :) //unreachable...
                    }
                }

                fillAndSaveLake:;

                Bitmap bitmapo = new Bitmap(tileList.Count*2, tileList[0].Count*2);

                seedo = LCGyNeg(LCGxNeg(seedo));
                if (seedo % 1000 == 0) { liquidTypeToFill = -1; }
                else if (seedo % 1000 < 5) { liquidTypeToFill = -3; }

                for (int i = 0; i < tileList.Count; i++)
                {
                    for (int j = 0; j < tileList[0].Count; j++)
                    {
                        Color color = Color.Green;
                        if (tileList[i][j] == 0) { color = Color.Gray; }
                        if (tileList[i][j] == 1) { color = Color.Black; }
                        if (tileList[i][j] < 0 ) { color = ColorFromHSV((36000+230+tileList[i][j]*12)%360, 1+tileList[i][j]*0.01, 1); }

                        using (var g = Graphics.FromImage(bitmapo))
                        {
                            g.FillRectangle(new SolidBrush(color), i*2, j*2, 2, 2);
                        }
                        if (tileList[i][j] < 0 && tileList[i][j] >= -numberOfSubLakes)
                        {
                            chunkList[i / 32][j / 32].fillStates[i % 32, j % 32] = liquidTypeToFill;
                            chunkList[i / 32][j / 32].modificationCount = 1;
                        }
                    }
                }

                bitmapo.Save($"{currentDirectory}\\bitmapos\\bitmapo{rand.Next(10000)}.png");

                foreach (List<Chunk> chunko in chunkList)
                {
                    foreach (Chunk chunk in chunko)
                    {
                        Files.saveChunk(chunk, false);
                    }
                }

                name = "";
                int syllables = 2 + Min((int)(seedo % 13), (int)(seedo % 3));
                for (int i = 0; i < syllables; i++)
                {
                    name += nameArray[seedo % nameArray.Length];
                    seedo = LCGz(seedo);
                }

            // when it stops going up and goes down, look around for a bit, while putting all new tiles in a buffer. If it's too much down, delete the buffer, return, if not, continue filling.
            // from this, make the actual structure map and return. Good luck to me.

            abandonLake:;
            }
            public class testPosForLakes
            {
                public int x;
                public int y;
                public List<(int x, int y)> previouslyFilledPositions;

            }
            public void drawLake2()
            {
                int[] startChunk = new int[2] { chunkBounds.Item1, chunkBounds.Item3 };
                long seedo = (seedX / 2 + seedY / 2) % 79461537;

                int tilesToFill = Min((int)(seedo % 1009), (int)(seedo % 1277)) + 1;
                tilesToFill = 5000;
                int tilesFilled = 0;
                int maximumY = 0;

                int liquidTypeToFill = -2;

                int numberOfSubLakes = 0; // the total number of mini lakes, connected or not, basically the amount of times the code passed through "searchForBottom"
                int numberOfConnectedSubLakes = 1; // the total number of mini lakes, that are touching the first minilake (basically those that are connected only, to not make separated lakes when saving and ignoring the non connected ones)

                List<List<Chunk>> chunkList = new List<List<Chunk>>();
                chunkList.Add(new List<Chunk>());
                chunkList[0].Add(new Chunk((startChunk[0], startChunk[1]), true, screen));

                List<List<int>> tileList = new List<List<int>>();
                for (int i = 0; i < 32; i++)
                {
                    tileList.Add(new List<int>());
                    for (int j = 0; j < 32; j++)
                    {
                        tileList[i].Add(chunkList[0][0].fillStates[i, j]);
                    }
                }

                List<testPosForLakes> listTestPos = new List<testPosForLakes>();
                testPosForLakes currentTestPos = new testPosForLakes();
                currentTestPos.x = (int)(seedo % 32);
                currentTestPos.y = (int)((seedo / 32) % 32);
                listTestPos.Add(currentTestPos);

                bool[] hitWallArray = new bool[2] { false, false };

                // Loop 1 : find a tile in the base chunk, try going down till ground is found, if not found soon enough cancel
                // Loop 2 : bottom found : check if it's really the bottom or not (water can't possibly flow out by the sides), try to find the real bottom for a bit if not, if not foudn cancel.

                if (tileList[currentTestPos.x][currentTestPos.y] == 1) // if in the wall/ceiling abandon the lake
                {
                    goto abandonLake;
                }
            searchForBottom:; // if the lake was not fully filled, go back here to try and test if it's possible to fill it more
                currentTestPos = listTestPos[listTestPos.Count - 1];
                numberOfSubLakes++;
                int repeatCounter = 0;
                while (repeatCounter < 100)
                {
                    if (tileList[currentTestPos.x][currentTestPos.y] != 0) // ground has been attained, will happen almost everytime
                    {
                        currentTestPos.y -= 1;
                        if (currentTestPos.y < 0)
                        {
                            extendLakeArrays(startChunk, tileList, chunkList, 0, -1);
                            currentTestPos.y += 32;
                        }
                        int[] testPosLeft = new int[2] { currentTestPos.x, currentTestPos.y };
                        int[] testPosRight = new int[2] { currentTestPos.x, currentTestPos.y };

                        while (!hitWallArray[0] || !hitWallArray[1]) // looop that sees if the ground attained is the lowest ground : checks on every side if water could fall lower
                        {
                            if (!hitWallArray[0])
                            {
                                testPosLeft[0] -= 1;
                                if (testPosLeft[0] < 0)
                                {
                                    extendLakeArrays(startChunk, tileList, chunkList, -1, 0);
                                    currentTestPos.x += 32;
                                    testPosLeft[0] += 32;
                                    testPosRight[0] += 32;
                                }
                                if (tileList[testPosLeft[0]][testPosLeft[1]] != 0)
                                {
                                    hitWallArray[0] = true;
                                    testPosLeft[0] += 1;
                                }
                                if (tileList[testPosLeft[0]][testPosLeft[1] + 1] == 0)
                                {
                                    currentTestPos.x = testPosLeft[0];
                                    currentTestPos.y = testPosLeft[1] + 1;
                                    break;
                                }
                            }
                            if (!hitWallArray[1])
                            {
                                testPosRight[0] += 1;
                                if (testPosRight[0] >= chunkList.Count * 32)
                                {
                                    extendLakeArrays(startChunk, tileList, chunkList, 1, 0);
                                }
                                if (tileList[testPosRight[0]][testPosRight[1]] != 0)
                                {
                                    hitWallArray[1] = true;
                                    testPosRight[0] -= 1;
                                }
                                if (tileList[testPosRight[0]][testPosRight[1] + 1] == 0)
                                {
                                    currentTestPos.x = testPosRight[0];
                                    currentTestPos.y = testPosRight[1] + 1;
                                    break;
                                }
                            }
                            repeatCounter++;
                        }
                        if (hitWallArray[0] && hitWallArray[1])
                        {
                            //yahoooooooo it's the bottom :)
                            maximumY = currentTestPos.y;
                            goto outOfLoop;
                        }
                        //else if not real bottom : do nothing lol just needs to continue checking
                    }
                    else
                    {
                        currentTestPos.y++;
                        if (currentTestPos.y >= chunkList[0].Count * 32)
                        {
                            extendLakeArrays(startChunk, tileList, chunkList, 0, 1);
                        }
                    }
                    repeatCounter++;
                }

                if (tilesFilled == 0)  // the loop ran too much, too hard to find a place to put lake, abandon the shit
                {
                    goto abandonLake;
                }
                else
                {
                    goto fillAndSaveLake;
                }


            outOfLoop:;

                while (tilesFilled < tilesToFill)
                {
                    int listIdx = 0;
                    //List<(int x, int y)> tilesToFill = new List<(int x, int y)>();
                    while(listIdx < listTestPos.Count)
                    {
                        currentTestPos = listTestPos[listIdx];



                        listIdx++;
                    }
                }




            fillAndSaveLake:;

                Bitmap bitmapo = new Bitmap(tileList.Count * 2, tileList[0].Count * 2);

                seedo = LCGyNeg(LCGxNeg(seedo));
                if (seedo % 1000 == 0) { liquidTypeToFill = -1; }
                else if (seedo % 1000 < 5) { liquidTypeToFill = -3; }

                for (int i = 0; i < tileList.Count; i++)
                {
                    for (int j = 0; j < tileList[0].Count; j++)
                    {
                        Color color = Color.Green;
                        if (tileList[i][j] == 0) { color = Color.Gray; }
                        if (tileList[i][j] == 1) { color = Color.Black; }
                        if (tileList[i][j] < 0) { color = ColorFromHSV((36000 + 230 + tileList[i][j] * 12) % 360, 1 + tileList[i][j] * 0.01, 1); }

                        using (var g = Graphics.FromImage(bitmapo))
                        {
                            g.FillRectangle(new SolidBrush(color), i * 2, j * 2, 2, 2);
                        }
                        if (tileList[i][j] < 0 && tileList[i][j] >= -numberOfSubLakes)
                        {
                            chunkList[i / 32][j / 32].fillStates[i % 32, j % 32] = liquidTypeToFill;
                            chunkList[i / 32][j / 32].modificationCount = 1;
                        }
                    }
                }

                bitmapo.Save($"{currentDirectory}\\bitmapos\\bitmapo{rand.Next(10000)}.png");

                foreach (List<Chunk> chunko in chunkList)
                {
                    foreach (Chunk chunk in chunko)
                    {
                        Files.saveChunk(chunk, false);
                    }
                }

                name = "";
                int syllables = 2 + Min((int)(seedo % 13), (int)(seedo % 3));
                for (int i = 0; i < syllables; i++)
                {
                    name += nameArray[seedo % nameArray.Length];
                    seedo = LCGz(seedo);
                }

            abandonLake:;

            }
            public void drawLakePapa()
            {
                long seedo = (seedX / 2 + seedY / 2) % 79461537;

                Dictionary<(int x, int y), Chunk> chunkDict = new Dictionary<(int x, int y), Chunk>();
                Dictionary<(int x, int y), bool> tilesToFill = new Dictionary<(int x, int y), bool>();
                int[] tilesFilled = new int[]{0};
                (int x, int y) testPos32 = ((int)(seedo % 32), (int)((seedo / 32) % 32));
                (int x, int y) testPos = (testPos32.x + chunkBounds.Item1*32, testPos32.y + chunkBounds.Item3*32); 

                floodPixel(testPos, testPos, tilesToFill, chunkDict, tilesFilled);
                
                if (tilesFilled[0] <= 2000)
                {
                    int liquidTypeToFill = -2;
                    Chunk chunkToTest = chunkDict[(Floor(testPos.x, 32) / 32, Floor(testPos.y, 32) / 32)];

                    if (chunkToTest.biomeIndex[testPos32.Item1, testPos32.Item2][0].Item1 == 5)
                    {
                        liquidTypeToFill = -3;
                    }
                    if (chunkToTest.biomeIndex[testPos32.Item1, testPos32.Item2][0].Item1 == 2)
                    {
                        if (chunkToTest.secondaryBiomeValues[testPos32.Item1, testPos32.Item2, 0] + chunkToTest.secondaryBigBiomeValues[testPos32.Item1, testPos32.Item2, 0] - 128 + rand.Next(200) - 200 > 100)
                        {
                            liquidTypeToFill = -4;
                        }
                    }
                    seedo = LCGyNeg(LCGxNeg(seedo));
                    if (seedo % 1000 == 0) { liquidTypeToFill = -1; }
                    else if (seedo % 1000 < 5) { liquidTypeToFill = -3; }

                    foreach ((int x, int y) poso in tilesToFill.Keys)
                    {
                        chunkDict[(Floor(poso.x, 32)/32, Floor(poso.y, 32)/32)].fillStates[(poso.x%32+ 32) % 32, (poso.y%32+ 32) % 32] = liquidTypeToFill;
                        chunkDict[(Floor(poso.x, 32)/32, Floor(poso.y, 32)/32)].modificationCount = 1;
                    }

                    foreach (Chunk chunk in chunkDict.Values)
                    {
                        Files.saveChunk(chunk, false);
                    }

                    name = "";
                    int syllables = 2 + Min((int)(seedo % 13), (int)(seedo % 3));
                    for (int i = 0; i < syllables; i++)
                    {
                        name += nameArray[seedo % nameArray.Length];
                        seedo = LCGz(seedo);
                    }
                }
            }
            void floodPixel((int x, int y) pos, (int x, int y) spawnPos, Dictionary<(int x, int y), bool> tilesToFill, Dictionary<(int x, int y), Chunk> chunkDict, int[] tilesFilled)
            {
                if (tilesFilled[0] > 2000) { return; }
                (int x, int y) chunkPos = (Floor(pos.x, 32) / 32, Floor(pos.y, 32) / 32);
                if (!chunkDict.ContainsKey(chunkPos)) { chunkDict.Add(chunkPos, new Chunk(chunkPos, true, screen)); }
                if (!tilesToFill.ContainsKey(pos) && chunkDict[chunkPos].fillStates[(pos.x%32 + 32) % 32, (pos.y%32 + 32) % 32] == 0)
                {
                    tilesToFill[pos] = true;
                    tilesFilled[0]++;
                    floodPixel((pos.x - 1, pos.y), spawnPos, tilesToFill, chunkDict, tilesFilled);
                    floodPixel((pos.x + 1, pos.y), spawnPos, tilesToFill, chunkDict, tilesFilled);
                    floodPixel((pos.x, pos.y - 1), spawnPos, tilesToFill, chunkDict, tilesFilled);
                    if (pos.y < spawnPos.y) { floodPixel((pos.x, pos.y + 1), spawnPos, tilesToFill, chunkDict, tilesFilled);}
                }
            }
            public void drawStructure()
            {
                structureArray = new int[size.Item1, size.Item2][,];
                for (int i = 0; i < size.Item1; i++)
                {
                    for (int j = 0; j < size.Item2; j++)
                    {
                        structureArray[i, j] = new int[32, 32];
                        for (int k = 0; k < 32; k++)
                        {
                            for (int l = 0; l < 32; l++)
                            {
                                structureArray[i, j][k, l] = -999;
                            }
                        }
                    }
                }

                if (type == 0) { cubeAmalgam(); }
                else if (type == 1) { circularBlade(); }
                else if (type == 2) { star(); }

                long seedo = (seedX / 2 + seedY / 2) % 79461537;
                name = "";
                int syllables = 2 + Min((int)(seedo % 13), (int)(seedo % 3));
                for (int i = 0; i < syllables; i++)
                {
                    name += nameArray[seedo % nameArray.Length];
                    seedo = LCGz(seedo);
                }
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
                    int relativeCenterX = (int)(sizo + seedoX % (size.Item1 * 32 - 2 * sizo));
                    int relativeCenterY = (int)(sizo + seedoY % (size.Item2 * 32 - 2 * sizo));
                    for (int i = 1 - sizo; i < sizo; i++)
                    {
                        for (int j = -sizo; j <= sizo; j++)
                        {
                            int ii = relativeCenterX + i;
                            int jj = relativeCenterY + j;
                            structureArray[ii / 32, jj / 32][ii % 32, jj % 32] = 0;
                        }
                    }
                    for (int i = -sizo; i <= sizo; i += 2 * sizo)
                    {
                        for (int j = -sizo; j <= sizo; j++)
                        {
                            int ii = relativeCenterX + i;
                            int jj = relativeCenterY + j;
                            structureArray[ii / 32, jj / 32][ii % 32, jj % 32] = 1;
                        }
                    }
                    for (int i = -sizo; i <= sizo; i++)
                    {
                        for (int j = -sizo; j <= sizo; j += 2 * sizo)
                        {
                            int ii = relativeCenterX + i;
                            int jj = relativeCenterY + j;
                            structureArray[ii / 32, jj / 32][ii % 32, jj % 32] = 1;
                        }
                    }
                }
            }
            public void circularBlade()
            {
                long seedoX = seedX;
                long seedoY = seedY;

                int angleOfShape = (int)LCGz(seedoX + seedoY) % 360;
                (int, int) centerCoords = (size.Item1 * 8, size.Item2 * 8);

                for (int i = 0; i < size.Item1 * 32; i++)
                {
                    for (int j = 0; j < size.Item2 * 32; j++)
                    {
                        int posToCenterX = i - centerCoords.Item1;
                        int posToCenterY = j - centerCoords.Item2;
                        int angleMod = (int)(Math.Atan2(posToCenterY, posToCenterX) * 180 / Math.PI);
                        int angle = (3600 + angleOfShape - angleMod) % 360;
                        float distance = (float)Math.Sqrt(posToCenterX * posToCenterX + posToCenterY * posToCenterY);

                        float sizo = (size.Item1 * (8 - sawBladeSeesaw(angle, 72) * 0.1f));

                        if (distance < sizo)
                        {
                            structureArray[i / 32, j / 32][i % 32, j % 32] = 0;


                            //outline

                            int newi = i - 1;
                            int newj = j;
                            if (newi >= 0 && newi < size.Item1 * 32 && newj >= 0 && newj < size.Item1 * 32 && structureArray[newi / 32, newj / 32][newi % 32, newj % 32] == -999)
                            {
                                structureArray[newi / 32, newj / 32][newi % 32, newj % 32] = 1;
                            }
                            newi = i + 1;
                            newj = j;
                            if (newi >= 0 && newi < size.Item1 * 32 && newj >= 0 && newj < size.Item1 * 32 && structureArray[newi / 32, newj / 32][newi % 32, newj % 32] == -999)
                            {
                                structureArray[newi / 32, newj / 32][newi % 32, newj % 32] = 1;
                            }
                            newi = i;
                            newj = j - 1;
                            if (newi >= 0 && newi < size.Item1 * 32 && newj >= 0 && newj < size.Item1 * 32 && structureArray[newi / 32, newj / 32][newi % 32, newj % 32] == -999)
                            {
                                structureArray[newi / 32, newj / 32][newi % 32, newj % 32] = 1;
                            }
                            newi = i;
                            newj = j + 1;
                            if (newi >= 0 && newi < size.Item1 * 32 && newj >= 0 && newj < size.Item1 * 32 && structureArray[newi / 32, newj / 32][newi % 32, newj % 32] == -999)
                            {
                                structureArray[newi / 32, newj / 32][newi % 32, newj % 32] = 1;
                            }
                        }

                    }
                }
                (int, int) tupelo = (centerCoords.Item1 - 1, centerCoords.Item2 - 1);
                structureArray[tupelo.Item1 / 32, tupelo.Item2 / 32][tupelo.Item1 % 32, tupelo.Item2 % 32] = 2;
                tupelo = (centerCoords.Item1, centerCoords.Item2 - 1);
                structureArray[tupelo.Item1 / 32, tupelo.Item2 / 32][tupelo.Item1 % 32, tupelo.Item2 % 32] = 0;
                tupelo = (centerCoords.Item1 + 1, centerCoords.Item2 - 1);
                structureArray[tupelo.Item1 / 32, tupelo.Item2 / 32][tupelo.Item1 % 32, tupelo.Item2 % 32] = 2;
                tupelo = (centerCoords.Item1 - 1, centerCoords.Item2);
                structureArray[tupelo.Item1 / 32, tupelo.Item2 / 32][tupelo.Item1 % 32, tupelo.Item2 % 32] = 0;
                tupelo = (centerCoords.Item1, centerCoords.Item2);
                structureArray[tupelo.Item1 / 32, tupelo.Item2 / 32][tupelo.Item1 % 32, tupelo.Item2 % 32] = 1;
                tupelo = (centerCoords.Item1 + 1, centerCoords.Item2);
                structureArray[tupelo.Item1 / 32, tupelo.Item2 / 32][tupelo.Item1 % 32, tupelo.Item2 % 32] = 0;
                tupelo = (centerCoords.Item1 - 1, centerCoords.Item2 + 1);
                structureArray[tupelo.Item1 / 32, tupelo.Item2 / 32][tupelo.Item1 % 32, tupelo.Item2 % 32] = 2;
                tupelo = (centerCoords.Item1, centerCoords.Item2 + 1);
                structureArray[tupelo.Item1 / 32, tupelo.Item2 / 32][tupelo.Item1 % 32, tupelo.Item2 % 32] = 0;
                tupelo = (centerCoords.Item1 + 1, centerCoords.Item2 + 1);
                structureArray[tupelo.Item1 / 32, tupelo.Item2 / 32][tupelo.Item1 % 32, tupelo.Item2 % 32] = 2;
            }
            public void star()
            {
                long seedoX = seedX;
                long seedoY = seedY;

                int angleOfShape = (int)LCGz(seedoX + seedoY) % 360;
                (int, int) centerCoords = (size.Item1 * 8, size.Item2 * 8);

                for (int i = 0; i < size.Item1 * 32; i++)
                {
                    for (int j = 0; j < size.Item2 * 32; j++)
                    {
                        int posToCenterX = i - centerCoords.Item1;
                        int posToCenterY = j - centerCoords.Item2;
                        int angleMod = (int)(Math.Atan2(posToCenterY, posToCenterX) * 180 / Math.PI);
                        int angle = (3600 + angleOfShape - angleMod) % 360;
                        float distance = (float)Math.Sqrt(posToCenterX * posToCenterX + posToCenterY * posToCenterY);

                        float sizo = (size.Item1 * (8 - Seesaw(angle, 72) * 0.1f));

                        if (distance < sizo)
                        {
                            structureArray[i / 32, j / 32][i % 32, j % 32] = 0;


                            //outline

                            int newi = i - 1;
                            int newj = j;
                            if (newi >= 0 && newi < size.Item1 * 32 && newj >= 0 && newj < size.Item1 * 32 && structureArray[newi / 32, newj / 32][newi % 32, newj % 32] == -999)
                            {
                                structureArray[newi / 32, newj / 32][newi % 32, newj % 32] = 1;
                            }
                            newi = i + 1;
                            newj = j;
                            if (newi >= 0 && newi < size.Item1 * 32 && newj >= 0 && newj < size.Item1 * 32 && structureArray[newi / 32, newj / 32][newi % 32, newj % 32] == -999)
                            {
                                structureArray[newi / 32, newj / 32][newi % 32, newj % 32] = 1;
                            }
                            newi = i;
                            newj = j - 1;
                            if (newi >= 0 && newi < size.Item1 * 32 && newj >= 0 && newj < size.Item1 * 32 && structureArray[newi / 32, newj / 32][newi % 32, newj % 32] == -999)
                            {
                                structureArray[newi / 32, newj / 32][newi % 32, newj % 32] = 1;
                            }
                            newi = i;
                            newj = j + 1;
                            if (newi >= 0 && newi < size.Item1 * 32 && newj >= 0 && newj < size.Item1 * 32 && structureArray[newi / 32, newj / 32][newi % 32, newj % 32] == -999)
                            {
                                structureArray[newi / 32, newj / 32][newi % 32, newj % 32] = 1;
                            }
                        }

                    }
                }
            }
            public void imprintChunks()
            {
                for (int i = 0; i < size.Item1; i++)
                {
                    for (int j = 0; j < size.Item2; j++)
                    {
                        int ii = chunkBounds.Item1 + i;
                        int jj = chunkBounds.Item3 + j;
                        Chunk chunko = new Chunk((ii, jj), true, screen);
                        for (int k = 0; k < 32; k++)
                        {
                            for (int l = 0; l < 32; l++)
                            {
                                if (structureArray[i, j][k, l] != -999)
                                {
                                    chunko.fillStates[k, l] = structureArray[i, j][k, l];
                                }
                            }
                        }
                        chunko.modificationCount = 1;
                        Files.saveChunk(chunko, false);
                    }
                }
            }
            public void saveInFile()
            {
                string savename = "";
                if (type == 3)
                {
                    savename = $"lake {name}";
                }
                else
                {
                    savename = $"{name} {structureNames[type]}";
                }
                using (StreamWriter f = new StreamWriter($"{currentDirectory}\\StructureData\\{screen.seed}\\{structChunkPosition.Item1}.{structChunkPosition.Item2}\\{savename}.txt", false))
                {
                    string stringo = $"Welcome to structure {name}'s file !";
                    stringo += $"{name} is a {structureNames[type]}.";
                    f.Write(stringo);
                }
            }
        }
        public class Room
        {
            public Nest nest;
            public int id;
            public long seed;
            public bool isToBeDestroyed = false;
            public (int x, int y) position;
            public List<(int x, int y)> tiles = new List<(int x, int y)>();
            public List<(int x, int y)> borders = new List<(int x, int y)>();
            public List<(int x, int y)> extremities = new List<(int x, int y)>(); //only used for corridors
            public int type;
            public List<int> linkedRoomsId;
            // type of room : 0 = mainRoom, 1 = normalRoom, 2 = HoneyRoom, 3 = Nursery, 10000 = corridor
            public Room(List<(int x, int y)> tileListToPut, int typeToPut, int idToPut, Nest nestToPut)
            {
                nest = nestToPut;
                tiles = tileListToPut;
                type = typeToPut;
                id = idToPut;
                position = tileListToPut[0];
                findSeed();
                linkedRoomsId = new List<int>();
            }
            public Room((int x, int y) posToPut, int typeToPut, int idToPut, Nest nestToPut)
            {
                nest = nestToPut;
                tiles = new List<(int x, int y)>();
                type = typeToPut;
                id = idToPut;
                position = posToPut;
                findSeed();
                linkedRoomsId = new List<int>();

                if (type == 0)
                {
                    makeBubbleRoom(posToPut, 0, null);
                }
                else if (type == 1)
                {
                    makeBubbleRoom(posToPut, 35 + rand.Next(200), null);
                }
                else
                {
                    isToBeDestroyed = true;
                }
                findBorders();
            }
            public Room((int x, int y) targetPos, int typeToPut, int idToPut, Room motherRoom, Nest nestToPut)
            {
                nest = nestToPut;
                tiles = new List<(int x, int y)>();
                type = typeToPut;
                id = idToPut;
                position = targetPos;
                findSeed();
                linkedRoomsId = new List<int>();

                if (type == 10000)
                {
                    makeCorridorBetweenPoints(new List<(int x, int y)>(motherRoom.borders), targetPos, 100);
                    if (isToBeDestroyed)
                    {
                        bool ohNo = true;
                    }
                }
                else
                {
                    isToBeDestroyed = true;
                }
                findBorders();
            }
            public Room((int x, int y) startPos, (int x, int y) targetPos, int typeToPut, int idToPut, Nest nestToPut)
            {
                nest = nestToPut;
                tiles = new List<(int x, int y)>();
                type = typeToPut;
                id = idToPut;
                position = startPos;
                findSeed();
                linkedRoomsId = new List<int>();

                
                if (type == 10000)
                {
                    makeCorridorBetweenPoints(new List<(int x, int y)> { startPos }, targetPos, 30);
                    if (isToBeDestroyed)
                    {
                        bool ohNo = true;
                    }
                }
                else
                {
                    isToBeDestroyed = true;
                }
                findBorders();
            }
            public long findSeed()
            {
                seed = nest.seed*(LCGxNeg(position.x) + LCGyNeg(position.y)) + LCGxPos(position.x)*LCGyPos(position.y);
                return seed;
            }
            public void findBorders()
            {
                List<(int x, int y)> tilesToTest = new List<(int x, int y)>(tiles);
                Dictionary<(int x, int y), bool> bordersDict = new Dictionary<(int x, int y), bool>();
                (int x, int y) tileToTest;
                (int x, int y) currentTile;
                while (tilesToTest.Count > 0)
                {
                    currentTile = tilesToTest[0];
                    tilesToTest.RemoveAt(0);

                    foreach((int x, int y) mod in neighbourArray)
                    {
                        tileToTest = (currentTile.x + mod.x, currentTile.y + mod.y);
                        if (!tiles.Contains(tileToTest))
                        {
                            bordersDict[tileToTest] = true;
                        }
                    }
                }
                borders = bordersDict.Keys.ToList();
            }
            public void fillTiles()
            {

                Dictionary<(int x, int y), Chunk> chunkDict = new Dictionary<(int x, int y), Chunk>();

                foreach((int x, int y) posToTest in tiles)
                {
                    (int x, int y) chunkPos = (Floor(posToTest.x, 32) / 32, Floor(posToTest.y, 32) / 32);
                    if (nest.screen.loadedChunks.ContainsKey(chunkPos)) { }
                    else
                    {
                        if (chunkDict.ContainsKey(chunkPos)) { }
                        else if (nest.screen.extraLoadedChunks.ContainsKey(chunkPos))
                        {
                            chunkDict.Add(chunkPos, nest.screen.extraLoadedChunks[chunkPos]);
                        }
                        else
                        {
                            chunkDict.Add(chunkPos, new Chunk(chunkPos, true, nest.screen));
                        }
                    }
                }

                foreach ((int x, int y) poso in tiles)
                {
                    (int x, int y) chunkPos = (Floor(poso.x, 32) / 32, Floor(poso.y, 32) / 32);
                    Chunk chunkToTest;
                    if (nest.screen.loadedChunks.ContainsKey(chunkPos)) { chunkToTest = nest.screen.loadedChunks[chunkPos]; }
                    else
                    {
                        if (!chunkDict.ContainsKey(chunkPos)) { chunkDict.Add(chunkPos, new Chunk(chunkPos, true, nest.screen)); }
                        chunkToTest = chunkDict[chunkPos];
                    }
                    chunkToTest.fillStates[(poso.x % 32 + 32) % 32, (poso.y % 32 + 32) % 32] = 0;
                    chunkToTest.modificationCount = 1;
                    chunkToTest.findTileColor((poso.x % 32 + 32) % 32, (poso.y % 32 + 32) % 32);
                }

                foreach (Chunk chunk in chunkDict.Values)
                {
                    Files.saveChunk(chunk, false);
                }
            }
            public void addToQueue(List<((int x, int y) position, float cost)> queue, ((int x, int y) position, float cost) valueToAdd)
            {
                int idx = 0;
                while (idx < queue.Count)
                {
                    if (valueToAdd.position == queue[idx].position) { return; }
                    if (valueToAdd.cost < queue[idx].cost)
                    {
                        queue.Insert(idx, valueToAdd);
                        return;
                    }
                    idx++;
                }
                queue.Add(valueToAdd);
            }
            public float heuristic((int x, int y) posToTest, (int x, int y) targetPos, float shape)
            {
                int diffX = Abs(posToTest.x - targetPos.x);
                int diffY = Abs(posToTest.y - targetPos.y);
                int diagNumber = Min(diffX, diffY);
                return (int)(diffX + diffY - 2 * diagNumber + shape * diagNumber);
            }
            public float heuristic((int x, int y) posToTest, List<(int x, int y)> targets, float shape)
            {
                int mini = 999999999;

                foreach ((int x, int y) pos in targets)
                {
                    int diffX = Abs(posToTest.x - pos.x);
                    int diffY = Abs(posToTest.y - pos.y);
                    int diagNumber = Min(diffX, diffY);
                    mini = Min((int)(diffX + diffY - 2 * diagNumber + shape * diagNumber), mini);
                }
                return mini;
            }
            public void makeBubbleRoom((int x, int y) centerPos, int forceSize, Room motherCorridor)
            {
                Dictionary<(int x, int y), Chunk> chunkDict = new Dictionary<(int x, int y), Chunk>();

                int tileAmount;
                if (forceSize == 0) { tileAmount = (int)(25 + (seed % 750)); }
                else { tileAmount = forceSize; }

                List<(int x, int y)> heuristicTargets = new List<(int x, int y)> { centerPos };
                long seedo = seed;
                int targetDistance = Sqrt((int)(tileAmount*0.5f))+2;
                for (int i = 0; i < seed % 4 + 5; i++)
                {
                    seedo = LCGxNeg(seedo);
                    heuristicTargets.Add((centerPos.x + (int)(seedo % 25) - 12, centerPos.y + (int)(LCGxPos(seedo) % 25) - 12));
                }

                Dictionary<(int x, int y), bool> tilesToFill = new Dictionary<(int x, int y), bool>();
                List<((int x, int y) pos, float cost)> tilesToTest = new List<((int x, int y) pos, float cost)> { (centerPos, 0) };
                (int x, int y) currentTile;
                (int x, int y) posToAdd;
                (int x, int y) posToTest;
                int repeatCounter = 0;
                while (tilesToTest.Count > 0 && repeatCounter < tileAmount)
                {
                    currentTile = tilesToTest[0].pos;
                    tilesToTest.RemoveAt(0);
                    foreach ((int x, int y) mod in bubbleNeighbourArray)
                    {
                        posToTest = (currentTile.x + mod.x, currentTile.y + mod.y);
                        (int x, int y) chunkPos = (Floor(posToTest.x, 32) / 32, Floor(posToTest.y, 32) / 32);
                        Chunk chunkToTest;
                        if (nest.screen.loadedChunks.ContainsKey(chunkPos)) { chunkToTest = nest.screen.loadedChunks[chunkPos]; }
                        else
                        {
                            if (chunkDict.ContainsKey(chunkPos)) { }
                            else if (nest.screen.extraLoadedChunks.ContainsKey(chunkPos))
                            {
                                chunkDict.Add(chunkPos, nest.screen.extraLoadedChunks[chunkPos]);
                            }
                            else
                            {
                                chunkDict.Add(chunkPos, new Chunk(chunkPos, true, nest.screen));
                            }
                            chunkToTest = chunkDict[chunkPos];
                        }
                        if (chunkToTest.fillStates[(posToTest.x % 32 + 32) % 32, (posToTest.y % 32 + 32) % 32] <= 0 && (motherCorridor == null || !motherCorridor.tiles.Contains(posToTest)) && repeatCounter > 0)
                        {
                            goto SkipToNextIteration;
                        }
                    }
                    tilesToFill[currentTile] = true;
                    foreach ((int x, int y) mod in neighbourArray)
                    {
                        posToAdd = (currentTile.x + mod.x, currentTile.y + mod.y);
                        if (!tilesToFill.ContainsKey(posToAdd) && (!nest.tiles.ContainsKey(posToAdd) || (motherCorridor != null && motherCorridor.tiles.Contains(posToAdd))))
                        {
                            addToQueue(tilesToTest, (posToAdd, repeatCounter + 20 * heuristic(posToAdd, heuristicTargets, nest.shape)));
                        }
                    }

                SkipToNextIteration:;
                    repeatCounter++;
                }

                tiles = tilesToFill.Keys.ToList();
                if (tiles.Count == 0)
                {
                    isToBeDestroyed = true;
                    return;
                }
            }





            public int manhattan((int x, int y) pos1, (int x, int y) pos2)
            {
                return Abs(pos1.x-pos2.x)+Abs(pos1.y-pos2.y);
            }
            public void sproutCorridor((int x, int y) startPos, int randomness)
            {
                int distance = 20;
                float angle = (float)(rand.NextDouble()*6.28318530718);
                (int x, int y) targetPos = (startPos.x + (int)(Math.Cos(angle) * distance), startPos.y + (int)(Math.Sin(angle) * distance));
                List<(int x, int y)> listo = new List<(int x, int y)>();
                listo.Add(startPos);
                makeCorridorBetweenPoints(listo, targetPos, randomness);
            }
            public void makeCorridorBetweenPoints(List<(int x, int y)> startPosList, (int x, int y) targetPos, int randomness)
            {

                Dictionary<(int x, int y), Chunk> chunkDict = new Dictionary<(int x, int y), Chunk>();

                long seedo = seed;

                Dictionary<(int x, int y), bool> tilesToFill = new Dictionary<(int x, int y), bool>();
                Dictionary<(int x, int y), (int x, int y)> originDict = new Dictionary<(int x, int y), (int x, int y)>();
                List<((int x, int y) pos, float cost)> tilesToTest = new List<((int x, int y) pos, float cost)>();
                foreach ((int x, int y) pos in startPosList)
                {
                    seedo = LCGz(Abs(LCGyPos(pos.x)) + Abs(LCGyNeg(pos.y)) + seedo);
                    originDict[pos] = pos;
                    tilesToTest.Add((pos, heuristic(pos, targetPos, 1.41421356237f) - 100000)); // to make sure all borders get tested at first
                }
                (int x, int y) currentTile;
                (int x, int y) posToTest;
                int repeatCounter = 0;
                int tileAmount = 2000;
                while (tilesToTest.Count > 0 && repeatCounter < tileAmount)
                {
                    currentTile = tilesToTest[0].pos;
                    if (currentTile == targetPos)
                    {
                        break;
                    }
                    tilesToTest.RemoveAt(0);
                    foreach ((int x, int y) mod in neighbourArray)
                    {
                        posToTest = (currentTile.x + mod.x, currentTile.y + mod.y);
                        (int x, int y) chunkPos = (Floor(posToTest.x, 32) / 32, Floor(posToTest.y, 32) / 32);
                        Chunk chunkToTest;
                        if (nest.screen.loadedChunks.ContainsKey(chunkPos)) { chunkToTest = nest.screen.loadedChunks[chunkPos]; }
                        else
                        {
                            if (chunkDict.ContainsKey(chunkPos)) { }
                            else if (nest.screen.extraLoadedChunks.ContainsKey(chunkPos))
                            {
                                chunkDict.Add(chunkPos, nest.screen.extraLoadedChunks[chunkPos]);
                            }
                            else
                            {
                                chunkDict.Add(chunkPos, new Chunk(chunkPos, true, nest.screen));
                            }
                            chunkToTest = chunkDict[chunkPos];
                        }
                        if (chunkToTest.fillStates[(posToTest.x % 32 + 32) % 32, (posToTest.y % 32 + 32) % 32] <= 0 && repeatCounter>=startPosList.Count && manhattan(currentTile, targetPos) > 2)
                        {
                            goto SkipToNextIteration;
                        }
                    }
                    foreach ((int x, int y) mod in neighbourArray)
                    {
                        posToTest = (currentTile.x + mod.x, currentTile.y + mod.y);
                        seedo = LCGz(Abs(LCGyPos(posToTest.x)) + Abs(LCGyNeg(posToTest.y)) + seedo);
                        if ((!originDict.ContainsKey(posToTest) && !nest.tiles.ContainsKey(posToTest)) || posToTest == targetPos)
                        {
                            addToQueue(tilesToTest, (posToTest, seedo % randomness + 20 * heuristic(posToTest, targetPos, 1.41421356237f)));
                            originDict[posToTest] = currentTile;
                        }
                    }

                SkipToNextIteration:;
                    repeatCounter++;
                }
                if (repeatCounter >= tileAmount || tilesToTest.Count == 0)
                {
                    isToBeDestroyed = true;
                    if (tilesToTest.Count == 0)
                    {
                        bool ohFuck = true;
                    }
                    return;
                }
                currentTile = targetPos;
                tilesToFill[targetPos] = true;
                while (!startPosList.Contains(currentTile))
                {
                    currentTile = originDict[currentTile];
                    tilesToFill[currentTile] = true;
                }

                tiles = tilesToFill.Keys.ToList();
                if (extremities.Count == 0) { extremities.Add(startPosList[0]); }
                extremities.Add(targetPos);
            }
        }
        public class Nest
        {
            public Form1.Screen screen;

            public int id;
            public long seed;

            public (int x, int y) position;

            // stats to build nest and what it looks like iggg
            public int roomSize; // bigger leads to bigger rooms
            public int connectivity; // bigger leads to less connexions (corridors)
            public int extensivity; // bigger leads to new rooms and corridors being dug out farther away from the center ig
            public float shape = 1.41421356237f;

            public Dictionary<(int x, int y), bool> tiles = new Dictionary<(int x, int y), bool>();
            public Dictionary<int, Room> rooms = new Dictionary<int, Room>();
            public int currentRoomId = 0;
            public Nest((int x, int y) posToPut, long seedToPut, Form1.Screen screenToPut)
            {
                screen = screenToPut;
                seed = seedToPut;
                position = posToPut;

                Room mainRoom;

                mainRoom = new Room(posToPut, 0, currentRoomId, this);
                if (!mainRoom.isToBeDestroyed)
                {
                    addRoom(mainRoom);
                    screen.activeNests.Add(this);
                }
            }
            public void addRoom(Room room)
            {
                rooms[room.id] = room;
                currentRoomId++;
                room.fillTiles();
                updateTiles();
            }
            public bool testRoomAvailability((int x, int y) centerPos, int amountToTest)
            {
                Dictionary<(int x, int y), Chunk> chunkDict = new Dictionary<(int x, int y), Chunk>();

                (int x, int y) currentPos;
                List<(int x, int y)> tilesToTest = new List<(int x, int y)>{ centerPos };
                (int x, int y) posToAdd;

                int repeatCounter = 0;
                while (repeatCounter < amountToTest)
                {
                    currentPos = tilesToTest[repeatCounter];
                    (int x, int y) chunkPos = (Floor(currentPos.x, 32) / 32, Floor(currentPos.y, 32) / 32);
                    Chunk chunkToTest;
                    if (screen.loadedChunks.ContainsKey(chunkPos)) { chunkToTest = screen.loadedChunks[chunkPos]; }
                    else
                    {
                        if (chunkDict.ContainsKey(chunkPos)) { }
                        else if (screen.extraLoadedChunks.ContainsKey(chunkPos))
                        {
                            chunkDict.Add(chunkPos, screen.extraLoadedChunks[chunkPos]);
                        }
                        else
                        {
                            chunkDict.Add(chunkPos, new Chunk(chunkPos, true, screen));
                        }
                        chunkToTest = chunkDict[chunkPos];
                    }
                    if (chunkToTest.fillStates[(currentPos.x % 32 + 32) % 32, (currentPos.y % 32 + 32) % 32] <= 0)
                    {
                        return false;
                    }

                    foreach ((int x, int y) mod in neighbourArray)
                    {
                        posToAdd = (currentPos.x + 2*mod.x, currentPos.y + 2*mod.y);
                        if (!tilesToTest.Contains(posToAdd)) { tilesToTest.Add(posToAdd); }
                    }

                    if (repeatCounter%2 == 0) // does not do what I want but whatever
                    {
                        foreach ((int x, int y) mod in diagNeighbourArray)
                        {
                            posToAdd = (currentPos.x + 2*mod.x, currentPos.y + 2*mod.y);
                            if (!tilesToTest.Contains(posToAdd)) { tilesToTest.Add(posToAdd); }
                        }
                    }
                    repeatCounter++;
                }
                return true;
            }
            public (bool valid, (int x, int y)) findNewRoomLocation()
            {
                int repeatCounter = 2;
                int distance;
                while (repeatCounter < 100)
                {
                    distance = repeatCounter*5;
                    float angle = (float)(rand.NextDouble() * 6.28318530718);
                    (int x, int y) targetPos = (position.x + (int)(Math.Cos(angle) * distance), position.y + (int)(Math.Sin(angle) * distance));

                    if (testRoomAvailability(targetPos, 45))
                    {
                        return (true, targetPos);
                    }
                    repeatCounter++;
                }
                return (false, (0, 0)); // CANNOT MAKE NOW ROOM ON 0, 0 LOL
            }
            public void forkRandomCorridorToRandomRoom()
            {
                int repeatCounter = 0;
                Room corridor;
                Room room;
                while (repeatCounter < 100)
                {
                    corridor = rooms[rand.Next(rooms.Count)];
                    if (corridor.type < 10000)
                    {
                        repeatCounter++;
                        continue;
                    }
                    room = rooms[rand.Next(rooms.Count)];
                    if (room.type >= 10000)
                    {
                        repeatCounter++;
                        continue;
                    }
                    corridor.makeCorridorBetweenPoints(corridor.tiles, room.borders[rand.Next(room.borders.Count)], 100);
                    updateTiles();
                    break;
                }
            }
            public void makeCorridorAndRoom((int x, int y) targetPos)
            {
                Room newRoom = new Room(targetPos, 1, currentRoomId, this);
                (int x, int y) originPoint = newRoom.borders[rand.Next(newRoom.borders.Count())];
                (int x, int y) targetPosCorridor = findClosestPoint(originPoint);

                Room corridoro = new Room(targetPosCorridor, 10000, currentRoomId+1, newRoom, this); //+1 is important ! else it will override lol
                if (!corridoro.isToBeDestroyed)
                {
                    addRoom(newRoom);
                    addRoom(corridoro);
                }
            }
            public void randomlyExtendNest()
            {
                (bool valid, (int x, int y) pos) returnTuple = findNewRoomLocation();
                if (returnTuple.valid)
                {
                    makeCorridorAndRoom(returnTuple.pos);
                }
                //forkRandomCorridorToRandomRoom();
            }
            public (int x, int y) findClosestPoint((int x, int y) posToTest)
            {
                int diffX;
                int diffY;
                int diagNumber;
                int distance;
                int distanceMin = 1000000;
                (int x, int y) posMin = (0, 0);
                foreach ((int x, int y) pos in tiles.Keys)
                {
                    diffX = Abs(posToTest.x - pos.x);
                    diffY = Abs(posToTest.y - pos.y);
                    diagNumber = Min(diffX, diffY);
                    distance = (int)(diffX + diffY - 2 * diagNumber + 1.41421356237f * diagNumber);
                    if (distance < distanceMin)
                    {
                        posMin = pos;
                        distanceMin = distance;
                    }
                }
                return posMin;
            }
            public void updateTiles()
            {
                tiles = new Dictionary<(int x, int y), bool>();
                foreach (Room room in rooms.Values)
                {
                    foreach ((int x, int y) tile in room.tiles)
                    {
                        tiles[tile] = true;
                    }
                }
            }
            public void saveInFile(Form1.Screen screen)
            {
                string savename = $"Hornet nest {id}";

                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new JavaScriptDateTimeConverter());
                serializer.NullValueHandling = NullValueHandling.Ignore;

                using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\NestData\\{screen.seed}\\{savename}.json"))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, this);
                }
            }
        }
    }
}
