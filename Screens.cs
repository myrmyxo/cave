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
using System.Linq.Expressions;

namespace Cave
{
    public class Screens
    {
        public class Game
        {
            public Dictionary<int, Screen> loadedScreens = new Dictionary<int, Screen>();
            public List<Player> playerList = new List<Player>();
            public Bitmap overlayBitmap;
            public long seed;
            public int livingDimensionId = -1;

            public bool isLight = true;
            public Game()
            {
                bool randomSeed = true;
                seed = 123456;

                int idToPut = 0;
                (int type, int subType) forceBiome = (0, 0);
                int PNGsize = 150;
                PNGsize = 100;

                bool isMonoeBiomeToPut = false;
                bool isPngToExport = false;

                loadStructuresYesOrNo = true;
                spawnEntities = true;
                spawnPlants = true;

                if (randomSeed)
                {
                    seed = rand.Next(1000000);
                    int counto = rand.Next(1000);
                    while (counto > 0)
                    {
                        seed = LCGxPos(seed);
                        counto -= 1;
                    }
                }
                worldSeed = seed;
                Files.createFolders(seed);

                playerList = new List<Player>();
                overlayBitmap = new Bitmap(512, 128);

                SettingsJson settings;
                settings = tryLoadSettings(this);
                applySettings(settings);

                playerList.Add(new Player(settings));
                Player player = playerList[0];

                timeElapsed = 0;

                // to make biome diagrams for testinggggg       order : temp, humi, acid, toxi
                for (int i = 0; i < 0; i++)
                {
                    int a = rand.Next(4);
                    int b = 0;
                    while (b == a) { b = rand.Next(4); }
                    int c = rand.Next(1024);
                    int d = rand.Next(1024);
                    makeBiomeDiagram((0, 0), (a, b), (c, d));
                }
                for (int i = -512; i < -1000/*1536*/; i += 64)
                {
                    makeBiomeDiagram((0, 0), (0, 1), (512, i));
                }

                if (settings != null)
                {
                    idToPut = settings.player.currentDimension;
                }

                if (isPngToExport)
                {
                    debugMode = true;
                    int oldChunkLength = ChunkLength;
                    ChunkLength = PNGsize;
                    loadDimension(idToPut, true, isMonoeBiomeToPut, forceBiome.type, forceBiome.subType);
                    timeAtLauch = DateTime.Now;

                    runGame(null, null);

                    loadedScreens[idToPut].updateScreen().Save($"{currentDirectory}\\caveMap.png");
                    loadedScreens.Remove(idToPut);
                    ChunkLength = oldChunkLength;
                }

                loadDimension(idToPut, false, isMonoeBiomeToPut, forceBiome.type, forceBiome.subType);
                setPlayerDimension(player, idToPut);
            }
            public void movePlayerStuff(Screen screen, Player player)
            {
                if (inventoryChangePress[0]) { inventoryChangePress[0] = false; player.moveInventoryCursor(-1); }
                if (inventoryChangePress[1]) { inventoryChangePress[1] = false; player.moveInventoryCursor(1); }

                player.movePlayer();
                screen.checkStructures(player);

                screen.chunkX = ChunkIdx(player.camPosX);
                screen.chunkY = ChunkIdx(player.camPosY);
            }
            public void applySettings(SettingsJson settings)
            {
                if (settings == null) { return; }
                timeElapsed = settings.time;
                currentStructureId = settings.structureId;
                currentEntityId = settings.entityId;
                currentPlantId = settings.plantId;
                currentNestId = settings.nestId;
                currentDimensionId = settings.dimensionId;
                livingDimensionId = settings.livingDimensionId;
            }
            public void runGame(PictureBox gamePictureBox, PictureBox overlayPictureBox)
            {
                liquidSlideCount = 0;

                Player player = playerList[0];

                if (pausePress) { return; }
                if (dimensionChangePress)
                {
                    if (dimensionSelection)
                    {
                        dimensionSelection = false;
                        setPlayerDimension(player, currentTargetDimension);
                    }
                    else { dimensionSelection = true; }
                    dimensionChangePress = false;
                }
                if (craftPress)
                {
                    if (craftSelection)
                    {
                        craftSelection = false;
                    }
                    else { craftSelection = true; }
                    craftPress = false;
                }

                foreach (Screen screen in loadedScreens.Values.ToArray())
                {
                    int framesFastForwarded = 0;
                LoopStart:;
                    timeElapsed += 0.02f;
                    screen.extraLoadedChunks.Clear(); // this will make many bugs
                    screen.liquidsThatCantGoLeft = new Dictionary<(int, int), bool>();
                    screen.liquidsThatCantGoRight = new Dictionary<(int, int), bool>();
                    screen.entitesToRemove = new Dictionary<int, Entity>();
                    screen.entitesToAdd = new Dictionary<int, Entity>();
                    screen.particlesToRemove = new Dictionary<Particle, bool>();
                    screen.particlesToAdd = new List<Particle>();
                    screen.structuresToAdd = new Dictionary<int, Structure>();
                    screen.structuresToRemove = new Dictionary<int, Structure>();
                    screen.attacksToDo = new List<((int x, int y) pos, (int type, int subType) attack)>();
                    screen.attacksToDraw = new List<((int x, int y) pos, Color color)>();
                    if (zoomPress[0] && timeElapsed > lastZoom + 0.25f) { screen.zoom(true); }
                    if (zoomPress[1] && timeElapsed > lastZoom + 0.25f) { screen.zoom(false); }
                    if (player.dimension == screen.id)
                    {
                        if (dimensionSelection && player.timeAtLastMenuChange + 0.2f < timeElapsed)
                        {
                            if (arrowKeysState[0] || arrowKeysState[2]) { currentTargetDimension--; player.timeAtLastMenuChange = timeElapsed; }
                            if (arrowKeysState[1] || arrowKeysState[3]) { currentTargetDimension++; player.timeAtLastMenuChange = timeElapsed; }
                        }
                        else if (craftSelection && player.timeAtLastMenuChange + 0.2f < timeElapsed)
                        {
                            if (arrowKeysState[0] || arrowKeysState[2]) { player.moveCraftCursor(-1); player.timeAtLastMenuChange = timeElapsed; }
                            if (arrowKeysState[1] || arrowKeysState[3]) { player.moveCraftCursor(1); player.timeAtLastMenuChange = timeElapsed; }
                        }
                        movePlayerStuff(screen, player); // move player, load new chunks, test craft, and stuff
                        screen.updateLoadedChunks();
                    }

                    foreach (Entity entity in screen.entitesToRemove.Values)
                    {
                        screen.activeEntities.Remove(entity.id);
                    }
                    foreach (Entity entity in screen.entitesToAdd.Values)
                    {
                        screen.activeEntities[entity.id] = entity;
                    }

                    if (timeElapsed > 3 && screen.activeNests.Count > 0)
                    {
                        Nest nestToTest = screen.activeNests.Values.ToArray()[rand.Next(screen.activeNests.Count)];
                        if (rand.Next(100) == 0) { nestToTest.isStable = false; }
                        if (!nestToTest.isStable && nestToTest.digErrands.Count == 0)
                        {
                            nestToTest.randomlyExtendNest();
                        }
                    }

                    foreach ((int x, int y) pos in screen.chunksToSpawnEntitiesIn.Keys)
                    {
                        if (screen.loadedChunks.ContainsKey(pos))
                        {
                            screen.loadedChunks[pos].spawnEntities();
                        }
                    }
                    screen.chunksToSpawnEntitiesIn = new Dictionary<(int x, int y), bool>();

                    List<int> orphansToRemove = new List<int>();
                    foreach (int entityId in screen.orphanEntities.Keys)    // add entities that were loaded when nests were not loaded if possible
                    {
                        if (!screen.activeEntities.ContainsKey(entityId))
                        {
                            orphansToRemove.Add(entityId);
                            continue;
                        }
                        Entity entityToTest = screen.activeEntities[entityId];
                        if (screen.activeNests.ContainsKey(entityToTest.nestId))
                        {
                            entityToTest.nest = screen.activeNests[entityToTest.nestId];
                            orphansToRemove.Add(entityId);
                        }
                    }
                    foreach (int entityId in orphansToRemove)
                    {
                        screen.orphanEntities.Remove(entityId);
                    }


                    screen.entitesToRemove = new Dictionary<int, Entity>();
                    screen.entitesToAdd = new Dictionary<int, Entity>();
                    foreach (Entity entity in screen.activeEntities.Values)
                    {
                        entity.moveEntity();
                    }
                    foreach (Entity entity in screen.entitesToRemove.Values)
                    {
                        screen.activeEntities.Remove(entity.id);
                    }
                    foreach (Entity entity in screen.entitesToAdd.Values)
                    {
                        screen.activeEntities[entity.id] = entity;
                    }
                    foreach (Plant plant in screen.activePlants.Values)
                    {
                        plant.testPlantGrowth(false);
                    }
                    foreach (Particle particle in screen.activeParticles)
                    {
                        particle.moveParticle();
                    }
                    foreach (Structure structure in screen.activeStructures.Values)
                    {
                        structure.moveStructure();
                    }
                    foreach ((int x, int y) pos in screen.loadedChunks.Keys)
                    {
                        if (rand.Next(200) == 0) { screen.loadedChunks[(pos.x, pos.y)].unstableLiquidCount++; }
                        screen.loadedChunks[(pos.x, pos.y)].moveLiquids();
                    }


                    screen.entitesToRemove = new Dictionary<int, Entity>();
                    screen.entitesToAdd = new Dictionary<int, Entity>();
                    screen.putEntitiesAndPlantsInChunks();


                    // attack shit
                    foreach (((int x, int y) pos, (int type, int subType) attack) attack in screen.attacksToDo)
                    {
                        player.sendAttack(attack);
                    }
                    if (player.willBeSetAsNotAttacking) { player.setAsNotAttacking(); }

                    foreach (Structure structure in screen.structuresToAdd.Values)
                    {
                        (int x, int y) megaChunkPos = MegaChunkIdxFromPixelPos(structure.pos.x, structure.pos.y);
                        if (!screen.megaChunks.ContainsKey(megaChunkPos)) { screen.megaChunks[megaChunkPos] = loadMegaChunk(screen, megaChunkPos); }
                        MegaChunk megaChunk = screen.megaChunks[megaChunkPos];
                        foreach (int id in megaChunk.structures) // this checks is there is already a structure of the same type overlapping with the chunks of the new one (not to make duplicates for portals n shite)
                        {
                            if (!screen.activeStructures.ContainsKey(id)) { continue; }
                            Structure structo = screen.activeStructures[id];
                            if (structo.type != structure.type) { continue; }
                            foreach ((int x, int y) pos in structo.chunkPresence.Keys)
                            {
                                if (structure.chunkPresence.ContainsKey(pos)) { goto doNotAddStructure; }
                            }
                        }
                        // after this the structure is VALID and WILL be added to existing structures
                        screen.activeStructures[structure.id] = structure;
                        structure.initAfterStructureValidated();
                        megaChunk.structures.Add(structure.id);
                        saveMegaChunk(megaChunk, megaChunkPos, screen.id);
                    doNotAddStructure:;
                    }
                    foreach (Structure structure in screen.structuresToRemove.Values)
                    {
                        structure.EraseFromTheWorld();
                    }



                    screen.unloadFarawayChunks();
                    screen.manageMegaChunks();

                    screen.removeEntitiesAndPlantsFromChunks(true);
                    foreach (Entity entity in screen.entitesToRemove.Values)
                    {
                        screen.activeEntities.Remove(entity.id);
                    }
                    foreach (Entity entity in screen.entitesToAdd.Values)
                    {
                        screen.activeEntities[entity.id] = entity;
                    }
                    foreach (Particle particle in screen.particlesToAdd)
                    {
                        screen.activeParticles.Add(particle);
                    }
                    foreach (Particle particle in screen.particlesToRemove.Keys)
                    {
                        screen.activeParticles.Remove(particle);
                    }
                    screen.particlesToRemove = new Dictionary<Particle, bool>();
                    screen.particlesToAdd = new List<Particle>();

                    if (fastForward && framesFastForwarded < 10)
                    {
                        framesFastForwarded++;
                        goto LoopStart;
                    }

                    if (player.dimension == screen.id && !screen.isPngToBeExported)
                    {
                        gamePictureBox.Image = screen.updateScreen();
                        gamePictureBox.Refresh();
                        overlayPictureBox.Image = overlayBitmap;
                        Sprites.drawSpriteOnCanvas(overlayBitmap, overlayBackground.bitmap, (0, 0), 4, false);
                        if (dimensionSelection) { drawNumber(overlayBitmap, currentTargetDimension, (200, 64), 4, true); }
                        else if (craftSelection) { drawCraftRecipe(this, craftRecipes[player.craftCursor]); }
                        drawInventory(player.screen.game, player.inventoryQuantities, player.inventoryElements, player.inventoryCursor);
                        overlayPictureBox.Refresh();
                    }
                }
                saveSettings(this);

                int gouga = liquidSlideCount;
                gouga = gouga + 1 - 1;
            }
            public void renderScreen()
            {

            }
            public void setPlayerDimension(Player player, int targetDimension)
            {
                if (targetDimension > currentDimensionId) { targetDimension = currentDimensionId; Globals.currentTargetDimension = currentDimensionId; }
                if (!loadedScreens.ContainsKey(targetDimension))
                {
                    loadDimension(targetDimension);
                }
                player.screen = loadedScreens[targetDimension];
                player.dimension = targetDimension;
                player.screen.checkStructuresOnSpawn(player);
                unloadAllDimensions(false);
            }
            public Screen loadDimension(int idToLoad, bool isPngToExport = false, bool isMonoToPut = false, int typeToPut = -999, int subTypeToPut = -999)
            {
                if (!loadedScreens.ContainsKey(idToLoad))
                {
                    Screen newScreen = new Screen(this, ChunkLength, idToLoad, isPngToExport, isMonoToPut, typeToPut, subTypeToPut);
                    loadedScreens[idToLoad] = newScreen;
                    return newScreen;
                }
                return loadedScreens[idToLoad];
            }
            public void unloadAllDimensions(bool unloadDimensionPlayerIsInAsWell)
            {
                Screen screen;
                List<int> screensToRemove = new List<int>();
                foreach (int id in loadedScreens.Keys)
                {
                    if (unloadDimensionPlayerIsInAsWell || playerList[0].dimension != id)
                    {
                        screen = loadedScreens[id];
                        screen.putEntitiesAndPlantsInChunks();
                        saveAllChunks(screen);
                        screensToRemove.Add(id);
                    }
                }
                foreach (int id in screensToRemove)
                {
                    loadedScreens.Remove(id);
                }
            }
        }
        public class Screen
        {
            public Game game;

