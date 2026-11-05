using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
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
using static Cave.Attacks;
using static Cave.Chunks;
using static Cave.Dialogues;
using static Cave.Entities;
using static Cave.Files;
using static Cave.Form1;
using static Cave.Globals;
using static Cave.MathF;
using static Cave.Nests;
using static Cave.Particles;
using static Cave.Plants;
using static Cave.Players;
using static Cave.Screens;
using static Cave.Sprites;
using static Cave.Structures;
using static Cave.Traits;
using static Cave.Villages;

namespace Cave
{
    public class Villages
    {
        public class Room
        {
            public bool isToBeDestroyed = false;
            public House house;
            public int id;
            public int type;
            public long seed;
            public (int x, int y) position;

            public HashSet<(int x, int y)> tiles = new HashSet<(int x, int y)>();
            public HashSet<(int x, int y)> borders = new HashSet<(int x, int y)>();
            public HashSet<(int x, int y)> entrances = new HashSet<(int x, int y)>();
            public bool containsDebris;

            public HashSet<Entity> assignedEntities = new HashSet<Entity>();

            public bool inConstruction = false;

            protected Room() { }    // for NestRooms
            public Room(House houseToPut, RoomJson roomJson)
            {
                house = houseToPut;
                id = roomJson.id;
                type = roomJson.type;
                seed = roomJson.seed;
                position = roomJson.pos;
                tiles = arrayToTileHashSet(roomJson.t);
                borders = arrayToTileHashSet(roomJson.b);
                entrances = arrayToTileHashSet(roomJson.e);
                findBorders();
                foreach (int entityId in roomJson.ent)
                {
                    if (house.screen.activeEntities.ContainsKey(entityId)) { assignedEntities.Add(house.screen.activeEntities[entityId]); }
                }
            }
            public Room((int x, int y) posToPut, int typeToPut, int idToPut, House houseToPut)
            {
                house = houseToPut;
                type = typeToPut;
                id = idToPut;
                position = posToPut;
                findSeed();

                if (type == 0) { makeBubbleRoom(posToPut, 125, true, false, true, false); }
                else { makeBubbleRoom(posToPut, 75, false, false, true, false); }
                smoothenTiles();
                findBorders();
                smoothenBorders();
                if (!carveEntrance(type != 0)) { isToBeDestroyed = true; return; }
                if (type == 0) { spawnEntities((0, 0), posToPut, 2); }
            }

