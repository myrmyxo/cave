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
    public class Screens
    {
        public class Game
        {
            public Dictionary<int, Screen> loadedScreens = new Dictionary<int, Screen>();
            public List<Player> playerList = new List<Player>();
            public Bitmap overlayBitmap;
            public long seed;

            public bool isLight = true;
            public Game(long seedToPut)
            {
                seed = seedToPut;
                playerList = new List<Player>();

                overlayBitmap = new Bitmap(512, 128);

                Screens.Screen mainScreen;
                SettingsJson settings;
                settings = tryLoadSettings(seedToPut);

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

                int idToPut = 2;
                int PNGsize = 150;
                PNGsize = 50;

                bool isMonoeBiomeToPut = false;
                bool isPngToExport = true;
                
                if (isPngToExport)
                {
                    debugMode = true;
                    int oldChunkLength = ChunkLength;
                    ChunkLength = PNGsize;
                    mainScreen = new Screen(this, ChunkLength, idToPut, isMonoeBiomeToPut, true);
                    timeAtLauch = DateTime.Now;

                    runGame(null, null);

                    mainScreen.updateScreen().Save($"{currentDirectory}\\caveMap.png");
                    ChunkLength = oldChunkLength;
                }

                mainScreen = new Screen(this, ChunkLength, idToPut, isMonoeBiomeToPut, false);
                if (currentScreenId == 0) { currentScreenId++; };
                loadedScreens[idToPut] = mainScreen;
                setPlayerDimension(player, idToPut);
            }
            public void movePlayerStuff(Screen screen, Player player)
            {
                if (inventoryChangePress[0]) { inventoryChangePress[0] = false; player.moveInventoryCursor(-1); }
                if (inventoryChangePress[1]) { inventoryChangePress[1] = false; player.moveInventoryCursor(1); }

                player.movePlayer();
                screen.checkStructures(player);

                screen.chunkX = Floor(player.camPosX, 32) / 32;
                screen.chunkY = Floor(player.camPosY, 32) / 32;
                screen.updateLoadedChunks();
            }
            public void runGame(PictureBox gamePictureBox, PictureBox overlayPictureBox)
            {
                Player player = playerList[0];

                if (pausePress) { return; }
                if (doShitPress)
                {
                    if (dimensionSelection)
                    {
                        dimensionSelection = false;
                        setPlayerDimension(player, currentTargetDimension);
                    }
                    else { dimensionSelection = true; }
                    doShitPress = false;
                }

                foreach (Screen screen in loadedScreens.Values)
                {
                    int framesFastForwarded = 0;
                LoopStart:;
                    timeElapsed += 0.02f;
                    screen.extraLoadedChunks.Clear(); // this will make many bugs
                    screen.broadTestUnstableLiquidList = new List<(int, int)>();
                    if (zoomPress[0] && timeElapsed > lastZoom + 0.25f) { screen.zoom(true); }
                    if (zoomPress[1] && timeElapsed > lastZoom + 0.25f) { screen.zoom(false); }
                    if (player.dimension == screen.id)
                    {
                        if (dimensionSelection && timeElapsed%0.3f < 0.019f)
                        {
                            if (arrowKeysState[0] || arrowKeysState[2]) { 
                                currentTargetDimension++; 
                            }
                            if (arrowKeysState[1] || arrowKeysState[3]) { currentTargetDimension--; }
                        }
                        movePlayerStuff(screen, player);
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

                    screen.unloadFarawayChunks();
                    screen.manageMegaChunks();

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
                    screen.entitesToAdd = new List<Entity>();
                    foreach (Entity entity in screen.activeEntities.Values)
                    {
                        entity.moveEntity();
                    }
                    foreach (Entity entity in screen.entitesToRemove.Values)
                    {
                        screen.activeEntities.Remove(entity.id);
                    }
                    foreach (Entity entity in screen.entitesToAdd)
                    {
                        screen.activeEntities[entity.id] = entity;
                    }
                    foreach (Plant plant in screen.activePlants.Values)
                    {
                        plant.testPlantGrowth(false);
                    }
                    foreach ((int x, int y) pos in screen.loadedChunks.Keys)
                    {
                        if (rand.Next(100) == 0) { screen.loadedChunks[(pos.x, pos.y)].unstableLiquidCount++; }
                        screen.loadedChunks[(pos.x, pos.y)].moveLiquids();
                    }


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
                        if (dimensionSelection)
                        {
                            drawNumber(overlayBitmap, currentTargetDimension, (200, 64), 4, true);
                        }
                        drawInventory(player.screen.game, player.inventoryQuantities, player.inventoryElements, player.inventoryCursor);
                        overlayPictureBox.Refresh();
                    }
                }
                saveSettings(this);
            }
            public void renderScreen()
            {

            }
            public void setPlayerDimension(Player player, int targetDimension)
            {
                if (!loadedScreens.ContainsKey(targetDimension))
                {
                    Screen newScreen = new Screen(this, ChunkLength, targetDimension, true, false);
                    loadedScreens[targetDimension] = newScreen;
                    currentScreenId++;
                }
                player.screen = loadedScreens[targetDimension];
                player.dimension = targetDimension;
                player.screen.checkStructuresOnSpawn(player);
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
            public (int, int) type;
            public bool isMonoBiome; // if yes, then the whole-ass dimension is only made ouf of ONE biome, that is of the type of... well type. If not, type is the type of dimension and not the biome (like idk normal, frozen, lampadaire, shitpiss world...)

            public Bitmap gameBitmap;

            public Bitmap lightBitmap;

            public (float x, float y) playerStartPos = (0, 0);

            public Dictionary<int, Entity> activeEntities = new Dictionary<int, Entity>();
            public Dictionary<int, Entity> entitesToRemove = new Dictionary<int, Entity>();
            public List<Entity> entitesToAdd = new List<Entity>();
            public Dictionary<int, bool> orphanEntities = new Dictionary<int, bool>();
            public Dictionary<int, Plant> activePlants = new Dictionary<int, Plant>();
            public Dictionary<int, Nest> activeNests = new Dictionary<int, Nest>();
            public Dictionary<int, Structure> activeStructures = new Dictionary<int, Structure>();

            public Dictionary<(int, int), List<Plant>> outOfBoundsPlants = new Dictionary<(int, int), List<Plant>>(); // not used as of now but in some functions so can't remove LMAO

            public List<(int, int)> broadTestUnstableLiquidList = new List<(int, int)>();

            public bool initialLoadFinished = false;

            public int chunkX = 0;
            public int chunkY = 0;

            // debug shit
            public Dictionary<(int x, int y), bool> nestLoadedChunkIndexes = new Dictionary<(int x, int y), bool>();
            public Dictionary<(int x, int y), bool> structureLoadedChunkIndexes = new Dictionary<(int x, int y), bool>();

            public Screen(Game gameToPut, int chunkResolutionToPut, int idToPut, bool isMonoToPut, bool isPngToExport)
            {
                game = gameToPut;
                id = idToPut;
                seed = game.seed + id;
                type = (id % 11, 0);
                isMonoBiome = isMonoToPut;
                isPngToBeExported = isPngToExport;
                chunkResolution = chunkResolutionToPut + UnloadedChunksAmount * 2; // invisible chunks of the sides/top/bottom
                createDimensionFolders(game, id);
                LCGCacheInit();
                chunkX = Floor(game.playerList[0].posX, 32) / 32;
                chunkY = Floor(game.playerList[0].posY, 32) / 32;
                loadChunks();

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
            public void saveAllChunks()
            {
                foreach (Chunk chunk in loadedChunks.Values)
                {
                    saveChunk(chunk);
                }
            }
            public void loadChunks()
            {
                loadedChunks = new Dictionary<(int, int), Chunk>();
                for (int i = 0; i < chunkResolution; i++)
                {
                    for (int j = 0; j < chunkResolution; j++)
                    {
                        if (!loadedChunks.ContainsKey((chunkX + i, chunkY + j)))
                        {
                            loadedChunks.Add((chunkX + i, chunkY + j), new Chunk((chunkX + i, chunkY + j), false, this));
                        }
                    }
                }
                if (isPngToBeExported) { gameBitmap = new Bitmap(32 * (chunkResolution - 1), 32 * (chunkResolution - 1)); }
                else { gameBitmap = new Bitmap(128 * (ChunkLength - 1), 128 * (ChunkLength - 1)); }
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
                while (activeEntities.Count() > 0)
                {
                    id = activeEntities.Keys.ToArray()[0]; // kinda cring, very inneficient :(
                    chunkIndex = findChunkAbsoluteIndex(activeEntities[id].posX, activeEntities[id].posY);
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
                while (activePlants.Count() > 0)
                {
                    id = activePlants.Keys.ToArray()[0]; // kinda cring, very inneficient :(
                    chunkIndex = findChunkAbsoluteIndex(activePlants[id].posX, activePlants[id].posY);
                    chunk = loadedChunks[chunkIndex];
                    chunk.plantList.Add(activePlants[id]);
                    activePlants.Remove(id);
                }
                foreach (Entity entito in cringeEntities)
                {
                    activeEntities[entito.id] = entito;
                }
            }
            public void updateLoadedChunks()
            {
                (int x, int y) posToTest;
                for (int i = 0; i < chunkResolution; i++)
                {
                    for (int j = 0; j < chunkResolution; j++)
                    {
                        posToTest = (chunkX + i, chunkY + j);
                        if (!loadedChunks.ContainsKey(posToTest))
                        {
                            loadedChunks.Add(posToTest, new Chunk(posToTest, false, this));
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
                Dictionary<(int x, int y), bool> extraChunkIndexes = new Dictionary<(int x, int y), bool>();
                activeStructures = new Dictionary<int, Structure>();            // DONT FORGET TO UNCOMMENT THIS WHEN NOT DEBUGGING !!!! else momori leek
                foreach (Structure structure in activeStructures.Values)
                {
                    foreach ((int x, int y) tile in structure.chunkPresence.Keys)
                    {
                        extraChunkIndexes[tile] = true;
                    }
                }
                structureLoadedChunkIndexes = extraChunkIndexes;
                extraChunkIndexes = new Dictionary<(int x, int y), bool>();
                foreach (Nest nest in activeNests.Values)
                {
                    foreach ((int x, int y) tile in nest.chunkPresence.Keys)
                    {
                        extraChunkIndexes[tile] = true;
                    }
                }
                nestLoadedChunkIndexes = extraChunkIndexes;
                int nestChunkAmount = extraChunkIndexes.Count;
                int minChunkAmount = chunkResolution * chunkResolution + nestChunkAmount;

                (int x, int y) cameraChunkIdx = (Floor(game.playerList[0].camPosX, 32) / 32 + chunkResolution / 2, Floor(game.playerList[0].camPosY, 32) / 32 + chunkResolution / 2);

                Dictionary<(int x, int y), bool> chunksToRemove = new Dictionary<(int x, int y), bool>();
                (int x, int y)[] chunkKeys = loadedChunks.Keys.ToArray();
                (int x, int y) currentIdx;
                float distanceToCenter;
                for (int i = loadedChunks.Count; i > minChunkAmount; i--)
                {
                    currentIdx = chunkKeys[rand.Next(loadedChunks.Count)];
                    distanceToCenter = distance((currentIdx), cameraChunkIdx);
                    if (!extraChunkIndexes.ContainsKey(currentIdx) && (float)(rand.NextDouble()) * distanceToCenter > 0.8f * chunkResolution)
                    {
                        chunksToRemove[currentIdx] = true;
                    }
                }

                actuallyUnloadTheChunks(chunksToRemove);
            }
            public void actuallyUnloadTheChunks(Dictionary<(int x, int y), bool> chunksDict)
            {
                if (chunksDict.Count > 0 && true)
                {
                    putEntitiesAndPlantsInChunks();

                    foreach ((int, int) chunkPos in chunksDict.Keys)
                    {
                        if (loadedChunks.ContainsKey(chunkPos))
                        {
                            //removePlantsFromChunk(loadedChunks[chunkPos]);
                            Files.saveChunk(loadedChunks[chunkPos]);
                            loadedChunks.Remove(chunkPos);
                        }
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
            }
            public void manageMegaChunks() // megachunk resolution : 512*512
            {
                (int x, int y) cameraChunkIdx = (Floor(game.playerList[0].camPosX, 32) / 32 + chunkResolution / 2, Floor(game.playerList[0].camPosY, 32) / 32 + chunkResolution / 2);

                Dictionary<(int x, int y), MegaChunk> newMegaChunks = new Dictionary<(int x, int y), MegaChunk>();

                (int x, int y) currentPos;
                foreach ((int x, int y) pos in loadedChunks.Keys)
                {
                    if (nestLoadedChunkIndexes.ContainsKey(pos) && distance(pos, cameraChunkIdx) > 0.8f * chunkResolution) { continue; }
                    currentPos = findMegaChunkAbsoluteIndex(pos);
                    newMegaChunks[currentPos] = null;
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
                        newMegaChunks[pos].loadAllChunksInNests(this);
                    }
                }
                Dictionary<(int x, int y), bool> chunksToRemove = new Dictionary<(int x, int y), bool>();
                foreach ((int x, int y) pos in megaChunks.Keys) // remove megachunks that have no chunks loaded in anymore
                {
                    megaChunks[pos].unloadAllNestsAndChunks(this, chunksToRemove);
                    saveMegaChunk(megaChunks[pos], pos, id);
                    actuallyUnloadTheChunks(chunksToRemove);
                }

                megaChunks = newMegaChunks;
            }
            public (int, int) findMegaChunkAbsoluteIndex((int x, int y) pos)
            {
                int chunkPosX = Floor(pos.x, 16) / 16;
                int chunkPosY = Floor(pos.y, 16) / 16;
                return (chunkPosX, chunkPosY);
            }
            public (int, int) findChunkAbsoluteIndex(int pixelPosX, int pixelPosY)
            {
                int chunkPosX = Floor(pixelPosX, 32) / 32;
                int chunkPosY = Floor(pixelPosY, 32) / 32;
                return (chunkPosX, chunkPosY);
            }
            public (int, int) findChunkAbsoluteIndex((int x, int y) pos)
            {
                int chunkPosX = Floor(pos.x, 32) / 32;
                int chunkPosY = Floor(pos.y, 32) / 32;
                return (chunkPosX, chunkPosY);
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
                if (loadStructuresYesOrNo && !System.IO.File.Exists($"{currentDirectory}\\CaveData\\{game.seed}\\MegaChunkData\\{id}\\{posX}.{posY}.json"))
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
                        Structure newStructure = new Structure(posX * 512 + 32 + (int)(seedX % 480), posY * 512 + 32 + (int)(seedY % 480), seedX, seedY, false, (posX, posY), this);
                        newStructure.drawStructure();
                        newStructure.imprintChunks();
                        megaChunk.structures.Add(newStructure.id);
                        activeStructures[newStructure.id] = newStructure;
                        //newStructure.saveInFile();
                    }
                    long waterLakesAmount = 15 + (seedX + seedY) % 150;
                    for (int i = 0; i < waterLakesAmount; i++)
                    {
                        seedX = LCGyNeg(seedX); // on porpoise x    /\_/\
                        seedY = LCGxNeg(seedY); // and y switched  ( ^o^ )
                        Structure newStructure = new Structure(posX * 512 + 32 + (int)(seedX % 480), posY * 512 + 32 + (int)(seedY % 480), seedX, seedY, true, (posX, posY), this);
                        newStructure.drawLakePapa();
                        megaChunk.structures.Add(newStructure.id);
                        activeStructures[newStructure.id] = newStructure;
                        //newStructure.saveInFile();
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
                else
                {
                    saveMegaChunk(new MegaChunk((posX, posY)), (posX, posY), id);
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
                    (int, int) chunkPos = findChunkAbsoluteIndex(entity.posX, entity.posY);
                    if (loadedChunks.ContainsKey(chunkPos))
                    {
                        Chunk chunkToTest = loadedChunks[chunkPos];
                        if (chunkToTest.fillStates[(entity.posX % 32 + 32) % 32, (entity.posY % 32 + 32) % 32].type > 0)
                        {
                            color = Color.Red;
                        }
                    }
                    if (game.isLight && entity.type == 0) { lightPositions.Add((entity.posX, entity.posY, 7, entity.lightColor)); }
                    drawPixel(gameBitmap, color, (entity.posX, entity.posY), camPos, PNGmultiplicator);
                }

                { // player
                    Color color = Color.Green;
                    (int, int) chunkPos = findChunkAbsoluteIndex(player.posX, player.posY);
                    Chunk chunkToTest = loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[(player.posX % 32 + 32) % 32, (player.posY % 32 + 32) % 32].type > 0)
                    {
                        color = Color.Red;
                    }
                    if (game.isLight) { lightPositions.Add((player.posX, player.posY, 9, player.lightColor)); }
                    drawPixel(gameBitmap, color, (player.posX, player.posY), camPos, PNGmultiplicator);
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

                if (debugMode) // debug for nests
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
                if (debugMode) // debug for paths
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

                if (debugMode) // debug shit for chunks and megachunks
                {
                    (int x, int y) cameraChunkIdx = (Floor(game.playerList[0].camPosX, 32) / 32, Floor(game.playerList[0].camPosY, 32) / 32);
                    foreach ((int x, int y) poso in megaChunks.Keys)
                    {
                        Color colorToDraw = Color.Crimson;
                        drawPixelFixed(gameBitmap, colorToDraw, (300 + poso.x * 16 - cameraChunkIdx.x, 300 + poso.y * 16 - cameraChunkIdx.y), 16);
                    }
                    foreach ((int x, int y) poso in structureLoadedChunkIndexes.Keys)
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
                        if (nestLoadedChunkIndexes.ContainsKey(poso)) { colorToDraw = Color.Cyan; }
                        drawPixelFixed(gameBitmap, colorToDraw, (300 + poso.x - cameraChunkIdx.x, 300 + poso.y - cameraChunkIdx.y), 1);
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
            public int getTileContent((int x, int y) posToTest)
            {
                (int x, int y) chunkPos = (Floor(posToTest.x, 32) / 32, Floor(posToTest.y, 32) / 32);
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
                return chunkToTest.fillStates[(posToTest.x % 32 + 32) % 32, (posToTest.y % 32 + 32) % 32].type;
            }
            public int getTileContent((int x, int y) posToTest, Dictionary<(int x, int y), Chunk> extraDictToCheckFrom)
            {
                (int x, int y) chunkPos = (Floor(posToTest.x, 32) / 32, Floor(posToTest.y, 32) / 32);
                Chunk chunkToTest = getChunkEvenIfNotLoaded(chunkPos, extraDictToCheckFrom);
                return chunkToTest.fillStates[(posToTest.x % 32 + 32) % 32, (posToTest.y % 32 + 32) % 32].type;
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
                    }
                    chunkToTest = chunkDict[chunkPos];
                }
                return chunkToTest;
            }
            public void addChunksToExtraLoaded(Dictionary<(int x, int y), Chunk> chunkDict)
            {
                foreach ((int x, int y) pos in chunkDict.Keys)
                {
                    if (!loadedChunks.ContainsKey(pos) && !extraLoadedChunks.ContainsKey(pos))
                    {
                        extraLoadedChunks[pos] = chunkDict[pos];
                    }
                }
            }
        }
    }
}
