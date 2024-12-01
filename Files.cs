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

namespace Cave
{
    public class Files
    {
        public class SettingsJson
        {
            public int nestId;
            public int entityId;
            public int plantId;
            public int structureId;
            public int dimensionId;
            public float time;
            public PlayerJson player;
            public int livingDimensionId;
            public SettingsJson(Game game)
            {
                time = timeElapsed;
                nestId = currentNestId;
                plantId = currentPlantId;
                dimensionId = currentDimensionId;
                entityId = currentEntityId;
                structureId = currentStructureId;
                player = new PlayerJson(game.playerList[0]);
            }
            public SettingsJson()
            {

            }
        }
        public class DimensionJson
        {
            public long seed;
            public int id;
            public (int biome, int subBiome) type;
            public bool isMono;
            public DimensionJson(Screens.Screen screen)
            {
                seed = screen.seed;
                id = screen.id;
                type = screen.type;
                isMono = screen.isMonoBiome;
            }
            public DimensionJson()
            {

            }
        }
        public class MegaChunk
        {
            public (int, int) pos;
            public List<int> nests = new List<int>();
            public List<int> structures = new List<int>();
            [NonSerialized] public bool isPurelyStructureLoaded = true;
            public MegaChunk((int x, int y) posToPut)
            {
                pos = posToPut;
            }
            public MegaChunk()
            {

            }
            public void loadAllChunksInNests(Screens.Screen screen)
            {
                Chunk newChunk;
                Nest nesto;
                foreach (int nestId in nests)
                {
                    if (!screen.activeNests.ContainsKey(nestId))
                    {
                        continue;
                    }
                    nesto = screen.activeNests[nestId];
                    foreach ((int x, int y) pos in nesto.chunkPresence.Keys)
                    {
                        if (!screen.loadedChunks.ContainsKey(pos))
                        {
                            newChunk = new Chunk(pos, false, screen); // this is needed cause uhh yeah idk sometimes loadedChunks is FUCKING ADDED IN AGAIN ???
                            if (!screen.loadedChunks.ContainsKey(pos)) { screen.loadedChunks[pos] = newChunk; }
                        }
                    }
                }
            }
            public void unloadAllNestsAndStructuresAndChunks(Screens.Screen screen, Dictionary<(int x, int y), bool> chunksToRemove)
            {
                (int x, int y) playerPos = (ChunkIdx(screen.game.playerList[0].posX), ChunkIdx(screen.game.playerList[0].posY));
                foreach (int nestId in nests)
                {
                    saveNest(screen.activeNests[nestId]);
                    foreach ((int x, int y) pososo in screen.activeNests[nestId].chunkPresence.Keys) // this unloads the chunks in nests that are getting unloaded, as the magachunk would get reloaded again if not as they wouldn't be counted as nest loaded chunks anymore
                    {
                        if (distance(pososo, playerPos) > 0.8f * screen.chunkResolution) { chunksToRemove[pososo] = true; }
                    }
                    screen.activeNests.Remove(nestId);
                }
                foreach (int structureId in structures)
                {
                    if (screen.activeStructures.ContainsKey(structureId))
                    {
                        saveStructure(screen.activeStructures[structureId]);
                        foreach ((int x, int y) pososo in screen.activeStructures[structureId].chunkPresence.Keys) // this unloads the chunks in nests that are getting unloaded, as the magachunk would get reloaded again if not as they wouldn't be counted as nest loaded chunks anymore
                        {
                            if (distance(pososo, playerPos) > 0.8f * screen.chunkResolution) { chunksToRemove[pososo] = true; }
                        }
                        screen.activeStructures.Remove(structureId);
                    }
                    if (screen.inertStructures.ContainsKey(structureId))
                    {
                        screen.inertStructures.Remove(structureId);
                    }
                }
            }
            public void loadAllNests(Screens.Screen screen)
            {
                Chunk newChunk;
                foreach (int nestId in nests)
                {
                    if (!screen.activeNests.ContainsKey(nestId))
                    {
                        Nest nesto = loadNest(screen, nestId);
                        screen.activeNests[nestId] = nesto;
                        foreach ((int, int) chunkPos in nesto.chunkPresence.Keys)
                        {
                            if (!screen.loadedChunks.ContainsKey(chunkPos))
                            {
                                newChunk = new Chunk(pos, false, screen); // this is needed cause uhh yeah idk sometimes loadedChunks is FUCKING ADDED IN AGAIN ???
                                if (!screen.loadedChunks.ContainsKey(pos)) { screen.loadedChunks[pos] = newChunk; }
                            }
                        }
                    }
                }
            }
            public void loadAllStructures(Screens.Screen screen)
            {
                foreach (int structureId in structures)
                {
                    loadStructure(screen, structureId); // loadStructure already adds the structure to the right dicto so no need to do it here
                }
            }
        }
        public class ChunkJson
        {
            public long seed;
            public (int, int) pos;
            public int[,] fill1;
            public int[,] fill2;
            public bool spwnd;
            public List<int> eLst;
            public List<int> pLst;
            public int explLvl;
            public bool[,] fog;
            public ChunkJson(Chunk chunk)
            {
                seed = chunk.chunkSeed;
                pos = chunk.position;
                (int[,] one, int[,] two) returnTuple = ChunkToChunkJsonfillStates(chunk.fillStates);
                fill1 = returnTuple.one;
                fill2 = returnTuple.two;
                spwnd = chunk.entitiesAndPlantsSpawned;
                eLst = new List<int>();
                foreach (Entity entity in chunk.entityList)
                {
                    saveEntity(entity);
                    eLst.Add(entity.id);
                }
                chunk.entityList = new List<Entity>();
                pLst = new List<int>();
                foreach (Plant plant in chunk.plantList)
                {
                    savePlant(plant);
                    pLst.Add(plant.id);
                }
                chunk.plantList = new List<Plant>();
                explLvl = chunk.explorationLevel;
                if (explLvl == 1)
                {
                    fog = chunk.fogOfWar;
                }
                else { fog = null; }
            }
            public ChunkJson()
            {

            }
        }
        public class PlayerJson
        {
            public int currentDimension;
            public (float x, float y) pos;
            public (float x, float y) speed;
            public int[,] inv;
            public (float x, float y) lastDP;
            public PlayerJson(Player player)
            {
                currentDimension = player.screen.id;
                pos = (player.realPosX, player.realPosY);
                speed = (player.speedX, player.speedY);
                inv = inventoryToArray(player.inventoryQuantities, player.inventoryElements);
                lastDP = (player.timeAtLastDig, player.timeAtLastPlace);
            }
            public PlayerJson()
            {

            }
        }
        public class EntityJson
        {
            public int seed;
            public int id;
            public int nstId;
            public (int, int) type;
            public int state;
            public (float, float) pos;
            public (int, int) tPos;
            public (float, float) speed;
            public int[,] inv;
            public (float, float) lastDP;
            public int hp;
            public float brth;
            public float sttCh;
            public float tp;
            public EntityJson(Entity entity)
            {
                seed = entity.seed;
                id = entity.id;
                if (entity.nest == null) { nstId = -1; }
                else { nstId = entity.nest.id; }
                type = (entity.type, entity.subType);
                state = entity.state;
                pos = (entity.realPosX, entity.realPosY);
                tPos = entity.targetPos;
                speed = (entity.speedX, entity.speedY);
                inv = inventoryToArray(entity.inventoryQuantities, entity.inventoryElements);
                lastDP = (entity.timeAtLastDig, entity.timeAtLastPlace);
                hp = entity.hp;
                brth = entity.timeAtBirth;
                sttCh = entity.timeAtLastStateChange;
                tp = entity.timeAtLastTeleportation;
            }
            public EntityJson()
            {

            }
        }
        public class PlantJson
        {
            public int seed;
            public int id;
            public (int, int) type;
            public (int, int) pos;
            public (int, int) lstGrPos;
            public int grLvl;
            public float lastGr;

