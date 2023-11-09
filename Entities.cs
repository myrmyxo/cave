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
using System.Runtime.Remoting.Proxies;

namespace Cave
{
    public class Entities
    {
        public class Entity
        {
            public Form1.Screen screen;

            public int type; // 0 = fairy , 1 = frog , 2 = fish
            public int state; // 0 = idle I guess idk
            public float realPosX = 0;
            public float realPosY = 0;
            public int posX = 0;
            public int posY = 0;
            public float speedX = 0;
            public float speedY = 0;
            public Color color;

            public Dictionary<(int index, bool isEntity), int> inventoryQuantities;
            public List<(int index, bool isEntity)> inventoryElements;
            public int elementsPossessed = 0;
            public int inventoryCursor = 0;

            public float timeAtLastDig = -9999;
            public float timeAtLastPlace = -9999;

            public bool isDeadAndShouldDisappear = false;

            public void findType(Chunk chunk)
            {
                int biome = chunk.biomeIndex[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16][0].Item1;
                if (chunk.fillStates[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16] < 0)
                {
                    type = 2;
                }
                else if (biome == 5)
                {
                    type = 0;
                }
                else if (biome == 6)
                {
                    type = 0;
                }
                else if (biome == 7)
                {
                    type = 0;
                }
                else { type = 1; }
            }
            public Color findColor(Chunk chunk)
            {
                int hueVar = rand.Next(101) - 50;
                int shadeVar = rand.Next(61) - 30;
                int biome = chunk.biomeIndex[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16][0].Item1;
                if (type == 0)
                {
                    if (biome == 6)
                    {
                        return Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar);
                    }
                    if (biome == 7)
                    {
                        hueVar = Abs((int)(hueVar * 0.4f));
                        shadeVar = Abs(shadeVar);
                        return Color.FromArgb(255 - hueVar - shadeVar, 255 - hueVar - shadeVar, 255 - shadeVar);
                    }
                    /* if (biome == 5)*/
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
            public Entity(int posXt, int posYt, int typet, int rt, int gt, int bt, Form1.Screen screenToPut)
            {
                screen = screenToPut;
                realPosX = posXt;
                posX = posXt;
                realPosY = posYt;
                posY = posYt;
                type = typet;
                if (type == 3) { initializeInventory(); }
                state = 0;
                color = Color.FromArgb(rt, gt, bt);
            }
            public Entity(Chunk chunk, Form1.Screen screenToPut)
            {
                screen = screenToPut;
                placeEntity(chunk);
                findType(chunk);
                if (type == 3) { initializeInventory(); }
                color = findColor(chunk);
            }
            public Entity(Chunk chunk, (int, int) positionToPut, int typeToPut, Form1.Screen screenToPut)
            {
                screen = screenToPut;
                posX = positionToPut.Item1;
                realPosX = posX;
                posY = positionToPut.Item2;
                realPosY = posY;
                type = typeToPut;
                if (type == 3) { initializeInventory(); }
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
                    speedY += 0.5f;
                    (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posX, posY + 1); // +1 cause coordinates are inverted lol
                    Chunk chunkToTest = screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                    if (chunkToTest.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 33) % 16] > 0)
                    {
                        speedX = Sign(speedX) * (Max(0, Abs(speedX) * (0.85f) - 0.2f));
                        if (rand.NextDouble() > 0.05f)
                        {
                            speedX += (float)(rand.NextDouble()) * 5 - 2.5f;
                            speedY += (float)(rand.NextDouble()) * 5 - 2.5f;
                        }
                    }
                }
                else if (type == 2) // fish
                {
                    (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posX, posY); // +1 cause coordinates are inverted lol
                    Chunk chunkToTest = screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                    if (chunkToTest.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 32) % 16] < 0)
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
                        speedY += 0.5f;
                        chunkRelativePos = screen.findChunkScreenRelativeIndex(posX, posY + 1); // +1 cause coordinates are inverted lol
                        chunkToTest = screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                        if (chunkToTest.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 33) % 16] > 0)
                        {
                            speedX = speedX * 0.9f - Sign(speedX) * 0.12f;
                            if (rand.NextDouble() > 0.05f)
                            {
                                speedX += (float)(rand.NextDouble()) * 2 - 1;
                                speedY = -(float)(rand.NextDouble()) * 1.5f + 0.5f;
                            }
                        }
                    }
                }
                if (type != 2)
                {
                    (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posX, posY);
                    if (screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2].fillStates[(posX % 16 + 32) % 16, (posY % 16 + 32) % 16] < 0)
                    {
                        speedX = speedX * 0.85f - Sign(speedX) * 0.15f;
                        speedY = speedY * 0.85f - Sign(speedY) * 0.15f;
                    }
                }
                float toMoveX = speedX;
                float toMoveY = speedY;

                while (Abs(toMoveY) > 0)
                {
                    (int, int) chunkRelativePos = screen.findChunkScreenRelativeIndex(posX, posY + (int)Sign(toMoveY));
                    (int, int) chunkAbsolutePos = screen.findChunkAbsoluteIndex(posX, posY + (int)Sign(toMoveY));
                    Chunk chunkToTest = screen.loadedChunks[chunkRelativePos.Item1, chunkRelativePos.Item2];
                    if (chunkAbsolutePos.Item2 < screenChunkY || chunkAbsolutePos.Item2 >= screenChunkY + screen.chunkResolution)
                    {
                        posY += (int)Sign(toMoveY);
                        saveEntity();
                        screen.entitesToRemove.Add(this);
                        return;
                    }
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
                    if (chunkAbsolutePos.Item1 < screenChunkX || chunkAbsolutePos.Item1 >= screenChunkX + screen.chunkResolution)
                    {
                        posX += (int)Sign(toMoveX);
                        saveEntity();
                        screen.entitesToRemove.Add(this);
                        return;
                    }
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

                (int, int) chunkRelativePoso = screen.findChunkScreenRelativeIndex(posX + (int)Sign(toMoveX), posY);
                Chunk chunkToTesto = screen.loadedChunks[chunkRelativePoso.Item1, chunkRelativePoso.Item2];
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
                        stringo += "\n\nx\n";
                        f.Write(stringo);
                    }
                }
            }
            public void initializeInventory()
            {
                inventoryQuantities = new Dictionary<(int index, bool isEntity), int>
                {

                };
                inventoryElements = new List<(int index, bool isEntity)>
                {

                };
            }
            public void moveInventoryCursor(int value)
            {
                int counto = inventoryElements.Count;
                if (counto == 0) { inventoryCursor = 0; return; }
                inventoryCursor = ((inventoryCursor + value) % counto + counto) % counto;
            }
            public bool Dig(int posToDigX, int posToDigY)
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
                    elementsPossessed++;
                    timeAtLastDig = timeElapsed;
                    return true;
                }
                return false;
            }
            public bool Place(int posToDigX, int posToDigY)
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
                    if (tileContent.isEntity)
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
                    elementsPossessed--;
                    return true;
                }
                return false;
            }
        }
        public class Plant
        {
            public Form1.Screen screen;
            public Chunk chunk;

            public long seed;
            public int type;
            public int state;
            public int growthLevel;
            public int posX = 0;
            public int posY = 0;
            public Color color;



            public bool isDeadAndShouldDisappear = false;
            public Plant(int posXt, int posYt, int typet, long seedt, Chunk chunkToPut, Form1.Screen screenToPut)
            {
                screen = screenToPut;
                chunk = chunkToPut;
                posX = posXt;
                posY = posYt;
                type = typet;
                state = 0;
                seed = seedt;
                growthLevel = 1 + (int)(seedt % 5);
                color = findColor();
            }
            public Plant(long seedToPut, Chunk chunkToPut, Form1.Screen screenToPut)
            {
                screen = screenToPut;
                chunk = chunkToPut;
                seed = LCGyNeg(seedToPut);
                placeEntity();
                findType();
                color = findColor();
                growthLevel = 1 + (int)(seed%5);
            }
            public Plant(Chunk chunkToPut, (int, int) positionToPut, int typeToPut, Form1.Screen screenToPut)
            {
                screen = screenToPut;
                chunk = chunkToPut;
                posX = positionToPut.Item1;
                posY = positionToPut.Item2;
                type = typeToPut;
                findType();
                color = findColor();
            }
            public void findType()
            {
                int biome = chunk.biomeIndex[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16][0].Item1;
                if (chunk.fillStates[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16] < 0)
                {
                    type = 2;
                }
                else if (biome == 3)
                {
                    if (rand.Next(2) == 0)
                    {
                        type = 1;
                    }
                    else { type = 0; }
                }
                else { type = 0; }
            }
            public Color findColor()
            {
                long seedo = LCGxPos(seed);
                int hueVar = (int)(seedo%101) - 50;
                seedo = LCGxPos(seed);
                int shadeVar = (int)(seedo%61) - 30;
                int biome = chunk.biomeIndex[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16][0].Item1;
                if (type == 0)
                {
                    return Color.FromArgb(50 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar);
                }
                if (type == 1)
                {
                    return Color.FromArgb(140 + (int)(hueVar*0.3f) - shadeVar, 140 - (int)(hueVar * 0.3f) - shadeVar, 50 - shadeVar);
                }
                if (type == 2)
                {
                    return Color.FromArgb(30 - shadeVar, 90 - shadeVar + hueVar, 140 - shadeVar - hueVar);
                }
                return Color.Red;
            }
            public void placeEntity()
            {
                int counto = 0;
                while (counto < 10000)
                {
                    int randX = rand.Next(16);
                    int randY = rand.Next(15);
                    if (chunk.fillStates[randX, randY] <= 0 && chunk.fillStates[randX, randY+1] > 0)
                    {
                        posX = (int)chunk.position.Item1*16 + randX;
                        posY = (int)chunk.position.Item2*16 + randY;
                        return;
                    }
                    counto += 1;
                }
                isDeadAndShouldDisappear = true;
            }
        }
    }
}
