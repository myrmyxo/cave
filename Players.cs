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
    public class Players
    {
        public class Player
        {
            
            public Screens.Screen screen;
            public int dimension = 0;

            public float realPosX = 0;
            public float realPosY = 0;
            public int posX = 0;
            public int posY = 0;
            public int structureX = 0;
            public int structureY = 0;
            public float speedX = 0;
            public float speedY = 0;

            public float realCamPosX = 0;
            public float realCamPosY = 0;
            public int camPosX = 0;
            public int camPosY = 0;
            public float speedCamX = 0;
            public float speedCamY = 0;

            public float timeAtLastDig = -9999;
            public float timeAtLastPlace = -9999;

            public Dictionary<(int index, int subType, int typeOfElement), int> inventoryQuantities;
            public List<(int index, int subType, int typeOfElement)> inventoryElements;
            public int inventoryCursor = 3;
            public Player(SettingsJson settingsJson)
            {
                if (settingsJson == null)
                {
                    initializeInventory();
                }
                else
                {
                    realPosX = settingsJson.player.pos.Item1;
                    posX = (int)realPosX;
                    realPosY = settingsJson.player.pos.Item2;
                    posY = (int)realPosY;
                    (Dictionary<(int index, int subType, int typeOfElement), int>, List<(int index, int subType, int typeOfElement)>) returnTuple = arrayToInventory(settingsJson.player.inv);
                    inventoryQuantities = returnTuple.Item1;
                    inventoryElements = returnTuple.Item2;
                    timeAtLastDig = settingsJson.player.lastDP.Item1;
                    timeAtLastPlace = settingsJson.player.lastDP.Item2;
                }

                camPosX = posX - ChunkLength * 24;
                realCamPosX = camPosX;
                camPosY = posY - ChunkLength * 24;
                realCamPosY = camPosY;
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
                initializeInventory();
            }
            public void initializeInventory()
            {
                inventoryQuantities = new Dictionary<(int index, int subType, int typeOfElement), int>
                {
                    {(0, 0, 1), -999 },
                    {(0, 1, 1), -999 },
                    {(0, 2, 1), -999 },
                    {(1, 0, 1), -999 },
                    {(2, 0, 1), -999 },
                    {(3, 0, 1), -999 },
                    {(3, 3, 1), -999 },
                    {(0, 0, 2), -999 },
                    {(1, 0, 2), -999 },
                    {(2, 0, 2), -999 },
                    {(2, 1, 2), -999 },
                    {(3, 0, 2), -999 },
                    {(4, 0, 2), -999 },
                    {(5, 0, 2), -999 },
                    {(5, 1, 2), -999 },
                    {(-1, 0, 0), -999 },
                    {(-4, 0, 0), -999 }
                };
                inventoryElements = new List<(int index, int subType, int typeOfElement)>
                {
                    (0, 0, 1),
                    (0, 1, 1),
                    (0, 2, 1),
                    (1, 0, 1),
                    (2, 0, 1),
                    (3, 0, 1),
                    (3, 3, 1),
                    (0, 0, 2),
                    (1, 0, 2),
                    (2, 0, 2),
                    (2, 1, 2),
                    (3, 0, 2),
                    (4, 0, 2),
                    (5, 0, 2),
                    (5, 1, 2),
                    (-1, 0, 0),
                    (-4, 0, 0)
                };
            }
            public void movePlayer()
            {
                if (digPress && timeElapsed > timeAtLastDig /*+ 0.2f*/)
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
                    if (screen.loadedChunks.ContainsKey(chunkPos))
                    {
                        if (screen.loadedChunks[chunkPos].fillStates[(posX % 32 + 32) % 32, (posY % 32 + 32) % 32] < 0)
                        {
                            speedX = speedX * 0.8f - Sign(speedX) * Sqrt(Max((int)speedX - 1, 0));
                            speedY = speedY * 0.8f - Sign(speedY) * Sqrt(Max((int)speedY - 1, 0));
                        }
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

                updateFogOfWar();
            }
            public void updateFogOfWar()
            {
                // CAREFUL, for fog of war, a false means the tile has not been visited (false = fog present), and a true means a tile has been visited (true = fog absent)

                (int x, int y) chunkPos;
                (int x, int y) posToTest;
                (int x, int y) tileIndex;
                Chunk chunkToTest;
                Dictionary<Chunk, bool> visitedChunks = new Dictionary<Chunk, bool>();
                List<(int x, int y)> modsToTest = new List<(int x, int y)>();
                modsToTest.Add((25, 25));
                modsToTest.Add((-25, 25));
                modsToTest.Add((25, -25));
                modsToTest.Add((-25, -25));
                for (int i = -24; i < 25; i++)
                {
                    modsToTest.Add((-25, i));
                    modsToTest.Add((25, i));
                    modsToTest.Add((i, 25));
                    modsToTest.Add((i, -25));
                }

                foreach ((int x, int y) mod in modsToTest)
                {
                    List<(int x, int y)> posList = rayCast((posX, posY), (posX + mod.x, posY + mod.y));
                    foreach ((int x, int y) pos in posList)
                    {
                        posToTest = pos;
                        chunkPos = screen.findChunkAbsoluteIndex(posToTest.x, posToTest.y);
                        if (screen.loadedChunks.ContainsKey(chunkPos))
                        {
                            chunkToTest = screen.loadedChunks[chunkPos];
                        }
                        else { continue; }
                        if (chunkToTest.explorationLevel == 2) { continue; }
                        visitedChunks[chunkToTest] = true;
                        if (chunkToTest.explorationLevel == 0)
                        {
                            chunkToTest.explorationLevel = 1;
                            chunkToTest.fogOfWar = new bool[32, 32];
                            chunkToTest.fogBitmap = new Bitmap(32, 32);
                            for (int ii = 0; ii < 32; ii++)
                            {
                                for (int jj = 0; jj < 32; jj++)
                                {
                                    chunkToTest.fogBitmap.SetPixel(ii, jj, Color.Black);
                                }
                            }
                        }
                        tileIndex = GetChunkIndexFromTile(posToTest.x, posToTest.y);
                        if (!chunkToTest.fogOfWar[tileIndex.x, tileIndex.y])
                        {
                            chunkToTest.fogOfWar[tileIndex.x, tileIndex.y] = true;
                            chunkToTest.fogBitmap.SetPixel(tileIndex.x, tileIndex.y, Color.Transparent);
                        }
                    }
                }
                foreach (Chunk chunko in visitedChunks.Keys)
                {
                    bool setAsVisited = true;
                    foreach (bool boolo in chunko.fogOfWar)
                    {
                        if (!boolo)
                        {
                            setAsVisited = false;
                            break;
                        }
                    }
                    if (setAsVisited)
                    {
                        chunko.explorationLevel = 2;
                        chunko.fogOfWar = null;
                        chunko.fogBitmap = null;
                    }
                }
            }
            public List<(int x, int y)> rayCast((int x, int y) startPos, (int x, int y) targetPos)
            {
                int lives = 3;

                List<(int x, int y)> posList = new List<(int x, int y)>();

                (int x, int y) diff = (targetPos.x - startPos.x, targetPos.y - startPos.y);
                bool goingUp = diff.x > 0;
                bool goingRight = diff.y > 0;
                int xMult;
                if (goingUp) { xMult = 1; }
                else { xMult = -1; }
                int yMult;
                if (goingRight) { yMult = 1; }
                else { yMult = -1; }

                float xToY = Abs((float)diff.x) / (diff.y + 0.0001f);
                float yToX = Abs((float)diff.y) / (diff.x + 0.0001f);

                (float x, float y) currentPos = (startPos.x + 0.5f, startPos.y + 0.5f);
                (int x, int y) currentPosInt;
                (int x, int y) chunkPos;
                Chunk chunkToTest;
                float valueX;
                float valueY;
                int repeatCounter = 0;
                while (repeatCounter < 100)
                {
                    valueX = PosMod(currentPos.x, 1);
                    if (goingUp) { valueX = 1 - valueX; };
                    valueY = PosMod(currentPos.y, 1);
                    if (goingRight) { valueY = 1 - valueY; }
                    if (Abs(valueX * xToY) > Abs(valueY))
                    {
                        currentPos = (currentPos.x + xMult * (valueX + 0.0001f), currentPos.y + yMult * yToX * (valueX + 0.0001f));
                        currentPosInt = ((int)currentPos.x, (int)currentPos.y);
                        chunkPos = screen.findChunkAbsoluteIndex(currentPosInt.x, currentPosInt.y);
                        chunkToTest = screen.tryToGetChunk(chunkPos);
                        posList.Add(currentPosInt);
                        if (lives < 3 || chunkToTest.fillStates[GetChunkIndexFromTile1D(currentPosInt.x), GetChunkIndexFromTile1D(currentPosInt.y)] > 0)
                        {
                            lives--;
                            if (lives <= 0) { return posList; }
                        }
                    }
                    else
                    {
                        currentPos = (currentPos.x + xMult * xToY * (valueY + 0.0001f), currentPos.y + yMult * (valueY + 0.0001f));
                        currentPosInt = ((int)currentPos.x, (int)currentPos.y);
                        chunkPos = screen.findChunkAbsoluteIndex(currentPosInt.x, currentPosInt.y);
                        chunkToTest = screen.tryToGetChunk(chunkPos);
                        posList.Add(currentPosInt);
                        if (lives < 3 || chunkToTest.fillStates[GetChunkIndexFromTile1D(currentPosInt.x), GetChunkIndexFromTile1D(currentPosInt.y)] > 0)
                        {
                            lives--;
                            if (lives <= 0) { return posList; }
                        }
                    }
                    repeatCounter++;
                }

                return posList;
            }
            public bool CheckStructurePosChange()
            {
                (int, int) oldStructurePos = (structureX, structureY);
                structureX = Floor(camPosX, 512) / 512;
                structureY = Floor(camPosY, 512) / 512;
                if (oldStructurePos == (structureX, structureY)) { return false; }
                return true;
            }
            public void Dig(int posToDigX, int posToDigY)
            {
                (int, int) chunkPos = screen.findChunkAbsoluteIndex(posToDigX, posToDigY);
                int value;
                foreach (Plant plant in screen.activePlants.Values)
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
                if (tileState == 0 || tileState < 0 && tileContent.typeOfElement > 0)
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
                        Entity newEntity = new Entity(screen, (posToDigX, posToDigY), tileContent.index, tileContent.subType);
                        screen.activeEntities[newEntity.id] = newEntity;
                        timeAtLastPlace = timeElapsed;
                    }
                    else if (tileContent.typeOfElement == 2)
                    {
                        Plant newPlant = new Plant(screen, (posToDigX, posToDigY), tileContent.index, tileContent.subType);
                        if (!newPlant.isDeadAndShouldDisappear) { screen.activePlants[newPlant.id] = newPlant; }
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
                inventoryCursor = ((inventoryCursor + value) % counto + counto) % counto;
            }
            public void drawInventory()
            {
                if (inventoryElements.Count > 0)
                {
                    (int index, int subType, int typeOfElement) element = inventoryElements[inventoryCursor];
                    if (element.typeOfElement == 0)
                    {
                        Sprites.drawSpriteOnCanvas(screen.game.overlayBitmap, compoundSprites[element.index].bitmap, (340, 64), 4, true);
                    }
                    else if (element.typeOfElement == 1)
                    {
                        Sprites.drawSpriteOnCanvas(screen.game.overlayBitmap, entitySprites[(element.index, element.subType)].bitmap, (340, 64), 4, true);
                    }
                    else if (element.typeOfElement == 2)
                    {
                        Sprites.drawSpriteOnCanvas(screen.game.overlayBitmap, plantSprites[(element.index, element.subType)].bitmap, (340, 64), 4, true);
                    }
                    else if (element.typeOfElement == 3)
                    {
                        Sprites.drawSpriteOnCanvas(screen.game.overlayBitmap, materialSprites[(element.index, element.subType)].bitmap, (340, 64), 4, true);
                    }
                    int quantity = inventoryQuantities[element];
                    if (quantity == -999)
                    {
                        Sprites.drawSpriteOnCanvas(screen.game.overlayBitmap, numberSprites[10].bitmap, (408, 64), 4, true);
                    }
                    else
                    {
                        drawNumber(screen.game.overlayBitmap, quantity, (408, 64), 4, true);
                    }
                }
            }
        }
        public static void drawNumber(Bitmap bitmap, int number, (int x, int y) pos, int scaleFactor, bool centeredDraw)
        {
            List<int> numberList = new List<int>();
            if (number == 0) { numberList.Add(0); }
            for (int i = 0; number > 0; i++)
            {
                numberList.Insert(0, number % 10);
                number = number / 10;
            }
            for (int i = 0; i < numberList.Count; i++)
            {
                Sprites.drawSpriteOnCanvas(bitmap, numberSprites[numberList[i]].bitmap, (pos.x + i * 32, pos.y), scaleFactor, centeredDraw);
            }

        }
    }
}
