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

using static Cave.Traits;
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
using static Cave.Villages;

namespace Cave
{
    public class Nests
    {
        public class NestRoom : Room
        {
            public Nest nest;

            public int maxBordelLevel;
            public HashSet<(int x, int y)> dropPositions = new HashSet<(int x, int y)>();

            public int capacity = 0;
            public int contentCount = 0;
            public bool isFull = false;

            // type of room : 0 = mainNestRoom, 1 = normalNestRoom, 2 = honeyNestRoom, 3 = nursery, 10000 = corridor
            public NestRoom(Nest nestToPut, RoomJson roomJson)
            {
                nest = nestToPut;
                house = nestToPut;
                id = roomJson.id;
                type = roomJson.type;
                seed = roomJson.seed;
                position = roomJson.pos;
                tiles = arrayToTileHashSet(roomJson.t);
                borders = arrayToTileHashSet(roomJson.b);
                entrances = arrayToTileHashSet(roomJson.e);
                findBorders();
                findDropPositions();
                findCapacity();
                foreach (int entityId in roomJson.ent)
                {
                    if (nest.screen.activeEntities.ContainsKey(entityId)) { assignedEntities.Add(nest.screen.activeEntities[entityId]); }
                }
                testFullness();
            }
            public NestRoom((int x, int y) posToPut, int typeToPut, int idToPut, Nest nestToPut)
            {
                nest = nestToPut;
                house = nestToPut;
                tiles = new HashSet<(int x, int y)>();
                type = typeToPut;
                id = idToPut;
                position = posToPut;
                findSeed();

                if (type == 0) { makeBubbleRoom(posToPut, 0); }
                else if (type == 1) { makeBubbleRoom(posToPut, 35 + rand.Next(200)); }
                else if (type == 2) { makeBubbleRoom(posToPut, 50 + rand.Next(100)); }
                else if (type == 3) { makeBubbleRoom(posToPut, 120 + rand.Next(100)); }
                else { isToBeDestroyed = true; }

                findBorders();
                findDropPositions();
                findCapacity();
            }
            public NestRoom((int x, int y) targetPos, int typeToPut, int idToPut, NestRoom motherNestRoom, Nest nestToPut)
            {
                nest = nestToPut;
                house = nestToPut;
                tiles = new HashSet<(int x, int y)>();
                type = typeToPut;
                id = idToPut;
                position = targetPos;
                findSeed();

                if (type == 10000) { makeCorridorBetweenPoints(motherNestRoom.borders, targetPos, 100); }
                else if (type == 10001)
                {
                    ((int x, int y) pos, bool found) returnTuple = findEntrancePoint(new List<(int x, int y)>(motherNestRoom.borders));
                    if (returnTuple.found) { makeCorridorBetweenPoints(motherNestRoom.borders, returnTuple.pos, 100); }
                    else { isToBeDestroyed = true; return; }
                }
                else { isToBeDestroyed = true; return; }

                findBorders();
                findDropPositions();
                findCapacity();
            }
            public NestRoom((int x, int y) startPos, (int x, int y) targetPos, int typeToPut, int idToPut, Nest nestToPut)
            {
                nest = nestToPut;
                house = nestToPut;
                tiles = new HashSet<(int x, int y)>();
                type = typeToPut;
                id = idToPut;
                position = startPos;
                findSeed();

                if (type == 10000) { makeCorridorBetweenPoints(new HashSet<(int x, int y)> { startPos }, targetPos, 100); }
                else { isToBeDestroyed = true; }

                findBorders();
                findDropPositions();
                findCapacity();
            }
            public override void findBorders()
            {
                List<(int x, int y)> tilesToTest = new List<(int x, int y)>(tiles);
                HashSet<(int x, int y)> bordersDict = new HashSet<(int x, int y)>();
                (int x, int y) tileToTest;
                (int x, int y) currentTile;
                while (tilesToTest.Count > 0)
                {
                    currentTile = tilesToTest[0];
                    tilesToTest.RemoveAt(0);

                    foreach ((int x, int y) mod in neighbourArray)
                    {
                        tileToTest = (currentTile.x + mod.x, currentTile.y + mod.y);
                        if (!tiles.Contains(tileToTest)) { bordersDict.Add(tileToTest); }
                    }
                }
                borders = bordersDict;
                if (type == 2 || type == 3)
                {
                    maxBordelLevel = getBound(borders, true, true) - (type == 2 ? 2 : 6);
                    HashSet<(int x, int y)> newBorders = new HashSet<(int x, int y)>();
                    foreach ((int x, int y) pos in borders) { if (pos.y >= maxBordelLevel) { newBorders.Add(pos); } }
                    borders = newBorders;
                }
            }
            public override void fillTiles()
            {
                nest.updateTiles();
                HashSet<Chunk> chunkSet = new HashSet<Chunk>();
                Chunk chunkToTest;

                TileTraits typeToFill = getTileTraits((0, 0));

                foreach ((int x, int y) chunkPos in nest.chunkPresence)
                {
                    chunkToTest = nest.screen.getChunkFromChunkPos(chunkPos, true, false);
                    if (chunkToTest.nonReplacableTiles is null && chunkToTest.maturity < 2) { chunkToTest.nonReplacableTiles = new HashSet<(int x, int y)>(); }
                }

                foreach ((int x, int y) posToTest in tiles)
                {
                    chunkToTest = nest.screen.getChunkFromPixelPos(posToTest, true, false);
                    chunkSet.Add(chunkToTest);
                    chunkToTest.fillStates[PosMod(posToTest.x), PosMod(posToTest.y)] = typeToFill;
                    if (chunkToTest.nonReplacableTiles != null) { chunkToTest.nonReplacableTiles.Add((PosMod(posToTest.x), PosMod(posToTest.y))); }
                    chunkToTest.modificationCount = 1;
                    chunkToTest.findTileColor(PosMod(posToTest.x), PosMod(posToTest.y));
                }

                foreach (Chunk chunk in chunkSet) { saveChunk(chunk); }
            }
            public override void makeBubbleRoom((int x, int y) centerPos, int forceSize, bool minFloor = false, bool failIfNeighborsAir = true, bool failIfLiquid = true, bool failIfSolid = false)
            {
                int tileAmount;
                if (forceSize == 0) { tileAmount = (int)(25 + (seed % 750)); }
                else { tileAmount = forceSize; }

                int targetDistance = (int)Math.Sqrt(tileAmount * 0.5f) + 2;
                (int x, int y)[] heuristicTargets = makeHeuristicTargets(centerPos, targetDistance);

                HashSet<(int x, int y)> tilesToFill = new HashSet<(int x, int y)>();
                List<((int x, int y) pos, float cost)> tilesToTest = new List<((int x, int y) pos, float cost)> { (centerPos, 0) };
                (int x, int y) currentTile;
                (int x, int y) posToAdd;
                (int x, int y) posToTest;
                int repeatCounter = 0;
                TileTraits traits;
                while (tilesToTest.Count > 0 && repeatCounter < tileAmount)
                {
                    currentTile = tilesToTest[0].pos;
                    tilesToTest.RemoveAt(0);

                    traits = nest.screen.getTileContent(currentTile);
                    if (((minFloor && currentTile.y < centerPos.y) || traits.isLiquid && failIfLiquid) || (traits.isSolid && failIfSolid)) { goto SkipToNextIteration; }
                    if (failIfNeighborsAir)
                    {
                        foreach ((int x, int y) mod in neighbourArray)
                        {
                            for (int mult = 1; mult <= 2; mult++)
                            {
                                posToTest = (currentTile.x + mod.x, currentTile.y + mod.y);
                                traits = nest.screen.getTileContent(posToTest);
                                if (traits.isAir) { goto SkipToNextIteration; }
                            }
                        }
                    }

                    tilesToFill.Add(currentTile);
                    foreach ((int x, int y) mod in neighbourArray)
                    {
                        posToAdd = (currentTile.x + mod.x, currentTile.y + mod.y);
                        if (!tilesToFill.Contains(posToAdd) && (!nest.tiles.Contains(posToAdd)))
                        {
                            addToQueue(tilesToTest, (posToAdd, repeatCounter + 20 * heuristic(posToAdd, heuristicTargets, nest.shape)));
                        }
                    }

                SkipToNextIteration:;
                    repeatCounter++;
                }

                tiles = tilesToFill;
                if (tiles.Count == 0)
                {
                    isToBeDestroyed = true;
                    return;
                }
            }
            public ((int x, int y) pos, bool found) findEntrancePoint(List<(int x, int y)> tilesToTest)
            {
                HashSet<(int x, int y)> visitedTiles = new HashSet<(int x, int y)>();

                foreach ((int x, int y) pos in tilesToTest) { visitedTiles.Add(pos); }

                (int x, int y) posToTest;
                (int x, int y) posToAdd;
                int repeatCounter = 0;
                while (tilesToTest.Count > 0 && repeatCounter < 5000000)
                {
                    posToTest = tilesToTest[0];
                    tilesToTest.RemoveAt(0);
                    visitedTiles.Add(posToTest);
                    if (nest.tiles.Contains(posToTest)) { goto SkipToNextIteration; }

                    TileTraits tile = nest.screen.getTileContent(posToTest);
                    if (tile.isAir)
                    {
                        if (tile.type.type != -4) { return (posToTest, true); }
                        goto SkipToNextIteration;
                    }

                    foreach ((int x, int y) mod in neighbourArray)
                    {
                        posToAdd = (posToTest.x + mod.x, posToTest.y + mod.y);
                        if (!visitedTiles.Contains(posToAdd))
                        {
                            tilesToTest.Add(posToAdd);
                            visitedTiles.Add(posToAdd);
                        }
                    }

                SkipToNextIteration:;
                    repeatCounter++;
                }
                return ((0, 0), false);
            }
            public void makeCorridorBetweenPoints(HashSet<(int x, int y)> startPosList, (int x, int y) targetPos, int randomness)
            {
                long seedo = seed;

                HashSet<(int x, int y)> tilesToFill = new HashSet<(int x, int y)>();
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
                    if (currentTile == targetPos) { break; }

                    tilesToTest.RemoveAt(0);
                    if (manhattanDistance(currentTile, targetPos) > 1)
                    {
                        foreach ((int x, int y) mod in neighbourArray)
                        {
                            posToTest = (currentTile.x + mod.x, currentTile.y + mod.y);
                            TileTraits tile = nest.screen.getTileContent(posToTest);
                            if ((!tile.isSolid || nest.tiles.Contains(posToTest)) && repeatCounter >= startPosList.Count) { goto SkipToNextIteration; }  // cumsum yummy yum    I have no idea why i wrote this LMAO
                        }
                    }
                    foreach ((int x, int y) mod in neighbourArray)
                    {
                        posToTest = (currentTile.x + mod.x, currentTile.y + mod.y);
                        seedo = LCGz(Abs(LCGyPos(posToTest.x)) + Abs(LCGyNeg(posToTest.y)) + seedo);
                        if ((!originDict.ContainsKey(posToTest) && !nest.tiles.Contains(posToTest)) || posToTest == targetPos)
                        {
                            addToQueue(tilesToTest, (posToTest, seedo % randomness + 20 * heuristic(posToTest, targetPos, 1.41421356237f)));
                            originDict[posToTest] = currentTile;
                        }
                    }

                SkipToNextIteration:;
                    repeatCounter++;
                }
                if (repeatCounter >= tileAmount || tilesToTest.Count == 0) { isToBeDestroyed = true; return; }
                
