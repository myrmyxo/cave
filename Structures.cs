﻿using System;
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
                centerChunkPos = (Floor(posX, 16) / 16, Floor(posY, 16) / 16);

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
                        chunkList[lenX].Add(new Chunk(startingChunk[0] + lenX, startingChunk[1] + j, screen.seed, true, screen));
                    }

                    for (int i = 0; i < 16; i++)
                    {
                        tileList.Add(new List<int>());
                        for (int j = 0; j < lenY * 16; j++)
                        {
                            tileList[lenX * 16 + i].Add(chunkList[lenX][j / 16].fillStates[i, j % 16]);
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
                        listo.Add(new Chunk(startingChunk[0], startingChunk[1] + j, screen.seed, true, screen));
                    }
                    chunkList.Insert(0, listo);

                    for (int i = 0; i < 16; i++)
                    {
                        tileList.Insert(i, new List<int>());
                        for (int j = 0; j < lenY * 16; j++)
                        {
                            tileList[i].Add(chunkList[0][j / 16].fillStates[i, j % 16]);
                        }
                    }
                }

                if (y > 0)
                {
                    int lenX = chunkList.Count();
                    int lenY = chunkList[0].Count();

                    for (int i = 0; i < lenX; i++)
                    {
                        chunkList[i].Add(new Chunk(startingChunk[0] + i, startingChunk[1] + lenY, screen.seed, true, screen));
                    }

                    for (int i = 0; i < lenX * 16; i++)
                    {
                        for (int j = 0; j < 16; j++)
                        {
                            tileList[i].Add(chunkList[i / 16][lenY].fillStates[i % 16, j]);
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
                        chunkList[i].Insert(0, new Chunk(startingChunk[0] + i, startingChunk[1], screen.seed, true, screen));
                    }

                    for (int i = 0; i < lenX * 16; i++)
                    {
                        for (int j = 0; j < 16; j++)
                        {
                            tileList[i].Insert(j, chunkList[i / 16][0].fillStates[i % 16, j]);
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
                chunkList[0].Add(new Chunk(startChunk[0], startChunk[1], screen.seed, true, screen));

                List<List<int>> tileList = new List<List<int>>();
                for (int i = 0; i < 16; i++)
                {
                    tileList.Add(new List<int>());
                    for (int j = 0; j < 16; j++)
                    {
                        tileList[i].Add(chunkList[0][0].fillStates[i, j]);
                    }
                }

                int[] testPos = new int[2] { (int)(seedo % 16), (int)((seedo / 16) % 16) };
                int[] testPosLeft;
                int[] testPosRight;
                bool[] hitWallArray = new bool[2] { false, false };

                // Loop 1 : find a tile in the base chunk, try going down till ground is found, if not found soon enough cancel
                // Loop 2 : bottom found : check if it's really the bottom or not (water can't possibly flow out by the sides), try to find the real bottom for a bit if not, if not foudn cancel.

                if (tileList[testPos[0]][testPos[1]] == 1) // if in the wall/ceiling abandon the lake
                {
                    goto abandonLake;
                }
                int repeatCounter = 0;
                while (repeatCounter < 100)
                {
                    if (tileList[testPos[0]][testPos[1]] != 0) // ground has been attained, will happen almost everytime
                    {
                        testPos[1] -= 1;
                        if (testPos[1] < 0)
                        {
                            extendLakeArrays(startChunk, tileList, chunkList, 0, -1);
                            testPos[1] += 16;
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
                                    testPos[0] += 16;
                                    testPosLeft[0] += 16;
                                    testPosRight[0] += 16;
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
                                if (testPosRight[0] >= chunkList.Count * 16)
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
                        if (testPos[1] >= chunkList[0].Count * 16)
                        {
                            extendLakeArrays(startChunk, tileList, chunkList, 0, 1);
                        }
                    }
                    repeatCounter++;
                }

                goto abandonLake; // the loop ran too much, too hard to find a place to put lake, abandon the shit

                outOfLoop:;

                // Bottom has been found ! fill up layer by layer using brute force pathfinding.
                // find the type of lake (if it is in fairy biome it is fairy liquid etc).

                Chunk chunkToTest = chunkList[testPos[0] / 16][testPos[1] / 16];
                (int, int) chunkTestPos = (testPos[0] % 16, testPos[1] % 16);

                int liquidTypeToFill = -2;
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

                int tilesToFill = Min((int)(seedo % 1009), (int)(seedo % 1277)) + 1;
                int tilesFilled = 0;
                List<(int, int)> tilesToAdd;
                testPos[1] += 1; // needed to counteract the first -= 1 just under
                while (tilesFilled < tilesToFill) // it can overfill, that's not a problem
                {
                    testPos[1] -= 1; // no need to check for out of bounds array cause the first test is always negative : at least 1 tile above loaded
                    if (testPos[1] < 0)
                    {
                        extendLakeArrays(startChunk, tileList, chunkList, 0, -1);
                        testPos[1] += 16;
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
                                    tilesToAdd[i] = (tilesToAdd[i].Item1 + 16, tilesToAdd[i].Item2);
                                }
                                testPos[0] += 16;
                                testPosLeft[0] += 16;
                                testPosRight[0] += 16;
                            }
                            if (tileList[testPosLeft[0]][testPosLeft[1] + 1] == 0)
                            {
                                goto fillAndSaveLake;
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
                            if (testPosRight[0] >= chunkList.Count * 16)
                            {
                                extendLakeArrays(startChunk, tileList, chunkList, 1, 0);
                            }
                            if (tileList[testPosRight[0]][testPosRight[1] + 1] == 0)
                            {
                                goto fillAndSaveLake;
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
                            tileList[tilesToAdd[i].Item1][tilesToAdd[i].Item2] = -1;
                        }
                        tilesFilled += tilesToAdd.Count;
                    }
                    else
                    {
                        goto fillAndSaveLake; // do not update water array, as uhhh it'd overflow lol. No writing of the lil numbers :)
                    }
                }

                fillAndSaveLake:;

                Bitmap bitmapo = new Bitmap(tileList.Count, tileList[0].Count);

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
                        if (tileList[i][j] == -1) { color = Color.Blue; }

                        bitmapo.SetPixel(i, j, color);
                        if (tileList[i][j] == -1)
                        {
                            chunkList[i / 16][j / 16].fillStates[i % 16, j % 16] = liquidTypeToFill;
                            chunkList[i / 16][j / 16].modificationCount = 1;
                        }
                    }
                }

                bitmapo.Save($"{currentDirectory}\\bitmapos\\bitmapo{rand.Next(10000)}.png");

                foreach (List<Chunk> chunko in chunkList)
                {
                    foreach (Chunk chunk in chunko)
                    {
                        chunk.saveChunk(false);
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

                        float sizo = (size.Item1 * (8 - sawBladeSeesaw(angle, 72) * 0.1f));

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
                (int, int) tupelo = (centerCoords.Item1 - 1, centerCoords.Item2 - 1);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 2;
                tupelo = (centerCoords.Item1, centerCoords.Item2 - 1);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 0;
                tupelo = (centerCoords.Item1 + 1, centerCoords.Item2 - 1);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 2;
                tupelo = (centerCoords.Item1 - 1, centerCoords.Item2);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 0;
                tupelo = (centerCoords.Item1, centerCoords.Item2);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 1;
                tupelo = (centerCoords.Item1 + 1, centerCoords.Item2);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 0;
                tupelo = (centerCoords.Item1 - 1, centerCoords.Item2 + 1);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 2;
                tupelo = (centerCoords.Item1, centerCoords.Item2 + 1);
                structureArray[tupelo.Item1 / 16, tupelo.Item2 / 16][tupelo.Item1 % 16, tupelo.Item2 % 16] = 0;
                tupelo = (centerCoords.Item1 + 1, centerCoords.Item2 + 1);
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
                        chunko.saveChunk(false);
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
        public class Nest
        {
            public bool naturallyGenerated;

        }

    }
}