            public Dictionary<(int x, int y), bool> chunksToSpawnEntitiesIn = new Dictionary<(int x, int y), bool>();
            public Dictionary<(int x, int y), Chunk> loadedChunks = new Dictionary<(int x, int y), Chunk>();
            public Dictionary<(int x, int y), Chunk> extraLoadedChunks = new Dictionary<(int x, int y), Chunk>();
            public Dictionary<(int x, int y), MegaChunk> megaChunks = new Dictionary<(int x, int y), MegaChunk>();

            public List<long>[,] LCGCacheListMatrix;

            public int chunkResolution; // included both loaded and unloaded chunks, side of the square
            public bool isPngToBeExported;

            public long seed;
            public int id;
            public (int type, int subType) type;
            public bool isMonoBiome; // if yes, then the whole-ass dimension is only made ouf of ONE biome, that is of the type of... well type. If not, type is the type of dimension and not the biome (like idk normal, frozen, lampadaire, shitpiss world...)

            public Bitmap gameBitmap;

            public Bitmap lightBitmap;

            public (float x, float y) playerStartPos = (0, 0);

            public Dictionary<int, Entity> activeEntities = new Dictionary<int, Entity>();
            public Dictionary<int, Entity> entitesToRemove = new Dictionary<int, Entity>();
            public Dictionary<int, Entity> entitesToAdd = new Dictionary<int, Entity>();
            public Dictionary<int, bool> orphanEntities = new Dictionary<int, bool>();
            public Dictionary<int, Plant> activePlants = new Dictionary<int, Plant>();
            public List<Particle> activeParticles = new List<Particle>();
            public List<Particle> particlesToAdd = new List<Particle>();
            public Dictionary<Particle, bool> particlesToRemove = new Dictionary<Particle, bool>();
            public Dictionary<int, Nest> activeNests = new Dictionary<int, Nest>();
            public Dictionary<int, Structure> inertStructures = new Dictionary<int, Structure>(); // structures that are just terrain and don't need to be tested for shit (lakes, cubes...)
            public Dictionary<int, Structure> activeStructures = new Dictionary<int, Structure>(); // structures that are active and can do shit to other shit (like portals)
            public Dictionary<int, Structure> structuresToAdd = new Dictionary<int, Structure>();
            public Dictionary<int, Structure> structuresToRemove = new Dictionary<int, Structure>();

