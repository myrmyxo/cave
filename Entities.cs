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
using static Cave.Form1;
using static Cave.Form1.Globals;
using static Cave.MathF;
using static Cave.Sprites;
using static Cave.Structures;
using static Cave.Entities;

namespace Cave
{
    public class Entities
    {
        public class Entity
        {
            public Form1.Screen screen;

            public int seed;
            public int type; // 0 = fairy , 1 = frog , 2 = fish, 3 = hornet
            public int subType;
            public int state; // 0 = idle I guess idk
            public float realPosX = 0;
            public float realPosY = 0;
            public int posX = 0;
            public int posY = 0;
            public float speedX = 0;
            public float speedY = 0;
            public Color color;

            public Dictionary<(int index, int subType, int typeOfElement), int> inventoryQuantities;
            public List<(int index, int subType, int typeOfElement)> inventoryElements;
            public int elementsPossessed = 0;
            public int inventoryCursor = 0;

            public float timeAtLastDig = -9999;
            public float timeAtLastPlace = -9999;

            public bool isDeadAndShouldDisappear = false;

            public Entity(int posXt, int posYt, int typet, int subTypet, int seedt, Form1.Screen screenToPut)
            {
                screen = screenToPut;
                realPosX = posXt;
                posX = posXt;
                realPosY = posYt;
                posY = posYt;
                type = typet;
                subType = subTypet;
                seed = seedt;
                if (type == 3) { initializeInventory(); }
                state = 0;
                color = findColor();
            }
            public Entity(Chunk chunk, Form1.Screen screenToPut)
            {
                screen = screenToPut;
                seed = LCGint1(Abs((int)chunk.chunkSeed));
                placeEntity(chunk);
                findType(chunk);
                color = findColor();
                if (type == 3) { initializeInventory(); }
            }
            public Entity(Chunk chunk, (int, int) positionToPut, int typeToPut, int subTypeToPut, Form1.Screen screenToPut)
            {
                screen = screenToPut;
                posX = positionToPut.Item1;
                realPosX = posX;
                posY = positionToPut.Item2;
                realPosY = posY;
                type = typeToPut;
                subType = subTypeToPut;
                seed = rand.Next(1000000000); //                               FALSE RANDOM NOT SEEDED ARGHHEHEEEE
                if (type == 3) { initializeInventory(); }
                color = findColor();
            }
            public void findType(Chunk chunk)
            {
                int biome = chunk.biomeIndex[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16][0].Item1;
                if (chunk.fillStates[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16] < 0)
                {
                    type = 2;
                    subType = 0;
                }
                else if (biome == 5)
                {
                    type = 0;
                    subType = 0;
                }
                else if (biome == 6)
                {
                    type = 0;
                    subType = 1;
                }
                else if (biome == 7)
                {
                    type = 0;
                    subType = 2;
                }
                else
                {
                    type = 1;
                    subType = 0;
                }
            }
            public Color findColor()
            {
                int hueVar = seed%101 - 50;
                int shadeVar = seed%61 - 30;
                if (type == 0)
                {
                    if (subType == 1)
                    {
                        return Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar);
                    }
                    if (subType == 2)
                    {
                        hueVar = Abs((int)(hueVar * 0.4f));
                        shadeVar = Abs(shadeVar);
                        return Color.FromArgb(255 - hueVar - shadeVar, 255 - hueVar - shadeVar, 255 - shadeVar);
                    }
                    return Color.FromArgb(130 + hueVar + shadeVar, 130 - hueVar + shadeVar, 210 + shadeVar);
                }
                if (type == 1)
                {
                    return Color.FromArgb(90 + hueVar + shadeVar, 210 + shadeVar, 110 - hueVar + shadeVar);
                }
                if (type == 2)
                {
                    return Color.FromArgb(190 + shadeVar, 80 - hueVar + shadeVar, 80 + hueVar + shadeVar);
                }
                if (type == 3)
                {
                    return Color.FromArgb(190 + (int)(hueVar * 0.2f) + shadeVar, 190 - (int)(hueVar * 0.2f) + shadeVar, 80 + shadeVar);
                }
                return Color.Red;
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
                    if (chunk.fillStates[randX, randY] <= 0)
                    {
                        posX = (int)chunk.position.Item1 * 16 + randX;
                        realPosX = posX;
                        posY = (int)chunk.position.Item2 * 16 + randY;
                        realPosY = posY;
                        return;
                    }
                    counto += 1;
                }
                isDeadAndShouldDisappear = true;
            }
            public void testDigPlace()
            {
                if (type == 3)
                {
                    if (timeAtLastDig + 0.5f + rand.NextDouble() * 3 < timeElapsed)
                    {
                        int direction = rand.Next(4);
                        int digorplace = elementsPossessed + rand.Next(5) - 5;
                        if (digorplace < 0)
                        {
                            Dig(posX + directionArray[direction].Item1, posY + directionArray[direction].Item2);
                        }
                        else
                        {
                            Place(posX + directionArray[direction].Item1, posY + directionArray[direction].Item2);
                        }
                    }

                    timeAtLastPlace = timeAtLastDig;
                }
            }
            public void moveEntity()
            {
                (int, int) chunkPos;
                if (type == 0 || type == 3)
                {
                    if (state == 0) // idle
                    {
                        if (Abs(speedX) > 0)
                        {
                            if (Abs(speedX) > 0.5f)
                            {
                                speedX = speedX - Sign(speedX) * 0.5f;
                            }
                            else
                            {
                                speedX = 0;
                            }
                        }
                        if (Abs(speedY) > 0)
                        {
                            if (Abs(speedY) > 0.5f)
                            {
                                speedY = speedY - Sign(speedY) * 0.5f;
                            }
                            else
                            {
                                speedY = 0;
                            }
                        }
                        if (rand.Next(100) == 0)
                        {
                            state = 1;
                        }
                    }
                    else if (state == 1) // moving in the air
                    {
                        speedX += (float)(rand.NextDouble()) - 0.5f;
                        speedY += (float)(rand.NextDouble()) - 0.5f;
                        if (rand.Next(1000) < 3)
                        {
                            state = 0;
                        }
                    }
                    else if (state == 2) // moving in the water
                    {
                        if (Abs(speedX) > 0)
                        {
                            if (Abs(speedX) > 0.5f)
                            {
                                speedX = speedX - Sign(speedX) * 0.5f;
                            }
                            else
                            {
                                speedX = 0;
                            }
                        }
                        if (Abs(speedY) > 0)
                        {
                            if (Abs(speedY) > 0.5f)
                            {
                                speedY = speedY - Sign(speedY) * 0.5f;
                            }
                            else
                            {
                                speedY = 0;
                            }
                        }
                        speedX += (float)(rand.NextDouble()) - 0.5f;
                        speedY += (float)(rand.NextDouble());
                    }
                    else { state = 0; }
                }
                else if (type == 1)
                {
                    speedY -= 0.5f;
                    chunkPos = screen.findChunkAbsoluteIndex(posX, posY - 1); // +1 cause coordinates are inverted lol
                    if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                    {
                        if (chunkToTest.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 31) % 16] > 0)
                        {
                            speedX = Sign(speedX) * (Max(0, Abs(speedX) * (0.85f) - 0.2f));
                            if (rand.NextDouble() > 0.05f)
                            {
                                speedX += (float)(rand.NextDouble()) * 5 - 2.5f;
                                speedY += (float)(rand.NextDouble()) * 2.5f;
                            }
                        }
                    }
                }
                else if (type == 2) // fish
                {
                    chunkPos = screen.findChunkAbsoluteIndex(posX, posY); // +1 cause coordinates are inverted lol
                    Chunk chunkToTesta = screen.loadedChunks[chunkPos];
                    if (chunkToTesta.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 32) % 16] < 0)
                    {
                        if (state >= 2)
                        {
                            state = 1;
                        }
                    }
                    else
                    {
                        state = 2;
                    }
                    if (state == 0) // idle
                    {
                        if (Abs(speedX) > 0)
                        {
                            if (Abs(speedX) > 0.5f)
                            {
                                speedX = speedX - Sign(speedX) * 0.5f;
                            }
                            else
                            {
                                speedX = 0;
                            }
                        }
                        if (Abs(speedY) > 0)
                        {
                            if (Abs(speedY) > 0.5f)
                            {
                                speedY = speedY - Sign(speedY) * 0.5f;
                            }
                            else
                            {
                                speedY = 0;
                            }
                        }
                        if (rand.Next(50) == 0)
                        {
                            state = 1;
                        }
                    }
                    else if (state == 1) // moving in water
                    {
                        speedX = speedX * 0.9f - Sign(speedX) * 0.12f;
                        speedY = speedY * 0.9f - Sign(speedY) * 0.12f;

                        speedX += (float)(rand.NextDouble()) - 0.5f;
                        speedY += (float)(rand.NextDouble()) - 0.5f;
                        if (rand.Next(100) == 0)
                        {
                            speedX = (float)(rand.NextDouble()) * 3 - 1.5f;
                            speedY = (float)(rand.NextDouble()) * 3 - 1.5f;
                        }
                        else if (rand.Next(1000) == 0)
                        {
                            state = 0;
                        }
                    }
                    else if (state == 2) // outside water
                    {
                        speedY -= 0.5f;
                        chunkPos = screen.findChunkAbsoluteIndex(posX, posY - 1); // +1 cause coordinates are inverted lol (no)

                        if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                        {
                            if (chunkToTest.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 31) % 16] > 0)
                            {
                                speedX = speedX * 0.9f - Sign(speedX) * 0.12f;
                                if (rand.NextDouble() > 0.05f)
                                {
                                    speedX += (float)(rand.NextDouble()) * 2 - 1;
                                    speedY = (float)(rand.NextDouble()) * 1.5f - 0.5f;
                                }
                            }
                        }
                    }
                }
                if (type != 2)
                {
                    chunkPos = screen.findChunkAbsoluteIndex(posX, posY);
                    if (screen.loadedChunks[chunkPos].fillStates[(posX % 16 + 32) % 16, (posY % 16 + 32) % 16] < 0)
                    {
                        speedX = speedX * 0.85f - Sign(speedX) * 0.15f;
                        speedY = speedY * 0.85f - Sign(speedY) * 0.15f;
                    }
                }
                float toMoveX = speedX;
                float toMoveY = speedY;

                while (Abs(toMoveY) > 0)
                {
                    chunkPos = screen.findChunkAbsoluteIndex(posX, posY + (int)Sign(toMoveY));
                    if (chunkPos.Item2 < screen.chunkY || chunkPos.Item2 >= screen.chunkY + screen.chunkResolution)
                    {
                        posY += (int)Sign(toMoveY);
                        saveEntity();
                        screen.entitesToRemove.Add(this);
                        return;
                    }
                    Chunk chunkToTest = screen.loadedChunks[chunkPos];
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
                    chunkPos = screen.findChunkAbsoluteIndex(posX + (int)Sign(toMoveX), posY);
                    if (chunkPos.Item1 < screen.chunkX || chunkPos.Item1 >= screen.chunkX + screen.chunkResolution)
                    {
                        posX += (int)Sign(toMoveX);
                        saveEntity();
                        screen.entitesToRemove.Add(this);
                        return;
                    }
                    Chunk chunkToTest = screen.loadedChunks[chunkPos];
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

                // test what happens if in special liquids (fairy lake, lava...)

                chunkPos = screen.findChunkAbsoluteIndex(posX, posY);
                Chunk chunkToTesto = screen.loadedChunks[chunkPos];
                if (type != 0 && chunkToTesto.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 32) % 16] == -3)
                {
                    if (rand.Next(10) == 0)
                    {
                        type = 0;
                        this.color = Color.Purple;
                    }
                }
                if (type != 2 && chunkToTesto.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 32) % 16] == -4)
                {
                    if (rand.Next(10) == 0)
                    {
                        screen.entitesToRemove.Add(this);
                    }
                }
            }
            public void saveEntity()
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
                    entityText += type + ";" + subType + ";";
                    entityText += seed + ";";
                    lines[1] += entityText;

                    System.IO.File.WriteAllLines($"{currentDirectory}\\ChunkData\\{screen.seed}\\{position.Item1}.{position.Item2}.txt", lines);
                }
                else
                {
                    using (StreamWriter f = new StreamWriter($"{currentDirectory}\\ChunkData\\{screen.seed}\\{position.Item1}.{position.Item2}.txt", false))
                    {
                        string stringo = "0;\n";
                        stringo += posX + ";" + posY + ";";
                        stringo += type + ";" + subType + ";";
                        stringo += seed + ";";
                        stringo += "\n\nx\n";
                        f.Write(stringo);
                    }
                }
            }
            public void initializeInventory()
            {
                inventoryQuantities = new Dictionary<(int index, int subType, int typeOfElement), int>
                {

                };
                inventoryElements = new List<(int index, int subType, int typeOfElement)>
                {

                };
            }
            public void moveInventoryCursor(int value)
            {
                int counto = inventoryElements.Count;
                if (counto == 0) { inventoryCursor = 0; return; }
                inventoryCursor = ((inventoryCursor + value) % counto + counto) % counto;
            }
            public void Dig(int posToDigX, int posToDigY)
            {
                (int, int) chunkPos = screen.findChunkAbsoluteIndex(posToDigX, posToDigY);
                if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest)) { return; }
                int tileContent = chunkToTest.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16];
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
                    chunkToTest.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16] = 0;
                    chunkToTest.findTileColor((posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16);
                    chunkToTest.testLiquidUnstableAir(posToDigX, posToDigY);
                    chunkToTest.modificationCount += 1;
                    elementsPossessed++;
                    timeAtLastDig = timeElapsed;
                }
            }
            public void Place(int posToDigX, int posToDigY)
            {
                (int, int) chunkPos = screen.findChunkAbsoluteIndex(posToDigX, posToDigY);
                if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest)) { return; }
                (int index, int subType, int typeOfElement) tileContent = inventoryElements[inventoryCursor];
                int tileState = chunkToTest.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16];
                if (tileState == 0 || tileState < 0 && tileContent.typeOfElement == 1)
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
                    if (tileContent.typeOfElement == 1)
                    {
                        Entity newEntity = new Entity(chunkToTest, (posToDigX, posToDigY), tileContent.index, tileContent.subType, screen);
                        screen.activeEntities.Add(newEntity);
                        timeAtLastPlace = timeElapsed;
                    }
                    else if (tileContent.typeOfElement == 2)
                    {
                        Plant newPlant = new Plant(posToDigX, posToDigY, tileContent.index, rand.Next(1000000), tileContent.subType, chunkToTest, screen);
                        if (!newPlant.isDeadAndShouldDisappear) { screen.activePlants.Add(newPlant); }
                        timeAtLastPlace = timeElapsed;
                    }
                    else
                    {
                        chunkToTest.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16] = tileContent.index;
                        chunkToTest.findTileColor((posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16);
                        chunkToTest.testLiquidUnstableLiquid(posToDigX, posToDigY);
                        chunkToTest.modificationCount += 1;
                        timeAtLastPlace = timeElapsed;
                    }
                    elementsPossessed--;
                }
            }
        }
        public class Branch
        {
            public Plant motherPlant;
            public int seed;
            public int growthLevel;
            public Dictionary<(int x, int y), int> fillStates;
            public List<Branch> childBranches;
            public List<Flower> childFlowers;
        }
        public class Flower
        {
            public Plant motherPlant;
            public int seed;
            public int growthLevel;
            public Dictionary<(int x, int y), int> fillStates;
            public List<Branch> childBranches;
            public List<Flower> childFlowers;
        }
        public class Plant
        {
            public Form1.Screen screen;
            public Chunk chunk;

            public int seed;
            public int type;
            public int subType;
            public int state;
            public int growthLevel;
            public int maxGrowthLevel;
            public int posX = 0;
            public int posY = 0;
            public int attachPoint; // 0 ground, 1 leftWall, 2 rightWall, 3 ceiling
            public Dictionary<int, Color> colorDict;

            public float timeAtLastGrowth = timeElapsed;

            public Bitmap bitmap;
            public Dictionary<(int, int), int> fillStates;
            public int[] posOffset;
            public int[] bounds;

            public bool isDeadAndShouldDisappear = false;
            public bool isStable = false;
            public Plant(int posXt, int posYt, int typet, int subTypet, int seedt, Chunk chunkToPut, Form1.Screen screenToPut)
            {
                screen = screenToPut;
                chunk = chunkToPut;
                posX = posXt;
                posY = posYt;
                type = typet;
                subType = subTypet;
                state = 0;
                seed = seedt;
                findColors();
                makeBitmap(false, -1);
                timeAtLastGrowth = timeElapsed;
            }
            public Plant(Chunk chunkToPut, Form1.Screen screenToPut)
            {
                screen = screenToPut;
                chunk = chunkToPut;
                seed = LCGint1(Abs((int)chunk.chunkSeed));
                placePlant();
                findType();
                findColors();
                makeBitmap(true, -1);
                timeAtLastGrowth = timeElapsed;
            }
            public Plant(Chunk chunkToPut, (int, int) positionToPut, int typeToPut, int subTypeToPut, Form1.Screen screenToPut)
            {
                screen = screenToPut;
                chunk = chunkToPut;
                posX = positionToPut.Item1;
                posY = positionToPut.Item2;
                type = typeToPut;
                subType = subTypeToPut;
                growthLevel = 1;
                seed = rand.Next(1000000000); //                               FALSE RANDOM NOT SEEDED ARGHHEHEEEE
                testPlantPosition();
                findColors();
                makeBitmap(false, -1);
                timeAtLastGrowth = timeElapsed;
            }
            public void findType()
            {
                int biome = chunk.biomeIndex[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16][0].Item1;
                if (attachPoint == 0)
                {
                    if (chunk.fillStates[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16] < 0)
                    {
                        type = 2;
                        subType = 0;
                    }
                    else if (biome == 3) // forest
                    {
                        if (rand.Next(2) == 0)
                        {
                            type = 0;
                            subType = 0;
                        }
                        else
                        {
                            type = 1;
                            subType = 0;
                        }
                    }
                    else if (biome == 5) // fairy
                    {
                        type = 4;
                        subType = 0;
                    }
                    else if (biome == 6) // obsidian
                    {
                        if (rand.Next(100) == 0)
                        {
                            type = 1;
                            subType = 0;
                        }
                        else
                        {
                            type = 3;
                            subType = 0;
                        }
                    }
                    else
                    {
                        type = 0;
                        subType = 0;
                    }
                }

                else if (attachPoint == 3)
                {
                    if (chunk.fillStates[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16] < 0)
                    {
                        type = 2;
                        subType = 1;
                    }
                    else if (biome == 6) // obsidian
                    {
                        type = 5;
                        subType = 1;
                    }
                    else
                    {
                        if (seed % 7 == 0)
                        {
                            type = 6;
                            subType = 0;
                        }
                        else
                        {
                            type = 5;
                            subType = 0;
                        }
                    }
                }
            }
            public void findColors()
            {
                colorDict = new Dictionary<int, Color>();
                int seedo = LCGint1(seed);
                int hueVar = (int)(seedo%101) - 50;
                seedo = LCGint1(seed);
                int shadeVar = (int)(seedo%61) - 30;
                int biome = chunk.biomeIndex[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16][0].Item1;
                if (type == 0) // normal
                {
                    colorDict.Add(1, Color.FromArgb(50 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                    return;
                }
                if (type == 1) // woody
                {
                    colorDict.Add(1, Color.FromArgb(50 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                    colorDict.Add(2, Color.FromArgb(140 + (int)(hueVar*0.3f) - shadeVar, 140 - (int)(hueVar * 0.3f) - shadeVar, 50 - shadeVar));
                    return;
                }
                if (type == 2) // kelp
                {
                    colorDict.Add(3, Color.FromArgb(30 - shadeVar, 90 - shadeVar + hueVar, 140 - shadeVar - hueVar));
                    return;
                }
                if (type == 3) // obsidian
                {
                    colorDict.Add(1, Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar));
                    return;
                }
                if (type == 4) // mushroom
                {
                    colorDict.Add(4, Color.FromArgb(180 + shadeVar, 160 + shadeVar, 165 + shadeVar));
                    colorDict.Add(5, Color.FromArgb(140 - shadeVar, 120 + hueVar, 170 - hueVar));
                    return;
                }
                if (type == 5) // vine
                {
                    if (subType == 0)
                    {
                        colorDict.Add(1, Color.FromArgb(50 - shadeVar, 120 - hueVar - shadeVar, 50 - shadeVar));
                        colorDict.Add(6, Color.FromArgb(170 - shadeVar, 120 - hueVar - shadeVar, 150 - shadeVar));
                        colorDict.Add(7, Color.FromArgb(170 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                        return;
                    }
                    else if (subType == 1)
                    {
                        colorDict.Add(1, Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar));
                        colorDict.Add(6, Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar));
                        colorDict.Add(7, Color.FromArgb(220 + shadeVar, 220 + shadeVar, 220 + shadeVar));
                        return;
                    }
                }
            }
            public bool testIfPositionEmpty(int modX, int modY)
            {
                (int x, int y) pixelPos = (posX + modX, posY + modY);
                (int x, int y) chunkPos = (Floor(pixelPos.x, 16) / 16, Floor(pixelPos.y, 16) / 16);

                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    if (screen.loadedChunks[chunkPos].fillStates[(pixelPos.x % 16 + 16) % 16, (pixelPos.y % 16 + 16) % 16] > 0)
                    {
                        return false;
                    }
                    return true;
                }

                if (!screen.extraLoadedChunks.ContainsKey(chunkPos)) { screen.extraLoadedChunks.Add(chunkPos, new Chunk(chunkPos, screen.seed, true, screen)); }
                if (screen.extraLoadedChunks[chunkPos].fillStates[(pixelPos.x % 16 + 16) % 16, (pixelPos.y % 16 + 16) % 16] > 0)
                {
                    return false;
                }
                return true;
            }
            public void makeBitmap(bool spawning, int growthLevelToTest) // 0 = nothing, 1 = plantMatter, 2 = wood, 3 = aquaticPlantMatter, 4 = mushroomStem, 5 = mushroomCap, 6 = petal, 7 = flowerPollen
            {
                //List<List<int>> listo = new List<List<int>>();
                fillStates = new Dictionary<(int, int), int>();
                bounds = new int[4] { 0, 0, 0, 0 };
                posOffset = new int[3] { 0, 0, 0 };
                int seedo = seed;

                int previousGrowthLevel = -1;
                if (growthLevel >= 0) { previousGrowthLevel = growthLevel; }
                growthLevel = growthLevelToTest;

                if (type == 0) // normal plant
                {
                    maxGrowthLevel = 1 + (int)(seed % 5);
                    growthLevel = Min(growthLevel, maxGrowthLevel);
                    int[] drawPos = new int[2] { 0, 0 };

                    for (int i = 0; i < growthLevel; i++)
                    {
                        seedo = LCGint1(seedo);
                        int resulto = seedo % 3;
                        if (resulto == 0 && i % 2 == 1)
                        {
                            drawPos[0]--;
                        }
                        else if (resulto == 2 && i % 2 == 1)
                        {
                            drawPos[0]++;
                        }

                        if (testIfPositionEmpty(drawPos[0], drawPos[1]))
                        {
                            fillStates[(drawPos[0], drawPos[1])] = 1;

                            drawPos[1] += 1;
                        }
                        else
                        {
                            growthLevel = i;
                            break;
                        }
                    }
                }
                else if (type == 1) // woody tree
                {
                    maxGrowthLevel = 10 + seed % 40;
                    growthLevel = Min(growthLevel, maxGrowthLevel);
                    int[] drawPos = new int[2] { 0, 0 };
                    List<(int x, int y)> drawnPos = new List<(int, int)>();

                    for (int i = 0; i < growthLevel; i++)
                    {
                        seedo = LCGint1(seedo);
                        int resulto = seedo % 3;
                        if (resulto == 0 && i == 2)
                        {
                            drawPos[0]--;
                        }
                        else if (resulto == 2 && i == 2)
                        {
                            drawPos[0]++;
                        }

                        if (testIfPositionEmpty(drawPos[0], drawPos[1]))
                        {
                            fillStates[(drawPos[0], drawPos[1])] = 2;
                            drawnPos.Add((drawPos[0], drawPos[1]));

                            drawPos[1] += 1;
                        }
                        else
                        {
                            growthLevel = i;
                            break;
                        }
                    }

                    seedo = seed;
                    int seeda;
                    int spacingOfBranches = 2 + seedo % 3;
                    List<int[]> endOfBranchesPos = new List<int[]> { new int[2] { drawPos[0], drawPos[1] } };

                    for (int rep = 0; true; rep += 1)
                    {
                        int direction; // -1 = left, 1 = right
                        direction = (seedo % 2) * 2 - 1;

                        seeda = LCGint2(seedo);
                        if (seeda % 5 < 2)
                        {
                            seedo = LCGint1(seedo);
                            continue;
                        }
                        drawPos[1] = 1 + rep * spacingOfBranches + seeda % 4;
                        if (drawPos[1] >= growthLevel)
                        {
                            break;
                        }
                        drawPos[0] = drawnPos[drawPos[1]].x;

                        int startPosY = drawPos[1];
                        int maxBranchSize = Max(seeda % Max(maxGrowthLevel - startPosY, 1), seeda % Max(maxGrowthLevel - startPosY + 1, 1));
                        for (int i = 0; i < Min(growthLevel - startPosY, maxBranchSize); i++)
                        {
                            seeda = LCGint1(seeda);
                            int resulto = seeda % 3;
                            if (direction == -1 && (i < 3 || resulto == 0))
                            {
                                drawPos[0]--;
                            }
                            else if (direction == 1 && (i < 3 || resulto == 0))
                            {
                                drawPos[0]++;
                            }

                            if (testIfPositionEmpty(drawPos[0], drawPos[1] + 1))
                            {
                                drawPos[1] += 1;

                                fillStates[(drawPos[0], drawPos[1])] = 2;
                                drawnPos.Add((drawPos[0], drawPos[1]));
                            }
                            else
                            {
                                break;
                            }
                        }
                        endOfBranchesPos.Add(new int[2] { drawPos[0], drawPos[1] });


                        seedo = LCGint1(seedo);
                    }

                    foreach (int[] tempPos in endOfBranchesPos)
                    {
                        tempPos[0] -= 2;
                        tempPos[1] += 2;

                        for (int i = 0; i < 5; i++)
                        {
                            for (int j = 0; j < 5; j++)
                            {
                                drawPos[0] = tempPos[0] + i;
                                drawPos[1] = tempPos[1] - j;

                                if ((Abs(i - 2) != 2 || Abs(j - 2) != 2) && testIfPositionEmpty(drawPos[0], drawPos[1]))
                                {
                                    fillStates[(drawPos[0], drawPos[1])] = 1;
                                }
                            }
                        }
                    }
                }
                else if (type == 2) // kelp
                {
                    if (subType == 0)
                    {
                        maxGrowthLevel = 1 + (seed % 10);
                        growthLevel = Min(growthLevel, maxGrowthLevel);
                        int startingPoint;
                        if (seed % 2 == 0)
                        {
                            startingPoint = 0;
                        }
                        else
                        {
                            startingPoint = 1;
                        }
                        for (int i = 0; i < growthLevel; i++)
                        {
                            int fillPosX = (startingPoint + i) % 2;
                            if (testIfPositionEmpty(fillPosX, i))
                            {
                                fillStates[(fillPosX, i)] = 3;
                            }
                            else
                            {
                                growthLevel = i;
                                goto finishStructure;
                            }
                        }
                    }
                    else if (subType == 1)
                    {
                        maxGrowthLevel = 1 + (seed % 10);
                        growthLevel = Min(growthLevel, maxGrowthLevel);
                        int startingPoint;
                        if (seed % 2 == 0)
                        {
                            startingPoint = 0;
                        }
                        else
                        {
                            startingPoint = 1;
                        }
                        for (int i = 0; i < growthLevel; i++)
                        {
                            int fillPos = (startingPoint + i) % 2;
                            if (testIfPositionEmpty(fillPos, -i))
                            {
                                fillStates[(fillPos, -i)] = 3;
                            }
                            else
                            {
                                growthLevel = i;
                                goto finishStructure;
                            }
                        }
                    }
                }

                else if (type == 3) // obsidian plant
                {
                    maxGrowthLevel = 1 + seed % 3;
                    growthLevel = Min(growthLevel, maxGrowthLevel);

                    for (int i = 0; i < growthLevel; i++)
                    {
                        fillStates[(0, i)] = 1;
                    }
                }

                else if (type == 4) // mushroom
                {
                    maxGrowthLevel = 1 + seed % 7;
                    growthLevel = Min(growthLevel, maxGrowthLevel);

                    int capY = 0;

                    for (int i = 0; i < growthLevel - 1; i++)
                    {
                        capY = i;
                        if (testIfPositionEmpty(0, i))
                        {
                            fillStates[(0, i)] = 4;
                        }
                        else
                        {
                            growthLevel = i;
                            break;
                        }
                    }

                    if (growthLevel > 1)
                    {
                        fillStates[(-1, capY)] = 5;
                        fillStates[(0, capY)] = 5;
                        fillStates[(1, capY)] = 5;
                        if (growthLevel > seed % 8 + 2)
                        {
                            fillStates[(-2, capY)] = 5;
                            fillStates[(2, capY)] = 5;
                            if (growthLevel > seed % 8 + 3 + seed % 2)
                            {
                                capY++;
                                fillStates[(-1, capY)] = 5;
                                fillStates[(0, capY)] = 5;
                                fillStates[(1, capY)] = 5;
                            }
                        }
                    }
                }
                else if (type == 5) // vine
                {
                    List<(int x, int y)> drawnPos = new List<(int x, int y)>();

                    maxGrowthLevel = 4 + (seed % 50);
                    growthLevel = Min(growthLevel, maxGrowthLevel);
                    int direction;
                    if (seed % 2 == 0)
                    {
                        direction = -1;
                    }
                    else
                    {
                        direction = 1;
                    }
                    for (int i = 0; i < growthLevel; i++)
                    {
                        int fillPos = direction * ( ((int)(i*0.5f)) % 2);
                        if (testIfPositionEmpty(fillPos, -i))
                        {
                            fillStates[(fillPos, -i)] = 1;
                            drawnPos.Add((fillPos, -i));
                        }
                        else
                        {
                            growthLevel = i;
                            break;
                        }
                    }
                    seedo = seed;
                    int seeda;
                    int spacingOfFlowers = 4 + seedo % 4;
                    int[] drawPos = new int[2];

                    for (int rep = 0; true; rep += 1)
                    {
                        seeda = LCGint2(seedo);
                        if (seeda % 4 == 0)
                        {
                            seedo = LCGint1(seedo);
                            continue;
                        }
                        drawPos[1] = rep * spacingOfFlowers + seeda % Max(1, spacingOfFlowers-1);
                        if (drawPos[1] >= growthLevel)
                        {
                            break;
                        }
                        drawPos[0] = drawnPos[drawPos[1]].x;
                        drawPos[1] = drawnPos[drawPos[1]].y;

                        if (testIfPositionEmpty(drawPos[0], drawPos[1]) && testIfPositionEmpty(drawPos[0] + 1, drawPos[1]) && testIfPositionEmpty(drawPos[0] - 1, drawPos[1]) && testIfPositionEmpty(drawPos[0], drawPos[1] + 1) && testIfPositionEmpty(drawPos[0], drawPos[1] - 1))
                        {
                            fillStates[(drawPos[0], drawPos[1])] = 7;
                            fillStates[(drawPos[0]+1, drawPos[1])] = 6;
                            fillStates[(drawPos[0]-1, drawPos[1])] = 6;
                            fillStates[(drawPos[0], drawPos[1]+1)] = 6;
                            fillStates[(drawPos[0], drawPos[1]-1)] = 6;
                        }
                        else
                        {
                            break;
                        }
                        seedo = LCGint1(seedo);
                    }
                }

            finishStructure:;

                int minX = 0;
                int maxX = 0;
                int minY = 0;
                int maxY = 0;

                foreach ((int x, int y) drawPos in fillStates.Keys)
                {
                    if (drawPos.x < minX)
                    {
                        minX = drawPos.x;
                    }
                    else if (drawPos.x > maxX)
                    {
                        maxX = drawPos.x;
                    }
                    if (drawPos.y < minY)
                    {
                        minY = drawPos.y;
                    }
                    else if (drawPos.y > maxY)
                    {
                        maxY = drawPos.y;
                    }
                }

                int width = maxX-minX+1;
                int height = maxY-minY+1;
                bitmap = new Bitmap(width, height);

                foreach ((int x, int y) drawPos in fillStates.Keys)
                {
                    bitmap.SetPixel(drawPos.x-minX, drawPos.y-minY, colorDict[fillStates[drawPos]]);
                }

                posOffset[0] = minX;
                posOffset[1] = minY;

                int one = posX - posOffset[0];
                int two = posX + width - posOffset[0] - 1;
                int three = posY - posOffset[1];
                int four = posY + height - posOffset[1] - 1;

                (int, int) tupelo = screen.findChunkAbsoluteIndex(one, three);
                (int, int) tupela = screen.findChunkAbsoluteIndex(two, four);

                bounds[0] = tupelo.Item1;
                bounds[1] = tupelo.Item2;
                bounds[2] = tupela.Item1;
                bounds[3] = tupela.Item2;

                if (previousGrowthLevel == growthLevel) { isStable = true; }

                //addPlantInChunks();
            }
            public void testPlantGrowth()
            {
                if (!isStable && timeElapsed >= 0.2f + timeAtLastGrowth && growthLevel < maxGrowthLevel)
                {
                    makeBitmap(false, growthLevel+1);
                    timeAtLastGrowth = timeElapsed;
                }
            }
            public void addPlantInChunks()
            {
                (int, int) chunkPos;
                Chunk chunkToTest;

                for (int i = bounds[0]; i <= bounds[1];i++)
                {
                    for (int j = bounds[2]; j <= bounds[3]; j++)
                    {
                        if (i < screen.chunkX || i >= screen.chunkX + screen.chunkResolution || j < screen.chunkY || j >= screen.chunkY + screen.chunkResolution)
                        {
                            if (i == chunk.position.Item1 && j == chunk.position.Item2)
                            {
                                chunkPos = screen.findChunkAbsoluteIndex(i, j);
                                chunkToTest = screen.loadedChunks[chunkPos];
                                chunkToTest.plantList.Add(this);
                            }
                            else
                            {
                                chunkPos = screen.findChunkAbsoluteIndex(i, j);
                                chunkToTest = screen.loadedChunks[chunkPos];
                                chunkToTest.exteriorPlantList.Add(this);
                            }
                        }
                        else
                        {
                            if (screen.outOfBoundsPlants.ContainsKey((i, j)))
                            {
                                screen.outOfBoundsPlants[(i, j)].Add(this);
                            }
                            else
                            {
                                screen.outOfBoundsPlants.Add((i, j), new List<Plant> { this });
                            }
                        }
                    }
                }
            }
            public void placePlant()
            {
                int counto = 0;
                while (counto < 10000)
                {
                    int randX = rand.Next(16);
                    int randY = rand.Next(14)+1;
                    if (chunk.fillStates[randX, randY] <= 0)
                    {
                        if (chunk.fillStates[randX, randY - 1] > 0)
                        {
                            posX = chunk.position.Item1 * 16 + randX;
                            posY = chunk.position.Item2 * 16 + randY;
                            attachPoint = 0;
                            return;
                        }
                        else if (chunk.fillStates[randX, randY + 1] > 0)
                        {
                            posX = chunk.position.Item1 * 16 + randX;
                            posY = chunk.position.Item2 * 16 + randY;
                            attachPoint = 3;
                            return;
                        }
                    }
                    counto += 1;
                }
                isDeadAndShouldDisappear = true;
            }
            public void testPlantPosition()
            {
                int randX = (posX%16+16)%16;
                int randY = (posY%16+16)%16;
                if (chunk.fillStates[randX, randY] <= 0)
                {
                    if (randY < 15 && chunk.fillStates[randX, randY + 1] > 0)
                    {
                        return;
                    }
                    else if (randY > 0 && chunk.fillStates[randX, randY - 1] > 0)
                    {
                        return;
                    }
                }
                isDeadAndShouldDisappear = true;
            }
        }
    }
}
