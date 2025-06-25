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
using Newtonsoft.Json.Linq;

using static Cave.Form1;
using static Cave.Globals;
using static Cave.MathF;
using static Cave.Sprites;
using static Cave.Structures;
using static Cave.Nests;
using static Cave.Entities;
using static Cave.Traits;
using static Cave.Attacks;
using static Cave.Files;
using static Cave.Plants;
using static Cave.Screens;
using static Cave.Chunks;
using static Cave.Players;
using static Cave.Particles;
using static Cave.Dialogues;

namespace Cave
{
    public partial class Globals
    {
        public static Dictionary<int, int> costDict = new Dictionary<int, int>
        {
            { 0, 1 },       // air
            { -1, 3 },      // piss
            { -2, 3 },      // water
            { -3, 999 },    // fairy liquid
            { -4, 999999 }, // lava (cannot cross)
            { -5, 5 },      // honey
            { -6, 3 },      // blood
            { -7, 999999 }, // acid (cannot cross)
        };
    }
    public class Entities
    {
        public class Entity
        {
            public Screens.Screen screen;

            public int seed;
            public int id;
            public (int type, int subType) type; // Type :     0 = fairy , 1 = frog , 2 = fish, 3 = hornet, 4 = worm, 5 = waterSkipper, 6 = goblin
                                                 // Subtype : (0 : normal, obsidian, frost, skeleton). (1 : frog, carnal, skeletal). (2 : fish, skeleton). (3 : egg, larva, cocoon, adult). (4 : worm, nematode). ( 5 : waterSkipper) 
            public EntityTraits traits;
            public int state; // 0 = idle I guess idk
            public float realPosX = 0;
            public float realPosY = 0;
            public int posX = 0;
            public int posY = 0;
            public float speedX = 0;
            public float speedY = 0;
            public (int x, int y) direction = (1, 0);

            public bool inWater = false;
            public bool onWater = false;
            public bool onGround = false;
            public bool inGround = false;

            public Color color;
            public Color lightColor;

            public List<(int x, int y)> pastPositions = new List<(int x, int y)>();
            public int length;

            public bool isCurrentlyClimbing = false;

            public Entity targetEntity = null;
            public (int x, int y) targetPos = (0, 0);
            public List<(int x, int y)> pathToTarget = new List<(int x, int y)>();
            public List<(int x, int y)> simplifiedPathToTarget = new List<(int x, int y)>();

            public Dictionary<(int index, int subType, int typeOfElement), int> inventoryQuantities;
            public List<(int index, int subType, int typeOfElement)> inventoryElements;

            public Attack currentAttack = null;

            public float hp = 1;
            public int food = 0;
            public float mana = 0;

            public float timeAtBirth = 0;
            public float timeAtLastStateChange = 0;
            public float timeAtLastDig = -9999;
            public float timeAtLastPlace = -9999;
            public float timeAtLastGottenHit = -9999;
            public float timeAtLastTeleportation = -9999;
            public float wingTimer = 0;

            public bool isDeadAndShouldDisappear = false;