            public List<((int x, int y) pos, (int type, int subType) attack)> attacksToDo = new List<((int x, int y) pos, (int type, int subType) attack)>();
            public List<((int x, int y) pos, Color color)> attacksToDraw = new List<((int x, int y), Color color)>();

            public Dictionary<(int, int), List<Plant>> outOfBoundsPlants = new Dictionary<(int, int), List<Plant>>(); // not used as of now but in some functions so can't remove LMAO

            public Dictionary<(int, int), bool> liquidsThatCantGoLeft = new Dictionary<(int, int), bool>();
            public Dictionary<(int, int), bool> liquidsThatCantGoRight = new Dictionary<(int, int), bool>();

            public bool initialLoadFinished = false;

            public int chunkX = 0;
            public int chunkY = 0;

            // debug shit
            public Dictionary<(int x, int y), bool> nestLoadedChunkIndexes = new Dictionary<(int x, int y), bool>();
            public Dictionary<(int x, int y), bool> inertStructureLoadedChunkIndexes = new Dictionary<(int x, int y), bool>();
            public Dictionary<(int x, int y), bool> activeStructureLoadedChunkIndexes = new Dictionary<(int x, int y), bool>();

            public Screen(Game gameToPut, int chunkResolutionToPut, int idToPut, bool isPngToExport, bool isMonoToPut = false, int forceType = -999, int forceSubType = -999)
            {
                game = gameToPut;
                id = idToPut;
                DimensionJson dimensionJson = tryLoadDimension(game, id);
                if (dimensionJson != null)
                {
                    type = dimensionJson.type;
                    isMonoBiome = dimensionJson.isMono;
                    seed = dimensionJson.seed;
                }
                else
                {
                    if (forceType != -999)
                    {
                        type = (forceType, forceSubType);
                        isMonoBiome = isMonoToPut;
                    }
                    else
                    {
                        if (rand.Next(2) == 0)
                        {
                            type = biomeDict.Keys.ToArray()[rand.Next(biomeDict.Count)];
                            isMonoBiome = true;
                        }
                        else
                        {
                            type = (rand.Next(3), 0);
                            isMonoBiome = false;
                        }
                    }
                    seed = game.seed + id;
                    currentDimensionId++;
                    if (game.livingDimensionId == -1 && isMonoBiome == false && type == (2, 0)) { game.livingDimensionId = id; }
                }
                saveDimensionData(this);
                isPngToBeExported = isPngToExport;
                chunkResolution = chunkResolutionToPut + UnloadedChunksAmount * 2; // invisible chunks of the sides/top/bottom
                createDimensionFolders(game, id);
                LCGCacheInit();
                Player player = game.playerList[0];
                if (isPngToBeExported) { gameBitmap = new Bitmap(32 * (chunkResolution - 1), 32 * (chunkResolution - 1)); }
                else { gameBitmap = new Bitmap(128 * (ChunkLength - 1), 128 * (ChunkLength - 1)); }
                chunkX = ChunkIdx(player.posX);
                chunkY = ChunkIdx(player.posY);
                if (player.dimension == id) { updateLoadedChunks(); }

                foreach ((int x, int y) pos in chunksToSpawnEntitiesIn.Keys)
                {
                    if (loadedChunks.ContainsKey(pos))
                    {
                        loadedChunks[pos].spawnEntities();
                    }
                }
            }
            public void LCGCacheInit()
            {
                LCGCacheListMatrix = new List<long>[2, 5];
                long longo;
                long longo2;
                for (int i = 0; i < 5; i++)
                {
                    LCGCacheListMatrix[0, i] = new List<long>();
                    LCGCacheListMatrix[1, i] = new List<long>();
                }
                longo = seed;
                longo2 = LCGz(seed);
                for (int j = 0; j < 10000; j += 50)
                {
                    LCGCacheListMatrix[0, 0].Add(longo);
                    LCGCacheListMatrix[1, 0].Add(longo2);
                    longo = LCGxPos(longo);
                    longo2 = LCGxPos(longo);
                }
                longo = seed;
                for (int j = 0; j < 10000; j += 50)
                {
                    LCGCacheListMatrix[0, 1].Add(longo);
                    LCGCacheListMatrix[1, 1].Add(longo2);
                    longo = LCGxNeg(longo);
                    longo2 = LCGxNeg(longo);
                }
                longo = seed;
                for (int j = 0; j < 10000; j += 50)
                {
                    LCGCacheListMatrix[0, 2].Add(longo);
                    LCGCacheListMatrix[1, 2].Add(longo2);
                    longo = LCGyPos(longo);
                    longo2 = LCGyPos(longo);
                }
                longo = seed;
                for (int j = 0; j < 10000; j += 50)
                {
                    if (j % 50 == 0)
                    {
                        LCGCacheListMatrix[0, 3].Add(longo);
                        LCGCacheListMatrix[1, 3].Add(longo2);
                    }
                    longo = LCGyNeg(longo);
                    longo2 = LCGyNeg(longo);
                }
                longo = seed;
                for (int j = 0; j < 10000; j += 50)
                {
                    if (j % 50 == 0)
                    {
                        LCGCacheListMatrix[0, 4].Add(longo);
                        LCGCacheListMatrix[1, 4].Add(longo2);
                    }
                    longo = LCGz(longo);
                    longo2 = LCGz(longo);
                }
            }
            public void addPlantsToChunk(Chunk chunk)
            {
                if (outOfBoundsPlants.ContainsKey((chunk.position.Item1, chunk.position.Item2)))
                {
                    chunk.exteriorPlantList = outOfBoundsPlants[(chunk.position.Item1, chunk.position.Item2)];
                    outOfBoundsPlants.Remove((chunk.position.Item1, chunk.position.Item2));
                }
            }
            public void removePlantsFromChunk(Chunk chunk)
            {
                if (chunk.exteriorPlantList.Count > 0)
                {
                    outOfBoundsPlants.Add((chunk.position.Item1, chunk.position.Item2), new List<Plant>());
                }
                foreach (Plant plant in chunk.exteriorPlantList)
                {
                    outOfBoundsPlants[(chunk.position.Item1, chunk.position.Item2)].Add(plant);
                }
            }
            public void putEntitiesAndPlantsInChunks()
            {
                (int, int) chunkIndex;
                Chunk chunk;
                int id;
                List<Entity> cringeEntities = new List<Entity>();
                int[] entityIdArray = activeEntities.Keys.ToArray();
                for (int i = 0; i < entityIdArray.Length; i++)
                {
                    id = entityIdArray[i];
                    chunkIndex = ChunkIdx(activeEntities[id].posX, activeEntities[id].posY);
                    if (loadedChunks.ContainsKey(chunkIndex))
                    {
                        chunk = loadedChunks[chunkIndex];
                        chunk.entityList.Add(activeEntities[id]);
                        activeEntities.Remove(id);
                    }
                    else
                    {
                        cringeEntities.Add(activeEntities[id]);
                        activeEntities.Remove(id);
                    }
                }
                int[] plantIdArray = activePlants.Keys.ToArray();
                for (int i = 0; i < plantIdArray.Length; i++)
                {
                    id = plantIdArray[i];
                    chunkIndex = ChunkIdx(activePlants[id].posX, activePlants[id].posY);
                    chunk = loadedChunks[chunkIndex];
                    chunk.plantList.Add(activePlants[id]);
                    activePlants.Remove(id);
                }
                foreach (Entity entito in cringeEntities)
                {
                    activeEntities[entito.id] = entito;
                }
            }
            public void removeEntitiesAndPlantsFromChunks(bool reinitializeActivePlantsAndEntities)
            {
                if (reinitializeActivePlantsAndEntities)
                {
                    activeEntities = new Dictionary<int, Entity>();
                    activePlants = new Dictionary<int, Plant>();
                }
                foreach (Chunk chunko in loadedChunks.Values)
                {
                    foreach (Entity entity in chunko.entityList)
                    {
                        activeEntities[entity.id] = entity;
                    }
                    foreach (Plant plant in chunko.plantList)
                    {
                        activePlants[plant.id] = plant;
                    }
                    chunko.entityList = new List<Entity>();
                    chunko.plantList = new List<Plant>();
                }
            }
            public void updateLoadedChunks()
            {
                Chunk newChunk;
                (int x, int y) posToTest;
                for (int i = 0; i < chunkResolution; i++)
                {
                    for (int j = 0; j < chunkResolution; j++)
                    {
                        posToTest = (chunkX + i, chunkY + j);
                        if (!loadedChunks.ContainsKey(posToTest))
                        {
                            newChunk = new Chunk(posToTest, false, this); // this is needed cause uhh yeah idk sometimes loadedChunks is FUCKING ADDED IN AGAIN ???
                            if (!loadedChunks.ContainsKey(posToTest)) { loadedChunks[posToTest] = newChunk; }
                        }
                    }
                }
            }
            public void updateLoadedChunksOld(int screenSlideXtoPut, int screenSlideYtoPut)
            {
                int screenSlideX = screenSlideXtoPut;
                int screenSlideY = screenSlideYtoPut;

                // Okay I've changed shit to dictionary instead of array please don't bug bug please please please... . . Gone ! Forever now ! it's. fucking. BACKKKKKK     !! ! !! GONE AGAIN fuck it's backkkk WOOOOOOOOOHOOOOOOOOOOOO BUG IS GONE !!! It's 4am !!!! FUCK !!!! PROBLEM !!!!! The update loaded chuncks is lagging 8 (7?) chunkcs behind the actual normal loading... but only in the updated dimension

                Dictionary<(int, int), bool> chunksToAdd = new Dictionary<(int, int), bool>();

                int addo = -1;
                if (screenSlideX < 0) { addo = chunkResolution; }
                while (Abs(screenSlideX) > 0)
                {
                    for (int j = chunkY; j < chunkY + chunkResolution; j++)
                    {
                        chunksToAdd[(chunkX + addo + Sign(screenSlideX) * chunkResolution + screenSlideX, j + screenSlideYtoPut)] = true;
                    }
                    screenSlideX = Sign(screenSlideX) * (Abs(screenSlideX) - 1);
                }
                addo = -1;
                if (screenSlideY < 0) { addo = chunkResolution; }
                while (Abs(screenSlideY) > 0)
                {
                    for (int i = chunkX; i < chunkX + chunkResolution; i++)
                    {
                        chunksToAdd[(i + screenSlideXtoPut, chunkY + addo + Sign(screenSlideY) * chunkResolution + screenSlideY)] = true;
                    }
                    screenSlideY = Sign(screenSlideY) * (Abs(screenSlideY) - 1);
                }
                chunkX += screenSlideXtoPut;
                chunkY += screenSlideYtoPut;


                foreach ((int, int) chunkPos in chunksToAdd.Keys)
                {
                    if (!loadedChunks.ContainsKey(chunkPos))
                    {
                        loadedChunks.Add(chunkPos, new Chunk(chunkPos, false, this));
                    }
                    //addPlantsToChunk(loadedChunks[chunkPos]);
                }
            }
            public void unloadFarawayChunks() // this function unloads random chunks, that are not in the always loaded square around the player or in nests. HOWEVER, while the farthest away a chunk is, the less chance it has to unload, it still is random. 
            {
                inertStructureLoadedChunkIndexes = new Dictionary<(int x, int y), bool>();
                foreach (Structure structure in inertStructures.Values)
                {
                    foreach ((int x, int y) tile in structure.chunkPresence.Keys)
                    {
                        inertStructureLoadedChunkIndexes[tile] = true;
                    }
                }

                activeStructureLoadedChunkIndexes = new Dictionary<(int x, int y), bool>();
                foreach (Structure structure in activeStructures.Values)
                {
                    foreach ((int x, int y) tile in structure.chunkPresence.Keys)
                    {
                        activeStructureLoadedChunkIndexes[tile] = true;
                    }
                }

                nestLoadedChunkIndexes = new Dictionary<(int x, int y), bool>();
                foreach (Nest nest in activeNests.Values)
                {
                    foreach ((int x, int y) tile in nest.chunkPresence.Keys)
                    {
                        nestLoadedChunkIndexes[tile] = true;
                    }
                }

                int forceLoadedChunkAmount = nestLoadedChunkIndexes.Count + activeStructureLoadedChunkIndexes.Count;
                int minChunkAmount = chunkResolution * chunkResolution + forceLoadedChunkAmount;

                (int x, int y) cameraChunkIdx = (ChunkIdx(game.playerList[0].posX), ChunkIdx(game.playerList[0].posY));

                Dictionary<(int x, int y), bool> chunksToRemove = new Dictionary<(int x, int y), bool>();
                (int x, int y)[] chunkKeys = loadedChunks.Keys.ToArray();
                (int x, int y) currentIdx;
                float distanceToCenter;
                for (int i = loadedChunks.Count; i > minChunkAmount; i--)
                {
                    currentIdx = chunkKeys[rand.Next(loadedChunks.Count)];
                    distanceToCenter = distance((currentIdx), cameraChunkIdx);
                    if (!nestLoadedChunkIndexes.ContainsKey(currentIdx) && !activeStructureLoadedChunkIndexes.ContainsKey(currentIdx) && (float)(rand.NextDouble()) * distanceToCenter > 0.8f * chunkResolution)
                    {
                        chunksToRemove[currentIdx] = true;
                    }
                }

                actuallyUnloadTheChunks(chunksToRemove);
            }
            public void actuallyUnloadTheChunks(Dictionary<(int x, int y), bool> chunksDict)  // Be careful to have put entities and plants inside the chunk before or else they won't be unloaded !! ! ! ! !
            {
                if (chunksDict.Count > 0 && true)
                {
                    foreach ((int, int) chunkPos in chunksDict.Keys)
                    {
                        if (loadedChunks.ContainsKey(chunkPos))
                        {
                            //removePlantsFromChunk(loadedChunks[chunkPos]);
                            Files.saveChunk(loadedChunks[chunkPos]);
                            loadedChunks.Remove(chunkPos);
                        }
                    }
                }
            }
            public void manageMegaChunks() // megachunk resolution : 512*512
            {
                Dictionary<(int x, int y), MegaChunk> newMegaChunks = new Dictionary<(int x, int y), MegaChunk>();
                (int x, int y) cameraChunkIdx = (ChunkIdx(game.playerList[0].posX), ChunkIdx(game.playerList[0].posY));
                (int x, int y) megaPos;

                // Find all megachunks who have only chunks that are can't be unloaded by unloadFarawayChunks (those in Nests and Active Structures)
                // This shit is CURRENTLY NOT WORKING. FFS.
                /*foreach (MegaChunk megaChunk in megaChunks.Values) { megaChunk.isPurelyStructureLoaded = true; }
                foreach (Nest nest in activeNests.Values) { nest.isPurelyStructureLoaded = true; }
                foreach (Structure structure in activeStructures.Values) { structure.isPurelyStructureLoaded = true; }
                foreach ((int x, int y) pos in loadedChunks.Keys)
                {
                    megaPos = MegaChunkIdx(pos);
                    if (!nestLoadedChunkIndexes.ContainsKey(megaPos) && !activeStructureLoadedChunkIndexes.ContainsKey(megaPos))
                    {
                        if (megaChunks.ContainsKey(megaPos)) { megaChunks[megaPos].isPurelyStructureLoaded = false; }
                    }
                }
                foreach (Nest nest in activeNests.Values) { foreach ((int x, int y) posToTest in nest.megaChunkPresence.Keys) { if (megaChunks.ContainsKey(posToTest) && !megaChunks[posToTest].isPurelyStructureLoaded) {  nest.isPurelyStructureLoaded = false; } } }
                foreach (Structure structure in activeStructures.Values) { foreach ((int x, int y) posToTest in structure.megaChunkPresence.Keys) { if (megaChunks.ContainsKey(posToTest) && !megaChunks[posToTest].isPurelyStructureLoaded) { structure.isPurelyStructureLoaded = false; } } }
                foreach (Nest nest in activeNests.Values) { if (nest.isPurelyStructureLoaded) { foreach ((int x, int y) posToTest in nest.megaChunkPresence.Keys) { newMegaChunks[posToTest] = null; } } }
                foreach (Nest nest in activeNests.Values) { if (nest.isPurelyStructureLoaded) { foreach ((int x, int y) posToTest in nest.megaChunkPresence.Keys) { newMegaChunks[posToTest] = null; } } }
                */

                foreach ((int x, int y) pos in loadedChunks.Keys)
                {
                    if ((nestLoadedChunkIndexes.ContainsKey(pos) || activeStructureLoadedChunkIndexes.ContainsKey(pos)) && distance(pos, cameraChunkIdx) > 0.8f * chunkResolution) { continue; }
                    megaPos = MegaChunkIdx(pos);
                    newMegaChunks[megaPos] = null;
                }
                foreach ((int x, int y) pos in newMegaChunks.Keys.ToArray()) // add new megachunks that were not loaded before and have now chunks in
                {
                    if (megaChunks.ContainsKey(pos))
                    {
                        newMegaChunks[pos] = megaChunks[pos];
                        megaChunks.Remove(pos); // needed for the last foreach don't remove !!!!
                    }
                    else
                    {
                        newMegaChunks[pos] = loadMegaChunk(this, pos);
                        newMegaChunks[pos].loadAllNests(this);
                        newMegaChunks[pos].loadAllStructures(this);
                        newMegaChunks[pos].loadAllChunksInNests(this);
                    }
                }
                Dictionary<(int x, int y), bool> chunksToRemove = new Dictionary<(int x, int y), bool>();
                foreach ((int x, int y) pos in megaChunks.Keys) // remove megachunks that have no chunks loaded in anymore
                {
                    megaChunks[pos].unloadAllNestsAndStructuresAndChunks(this, chunksToRemove);
                    saveMegaChunk(megaChunks[pos], pos, id);
                    actuallyUnloadTheChunks(chunksToRemove);
                }

                megaChunks = newMegaChunks;
            }
            public void checkStructuresOnSpawn(Player player)
            {
                player.CheckStructurePosChange();
                for (int i = -1; i <= 2; i++)
                {
                    for (int j = -1; j <= 2; j++)
                    {
                        createStructures(i + player.structureX, j + player.structureY);
                    }
                }
            }
            public void checkStructures(Player player)
            {
                (int x, int y) oldStructurePos = (player.structureX, player.structureY);
                if (player.CheckStructurePosChange())
                {
                    int changeX = player.structureX - oldStructurePos.x;
                    int changeY = player.structureY - oldStructurePos.y;
                    if (Abs(changeX) > 0)
                    {
                        createStructures(player.structureX + changeX + 1, player.structureY + 2);
                        createStructures(player.structureX + changeX + 1, player.structureY + 1);
                        createStructures(player.structureX + changeX + 1, player.structureY);
                        createStructures(player.structureX + changeX + 1, player.structureY - 1);
                    }
                    if (Abs(changeY) > 0)
                    {
                        createStructures(player.structureX + 2, player.structureY + changeY + 1);
                        createStructures(player.structureX + 1, player.structureY + changeY + 1);
                        createStructures(player.structureX, player.structureY + changeY + 1);
                        createStructures(player.structureX - 1, player.structureY + changeY + 1);
                    }
                }
            }
            public void createStructures(int posX, int posY)
            {
                if (!loadStructuresYesOrNo) { return; }
                if (!System.IO.File.Exists($"{currentDirectory}\\CaveData\\{game.seed}\\MegaChunkData\\{id}\\{posX}.{posY}.json"))
                {
                    MegaChunk megaChunk = new MegaChunk((posX, posY));
                    saveMegaChunk(megaChunk, megaChunk.pos, id);

                    int x = posY % 10 + 15;
                    long seedX = seed + posX;
                    int y = posX % 10 + 15;
                    long seedY = seed + posY;
                    while (x > 0)
                    {
                        seedX = LCGxPos(seedX);
                        x--;
                    }
                    while (y > 0)
                    {
                        seedY = LCGyPos(seedY);
                        y--;
                    }
                    long structuresAmount = (seedX + seedY) % 3 + 1;
                    for (int i = 0; i < structuresAmount; i++)
                    {
                        seedX = LCGyPos(seedX); // on porpoise x    /\_/\
                        seedY = LCGxPos(seedY); // and y switched  ( ^o^ )
                        Structure newStructure = new Structure(this, (posX * 512 + 32 + (int)(seedX % 480), posY * 512 + 32 + (int)(seedY % 480)), (seedX, seedY), (-1, -1, -1));
                        megaChunk.structures.Add(newStructure.id);
                    }
                    long waterLakesAmount = 15 + (seedX + seedY) % 150;
                    for (int i = 0; i < waterLakesAmount; i++)
                    {
                        seedX = LCGyNeg(seedX); // on porpoise x    /\_/\
                        seedY = LCGxNeg(seedY); // and y switched  ( ^o^ )
                        Structure newStructure = new Structure(this, (posX * 512 + 32 + (int)(seedX % 480), posY * 512 + 32 + (int)(seedY % 480)), (seedX, seedY), (0, 0, 0));
                        megaChunk.structures.Add(newStructure.id);
                    }
                    long nestAmount = (seedX + seedY) % 3;
                    //nestAmount = 0;
                    for (int i = 0; i < nestAmount; i++)
                    {
                        seedX = LCGyPos(seedX); // on porpoise x    /\_/\
                        seedY = LCGxPos(seedY); // and y switched  ( ^o^ )
                        Nest nest = new Nest((posX * 512 + 32 + (int)(seedX % 480), posY * 512 + 32 + (int)(seedY % 480)), (long)(seedX * 0.5f + seedY * 0.5f), this);
                        if (!nest.isNotToBeAdded)
                        {
                            megaChunk.nests.Add(nest.id);
                            activeNests[nest.id] = nest;
                        }
                    }
                    /*if (posX == 0 && posY == 0) // to have a nest spawn at (0, 0) for testing shit
                    {
                        Nest nesto = new Nest((0, 0), (long)(seedX * 0.5f + seedY * 0.5f), this);
                        if (!nesto.isNotToBeAdded)
                        {
                            megaChunk.nests.Add(nesto.id);
                            activeNests[nesto.id] = nesto;
                        }
                    }*/
                    megaChunks[megaChunk.pos] = megaChunk;
                    saveMegaChunk(megaChunk, megaChunk.pos, id);
                }
            }
            public void fillBitmap(Bitmap receiver, Color color)
            {
                drawRectangle(receiver, color, (0, 0), (receiver.Size.Width, receiver.Size.Height));
            }
            public void drawRectangle(Bitmap receiver, Color color, (int x, int y) posToDraw, (int x, int y) size)
            {
                using (var g = Graphics.FromImage(receiver))
                {
                    g.FillRectangle(new SolidBrush(color), posToDraw.x, posToDraw.y, size.x, size.y);
                }
            }
            public void drawPixelFixed(Bitmap receiver, Color color, (int x, int y) posToDraw, int scale)
            {
                using (var g = Graphics.FromImage(receiver))
                {
                    g.FillRectangle(new SolidBrush(color), posToDraw.x, posToDraw.y, scale, scale);
                }
            }
            public void drawPixel(Bitmap receiver, Color color, (int x, int y) position, (int x, int y) camPos, int PNGmultiplicator)
            {
                (int x, int y) posToDraw = (position.x - camPos.x - UnloadedChunksAmount * 32, position.y - camPos.y - UnloadedChunksAmount * 32);
                if (posToDraw.x >= 0 && posToDraw.x < (chunkResolution - 1) * 32 && posToDraw.y >= 0 && posToDraw.y < (chunkResolution - 1) * 32)
                {
                    using (var g = Graphics.FromImage(receiver))
                    {
                        g.FillRectangle(new SolidBrush(color), posToDraw.x * PNGmultiplicator, posToDraw.y * PNGmultiplicator, PNGmultiplicator, PNGmultiplicator);
                    }
                }
            }
            public void pasteImage(Bitmap receiver, Bitmap bitmapToDraw, (int x, int y) position, (int x, int y) camPos, int PNGmultiplicator)
            {
                (int x, int y) posToDraw = (position.x - camPos.x - UnloadedChunksAmount * 32, position.y - camPos.y - UnloadedChunksAmount * 32);
                if (true || posToDraw.x >= -bitmapToDraw.Width && posToDraw.x < (chunkResolution) * 32 && posToDraw.y >= -bitmapToDraw.Height && posToDraw.y < (chunkResolution) * 32)
                {
                    using (Graphics g = Graphics.FromImage(receiver))
                    {
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        g.DrawImage(bitmapToDraw, posToDraw.x * PNGmultiplicator, posToDraw.y * PNGmultiplicator, bitmapToDraw.Width * PNGmultiplicator, bitmapToDraw.Height * PNGmultiplicator);
                    }
                }
            }
            public unsafe void pasteImage(Bitmap receiver, Bitmap bitmapToDraw, Color colorToPaste, (int x, int y) position, (int x, int y) camPos, int PNGmultiplicator)
            {
                // Thank you Stephen from stackOverflow !! !! !
                //BitmapData bData = bitmapToDraw.LockBits(new Rectangle(0, 0, bitmapToDraw.Width, bitmapToDraw.Height), ImageLockMode.ReadWrite, bitmapToDraw.PixelFormat);

                /*if (false)
                {
                    ColorPalette oldPalette = bitmapToDraw.Palette;
                    ColorPalette newPalette = bitmapToDraw.Palette;
                    for (int i = 0; i < newPalette.Entries.Length; i++)
                    {
                        Color current = newPalette.Entries[i];
                        newPalette.Entries[i] = Color.FromArgb((int)(colorToPaste.A*current.A*_1On255), (int)(colorToPaste.R*current.R*_1On255), (int)(colorToPaste.G*current.G * _1On255), (int)(colorToPaste.B*current.B*_1On255));
                    }
                    bitmapToDraw.Palette = newPalette; // The crucial statement
                }*/

                (int x, int y) posToDraw = (position.x - camPos.x - UnloadedChunksAmount * 32, position.y - camPos.y - UnloadedChunksAmount * 32);
                if (true || posToDraw.x >= -bitmapToDraw.Width && posToDraw.x < (chunkResolution) * 32 && posToDraw.y >= -bitmapToDraw.Height && posToDraw.y < (chunkResolution) * 32)
                {
                    using (Graphics g = Graphics.FromImage(receiver))
                    {
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        g.DrawImage(bitmapToDraw, posToDraw.x * PNGmultiplicator, posToDraw.y * PNGmultiplicator, bitmapToDraw.Width * PNGmultiplicator, bitmapToDraw.Height * PNGmultiplicator);
                    }
                }

                //bitmapToDraw.Palette = oldPalette; // The crucial statement

                //bitmapToDraw.UnlockBits(bData);
            }
            public Bitmap updateScreen()
            {
                Graphics gg = Graphics.FromImage(gameBitmap);
                gg.Clear(Color.White);
                gg.Dispose();

                List<(int x, int y, int radius, Color color)> lightPositions = new List<(int x, int y, int radius, Color color)>();

                Chunk chunko;
                int PNGmultiplicator = 4;
                if (isPngToBeExported) { PNGmultiplicator = 1; }
                Player player = game.playerList[0];
                (int x, int y) camPos = (player.camPosX, player.camPosY);

                for (int i = UnloadedChunksAmount; i < chunkResolution - UnloadedChunksAmount; i++)
                {
                    for (int j = UnloadedChunksAmount; j < chunkResolution - UnloadedChunksAmount; j++)
                    {
                        chunko = loadedChunks[(chunkX + i, chunkY + j)];
                        pasteImage(gameBitmap, chunko.bitmap, (chunko.position.x * 32, chunko.position.y * 32), camPos, PNGmultiplicator);
                        //if (debugMode) { drawPixel(Color.Red, (chunko.position.x*32, chunko.position.y*32), PNGmultiplicator); } // if want to show chunk origin
                    }
                }

                foreach (Structure structure in activeStructures.Values)
                {
                    if (structure.bitmap != null) { pasteImage(gameBitmap, structure.bitmap, (structure.pos.x + structure.posOffset[0], structure.pos.y + structure.posOffset[1]), camPos, PNGmultiplicator); }
                    if (structure.type == (3, 0, 0))
                    {
                        int frame = ((int)(timeElapsed * 10) + (int)(structure.seed.x) % 100) % 4;
                        pasteImage(gameBitmap, livingPortalAnimation.frames[frame], (structure.pos.x + structure.posOffset[0], structure.pos.y + structure.posOffset[1]), camPos, PNGmultiplicator);
                    }
                }

                foreach (Plant plant in activePlants.Values)
                {
                    pasteImage(gameBitmap, plant.bitmap, (plant.posX + plant.posOffset[0], plant.posY + plant.posOffset[1]), camPos, PNGmultiplicator);
                    if (plant.type == 0 && plant.subType == 1 && plant.childFlowers.Count > 0)
                    {
                        Flower fireFlower = plant.childFlowers[0];
                        int frame = ((int)(timeElapsed*20) + plant.seed % 100) % 6;
                        pasteImage(gameBitmap, fireAnimation.frames[frame], (plant.posX + fireFlower.pos.x /*!!!!!!!!*/ - 1 /*!!!!!!!*/ + plant.posOffset[0], plant.posY + fireFlower.pos.y + plant.posOffset[1]), camPos, PNGmultiplicator);
                    }
                    if (game.isLight)
                    {
                        int radius = 3;
                        if (plant.type == 0 && plant.subType == 1) { radius = 5; }
                        else if (plant.type == 1 && plant.subType == 1) { radius = 11; }

                        foreach ((int x, int y) pos in plant.lightPositions)
                        {
                            lightPositions.Add((pos.x, pos.y, radius, plant.lightColor));
                        }
                    }
                }

                foreach (Entity entity in activeEntities.Values)
                {
                    Color color = entity.color;
                    if (entity.timeAtLastGottenHit > timeElapsed - 0.5f)
                    {
                        float redMult = Min(1, (entity.timeAtLastGottenHit - timeElapsed + 0.5f) * 3);
                        float entityMult = 1 - redMult;
                        color = Color.FromArgb((int)(entityMult * color.R + redMult * 255), (int)(entityMult * color.G), (int)(entityMult * color.B));
                    }
                    if (game.isLight && entity.type == 0) { lightPositions.Add((entity.posX, entity.posY, 7, entity.lightColor)); }
                    drawPixel(gameBitmap, color, (entity.posX, entity.posY), camPos, PNGmultiplicator);
                    if (entity.length > 0)
                    {
                        int county = entity.pastPositions.Count;
                        for (int i = 0; i < entity.length - 1; i++)
                        {
                            if (i >= county) { break; }
                            drawPixel(gameBitmap, color, entity.pastPositions[i], camPos, PNGmultiplicator);
                        }
                    }
                }

                foreach (Particle particle in activeParticles)
                {
                    Color color = particle.color;
                    //if (game.isLight && entity.type == 0) { lightPositions.Add((entity.posX, entity.posY, 7, entity.lightColor)); }
                    drawPixel(gameBitmap, color, (particle.posX, particle.posY), camPos, PNGmultiplicator);
                }

                { // player
                    Color color = Color.Green;
                    (int, int) chunkPos = ChunkIdx(player.posX, player.posY);
                    if (!loadedChunks.ContainsKey(chunkPos))
                    {
                        int seeeEEEXXXXXXXOOOOOOOOOOOODANAAAAAAAAAAAAAAAAAAAAaaaaaaaaaaaaaaa = 69;
                        goto helpMe;
                    }
                    Chunk chunkToTest = loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(player.posX), PosMod(player.posY)].type > 0)
                    {
                        color = Color.Red;
                    }
                    if (game.isLight) { lightPositions.Add((player.posX, player.posY, 9, player.lightColor)); }
                    drawPixel(gameBitmap, color, (player.posX, player.posY), camPos, PNGmultiplicator);
                helpMe:;
                }