                currentTile = targetPos;
                tilesToFill.Add(targetPos);
                while (!startPosList.Contains(currentTile))
                {
                    currentTile = originDict[currentTile];
                    tilesToFill.Add(currentTile);
                }

                tiles = tilesToFill;
            }
            public void findDropPositions()
            {
                foreach ((int x, int y) pos in tiles) { if (pos.y == maxBordelLevel - 1) { dropPositions.Add(pos); } }
            }
            public void testFullness()
            {
                countContent();
                if (type == 2 || type == 3) { isFull = contentCount >= capacity; }
            }
            public void findCapacity()
            {
                capacity = 0;

                if (type >= 10000) { capacity = 0; } // corridors
                else if (type <= 1) { capacity = tiles.Count; } // empty rooms
                else if (type == 2) // honey rooms
                {
                    foreach ((int x, int y) posToTest in tiles)
                    {
                        if (posToTest.y < maxBordelLevel) { capacity++; }
                    }
                    isFull = true;
                    return;
                }
                else if (type == 3) { capacity = (int)(tiles.Count * 0.1f); }   // nurseries 
            }
            public void countContent()
            {
                contentCount = 0;

                // capacity of list of tiles, count, and shit, idkkkkk broooo

                if (type == 2)
                {
                    foreach ((int x, int y) posToTest in tiles)
                    {
                        TileTraits tile = nest.screen.getTileContent(posToTest);
                        if (posToTest.y < maxBordelLevel)
                        {
                            if (tile.type.type == -5) { contentCount++; }
                            else if (!tile.isAir) { containsDebris = true; }
                        }
                    }
                }
                else if (type == 3) { contentCount = assignedEntities.Count; }
            }
        }
        public class Nest : House
        {
            // stats to build nest and what it looks like iggg
            public int roomSize = 0; // bigger leads to bigger rooms NOT USED
            public int connectivity = 0; // bigger leads to less connexions (corridors) NOT USED
            public int extensivity = 0; // bigger leads to new rooms and corridors being dug out farther away from the center ig NOT USED
            public float shape = 1.41421356237f; //the only one that has an actual effect LMFAO

