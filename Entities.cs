using System;
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

using static Cave.Form1;
using static Cave.Form1.Globals;
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
    public class Entities
    {
        public class Entity
        {
            public Screens.Screen screen;

            public int seed;
            public int id;
            public int type; // 0 = fairy , 1 = frog , 2 = fish, 3 = hornet, 4 = worm, 5 = waterSkipper
            public int subType; // (0 : normal, obsidian, frost, skeleton). (1 : frog, carnal, skeletal). (2 : fish, skeleton). (3 : egg, larva, cocoon, adult). (4 : worm, nematode). ( 5 : waterSkipper) 
            public int state; // 0 = idle I guess idk
            public float realPosX = 0;
            public float realPosY = 0;
            public int posX = 0;
            public int posY = 0;
            public float speedX = 0;
            public float speedY = 0;
            public Color color;
            public Color lightColor;

            public List<(int x, int y)> pastPositions = new List<(int x, int y)>();
            public int length;

            public Entity targetEntity = null;
            public (int x, int y) targetPos = (0, 0);
            public List<(int x, int y)> pathToTarget = new List<(int x, int y)>();
            public List<(int x, int y)> simplifiedPathToTarget = new List<(int x, int y)>();

            public Dictionary<(int index, int subType, int typeOfElement), int> inventoryQuantities;
            public List<(int index, int subType, int typeOfElement)> inventoryElements;
            public int elementsPossessed = 0;

            public int hp = 1;
            public int food = 0;

            public float timeAtBirth = 0;
            public float timeAtLastStateChange = 0;
            public float timeAtLastDig = -9999;
            public float timeAtLastPlace = -9999;
            public float timeAtLastGottenHit = -9999;
            public float timeAtLastTeleportation = -9999;

            public bool isDeadAndShouldDisappear = false;

            public Nest nest = null;
            public int nestId = -1;
            public Entity(Screens.Screen screenToPut, EntityJson entityJson)
            {
                screen = screenToPut;
                realPosX = entityJson.pos.Item1;
                posX = (int)realPosX;
                realPosY = entityJson.pos.Item2;
                posY = (int)realPosY;
                targetPos = entityJson.tPos;
                seed = entityJson.seed;;
                id = entityJson.id;
                if (entityJson.nstId >= 0)
                {
                    // weird shit thingy...
                    if (screen.activeNests.ContainsKey(entityJson.nstId))
                    {
                        nest = screen.activeNests[entityJson.nstId];
                    }
                    else
                    {
                        screen.orphanEntities[id] = true;
                    }
                }
                (Dictionary<(int index, int subType, int typeOfElement), int>, List<(int index, int subType, int typeOfElement)>) returnTuple = arrayToInventory(entityJson.inv);
                inventoryQuantities = returnTuple.Item1;
                inventoryElements = returnTuple.Item2;
                hp = entityJson.hp;
                timeAtLastDig = entityJson.lastDP.Item1;
                timeAtLastPlace = entityJson.lastDP.Item2;
                timeAtBirth = entityJson.brth;
                timeAtLastStateChange = entityJson.sttCh;
                timeAtLastTeleportation = entityJson.tp;
                state = entityJson.state;
                transformEntity(entityJson.type, false); // false to not reinitialize hp
            }
            public Entity(Chunk chunk)
            {
                screen = chunk.screen;
                seed = LCGint1(Abs((int)chunk.chunkSeed));
                id = currentEntityId;
                placeEntity(chunk);
                findType(chunk); // contains transformEntity so contains Color and LightColor finding
                initializeInventory();
                timeAtBirth = timeElapsed;

                currentEntityId++;
            }
            public Entity(Screens.Screen screenToPut, (int, int) positionToPut, (int type, int subType) typeToPut)
            {
                screen = screenToPut;
                posX = positionToPut.Item1;
                realPosX = posX;
                posY = positionToPut.Item2;
                realPosY = posY;
                targetPos = positionToPut;
                seed = rand.Next(1000000000); //                              TO CHANGE FALSE RANDOM NOT SEEDED ARGHHEHEEEE
                id = currentEntityId;
                transformEntity(typeToPut, true);
                initializeInventory();
                timeAtBirth = timeElapsed;

                currentEntityId++;
            }
            public void findType(Chunk chunk)
            {
                (int x, int y) tileIndex = PosMod((posX, posY));
                (int biome, int subBiome) biome = chunk.biomeIndex[tileIndex.x, tileIndex.y][0].Item1;
                (int type, int subType) material = chunk.fillStates[tileIndex.x, tileIndex.y];
                if (screen.type.Item1 == 2)
                {
                    if (material.type != 0) {  transformEntity(4, 1, true); }
                    else if (biome == (10, 0) || biome == (10, 1)) { transformEntity(1, 1 + rand.Next(2), true); }
                    else if (biome == (10, 2)) { transformEntity(1, 2, true); }
                    else { transformEntity(4, 1, true); }
                }
                else
                {
                    if (material.type < 0)
                    {
                        if (rand.Next(2) == 0) { transformEntity(2, 0, true); }
                        else { transformEntity(5, 0, true); }
                    }
                    else if (material.type > 0)
                    {
                        if (rand.Next(10) > 0) { isDeadAndShouldDisappear = true; }
                        else { transformEntity(4, 0, true); }
                    }
                    else if (biome == (5, 0)) { transformEntity(0, 0, true); }
                    else if (biome == (2, 2)) { transformEntity(0, 1, true); }
                    else if (biome == (0, 1) || biome == (9, 0)) { transformEntity(0, 2, true); }
                    else { transformEntity(1, 0, true); }
                }
            }
            public Color findColor()
            {
                int hueVar = seed % 101 - 50;
                int shadeVar = seed % 61 - 30;
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
                    if (subType == 3)
                    {
                        hueVar = Abs((int)(hueVar * 0.6f));
                        shadeVar = Abs(shadeVar);
                        return Color.FromArgb(230 - hueVar - shadeVar, 230 - hueVar - shadeVar, 230 - shadeVar);
                    }
                    return Color.FromArgb(130 + hueVar + shadeVar, 130 - hueVar + shadeVar, 210 + shadeVar);
                }
                if (type == 1)
                {
                    if (subType == 1)
                    {
                        hueVar = Abs((int)(hueVar * 0.6f));
                        shadeVar = -Abs(shadeVar);
                        return Color.FromArgb(135 - shadeVar, 55 - hueVar - shadeVar, 55 - hueVar - shadeVar);
                    }
                    if (subType == 2)
                    {
                        hueVar = Abs((int)(hueVar * 0.6f));
                        shadeVar = Abs(shadeVar);
                        return Color.FromArgb(230 - hueVar - shadeVar, 230 - hueVar - shadeVar, 230 - shadeVar);
                    }
                    return Color.FromArgb(90 + hueVar + shadeVar, 210 + shadeVar, 110 - hueVar + shadeVar);
                }
                if (type == 2)
                {
                    if (subType == 1)
                    {
                        shadeVar = (int)(0.34f * shadeVar);
                        return Color.FromArgb(245 + shadeVar, 245 + shadeVar, 245 + shadeVar);
                    }
                    return Color.FromArgb(190 + shadeVar, 80 - hueVar + shadeVar, 80 + hueVar + shadeVar);
                }
                if (type == 3)
                {
                    if (subType == 0)
                    {
                        hueVar = Abs((int)(hueVar * 0.4f));
                        shadeVar = Abs(shadeVar);
                        return Color.FromArgb(255 - hueVar - shadeVar, 255 - hueVar - shadeVar, 255 - shadeVar);
                    }
                    else if (subType == 1)
                    {
                        hueVar = Abs((int)(hueVar * 0.4f));
                        shadeVar = Abs(shadeVar);
                        return Color.FromArgb(230 - hueVar - shadeVar, 230 - hueVar - shadeVar, 190 - shadeVar);
                    }
                    else if (subType == 2)
                    {
                        return Color.FromArgb(120 + (int)(hueVar * 0.2f) + shadeVar, 120 - (int)(hueVar * 0.2f) + shadeVar, Max(0, 10 + shadeVar));
                    }
                    else if (subType == 3)
                    {
                        return Color.FromArgb(190 + (int)(hueVar * 0.2f) + shadeVar, 190 - (int)(hueVar * 0.2f) + shadeVar, 80 + shadeVar);
                    }
                }
                if (type == 4)
                {
                    shadeVar = -Abs(shadeVar);
                    hueVar = Abs((int)(hueVar * 0.4f));
                    if (subType == 1)
                    {
                        return Color.FromArgb(220 - hueVar + shadeVar, 220 + hueVar + shadeVar, 220 + shadeVar);
                    }
                    return Color.FromArgb(210 + shadeVar, 140 + hueVar + shadeVar, 140 + hueVar + shadeVar);
                }
                if (type == 5)
                {
                    hueVar = Abs((int)(hueVar * 0.4f));
                    return Color.FromArgb(110 + shadeVar, 110 + shadeVar, 140 + hueVar + shadeVar);
                }
                return Color.Red;
            }
            public void findLightColor()
            {
                lightColor = Color.FromArgb(255, (color.R + 255) / 2, (color.G + 255) / 2, (color.B + 255) / 2);
            }
            public (int, int) findIntPos(float positionX, float positionY)
            {
                return ((int)Floor(positionX, 1), (int)Floor(positionY, 1));
            }
            public void placeEntity(Chunk chunk)
            {
                int randX = rand.Next(32);
                int randY = rand.Next(32);
                int counto = 0;
                while (counto < 5) // try 5 times to spawn entity NOT in terrain, to make more chance for frog fish fairies and shit instead of worms.
                {
                    if (chunk.fillStates[randX, randY].type <= 0)
                    {
                        break;
                    }
                    randX = rand.Next(32);
                    randY = rand.Next(32);
                    counto += 1;
                }
                posX = chunk.position.Item1 * 32 + randX;
                realPosX = posX;
                posY = chunk.position.Item2 * 32 + randY;
                realPosY = posY;
                targetPos = (posX, posY);
                return;
            }
            public void findRandomDestination(int distance)
            {
                targetPos = (posX + rand.Next(distance*2+1) - distance, posY + rand.Next(distance*2+1) - distance);
            }
            public bool findPointOfInterestInPlants((int type, int subType) elementOfInterest)
            {
                int plantCount = screen.activePlants.Count;
                if (plantCount == 0) { return false; }
                Plant plantToTest = screen.activePlants.Values.ToArray()[rand.Next(plantCount)];
                {
                    (bool found, int x, int y) returnTuple = plantToTest.findPointOfInterestInPlant(elementOfInterest);
                    if (returnTuple.found)
                    {
                        targetPos = (returnTuple.x, returnTuple.y);
                        state = 2;
                        return true;
                    }
                }
                return false;
            }
            public void addToQueue(List<((int x, int y) position, float cost)> queue, ((int x, int y) position, float cost) valueToAdd)
            {
                int idx = 0;
                while (idx < queue.Count)
                {
                    if (valueToAdd.cost < queue[idx].cost)
                    {
                        queue.Insert(idx, valueToAdd);
                        return;
                    }
                    idx++;
                }
                queue.Add(valueToAdd);
            }
            public void addRemoveToQueue(List<((int x, int y) position, float cost)> queue, ((int x, int y) position, float cost) valueToAdd)
            {
                int idx = 0;
                while (idx < queue.Count)
                {
                    if (valueToAdd.cost < queue[idx].cost)
                    {
                        queue.Insert(idx, valueToAdd);
                        idx++;
                        goto AfterLoop;
                    }
                    idx++;
                }
                queue.Add(valueToAdd);
                return;
            AfterLoop:;
                while (idx < queue.Count)
                {
                    if (valueToAdd.position == queue[idx].position)
                    {
                        queue.RemoveAt(idx);
                        return;
                    }
                    idx++;
                }
            }
            public int heuristic((int x, int y) currentLocation, (int x, int y) targetLocation)
            {
                int diffX = Abs(targetLocation.x - currentLocation.x);
                int diffY = Abs(targetLocation.y - currentLocation.y);
                int diagNumber = Min(diffX, diffY);
                return (int)(diffX + diffY - 2*diagNumber + 1.41421356237f*diagNumber);
            }
            public int evaluateTile((int x, int y) location, (int x, int y) targetLocation, Dictionary<(int x, int y), int> costOfTiles)
            {
                if (location == targetLocation)
                {
                    costOfTiles[location] = 0;
                    return 0;
                }
                (int x, int y) chunkPos = ChunkIdx(location.x, location.y);
                if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                {
                    (int x, int y) tileIndex = PosMod(location);
                    int tileContent = chunkToTest.fillStates[tileIndex.x, tileIndex.y].type;
                    if (tileContent <= 0)
                    {
                        costOfTiles[location] = costDict[tileContent];
                        return costOfTiles[location];
                    }
                }
                costOfTiles[location] = 1000000;
                return 1000000;
            }
            public bool testQueueAdd((int x, int y) tileToTest, (int x, int y) targetLocation, Dictionary<(int x, int y), int> costOfTiles)
            {
                int tileCost;
                if (costOfTiles.TryGetValue(tileToTest, out int returnedResult))
                {
                    tileCost = returnedResult;
                }
                else
                {
                    tileCost = evaluateTile(tileToTest, targetLocation, costOfTiles);
                }

                if (tileCost < 999999)
                {
                    return true;
                }
                return false;
            }
            public (bool topLeft, bool bottomLeft, bool bottomRight, bool topRight) updateDiagsToAdd((bool topLeft, bool bottomLeft, bool bottomRight, bool topRight) diagsToAdd, (int x, int y) neighbour)
            {
                if (neighbour.x == -1)
                {
                    diagsToAdd.topLeft = true;
                    diagsToAdd.bottomLeft = true;
                }
                else if (neighbour.x == 1)
                {
                    diagsToAdd.topRight = true;
                    diagsToAdd.bottomRight = true;
                }
                else if (neighbour.y == -1)
                {
                    diagsToAdd.bottomLeft = true;
                    diagsToAdd.bottomRight = true;
                }
                else if (neighbour.y == 1)
                {
                    diagsToAdd.topLeft = true;
                    diagsToAdd.topRight = true;
                }
                return diagsToAdd;
            }
            public bool pathfindToLocation((int x, int y) targetLocation)
            {
                //TEST IF THE TILE IS EXPOSED TO AIR/LIQUID AT LEAST NOT TO PATHFIND FOR NUTHIN LOL
                if (
                    screen.getTileContent((targetLocation.x+1, targetLocation.y)).type > 0
                    &&
                    screen.getTileContent((targetLocation.x-1, targetLocation.y)).type > 0
                    &&
                    screen.getTileContent((targetLocation.x, targetLocation.y+1)).type > 0
                    &&
                    screen.getTileContent((targetLocation.x, targetLocation.y-1)).type > 0
                    )
                {
                    return false;
                }


                ((int x, int y) pos, float cost)[] neighbourDict = new ((int x, int y) pos, float cost)[]
                {
                    ((1, 0), 1), ((-1, 0), 1), ((0, -1), 1), ((0, 1), 1),
                };
                ((int x, int y) pos, float cost)[] diagNeighbourDicto = new ((int x, int y) pos, float cost)[]
                {
                    ((1, 1), 1.41421356237f), ((-1, 1), 1.41421356237f), ((-1, -1), 1.41421356237f), ((1, -1), 1.41421356237f)
                };

                Dictionary<(int x, int y), int> costOfTiles = new Dictionary<(int x, int y), int> { { (posX, posY), 1000000 } };
                Dictionary<(int x, int y), float> cumulativeCostOfTiles = new Dictionary<(int x, int y), float> { { (posX, posY), 0 } };
                Dictionary<(int x, int y), (int x, int y)> originOfTiles = new Dictionary<(int x, int y), (int x, int y)>();
                List<((int x, int y) position, float cost)> tilesToVisit = new List<((int x, int y) position, float cost)> { ((posX, posY), heuristic((posX, posY), targetLocation)) };
                int repeatCounter = 0;

                (int x, int y) tileToTest;
                while (repeatCounter < 5000 && tilesToVisit.Count > 0)
                {
                    tileToTest = tilesToVisit[0].position;
                    tilesToVisit.RemoveAt(0);
                    if ((tileToTest.x, tileToTest.y) == targetLocation)
                    {
                        goto pathFound;
                    }
                    // add neighbours
                    (bool topLeft, bool bottomLeft, bool bottomRight, bool topRight) diagsToAdd = (false, false, false, false);
                    foreach (((int x, int y) pos, float cost) neighbour in neighbourDict)
                    {
                        (int x, int y) posToAddMaybe = (tileToTest.x + neighbour.pos.x, tileToTest.y + neighbour.pos.y);
                        if (testQueueAdd(posToAddMaybe, targetLocation, costOfTiles))
                        {
                            float costo = cumulativeCostOfTiles[tileToTest] + neighbour.cost*costOfTiles[posToAddMaybe];
                            if (!cumulativeCostOfTiles.ContainsKey(posToAddMaybe)) // if never visited
                            {
                                cumulativeCostOfTiles[posToAddMaybe] = costo;
                                originOfTiles[posToAddMaybe] = tileToTest;
                                addToQueue(tilesToVisit, (posToAddMaybe, costo + heuristic(posToAddMaybe, targetLocation)));
                                diagsToAdd = updateDiagsToAdd(diagsToAdd, neighbour.pos);
                            }
                            else if (costo < cumulativeCostOfTiles[posToAddMaybe]) // else if the new path is less expensive
                            {
                                cumulativeCostOfTiles[posToAddMaybe] = costo;
                                originOfTiles[posToAddMaybe] = tileToTest;
                                addRemoveToQueue(tilesToVisit, (posToAddMaybe, costo + heuristic(posToAddMaybe, targetLocation)));
                                diagsToAdd = updateDiagsToAdd(diagsToAdd, neighbour.pos);
                            }
                        }
                    }
                    if (diagsToAdd.topLeft)
                    {
                        (int x, int y) posToAddMaybe = (tileToTest.x - 1, tileToTest.y + 1);
                        if (testQueueAdd(posToAddMaybe, targetLocation, costOfTiles))
                        {
                            float costo = cumulativeCostOfTiles[tileToTest] + 1.41421356237f * costOfTiles[posToAddMaybe];
                            if (!cumulativeCostOfTiles.ContainsKey(posToAddMaybe)) // if never visited
                            {
                                cumulativeCostOfTiles[posToAddMaybe] = costo;
                                originOfTiles[posToAddMaybe] = tileToTest;
                                addToQueue(tilesToVisit, (posToAddMaybe, costo + heuristic(posToAddMaybe, targetLocation)));
                            }
                            else if (costo < cumulativeCostOfTiles[posToAddMaybe]) // else if the new path is less expensive
                            {
                                cumulativeCostOfTiles[posToAddMaybe] = costo;
                                originOfTiles[posToAddMaybe] = tileToTest;
                                addRemoveToQueue(tilesToVisit, (posToAddMaybe, costo + heuristic(posToAddMaybe, targetLocation)));
                            }
                        }
                    }
                    if (diagsToAdd.topRight)
                    {
                        (int x, int y) posToAddMaybe = (tileToTest.x + 1, tileToTest.y + 1);
                        if (testQueueAdd(posToAddMaybe, targetLocation, costOfTiles))
                        {
                            float costo = cumulativeCostOfTiles[tileToTest] + 1.41421356237f * costOfTiles[posToAddMaybe];
                            if (!cumulativeCostOfTiles.ContainsKey(posToAddMaybe)) // if never visited
                            {
                                cumulativeCostOfTiles[posToAddMaybe] = costo;
                                originOfTiles[posToAddMaybe] = tileToTest;
                                addToQueue(tilesToVisit, (posToAddMaybe, costo + heuristic(posToAddMaybe, targetLocation)));
                            }
                            else if (costo < cumulativeCostOfTiles[posToAddMaybe]) // else if the new path is less expensive
                            {
                                cumulativeCostOfTiles[posToAddMaybe] = costo;
                                originOfTiles[posToAddMaybe] = tileToTest;
                                addRemoveToQueue(tilesToVisit, (posToAddMaybe, costo + heuristic(posToAddMaybe, targetLocation)));
                            }
                        }
                    }
                    if (diagsToAdd.bottomLeft)
                    {
                        (int x, int y) posToAddMaybe = (tileToTest.x - 1, tileToTest.y - 1);
                        if (testQueueAdd(posToAddMaybe, targetLocation, costOfTiles))
                        {
                            float costo = cumulativeCostOfTiles[tileToTest] + 1.41421356237f * costOfTiles[posToAddMaybe];
                            if (!cumulativeCostOfTiles.ContainsKey(posToAddMaybe)) // if never visited
                            {
                                cumulativeCostOfTiles[posToAddMaybe] = costo;
                                originOfTiles[posToAddMaybe] = tileToTest;
                                addToQueue(tilesToVisit, (posToAddMaybe, costo + heuristic(posToAddMaybe, targetLocation)));
                            }
                            else if (costo < cumulativeCostOfTiles[posToAddMaybe]) // else if the new path is less expensive
                            {
                                cumulativeCostOfTiles[posToAddMaybe] = costo;
                                originOfTiles[posToAddMaybe] = tileToTest;
                                addRemoveToQueue(tilesToVisit, (posToAddMaybe, costo + heuristic(posToAddMaybe, targetLocation)));
                            }
                        }
                    }
                    if (diagsToAdd.bottomRight)
                    {
                        (int x, int y) posToAddMaybe = (tileToTest.x + 1, tileToTest.y - 1);
                        if (testQueueAdd(posToAddMaybe, targetLocation, costOfTiles))
                        {
                            float costo = cumulativeCostOfTiles[tileToTest] + 1.41421356237f * costOfTiles[posToAddMaybe];
                            if (!cumulativeCostOfTiles.ContainsKey(posToAddMaybe)) // if never visited
                            {
                                cumulativeCostOfTiles[posToAddMaybe] = costo;
                                originOfTiles[posToAddMaybe] = tileToTest;
                                addToQueue(tilesToVisit, (posToAddMaybe, costo + heuristic(posToAddMaybe, targetLocation)));
                            }
                            else if (costo < cumulativeCostOfTiles[posToAddMaybe]) // else if the new path is less expensive
                            {
                                cumulativeCostOfTiles[posToAddMaybe] = costo;
                                originOfTiles[posToAddMaybe] = tileToTest;
                                addRemoveToQueue(tilesToVisit, (posToAddMaybe, costo + heuristic(posToAddMaybe, targetLocation)));
                            }
                        }
                    }
                    repeatCounter++;
                }
                return false; // exit early (if impossible to find a path)
            pathFound:;
                //backtrack function
                List<(int x, int y)> path = new List<(int x, int y)> { targetLocation };
                (int x, int y) currentPos = targetLocation;
                while (originOfTiles.TryGetValue(currentPos, out (int x, int y) gottenValue))
                {
                    path.Insert(0, gottenValue);
                    currentPos = gottenValue;
                }
                pathToTarget = path;
                simplifiedPathToTarget = new List<(int x, int y)>(path);
                simplifyPath(simplifiedPathToTarget);
                return true;
            }
            public void simplifyPath(List<(int x, int y)> path)
            {
                int idx = path.Count - 1;
                (int x, int y) direction;
                (int x, int y) lastKeptPointPos;
                (int x, int y) posToTest;
                int multCounter;

                while (idx > 0)
                {
                    lastKeptPointPos = path[idx];
                    idx--;
                    posToTest = path[idx];
                    direction = (posToTest.x - lastKeptPointPos.x, posToTest.y - lastKeptPointPos.y);
                    idx--;

                    multCounter = 2;
                    while (idx >= 0)
                    {
                        posToTest = path[idx];
                        if (posToTest.x - lastKeptPointPos.x == multCounter*direction.x && posToTest.y - lastKeptPointPos.y == multCounter*direction.y)
                        {
                            path.RemoveAt(idx+1);
                            idx--;
                            multCounter++;
                        }
                        else
                        {
                            idx++;
                            break;
                        }
                    }
                }
                if (path.Count > 0) { path.RemoveAt(0); }
                if (path.Count > 0) { path.RemoveAt(path.Count - 1); }
            }
            public void applyGravity()
            {
                speedY -= 0.5f;
            }
            public void changeSpeedRandom(float range)
            {
                speedX += range*(float)(rand.NextDouble() - 0.5);
                speedY += range*(float)(rand.NextDouble() - 0.5);
            }
            public void jumpRandom(float rangeX, float maxY)
            {
                speedX += rangeX*(float)(rand.NextDouble() - 0.5);
                speedY += maxY*(float)(rand.NextDouble());
            }
            public void slowDown(float slowdownSpeed)
            {
                speedX = Sign(speedX) * Max(Abs(speedX) - slowdownSpeed, 0);
                speedY = Sign(speedY) * Max(Abs(speedY) - slowdownSpeed, 0);
            }
            public void ariGeoSlowDownX(float ari, float geo)
            {
                speedX = Sign(speedX) * Max(0, Abs(speedX) * geo - ari);
            }
            public void ariGeoSlowDownY(float ari, float geo)
            {
                speedY = Sign(speedY) * Max(0, Abs(speedY) * geo - ari);
            }
            public void hoverIdle(float slowdownSpeed, int stateChangeChance)
            {
                slowDown(slowdownSpeed);
                if (rand.Next(stateChangeChance) == 0)
                {
                    state = 1;
                }
            }
            public void clampSpeed(float clampX, float clampY)
            {
                speedX = Clamp(-clampX, speedX, clampX);
                speedY = Clamp(-clampY, speedY, clampY);
            }
            public void moveEntity()
            {
                if (type == 0) // fairy
                {
                    if (state == 0) // idle
                    {
                        hoverIdle(0.5f, 100);
                    }
                    else if (state == 1) // moving in the air randomly
                    {
                        changeSpeedRandom(0.5f);
                        if (rand.Next(333) == 0)
                        {
                            state = 0;
                        }
                    }
                    else { state = 0; }
                }
                else if (type == 1) // frog
                {
                    applyGravity();
                    (int type, int subType) material = screen.getTileContent((posX, posY - 1));
                    if (material.type > 0)
                    {
                        ariGeoSlowDownX(0.2f, 0.85f);
                        if (rand.NextDouble() > 0.05f)
                        {
                            jumpRandom(2.5f, 2.5f);
                        }
                    }
                }
                else if (type == 2) // fish
                {
                    (int type, int subType) material = screen.getTileContent((posX, posY));
                    if (material.type < 0)
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
                        slowDown(0.5f);

                        if (rand.Next(50) == 0)
                        {
                            state = 1;
                        }
                    }
                    else if (state == 1) // moving in water
                    {
                        ariGeoSlowDownX(0.12f, 0.9f);
                        ariGeoSlowDownY(0.12f, 0.9f);

                        changeSpeedRandom(0.5f);

                        if (rand.Next(50) == 0)
                        {
                            changeSpeedRandom(2.5f); // big dash nyoooooom
                        }
                        else if (rand.Next(1000) == 0)
                        {
                            state = 0;
                        }
                    }
                    else if (state == 2) // outside water
                    {
                        applyGravity();
                        (int type, int subType) material2 = screen.getTileContent((posX, posY - 1)); // +1 cause coordinates are inverted lol (no)
                        if (material2.type > 0)
                        {
                            ariGeoSlowDownX(0.12f, 0.9f);
                            if (rand.Next(10) != 0)
                            {
                                jumpRandom(1, 2);
                            }
                        }
                    }
                }
                if (type == 3) // hornet
                {
                    if (subType == 0)
                    {
                        applyGravity();
                        if (timeElapsed - timeAtBirth > 30)
                        {
                            transformEntity(3, 1, true);
                            timeAtLastStateChange = timeElapsed;
                        }
                    }
                    else if (subType == 1)
                    {
                        applyGravity();
                        if (food < 3)
                        {
                            if (timeElapsed - timeAtLastStateChange > 15 + 15*food)
                            {
                                if (nest != null && !nest.hungryLarvae.Contains(this))
                                {
                                    nest.hungryLarvae.Add(this);
                                }
                            }
                        }
                        else if (timeElapsed - timeAtLastStateChange > 60)
                        {
                            transformEntity(3, 2, true);
                            timeAtLastStateChange = timeElapsed;
                            goto AfterTest;
                        }
                        (int type, int subType) material = screen.getTileContent((posX, posY - 1));
                        if (material.type > 0)
                        {
                            ariGeoSlowDownX(0.2f, 0.85f);
                            if (rand.NextDouble() < 0.05f)
                            {
                                jumpRandom(1, 1.5f);
                            }
                        }
                    }
                    else if (subType == 2)
                    {
                        hoverIdle(0.5f, 100);
                        applyGravity();
                        if (timeElapsed - timeAtLastStateChange > 30)
                        {
                            if (nest != null)
                            {
                                if (nest.larvae.Contains(this))
                                {
                                    nest.larvae.Remove(this);
                                }
                                int id = nest.getRoomId(this);
                                if (id >= 0)
                                {
                                    nest.rooms[id].assignedEntities.Remove(this);
                                    nest.rooms[id].contentCount--;
                                }
                                nest.adults.Add(this);
                            }
                            transformEntity(3, 2, true);
                            timeAtLastStateChange = timeElapsed;
                        }
                    }
                    else if (subType == 3)
                    {
                        if (state == 0) // idle
                        {
                            hoverIdle(0.5f, 100);
                        }
                        else if (state == 1) // moving in the air randomly
                        {
                            changeSpeedRandom(0.5f);
                            if (rand.Next(333) == 0)
                            {
                                state = 0;
                            }
                            else if (nest != null)
                            {
                                if (inventoryElements.Contains((-5, 0, 0)) && nest.availableHoneyRooms.Count > 0)
                                {
                                    Room targetRoom = nest.rooms[nest.availableHoneyRooms[rand.Next(nest.availableHoneyRooms.Count)]];
                                    targetPos = targetRoom.dropPositions[rand.Next(targetRoom.dropPositions.Count)];
                                    if (pathfindToLocation(targetPos))
                                    {
                                        state = 10007;
                                    }
                                    else
                                    {
                                        pathToTarget = new List<(int x, int y)>();
                                        simplifiedPathToTarget = new List<(int x, int y)>();
                                    }
                                }
                                else if (rand.Next(3) == 0 && nest.hungryLarvae.Count > 0)
                                {
                                    Room roomToTest = nest.getRandomRoomOfType(2);
                                    ((int x, int y) pos, bool found) returnTuple = roomToTest.findTileOfTypeInRoom(-5);
                                    if (!returnTuple.found) { goto AfterTest; }
                                    targetPos = returnTuple.pos;
                                    if (pathfindToLocation(targetPos))
                                    {
                                        state = 10006;
                                    }
                                    else
                                    {
                                        pathToTarget = new List<(int x, int y)>();
                                        simplifiedPathToTarget = new List<(int x, int y)>();
                                    }
                                }
                                else if (rand.Next(3) > 0 && nest.eggsToLay > 0 && nest.availableNurseries.Count > 0)
                                {
                                    Room targetRoom = nest.rooms[nest.availableNurseries[rand.Next(nest.availableNurseries.Count)]];
                                    targetPos = targetRoom.dropPositions[rand.Next(targetRoom.dropPositions.Count)];
                                    if (pathfindToLocation(targetPos))
                                    {
                                        state = 10008;
                                    }
                                    else
                                    {
                                        pathToTarget = new List<(int x, int y)>();
                                        simplifiedPathToTarget = new List<(int x, int y)>();
                                    }
                                }
                                else if (rand.Next(3) == 0 && findPointOfInterestInPlants((2, 1)))
                                {
                                    if (pathfindToLocation(targetPos))
                                    {
                                        state = 10005;
                                    }
                                    else
                                    {
                                        pathToTarget = new List<(int x, int y)>();
                                        simplifiedPathToTarget = new List<(int x, int y)>();
                                    }
                                }
                                else if (nest.digErrands.Count > 0)
                                {
                                    targetPos = nest.digErrands[rand.Next(nest.digErrands.Count)];
                                    if (pathfindToLocation(targetPos))
                                    {
                                        state = 10006;
                                    }
                                    else
                                    {
                                        pathToTarget = new List<(int x, int y)>();
                                        simplifiedPathToTarget = new List<(int x, int y)>();
                                    }
                                }
                            }
                        }
                        else if (state >= 10000) // moving in the air towards direction
                        {
                            // if following path
                            float realDiffX;
                            float realDiffY;
                            if (simplifiedPathToTarget.Count > 0)
                            {
                                realDiffX = simplifiedPathToTarget[0].x + 0.5f - realPosX;
                                realDiffY = simplifiedPathToTarget[0].y + 0.5f - realPosY;
                                while (Abs(realDiffX) < 0.9f && Abs(realDiffY) < 0.9f)
                                {
                                    simplifiedPathToTarget.RemoveAt(0);
                                    if (simplifiedPathToTarget.Count == 0) { break; }
                                    realDiffX = simplifiedPathToTarget[0].x + 0.5f - realPosX;
                                    realDiffY = simplifiedPathToTarget[0].y + 0.5f - realPosY;
                                }
                            }
                            else
                            {
                                realDiffX = targetPos.x + 0.5f - realPosX;
                                realDiffY = targetPos.y + 0.5f - realPosY;
                            }

                            int maxDist;
                            if (state == 10005) { maxDist = 0; }
                            else { maxDist = 1; }
                            if (manhattanDistance(targetPos, (posX, posY)) <= maxDist)
                            {
                                if (state == 10009)
                                {
                                    if (manhattanDistance((targetEntity.posX, targetEntity.posY), (posX, posY)) > maxDist)
                                    {
                                        targetPos = (targetEntity.posX, targetEntity.posY);
                                        if (pathfindToLocation(targetPos))
                                        {

                                        }
                                        else
                                        {
                                            targetEntity = null;
                                            pathToTarget = new List<(int x, int y)>();
                                            simplifiedPathToTarget = new List<(int x, int y)>();
                                            state = 0;
                                        }
                                        goto AfterTest;
                                    }
                                }
                                timeAtLastDig = timeElapsed;
                                state -= 10000; // go to corresponding state, easier to to +3 lol (wait what no??)
                                speedX = 0;
                                speedY = 0;
                                //goto AfterTest; // makes so it digs next frame, idk if to change idk
                            }
                            else
                            {
                                if (Abs(realDiffX) < 0.5f) { speedX = Sign(speedX) * Max(Abs(speedX) - 0.55f, 0); }
                                else if (speedX * speedX > Abs(realDiffX) && Sign(speedX) == Sign(realDiffX)) { speedX -= Sign(realDiffX) * 0.35f; }
                                else { speedX += Sign(realDiffX) * 0.35f; }
                                if (Abs(realDiffY) < 0.5f) { speedY = Sign(speedY) * Max(Abs(speedY) - 0.55f, 0); }
                                else if (speedY * speedY > Abs(realDiffY) && Sign(speedY) == Sign(realDiffY)) { speedY -= Sign(realDiffY) * 0.35f; }
                                else { speedY += Sign(realDiffY) * 0.35f; }

                                if (rand.Next(2500) == 0) { state = 0; }
                            }
                        }
                        else if (state == 5) // preparing to dig plant
                        {
                            if (timeAtLastDig + 1 < timeElapsed)
                            {
                                if (tryPlantDig(targetPos.x, targetPos.y))
                                {
                                    tryManufactureElement();
                                }
                                state = 0;
                            }
                        }
                        else if (state == 6) // preparing to dig tile
                        {
                            if (timeAtLastDig + 1 < timeElapsed)
                            {
                                (int type, int subType) dugTile = Dig(targetPos.x, targetPos.y);
                                if (nest != null)
                                {
                                    if (dugTile.type >= 0) { nest.digErrands.Remove(targetPos); }
                                    if (dugTile.type == -5 && nest.hungryLarvae.Count > 0)
                                    {
                                        targetEntity = nest.hungryLarvae[rand.Next(nest.hungryLarvae.Count)];
                                        targetPos = (targetEntity.posX, targetEntity.posY);
                                        if (pathfindToLocation(targetPos))
                                        {
                                            state = 10009;
                                        }
                                        else
                                        {
                                            targetEntity = null;
                                            pathToTarget = new List<(int x, int y)>();
                                            simplifiedPathToTarget = new List<(int x, int y)>();
                                            state = 0;
                                        }
                                    }
                                    else { state = 0; }
                                }
                                else { state = 0; }
                            }
                        }
                        else if (state == 7) // preparing to place honeyy
                        {
                            if (timeAtLastPlace + 1 < timeElapsed)
                            {
                                Place(targetPos.x, targetPos.y, (-5, 0, 0), false);
                                if (nest != null)
                                {
                                    Room room = nest.rooms[nest.getRoomId(targetPos)];
                                    if (room.type == 2)
                                    {
                                        room.testFullness();
                                        if (room.isFull)
                                        {
                                            nest.updateDropPositions();
                                        }
                                    }
                                }
                                state = 0;
                            }
                        }
                        else if (state == 8) // preparing to LAY AN EGGGGG
                        {
                            if (nest != null && nest.eggsToLay > 0)
                            {
                                if (timeAtLastPlace + 1 < timeElapsed)
                                {
                                    Room room = nest.rooms[nest.getRoomId(targetPos)];
                                    if (room.type == 3 && Place(targetPos.x, targetPos.y, (3, 0, 1), true))
                                    {
                                        Entity kiddo = screen.entitesToAdd[screen.entitesToAdd.Count - 1];
                                        kiddo.nest = nest;
                                        room.assignedEntities.Add(kiddo);  // since it's the last to be added, add last entity in entity list to the room
                                        nest.larvae.Add(kiddo);
                                        nest.eggsToLay--;
                                        room.testFullness();
                                        if (room.isFull)
                                        {
                                            nest.updateDropPositions();
                                        }
                                    }
                                    state = 0;
                                }
                            }
                            else
                            {
                                state = 0;
                            }
                        }
                        else if (state == 9) // feeding kiddo !
                        {
                            if (nest != null && nest.hungryLarvae.Contains(targetEntity))
                            {
                                tryFeedTargetEntity();
                            }
                            state = 0;
                        }
                        else { state = 0; }
                    }
                AfterTest:;
                }
                else if (type == 4)
                {
                    (int type, int subType) material = screen.getTileContent((posX, posY));
                    if (material.type == 0)
                    {
                        applyGravity();
                        changeSpeedRandom(0.5f);
                    }
                    else if (material.type < 0)
                    {
                        if (subType == 1)
                        {
                            changeSpeedRandom(0.1f);
                            clampSpeed(1, 1);
                        }
                        else
                        {
                            applyGravity();
                            changeSpeedRandom(0.5f);
                        }
                    }
                    else
                    {
                        changeSpeedRandom(0.05f);
                        clampSpeed(0.2f, 0.2f);
                    }
                }
                else if (type == 5) // water skipper
                {
                    bool isFalling = false;
                    (int type, int subType) material = screen.getTileContent((posX, posY));
                    (int type, int subType) materialUnder = screen.getTileContent((posX, posY - 1));

                    if (materialUnder.type != 0)
                    {
                        if (rand.Next(20) == 0) { jumpRandom(7, 0); }
                    }
                    else if (materialUnder.type == 0) { isFalling = true; }

                    if (material.type < 0) { speedY += 0.4f; }
                    else if (material.type == 0 && isFalling) { applyGravity(); }
                    ariGeoSlowDownY(0.15f, 0.8f);
                    ariGeoSlowDownX(0.1f, 0.9f);
                }
                if (type != 2 && (type, subType) != (4, 1))
                {
                    (int type, int subType) material = screen.getTileContent((posX, posY));
                    if (material.type < 0)
                    {
                        ariGeoSlowDownX(0.15f, 0.85f);
                    }
                }

                actuallyMoveTheEntity();



                // test what happens if in special liquids (fairy lake, lava...)
                {
                    (int type, int subType) material = screen.getTileContent((posX, posY));
                    if (type != 0 && material == (-3, 0))
                    {
                        if (rand.Next(10) == 0)
                        {
                            if (type == 2 && subType == 1) { transformEntity(0, 3, true); }
                            else { transformEntity(0, 0, true); }
                        }
                    }
                    if (type == 2 && material == (-7, 0))
                    {
                        if (rand.Next(10) == 0)
                        {
                            transformEntity(2, 1, true);
                        }
                    }
                    if (material == (-4, 0) && type != 2 && (type, subType) != (0, 3))
                    {
                        if (rand.Next(10) == 0)
                        {
                            screen.entitesToRemove[id] = this;
                        }
                    }
                    if (material == (-7, 0) && type != 2 && (type, subType) != (0, 3) && (type, subType) != (4, 1) && (type, subType) != (1, 1) && (type, subType) != (1, 2))
                    {
                        if (rand.Next(10) == 0)
                        {
                            screen.entitesToRemove[id] = this;
                        }
                    }
                }
            }
            public void transformEntity(int typeToPut, int subTypeToPut, bool setHp)
            {
                type = typeToPut;
                subType = subTypeToPut;
                if (setHp) { hp = entityStartingHp[(type, subType)]; }
                findLength();
                color = findColor();
                findLightColor();
            }
            public void transformEntity((int type, int subType) newType, bool setHp)
            {
                type = newType.type;
                subType = newType.subType;
                if (setHp) { hp = entityStartingHp[(type, subType)]; }
                findLength();
                color = findColor();
                findLightColor();
            }
            public void dieAndDrop(Player playerToGive)
            {
                ((int type, int subType, int megaType) element, int count) entityDrop = entityDrops[(type, subType)];
                if (type == 4) { entityDrop = (entityDrop.element, length); }
                playerToGive.addElementToInventory(entityDrop.element, entityDrop.count);
                screen.entitesToRemove[id] = this;
            }
            public void findLength()
            {
                if (type == 4)
                {
                    if (subType == 1) { length = 2 + seed % 9; }
                    else { length = 2 + seed % 5; }
                }
                else { length = 0; }
            }
            public void updatePastPositions((int x, int y) posToAdd)    // if lenght DECREASES it will not work anymore probably idk
            {
                int counto = pastPositions.Count;
                if (counto == 0 || pastPositions[0].x == posX || pastPositions[0].y == posY || manhattanDistance(pastPositions[0], (posX, posY)) > 2) // if pos-2 and current pos have no x and y in common, means there are diag and pos - 1 should not be added
                {
                    pastPositions.Insert(0, posToAdd);
                    if (counto >= length)
                    {
                        pastPositions.RemoveAt(counto - 1);
                    }
                }
            }
            public void actuallyMoveTheEntity()
            {
                (int x, int y) chunkPos;
                (int x, int y) previousPos = (posX, posY);
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
                    chunkPos = ChunkIdx(posX, posToTest);
                    if (!screen.loadedChunks.ContainsKey(chunkPos)) // if chunk to test is outside loaded chunks, save Entity in the non-loaded chunk
                    {
                        realPosY = realPosToTest;
                        saveEntity(this);
                        screen.entitesToRemove[id] = this;
                        return;
                    }
                    material = screen.getTileContent((posX, posToTest));
                    if (material.type <= 0 || type == 4) // if a worm or the material is not a solid tile, update positions and continue
                    {
                        realPosY = realPosToTest;
                        posY = posToTest;
                        toMoveY -= diff;
                        if (type == 4) { updatePastPositions(previousPos); } // to make worm's tails
                        previousPos = (posX, posY);
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
                    chunkPos = ChunkIdx(posToTest, posY);
                    if (!screen.loadedChunks.ContainsKey(chunkPos)) // if chunk to test is outside loaded chunks, save Entity in the non-loaded chunk
                    {
                        realPosX = realPosToTest;
                        saveEntity(this);
                        screen.entitesToRemove[id] = this;
                        return;
                    }
                    material = screen.getTileContent((posToTest, posY));
                    if (material.type <= 0 || type == 4) // if a worm or the material is not a solid tile, update positions and continue
                    {
                        realPosX = realPosToTest;
                        posX = posToTest;
                        toMoveX -= diff;
                        if (type == 4) { updatePastPositions(previousPos); } // to make worm's tails
                        previousPos = (posX, posY);
                    }
                    else
                    {
                        speedX = 0;
                        break;
                    }
                }
            }
            public void teleport((int x, int y) newPos, int screenIdToTeleport)
            {
                Screens.Screen screenToTeleportTo = screen.game.getScreen(screenIdToTeleport);
                screen.entitesToRemove[id] = this;
                screenToTeleportTo.activeEntities[id] = this;
                screen = screenToTeleportTo;
                realPosX = newPos.x;
                posX = newPos.x;
                realPosY = newPos.y;
                posY = newPos.y;
                speedX = 0;
                speedY = 0;
                timeAtLastTeleportation = timeElapsed;
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
            public void addElementToInventory((int index, int subType, int typeOfElement) elementToAdd, int quantityToAdd = 1)
            {
                if (!inventoryQuantities.ContainsKey(elementToAdd))
                {
                    inventoryQuantities.Add(elementToAdd, quantityToAdd);
                    inventoryElements.Add(elementToAdd);
                    return;
                }
                if (inventoryQuantities[elementToAdd] != -999)
                {
                    inventoryQuantities[elementToAdd] += quantityToAdd;
                }
                return;
            }
            public void removeElementFromInventory((int index, int subType, int typeOfElement) elementToRemove, int quantityToRemove = 1)
            {
                if (!inventoryQuantities.ContainsKey(elementToRemove)) { return; }
                if (inventoryQuantities[elementToRemove] != -999)
                {
                    inventoryQuantities[elementToRemove] -= quantityToRemove;
                    if (inventoryQuantities[elementToRemove] <= 0)
                    {
                        inventoryQuantities.Remove(elementToRemove);
                        inventoryElements.Remove(elementToRemove);
                    }
                }
            }
            public bool tryPlantDig(int posToDigX, int posToDigY)
            {
                (int type, int subType) value;
                foreach (Plant plant in screen.activePlants.Values)
                {
                    value = plant.testDig(posToDigX, posToDigY);
                    if (value.type != 0)
                    {
                        addElementToInventory((value.type, value.subType, 3));
                        timeAtLastDig = timeElapsed;
                        return true;
                    }
                }
                return false;
            }
            public (int type, int subType) Dig(int posToDigX, int posToDigY)
            {
                (int, int) chunkPos = ChunkIdx(posToDigX, posToDigY);
                if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest)) { return (0, 0); }
                (int x, int y) tileIndex = PosMod((posToDigX, posToDigY));
                (int type, int subType) tileContent = chunkToTest.fillStates[tileIndex.x, tileIndex.y];
                if (tileContent.type != 0)
                {
                    addElementToInventory((tileContent.type, tileContent.subType, 0));
                    chunkToTest.tileModification(posToDigX, posToDigY, (0, 0));
                    elementsPossessed++;
                    timeAtLastDig = timeElapsed;
                }
                return tileContent;
            }
            public bool Place(int posToDigX, int posToDigY, (int type, int subType, int typeOfElement) elementToPlace, bool forcePlace)
            {
                (int, int) chunkPos = ChunkIdx(posToDigX, posToDigY);
                if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest)) { return false; }
                if (!forcePlace && !inventoryElements.Contains(elementToPlace)) { return false; } 
                (int x, int y) tileIndex = PosMod((posToDigX, posToDigY));
                int tileState = chunkToTest.fillStates[tileIndex.x, tileIndex.y].type;
                if (tileState == 0 || tileState < 0 && elementToPlace.typeOfElement > 0)
                {
                    if (elementToPlace.typeOfElement == 0)
                    {
                        chunkToTest.screen.setTileContent((posToDigX, posToDigY), (elementToPlace.type, elementToPlace.subType));
                        timeAtLastPlace = timeElapsed;
                    }
                    else if (elementToPlace.typeOfElement == 1)
                    {
                        Entity newEntity = new Entity(screen, (posToDigX, posToDigY), (elementToPlace.type, elementToPlace.subType));
                        screen.entitesToAdd[newEntity.id] = newEntity;
                        timeAtLastPlace = timeElapsed;
                    }
                    else if (elementToPlace.typeOfElement == 2)
                    {
                        Plant newPlant = new Plant(screen, (posToDigX, posToDigY), (elementToPlace.type, elementToPlace.subType));
                        if (!newPlant.isDeadAndShouldDisappear) { screen.activePlants[newPlant.id] = newPlant; }
                        timeAtLastPlace = timeElapsed;
                    }
                    else { return false; }
                    if (!forcePlace) { removeElementFromInventory(elementToPlace); }
                    return true;
                }
                return false;
            }
            public bool tryManufactureElement()
            {
                foreach ((int index, int subType, int typeOfElement) tupel in inventoryElements)
                {
                    if (tupel == (7, 0, 3)) // if it got pollen, try to make honey
                    {
                        if (inventoryQuantities[tupel] > 3)
                        {
                            inventoryQuantities[tupel] = inventoryQuantities[tupel] - 3;
                            addElementToInventory((-5, 0, 0));
                            return true;
                        }
                        else { return false; }
                    }
                }
                return false;
            }
            public bool tryFeedTargetEntity()
            {
                if (targetEntity == null) { return false; }
                if (targetEntity.type == 3 && targetEntity.subType == 1)
                {
                    if (inventoryElements.Contains((-5, 0, 0)) && targetEntity.food < 3 && timeElapsed - targetEntity.timeAtBirth > 45 + 15 * food)
                    {
                        removeElementFromInventory((-5, 0, 0));
                        targetEntity.food++;
                        if (targetEntity.food < 3 && timeElapsed - targetEntity.timeAtBirth > 45 + 15 * food)
                        {
                            targetEntity.nest.hungryLarvae.Remove(targetEntity);
                        }
                        return true;
                    }
                }
                targetEntity.nest.hungryLarvae.Remove(targetEntity);
                return false;
            }
        }
    }   
}