                foreach (((int x, int y) pos, Color color) item in attacksToDraw)
                {
                    drawPixel(gameBitmap, item.color, item.pos, camPos, PNGmultiplicator);
                }
                if (debugMode && !isPngToBeExported)
                {
                    foreach (((int x, int y) pos, (int type, int subType) attack) attack in attacksToDo)
                    {
                        drawPixel(gameBitmap, Color.IndianRed, attack.pos, camPos, PNGmultiplicator);
                    }
                }

                if (game.isLight && !debugMode) // light shit
                {
                    lightBitmap = new Bitmap(gameBitmap.Size.Width / 4, gameBitmap.Size.Height / 4);
                    for (int i = UnloadedChunksAmount; i < chunkResolution - UnloadedChunksAmount; i++)
                    {
                        for (int j = UnloadedChunksAmount; j < chunkResolution - UnloadedChunksAmount; j++)
                        {
                            chunko = loadedChunks[(chunkX + i, chunkY + j)];
                            pasteImage(lightBitmap, chunko.lightBitmap, (chunko.position.x * 32, chunko.position.y * 32), camPos, 1);
                            //if (debugMode) { drawPixel(Color.Red, (chunko.position.x*32, chunko.position.y*32), PNGmultiplicator); } // if want to show chunk origin
                        }
                    }


                    pasteBitmapTransparenciesOnBitmap(lightBitmap, lightPositions, camPos); // very slow for some reason !
                    /*else
                    {
                        foreach ((int x, int y, int radius, Color color) pos in lightPositions)
                        {
                            Bitmap bitmapo = Form1.getLightBitmap(pos.color, pos.radius);
                            for (int k = 0; k < 3; k++)
                            {
                                pasteImage(lightBitmap[k], bitmapo, pos.color, (pos.x - pos.radius, pos.y - pos.radius), camPos, 1);
                            }
                            pasteImage(lightBitmap2, bitmapo, pos.color, (pos.x - pos.radius, pos.y - pos.radius), camPos, 1);
                        }
                    }*/

                    Bitmap resizedBitmap = new Bitmap(lightBitmap.Width * 4, lightBitmap.Height * 4);
                    using (Graphics g = Graphics.FromImage(resizedBitmap))
                    {
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        g.DrawImage(lightBitmap, 0, 0, resizedBitmap.Width, resizedBitmap.Height);
                    }
                    lightBitmap = resizedBitmap;
                    pasteLightBitmapOnGameBitmap(gameBitmap, lightBitmap);
                }