            public new Dictionary<int, NestRoom> rooms = new Dictionary<int, NestRoom>();
            public bool isStable = false;

            // entity management
            public int eggsToLay = 0;
            public int totalHoney = 0;
            public HashSet<Entity> larvae = new HashSet<Entity>();
            public HashSet<Entity> adults = new HashSet<Entity>();
            public HashSet<Entity> hungryLarvae = new HashSet<Entity>();
            public HashSet<(int x, int y)> digErrands = new HashSet<(int x, int y)>();
            public HashSet<int> availableHoneyRooms = new HashSet<int>();
            public HashSet<int> availableNurseries = new HashSet<int>();

            public Nest(Game game, NestJson nestJson)
            {
                setAllStructureJsonVariables(game, nestJson);
                applyTraitsToStructure();
                if (isErasedFromTheWorld) { return; }

                foreach (RoomJson roomJson in nestJson.rooms) { rooms[roomJson.id] = new NestRoom(this, roomJson); }
                currentRoomId = nestJson.roomId;

                foreach (int entityId in nestJson.ent)
                {
                    if (screen.activeEntities.ContainsKey(entityId)) { addEntityToStructure(screen.activeEntities[entityId]); }
                    else { outsideEntities.Add(id); }
                }
                updateTiles();
                updateDropPositions();
                updateDigErrands();
                decideForBabies();

                addStructureToTheRightDictInTheScreen();
            }
            public Nest(Screens.Screen screenToPut, (int x, int y) posToPut, long seedToPut, (int type, int subType, int subSubType) typeToPut)
            {
                screen = screenToPut;
                seed = seedToPut;
                pos = posToPut;
                type = typeToPut;
                traits = getStructureTraits(type);
                applyTraitsToStructure();

                NestRoom mainNestRoom = new NestRoom(posToPut, 0, currentRoomId, this);
                if (!mainNestRoom.isToBeDestroyed)
                {
                    addRoom(mainNestRoom, true);
                    NestRoom corridoro = new NestRoom((0, 0), 10001, currentRoomId, mainNestRoom, this); // (0, 0) is placeholder cause it'll get replaced
                    if (!corridoro.isToBeDestroyed) { addRoom(corridoro, true); }
                    else { isErasedFromTheWorld = true; return; }
                }
                else { isErasedFromTheWorld = true; return; }

                for (int i = 0; i < 5; i++)
                {
                    Entity hornet = new Entity(screen, posToPut, (3, 3));
                    screen.entitiesToAdd[hornet.id] = hornet;
                    addEntityToStructure(hornet);
                }
                updateTiles();
                updateDropPositions();
                updateDigErrands();
                decideForBabies();

                id = currentStructureId;
                currentStructureId++;
                saveStructure();
                addToMegaChunks();
                addStructureToTheRightDictInTheScreen();
            }
            public override void saveStructure()
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new JavaScriptDateTimeConverter());
                serializer.NullValueHandling = NullValueHandling.Ignore;

