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
using static System.Net.Mime.MediaTypeNames;

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

            public (int x, int y) targetPos = (0, 0);
            public List<(int x, int y)> pathToTarget = new List<(int x, int y)>();

            public Dictionary<(int index, int subType, int typeOfElement), int> inventoryQuantities;
            public List<(int index, int subType, int typeOfElement)> inventoryElements;
            public int elementsPossessed = 0;

            public float timeAtLastDig = -9999;
            public float timeAtLastPlace = -9999;

            public bool isDeadAndShouldDisappear = false;

            public Entity(Chunk chunk, EntityJson entityJson)
            {
                screen = chunk.screen;
                realPosX = entityJson.pos.Item1;
                posX = (int)realPosX;
                realPosY = entityJson.pos.Item2;
                posY = (int)realPosY;
                targetPos = entityJson.tPos;
                type = entityJson.type.Item1;
                subType = entityJson.type.Item2;
                seed = entityJson.seed;
                (Dictionary<(int index, int subType, int typeOfElement), int>, List<(int index, int subType, int typeOfElement)>) returnTuple = arrayToInventory(entityJson.inv);
                inventoryQuantities = returnTuple.Item1;
                inventoryElements = returnTuple.Item2;
                state = entityJson.state;
                color = findColor();
            }
            public Entity(Chunk chunk)
            {
                screen = chunk.screen;
                seed = LCGint1(Abs((int)chunk.chunkSeed));
                placeEntity(chunk);
                findType(chunk);
                color = findColor();
                initializeInventory();
            }
            public Entity(Chunk chunk, (int, int) positionToPut, int typeToPut, int subTypeToPut)
            {
                screen = chunk.screen;
                posX = positionToPut.Item1;
                realPosX = posX;
                posY = positionToPut.Item2;
                realPosY = posY;
                targetPos = positionToPut;
                type = typeToPut;
                subType = subTypeToPut;
                seed = rand.Next(1000000000); //                               FALSE RANDOM NOT SEEDED ARGHHEHEEEE
                initializeInventory();
                color = findColor();
            }
            public void findType(Chunk chunk)
            {
                (int x, int y) tileIndex = GetChunkTileIndex(posX, posY, 32);
                int biome = chunk.biomeIndex[tileIndex.x, tileIndex.y][0].Item1;
                if (chunk.fillStates[tileIndex.x, tileIndex.y] < 0)
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
                    int randX = rand.Next(32);
                    int randY = rand.Next(32);
                    if (chunk.fillStates[randX, randY] <= 0)
                    {
                        posX = chunk.position.Item1 * 32 + randX;
                        realPosX = posX;
                        posY = chunk.position.Item2 * 32 + randY;
                        realPosY = posY;
                        targetPos = (posX, posY);
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
            public void findRandomDestination(int distance)
            {
                targetPos = (posX + rand.Next(distance*2+1) - distance, posY + rand.Next(distance*2+1) - distance);
            }
            public bool findPointOfInterestInPlants(int elementOfInterest)
            {
                int plantCount = screen.activePlants.Count;
                if (plantCount == 0) { return false; }
                Plant plantToTest = screen.activePlants[rand.Next(plantCount)];
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
            public int evaluateTile((int x, int y) location, Dictionary<(int x, int y), int> costOfTiles)
            {
                (int x, int y) chunkPos = screen.findChunkAbsoluteIndex(location.x, location.y);
                if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                {
                    (int x, int y) tileIndex = GetChunkTileIndex(location, 32);
                    int tileContent = chunkToTest.fillStates[tileIndex.x, tileIndex.y];
                    if (tileContent <= 0)
                    {
                        costOfTiles[location] = costDict[tileContent];
                        return costOfTiles[location];
                    }
                }
                costOfTiles[location] = 1000000;
                return 1000000;
            }
            public bool testQueueAdd((int x, int y) tileToTest, Dictionary<(int x, int y), int> costOfTiles)
            {
                int tileCost;
                if (costOfTiles.TryGetValue(tileToTest, out int returnedResult))
                {
                    tileCost = returnedResult;
                }
                else
                {
                    tileCost = evaluateTile(tileToTest, costOfTiles);
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
                ((int x, int y) pos, float cost)[] neighbourDict = new ((int x, int y) pos, float cost)[]
                {
                    ((1, 0), 1), ((-1, 0), 1), ((0, -1), 1), ((0, 1), 1),
                }; ((int x, int y) pos, float cost)[] diagNeighbourDicto = new ((int x, int y) pos, float cost)[]
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
                        if (testQueueAdd(posToAddMaybe, costOfTiles))
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
                        if (testQueueAdd(posToAddMaybe, costOfTiles))
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
                        if (testQueueAdd(posToAddMaybe, costOfTiles))
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
                        if (testQueueAdd(posToAddMaybe, costOfTiles))
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
                        if (testQueueAdd(posToAddMaybe, costOfTiles))
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
                return true;
            }
            public void moveEntity()
            {
                (int, int) chunkPos;
                if (type == 0 || type == 3) // fairy and hornet
                {
                    if (state == 0) // idle
                    {
                        speedX = Sign(speedX) * Max(Abs(speedX) - 0.5f, 0);
                        speedY = Sign(speedY) * Max(Abs(speedY) - 0.5f, 0);

                        if (rand.Next(100) == 0)
                        {
                            state = 1;
                        }
                    }
                    else if (state == 1) // moving in the air randomly
                    {
                        speedX += (float)(rand.NextDouble()) - 0.5f;
                        speedY += (float)(rand.NextDouble()) - 0.5f;
                        if (rand.Next(333) == 0)
                        {
                            state = 0;
                        }
                        else if (rand.Next(10) == 0)
                        {
                            if (findPointOfInterestInPlants(7))
                            {
                                state = 2;
                                pathfindToLocation(targetPos);
                            }
                        }
                    }
                    else if (state == 2) // moving in the air towards direction
                    {
                        float realDiffX = targetPos.x+0.5f - realPosX;
                        float realDiffY = targetPos.y+0.5f - realPosY;

                        if (targetPos.x == posX && targetPos.y == posY)
                        {
                            timeAtLastDig = timeElapsed;
                            state = 3;
                            speedX = 0;
                            speedY = 0;
                            goto AfterTest;
                        }

                        if (Abs(realDiffX) < 0.5f) { speedX = Sign(speedX) * Max(Abs(speedX) - 0.55f, 0); }
                        else if (speedX*speedX > Abs(realDiffX) && Sign(speedX) == Sign(realDiffX)) { speedX -= Sign(realDiffX) * 0.35f; }
                        else { speedX += Sign(realDiffX) *0.35f; }
                        if (Abs(realDiffY) < 0.5f) { speedY = Sign(speedY) * Max(Abs(speedY) - 0.55f, 0); }
                        else if (speedY*speedY > Abs(realDiffY) && Sign(speedY) == Sign(realDiffY)) { speedY -= Sign(realDiffY) * 0.35f; }
                        else { speedY += Sign(realDiffY) *0.35f; }

                        if (rand.Next(150) == 0) { state = 1; }
                    }
                    else if (state == 3) // preparing to dig
                    {
                        if (timeAtLastDig + 1 < timeElapsed)
                        {
                            if (tryPlantDig(targetPos.x, targetPos.y))
                            {
                                if (type == 3)
                                {
                                    tryManufactureElement();
                                }
                                state = 0;
                            }
                            else { state = 0; }
                        }
                    }
                    else { state = 0; }
                AfterTest:;
                }


                else if (type == 1)
                {
                    speedY -= 0.5f;
                    chunkPos = screen.findChunkAbsoluteIndex(posX, posY - 1); // +1 cause coordinates are inverted lol
                    if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                    {
                        (int x, int y) tileIndex = GetChunkTileIndex(posX, posY - 1, 32);
                        if (chunkToTest.fillStates[tileIndex.x, tileIndex.y] > 0)
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

                    if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTesta))
                    {
                        (int x, int y) tileIndex = GetChunkTileIndex(posX, posY, 32);
                        if (chunkToTesta.fillStates[tileIndex.x, tileIndex.y] < 0)
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
                    }

                    if (state == 0) // idle
                    {
                        speedX = Sign(speedX) * Max(Abs(speedX) - 0.5f, 0);
                        speedY = Sign(speedY) * Max(Abs(speedY) - 0.5f, 0);

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
                            (int x, int y) tileIndex = GetChunkTileIndex(posX, posY - 1, 32);
                            if (chunkToTest.fillStates[tileIndex.x, tileIndex.y] > 0)
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
                    if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                    {
                        (int x, int y) tileIndex = GetChunkTileIndex(posX, posY, 32);
                        if (chunkToTest.fillStates[tileIndex.x, tileIndex.y] < 0)
                        {
                            speedX = speedX * 0.85f - Sign(speedX) * 0.15f;
                            speedY = speedY * 0.85f - Sign(speedY) * 0.15f;
                        }
                    }
                }
                float toMoveX = speedX;
                float toMoveY = speedY;

                while (Abs(toMoveY) > 0)
                {
                    chunkPos = screen.findChunkAbsoluteIndex(posX, posY + (int)Sign(toMoveY));
                    if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                    {
                        posY += (int)Sign(toMoveY);
                        saveEntity(this);
                        screen.entitesToRemove.Add(this);
                        return;
                    }
                    (int x, int y) tileIndex = GetChunkTileIndex(posX, posY + (int)Sign(toMoveY), 32);
                    if (chunkToTest.fillStates[tileIndex.x, tileIndex.y] <= 0)
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
                    if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                    {
                        posX += (int)Sign(toMoveX);
                        saveEntity(this);
                        screen.entitesToRemove.Add(this);
                        return;
                    }
                    (int x, int y) tileIndex = GetChunkTileIndex(posX + (int)Sign(toMoveX), posY, 32);
                    if (chunkToTest.fillStates[tileIndex.x, tileIndex.y] <= 0)
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

                {
                    chunkPos = screen.findChunkAbsoluteIndex(posX, posY);
                    if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                    {
                        return;
                    }
                    (int x, int y) tileIndex = GetChunkTileIndex(posX, posY, 32);
                    if (type != 0 && chunkToTest.fillStates[tileIndex.x, tileIndex.y] == -3)
                    {
                        if (rand.Next(10) == 0)
                        {
                            type = 0;
                            this.color = Color.Purple;
                        }
                    }
                    if (type != 2 && chunkToTest.fillStates[tileIndex.x, tileIndex.y] == -4)
                    {
                        if (rand.Next(10) == 0)
                        {
                            screen.entitesToRemove.Add(this);
                        }
                    }
                }
            }
            public bool testTargetedDig()
            {
                int value;
                foreach (Plant plant in screen.activePlants)
                {
                    value = plant.testDig(targetPos.x, targetPos.y);
                    if (value == 7)
                    {
                        Dig(targetPos.x, targetPos.y);
                        return true;
                    }
                }
                return false;
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
            public void addElementToInventory((int index, int subType, int typeOfElement) elementToAdd)
            {
                (int index, int subType, int typeOfElement)[] inventoryKeys = inventoryQuantities.Keys.ToArray();
                for (int i = 0; i < inventoryKeys.Length; i++)
                {
                    if (inventoryKeys[i] == elementToAdd)
                    {
                        if (inventoryQuantities[elementToAdd] != -999)
                        {
                            inventoryQuantities[elementToAdd]++;
                        }
                        timeAtLastDig = timeElapsed;
                        return;
                    }
                }
                // there was none of the thing present in the inventory already so gotta create it
                inventoryQuantities.Add(elementToAdd, 1);
                inventoryElements.Add(elementToAdd);
            }
            public void removeElementFromInventory((int index, int subType, int typeOfElement) elementToRemove)
            {
                if (inventoryQuantities[elementToRemove] != -999)
                {
                    inventoryQuantities[elementToRemove]--;
                    if (inventoryQuantities[elementToRemove] <= 0)
                    {
                        inventoryQuantities.Remove(elementToRemove);
                        inventoryElements.Remove(elementToRemove);
                    }
                    elementsPossessed--;
                }
            }
            public bool tryPlantDig(int posToDigX, int posToDigY)
            {
                int value;
                foreach (Plant plant in screen.activePlants)
                {
                    value = plant.testDig(posToDigX, posToDigY);
                    if (value != 0)
                    {
                        addElementToInventory((value, 0, 3));
                        timeAtLastDig = timeElapsed;
                        return true;
                    }
                }
                return false;
            }
            public void Dig(int posToDigX, int posToDigY)
            {
                (int, int) chunkPos = screen.findChunkAbsoluteIndex(posToDigX, posToDigY);
                if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest)) { return; }
                (int x, int y) tileIndex = GetChunkTileIndex(posToDigX, posToDigY, 32);
                int tileContent = chunkToTest.fillStates[tileIndex.x, tileIndex.y];
                if (tileContent != 0)
                {
                    addElementToInventory((tileContent, 0, 0));
                    chunkToTest.fillStates[tileIndex.x, tileIndex.y] = 0;
                    chunkToTest.findTileColor(tileIndex.x, tileIndex.y);
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
                (int index, int subType, int typeOfElement) elementToPlace = inventoryElements[rand.Next(inventoryElements.Count)];
                (int x, int y) tileIndex = GetChunkTileIndex(posToDigX, posToDigY, 32);
                int tileState = chunkToTest.fillStates[tileIndex.x, tileIndex.y];
                if (tileState == 0 || tileState < 0 && elementToPlace.typeOfElement > 0)
                {
                    if (elementToPlace.typeOfElement == 0)
                    {
                        chunkToTest.fillStates[(posToDigX % 32 + 32) % 32, (posToDigY % 32 + 32) % 32] = elementToPlace.index;
                        chunkToTest.findTileColor((posToDigX % 32 + 32) % 32, (posToDigY % 32 + 32) % 32);
                        chunkToTest.testLiquidUnstableLiquid(posToDigX, posToDigY);
                        chunkToTest.modificationCount += 1;
                        timeAtLastPlace = timeElapsed;
                    }
                    else if (elementToPlace.typeOfElement == 1)
                    {
                        Entity newEntity = new Entity(chunkToTest, (posToDigX, posToDigY), elementToPlace.index, elementToPlace.subType);
                        screen.activeEntities.Add(newEntity);
                        timeAtLastPlace = timeElapsed;
                    }
                    else if (elementToPlace.typeOfElement == 2)
                    {
                        Plant newPlant = new Plant(chunkToTest, (posToDigX, posToDigY), elementToPlace.index, elementToPlace.subType);
                        if (!newPlant.isDeadAndShouldDisappear) { screen.activePlants.Add(newPlant); }
                        timeAtLastPlace = timeElapsed;
                    }
                    else { return; }
                    removeElementFromInventory(elementToPlace);
                }
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
        }
    }   
}
