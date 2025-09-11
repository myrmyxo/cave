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
using static Cave.Files;
using static Cave.Plants;
using static Cave.Screens;
using static Cave.Chunks;
using static Cave.Players;
using static Cave.Particles;
using static Cave.Traits;

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
                plantId = currentPlantId;
                dimensionId = currentDimensionId;
                entityId = currentEntityId;
                structureId = currentStructureId;
                player = new PlayerJson(game.playerList[0]);
            }
            public SettingsJson() { }
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
            public DimensionJson() { }
        }
        public class MegaChunk
        {
            public (int x, int y) pos;
            public bool doneGeneratingStructures = false;
            public List<(int x, int y, long seed, ((int type, int subType, int subSubType), bool isNest)? forceType)> structuresLeftToGenerate;
            public List<int> structures = new List<int>();
            [NonSerialized] public bool isImmuneToUnloading = false;
            [NonSerialized] public Screens.Screen screen;
            public MegaChunk(Screens.Screen screenToPut, (int x, int y) posToPut)
            {
                pos = posToPut;
                screen = screenToPut;
            }
            public MegaChunk()
            {

            }
            public bool generateStructuresInTheBackground() // returns true if done generating
            {
                if (doneGeneratingStructures) { return true; }
                if (structuresLeftToGenerate == null) { createStructuresToGenerate(); }
                for (int i = 0; i < 1; i++)
                {
                    if (generateNextStructure()) { doneGeneratingStructures = true; break; }
                }
                saveMegaChunk(this);
                return doneGeneratingStructures;
            }
            public void promoteFromExtraToFullyLoaded() // Load all stuff in it (structures/nest -> and so their chunks if they're dynamic)
            {
                if (!doneGeneratingStructures)
                {
                    if (structuresLeftToGenerate == null) { createStructuresToGenerate(); }
                    forceGenerateAllStructures();
                    saveMegaChunk(this);
                }
                if (loadAllStructures()) { saveMegaChunk(this); }
            }
            public void unloadAllNestsAndStructuresAndChunks(Dictionary<(int x, int y), bool> chunksToRemove)
            {
                (int x, int y) playerPos = (ChunkIdx(screen.game.playerList[0].posX), ChunkIdx(screen.game.playerList[0].posY));
                Structure structure;
                foreach (int structureId in structures)
                {
                    if (screen.activeStructures.ContainsKey(structureId))
                    {
                        structure = screen.activeStructures[structureId];
                        structure.isImmuneToUnloading = false;
                        structure.saveStructure();
                        foreach ((int x, int y) pososo in screen.activeStructures[structureId].chunkPresence.Keys) // this unloads the chunks in nests that are getting unloaded, as the magachunk would get reloaded again if not as they wouldn't be counted as nest loaded chunks anymore
                        {
                            if (Distance(pososo, playerPos) * 32 > 1.6f * screen.game.effectiveRadius) { chunksToRemove[pososo] = true; }
                        }
                        screen.activeStructures.Remove(structureId);
                    }
                    if (screen.inertStructures.ContainsKey(structureId))
                    {
                        screen.inertStructures.Remove(structureId);
                    }
                }
            }
            public bool loadAllStructures()
            {
                bool triedLoadingErasedStructures = false;
                List<int> structuresToRemove = new List<int>();
                Structure structure;
                foreach (int structureId in structures)
                {
                    if (screen.inertStructures.ContainsKey(structureId) || screen.activeStructures.ContainsKey(structureId)) { continue; }  // don't load if already in the dicts
                    structure = loadStructure(screen.game, structureId); // loadStructure already adds the structure to the right dicto so no need to do it here
                    if (structure.isErasedFromTheWorld)
                    {
                        
                        triedLoadingErasedStructures = true;    // This happens if structures were incorrectly deleted from MegaChunks. Not a big deal as this takes care of it, but still indicative of a problem somewhere
                        structuresToRemove.Add(structureId);
                    }
                }
                foreach(int structureId in structuresToRemove) { structures.Remove(structureId); }
                return triedLoadingErasedStructures;
            }
            public void forceGenerateAllStructures() { while (!generateNextStructure()); doneGeneratingStructures = true; }
            public bool generateNextStructure() // we assume that structuresLeftToGenerate is not null. Returns true is the megaChunk has generated all the structures it needed and is now complete
            {
                if (structuresLeftToGenerate.Count == 0) { structuresLeftToGenerate = null; return true; }
                (int x, int y, long seed, ((int type, int subType, int subSubType), bool isNest)? forceType) item = structuresLeftToGenerate[0];
                structuresLeftToGenerate.RemoveAt(0);
                if (item.forceType is null) { new Structure(screen, (item.x, item.y), item.seed, null); }
                else if (!item.forceType.Value.isNest) { new Structure(screen, (item.x, item.y), item.seed, item.forceType.Value.Item1); }
                else { new Nest(screen, (item.x, item.y), item.seed); }
                return false;
            }
            public void createStructuresToGenerate()
            {
                structuresLeftToGenerate = new List<(int x, int y, long seed, ((int type, int subType, int subSubType), bool isNest)? forceType)>();
                if (loadStructuresYesOrNo)
                {
                    int validatedStructureCount = 0;
                    int totalStructureTestedCount = 0;

                    long seed = LCGxy((pos, screen.id), screen.seed);   // the screen.id might cause stupid shite l8r. . . .. such as dimensions varying depending on ID ?? which might be something good actually idk ??
                    if (seed > 0) {; }

                    int structuresAmount = (int)(seed % 3 + 1);
                    totalStructureTestedCount += structuresAmount;
                    for (int i = 0; i < structuresAmount; i++)
                    {                           // Don't mind    /\_/\
                        seed = LCGxPos(seed);   // the mogege   ( ^o^ )
                        // if (!new Structure(screen, (pos.x * 512 + 32 + (int)(seedX % 480), pos.y * 512 + 32 + (int)(seedY % 480)), (seedX, seedY), null).isErasedFromTheWorld) { validatedStructureCount++; };
                        structuresLeftToGenerate.Add((pos.x * 512 + 32 + (int)(seed % 480), pos.y * 512 + 32 + (int)(LCGxNeg(seed) % 480), seed, null));
                    }
                    int waterLakesAmount = (int)(15 + seed % 150);
                    totalStructureTestedCount += waterLakesAmount;
                    for (int i = 0; i < waterLakesAmount; i++)
                    {                           // Don't mind    /\_/\
                        seed = LCGxPos(seed);   // the mogege   ( ^o^ )
                        // if (!new Structure(screen, (pos.x * 512 + 32 + (int)(seedX % 480), pos.y * 512 + 32 + (int)(seedY % 480)), (seedX, seedY), (0, 0, 0)).isErasedFromTheWorld) { validatedStructureCount++; };
                        structuresLeftToGenerate.Add((pos.x * 512 + 32 + (int)(seed % 480), pos.y * 512 + 32 + (int)(LCGxNeg(seed) % 480), seed, ((0, 0, 0), false)));
                    }
                    int nestAmount = (int)(seed % 3);
                    totalStructureTestedCount += nestAmount;
                    if (!spawnNests) { nestAmount = 0; }
                    for (int i = 0; i < nestAmount; i++)
                    {                           // Don't mind    /\_/\
                        seed = LCGxPos(seed);   // the mogege   ( ^o^ )
                        // if (!new Nest(screen, (pos.x * 512 + 32 + (int)(seedX % 480), pos.y * 512 + 32 + (int)(seedY % 480)), (long)(seedX * 0.5f + seedY * 0.5f)).isErasedFromTheWorld) { validatedStructureCount++; }
                        structuresLeftToGenerate.Add((pos.x * 512 + 32 + (int)(seed % 480), pos.y * 512 + 32 + (int)(LCGxNeg(seed) % 480), seed, ((0, 0, 0), true)));
                    }
                    /*if (posX == 0 && posY == 0) // to have a nest spawn at (0, 0) for testing shit
                    {
                        Nest nesto = new Nest((0, 0), (long)(seedX * 0.5f + seedY * 0.5f), this);
                        if (!nesto.isNotToBeAdded)
                        {
                            megaChunk.nests.Add(nesto.id);
                            activeNests[nesto.id] = nesto;
                            validatedStructureCount++;
                        }
                        totalStructureTestedCount++;
                    }*/
                    screen.game.structureGenerationLogs.Add($"-- Created {validatedStructureCount}/{totalStructureTestedCount} structures at Dimension {screen.id}, MegaChunk ({pos.x}, {pos.y}) --\n");
                }
            }
        }
        public class ChunkJson
        {
            public long seed;
            public (int, int) pos;
            public int[,] fill1;
            public int[,] fill2;
            public bool mt;
            public List<int> eLst;
            public List<int> pLst;
            public int explLvl;
            public bool[,] fog;
            public ChunkJson(Chunk chunk)
            {
                seed = chunk.chunkSeed;
                pos = chunk.pos;
                (int[,] one, int[,] two) returnTuple = ChunkToChunkJsonfillStates(chunk.fillStates);
                fill1 = returnTuple.one;
                fill2 = returnTuple.two;
                mt = chunk.isMature;
                eLst = new List<int>();
                foreach (Entity entity in chunk.entityList)
                {
                    saveEntity(entity);
                    eLst.Add(entity.id);
                }
                chunk.entityList = new List<Entity>();
                pLst = new List<int>();
                foreach (Plant plant in chunk.plants.Values)
                {
                    savePlant(plant);
                    pLst.Add(plant.id);
                }
                chunk.plants = new Dictionary<int, Plant>();
                explLvl = chunk.explorationLevel;
                if (explLvl == 1)
                {
                    fog = chunk.fogOfWar;
                }
                else { fog = null; }
            }
            public ChunkJson() { }
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
            public PlayerJson() { }
        }
        public class EntityJson
        {
            public int seed;
            public int id;
            public int nstId;
            public (int, int) type;
            public int state;
            public (float, float) pos;
            public (int, int) hPos;
            public (int, int)? tPos;
            public (float, float) speed;
            public int[,] inv;
            public (float, float) lastDP;
            public float hp;
            public float brth;
            public float sttCh;
            public float tp;
            public EntityJson(Entity entity)
            {
                seed = entity.seed;
                id = entity.id;
                if (entity.nest == null) { nstId = -1; }
                else { nstId = entity.nest.id; }
                type = entity.type;
                state = entity.state;
                hPos = entity.homePosition;
                pos = (entity.realPosX, entity.realPosY);
                if (entity.isCurrentlyMovingTowardsTarget) { tPos = null; }
                tPos = entity.targetPos;
                speed = (entity.speedX, entity.speedY);
                inv = inventoryToArray(entity.inventoryQuantities, entity.inventoryElements);
                lastDP = (entity.timeAtLastDig, entity.timeAtLastPlace);
                hp = entity.hp;
                brth = entity.timeAtBirth;
                sttCh = entity.timeAtLastStateChange;
                tp = entity.timeAtLastTeleportation;
            }
            public EntityJson() { }
        }
        public class PlantJson
        {
            public int seed;
            public int rand;
            public int id;
            public (int, int) type;
            public (int, int) pos;
            public int grLvl;
            public float lastGr;

            public PlantElementJson pE;
            public PlantJson(Plant plant)
            {
                seed = plant.seed;
                rand = plant.randValue;
                id = plant.id;
                type = plant.type;
                pos = (plant.posX, plant.posY);
                grLvl = plant.growthLevel;
                lastGr = plant.timeAtLastGrowth;
                pE = new PlantElementJson(plant.plantElement);
            }
            public PlantJson() { }
        }
        public class PlantElementJson
        {
            public int s;
            public (int, int, int) t;

            public float mG;
            public int gR;

            public (int, int) pos;

            public (int, int) lGP;
            public (int, int) gD;
            public (int, int) bD;

            public int[,] fS;
            public int[] oIA;   // offsets indexes array (for all the little ints)

            public List<PlantElementJson> pEs;
            public PlantElementJson(PlantElement plantElement)
            {
                s = plantElement.seed;
                t = plantElement.type;
                pos = plantElement.pos;
                lGP = plantElement.lastDrawPos;
                gD = plantElement.growthDirection;
                bD = plantElement.baseDirection;
                mG = plantElement.maxGrowthLevel;
                gR = plantElement.growthLevel;
                fS = fillstatesToArray(plantElement.fillStates);
                oIA = new int[8] { plantElement.currentFrameArrayIdx, plantElement.frameArrayOffset, plantElement.currentChildArrayIdx, plantElement.childArrayOffset, plantElement.currentDirectionArrayIdx, plantElement.directionArrayOffset, plantElement.currentModArrayIdx, plantElement.modArrayOffset };
                pEs = new List<PlantElementJson>();
                foreach (PlantElement childPlantElement in plantElement.childPlantElements)
                {
                    pEs.Add(new PlantElementJson(childPlantElement));
                }
            }
            public PlantElementJson() { }
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
            public RoomJson() { }
        }
        public class StructureJson
        {
            public int c;   // for class, if it's a normal Structure, or a Nest etc...
            public int id;
            public int dim;
            public (int, int, int) type;
            public bool isD;
            public bool isE;
            public long seed;
            public (int, int) pos;
            public (int, int) size;
            public string name;
            public float brth;
            public int state;
            public int sis;

            public int[,] fS;
            public StructureJson(Structure structure)
            {
                setAllStructureJsonVariables(structure);
            }
            public void setAllStructureJsonVariables(Structure structure)
            {
                c = structure.setClassTypeInJson();
                id = structure.id;
                dim = structure.screen.id;
                type = structure.type;
                isD = structure.isDynamic;
                isE = structure.isErasedFromTheWorld;
                seed = structure.seed;
                pos = structure.pos;
                size = structure.size;
                name = structure.name;
                brth = structure.timeAtBirth;
                state = structure.state;
                if (structure.sisterStructure != null) { sis = structure.sisterStructure.id; }
                else { sis = -1; }
                fS = fillstatesToArray(structure.structureDict);
            }
            public StructureJson() { }
        }
        public class NestJson : StructureJson
        {
            public RoomJson[] rooms;
            public int roomId = 0;
            public bool stable = false;

            public int[] ent;
            public NestJson(Nest nest)
            {
                setAllStructureJsonVariables(nest);

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
            public NestJson() { }
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
        public static TileTraits[,] ChunkJsonToChunkfillStates(int[,] array1, int[,] array2)                                                                                                                                                                     
        {
            TileTraits[,] arrayo = new TileTraits[32, 32];
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    arrayo[i, j] = getTileTraits((array1[i, j], array2[i, j]));
                }
            }
            return arrayo;
        }
        public static (int[,], int[,]) ChunkToChunkJsonfillStates(TileTraits[,] array)
        {
            int[,] arrayo1 = new int[32, 32];
            int[,] arrayo2 = new int[32, 32];
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    arrayo1[i, j] = array[i, j].type.type;
                    arrayo2[i, j] = array[i, j].type.subType;
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
        public static void saveMegaChunk(MegaChunk megaChunk)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\CaveData\\{worldSeed}\\MegaChunkData\\{megaChunk.screen.id}\\{megaChunk.pos.x}.{megaChunk.pos.y}.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, megaChunk);
            }
        }
        public static MegaChunk loadMegaChunk(Screens.Screen screenToPut, (int x, int y) pos, bool isExtraLoading)
        {
            // --- IMPROVEMENT TO MAKE --- When CREATING a new megachunk, it loads all structure, to unload all of them, to reload them... a bit useless... TBH it might actually already not unload the structures LMFAO but idk
            MegaChunk newMegaChunk;
            if (!System.IO.File.Exists($"{currentDirectory}\\CaveData\\{worldSeed}\\MegaChunkData\\{screenToPut.id}\\{pos.x}.{pos.y}.json"))
            {
                newMegaChunk = new MegaChunk(screenToPut, pos);
                saveMegaChunk(newMegaChunk);
            }
            else
            {
                using (StreamReader f = new StreamReader($"{currentDirectory}\\CaveData\\{worldSeed}\\MegaChunkData\\{screenToPut.id}\\{pos.x}.{pos.y}.json"))
                {
                    string content = f.ReadToEnd();
                    newMegaChunk = JsonConvert.DeserializeObject<MegaChunk>(content);
                    newMegaChunk.screen = screenToPut;
                    newMegaChunk.pos = pos;
                }
            }
            if (isExtraLoading && !screenToPut.megaChunks.ContainsKey(pos)) { screenToPut.extraLoadedMegaChunks[pos] = newMegaChunk; }
            else
            {
                screenToPut.megaChunks[pos] = newMegaChunk;
                newMegaChunk.promoteFromExtraToFullyLoaded();
            }
            return newMegaChunk;
        }
        public static void saveChunk(Chunk chunk)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\CaveData\\{chunk.screen.game.seed}\\ChunkData\\{chunk.screen.id}\\{chunk.pos.x}.{chunk.pos.y}.json"))
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
        public static ChunkJson getChunkJson(Screens.Screen screen, (int x, int y) pos)
        {
            if (!System.IO.File.Exists($"{currentDirectory}\\CaveData\\{worldSeed}\\ChunkData\\{screen.id}\\{pos.x}.{pos.y}.json")) { return null; }
            using (StreamReader f = new StreamReader($"{currentDirectory}\\CaveData\\{worldSeed}\\ChunkData\\{screen.id}\\{pos.x}.{pos.y}.json"))
            {
                return JsonConvert.DeserializeObject<ChunkJson>(f.ReadToEnd());
            }
        }
        public static Chunk loadChunk(Screens.Screen screen, (int x, int y) pos, bool isExtraLoading)
        {
            bool firstLoading = false;
            Chunk newChunk;
            ChunkJson chunkJson = null;
            if (!System.IO.File.Exists($"{currentDirectory}\\CaveData\\{worldSeed}\\ChunkData\\{screen.id}\\{pos.x}.{pos.y}.json"))
            {
                firstLoading = true;
                newChunk = new Chunk(screen, pos);
            }
            else
            {
                chunkJson = getChunkJson(screen, pos);
                newChunk = new Chunk(screen, chunkJson);
            }
            if (isExtraLoading) { screen.extraLoadedChunks[pos] = newChunk; }
            else
            {
                screen.loadedChunks[pos] = newChunk;
                newChunk.promoteFromExtraToFullyLoaded(chunkJson);
            }
            if (firstLoading) { saveChunk(newChunk); }
            return newChunk;
        }
        public static Plant loadPlant(Screens.Screen screenToPut, int plantId)
        {
            if (screenToPut.activePlants.ContainsKey(plantId)) { return screenToPut.activePlants[plantId]; }
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
            if (!plant.hasBeenModified) { return; }
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            PlantJson plantJson = new PlantJson(plant);

            using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\CaveData\\{plant.screen.game.seed}\\PlantData\\{plant.id}.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, plantJson);
            }
            plant.hasBeenModified = false;
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
        public static Structure loadStructure(Game game, int id)
        {
            using (StreamReader f = new StreamReader($"{currentDirectory}\\CaveData\\{game.seed}\\StructureData\\{id}.json"))
            {
                string content = f.ReadToEnd();
                string classType = getSingleValueFromJsonString(content, "c");
                if (classType == "0")
                {
                    StructureJson structureJson = JsonConvert.DeserializeObject<StructureJson>(content);
                    return new Structure(game, structureJson);
                }
                else if (classType == "1")
                {
                    NestJson structureJson = JsonConvert.DeserializeObject<NestJson>(content);
                    return new Nest(game, structureJson);
                }
                return null;
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
            if (!Directory.Exists($"{currentDirectory}\\CaveData\\{seed}\\ChunkNoise"))
            {
                Directory.CreateDirectory($"{currentDirectory}\\CaveData\\{seed}\\ChunkNoise");
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
            if (!Directory.Exists($"{currentDirectory}\\CaveData\\bitmapos"))
            {
                Directory.CreateDirectory($"{currentDirectory}\\CaveData\\bitmapos");
            }
        }
        public static string getSingleValueFromPath(string path, string valueToGet)
        {
            using (StreamReader f = new StreamReader(path))
            {
                string content = f.ReadToEnd();
                return (string)JObject.Parse(content)[valueToGet];
            }
        }
        public static string getSingleValueFromJsonString(string jsonString, string valueToGet)
        {
            return (string)JObject.Parse(jsonString)[valueToGet];
        }
        public static void updateStructureLogFile(Game game)
        {
            if (game.structureGenerationLogs.Count == 0 && game.structureGenerationLogsStructureUpdateCount.Count == 0) { return; }
            string path = $"{currentDirectory}\\CaveData\\{game.seed}\\StructureGenerationLog.txt";
            string text = "";

            if (System.IO.File.Exists(path)) { text = File.ReadAllText(path); }
            foreach (string stringo in game.structureGenerationLogs) { text = text + stringo; }

            foreach ((int dim, int x, int y) pos in game.structureGenerationLogsStructureUpdateCount.Keys)
            {
                text = text + $"    - {game.structureGenerationLogsStructureUpdateCount[pos]} new structures added in Dimension {pos.dim}, MegaChunk ({pos.x}, {pos.y})\n";
            }

            using (StreamWriter writetext = new StreamWriter(path))
            {
                writetext.Write(text + "\n");
            }
            game.structureGenerationLogs = new List<string>();
            game.structureGenerationLogsStructureUpdateCount = new Dictionary<(int dim, int x, int y), int>();
        }
    }
}