                NestJson nestJson = new NestJson(this);

                using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\CaveData\\{screen.game.seed}\\StructureData\\{id}.json"))
                using (JsonWriter writer = new JsonTextWriter(sw)) { serializer.Serialize(writer, nestJson); }
            }
            public override int setClassTypeInJson() { return 1; }
            public override void addEntityToStructure(Entity entity)
            {
                if (entity.type.type == 3) { adults.Add(entity); }
                else { larvae.Add(entity); }
                if (outsideEntities.Contains(entity.id)) { outsideEntities.Remove(entity.id); }
                setNestAsEntitysNest(entity);
            }
            public override Nest getItselfAsNest() { return this; } // not perfect but ehhhhh... for debug
            public void setNestAsEntitysNest(Entity entity)
            {
                entity.home = this;
                entity.homeId = id;
            }
            public override void removeEntity(Entity entity)
            {
                assignedEntities.Remove(entity);
                adults.Remove(entity);
                larvae.Remove(entity);
                outsideEntities.Remove(entity.id);
            }
            public void addRoom(NestRoom room, bool fillTiles)
            {
                room.id = currentRoomId;
                rooms[currentRoomId] = room;
                currentRoomId++;
                if (fillTiles) { room.fillTiles(); }
                updateTiles();
            }
            public void removeRoom(NestRoom room)
            {
                rooms.Remove(room.id);
                updateTiles();
            }
            public int getRoomId(Entity entityToTest)
            {
                foreach (NestRoom room in rooms.Values)
                {
                    if (room.assignedEntities.Contains(entityToTest)) { return room.id; }
                }
                return -1;
            }
            public int getRoomId((int x, int y) posToGetFrom)
            {
                foreach (NestRoom room in rooms.Values)
                {
                    if (room.tiles.Contains(posToGetFrom)) { return room.id; }
                }
                return -1;
            }
            public bool testNestRoomAvailability((int x, int y) centerPos, int amountToTest)
            {
                (int x, int y) currentPos;
                List<(int x, int y)> tilesToTest = new List<(int x, int y)> { centerPos };
                (int x, int y) posToAdd;

                int repeatCounter = 0;
                while (repeatCounter < amountToTest)
                {
                    currentPos = tilesToTest[repeatCounter];
                    TileTraits tile = screen.getTileContent(currentPos);
                    if (!tile.isSolid || tiles.Contains(currentPos)) { return false; }

                    foreach ((int x, int y) mod in neighbourArray)
                    {
                        posToAdd = (currentPos.x + 2 * mod.x, currentPos.y + 2 * mod.y);
                        if (!tilesToTest.Contains(posToAdd)) { tilesToTest.Add(posToAdd); }
                    }

                    if (repeatCounter % 2 == 0) // does not do what I want but whatever
                    {
                        foreach ((int x, int y) mod in diagArray)
                        {
                            posToAdd = (currentPos.x + 2 * mod.x, currentPos.y + 2 * mod.y);
                            if (!tilesToTest.Contains(posToAdd)) { tilesToTest.Add(posToAdd); }
                        }
                    }
                    repeatCounter++;
                }
                return true;
            }
            public (bool valid, (int x, int y)) findNewNestRoomLocation()
            {
                int repeatCounter = 2;
                int distance;
                while (repeatCounter < 100)
                {
                    distance = repeatCounter * 5;
                    float angle = (float)(rand.NextDouble() * 6.28318530718);
                    (int x, int y) targetPos = (pos.x + (int)(Math.Cos(angle) * distance), pos.y + (int)(Math.Sin(angle) * distance));

                    if (testNestRoomAvailability(targetPos, 45)) { return (true, targetPos); }
                    repeatCounter++;
                }
                return (false, (0, 0)); // CANNOT MAKE NOW ROOM ON 0, 0 LOL
            }
            public void forkRandomCorridorToRandomRoom()
            {
                int repeatCounter = 0;
                NestRoom corridor;
                NestRoom room;
                while (repeatCounter < 100)
                {
                    corridor = rooms[rand.Next(rooms.Count)];
                    if (corridor.type < 10000) { repeatCounter++; continue; }

                    room = rooms[rand.Next(rooms.Count)];
                    if (room.type >= 10000) { repeatCounter++; continue; }

                    corridor.makeCorridorBetweenPoints(corridor.tiles, getRandomItem(room.borders), 100);
                    updateTiles();
                    break;
                }
            }
            public bool makeCorridorAndRoom((int x, int y) targetPos)
            {
                int roomTypeToMake = findTypeOfNestRoomToDig();
                if (roomTypeToMake == -1) { isStable = true; return false; } //if no need for new room don't make new room lol. isStable
                NestRoom newNestRoom = new NestRoom(targetPos, roomTypeToMake, currentRoomId, this);
                if (newNestRoom.isToBeDestroyed) { return false; }
                (int x, int y) originPoint = getRandomItem(newNestRoom.borders);
                (int x, int y) targetPosCorridor = findClosestBorder(originPoint);
                addRoom(newNestRoom, false);

                NestRoom corridoro = new NestRoom(targetPosCorridor, 10000, currentRoomId, newNestRoom, this); //+1 is important ! else it will override lol
                if (!corridoro.isToBeDestroyed) { addRoom(corridoro, false); return true; }

                removeRoom(newNestRoom);
                return false;
            }
            public override void moveStructure()
            {
                if (rand.Next(100) == 0) { isStable = false; }
                if (!isStable && digErrands.Count == 0) { randomlyExtendNest(); }
                if (adults.Count == 0 && larvae.Count == 0 && outsideEntities.Count == 0) { screen.game.structuresToRemove.Add(id, this); }
            }
            public void randomlyExtendNest()
            {
                decideForBabies();
                (bool valid, (int x, int y) pos) returnTuple = findNewNestRoomLocation();
                if (returnTuple.valid)
                {
                    makeCorridorAndRoom(returnTuple.pos);
                    updateTiles();
                    updateDigErrands();
                    updateDropPositions();
                }
                //forkRandomCorridorToRandomNestRoom();
            }
            public (int x, int y) findClosestPoint((int x, int y) posToTest)
            {
                int diffX;
                int diffY;
                int diagNumber;
                int distance;
                int distanceMin = 1000000;
                (int x, int y) posMin = (0, 0);
                foreach ((int x, int y) pos in tiles)
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
            public (int x, int y) findClosestBorder((int x, int y) posToTest)
            {
                int diffX;
                int diffY;
                int diagNumber;
                int distance;
                int distanceMin = 1000000;
                (int x, int y) posMin = (0, 0);
                foreach ((int x, int y) pos in borders)
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
            public override void updateTiles()
            {
                chunkPresence = new HashSet<(int x, int y)>();
                megaChunkPresence = new HashSet<(int x, int y)>();
                tiles = new HashSet<(int x, int y)>();
                borders = new HashSet<(int x, int y)>();
                foreach (NestRoom room in rooms.Values)
                {
                    foreach ((int x, int y) tile in room.tiles)
                    {
                        tiles.Add(tile);
                        chunkPresence.Add(ChunkIdx(tile.x, tile.y));
                    }
                    foreach ((int x, int y) tile in room.borders)
                    {
                        borders.Add(tile);
                        chunkPresence.Add(ChunkIdx(tile.x, tile.y));
                    }
                }
                foreach ((int x, int y) poso in chunkPresence) { megaChunkPresence.Add(MegaChunkIdxFromChunkPos(poso)); }
            }
            public void setAllNestRoomsAsFinished()
            {
                foreach (NestRoom room in rooms.Values) { room.inConstruction = false; }
            }
            public void updateDigErrands()
            {
                setAllNestRoomsAsFinished();

                digErrands = new HashSet<(int x, int y)>();
                foreach ((int x, int y) pos in tiles)
                {
                    TileTraits traits = screen.getTileContent(pos);
                    NestRoom room = rooms[getRoomId(pos)];
                    if (traits.isAir || (traits.type.type == -5 && room.type == 2)) { }
                    else
                    {
                        digErrands.Add(pos);
                        room.inConstruction = true;
                    }
                }
            }
            public void updateDropPositions()
            {
                availableHoneyRooms = new HashSet<int>();
                availableNurseries = new HashSet<int>();
                updateTiles();
                foreach (NestRoom room in rooms.Values)
                {
                    if (room.type == 2)
                    {
                        room.testFullness();
                        if (!room.isFull && !room.inConstruction) { availableHoneyRooms.Add(room.id); }
                    }
                    else if (room.type == 3)
                    {
                        room.testFullness();
                        if (!room.isFull && !room.inConstruction) { availableNurseries.Add(room.id); }
                    }
                }
            }
            public int findTypeOfNestRoomToDig()
            {
                int honeyCount = countTotalHoney();
                int honeyCapacity = 0;
                int larvaCapacity = 0;
                int larvaCount = 0;
                int miscTiles = 0;
                int totalEntities = larvae.Count + adults.Count;
                foreach (NestRoom room in rooms.Values)
                {
                    if (room.type == 2) { honeyCapacity += room.capacity; }
                    else if (room.type == 3)
                    {
                        room.countContent();
                        larvaCapacity += room.capacity;
                        larvaCount += room.contentCount;
                    }
                    else { miscTiles += room.capacity; } // +0 for corridors as they got capacity = 0, +tileCount for empty rooms (type 0 and 1)
                }

                int remainingHoneys = honeyCapacity - honeyCount;
                float honeyRatio = 1;
                if (honeyCapacity > 0) { honeyRatio = (float)honeyCount / honeyCapacity; }

                int remainingLarvae = larvaCapacity - larvaCount;
                float larvaRatio = 1;
                if (larvaCapacity > 0) { larvaRatio = (float)larvaCount / larvaCapacity; }

                if (remainingHoneys <= 0) { return 2; }
                if (remainingLarvae <= 0) { return 3; }
                if (honeyRatio < 0.75f && larvaRatio < 0.75f)
                {
                    if (miscTiles <= totalEntities * 5) { return 1; }
                    return -1;
                }
                if (honeyRatio > larvaRatio) { return 2; }
                return 3;
            }
            public int countTotalHoney()
            {
                totalHoney = 0;
                foreach (NestRoom room in rooms.Values)
                {
                    if (room.type == 2)
                    {
                        room.countContent();
                        totalHoney += room.contentCount;
                    }
                }
                foreach (Entity entity in adults)
                {
                    if (entity.inventoryElements.Contains((-5, 0, 0))) { totalHoney += entity.inventoryQuantities[(-5, 0, 0)]; }
                }
                return totalHoney;
            }
            public void decideForBabies()
            {
                countTotalHoney();
                int upkeepHoneyCost = adults.Count;
                foreach (Entity kiddo in larvae)
                {
                    if (kiddo.type.subType == 0) { upkeepHoneyCost += 4; }
                    if (kiddo.type.subType == 1) { upkeepHoneyCost += 4 - kiddo.food; }
                    if (kiddo.type.subType == 2) { upkeepHoneyCost += 1; }
                }
                eggsToLay = Max(0, (int)(0.25 * (totalHoney - upkeepHoneyCost)));
            }
            public NestRoom getRandomRoomOfType(int typeToGet)
            {
                List<NestRoom> roomList = rooms.Values.ToList();
                NestRoom roomToTest;
                while (roomList.Count > 0)
                {
                    roomToTest = roomList.ElementAt(rand.Next(roomList.Count));
                    if (roomToTest.type == typeToGet) { return roomToTest; }
                }
                return null;
            }
        }
    }
}