            public void spawnEntities((int type, int subType) typeToSpawn, (int x, int y) posToPut, int amountToSpawn)
            {
                for (int i = 0; i < amountToSpawn; i++)
                {
                    Entity newEntity = new Entity(house.screen, posToPut, typeToSpawn);
                    house.screen.entitiesToAdd[newEntity.id] = newEntity;
                    house.addEntityToStructure(newEntity);
                }
            }
            public long findSeed()
            {
                seed = LCGxy((position, house.seed), house.screen.seed);
                return seed;
            }
            public void smoothenTiles()
            {
                bool modified = true;
                while (modified)
                {
                    modified = false;
                    HashSet<(int x, int y)> posToRemove = new HashSet<(int x, int y)>();
                    foreach ((int x, int y) pos in tiles)
                    {
                        int counto = 0;
                        foreach ((int x, int y) mod in neighbourArray)
                        {
                            if (tiles.Contains((pos.x + mod.x, pos.y + mod.y))) { counto++; }
                        }
                        if (counto < 2) { posToRemove.Add(pos); modified = true; }
                    }
                    foreach ((int x, int y) pos in posToRemove) { tiles.Remove(pos); }
                }
            }
            public void smoothenBorders()
            {
                bool modified = true;
                while (modified)
                {
                    modified = false;
                    HashSet<(int x, int y)> posToTurnIntoTiles = new HashSet<(int x, int y)>();
                    foreach ((int x, int y) pos in borders)
                    {
                        int tileCounto = 0;
                        int borderCounto = 0;
                        foreach ((int x, int y) mod in neighbourArray)
                        {
                            if (tiles.Contains((pos.x + mod.x, pos.y + mod.y))) { tileCounto++; }
                            else if (borders.Contains((pos.x + mod.x, pos.y + mod.y))) { borderCounto++; }
                        }
                        if (borderCounto + tileCounto == 4 || tileCounto >= 3) { posToTurnIntoTiles.Add(pos); modified = true; }
                    }
                    foreach ((int x, int y) pos in posToTurnIntoTiles) { borders.Remove(pos); tiles.Add(pos); }
                    if (modified) { findBorders(); }
                }
            }
            public virtual void findBorders()
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
            }
            public virtual void fillTiles()
            {
                Chunk chunkToTest;
                foreach ((int x, int y) chunkPos in house.chunkPresence)
                {
                    chunkToTest = house.screen.getChunkFromChunkPos(chunkPos, true, false);
                    if (chunkToTest.nonReplacableTiles is null && chunkToTest.maturity < 2) { chunkToTest.nonReplacableTiles = new HashSet<(int x, int y)>(); }
                }

                HashSet<Chunk> chunkSet = new HashSet<Chunk>();
                fillTilesHashSet(tiles, chunkSet, false, true, true);
                fillTilesHashSet(borders, chunkSet, true, true);   // Will put foreground on entrance, but entrances fills it back to air
                fillTilesHashSet(entrances, chunkSet, false, type > 0, true);    // When the entrance is the entrance to outside, don't fill foreground. Else if room to room entrance, fill foreground (it's in the house)

                foreach (Chunk chunk in chunkSet) { saveChunk(chunk); }
            }
            public void fillTilesHashSet(HashSet<(int x, int y)> setToUse, HashSet<Chunk> saveChunkSet, bool fillTiles = false, bool fillForeground = false, bool fillBackground = false)
            {
                Chunk chunkToTest;
                (int x, int y) posMod;
                TileTraits airTraits = getTileTraits((0, 0));
                TileTraits woodTraits = getTileTraits((3, 1));

                foreach ((int x, int y) posToTest in setToUse)
                {
                    chunkToTest = house.screen.getChunkFromPixelPos(posToTest, true, false);
                    saveChunkSet.Add(chunkToTest);

                    posMod = (PosMod(posToTest.x), PosMod(posToTest.y));
                    chunkToTest.foregroundFillStates[posMod.x, posMod.y] = fillForeground ? woodTraits : airTraits;
                    chunkToTest.fillStates[posMod.x, posMod.y] = fillTiles ? woodTraits : airTraits;
                    chunkToTest.backgroundFillStates[posMod.x, posMod.y] = fillBackground ? woodTraits: airTraits;

                    if (chunkToTest.nonReplacableTiles != null) { chunkToTest.nonReplacableTiles.Add((PosMod(posToTest.x), PosMod(posToTest.y))); }
                    chunkToTest.modificationCount = 1;
                    chunkToTest.findTileColor(PosMod(posToTest.x), PosMod(posToTest.y));
                }
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
            public virtual void makeBubbleRoom((int x, int y) centerPos, int forceSize, bool minFloor, bool failIfNeighborsAir = true, bool failIfLiquid = true, bool failIfSolid = false)
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

                    traits = house.screen.getTileContent(currentTile);
                    if (((minFloor && currentTile.y < centerPos.y) || traits.isLiquid && failIfLiquid) || (traits.isSolid && failIfSolid) || (house.village is null ? house.forbiddenTiles.Contains(currentTile): house.village.forbiddenTiles.Contains(currentTile))) { goto SkipToNextIteration; }
                    if (failIfNeighborsAir)
                    {
                        foreach ((int x, int y) mod in neighbourArray)
                        {
                            for (int mult = 1; mult <= 2; mult++)
                            {
                                posToTest = (currentTile.x + mod.x, currentTile.y + mod.y);
                                traits = house.screen.getTileContent(posToTest);
                                if (traits.isAir) { goto SkipToNextIteration; }
                            }
                        }
                    }

                    tilesToFill.Add(currentTile);
                    foreach ((int x, int y) mod in neighbourArray)
                    {
                        posToAdd = (currentTile.x + mod.x, currentTile.y + mod.y);
                        if (!tilesToFill.Contains(posToAdd) && (house.village is null ? !house.tiles.Contains(posToAdd) : !house.village.tiles.Contains(posToAdd)))
                        {
                            addToQueue(tilesToTest, (posToAdd, repeatCounter + 20 * heuristic(posToAdd, heuristicTargets, house.village is null ? 1.41f : house.village.shape)));
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
            public bool carveEntrance(bool linkToAnotherRoom) // shuffle borders, foreach to find one that has >=2 empty tiles on neighbours, make sure it isn't another house/room that shares the border
            {
                List<(int x, int y)> borderListo = shuffleList(borders.ToList());
                foreach ((int x, int y) pos in borderListo)
                {
                    bool airValidated = false;
                    foreach ((int x, int y) mod in neighbourArray) { if (tiles.Contains((pos.x + mod.x, pos.y + mod.y)) && house.screen.getTileContent((pos.x - mod.x, pos.y - mod.y)).isAir) { airValidated = true; break; } }
                    if (!airValidated) { continue; }
                    if (linkToAnotherRoom)
                    {
                        bool isLinked = false;
                        foreach (Room otherRoom in house.rooms.Values) { if (otherRoom != this && otherRoom.borders.Contains(pos)) { isLinked = true; break; } }
                        if (!isLinked) { continue; }
                        
                    }
                    if (house.village != null) { foreach (House otherHouse in house.village.houses.Values) { if (otherHouse != house && otherHouse.borders.Contains(pos)) { goto SkipPos; } } }
                    // Position validated as entrance
                    entrances.Add(pos);
                    foreach ((int x, int y) mod in neighbourArray)
                    {
                        if (borders.Contains((pos.x + mod.x, pos.y + mod.y)) && !tiles.Contains((pos.x - mod.x, pos.y - mod.y)) && !house.tiles.Contains((pos.x - mod.x, pos.y - mod.y))) { borders.Add((pos.x - mod.x, pos.y - mod.y)); }
                    }
                    return true;
                SkipPos:;
                }
                return false;
            }
            public ((int x, int y) pos, bool found) findTileOfTypeInRoom((int type, int subType) tileToFind)
            {
                List<(int x, int y)> tileList = new List<(int x, int y)>(tiles);
                (int x, int y) posToTest;
                int idxToTest;
                while (tileList.Count > 0)
                {
                    idxToTest = rand.Next(tileList.Count);
                    posToTest = tileList[idxToTest];
                    tileList.RemoveAt(idxToTest);
                    TileTraits tile = house.screen.getTileContent(posToTest);
                    if (tile.type == tileToFind) { return (posToTest, true); }
                }
                return ((0, 0), false);
            }
        }

        public class House : Structure
        {
            public bool isToBeDestroyed = false;
            public Village village;
            public int villageId;
            public int currentRoomId;

            public HashSet<(int x, int y)> tiles = new HashSet<(int x, int y)>();
            public HashSet<(int x, int y)> borders = new HashSet<(int x, int y)>();
            public HashSet<(int x, int y)> entrances = new HashSet<(int x, int y)>();
            public HashSet<(int x, int y)> forbiddenTiles = new HashSet<(int x, int y)>();
            public bool containsDebris;

            public HashSet<Entity> assignedEntities = new HashSet<Entity>();
            public HashSet<int> outsideEntities = new HashSet<int>();

            public bool inConstruction = false;
            public Dictionary<int, Room> rooms = new Dictionary<int, Room>();

            public Color displayColor;

            public House() { }
            public House(Game game, HouseJson houseJson, bool isPartOfVillage)
            {
                setAllStructureJsonVariables(game, houseJson);
                applyTraitsToStructure();
                if (isErasedFromTheWorld) { return; }

                displayColor = Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));

                foreach (RoomJson roomJson in houseJson.rooms) { rooms[roomJson.id] = new Room(this, roomJson); }
                currentRoomId = houseJson.roomId;

                foreach (int entityId in houseJson.ent)
                {
                    if (screen.activeEntities.ContainsKey(entityId)) { addEntityToStructure(screen.activeEntities[entityId]); }
                    else { outsideEntities.Add(id); }
                }

                updateTiles();
                if (!isPartOfVillage) { addStructureToTheRightDictInTheScreen(); }
            }
            public House(Screens.Screen screenToPut, (int x, int y) posToPut, long seedToPut, (int type, int subType, int subSubType) typeToPut)    // When creating independant house structures
            {
                screen = screenToPut;
                villageId = -1;
                type = typeToPut;
                seed = seedToPut;

                (int x, int y)? posToSpawn = findNextHousePosition(screen, posToPut, null);
                if (posToSpawn is null) { return; }
                pos = posToSpawn.Value;
                if (!initAndMakeRooms(false)) { return; }

                saveStructure();
                addToMegaChunks();
                addStructureToTheRightDictInTheScreen();
            }
            public House(Village villageToPut, (int x, int y) posToPut, int villageIdToPut, (int type, int subType, int subSubType) typeToPut)  // When creating house structures as part of villages
            {
                village = villageToPut;
                screen = village.screen;
                pos = posToPut;
                villageId = villageIdToPut;
                type = typeToPut;
                seed = village.seed;

                initAndMakeRooms(true);
            }
            public override void saveStructure()
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new JavaScriptDateTimeConverter());
                serializer.NullValueHandling = NullValueHandling.Ignore;