                if (!debugMode) // fog of war
                {
                    for (int i = UnloadedChunksAmount; i < chunkResolution - UnloadedChunksAmount; i++)
                    {
                        for (int j = UnloadedChunksAmount; j < chunkResolution - UnloadedChunksAmount; j++)
                        {
                            chunko = loadedChunks[(chunkX + i, chunkY + j)];
                            if (chunko.explorationLevel == 0)
                            {
                                pasteImage(gameBitmap, black32Bitmap, (chunko.position.x * 32, chunko.position.y * 32), camPos, PNGmultiplicator);
                            }
                            else if (chunko.explorationLevel == 1)
                            {
                                pasteImage(gameBitmap, chunko.fogBitmap, (chunko.position.x * 32, chunko.position.y * 32), camPos, PNGmultiplicator);
                            }
                        }
                    }
                }

                if (debugMode && !isPngToBeExported) // debug for nests
                {
                    foreach (Nest nest in activeNests.Values)
                    {
                        foreach ((int x, int y) posToDrawAt in nest.digErrands)
                        {
                            drawPixel(gameBitmap, Color.FromArgb(100, 255, 0, 0), posToDrawAt, camPos, PNGmultiplicator);
                        }
                        if (nest.rooms.ContainsKey(1))
                        {
                            foreach ((int x, int y) posToDrawAt in nest.rooms[1].tiles)
                            {
                                drawPixel(gameBitmap, Color.FromArgb(100, 120, 0, 100), posToDrawAt, camPos, PNGmultiplicator);
                            }
                        }
                        foreach (Room room in nest.rooms.Values)
                        {
                            if (room.type == 2)
                            {
                                foreach ((int x, int y) posToDrawAt in room.dropPositions)
                                {
                                    drawPixel(gameBitmap, Color.FromArgb(100, 0, 0, 255), posToDrawAt, camPos, PNGmultiplicator);
                                }
                            }
                            else if (room.type == 3)
                            {
                                foreach ((int x, int y) posToDrawAt in room.dropPositions)
                                {
                                    drawPixel(gameBitmap, Color.FromArgb(100, 220, 255, 150), posToDrawAt, camPos, PNGmultiplicator);
                                }
                            }
                        }
                    }
                }
                if (debugMode && !isPngToBeExported) // debug for paths
                {
                    foreach (Entity entity in activeEntities.Values)
                    {
                        foreach ((int x, int y) posToDrawAt in entity.pathToTarget)
                        {
                            drawPixel(gameBitmap, Color.FromArgb(100, entity.color.R, entity.color.G, entity.color.B), posToDrawAt, camPos, PNGmultiplicator);
                        }
                        foreach ((int x, int y) posToDrawAt in entity.simplifiedPathToTarget)
                        {
                            drawPixel(gameBitmap, Color.FromArgb(100, 200, 0, 100), posToDrawAt, camPos, PNGmultiplicator);
                        }
                    }
                }