            public int[,] fS;

            public List<BranchJson> branches;
            public List<FlowerJson> flowers;
            public PlantJson(Plant plant)
            {
                seed = plant.seed;
                id = plant.id;
                type = plant.type;
                pos = (plant.posX, plant.posY);
                lstGrPos = plant.lastDrawPos;
                grLvl = plant.growthLevel;
                lastGr = plant.timeAtLastGrowth;
                fS = fillstatesToArray(plant.fillStates);
                branches = new List<BranchJson>();
                foreach (Branch childBranch in plant.childBranches)
                {
                    branches.Add(new BranchJson(childBranch));
                }
                flowers = new List<FlowerJson>();
                foreach (Flower flower in plant.childFlowers)
                {
                    flowers.Add(new FlowerJson(flower));
                }
            }
            public PlantJson()
            {

            }
        }
        public class BranchJson
        {
            public int seed;
            public (int, int) pos;
            public (int, int) lstGrPos;
            public int type;
            public int grLvl;

            public int[,] fS;

            public List<BranchJson> branches;
            public List<FlowerJson> flowers;
            public BranchJson(Branch branch)
            {
                seed = branch.seed;
                type = branch.type;
                pos = branch.pos;
                lstGrPos = branch.lastDrawPos;
                grLvl = branch.growthLevel;
                fS = fillstatesToArray(branch.fillStates);
                branches = new List<BranchJson>();
                foreach (Branch childBranch in branch.childBranches)
                {
                    branches.Add(new BranchJson(childBranch));
                }
                flowers = new List<FlowerJson>();
                foreach (Flower flower in branch.childFlowers)
                {
                    flowers.Add(new FlowerJson(flower));
                }
            }
            public BranchJson()
            {

            }
        }
        public class FlowerJson
        {
            public int seed;
            public (int, int) pos;
            public (int, int) lstGrPos;
            public int type;
            public int grLvl;