                HouseJson houseJson = new HouseJson(this);

                using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\CaveData\\{screen.game.seed}\\StructureData\\{id}.json"))
                using (JsonWriter writer = new JsonTextWriter(sw)) { serializer.Serialize(writer, houseJson); }
            }
            public override int setClassTypeInJson() { return 3; }
            public override House getItselfAsHouse() { return this; } // not perfect but ehhhhh... for debug
            public bool initAndMakeRooms(bool isPartOfVillage)
            {
                traits = getStructureTraits(type);
                applyTraitsToStructure();
                currentRoomId = 0;
                displayColor = Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));

                Room newRoom = new Room(pos, 0, currentRoomId, this);
                if (newRoom.isToBeDestroyed) { isToBeDestroyed = true; return false; }
                currentRoomId++;

                if (isPartOfVillage) { village.houses[villageId] = this; village.currentHouseId++; }

                id = currentStructureId;    // id is STRUCTURE id, in the game, roomId is room ID in the VILLAGE. BC Houses are both structures, and parts of villages
                currentStructureId++;

                newRoom.fillTiles();
                rooms.Add(newRoom.id, newRoom);
                updateTiles();
                if (isPartOfVillage) { village.updateTiles(); }

                int extraRoomCount = rand.Next(7) - 3 + (rand.Next(10) == 0 ? 10 : 0) + (rand.Next(1000) == 0 ? 50 : 0);
                for (int i = 0; i < extraRoomCount; i++)
                {
                    (int x, int y) posToAddTo = getRandomItem(borders.ToList());
                    posToAddTo = (posToAddTo.x + rand.Next(3) - 1, posToAddTo.y + rand.Next(3) - 1);
                    int counto = 0;
                    while (counto < 50)
                    {
                        if (tiles.Contains(posToAddTo) || borders.Contains(posToAddTo))
                        {
                            posToAddTo = getRandomItem(borders.ToList());
                            posToAddTo = (posToAddTo.x + rand.Next(5) - 2, posToAddTo.y + rand.Next(5) - 2);
                            counto++;
                            continue;
                        }
                        break;
                    }
                    if (counto == 50) { continue; }
                    newRoom = new Room(posToAddTo, 1, currentRoomId, this);
                    if (newRoom.isToBeDestroyed) { continue; }
                    currentRoomId++;
                    newRoom.fillTiles();
                    rooms.Add(newRoom.id, newRoom);
                    updateTiles();
                    if (isPartOfVillage) { village.updateTiles(); }
                }

                return true;
            }

            public virtual void removeEntity(Entity entity)
            {
                assignedEntities.Remove(entity);
                outsideEntities.Remove(entity.id);
            }
            public virtual void updateTiles()
            {
                chunkPresence = new HashSet<(int x, int y)>();
                megaChunkPresence = new HashSet<(int x, int y)>();
                tiles = new HashSet<(int x, int y)>();
                borders = new HashSet<(int x, int y)>();
                entrances = new HashSet<(int x, int y)>();
                foreach (Room room in rooms.Values)
                {
                    foreach ((int x, int y) tile in room.tiles)
                    {
                        tiles.Add(tile);
                        forbiddenTiles.Add(tile);
                        chunkPresence.Add(ChunkIdx(tile.x, tile.y));
                    }
                    foreach ((int x, int y) tile in room.borders)
                    {
                        borders.Add(tile);
                        forbiddenTiles.Add(tile);
                        chunkPresence.Add(ChunkIdx(tile.x, tile.y));
                    }
                    foreach ((int x, int y) tile in room.entrances)
                    {
                        entrances.Add(tile);
                        chunkPresence.Add(ChunkIdx(tile.x, tile.y));
                        for (int i = -3; i <= 3; i++)
                        {
                            for (int j = -3; j <= 3; j++)
                            {
                                forbiddenTiles.Add((tile.x + i, tile.y + j));
                            }
                        }
                    }
                }
                foreach ((int x, int y) poso in chunkPresence) { megaChunkPresence.Add(MegaChunkIdxFromChunkPos(poso)); }
            }
            public ((int x, int y) pos, bool found) findTileOfType((int type, int subType) tileToFind)
            {
                List<(int x, int y)> tileList = new List<(int x, int y)>(tiles);
                (int x, int y) posToTest;
                int idxToTest;
                while (tileList.Count > 0)
                {
                    idxToTest = rand.Next(tileList.Count);
                    posToTest = tileList[idxToTest];
                    tileList.RemoveAt(idxToTest);
                    TileTraits tile = screen.getTileContent(posToTest);
                    if (tile.type == tileToFind) { return (posToTest, true); }
                }
                return ((0, 0), false);
            }
        }

        public class Village : Structure
        {
            // stats to build nest and what it looks like iggg
            // public int roomSize = 0; // bigger leads to bigger rooms NOT USED
            // public int connectivity = 0; // bigger leads to less connexions (corridors) NOT USED
            // public int extensivity = 0; // bigger leads to new rooms and corridors being dug out farther away from the center ig NOT USED
            public float shape = 1.2f;// 1.41421356237f; //the only one that has an actual effect LMFAO

            public HashSet<(int x, int y)> tiles = new HashSet<(int x, int y)>();
            public HashSet<(int x, int y)> borders = new HashSet<(int x, int y)>();
            public HashSet<(int x, int y)> entrances = new HashSet<(int x, int y)>();
            public HashSet<(int x, int y)> forbiddenTiles = new HashSet<(int x, int y)>();
            public Dictionary<int, House> houses = new Dictionary<int, House>();
            public int currentHouseId = 0;
            // public bool isStable = false;

            // entity management
            // public HashSet<int> outsideVillagers = new HashSet<int>();
            // public HashSet<Entity> villagers = new HashSet<Entity>();
            // public HashSet<(int x, int y)> digErrands = new HashSet<(int x, int y)>();
            
            public Village(Game game, VillageJson villageJson)
            {
                setAllStructureJsonVariables(game, villageJson);
                applyTraitsToStructure();
                if (isErasedFromTheWorld) { return; }

                foreach (HouseJson houseJson in villageJson.houses) { houses[houseJson.id] = new House(game, houseJson, true); }
                currentHouseId = villageJson.houseId;

                /*foreach (int entityId in villageJson.ent)
                {
                    if (screen.activeEntities.ContainsKey(entityId)) { addEntityToStructure(screen.activeEntities[entityId]); }
                    else { outsideEntities.Add(id); }
                }*/
                updateTiles();
                addStructureToTheRightDictInTheScreen();
            }
            public Village(Screens.Screen screenToPut, (int x, int y) posToPut, long seedToPut, (int type, int subType, int subSubType) typeToPut)
            {
                screen = screenToPut;
                pos = posToPut;
                seed = seedToPut;
                type = typeToPut;
                traits = getStructureTraits(type);
                applyTraitsToStructure();

                spawnHouses();
                updateTiles();

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

                VillageJson villageJson = new VillageJson(this);

                using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\CaveData\\{screen.game.seed}\\StructureData\\{id}.json"))
                using (JsonWriter writer = new JsonTextWriter(sw)) { serializer.Serialize(writer, villageJson); }
            }
            public override int setClassTypeInJson() { return 2; }
            public override Village getItselfAsVillage() { return this; } // not perfect but ehhhhh... for debug

            public void spawnHouses()
            {
                int amountToSpawn = 2 + rand.Next(12) + rand.Next(13);
                (int x, int y)? posToSpawn;
                for (int i = 0; i < amountToSpawn; i++)
                {
                    posToSpawn = findNextHousePosition(screen, pos, this);
                    if (posToSpawn == null) { continue; }
                    House newHouse = new House(this, posToSpawn.Value, currentHouseId, (6, 0, 0));
                }
            }
            public void updateTiles()
            {
                chunkPresence = new HashSet<(int x, int y)>();
                megaChunkPresence = new HashSet<(int x, int y)>();
                tiles = new HashSet<(int x, int y)>();
                borders = new HashSet<(int x, int y)>();
                entrances = new HashSet<(int x, int y)>();
                forbiddenTiles = new HashSet<(int x, int y)>();
                foreach (House house in houses.Values)
                {
                    house.updateTiles();
                    foreach ((int x, int y) tile in house.tiles) { tiles.Add(tile); }
                    foreach ((int x, int y) tile in house.borders) { borders.Add(tile); }
                    foreach ((int x, int y) tile in house.entrances) { entrances.Add(tile); }
                    foreach ((int x, int y) tile in house.forbiddenTiles) { forbiddenTiles.Add(tile); }
                    foreach ((int x, int y) tile in house.chunkPresence) { chunkPresence.Add(tile); }
                }
                foreach ((int x, int y) poso in chunkPresence) { megaChunkPresence.Add(MegaChunkIdxFromChunkPos(poso)); }
            }
        }

        public static (int x, int y)? findNextHousePosition(Screens.Screen screen, (int x, int y) startPos, Village village)
        {
            (int x, int y) posToTest = startPos;
            TileTraits tileTraits = screen.getTileContent(posToTest);   // these values are just there as placeholder because linter shitty poop
            int count = 0;
        tryAgain:;
            while (count < 50)
            {
                count++;
                posToTest = (startPos.x + rand.Next(101) - 50, startPos.y + rand.Next(101) - 50);
                tileTraits = screen.getTileContent(posToTest);
                if (tileTraits.isAir && (village is null || !village.tiles.Contains(posToTest))) { goto airFound; }
            }
            return null;
        airFound:;

            int count2 = 0;
            (int x, int y) posUnder;
            while (count2 < 100)
            {
                posUnder = (posToTest.x, posToTest.y - 1);
                tileTraits = screen.getTileContent(posUnder);
                if (tileTraits.isLiquid) { goto tryAgain; }
                if (tileTraits.isSolid) { goto solidFound; }
                posToTest = posUnder;
                count2++;
            }
            return null;
        solidFound:;
            if (village != null && village.borders.Contains(posUnder)) { goto tryAgain; }
            (int x, int y) posToKeep = posToTest;
            posToTest = (posToTest.x, posToTest.y + count2);
            while (count2 < 20)
            {
                if (screen.getTileContent(posToTest).isSolid) { goto tryAgain; }    // if count < 12 try again
                posToTest = (posToTest.x, posToTest.y + 1);
                count2++;
            }
            return posToKeep;
            // find house by bordel, maisons mitoyennes etc etc
        }
    }
}