            public Nest nest = null;
            public int nestId = -1;
            public Entity() { }
            public Entity(Screens.Screen screenToPut, EntityJson entityJson)
            {
                screen = screenToPut;
                realPosX = entityJson.pos.Item1;
                posX = (int)realPosX;
                realPosY = entityJson.pos.Item2;
                posY = (int)realPosY;
                targetPos = entityJson.tPos;
                seed = entityJson.seed; ;
                id = entityJson.id;
                nestId = entityJson.nstId;
                testOrphanage();
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
            public Entity(Chunk chunk, (int type, int subType) typeToPut, (int x, int y) posToPut)
            {
                screen = chunk.screen;
                posX = posToPut.Item1;
                realPosX = posX;
                posY = posToPut.Item2;
                realPosY = posY;
                targetPos = posToPut;
                seed = LCGint1(Abs((int)chunk.chunkSeed));
                type = typeToPut;
                id = currentEntityId;
                transformEntity(typeToPut); // contains transformEntity so contains Color and LightColor finding
                initializeInventory();
                timeAtBirth = timeElapsed;

                currentEntityId++;
            }
            public Entity(Screens.Screen screenToPut, (int, int) posToPut, (int type, int subType) typeToPut)
            {
                screen = screenToPut;
                posX = posToPut.Item1;
                realPosX = posX;
                posY = posToPut.Item2;
                realPosY = posY;
                targetPos = posToPut;
                seed = rand.Next(1000000000); //                              TO CHANGE FALSE RANDOM NOT SEEDED ARGHHEHEEEE
                id = currentEntityId;
                transformEntity(typeToPut, true);
                initializeInventory();
                timeAtBirth = timeElapsed;

                currentEntityId++;
            }
            public void testOrphanage()
            {
                if (nestId == -1 || nest != null) { return; }
                if (screen.activeStructures.ContainsKey(nestId))
                {
                    screen.activeStructures[nestId].addEntityToStructure(this);
                }
            }
            public Color findColor()
            {
                float hueVar = (float)((seed % 11) * 0.2f - 1);
                float shadeVar = (float)((LCGz(seed) % 11) * 0.2f - 1);
                ColorRange c = traits.colorRange;
                return Color.FromArgb(
                    ColorClamp(c.r.v + (int)(hueVar * c.r.h) + (int)(shadeVar * c.r.s)),
                    ColorClamp(c.g.v + (int)(hueVar * c.g.h) + (int)(shadeVar * c.g.s)),
                    ColorClamp(c.b.v + (int)(hueVar * c.b.h) + (int)(shadeVar * c.b.s))
                );
            }
            public void findLightColor()
            {
                lightColor = Color.FromArgb(255, (color.R + 255) / 2, (color.G + 255) / 2, (color.B + 255) / 2);
            }
            public Color getLightColorFromColor(Color colorToGetLightFrom)
            {
                return Color.FromArgb(255, (colorToGetLightFrom.R + 255) / 2, (colorToGetLightFrom.G + 255) / 2, (colorToGetLightFrom.B + 255) / 2);
            }
            public void findRandomDestination(int distance)
            {
                targetPos = (posX + rand.Next(distance * 2 + 1) - distance, posY + rand.Next(distance * 2 + 1) - distance);
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
                return (int)(diffX + diffY - 2 * diagNumber + 1.41421356237f * diagNumber);
            }
            public int evaluateTile((int x, int y) location, (int x, int y) targetLocation, Dictionary<(int x, int y), int> costOfTiles)
            {
                if (location == targetLocation)
                {
                    costOfTiles[location] = 0;
                    return 0;
                }
                TileTraits traits = screen.getChunkFromPixelPos(location).fillStates[PosMod(location.x), PosMod(location.y)];
                if (!traits.isSolid)
                {
                    costOfTiles[location] = costDict[traits.type.type];
                    return costOfTiles[location];
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
                    screen.getTileContent((targetLocation.x + 1, targetLocation.y)).isSolid
                    &&
                    screen.getTileContent((targetLocation.x - 1, targetLocation.y)).isSolid
                    &&
                    screen.getTileContent((targetLocation.x, targetLocation.y + 1)).isSolid
                    &&
                    screen.getTileContent((targetLocation.x, targetLocation.y - 1)).isSolid
                    )
                { return false; }


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
                            float costo = cumulativeCostOfTiles[tileToTest] + neighbour.cost * costOfTiles[posToAddMaybe];
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
                        if (posToTest.x - lastKeptPointPos.x == multCounter * direction.x && posToTest.y - lastKeptPointPos.y == multCounter * direction.y)
                        {
                            path.RemoveAt(idx + 1);
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
            public (TileTraits entityPos, TileTraits under) applyForces()
            {
                TileTraits tileUnder = screen.getTileContent((posX, posY - 1));
                TileTraits entityTile = screen.getTileContent((posX, posY));
                if (tileUnder.isSolid)          // On terrain
                {
                    onGround = true;
                    onWater = false;
                }
                else if (tileUnder.isLiquid)    // On water
                {
                    onGround = false;
                    onWater = true;
                }
                else                            // In air
                {
                    onGround = false;
                    onWater = false;
                }

                if (entityTile.isSolid)         // In terrain
                {
                    inGround = true;
                    inWater = false;
                    if (!traits.isDigging) { speedX = 0; speedY = 0; }
                }
                else if (entityTile.isLiquid)   // In water
                {
                    inGround = false;
                    inWater = true;
                    if (!traits.isSwimming) { ariGeoSlowDown(0.85f, 0.15f); speedY += 0.1f; }
                }
                else                            // In air
                {
                    inGround = false;
                    inWater = false;
                    if (onWater && traits.isJesus) { ariGeoSlowDownY(0.8f, 0.25f); }
                    else if (!onGround && !traits.isFlying && (!traits.isCliming || !isCurrentlyClimbing)) { speedY -= 0.5f; }
                }

                return (entityTile, tileUnder);
            }
            public void changeSpeedRandom(float range)
            {
                speedX += range * (float)(rand.NextDouble() - 0.5);
                speedY += range * (float)(rand.NextDouble() - 0.5);
            }
            public void jumpRandom(float rangeX, float maxY)
            {
                speedX += rangeX * (float)(rand.NextDouble() - 0.5);
                speedY += maxY * (float)(rand.NextDouble());
            }
            public void Jump(float changeX, float jumpSpeed)
            {
                speedX += changeX;
                speedY = Max(jumpSpeed, speedY);
            }
            public void slowDown(float slowdownSpeed)
            {
                speedX = Sign(speedX) * Max(Abs(speedX) - slowdownSpeed, 0);
                speedY = Sign(speedY) * Max(Abs(speedY) - slowdownSpeed, 0);
            }
            public void ariGeoSlowDown(float geo, float ari)
            {
                speedX = Sign(speedX) * Max(0, Abs(speedX) * geo - ari);
                speedY = Sign(speedY) * Max(0, Abs(speedY) * geo - ari);
            }
            public void ariGeoSlowDownGravity(float geo, float ari, float capX, float capY)
            {
                speedX = Abs(speedX) > capX ? MaxAbs(Sign(speedX) * capX, Sign(speedX) * Max(0, Abs(speedX) * geo - ari)) : speedX;
                speedY = speedY > capY ? MaxAbs(Sign(speedY) * capY, Sign(speedY) * Max(0, Abs(speedY) * geo - ari)) : speedY;
            }
            public void ariGeoSlowDownX(float geo, float ari)
            {
                speedX = Sign(speedX) * Max(0, Abs(speedX) * geo - ari);
            }
            public void ariGeoSlowDownY(float geo, float ari)
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
            public void clampSpeedX(float clampX)
            {
                speedX = Clamp(-clampX, speedX, clampX);
            }
            public void clampSpeedY(float clampY)
            {
                speedX = Clamp(-clampY, speedY, clampY);
            }
            public void moveHornet()
            {
                testOrphanage();
                if (type.subType == 0)
                {
                    if (timeElapsed - timeAtBirth > 30)
                    {
                        transformEntity((3, 1), true);
                        timeAtLastStateChange = timeElapsed;
                    }
                }
                else if (type.subType == 1)
                {
                    if (food < 3)
                    {
                        if (timeElapsed - timeAtLastStateChange > 15 + 15 * food)
                        {
                            if (nest != null && !nest.hungryLarvae.Contains(this)) { nest.hungryLarvae.Add(this); }
                        }
                    }
                    else if (timeElapsed - timeAtLastStateChange > 60)
                    {
                        transformEntity((3, 2), true);
                        timeAtLastStateChange = timeElapsed;
                        goto AfterTest;
                    }
                }
                else if (type.subType == 2)
                {
                    if (timeElapsed - timeAtLastStateChange > 30)
                    {
                        if (nest != null)
                        {
                            if (nest.larvae.Contains(this)) { nest.larvae.Remove(this); }
                            int id = nest.getRoomId(this);
                            if (id >= 0)
                            {
                                nest.rooms[id].assignedEntities.Remove(this);
                                nest.rooms[id].contentCount--;
                            }
                            nest.adults.Add(this);
                        }
                        transformEntity((3, 3), true);
                        timeAtLastStateChange = timeElapsed;
                    }
                }
                else if (type.subType == 3)
                {
                    moveAdultHornet();
                }
            AfterTest:;
            }
            public void moveAdultHornet()
            {
                if (state == 0) { hoverIdle(0.5f, 100); }   // idle
                else if (state == 1) // moving in the air randomly
                {
                    changeSpeedRandom(0.5f);
                    if (rand.Next(333) == 0) { state = 0; }
                    else if (nest != null)
                    {
                        if (inventoryElements.Contains((-5, 0, 0)) && nest.availableHoneyRooms.Count > 0)
                        {
                            Room targetRoom = nest.rooms[nest.availableHoneyRooms[rand.Next(nest.availableHoneyRooms.Count)]];
                            targetPos = targetRoom.dropPositions[rand.Next(targetRoom.dropPositions.Count)];

                            if (pathfindToLocation(targetPos)) { state = 10007; }
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
                            if (!returnTuple.found) { return; }
                            targetPos = returnTuple.pos;

                            if (pathfindToLocation(targetPos)) { state = 10006; }
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

                            if (pathfindToLocation(targetPos)) { state = 10008; }
                            else
                            {
                                pathToTarget = new List<(int x, int y)>();
                                simplifiedPathToTarget = new List<(int x, int y)>();
                            }
                        }
                        else if (rand.Next(3) == 0 && findPointOfInterestInPlants((2, 1)))
                        {
                            if (pathfindToLocation(targetPos)) { state = 10005; }
                            else
                            {
                                pathToTarget = new List<(int x, int y)>();
                                simplifiedPathToTarget = new List<(int x, int y)>();
                            }
                        }
                        else if (nest.digErrands.Count > 0)
                        {
                            targetPos = nest.digErrands[rand.Next(nest.digErrands.Count)];
                            if (pathfindToLocation(targetPos)) { state = 10006; }
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
                        while (Abs(realDiffX) < 0.5f && Abs(realDiffY) < 0.5f)
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
                    if (state == 10005) { maxDist = 1; }
                    else { maxDist = 1; }
                    if (manhattanDistance(targetPos, (posX, posY)) <= maxDist)
                    {
                        if (state == 10009)
                        {
                            if (targetEntity is null) { state = 1; }
                            if (manhattanDistance((targetEntity.posX, targetEntity.posY), (posX, posY)) > maxDist)
                            {
                                targetPos = (targetEntity.posX, targetEntity.posY);
                                if (!pathfindToLocation(targetPos))
                                {
                                    targetEntity = null;
                                    pathToTarget = new List<(int x, int y)>();
                                    simplifiedPathToTarget = new List<(int x, int y)>();
                                    state = 0;
                                }
                                return;
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
                        if (Abs(realDiffX) < 0.25f) { speedX = Sign(speedX) * Max(Abs(speedX) - 0.55f, 0); }
                        else if (speedX * speedX > Abs(realDiffX) && Sign(speedX) == Sign(realDiffX)) { speedX -= Sign(realDiffX) * 0.35f; }
                        else { speedX += Sign(realDiffX) * 0.335f; }
                        if (Abs(realDiffY) < 0.25f) { speedY = Sign(speedY) * Max(Abs(speedY) - 0.55f, 0); }
                        else if (speedY * speedY > Abs(realDiffY) && Sign(speedY) == Sign(realDiffY)) { speedY -= Sign(realDiffY) * 0.35f; }
                        else { speedY += Sign(realDiffY) * 0.335f; }

                        if (rand.Next(2500) == 0) { state = 0; }
                    }
                }
                else if (state == 5 || state == 6) // preparing to dig plant OR terrain
                {
                    if (currentAttack is null)
                    {
                        if (timeAtLastDig + 1 < timeElapsed)
                        {
                            currentAttack = new Attack(screen, this, (3, 1, 0, 5), targetPos, (0, 0)); // Mandible attack
                            state *= 10;
                        }
                    }
                    else { state *= 10; }
                }
                else if (state == 50)   // After plant dig
                {
                    if (currentAttack is null || currentAttack.isDone)
                    {
                        tryManufactureElement();
                        state = 0;
                    }
                }
                else if (state == 60)   // After terrain dig
                {
                    if (currentAttack is null || nest is null) { state = 0; }
                    else if (currentAttack.isDone)
                    {
                        if (currentAttack.dugTile.type >= 0) { nest.digErrands.Remove(targetPos); }
                        if (currentAttack.dugTile.type == -5 && nest.hungryLarvae.Count > 0)
                        {
                            targetEntity = nest.hungryLarvae[rand.Next(nest.hungryLarvae.Count)];
                            targetPos = (targetEntity.posX, targetEntity.posY);

                            if (pathfindToLocation(targetPos)) { state = 10009; }
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
                }
                else if (state == 7) // preparing to place honeyy
                {
                    if (timeAtLastPlace + 1 < timeElapsed)
                    {
                        Place(targetPos, (-5, 0, 0), false);
                        if (nest != null)
                        {
                            Room room = nest.rooms[nest.getRoomId(targetPos)];
                            if (room.type == 2)
                            {
                                room.testFullness();
                                if (room.isFull) { nest.updateDropPositions(); }
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
                            if (room.type == 3 && Place(targetPos, (3, 0, 1), true))
                            {
                                Entity kiddo = screen.entitesToAdd.Values.ToArray()[screen.entitesToAdd.Count - 1];
                                kiddo.nest = nest;
                                room.assignedEntities.Add(kiddo);  // since it's the last to be added, add last entity in entity list to the room
                                nest.larvae.Add(kiddo);
                                nest.eggsToLay--;
                                room.testFullness();
                                if (room.isFull) { nest.updateDropPositions(); }
                            }
                            state = 0;
                        }
                    }
                    else { state = 0; }
                }
                else if (state == 9) // feeding kiddo !
                {
                    if (nest != null && nest.hungryLarvae.Contains(targetEntity)) { tryFeedTargetEntity(); }
                    state = 0;
                }
                else { state = 0; }
            }
            public void moveEntity()
            {
                (TileTraits entityPos, TileTraits under) returnType = applyForces();    // sets onGround and inWater
                TileTraits entityTile = returnType.entityPos;
                TileTraits tileUnder = returnType.under;

                float oldSpeedY = speedY;
                if (type.type == 3) // hornet moving, as they need to pathfind and shit
                {
                    moveHornet();
                }


                if (isCurrentlyClimbing)
                {
                    if (traits.onPlantBehavior != 1 || !screen.climbablePositions.Contains((posX, posY))) { isCurrentlyClimbing = false; }
                    else
                    {
                        changeSpeedRandom(0.2f);
                        clampSpeed(1, 1);
                        goto AfterBehaviorMovement;
                    }
                }
                else if (traits.onPlantBehavior == 1 && screen.climbablePositions.Contains((posX, posY))) { isCurrentlyClimbing = true; speedX = 0; speedY = 0; }

                if (inGround)
                {
                    if (traits.inGroundBehavior == 2)   // For worms, don't care about tile under always do the same thing
                    {
                        changeSpeedRandom(0.05f);
                        clampSpeed(0.2f, 0.2f);

                        if (entityTile.isAir) { changeSpeedRandom(0.5f); }
                        else if (entityTile.isLiquid)
                        {
                            if (type.subType == 1)
                            {
                                changeSpeedRandom(0.1f);
                                clampSpeed(1, 1);
                            }
                            else { changeSpeedRandom(0.5f); }
                        }
                    }
                    else if (onGround) { jumpRandom(1, 1); }    // For non worms, if stuck, try jumping out, so if stuck in a tile it can escape if next to air
                    else { speedY -= 1; }   // fall under if tile under is free (might have to change that depending on gravity)
                }
                else if (inWater)
                {
                    if (traits.inWaterBehavior == 1)    // drift upwards
                    {
                        changeSpeedRandom(0.5f); // worm ???????? and others ????????
                    }
                    else if (traits.inWaterBehavior == 2)
                    {
                        changeSpeedRandom(traits.swimSpeed);
                        clampSpeed(traits.swimMaxSpeed, traits.swimMaxSpeed);
                    }
                }
                else // if (inAir)
                {
                    if (traits.inAirBehavior == 1)  // if flying
                    {
                        if (type.type != 3) { changeSpeedRandom(0.5f); }
                    }
                    else if (onGround)
                    {
                        if (tileUnder.isSlippery) { ariGeoSlowDownX(0.96f, 0.04f); }
                        else { ariGeoSlowDownX(0.8f, 0.1f); }
                        if ((float)rand.NextDouble() <= traits.jumpChance) { jumpRandom(traits.jumpStrength.x, traits.jumpStrength.y); }
                    }
                    else if (onWater)
                    {
                        ariGeoSlowDownX(0.9f, 0.1f);
                        if (traits.isJesus && (float)rand.NextDouble() <= traits.jumpChance) { jumpRandom(5, 0); }
                    }
                }
            AfterBehaviorMovement:;

                // inWater   -> 0: nothing, 1: float upwards, 2: move randomly in water
                // onWater   -> 0: nothing, 1: skip, 2: drift towards land                   
                // inAir     -> 0: nothing, 1: fly randomly, 2: random jump ?
                // onGround  -> 0: nothing, 1: random jump, 2: move around, 3: dig down
                // inGround  -> 0: nothing, 1: random jump, 2: dig around, 3: teleport, 4: dig tile                   
                // onPlant   -> 0: fallOut, 1: random movement

                actuallyMoveTheEntity(isCurrentlyClimbing);

                testTileEffects(entityTile);  // test what happens if in special liquids (fairy lake, lava...)

                if (currentAttack != null && currentAttack.isDone) { currentAttack = null; }

                if (traits.wingTraits != null && (!onGround || (traits.wingTraits.Value.type == 1))/*oldSpeedY + 0.001f <= speedY*/) { wingTimer += 0.02f; }
            }
            public void testTileEffects(TileTraits tile)
            {
                if (tile.isTransformant && type.type != 0 && tile.type == (-3, 0))
                {
                    if (rand.Next(10) == 0)
                    {
                        if (type.type == 2 && type.subType == 1) { transformEntity((0, 3), true); }
                        else { transformEntity((0, 0), true); }
                    }
                }
                if (type.type == 2 && tile.isAcidic)
                {
                    if (rand.Next(10) == 0) { transformEntity((2, 1), true); }
                }
                if (tile.isLava && type.type != 2 && type != (0, 3))
                {
                    if (rand.Next(10) == 0) { screen.entitesToRemove[id] = this; }
                }
                if (tile.isAcidic && type.type != 2 && type != (0, 3) && type != (4, 1) && type != (1, 1) && type != (1, 2))
                {
                    if (rand.Next(10) == 0) { screen.entitesToRemove[id] = this; }
                }
            }
            public void transformEntity((int type, int subType) newType, bool setHp = true)
            {
                type = newType;
                traits = entityTraitsDict.ContainsKey(type) ? entityTraitsDict[type] : entityTraitsDict[(-1, 0)];

                if (setHp) { hp = traits.startingHp; }
                findLength();
                color = findColor();
                findLightColor();
            }
            public void dieAndDrop(Entity entityToGive)
            {
                ((int type, int subType, int megaType) element, int count) entityDrop = traits.drops;
                if (length > 0) { entityDrop = (entityDrop.element, length); }
                entityToGive.addElementToInventory(entityDrop.element, entityDrop.count);
                screen.entitesToRemove[id] = this;
            }
            public void findLength()
            {
                if (traits.length is null) { length = 0; pastPositions = new List<(int x, int y)>(); }
                else
                {
                    length = traits.length.Value.baseLength + seed % (traits.length.Value.variation + 1);
                    while (pastPositions.Count > length) { pastPositions.RemoveAt(pastPositions.Count - 1); }
                }
            }
            public void updatePastPositions((int x, int y) posToAdd)    // if lenght DECREASES it will not work anymore probably idk
            {
                if (pastPositions.Count == 0 || pastPositions[0].x == posX || pastPositions[0].y == posY || manhattanDistance(pastPositions[0], (posX, posY)) > 2) // if pos-2 and current pos have no x and y in common, means there are diag and pos - 1 should not be added
                {
                    pastPositions.Insert(0, posToAdd);
                    while (pastPositions.Count >= length) { pastPositions.RemoveAt(pastPositions.Count - 1); }
                }
            }
            public void applyTailRigidity(float rigidity)
            {
                int diff;
                int yVariationAllowed;
                (int x, int y) pos;
                for(int i = 0; i < pastPositions.Count; i++)
                {
                    pos = pastPositions[i];
                    yVariationAllowed = (int)((i + 1)/ rigidity); // i+ 1 because First position of pastPositions actually is tail's second position (since the first one is entity.pos)
                    diff = posY - pos.y; 
                    if (Abs(diff) > yVariationAllowed) { pastPositions[i] = (pos.x, posY - yVariationAllowed * Sign(diff)); }
                }
            }
            public bool actuallyMoveTheEntity(bool climbing = false)
            {
                TileTraits tile;
                (int x, int y) previousPos = (posX, posY);
                (int x, int y) posToTest;
                float realPosToTest;

                float toMoveX = speedX;
                float toMoveY = speedY;

                float xRatio = toMoveX / (Abs(toMoveY) + 0.0000001f);
                float yRatio = toMoveY / (Abs(toMoveX) + 0.0000001f);
                float currentSide = 0; // + go horizontal, - go vertical
                float diff;

                bool allowX = true;
                bool allowY = true;
                bool isDoneX = false;
                bool isDoneY = false;
                bool hasIntMovedX = false;
                bool hasIntMovedY = false;

                while (true)
                {
                    if (allowX && !isDoneX && (isDoneY || !allowY || currentSide >= 0))
                    {
                        diff = Clamp(-1, toMoveX, 1);
                        realPosToTest = realPosX + diff;
                        posToTest = ((int)Floor(realPosToTest, 1), posY);

                        if (posX == posToTest.x) // if movement is too small to move by one whole pixel, update realPosY, and stop
                        {
                            realPosX = realPosToTest;
                            isDoneX = true;
                            continue;
                        }

                        Chunk chunk = screen.getChunkFromPixelPos(posToTest, false, true);
                        if (chunk is null) { goto SaveEntity; }
                        tile = screen.getTileContent(posToTest);
                        if ((!tile.isSolid || traits.isDigging) && (!climbing || screen.climbablePositions.Contains(posToTest))) // if a worm or the material is not a solid tile, update positions and continue
                        {
                            hasIntMovedX = true;
                            realPosX = realPosToTest;
                            posX = posToTest.x;
                            toMoveX -= diff;
                            if (traits.length != null) { updatePastPositions(previousPos); } // to make worm's tails
                            previousPos = (posX, posY);
                            currentSide -= yRatio;
                            allowX = true;
                            allowY = true;
                        }
                        else { allowX = false; }
                    }

                    else if (allowY && !isDoneY)
                    {
                        diff = Clamp(-1, toMoveY, 1);
                        realPosToTest = realPosY + diff;
                        posToTest = (posX, (int)Floor(realPosToTest, 1));

                        if (posY == posToTest.y) // if movement is too small to move by one whole pixel, update realPosY, and stop
                        {
                            realPosY = realPosToTest;
                            isDoneY = true;
                            continue;
                        }

                        Chunk chunk = screen.getChunkFromPixelPos(posToTest, false, true);
                        if (chunk is null) { goto SaveEntity; }
                        tile = screen.getTileContent(posToTest);
                        if ((!tile.isSolid || traits.isDigging) && (!climbing || screen.climbablePositions.Contains(posToTest))) // if a worm or the material is not a solid tile, update positions and continue
                        {
                            hasIntMovedY = true;
                            realPosY = realPosToTest;
                            posY = posToTest.y;
                            toMoveY -= diff;
                            if (traits.length != null) { updatePastPositions(previousPos); } // to make worm's tails
                            previousPos = (posX, posY);
                            currentSide += xRatio;
                            allowX = true;
                            allowY = true;
                        }
                        else { allowY = false; }
                    }

                    if ((allowX && !isDoneX) || (allowY && !isDoneY)) { continue; }
                    break;
                }
                if (!isDoneX) { speedX = 0; }
                if (!isDoneY) { speedY = 0; }
                if (hasIntMovedX || hasIntMovedY)
                {
                    if (traits.tailAxisRigidity != null) { applyTailRigidity(traits.tailAxisRigidity.Value); }
                    return true;
                }
                return false;

            SaveEntity:;
                entityExitingChunk(posToTest);
                return true;
            }
            public void setEntityPos((int x, int y) posToSet)
            {
                posX = posToSet.x;
                realPosX = posToSet.x + 0.5f;
                posY = posToSet.y;
                realPosY = posToSet.y + 0.5f;
            }
            public virtual void entityExitingChunk((int x, int y) posToTest)
            {
                posX = posToTest.x;
                posY = posToTest.y;
                saveEntity(this);
                screen.entitesToRemove[id] = this;
            }
            public virtual void teleport((int x, int y) newPos, int screenIdToTeleport)
            {
                Screens.Screen screenToTeleportTo = screen.game.getScreen(screenIdToTeleport);
                screen.entitesToRemove[id] = this;
                screenToTeleportTo.activeEntities[id] = this;
                screen = screenToTeleportTo;
                setEntityPos(newPos);
                speedX = 0;
                speedY = 0;
                timeAtLastTeleportation = timeElapsed;
            }
            public virtual void initializeInventory()
            {
                inventoryQuantities = new Dictionary<(int index, int subType, int typeOfElement), int> { };
                inventoryElements = new List<(int index, int subType, int typeOfElement)> { };
            }
            public void addElementToInventory((int index, int subType, int typeOfElement) elementToAdd, int quantityToAdd = 1)
            {
                if (!inventoryQuantities.ContainsKey(elementToAdd))
                {
                    inventoryQuantities.Add(elementToAdd, quantityToAdd);
                    inventoryElements.Add(elementToAdd);
                    return;
                }
                if (inventoryQuantities[elementToAdd] != -999) { inventoryQuantities[elementToAdd] += quantityToAdd; }
                return;
            }
            public virtual void removeElementFromInventory((int index, int subType, int typeOfElement) elementToRemove, int quantityToRemove = 1)
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
            public bool PlantDig((int x, int y) posToDig, (int type, int subType, int typeOfElement)? currentItem = null, Chunk chunk = null, (int type, int subType)? targetMaterialNullable = null, bool toolRestrictions = true)
            {
                (int x, int y) targetMaterial = targetMaterialNullable ?? (0, 0);
                MaterialTraits traits = getMaterialTraits(targetMaterial);
                if (toolRestrictions && targetMaterialNullable != null && traits.toolGatheringRequirement != null && traits.toolGatheringRequirement != currentItem) { return false; }   // Don't bother digging if trying to dig a particular tile without having the correct tool equipped. Should rarely happen but whatever
                (int type, int subType) value;
                foreach (Plant plant in chunk is null ? screen.activePlants.Values : chunk.plants.Values)
                {
                    value = plant.tryDig(posToDig, currentItem, targetMaterialNullable, toolRestrictions);
                    if (value == (0, 0)) { continue; }
                    addElementToInventory((value.type, value.subType, 3));
                    timeAtLastDig = timeElapsed;
                    return true;
                }
                return false;
            }
            public ((int type, int subType) tileDug, bool success) TerrainDig((int x, int y) posToDig)
            {
                (int, int) chunkPos = ChunkIdx(posToDig);
                if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest)) { return ((0, 0), false); }
                (int x, int y) tileIndex = PosMod(posToDig);
                (int type, int subType) tileContent = chunkToTest.fillStates[tileIndex.x, tileIndex.y].type;
                if (tileContent.type != 0)
                {
                    addElementToInventory((tileContent.type, tileContent.subType, 0));
                    chunkToTest.tileModification(posToDig.x, posToDig.y, (0, 0));
                    timeAtLastDig = timeElapsed;
                    return (tileContent, true);
                }
                return (tileContent, false);
            }
            public bool Place((int x, int y) posToDig, (int type, int subType, int typeOfElement) elementToPlace, bool forcePlace)
            {
                (int, int) chunkPos = ChunkIdx(posToDig);
                if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest)) { return false; }
                if (!forcePlace && !inventoryElements.Contains(elementToPlace)) { return false; } 
                (int x, int y) tileIndex = PosMod(posToDig);
                TileTraits traits = chunkToTest.fillStates[tileIndex.x, tileIndex.y];
                if (traits.isAir || traits.isLiquid && elementToPlace.typeOfElement > 0)
                {
                    if (elementToPlace.typeOfElement == 0)
                    {
                        chunkToTest.screen.setTileContent(posToDig, (elementToPlace.type, elementToPlace.subType));
                        timeAtLastPlace = timeElapsed;
                    }
                    else if (elementToPlace.typeOfElement == 1)
                    {
                        Entity newEntity = new Entity(screen, posToDig, (elementToPlace.type, elementToPlace.subType));
                        screen.entitesToAdd[newEntity.id] = newEntity;
                        timeAtLastPlace = timeElapsed;
                    }
                    else if (elementToPlace.typeOfElement == 2)
                    {
                        Plant newPlant = new Plant(screen, posToDig, (elementToPlace.type, elementToPlace.subType));
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
                    if (tupel == (2, 1, 3)) // if it got pollen, try to make honey
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
                if (targetEntity.type.type == 3 && targetEntity.type.subType == 1)
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