            public int[,] fS;
            public FlowerJson(Flower flower)
            {
                seed = flower.seed;
                type = flower.type;
                pos = flower.pos;
                lstGrPos = flower.lastDrawPos;
                grLvl = flower.growthLevel;
                fS = fillstatesToArray(flower.fillStates);
            }
            public FlowerJson()
            {

            }
        }
        public class RoomJson
        {
            public int id;
            public int type;
            public long seed;
            public (int x, int y) pos;

            public int[,] tiles;

            public int[] ent;
            public RoomJson(Room room)
            {
                id = room.id;
                seed = room.seed;
                type = room.type;
                pos = room.position;
                tiles = tileListToArray(room.tiles);
                ent = new int[room.assignedEntities.Count];
                for (int i = 0; i < room.assignedEntities.Count; i++)
                {
                    ent[i] = room.assignedEntities[i].id;
                }
            }
            public RoomJson()
            {

            }
        }
        public class StructureJson
        {
            public int id;
            public (int, int, int) type;
            public bool isD;
            public (long, long) seed;
            public (int, int) pos;
            public (int, int) size;
            public string name;
            public float brth;
            public int state;
            public int sis;

            public int[,] fS;
            public StructureJson(Structure structure)
            {
                id = structure.id;
                type = structure.type;
                isD = structure.isDynamic;
                seed = structure.seed;
                pos = structure.pos;
                size = structure.size;
                name = structure.name;
                brth = structure.timeAtBirth;
                state = structure.state;
                sis = structure.sisterStructure;
                fS = fillstatesToArray(structure.structureDict);
            }
            public StructureJson()
            {

            }
        }
        public class NestJson
        {
            public int id;
            public int type;
            public long seed;

