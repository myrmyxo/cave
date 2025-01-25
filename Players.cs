﻿using System;
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
using Newtonsoft.Json.Linq;

using static Cave.Form1;
using static Cave.Globals;
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
using static Cave.Particles;

namespace Cave
{
    public class Players
    {
        public class Player : Entity
        {
            public int dimension = 0;

            public int structureX = 0;
            public int structureY = 0;

            public (int x, int y) direction = (-1, 0);

            public (int type, int subType) currentAttack = (-1, -1);
            public int attackState = 0;
            public Dictionary<int, bool> entitiesAlreadyHitByCurrentAttack = new Dictionary<int, bool>();
            public (int x, int y) storedAttackPos;
            public bool willBeSetAsNotAttacking = false;

            public float realCamPosX = 0;
            public float realCamPosY = 0;
            public int camPosX = 0;
            public int camPosY = 0;
            public float speedCamX = 0;
            public float speedCamY = 0;

            public float timeAtLastMenuChange = -9999;

            public int inventoryCursor = 0;
            public int craftCursor = 0;
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

                color = Color.Green;
                findLightColor();
            }
            public void placePlayer()
            {
                bool setFarAway = true;
                if (setFarAway)
                {
                    posX = 189495;
                    realPosX = posX;
                    posY = 344453;
                    realPosY = posY;
                    camPosX = posX;
                    realCamPosX = camPosX;
                    camPosY = posY;
                    realCamPosY = camPosY;
                }

                if (devMode) { return; }
                int counto = 0;
                (int x, int y) chunkPos = (0, 0);
                Chunk chunk;
                while (counto < 10000)
                {
                    chunk = screen.getChunkFromChunkPos(chunkPos, false);
                    for (int j = 0; j < 32; j++)
                    {
                        for (int i = 0; i < 32; i++)
                        {
                            if (chunk.fillStates[i % 32, j % 32].type == 0)
                            {
                                posX = chunk.pos.x * 32 + i;
                                realPosX = posX;
                                posY = chunk.pos.y * 32 + j;
                                realPosY = posY;
                                goto ExitLoop;
                            }
                        }
                    }
                    chunkPos = spiralProgression(chunkPos);
                    counto++;
                }
            ExitLoop:;
                initializeInventory();
            }
            public override void initializeInventory()
            {
                bool plantsMode = true;
                if (!devMode)
                {
                    inventoryQuantities = new Dictionary<(int index, int subType, int typeOfElement), int>
                    {
                        {(0, 0, 4), -999 }, // tools
                        {(4, 0, 4), -999 },
                        {(1, 0, 4), -999 },
                        {(2, 0, 4), -999 },
                    };
                    inventoryElements = new List<(int index, int subType, int typeOfElement)>
                    {
                        (0, 0, 4), // tools
                        (4, 0, 4),
                        (1, 0, 4),
                        (2, 0, 4),
                    };
                }
                else if (!plantsMode) // True -> Important things, False -> EVERY thing
                {
                    inventoryQuantities = new Dictionary<(int index, int subType, int typeOfElement), int>
                    {
                        {(0, 0, 4), -999 }, // tools
                        {(4, 0, 4), -999 },
                        {(1, 0, 4), -999 },
                        {(2, 0, 4), -999 },
                        {(3, 0, 4), -999 },
                        {(0, 0, 1), -999 }, // entitities
                        {(1, 0, 1), -999 },
                        {(2, 0, 1), -999 },
                        {(4, 0, 1), -999 },
                        {(4, 1, 1), -999 },
                        {(5, 0, 1), -999 },
                        {(0, 0, 2), -999 }, // plants
                        {(0, 1, 2), -999 },
                        {(1, 0, 2), -999 },
                        {(1, 1, 2), -999 },
                        {(4, 1, 2), -999 },
                        {(5, 0, 2), -999 },
                        {(5, 1, 2), -999 },
                        {(-1, 0, 0), -999 }, // materials
                        {(-4, 0, 0), -999 },
                        {(4, 0, 0), -999 },
                        {(-6, 0, 0), -999 },
                    };
                    inventoryElements = new List<(int index, int subType, int typeOfElement)>
                    {
                        (0, 0, 4), // tools
                        (4, 0, 4),
                        (1, 0, 4),
                        (2, 0, 4),
                        (3, 0, 4),
                        (0, 0, 1), // entitititities
                        (1, 0, 1),
                        (2, 0, 1),
                        (4, 0, 1),
                        (4, 1, 1),
                        (5, 0, 1),
                        (0, 0, 2), // plants
                        (0, 1, 2),
                        (1, 0, 2),
                        (1, 1, 2),
                        (4, 1, 2),
                        (5, 0, 2),
                        (5, 1, 2),
                        (-1, 0, 0), // materials
                        (-4, 0, 0),
                        (4, 0, 0),
                        (-6, 0, 0),
                    };
                }
                else
                {
                    inventoryQuantities = new Dictionary<(int index, int subType, int typeOfElement), int>
                    {
                        {(0, 0, 4), -999 }, // tools
                        {(4, 0, 4), -999 },
                        {(1, 0, 4), -999 },
                        {(2, 0, 4), -999 },
                        {(3, 0, 4), -999 },
                        {(0, 0, 1), -999 }, // entitities
                        {(1, 0, 1), -999 },
                        {(2, 0, 1), -999 },
                        {(4, 0, 1), -999 },
                        {(4, 1, 1), -999 },
                        {(5, 0, 1), -999 },
                        {(0, 0, 2), -999 }, // plants
                        {(0, 1, 2), -999 },
                        {(0, 2, 2), -999 },
                        {(0, 3, 2), -999 },
                        {(1, 0, 2), -999 },
                        {(1, 1, 2), -999 },
                        {(2, 0, 2), -999 },
                        {(2, 1, 2), -999 },
                        {(3, 0, 2), -999 },
                        {(4, 0, 2), -999 },
                        {(4, 1, 2), -999 },
                        {(5, 0, 2), -999 },
                        {(5, 1, 2), -999 },
                        {(-1, 0, 0), -999 }, // materials
                        {(-4, 0, 0), -999 },
                        {(4, 0, 0), -999 },
                        {(-6, 0, 0), -999 },
                    };
                    inventoryElements = new List<(int index, int subType, int typeOfElement)>
                    {
                        (0, 0, 4), // tools
                        (4, 0, 4),
                        (1, 0, 4),
                        (2, 0, 4),
                        (3, 0, 4),
                        (0, 0, 1), // entitititities
                        (1, 0, 1),
                        (2, 0, 1),
                        (4, 0, 1),
                        (4, 1, 1),
                        (5, 0, 1),
                        (0, 0, 2), // plants
                        (0, 1, 2),
                        (0, 2, 2),
                        (0, 3, 2),
                        (1, 0, 2),
                        (1, 1, 2),
                        (2, 0, 2),
                        (2, 1, 2),
                        (3, 0, 2),
                        (4, 0, 2),
                        (4, 1, 2),
                        (5, 0, 2),
                        (5, 1, 2),
                        (-1, 0, 0), // materials
                        (-4, 0, 0),
                        (4, 0, 0),
                        (-6, 0, 0),
                    };
                }
            }
            public void ariGeoSlowDown(float ari, float geo)
            {
                speedX = Sign(speedX) * Max(0, Abs(speedX) * geo - ari);
                speedY = Sign(speedY) * Max(0, Abs(speedY) * geo - ari);
            }
            public void Jump(int direction, float jumpSpeed)
            {
                speedX += direction;
                speedY = Max(0, speedY) + jumpSpeed;
            }
            public void movePlayer()
            {
                bool inWater = false;
                if (screen.getChunkFromPixelPos((posX, posY)).fillStates[PosMod(posX), PosMod(posY)].type < 0) { inWater = true; }
                bool onGround = false;
                if (screen.getChunkFromPixelPos((posX, posY - 1)).fillStates[PosMod(posX), PosMod((posY - 1))].type > 0) { onGround = true; }

                ariGeoSlowDownX(0.8f, 0.15f);
                if (inWater) { ariGeoSlowDown(0.95f, 0.2f); }
                int directionState = 0;
                if (!dimensionSelection && !craftSelection)
                {
                    if (arrowKeysState[0]) { speedX -= 0.5f; directionState -= 1; }
                    if (arrowKeysState[1]) { speedX += 0.5f; directionState += 1; }
                    if (onGround && (directionState == 0 || (directionState == 1 && speedX < 0) || (directionState == -1 && speedX > 0))) { ariGeoSlowDownX(0.7f, 0.3f); }
                    if (arrowKeysState[2]) { speedY -= 0.5f; }
                    if (arrowKeysState[3])
                    {
                        if (onGround && !shiftPress) { Jump(directionState, 4); }
                        else if (debugMode || inWater) { speedY += 1f; }
                    }
                }
                applyGravity();
                if (shiftPress)
                {
                    speedX = Sign(speedX) * (Max(0, Abs(speedX) * (0.75f) - 0.7f));
                    speedY = Sign(speedY) * (Max(0, Abs(speedY) * (0.75f) - 0.7f));
                }

                if (digPress && timeElapsed > timeAtLastDig /*+ 0.2f*/)
                {
                    (int x, int y) digPos;
                    if (arrowKeysState[0] && !arrowKeysState[1]) { digPos = (posX - 1, posY); }
                    else if (arrowKeysState[1] && !arrowKeysState[0]) { digPos = (posX + 1, posY); }
                    else if (arrowKeysState[2] && !arrowKeysState[3]) { digPos = (posX, posY - 1); }
                    else if (arrowKeysState[3] && !arrowKeysState[2]) { digPos = (posX, posY + 1); }
                    else { digPos = (posX, posY); }

                    (int type, int subType, int typeOfElement) currentItem = inventoryElements[inventoryCursor];
                    if (currentItem == (1, 0, 4)) { TerrainDig(digPos); }
                    if (currentItem == (4, 0, 4)) { PlantDig(digPos, currentItem); }     // Don't do scythe here cause it's done with the attacks !
                }
                if ((placePress[0] || placePress[1]) && ((inventoryElements[inventoryCursor].typeOfElement == 0 && timeElapsed > timeAtLastPlace + 0.01f) || (timeElapsed > timeAtLastPlace + 0.2f)))
                {
                    (int x, int y) placePos;
                    if (arrowKeysState[0] && !arrowKeysState[1]) { placePos = (posX - 1, posY); }
                    else if (arrowKeysState[1] && !arrowKeysState[0]) { placePos = (posX + 1, posY); }
                    else if (arrowKeysState[2] && !arrowKeysState[3]) { placePos = (posX, posY - 1); }
                    else if (arrowKeysState[3] && !arrowKeysState[2]) { placePos = (posX, posY + 1); }
                    else { goto notPlace; }
                    Place(placePos);
                }
            notPlace:;

                if (currentAttack.type == (-1)) // only update direction for attack if player is not attacking lol
                {
                    updateDirectionForAttack();
                }

                // Actually move the player
                actuallyMoveTheEntity();

                // camera stuff (not really working anymore which makes it better than it was before yayyy) 

                int posDiffX = posX - (camPosX + 16 * (screen.chunkResolution - 1)); //*2 is needed cause there's only *8 and not *16 before
                int posDiffY = posY - (camPosY + 16 * (screen.chunkResolution - 1));
                speedCamX = Clamp(posDiffX/2, -15f, 15f);
                speedCamY = Clamp(posDiffY/2, -15f, 15f);
                realCamPosX += speedCamX;
                realCamPosY += speedCamY;
                camPosX = (int)(realCamPosX + 0.5f);
                camPosY = (int)(realCamPosY + 0.5f);

                updateFogOfWar();
                updateAttack();
                if (craftSelection && digPress && tryCraft()) { digPress = false; }
            }
            public override void actuallyMoveTheEntity()
            {
                (int type, int subType) material;
                int posToTest;
                float realPosToTest;
                float diff;
                float toMoveX = speedX;
                float toMoveY = speedY;

                while (toMoveY != 0)
                {
                    diff = Sign(toMoveY) * Min(1, Abs(toMoveY));
                    realPosToTest = realPosY + diff;
                    posToTest = (int)realPosToTest;
                    if (posY == posToTest) // if movement is too small to move by one whole pixel, update realPosY, and stop
                    {
                        realPosY = realPosToTest;
                        break;
                    }
                    material = screen.getTileContent((posX, posToTest));
                    if (material.type <= 0) // if is not a solid tile, update positions and continue
                    {
                        realPosY = realPosToTest;
                        posY = posToTest;
                        toMoveY -= diff;
                    }
                    else
                    {
                        speedY = 0;
                        break;
                    }
                }
                while (toMoveX != 0)
                {
                    diff = Sign(toMoveX) * Min(1, Abs(toMoveX));
                    realPosToTest = realPosX + diff;
                    posToTest = (int)realPosToTest;
                    if (posX == posToTest) // if movement is too small to move by one whole pixel, update realPosY, and stop
                    {
                        realPosX = realPosToTest;
                        break;
                    }
                    material = screen.getTileContent((posToTest, posY));
                    if (material.type <= 0) // if is not a solid tile, update positions and continue
                    {
                        realPosX = realPosToTest;
                        posX = posToTest;
                        toMoveX -= diff;
                    }
                    else
                    {
                        speedX = 0;
                        break;
                    }
                }
            }
            public override void teleport((int x, int y) newPos, int dimensionToTeleportTo)
            {
                screen.game.setPlayerDimension(this, dimensionToTeleportTo);
                posX = newPos.x;
                realPosY = newPos.y;
                posY = newPos.y;
                speedX = 0;
                speedY = 0;
                timeAtLastTeleportation = timeElapsed;
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
                        chunkPos = ChunkIdx(posToTest.x, posToTest.y);
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
                                    setPixelButFaster(chunkToTest.fogBitmap, (ii, jj), Color.Black);
                                }
                            }
                        }
                        tileIndex = PosMod(posToTest);
                        if (!chunkToTest.fogOfWar[tileIndex.x, tileIndex.y])
                        {
                            chunkToTest.fogOfWar[tileIndex.x, tileIndex.y] = true;
                            setPixelButFaster(chunkToTest.fogBitmap, (tileIndex.x, tileIndex.y), Color.Transparent);
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
                            chunko.fogBitmap.MakeTransparent(Color.White);
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
                        chunkToTest = screen.getChunkFromPixelPos(currentPosInt);
                        posList.Add(currentPosInt);
                        if (lives < 3 || chunkToTest.fillStates[PosMod(currentPosInt.x), PosMod(currentPosInt.y)].type > 0)
                        {
                            lives--;
                            if (lives <= 0) { return posList; }
                        }
                    }
                    else
                    {
                        currentPos = (currentPos.x + xMult * xToY * (valueY + 0.0001f), currentPos.y + yMult * (valueY + 0.0001f));
                        currentPosInt = ((int)currentPos.x, (int)currentPos.y);
                        chunkToTest = screen.getChunkFromPixelPos(currentPosInt);
                        posList.Add(currentPosInt);
                        if (lives < 3 || chunkToTest.fillStates[PosMod(currentPosInt.x), PosMod(currentPosInt.y)].type > 0)
                        {
                            lives--;
                            if (lives <= 0) { return posList; }
                        }
                    }
                    repeatCounter++;
                }

                return posList;
            }
            public void updateDirectionForAttack()
            {
                if (arrowKeysState[0] && !arrowKeysState[1]) { direction = (-1, direction.y); }
                else if (arrowKeysState[1] && !arrowKeysState[0]) { direction = (1, direction.y); }

                if (arrowKeysState[2] && !arrowKeysState[3]) { direction = (direction.x, -1); }
                else if (arrowKeysState[3] && !arrowKeysState[2]) { direction = (direction.x, 1); }
                else if (direction.x != 0) { direction = (direction.x, 0); }

                if (arrowKeysState[0] == arrowKeysState[1] && direction.y != 0) { direction = (0, direction.y); }
            }
            public void updateAttack()
            {
                List<((int x, int y), Color color)> posToDrawList = new List<((int x, int y), Color color)>();
                List<((int x, int y) pos, (int type, int subType) attack)> posToAttackList = new List<((int x, int y) pos, (int type, int subType) attack)>();
                (int type, int subType, int megaType) currentItem = inventoryElements[inventoryCursor];

                if (digPress && currentAttack.type == -1 && currentItem.megaType == 4 )  // start an attack if a tool that can attack is selected, X is pressed, and player is not already attacking
                {
                    if (currentItem == (0, 0, 4)) { startAttack((0, 0)); }
                    if (currentItem == (2, 0, 4)) { startAttack((2, 0)); }
                    else if (currentItem == (3, 0, 4)) { startAttack((3, 0)); }
                }

                if (currentAttack == (0, 0)) // if sword attack
                {
                    attackState++;
                    int sign = 1;
                    if (direction.x > 0) { sign = -1; }
                    (int x, int y) attackDirection = directionPositionArray[PosMod(directionPositionDictionary[direction] + (sign * (attackState - 2)), 8)];

                    // draw 2 pixels, at attack direction, attack direction*2
                    posToDrawList.Add(((posX + attackDirection.x, posY + attackDirection.y), Color.White));
                    posToDrawList.Add(((posX + 2 * attackDirection.x, posY + 2 * attackDirection.y), Color.White));

                    if (attackDirection.x != 0 && attackDirection.y != 0) // diagonal, add 4 attack pixels (sword + sides)
                    {
                        posToAttackList.Add(((posX + attackDirection.x, posY + attackDirection.y), currentAttack));
                        posToAttackList.Add(((posX + 2 * attackDirection.x, posY + attackDirection.y), currentAttack));
                        posToAttackList.Add(((posX + attackDirection.x, posY + 2 * + attackDirection.y), currentAttack));
                        posToAttackList.Add(((posX + 2 * attackDirection.x, posY + 2 * attackDirection.y), currentAttack));

                    }
                    else if (attackDirection.x != 0) // not diagonal, add attack pixels (sword + sides)
                    {
                        posToAttackList.Add(((posX + attackDirection.x, posY - 1), currentAttack));
                        posToAttackList.Add(((posX + attackDirection.x, posY), currentAttack));
                        posToAttackList.Add(((posX + attackDirection.x, posY + 1), currentAttack));
                        posToAttackList.Add(((posX + 2 * attackDirection.x, posY - 1), currentAttack));
                        posToAttackList.Add(((posX + 2 * attackDirection.x, posY), currentAttack));
                        posToAttackList.Add(((posX + 2 * attackDirection.x, posY + 1), currentAttack));
                    }
                    else
                    {
                        posToAttackList.Add(((posX - 1, posY + attackDirection.y), currentAttack));
                        posToAttackList.Add(((posX, posY + attackDirection.y), currentAttack));
                        posToAttackList.Add(((posX + 1, posY + attackDirection.y), currentAttack));
                        posToAttackList.Add(((posX - 1, posY + 2 * attackDirection.y), currentAttack));
                        posToAttackList.Add(((posX, posY + 2 * attackDirection.y), currentAttack));
                        posToAttackList.Add(((posX + 1, posY + 2 * attackDirection.y), currentAttack));
                    }

                    if (attackState >= 4) { willBeSetAsNotAttacking = true; }
                }
                else if (currentAttack == (2, 0)) // if scythe attack
                {
                    attackState++;
                    (int x, int y) attackPos = (0, 0);
                    int sign = 1;
                    if (direction.x > 0) { sign = -1; }
                    
                    if (attackState == 0) { attackPos = (posX + sign, posY + 1); }
                    else if (attackState == 1) { attackPos = (posX - sign, posY + 1); }
                    else if (attackState == 2) { attackPos = (posX - 2 * sign, posY); }
                    else if (attackState == 3) { attackPos = (posX - sign, posY - 1); }
                    else if (attackState == 4) { attackPos = (posX, posY - 1); }

                    posToDrawList.Add(((attackPos.x, attackPos.y), Color.White));
                    posToDrawList.Add(((attackPos.x - sign, attackPos.y), Color.White));
                    for (int j = -1; j <= 1; j+= 1)
                    {
                        posToAttackList.Add(((attackPos.x, attackPos.y + j), currentAttack));
                        posToAttackList.Add(((attackPos.x - sign, attackPos.y + j), currentAttack));
                    }

                    if (attackState >= 4) { willBeSetAsNotAttacking = true; }
                }
                else if (currentAttack == (3, 0))   // If magic wand attack
                {
                    attackState++;
                    int sign = -1;
                    if (direction.x > 0) { sign = 1; }

                    // draw the wooden staff, 1 pixel, starts on top of player, then diag, then in front. If in front of player, add the "magic pixel" (lmao) that's purple
                    (int x, int y) mod;
                    if (attackState < 1) { mod = (0, 1); }
                    else if (attackState == 1) { mod = (1, 1); }
                    else
                    {
                        if (attackState == 2) { storedAttackPos = (posX, posY); }
                        mod = (1, 0);
                        int posoX = storedAttackPos.x + attackState * sign;
                        posToDrawList.Add(((posoX, storedAttackPos.y), Color.BlueViolet));
                        posToAttackList.Add(((posoX, storedAttackPos.y), currentAttack));
                    }
                    posToDrawList.Add(((posX + mod.x * sign, posY + mod.y), Color.FromArgb(140, 140, 50)));


                    if (attackState >= 10) { willBeSetAsNotAttacking = true; }
                }
                else { willBeSetAsNotAttacking = true; }

                foreach (((int x, int y) pos, (int type, int subType) attack) attack in posToAttackList)
                {
                    screen.attacksToDo.Add(attack);
                }
                foreach (((int x, int y), Color) pos in posToDrawList)
                {
                    screen.attacksToDraw.Add(pos);
                }
                // send to list of attacks to draw
            }
            public void startAttack((int type, int subType) attackToStart)
            {
                attackState = -1;
                currentAttack = attackToStart;
                willBeSetAsNotAttacking = false;
            }
            public void setAsNotAttacking()
            {
                attackState = 0;
                currentAttack = (-1, -1);
                entitiesAlreadyHitByCurrentAttack = new Dictionary<int, bool>();
            }
            public void sendAttack(((int x, int y) pos, (int type, int subType) attack) attack)
            {
                if (attack.attack == (3, 0))
                {
                    if (screen.type.type != 2) testForBloodAltar(screen, attack.pos);
                    return;
                }

                Chunk chunkToTest = screen.getChunkFromPixelPos(attack.pos);
                if (attack.attack == (2, 0))
                {
                    PlantDig(attack.pos, (attack.attack.type, attack.attack.subType, 4), chunkToTest);
                }

                float damage = 0;
                if (attack.attack == (0, 0)) { damage = 1; }
                if (attack.attack == (2, 0)) { damage = 0.5f; }
                foreach (Entity entity in chunkToTest.entityList)
                {
                    if ((entity.posX, entity.posY) == attack.pos && !entitiesAlreadyHitByCurrentAttack.ContainsKey(entity.id))
                    {
                        entity.hp -= damage;
                        entitiesAlreadyHitByCurrentAttack[entity.id] = true;
                        entity.timeAtLastGottenHit = timeElapsed;
                        if (entity.hp <= 0)
                        {
                            entity.dieAndDrop(this);
                        }
                    }
                }
            }
            public bool Place((int x, int y) posToPlace)
            {
                Chunk chunkToTest = screen.getChunkFromPixelPos(posToPlace);
                (int type, int subType, int typeOfElement) currentItem = inventoryElements[inventoryCursor];
                (int type, int subType) tileState = chunkToTest.fillStates[PosMod(posToPlace.x), PosMod(posToPlace.y)];
                if (tileState.type == 0 || tileState.type < 0 && currentItem.typeOfElement > 0)
                {
                    if (currentItem.typeOfElement == 0)
                    {
                        chunkToTest.screen.setTileContent(posToPlace, (currentItem.type, currentItem.subType));
                        timeAtLastPlace = timeElapsed;
                    }
                    else if (currentItem.typeOfElement == 1)
                    {
                        Entity newEntity = new Entity(screen, posToPlace, (currentItem.type, currentItem.subType));
                        screen.activeEntities[newEntity.id] = newEntity;
                        timeAtLastPlace = timeElapsed;
                    }
                    else if (currentItem.typeOfElement == 2)
                    {
                        Plant newPlant = new Plant(screen, posToPlace, (currentItem.type, currentItem.subType));
                        if (!newPlant.isDeadAndShouldDisappear) { screen.activePlants[newPlant.id] = newPlant; }
                        timeAtLastPlace = timeElapsed;
                    }
                    else { return false; }
                    if (inventoryQuantities[currentItem] != -999) { removeElementFromInventory(currentItem); }
                    return true;
                }
                return false;
            }
            public override void removeElementFromInventory((int index, int subType, int typeOfElement) elementToRemove, int quantityToRemove = 1)
            {
                if (!inventoryQuantities.ContainsKey(elementToRemove)) { return; }
                if (inventoryQuantities[elementToRemove] != -999)
                {
                    inventoryQuantities[elementToRemove] -= quantityToRemove;
                    if (inventoryQuantities[elementToRemove] <= 0)
                    {
                        inventoryQuantities.Remove(elementToRemove);
                        inventoryElements.Remove(elementToRemove);
                        moveInventoryCursor(0);
                    }
                }
            }
            public void moveInventoryCursor(int value)
            {
                int counto = inventoryElements.Count;
                if (counto == 0) { inventoryCursor = 0; }
                inventoryCursor = ((inventoryCursor + value) % counto + counto) % counto;
            }
            public void moveCraftCursor(int value)
            {
                int counto = craftRecipes.Count;
                if (counto == 0) { craftCursor = 0; }
                craftCursor = ((craftCursor + value) % counto + counto) % counto;
            }
            public bool tryCraft()
            {
                if (craftRecipes.Count <= 0 || craftCursor >= craftRecipes.Count || craftCursor < 0) { return false; }
                ((int type, int subType, int megaType) material, int count)[] currentCraftRecipe = craftRecipes[craftCursor];

                foreach (((int type, int subType, int megaType) material, int count) tupel in currentCraftRecipe) // test if player has all ingredients to craft, and in sufficient amounts
                {
                    if (tupel.count < 0 && (!inventoryQuantities.ContainsKey(tupel.material) || (inventoryQuantities[tupel.material] != -999 && inventoryQuantities[tupel.material] < -tupel.count))) // if it got pollen, try to make honey
                    { return false; }
                }

                foreach (((int type, int subType, int megaType) material, int count) tupel in currentCraftRecipe) // craft each shit
                {
                    if (tupel.count < 0) { removeElementFromInventory(tupel.material, -tupel.count); } // since quantityToRemove is NEGATIVE it needs to be put positive else it will add lol
                    else if (tupel.count > 0) { addElementToInventory(tupel.material, tupel.count); }
                }

                return true;
            }
        }
    }
}