                if (debugMode && !isPngToBeExported) // debug shit for chunks and megachunks
                {
                    (int x, int y) cameraChunkIdx = (ChunkIdx(game.playerList[0].camPosX), ChunkIdx(game.playerList[0].camPosY));
                    foreach ((int x, int y) poso in megaChunks.Keys)
                    {
                        Color colorToDraw = Color.Crimson;
                        drawPixelFixed(gameBitmap, colorToDraw, (300 + poso.x * 16 - cameraChunkIdx.x, 300 + poso.y * 16 - cameraChunkIdx.y), 16);
                    }
                    foreach ((int x, int y) poso in activeStructureLoadedChunkIndexes.Keys)
                    {
                        Color colorToDraw = Color.FromArgb(100, 255, 255, 100);
                        drawPixelFixed(gameBitmap, colorToDraw, (300 + poso.x - cameraChunkIdx.x, 300 + poso.y - cameraChunkIdx.y), 1);
                    }
                    foreach ((int x, int y) poso in extraLoadedChunks.Keys)
                    {
                        Color colorToDraw = Color.Purple;
                        drawPixelFixed(gameBitmap, colorToDraw, (300 + poso.x - cameraChunkIdx.x, 300 + poso.y - cameraChunkIdx.y), 1);
                    }
                    foreach ((int x, int y) poso in loadedChunks.Keys)
                    {
                        Color colorToDraw = Color.FromArgb(150, 0, 128, 0);
                        if (loadedChunks[poso].unstableLiquidCount > 0) { colorToDraw = Color.DarkBlue; }
                        else if (nestLoadedChunkIndexes.ContainsKey(poso)) { colorToDraw = Color.Cyan; }
                        else if (activeStructureLoadedChunkIndexes.ContainsKey(poso)) { colorToDraw = Color.FromArgb(100, 150, 180); }
                        else if (inertStructureLoadedChunkIndexes.ContainsKey(poso)) { colorToDraw = Color.FromArgb(130, 50, 130); }
                        drawPixelFixed(gameBitmap, colorToDraw, (300 + poso.x - cameraChunkIdx.x, 300 + poso.y - cameraChunkIdx.y), 1);
                    }
                    drawPixelFixed(gameBitmap, Color.Red, (300 + ChunkIdx(player.posX) - cameraChunkIdx.x, 300 + ChunkIdx(player.posY) - cameraChunkIdx.y), 1);

                    for (int i = UnloadedChunksAmount; i < chunkResolution - UnloadedChunksAmount; i++)
                    {
                        for (int j = UnloadedChunksAmount; j < chunkResolution - UnloadedChunksAmount; j++)
                        {
                            chunko = loadedChunks[(chunkX + i, chunkY + j)];
                            if (chunko.unstableLiquidCount > 0) { pasteImage(gameBitmap, transBlue32Bitmap, (chunko.position.x * 32, chunko.position.y * 32), camPos, PNGmultiplicator); }
                        }
                    }
                }



                gameBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                return gameBitmap;
            }
            public void zoom(bool isZooming)
            {
                if (isZooming)
                {
                    if (ChunkLength > 2)
                    {
                        ChunkLength -= 2;
                        UnloadedChunksAmount++;
                        gameBitmap = new Bitmap(128 * (ChunkLength - 1), 128 * (ChunkLength - 1));
                    }
                }
                else
                {
                    if (UnloadedChunksAmount > 1)
                    {
                        ChunkLength += 2;
                        UnloadedChunksAmount--;
                        gameBitmap = new Bitmap(128 * (ChunkLength - 1), 128 * (ChunkLength - 1));
                    }

                }
                lastZoom = timeElapsed;
            }
            public Chunk tryToGetChunk((int x, int y) chunkCoords)
            {
                if (loadedChunks.TryGetValue(chunkCoords, out Chunk chunkToTest))
                {
                    return chunkToTest;
                }
                return theFilledChunk;
            }
            public (int type, int subType) getTileContent((int x, int y) posToTest)
            {
                (int x, int y) chunkPos = ChunkIdx(posToTest);
                Chunk chunkToTest;
                if (loadedChunks.ContainsKey(chunkPos)) { chunkToTest = loadedChunks[chunkPos]; }
                else
                {
                    if (!extraLoadedChunks.ContainsKey(chunkPos))
                    {
                        extraLoadedChunks.Add(chunkPos, new Chunk(chunkPos, true, this));
                    }
                    chunkToTest = extraLoadedChunks[chunkPos];
                }
                return chunkToTest.fillStates[PosMod(posToTest.x), PosMod(posToTest.y)];
            }
            public (int type, int subType) setTileContent((int x, int y) posToTest, (int type, int subType) typeToSet)
            {
                (int x, int y) chunkPos = ChunkIdx(posToTest);
                Chunk chunkToTest;
                if (loadedChunks.ContainsKey(chunkPos)) { chunkToTest = loadedChunks[chunkPos]; }
                else
                {
                    if (!extraLoadedChunks.ContainsKey(chunkPos))
                    {
                        extraLoadedChunks.Add(chunkPos, new Chunk(chunkPos, true, this));
                    }
                    chunkToTest = extraLoadedChunks[chunkPos];
                }
                (int type, int subType) previous = chunkToTest.tileModification(posToTest.x, posToTest.y, typeToSet);
                return previous;
            }
            public int getTileContent((int x, int y) posToTest, Dictionary<(int x, int y), Chunk> extraDictToCheckFrom)
            {
                (int x, int y) chunkPos = ChunkIdx(posToTest);
                Chunk chunkToTest = getChunkEvenIfNotLoaded(chunkPos, extraDictToCheckFrom);
                return chunkToTest.fillStates[PosMod(posToTest.x), PosMod(posToTest.y)].type;
            }
            public Chunk getChunkEvenIfNotLoaded((int x, int y) chunkPos, Dictionary<(int x, int y), Chunk> chunkDict)
            {
                Chunk chunkToTest;
                if (loadedChunks.ContainsKey(chunkPos)) { chunkToTest = loadedChunks[chunkPos]; }
                else
                {
                    if (chunkDict.ContainsKey(chunkPos)) { }
                    else if (extraLoadedChunks.ContainsKey(chunkPos))
                    {
                        chunkDict.Add(chunkPos, extraLoadedChunks[chunkPos]);
                    }
                    else
                    {
                        chunkDict.Add(chunkPos, new Chunk(chunkPos, true, this));
                        extraLoadedChunks.Add(chunkPos, chunkDict[chunkPos]);
                    }
                    chunkToTest = chunkDict[chunkPos];
                }
                return chunkToTest;
            }
        }
    }
}