            public (int x, int y) pos;
            public RoomJson[] rooms;
            public int roomId = 0;
            public bool stable = false;

            public int[] ent;
            public NestJson(Nest nest)
            {
                id = nest.id;
                seed = nest.seed;
                type = nest.type;
                pos = nest.position;
                Room[] allRooms = nest.rooms.Values.ToArray();
                rooms = new RoomJson[allRooms.Length];
                for (int i = 0; i < allRooms.Length; i++)
                {
                    rooms[i] = new RoomJson(allRooms[i]);
                }

                ent = new int[nest.outsideEntities.Count + nest.adults.Count + nest.larvae.Count];
                int idx = 0;
                for (int i = 0; i < nest.outsideEntities.Count; i++)
                {
                    ent[idx] = nest.outsideEntities[i];
                    idx++;
                }
                for (int i = 0; i < nest.adults.Count; i++)
                {
                    ent[idx] = nest.adults[i].id;
                    idx++;
                }
                for (int i = 0; i < nest.larvae.Count; i++)
                {
                    ent[idx] = nest.larvae[i].id;
                    idx++;
                }
            }
            public NestJson()
            {

            }
        }
        public static int[,] tileListToArray(List<(int x, int y)> listo)
        {
            int[,] arrayo = new int[listo.Count, 2];
            (int x, int y)[] keyo = listo.ToArray();
            for (int i = 0; i < keyo.Length; i++)
            {
                arrayo[i, 0] = keyo[i].x;
                arrayo[i, 1] = keyo[i].y;
            }
            return arrayo;
        }
        public static int[,] fillstatesToArray(Dictionary<(int x, int y), (int type, int subType)> dicto)
        {
            int[,] arrayo = new int[dicto.Count, 4];
            (int x, int y)[] keyo = dicto.Keys.ToArray();
            (int type, int subType) current;
            for (int i = 0; i < keyo.Length; i++)
            {
                arrayo[i, 0] = keyo[i].x;
                arrayo[i, 1] = keyo[i].y;
                current = dicto[keyo[i]];
                arrayo[i, 2] = current.type;
                arrayo[i, 3] = current.subType;
            }
            return arrayo;
        }
        public static int[,] inventoryToArray(Dictionary<(int index, int subType, int typeOfElement), int> dicto, List<(int index, int subType, int typeOfElement)> listo)
        {
            int[,] arrayo = new int[dicto.Count, 4];
            for (int i = 0; i < listo.Count; i++)
            {
                arrayo[i, 0] = listo[i].index;
                arrayo[i, 1] = listo[i].subType;
                arrayo[i, 2] = listo[i].typeOfElement;
                arrayo[i, 3] = dicto[listo[i]];
            }
            return arrayo;
        }
        public static Dictionary<(int x, int y), (int type, int subType)> arrayToFillstates(int[,] arrayo)
        {
            Dictionary<(int x, int y), (int type, int subType)> dicto = new Dictionary<(int x, int y), (int type, int subType)>();
            for (int i = 0; i < arrayo.GetLength(0); i++)
            {
                dicto[(arrayo[i, 0], arrayo[i, 1])] = (arrayo[i, 2], arrayo[i, 3]);
            }
            return dicto;
        }
        public static List<(int x, int y)> arrayToTileList(int[,] arrayo)
        {
            List<(int x, int y)> listo = new List<(int x, int y)>();
            for (int i = 0; i < arrayo.GetLength(0); i++)
            {
                listo.Add((arrayo[i, 0], arrayo[i, 1]));
            }
            return listo;
        }
        public static (int type, int subType)[,] ChunkJsonToChunkfillStates(int[,] array1, int[,] array2)
        {
            (int type, int subType)[,] arrayo = new (int type, int subType)[32, 32];
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    arrayo[i, j] = (array1[i, j], array2[i, j]);
                }
            }
            return arrayo;
        }
        public static (int[,], int[,]) ChunkToChunkJsonfillStates((int type, int subType)[,] array)
        {
            int[,] arrayo1 = new int[32, 32];
            int[,] arrayo2 = new int[32, 32];
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    arrayo1[i, j] = array[i, j].type;
                    arrayo2[i, j] = array[i, j].subType;
                }
            }
            return (arrayo1, arrayo2);
        }
        public static (Dictionary<(int index, int subType, int typeOfElement), int>, List<(int index, int subType, int typeOfElement)>) arrayToInventory(int[,] arrayo)
        {
            Dictionary<(int index, int subType, int typeOfElement), int> dicto = new Dictionary<(int index, int subType, int typeOfElement), int>();
            List<(int index, int subType, int typeOfElement)> listo = new List<(int index, int subType, int typeOfElement)>();
            for (int i = 0; i < arrayo.GetLength(0); i++)
            {
                dicto[(arrayo[i, 0], arrayo[i, 1], arrayo[i, 2])] = arrayo[i, 3];
                listo.Add((arrayo[i, 0], arrayo[i, 1], arrayo[i, 2]));
            }
            return (dicto, listo);
        }
        public static void saveMegaChunk(MegaChunk megaChunk, (int x, int y) pos, int id)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\CaveData\\{worldSeed}\\MegaChunkData\\{id}\\{pos.x}.{pos.y}.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, megaChunk);
            }
        }
        public static MegaChunk loadMegaChunk(Screens.Screen screenToPut, (int x, int y) pos)
        {
            if (!System.IO.File.Exists($"{currentDirectory}\\CaveData\\{worldSeed}\\MegaChunkData\\{screenToPut.id}\\{pos.x}.{pos.y}.json"))
            {
                MegaChunk oOoOdindondindondandoyoufeelmylove = new MegaChunk(pos);
                saveMegaChunk(oOoOdindondindondandoyoufeelmylove, pos, screenToPut.id);
                return oOoOdindondindondandoyoufeelmylove;
            }
            using (StreamReader f = new StreamReader($"{currentDirectory}\\CaveData\\{worldSeed}\\MegaChunkData\\{screenToPut.id}\\{pos.x}.{pos.y}.json"))
            {
                string content = f.ReadToEnd();
                MegaChunk imatwinklelittlemermaidgirl = JsonConvert.DeserializeObject<MegaChunk>(content);

                return imatwinklelittlemermaidgirl;
            }
        }
        public static void saveChunk(Chunk chunk)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\CaveData\\{chunk.screen.game.seed}\\ChunkData\\{chunk.screen.id}\\{chunk.position.Item1}.{chunk.position.Item2}.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                ChunkJson baby = new ChunkJson(chunk);
                serializer.Serialize(writer, baby);
            }
        }
        public static void saveAllChunks(Screens.Screen screen)
        {
            foreach (Chunk chunko in screen.loadedChunks.Values)
            {
                saveChunk(chunko);
            }
        }
        public static void loadChunk(Chunk chunk, bool loadEntitiesAndPlants)
        {
            //bool willSpawnEntities;
            using (StreamReader f = new StreamReader($"{currentDirectory}\\CaveData\\{chunk.screen.game.seed}\\ChunkData\\{chunk.screen.id}\\{chunk.position.Item1}.{chunk.position.Item2}.json"))
            {
                string content = f.ReadToEnd();
                ChunkJson chunkJson = JsonConvert.DeserializeObject<ChunkJson>(content);

                chunk.chunkSeed = chunkJson.seed;
                chunk.position = chunkJson.pos;
                chunk.fillStates = ChunkJsonToChunkfillStates(chunkJson.fill1, chunkJson.fill2);
                chunk.entitiesAndPlantsSpawned = chunkJson.spwnd;

                if (loadEntitiesAndPlants)
                {
                    chunk.entityList = new List<Entity>();
                    foreach (int entityId in chunkJson.eLst)
                    {
                        chunk.entityList.Add(loadEntity(chunk.screen, entityId));
                    }
                    chunk.plantList = new List<Plant>();
                    foreach (int plantId in chunkJson.pLst)
                    {
                        chunk.plantList.Add(loadPlant(chunk.screen, plantId));
                    }
                }

                chunk.explorationLevel = chunkJson.explLvl;
                if (chunk.explorationLevel == 1)
                {
                    chunk.fogOfWar = chunkJson.fog;
                    chunk.fogBitmap = new Bitmap(32, 32);
                    for (int i = 0; i < 32; i++)
                    {
                        for (int j = 0; j < 32; j++)
                        {
                            if (!chunk.fogOfWar[i, j]) { setPixelButFaster(chunk.fogBitmap, (i, j), Color.Black); }
                        }
                    }
                }
                else { chunk.fogOfWar = null; }
            }
        }
        public static Plant loadPlant(Screens.Screen screenToPut, int plantId)
        {
            using (StreamReader f = new StreamReader($"{currentDirectory}\\CaveData\\{worldSeed}\\PlantData\\{plantId}.json"))
            {
                string content = f.ReadToEnd();
                PlantJson plantJson = JsonConvert.DeserializeObject<PlantJson>(content);

                return new Plant(screenToPut, plantJson);
            }
        }
        public static Entity loadEntity(Screens.Screen screenToPut, int entityId)
        {
            using (StreamReader f = new StreamReader($"{currentDirectory}\\CaveData\\{worldSeed}\\EntityData\\{entityId}.json"))
            {
                string content = f.ReadToEnd();
                EntityJson entityJson = JsonConvert.DeserializeObject<EntityJson>(content);

                return new Entity(screenToPut, entityJson);
            }
        }
        public static void savePlant(Plant plant)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            PlantJson plantJson = new PlantJson(plant);

            using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\CaveData\\{plant.screen.game.seed}\\PlantData\\{plant.id}.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, plantJson);
            }
        }
        public static void saveEntity(Entity entity)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            EntityJson entityJson = new EntityJson(entity);

            using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\CaveData\\{entity.screen.game.seed}\\EntityData\\{entity.id}.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, entityJson);
            }
        }
        public static void saveStructure(Structure structure)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            StructureJson structureJson = new StructureJson(structure);

            using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\CaveData\\{structure.screen.game.seed}\\StructureData\\{structure.id}.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, structureJson);
            }
        }
        public static Structure loadStructure(Screens.Screen screen, int id)
        {
            using (StreamReader f = new StreamReader($"{currentDirectory}\\CaveData\\{screen.game.seed}\\StructureData\\{id}.json"))
            {
                string content = f.ReadToEnd();
                StructureJson structureJson = JsonConvert.DeserializeObject<StructureJson>(content);

                return new Structure(screen, structureJson);
            }
        }
        public static void saveNest(Nest nest)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            NestJson nestJson = new NestJson(nest);

            using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\CaveData\\{nest.screen.game.seed}\\NestData\\{nest.id}.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, nestJson);
            }
        }
        public static Nest loadNest(Screens.Screen screen, int id)
        {
            using (StreamReader f = new StreamReader($"{currentDirectory}\\CaveData\\{screen.game.seed}\\NestData\\{id}.json"))
            {
                string content = f.ReadToEnd();
                NestJson nestJson = JsonConvert.DeserializeObject<NestJson>(content);

                return new Nest(screen, nestJson);
            }
        }
        public static void saveSettings(Game game)
        {
            if (game.playerList.Count == 0) { return; }

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            SettingsJson settingsJson = new SettingsJson(game);

            using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\CaveData\\{worldSeed}\\settings.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, settingsJson);
            }
        }
        public static SettingsJson tryLoadSettings(Game game)
        {
            if (!System.IO.File.Exists($"{currentDirectory}\\CaveData\\{game.seed}\\settings.json")) { return null; }

            SettingsJson settingsJson;
            using (StreamReader f = new StreamReader($"{currentDirectory}\\CaveData\\{game.seed}\\settings.json"))
            {
                string content = f.ReadToEnd();
                settingsJson = JsonConvert.DeserializeObject<SettingsJson>(content);
            }
            return settingsJson;
        }
        public static void saveDimensionData(Screens.Screen screen)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            DimensionJson dimensionJson = new DimensionJson(screen);
            
            using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\CaveData\\{screen.game.seed}\\DimensionData\\{screen.id}.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, dimensionJson);
            }
        }
        public static DimensionJson tryLoadDimension(Game game, int id)
        {
            if (!System.IO.File.Exists($"{currentDirectory}\\CaveData\\{game.seed}\\DimensionData\\{id}.json")) { return null; }

            DimensionJson dimensionJson;
            using (StreamReader f = new StreamReader($"{currentDirectory}\\CaveData\\{game.seed}\\dimensionData\\{id}.json"))
            {
                string content = f.ReadToEnd();
                dimensionJson = JsonConvert.DeserializeObject<DimensionJson>(content);
            }
            return dimensionJson;
        }
        public static void createDimensionFolders(Game game, int id)
        {
            if (!Directory.Exists($"{currentDirectory}\\CaveData\\{game.seed}\\MegaChunkData\\{id}"))
            {
                Directory.CreateDirectory($"{currentDirectory}\\CaveData\\{game.seed}\\MegaChunkData\\{id}");
            }
            if (!Directory.Exists($"{currentDirectory}\\CaveData\\{game.seed}\\ChunkData\\{id}"))
            {
                Directory.CreateDirectory($"{currentDirectory}\\CaveData\\{game.seed}\\ChunkData\\{id}");
            }
        }
        public static void createFolders(long seed)
        {
            if (!Directory.Exists($"{currentDirectory}\\BiomeDiagrams"))
            {
                Directory.CreateDirectory($"{currentDirectory}\\BiomeDiagrams");
            }


            if (!Directory.Exists($"{currentDirectory}\\CaveData\\{seed}\\DimensionData"))
            {
                Directory.CreateDirectory($"{currentDirectory}\\CaveData\\{seed}\\DimensionData");
            }
            if (!Directory.Exists($"{currentDirectory}\\CaveData\\{seed}\\MegaChunkData"))
            {
                Directory.CreateDirectory($"{currentDirectory}\\CaveData\\{seed}\\MegaChunkData");
            }
            if (!Directory.Exists($"{currentDirectory}\\CaveData\\{seed}\\ChunkData"))
            {
                Directory.CreateDirectory($"{currentDirectory}\\CaveData\\{seed}\\ChunkData");
            }
            if (!Directory.Exists($"{currentDirectory}\\CaveData\\{seed}\\StructureData"))
            {
                Directory.CreateDirectory($"{currentDirectory}\\CaveData\\{seed}\\StructureData");
            }
            if (!Directory.Exists($"{currentDirectory}\\CaveData\\{seed}\\PlantData"))
            {
                Directory.CreateDirectory($"{currentDirectory}\\CaveData\\{seed}\\PlantData");
            }
            if (!Directory.Exists($"{currentDirectory}\\CaveData\\{seed}\\EntityData"))
            {
                Directory.CreateDirectory($"{currentDirectory}\\CaveData\\{seed}\\EntityData");
            }
            if (!Directory.Exists($"{currentDirectory}\\CaveData\\{seed}\\NestData"))
            {
                Directory.CreateDirectory($"{currentDirectory}\\CaveData\\{seed}\\NestData");
            }
            if (!Directory.Exists($"{currentDirectory}\\CaveData\\bitmapos"))
            {
                Directory.CreateDirectory($"{currentDirectory}\\CaveData\\bitmapos");
            }
        }
    }
}
