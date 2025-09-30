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

namespace Cave
{
    public class Nests
    {
        public class Room
        {
            public bool isToBeDestroyed = false;
            public Nest nest;
            public int id;
            public int type;
            public long seed;
            public (int x, int y) position;

            public HashSet<(int x, int y)> tiles = new HashSet<(int x, int y)>();
            public HashSet<(int x, int y)> borders = new HashSet<(int x, int y)>();
            public int maxBordelLevel;
            public HashSet<(int x, int y)> dropPositions = new HashSet<(int x, int y)>();
            public bool containsDebris;

            public HashSet<Entity> assignedEntities = new HashSet<Entity>();

            public int capacity = 0;
            public int contentCount = 0;
            public bool isFull = false;
            public bool inConstruction = false;

            // type of room : 0 = mainRoom, 1 = normalRoom, 2 = honeyRoom, 3 = nursery, 10000 = corridor
            public Room(Nest nestToPut, RoomJson roomJson)
            {
                nest = nestToPut;
                id = roomJson.id;
                type = roomJson.type;
                seed = roomJson.seed;
                position = roomJson.pos;
                tiles = arrayToTileHashSet(roomJson.tiles);
                findBorders();
                findDropPositions();
                findCapacity();
                foreach (int entityId in roomJson.ent)
                {
                    if (nest.screen.activeEntities.ContainsKey(entityId)) { assignedEntities.Add(nest.screen.activeEntities[entityId]); }
                }
                testFullness();
            }
            public Room((int x, int y) posToPut, int typeToPut, int idToPut, Nest nestToPut)
            {
                nest = nestToPut;
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
            public Room((int x, int y) targetPos, int typeToPut, int idToPut, Room motherRoom, Nest nestToPut)
            {
                nest = nestToPut;
                tiles = new HashSet<(int x, int y)>();
                type = typeToPut;
                id = idToPut;
                position = targetPos;
                findSeed();

                if (type == 10000) { makeCorridorBetweenPoints(motherRoom.borders, targetPos, 100); }
                else if (type == 10001)
                {
                    ((int x, int y) pos, bool found) returnTuple = findEntrancePoint(new List<(int x, int y)>(motherRoom.borders));
                    if (returnTuple.found) { makeCorridorBetweenPoints(motherRoom.borders, returnTuple.pos, 100); }
                    else { isToBeDestroyed = true; return; }
                }
                else { isToBeDestroyed = true; return; }

                findBorders();
                findDropPositions();
                findCapacity();
            }
            public Room((int x, int y) startPos, (int x, int y) targetPos, int typeToPut, int idToPut, Nest nestToPut)
            {
                nest = nestToPut;
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
            public long findSeed()
            {
                seed = nest.seed * (LCGxNeg(position.x) + LCGyNeg(position.y)) + LCGxPos(position.x) * LCGyPos(position.y);
                return seed;
            }
            public void findBorders()
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
            public void fillTiles()
            {
                Dictionary<(int x, int y), Chunk> chunkDict = new Dictionary<(int x, int y), Chunk>();
                Chunk chunkToTest;
                (int type, int subType) typeToFill = (0, 0);

                foreach ((int x, int y) posToTest in tiles)
                {
                    chunkToTest = nest.screen.getChunkFromPixelPos(posToTest, true, false, chunkDict);
                    chunkToTest.fillStates[PosMod(posToTest.x), PosMod(posToTest.y)] = getTileTraits(typeToFill);
                    chunkToTest.modificationCount = 1;
                    chunkToTest.findTileColor(PosMod(posToTest.x), PosMod(posToTest.y));
                }

                foreach (Chunk chunk in chunkDict.Values) { saveChunk(chunk); }
            }
            public void addToQueue(List<((int x, int y) position, float cost)> queue, ((int x, int y) position, float cost) valueToAdd)
            {
                int idx = 0;
                while (idx < queue.Count)
                {
                    if (valueToAdd.position == queue[idx].position) { return; }
                    if (valueToAdd.cost < queue[idx].cost) { queue.Insert(idx, valueToAdd); return; }
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
            public float heuristic((int x, int y) posToTest, (int x, int y)[] targets, float shape)
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
            public (int x, int y)[] makeHeuristicTargets((int x, int y) centerPos, int targetDistance)
            {
                (int x, int y)[] arrayo = new (int x, int y)[seed % 4 + 5];
                long seedo = seed;
                if (type == 2)
                {
                    targetDistance = (int)(targetDistance * 0.5f);
                    for (int i = 0; i < seed % 4 + 5; i++)
                    {
                        seedo = LCGxNeg(seedo);
                        arrayo[i] = (centerPos.x + (int)(seedo % targetDistance) - (int)(0.5f * targetDistance), centerPos.y + i * 2);
                    }
                }
                else if (type == 3)
                {
                    targetDistance = (int)(targetDistance * 0.5f);
                    for (int i = 0; i < seed % 4 + 5; i++)
                    {
                        int modX = (((i % 2) * 2) - 1) * i;
                        seedo = LCGxNeg(seedo);
                        arrayo[i] = (centerPos.x + modX, centerPos.y + (int)((Abs(modX) + i) * 0.5f));
                    }
                }
                else
                {
                    for (int i = 0; i < seed % 4 + 5; i++)
                    {
                        seedo = LCGxNeg(seedo);
                        arrayo[i] = (centerPos.x + (int)(seedo % targetDistance) - (int)(0.5f * targetDistance), centerPos.y + (int)(LCGxPos(seedo) % targetDistance) - (int)(0.5f * targetDistance));
                    }
                }
                return arrayo;
            }
            public void makeBubbleRoom((int x, int y) centerPos, int forceSize)
            {
                Dictionary<(int x, int y), Chunk> chunkDict = new Dictionary<(int x, int y), Chunk>();

                int tileAmount;
                if (forceSize == 0) { tileAmount = (int)(25 + (seed % 750)); }
                else { tileAmount = forceSize; }

                int targetDistance = Sqrt((int)(tileAmount * 0.5f)) + 2;
                (int x, int y)[] heuristicTargets = makeHeuristicTargets(centerPos, targetDistance);

                HashSet<(int x, int y)> tilesToFill = new HashSet<(int x, int y)>();
                List<((int x, int y) pos, float cost)> tilesToTest = new List<((int x, int y) pos, float cost)> { (centerPos, 0) };
                (int x, int y) currentTile;
                (int x, int y) posToAdd;
                (int x, int y) posToTest;
                int repeatCounter = 0;
                while (tilesToTest.Count > 0 && repeatCounter < tileAmount)
                {
                    currentTile = tilesToTest[0].pos;
                    tilesToTest.RemoveAt(0);
                    foreach ((int x, int y) mod in neighbourArray)
                    {
                        for (int mult = 1; mult <= 2; mult++)
                        {
                            posToTest = (currentTile.x + mod.x, currentTile.y + mod.y);
                            TileTraits traits = nest.screen.getTileContent(posToTest);
                            if (!traits.isSolid || nest.tiles.Contains(posToTest)) { goto SkipToNextIteration; }
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
                Dictionary<(int x, int y), Chunk> chunkDict = new Dictionary<(int x, int y), Chunk>();
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
                Dictionary<(int x, int y), Chunk> chunkDict = new Dictionary<(int x, int y), Chunk>();

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
                Dictionary<(int x, int y), Chunk> chunkDict = new Dictionary<(int x, int y), Chunk>();

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
            public ((int x, int y) pos, bool found) findTileOfTypeInRoom(int typeToFind)
            {
                Dictionary<(int x, int y), Chunk> chunkDict = new Dictionary<(int x, int y), Chunk>();

                List<(int x, int y)> tileList = new List<(int x, int y)>(tiles);
                (int x, int y) posToTest;
                int idxToTest;
                while (tileList.Count > 0)
                {
                    idxToTest = rand.Next(tileList.Count);
                    posToTest = tileList[idxToTest];
                    tileList.RemoveAt(idxToTest);
                    TileTraits tile = nest.screen.getTileContent(posToTest);
                    if (tile.type.type == typeToFind) { return (posToTest, true); }
                }
                return ((0, 0), false);
            }
        }
        public class Nest : Structure
        {
            // stats to build nest and what it looks like iggg
            public int roomSize = 0; // bigger leads to bigger rooms NOT USED
            public int connectivity = 0; // bigger leads to less connexions (corridors) NOT USED
            public int extensivity = 0; // bigger leads to new rooms and corridors being dug out farther away from the center ig NOT USED
            public float shape = 1.41421356237f; //the only one that has an actual effect LMFAO

            public HashSet<(int x, int y)> tiles = new HashSet<(int x, int y)>();
            public HashSet<(int x, int y)> borders = new HashSet<(int x, int y)>();
            public Dictionary<int, Room> rooms = new Dictionary<int, Room>();
            public int currentRoomId = 0;
            public bool isStable = false;

            // entity management
            public int eggsToLay = 0;
            public int totalHoney = 0;
            public HashSet<int> outsideEntities = new HashSet<int>();
            public HashSet<Entity> larvae = new HashSet<Entity>();
            public HashSet<Entity> adults = new HashSet<Entity>();
            public HashSet<Entity> hungryLarvae = new HashSet<Entity>();
            public HashSet<(int x, int y)> digErrands = new HashSet<(int x, int y)>();
            public HashSet<int> availableHoneyRooms = new HashSet<int>();
            public HashSet<int> availableNurseries = new HashSet<int>();

            public Nest(Game game, NestJson nestJson)
            {
                setAllStructureJsonVariables(game, nestJson);
                if (isErasedFromTheWorld) { return; }

                foreach (RoomJson roomJson in nestJson.rooms) { rooms[roomJson.id] = new Room(this, roomJson); }

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
            public Nest(Screens.Screen screenToPut, (int x, int y) posToPut, long seedToPut)
            {
                screen = screenToPut;
                seed = seedToPut;
                type = (0, 0, 0);
                pos = posToPut;
                isDynamic = true;

                Room mainRoom = new Room(posToPut, 0, currentRoomId, this);
                if (!mainRoom.isToBeDestroyed)
                {
                    addRoom(mainRoom, true);
                    Room corridoro = new Room((0, 0), 10001, currentRoomId, mainRoom, this); // (0, 0) is placeholder cause it'll get replaced
                    if (!corridoro.isToBeDestroyed) { addRoom(corridoro, true); }
                    else { isErasedFromTheWorld = true; return; }
                }
                else { isErasedFromTheWorld = true; return; }

                for (int i = 0; i < 5; i++)
                {
                    Entity hornet = new Entity(screen, posToPut, (3, 3));
                    screen.entitesToAdd[hornet.id] = hornet;
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
                entity.nest = this;
                entity.nestId = id;
            }
            public void addRoom(Room room, bool fillTiles)
            {
                room.id = currentRoomId;
                rooms[currentRoomId] = room;
                currentRoomId++;
                if (fillTiles) { room.fillTiles(); }
                updateTiles();
            }
            public void removeRoom(Room room)
            {
                rooms.Remove(room.id);
                updateTiles();
            }
            public int getRoomId(Entity entityToTest)
            {
                foreach (Room room in rooms.Values)
                {
                    if (room.assignedEntities.Contains(entityToTest)) { return room.id; }
                }
                return -1;
            }
            public int getRoomId((int x, int y) posToGetFrom)
            {
                foreach (Room room in rooms.Values)
                {
                    if (room.tiles.Contains(posToGetFrom)) { return room.id; }
                }
                return -1;
            }
            public bool testRoomAvailability((int x, int y) centerPos, int amountToTest)
            {
                Dictionary<(int x, int y), Chunk> chunkDict = new Dictionary<(int x, int y), Chunk>();

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
            public (bool valid, (int x, int y)) findNewRoomLocation()
            {
                int repeatCounter = 2;
                int distance;
                while (repeatCounter < 100)
                {
                    distance = repeatCounter * 5;
                    float angle = (float)(rand.NextDouble() * 6.28318530718);
                    (int x, int y) targetPos = (pos.x + (int)(Math.Cos(angle) * distance), pos.y + (int)(Math.Sin(angle) * distance));

                    if (testRoomAvailability(targetPos, 45)) { return (true, targetPos); }
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
                int roomTypeToMake = findTypeOfRoomToDig();
                if (roomTypeToMake == -1) { isStable = true; return false; } //if no need for new room don't make new room lol. isStable
                Room newRoom = new Room(targetPos, roomTypeToMake, currentRoomId, this);
                if (newRoom.isToBeDestroyed) { return false; }
                (int x, int y) originPoint = getRandomItem(newRoom.borders);
                (int x, int y) targetPosCorridor = findClosestBorder(originPoint);
                addRoom(newRoom, false);

                Room corridoro = new Room(targetPosCorridor, 10000, currentRoomId, newRoom, this); //+1 is important ! else it will override lol
                if (!corridoro.isToBeDestroyed) { addRoom(corridoro, false); return true; }

                removeRoom(newRoom);
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
                (bool valid, (int x, int y) pos) returnTuple = findNewRoomLocation();
                if (returnTuple.valid)
                {
                    makeCorridorAndRoom(returnTuple.pos);
                    updateTiles();
                    updateDigErrands();
                    updateDropPositions();
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
            public void updateTiles()
            {
                chunkPresence = new HashSet<(int x, int y)>();
                megaChunkPresence = new HashSet<(int x, int y)>();
                tiles = new HashSet<(int x, int y)>();
                borders = new HashSet<(int x, int y)>();
                foreach (Room room in rooms.Values)
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
            public void setAllRoomsAsFinished()
            {
                foreach (Room room in rooms.Values) { room.inConstruction = false; }
            }
            public void updateDigErrands()
            {
                setAllRoomsAsFinished();

                Dictionary<(int x, int y), Chunk> chunkDict = new Dictionary<(int x, int y), Chunk>();

                digErrands = new HashSet<(int x, int y)>();
                foreach ((int x, int y) pos in tiles)
                {
                    TileTraits traits = screen.getTileContent(pos);
                    Room room = rooms[getRoomId(pos)];
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
                foreach (Room room in rooms.Values)
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
            public int findTypeOfRoomToDig()
            {
                int honeyCount = countTotalHoney();
                int honeyCapacity = 0;
                int larvaCapacity = 0;
                int larvaCount = 0;
                int miscTiles = 0;
                int totalEntities = larvae.Count + adults.Count;
                foreach (Room room in rooms.Values)
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
                foreach (Room room in rooms.Values)
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
            public Room getRandomRoomOfType(int typeToGet)
            {
                List<Room> roomList = rooms.Values.ToList();
                Room roomToTest;
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
