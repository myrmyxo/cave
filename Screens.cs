﻿using System;
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

namespace Cave
{
    public class Screens
    {
        public class Game
        {
            public List<string> structureGenerationLogs = new List<string>();
            public Dictionary<(int dim, int x, int y), int> structureGenerationLogsStructureUpdateCount = new Dictionary<(int dim, int x, int y), int>();

            public Dictionary<int, Screen> loadedScreens = new Dictionary<int, Screen>();
            public List<Player> playerList = new List<Player>();

            public Dictionary<int, Structure> structuresToAdd = new Dictionary<int, Structure>();
            public Dictionary<int, Structure> structuresToRemove = new Dictionary<int, Structure>();

            public Bitmap overlayBitmap;
            public long seed;
            public int livingDimensionId = -1;

            public int zoomLevel;
            public int effectiveRadius;

            public Bitmap gameBitmap;
            public Bitmap lightBitmap;
            public bool isLight = true;
            public Game()
            {
                devMode = true;
                bool randomSeed = true;
                seed = 123456;

                int idToPut = 0;
                (int type, int subType) forceBiome = (0, 0);
                int PNGsize = 150;
                PNGsize = 100;

                zoomLevel = 42;
                effectiveRadius = chunkLoadMininumRadius;

                bool isMonoeBiomeToPut = false;
                bool isPngToExport = false;

                loadStructuresYesOrNo = true;
                spawnNests = true;
                spawnEntities = true;
                spawnPlants = true;
                bool spawnNOTHING = false;
                if (spawnNOTHING) { loadStructuresYesOrNo = false; spawnEntities = false; spawnPlants = false; }

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
                    int oldChunkLength = chunkLoadMininumRadius;
                    chunkLoadMininumRadius = PNGsize;
                    findEffectiveChunkLoadingRadius();
                    loadDimension(idToPut, true, isMonoeBiomeToPut, forceBiome.type, forceBiome.subType);
                    setPlayerDimension(player, idToPut);
                    player.placePlayer();
                    timeAtLauch = DateTime.Now;

                    runGame(null, null);
                    makeGameBitmaps(true);
                    loadedScreens[idToPut].updateScreen(gameBitmap, lightBitmap, true).Save($"{currentDirectory}\\caveMap.png");
                    makeGameBitmaps(false);
                    chunkLoadMininumRadius = oldChunkLength;
                    findEffectiveChunkLoadingRadius();
                }
                else
                {
                    makeGameBitmaps(false);
                    loadDimension(idToPut, false, isMonoeBiomeToPut, forceBiome.type, forceBiome.subType);
                    setPlayerDimension(player, idToPut);
                    player.placePlayer();
                }
            }
            public void movePlayerStuff(Player player)
            {
                if (inventoryChangePress[0]) { inventoryChangePress[0] = false; player.moveInventoryCursor(-1); }
                if (inventoryChangePress[1]) { inventoryChangePress[1] = false; player.moveInventoryCursor(1); }

                player.movePlayer();
            }
            public void applySettings(SettingsJson settings)
            {
                if (settings == null) { return; }
                timeElapsed = settings.time;
                currentStructureId = settings.structureId;
                currentEntityId = settings.entityId;
                currentPlantId = settings.plantId;
                currentDimensionId = settings.dimensionId;
                livingDimensionId = settings.livingDimensionId;
            }
            public void makeChunkLoadingPoints(Dictionary<int, bool> testedStructures, int currentMagnitude, Structure structureToTest = null)
            {
                int newMagnitude;
                Screen currentScreen;
                if (structureToTest == null) // first recursion : testing from PLAYER
                {
                    Player player = playerList[0];
                    currentScreen = player.screen;
                    currentScreen.chunkLoadingPoints[ChunkIdx(player.posX, player.posY)] = currentMagnitude;
                    foreach (Structure structure in currentScreen.activeStructures.Values)
                    {
                        if (testedStructures.ContainsKey(structure.id)) { continue; }
                        (int x, int y) chunkPos = ChunkIdx(structure.pos);
                        newMagnitude = currentMagnitude - (int)(Distance(chunkPos, ChunkIdx(player.posX, player.posY)));
                        if (newMagnitude <= 0) { continue; }
                        makeChunkLoadingPoints(testedStructures, newMagnitude, structure);
                    }
                }
                else if (structureToTest.type.type == 3 && structureToTest.sisterStructure != null) // next recursions : testing from a STRUCTURE
                {
                    testedStructures[structureToTest.id] = true;
                    structureToTest = structureToTest.sisterStructure; // select sister structure of portal to test for that dimension
                    currentScreen = structureToTest.screen;

                    currentScreen.chunkLoadingPoints[ChunkIdx(structureToTest.pos)] = currentMagnitude;
                    testedStructures[structureToTest.id] = true;

                    foreach (Structure structure in currentScreen.activeStructures.Values)
                    {
                        if (testedStructures.ContainsKey(structure.id)) { continue; }
                        (int x, int y) chunkPos = ChunkIdx(structure.pos);
                        newMagnitude = currentMagnitude + 1 - (int)(Distance(chunkPos, ChunkIdx(structureToTest.pos))); // + 1 to counteract float point value int rounding loss, and ensure portals don't have chunks that need to be rendered not loaded when entering
                        if (newMagnitude <= 0) { continue; }
                        makeChunkLoadingPoints(testedStructures, newMagnitude, structure);
                    }
                }
            }
            public void runGame(PictureBox gamePictureBox, PictureBox overlayPictureBox)
            {
                int framesFastForwarded = 0;
            LoopStart:;

                timeElapsed += 0.02f;

                foreach (Screen screen in loadedScreens.Values.ToArray())
                {
                    screen.liquidsThatCantGoLeft = new Dictionary<(int, int), bool>();
                    screen.liquidsThatCantGoRight = new Dictionary<(int, int), bool>();
                    screen.entitesToRemove = new Dictionary<int, Entity>();
                    screen.entitesToAdd = new Dictionary<int, Entity>();
                    screen.particlesToRemove = new Dictionary<Particle, bool>();
                    screen.particlesToAdd = new List<Particle>();
                    screen.attacksToDo = new List<((int x, int y) pos, (int type, int subType) attack)>();
                    screen.attacksToDraw = new List<((int x, int y) pos, Color color)>();
                }

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
                if (zoomPress[0] && timeElapsed > lastZoom + 0.25f) { zoom(true); }
                if (zoomPress[1] && timeElapsed > lastZoom + 0.25f) { zoom(false); }



                Screen playerScreen = getScreen(player.dimension);
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
                movePlayerStuff(player); // move player, load new chunks, test craft, and stuff
                playerScreen.chunkX = ChunkIdx(player.posX);
                playerScreen.chunkY = ChunkIdx(player.posY);




                foreach (Screen screen in loadedScreens.Values) { screen.chunkLoadingPoints = new Dictionary<(int x, int y), int>(); }
                makeChunkLoadingPoints(new Dictionary<int, bool>(), playerScreen.game.effectiveRadius);
                playerScreen.forceLoadChunksForOnePoint(ChunkIdx(player.posX, player.posY), playerScreen.game.effectiveRadius);

                List<int> dimensionsToUnload = new List<int>();
                foreach (Screen screen in loadedScreens.Values.ToArray())
                {
                    screen.loadCloseChunks();

                    screen.addRemoveEntities();

                    foreach ((int x, int y) pos in screen.chunksToSpawnEntitiesIn.Keys)
                    {
                        if (screen.loadedChunks.ContainsKey(pos)) { screen.loadedChunks[pos].spawnEntities(); }
                    }
                    screen.chunksToSpawnEntitiesIn = new Dictionary<(int x, int y), bool>();

                    foreach (Entity entity in screen.activeEntities.Values) { entity.moveEntity(); }
                    screen.addRemoveEntities();

                    foreach (Plant plant in screen.activePlants.Values) { plant.testPlantGrowth(false); }
                    foreach (Particle particle in screen.activeParticles) { particle.moveParticle(); }
                    foreach (Structure structure in screen.activeStructures.Values) { structure.moveStructure(); }
                    screen.addRemoveEntities();
                    foreach ((int x, int y) pos in screen.loadedChunks.Keys)
                    {
                        if (rand.Next(200) == 0) { screen.loadedChunks[(pos.x, pos.y)].unstableLiquidCount++; }
                        screen.loadedChunks[(pos.x, pos.y)].moveLiquids();
                    }

                    screen.makeBitmapsOfPlants();
                    screen.putEntitiesAndPlantsInChunks();
                    foreach (((int x, int y) pos, (int type, int subType) attack) attack in screen.attacksToDo)
                    {
                        player.sendAttack(attack);
                    }
                    if (player.willBeSetAsNotAttacking) { player.setAsNotAttacking(); }
                    screen.removePlants();
                    screen.makeBitmapsOfPlants();


                    while (structuresToAdd.Count > 0)
                    {
                        Dictionary<int, Structure> structuresToAddNow = new Dictionary<int, Structure>(structuresToAdd);
                        structuresToAdd = new Dictionary<int, Structure>();
                        foreach (Structure structure in structuresToAddNow.Values)
                        {
                            MegaChunk megaChunk = screen.getMegaChunkFromPixelPos(structure.pos, false);
                            foreach (int id in megaChunk.structures) // this checks is there is already a structure of the same type overlapping with the chunks of the new one (not to make duplicates for portals n shite)
                            {
                                if (!structure.screen.activeStructures.ContainsKey(id)) { continue; }
                                Structure structo = structure.screen.activeStructures[id];
                                if (structo.type != structure.type) { continue; }
                                foreach ((int x, int y) pos in structo.chunkPresence.Keys)
                                {
                                    if (structure.chunkPresence.ContainsKey(pos)) { goto doNotAddStructure; }
                                }
                            }
                            // after this the structure is VALID and WILL be added to existing structures
                            structure.initAfterStructureValidated();
                        doNotAddStructure:;
                        }
                    }
                    foreach (Structure structure in screen.game.structuresToRemove.Values)
                    {
                        structure.EraseFromTheWorld();
                    }
                    structuresToRemove = new Dictionary<int, Structure>();
                }
                foreach (Screen screen in loadedScreens.Values.ToArray()) { screen.unloadFarawayChunks(); }
                foreach (Screen screen in loadedScreens.Values.ToArray()) { screen.manageExtraLoadedChunks(); }
                setUnloadingImmunity(); // Prevent MegaChunks/Chunks/Structures to be unloaded when they should not be
                foreach (Screen screen in loadedScreens.Values.ToArray())
                {
                    screen.unloadMegaChunks();

                    screen.removeEntitiesAndPlantsFromChunks(true);
                    screen.addRemoveEntities();
                    foreach (Particle particle in screen.particlesToAdd)         { screen.activeParticles.Add(particle); }
                    foreach (Particle particle in screen.particlesToRemove.Keys) { screen.activeParticles.Remove(particle); }
                    screen.particlesToRemove = new Dictionary<Particle, bool>();
                    screen.particlesToAdd = new List<Particle>();

                    if (screen != playerScreen && screen.loadedChunks.Count == 0) { dimensionsToUnload.Add(screen.id); }
                }
                foreach (int id in dimensionsToUnload)
                {
                    unloadDimension(id);
                }
                saveSettings(this);

                // go back 10 times if fastForward
                if (fastForward && framesFastForwarded < 10)
                {
                    framesFastForwarded++;
                    goto LoopStart;
                }

                // render screen and update game image box thing
                playerScreen = getScreen(player.dimension);
                gamePictureBox.Image = playerScreen.updateScreen(gameBitmap, lightBitmap);
                gamePictureBox.Refresh();
                overlayPictureBox.Image = overlayBitmap;
                Sprites.drawSpriteOnCanvas(overlayBitmap, overlayBackground.bitmap, (0, 0), 4, false);
                if (dimensionSelection) { drawNumber(overlayBitmap, currentTargetDimension, (200, 64), 4, true); }
                else if (craftSelection) { drawCraftRecipe(this, craftRecipes[player.craftCursor]); }
                drawInventory(player.screen.game, player.inventoryQuantities, player.inventoryElements, player.inventoryCursor);
                overlayPictureBox.Refresh();

                updateStructureLogFile(this);

                int gouga = liquidSlideCount;
                gouga = gouga + 1 - 1;
            }
            public void setUnloadingImmunity()
            {
                // Shit to go through chunks, megachunks, nests/structures broad search and mark them immune to unloading as it progresses.
                foreach (Screen screen in loadedScreens.Values)
                {
                    foreach (Structure structure in screen.inertStructures.Values) { structure.isImmuneToUnloading = false; }
                    foreach (Structure structure in screen.activeStructures.Values) { structure.isImmuneToUnloading = false; }
                    foreach (MegaChunk megaChunk in screen.megaChunks.Values) { megaChunk.isImmuneToUnloading = false; }
                }

                Dictionary<Chunk, bool> newImmuneChunks = new Dictionary<Chunk, bool>();
                Dictionary<MegaChunk, bool> newImmuneMegaChunks = new Dictionary<MegaChunk, bool>();
                Dictionary<Nest, bool> newImmuneNests = new Dictionary<Nest, bool>();
                Dictionary<Structure, bool> newImmuneStructures = new Dictionary<Structure, bool>();

                foreach (Screen screen in loadedScreens.Values.ToArray())
                {
                    // 1. non nest/structure chunks set MegaChunks as Immune
                    MegaChunk megaChunk;
                    foreach (Chunk chunk in screen.loadedChunks.Values.ToArray())
                    {
                        if (!screen.activeStructureLoadedChunkIndexes.ContainsKey(chunk.pos))
                        {
                            chunk.isImmuneToUnloading = true;
                            megaChunk = chunk.getMegaChunk();
                            megaChunk.isImmuneToUnloading = true;
                            newImmuneMegaChunks[megaChunk] = true;
                        }
                        else { chunk.isImmuneToUnloading = false; }
                    }
                }
                while (newImmuneMegaChunks.Count > 0)
                {
                    Chunk chunkToTest;
                    Screen screen;

                    // 2. new Immune megaChunks set structures/nests as Immune
                    foreach (MegaChunk megaChunk in newImmuneMegaChunks.Keys)
                    {
                        screen = megaChunk.screen;
                        Structure structure;
                        foreach (int structureId in megaChunk.structures)
                        {
                            if (!screen.activeStructures.ContainsKey(structureId)) { continue; }
                            structure = screen.activeStructures[structureId];
                            if (structure.isImmuneToUnloading) { continue; } // To not do the same one twice -> infinite loop
                            structure.isImmuneToUnloading = true;
                            newImmuneStructures[structure] = true;
                            if (structure.sisterStructure != null)  // REPLACE THIS PART WITH THE LIST OF SISTER STUCTURES WHERE STRUCTURES WILL HAVE MULTIPLE SISTER STRUCTURES (so in 7 years)
                            {
                                structure.sisterStructure.isImmuneToUnloading = true;
                                newImmuneStructures[structure.sisterStructure] = true;
                            }
                        }
                    }
                    newImmuneMegaChunks = new Dictionary<MegaChunk, bool>();

                    // 3. new Immune structures/nests set chunks as Immune
                    foreach (Structure structure in newImmuneStructures.Keys)
                    {
                        screen = structure.screen;
                        foreach ((int x, int y) chunkPos in structure.chunkPresence.Keys)
                        {
                            chunkToTest = screen.getChunkFromChunkPos(chunkPos);
                            if (chunkToTest.isImmuneToUnloading || chunkToTest == theFilledChunk) { continue; } // To not do the same one twice -> infinite loop, or if chunk wasn't loaded (impossible but better safe than salsifi
                            chunkToTest.isImmuneToUnloading = true;
                            newImmuneChunks[chunkToTest] = true;
                        }
                    }
                    newImmuneStructures = new Dictionary<Structure, bool>();
                    foreach (Nest nest in newImmuneNests.Keys)
                    {
                        screen = nest.screen;
                        foreach ((int x, int y) chunkPos in nest.chunkPresence.Keys)
                        {
                            chunkToTest = screen.getChunkFromChunkPos(chunkPos);
                            if (chunkToTest.isImmuneToUnloading || chunkToTest == theFilledChunk) { continue; } // To not do the same one twice -> infinite loop, or if chunk wasn't loaded (impossible but better safe than salsifi
                            chunkToTest.isImmuneToUnloading = true;
                            newImmuneChunks[chunkToTest] = true;
                        }
                    }
                    newImmuneNests = new Dictionary<Nest, bool>();

                    // 4. new Immune chunks set MegaChunks as Immune
                    MegaChunk megaChunkToTest;
                    foreach (Chunk chunk in newImmuneChunks.Keys)
                    {
                        megaChunkToTest = chunk.getMegaChunk();
                        if (megaChunkToTest.isImmuneToUnloading) { continue; } // To not do the same one twice -> infinite loop
                        megaChunkToTest.isImmuneToUnloading = true;
                        newImmuneMegaChunks[megaChunkToTest] = true;
                    }
                    newImmuneChunks = new Dictionary<Chunk, bool>();
                }
            }
            public void zoom(bool isZooming)
            {
                if (isZooming)
                {
                    zoomLevel += 5;
                }
                else
                {
                    zoomLevel -= 5;
                    zoomLevel = Max(zoomLevel, 2);
                }
                findEffectiveChunkLoadingRadius();
                makeGameBitmaps();
                lastZoom = timeElapsed - 100;
            }
            public void findEffectiveChunkLoadingRadius() { effectiveRadius = Max(chunkLoadMininumRadius, RoundUp(zoomLevel, 32) / 32); }
            public void makeGameBitmaps(bool isPngToBeExported = false)
            {
                if (isPngToBeExported) { gameBitmap = new Bitmap(2 * zoomLevel + 1, 2 * zoomLevel + 1); }
                else { gameBitmap = new Bitmap(8 * zoomLevel + 1, 8 * zoomLevel + 1); }
                lightBitmap = new Bitmap(2 * zoomLevel + 1, 2 * zoomLevel + 1);
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
                // unloadAllDimensions(false);
            }
            public Screen loadDimension(int idToLoad, bool isPngToExport = false, bool isMonoToPut = false, int typeToPut = -999, int subTypeToPut = -999)
            {
                if (!loadedScreens.ContainsKey(idToLoad))
                {
                    Screen newScreen = new Screen(this, idToLoad, isMonoToPut, typeToPut, subTypeToPut);
                    loadedScreens[idToLoad] = newScreen;
                    return newScreen;
                }
                return loadedScreens[idToLoad];
            }
            public void unloadDimension(int id)
            {
                Screen screen = loadedScreens[id];
                screen.putEntitiesAndPlantsInChunks();
                saveAllChunks(screen);
                loadedScreens.Remove(id);
            }
            public void unloadAllDimensions(bool unloadDimensionPlayerIsInAsWell)
            {
                foreach (int id in loadedScreens.Keys.ToList())
                {
                    if (unloadDimensionPlayerIsInAsWell || playerList[0].dimension != id)
                    {
                        unloadDimension(id);
                    }
                }
            }
            public Screen getScreen(int id)
            {
                if (!loadedScreens.ContainsKey(id)) { loadDimension(id); }
                return loadedScreens[id];
            }
            public Structure getStructure(int id)
            {
                foreach (Screen screen in loadedScreens.Values)
                {
                    if (screen.activeStructures.ContainsKey(id)) { return screen.activeStructures[id]; }
                    if (screen.inertStructures.ContainsKey(id)) { return screen.inertStructures[id]; }
                }
                return loadStructure(this, id);
            }
        }
        public class Screen
        {
            public Game game;

            public Dictionary<(int x, int y), bool> chunksToSpawnEntitiesIn = new Dictionary<(int x, int y), bool>();
            public Dictionary<(int x, int y), Chunk> loadedChunks = new Dictionary<(int x, int y), Chunk>();
            public Dictionary<(int x, int y), Chunk> extraLoadedChunks = new Dictionary<(int x, int y), Chunk>();
            public Dictionary<(int x, int y), MegaChunk> megaChunks = new Dictionary<(int x, int y), MegaChunk>();
            public Dictionary<(int x, int y), MegaChunk> extraLoadedMegaChunks = new Dictionary<(int x, int y), MegaChunk>();

            public Dictionary<((int x, int y) pos, int layer), int> LCGCacheDict = new Dictionary<((int x, int y) pos, int layer), int>();

            public long seed;
            public int id;
            public (int type, int subType) type;
            public bool isMonoBiome; // if yes, then the whole-ass dimension is only made ouf of ONE biome, that is of the type of... well type. If not, type is the type of dimension and not the biome (like idk normal, frozen, lampadaire, shitpiss world...)

            public (float x, float y) playerStartPos = (0, 0);

            public Dictionary<(int x, int y), int> chunkLoadingPoints = new Dictionary<(int x, int y), int>();

            public Dictionary<int, Entity> activeEntities = new Dictionary<int, Entity>();
            public Dictionary<int, Entity> entitesToRemove = new Dictionary<int, Entity>();
            public Dictionary<int, Entity> entitesToAdd = new Dictionary<int, Entity>();
            public Dictionary<int, Plant> activePlants = new Dictionary<int, Plant>();
            public Dictionary<int, Plant> plantsToRemove = new Dictionary<int, Plant>();
            public Dictionary<int, Plant> plantsToMakeBitmapsOf = new Dictionary<int, Plant>();
            public List<Particle> activeParticles = new List<Particle>();
            public List<Particle> particlesToAdd = new List<Particle>();
            public Dictionary<Particle, bool> particlesToRemove = new Dictionary<Particle, bool>();
            public Dictionary<int, Structure> inertStructures = new Dictionary<int, Structure>(); // structures that are just terrain and don't need to be tested for shit (lakes, cubes...)
            public Dictionary<int, Structure> activeStructures = new Dictionary<int, Structure>(); // structures that are active and can do shit to other shit (like portals)

            public Dictionary<(int x, int y), bool> megaChunksToSave = new Dictionary<(int x, int y), bool>();

            public List<((int x, int y) pos, (int type, int subType) attack)> attacksToDo = new List<((int x, int y) pos, (int type, int subType) attack)>();
            public List<((int x, int y) pos, Color color)> attacksToDraw = new List<((int x, int y), Color color)>();

            public Dictionary<(int, int), List<Plant>> outOfBoundsPlants = new Dictionary<(int, int), List<Plant>>(); // not used as of now but in some functions so can't remove LMAO

            public Dictionary<(int, int), bool> liquidsThatCantGoLeft = new Dictionary<(int, int), bool>();
            public Dictionary<(int, int), bool> liquidsThatCantGoRight = new Dictionary<(int, int), bool>();

            public bool initialLoadFinished = false;

            public int chunkX = 0;
            public int chunkY = 0;

            // debug shit
            public Dictionary<(int x, int y), bool> inertStructureLoadedChunkIndexes = new Dictionary<(int x, int y), bool>();
            public Dictionary<(int x, int y), bool> activeStructureLoadedChunkIndexes = new Dictionary<(int x, int y), bool>();

            public Screen(Game gameToPut, int idToPut, bool isMonoToPut = false, int forceType = -999, int forceSubType = -999)
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
                createDimensionFolders(game, id);
                Player player = game.playerList[0];
                chunkX = ChunkIdx(player.posX);
                chunkY = ChunkIdx(player.posY);
                if (player.dimension == id)
                {
                    forceLoadChunksForAllPoints();
                }

                foreach ((int x, int y) pos in chunksToSpawnEntitiesIn.Keys)
                {
                    if (loadedChunks.ContainsKey(pos))
                    {
                        loadedChunks[pos].spawnEntities();
                    }
                }
            }
            public int getLCGValue(((int x, int y) pos, int layer) key, int noiseAmplitude)
            {
                if (!LCGCacheDict.ContainsKey(key)) { LCGCacheDict[key] = (int)(LCGxy(key, seed) % noiseAmplitude); }
                return LCGCacheDict[key];
            }
            public void addPlantsToChunk(Chunk chunk)
            {
                if (outOfBoundsPlants.ContainsKey((chunk.pos.Item1, chunk.pos.Item2)))
                {
                    chunk.exteriorPlantList = outOfBoundsPlants[(chunk.pos.Item1, chunk.pos.Item2)];
                    outOfBoundsPlants.Remove((chunk.pos.Item1, chunk.pos.Item2));
                }
            }
            public void removePlantsFromChunk(Chunk chunk)
            {
                if (chunk.exteriorPlantList.Count > 0)
                {
                    outOfBoundsPlants.Add((chunk.pos.Item1, chunk.pos.Item2), new List<Plant>());
                }
                foreach (Plant plant in chunk.exteriorPlantList)
                {
                    outOfBoundsPlants[(chunk.pos.Item1, chunk.pos.Item2)].Add(plant);
                }
            }
            public void addRemoveEntities()
            {
                foreach (Entity entity in entitesToRemove.Values) { activeEntities.Remove(entity.id); }
                foreach (Entity entity in entitesToAdd.Values) { activeEntities[entity.id] = entity; }
                entitesToRemove = new Dictionary<int, Entity>();
                entitesToAdd = new Dictionary<int, Entity>();
            }
            public void removePlants()
            {
                foreach (Plant plant in plantsToRemove.Values)
                {
                    foreach ((int x, int y) chunkPos in plant.chunkPresence.Keys) { getChunkFromChunkPos(chunkPos).plants.Remove(plant.id); }
                    activePlants.Remove(plant.id);
                }
                plantsToRemove = new Dictionary<int, Plant>();
            }
            public void makeBitmapsOfPlants()
            {
                foreach (Plant plant in plantsToMakeBitmapsOf.Values)
                {
                    plant.makeBitmap();
                }
                plantsToMakeBitmapsOf = new Dictionary<int, Plant>();
            }
            public void putPlantsInChunks()
            {
                Plant[] plantArray = activePlants.Values.ToArray();
                Plant plant;
                for (int i = 0; i < plantArray.Length; i++)
                {
                    plant = plantArray[i];
                    foreach ((int x, int y) chunkPos in plant.chunkPresence.Keys)
                    {
                        getChunkFromChunkPos(chunkPos).plants[plant.id] = activePlants[plant.id];
                    }
                    activePlants.Remove(plant.id);
                }
            }
            public void putEntitiesInChunks()
            {
                (int, int) chunkIndex;
                List<Entity> cringeEntities = new List<Entity>();
                Entity[] entityArray = activeEntities.Values.ToArray();
                Entity entity;
                for (int i = 0; i < entityArray.Length; i++)
                {
                    entity = entityArray[i];
                    chunkIndex = ChunkIdx(entity.posX, entity.posY);
                    if (loadedChunks.ContainsKey(chunkIndex))
                    {
                        getChunkFromChunkPos(chunkIndex).entityList.Add(activeEntities[entity.id]);
                    }
                    else { cringeEntities.Add(activeEntities[id]); }
                    activeEntities.Remove(entity.id);
                }
                foreach (Entity entito in cringeEntities)
                {
                    activeEntities[entito.id] = entito;
                }
            }
            public void putEntitiesAndPlantsInChunks()
            {
                putEntitiesInChunks();
                putPlantsInChunks();
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
                    foreach (Entity entity in chunko.entityList) { activeEntities[entity.id] = entity; }
                    foreach (Plant plant in chunko.plants.Values) { activePlants[plant.id] = plant; }
                    chunko.entityList = new List<Entity>();
                    chunko.plants = new Dictionary<int, Plant>();
                }
            }
            public void forceLoadChunksForOnePoint((int x, int y) chunkLoadingPos, int magnitude)
            {
                (int x, int y) posToTest;
                for (int i = -magnitude; i < magnitude; i++)
                {
                    for (int j = -magnitude; j < magnitude; j++)
                    {
                        posToTest = (chunkLoadingPos.x + i, chunkLoadingPos.y + j);
                        getChunkFromChunkPos(posToTest, false);
                    }
                }
            }
            public void forceLoadChunksForAllPoints()
            {
                (int x, int y) posToTest;
                int magnitude;
                foreach ((int x, int y) chunkLoadingPos in chunkLoadingPoints.Keys)
                {
                    magnitude = chunkLoadingPoints[chunkLoadingPos];
                    for (int i = -magnitude; i < magnitude; i++)
                    {
                        for (int j = -magnitude; j < magnitude; j++)
                        {
                            posToTest = (chunkLoadingPos.x + i, chunkLoadingPos.y + j);
                            getChunkFromChunkPos(posToTest, false);
                        }
                    }
                }
            }
            public void loadCloseChunks()
            {
                (int x, int y) posToTest;
                int magnitude;
                int count = 0;
                foreach ((int x, int y) chunkLoadingPos in chunkLoadingPoints.Keys)
                {
                    magnitude = chunkLoadingPoints[chunkLoadingPos];
                    for (int i = -1 - magnitude; i <= magnitude; i++)
                    {
                        for (int j = -1 - magnitude; j <= magnitude; j++)
                        {
                            posToTest = (chunkLoadingPos.x + i, chunkLoadingPos.y + j);
                            if (!loadedChunks.ContainsKey(posToTest))
                            {
                                getChunkFromChunkPos(posToTest, false);
                                count++;
                                if (count > 1 + 0.5 * (Abs(game.playerList[0].speedX) + Abs(game.playerList[0].speedY))) { return; }
                            }
                        }
                    }
                }
            }
            public void unloadFarawayChunks() // this function unloads random chunks, that are not in the always loaded square around the player or in nests. HOWEVER, while the farthest away a chunk is, the less chance it has to unload, it still is random. 
            {
                // for debug

                inertStructureLoadedChunkIndexes = new Dictionary<(int x, int y), bool>();
                foreach (Structure structure in inertStructures.Values) { foreach ((int x, int y) tile in structure.chunkPresence.Keys) { inertStructureLoadedChunkIndexes[tile] = true; } }

                activeStructureLoadedChunkIndexes = new Dictionary<(int x, int y), bool>();
                foreach (Structure structure in activeStructures.Values) { foreach ((int x, int y) tile in structure.chunkPresence.Keys) { activeStructureLoadedChunkIndexes[tile] = true; } }

                int magnitude = 0;
                foreach ((int x, int y) pos in chunkLoadingPoints.Keys) { magnitude = Max(magnitude, chunkLoadingPoints[pos]); }
                int minChunkAmount = Max(magnitude, 0) * magnitude + activeStructureLoadedChunkIndexes.Count; // if magnitude < 0 set magnitude squared to 0

                Dictionary<(int x, int y), bool> chunksToRemove = new Dictionary<(int x, int y), bool>();
                (int x, int y)[] chunkKeys = loadedChunks.Keys.ToArray();
                (int x, int y) currentIdx;
                float distanceToCenter;
                for (int i = loadedChunks.Count; i > minChunkAmount; i--)
                {
                    currentIdx = chunkKeys[rand.Next(loadedChunks.Count)];
                    if (!activeStructureLoadedChunkIndexes.ContainsKey(currentIdx))
                    {
                        foreach ((int x, int y) pos in chunkLoadingPoints.Keys)
                        {
                            distanceToCenter = Distance(currentIdx, pos);
                            if ((float)(rand.NextDouble()) * distanceToCenter < 1.6f * chunkLoadingPoints[pos]) { goto Fail; } // if it's too close to ANY point of loading, do not unload
                        }
                        chunksToRemove[currentIdx] = true;
                    Fail:;
                    }
                }

                actuallyUnloadTheChunks(chunksToRemove);
            }
            public void actuallyUnloadTheChunks(Dictionary<(int x, int y), bool> chunksDict)  // Be careful to have put entities and plants inside the chunk before or else they won't be unloaded !! ! ! ! !
            {
                Chunk chunk;
                if (chunksDict.Count > 0 && true)
                {
                    foreach ((int, int) chunkPos in chunksDict.Keys)
                    {
                        if (loadedChunks.ContainsKey(chunkPos))
                        {
                            //removePlantsFromChunk(loadedChunks[chunkPos]);
                            chunk = loadedChunks[chunkPos];
                            saveChunk(loadedChunks[chunkPos]);
                            chunk.demoteToExtra();
                            extraLoadedChunks[chunkPos] = chunk;
                            loadedChunks.Remove(chunkPos);
                        }
                    }
                }
            }
            public void manageExtraLoadedChunks()
            {
                Dictionary<(int x, int y), bool> extraLoadedChunksToRemove = new Dictionary<(int x, int y), bool>();
                foreach (Chunk chunk in extraLoadedChunks.Values)
                {
                    chunk.framesSinceLastExtraGetting += 1;
                    if (chunk.framesSinceLastExtraGetting > 5) { extraLoadedChunksToRemove[chunk.pos] = true; }
                }
                foreach ((int x, int y) pos in extraLoadedChunksToRemove.Keys) { extraLoadedChunks.Remove(pos); }
            }
            public void unloadMegaChunks() // megachunk resolution : 512*512
            {
                Dictionary<(int x, int y), MegaChunk> newMegaChunks = new Dictionary<(int x, int y), MegaChunk>();
                (int x, int y) megaChunkPos;
                foreach ((int x, int y) chunkPos in loadedChunks.Keys.ToArray())
                {
                    megaChunkPos = MegaChunkIdxFromChunkPos(chunkPos);
                    if (newMegaChunks.ContainsKey(megaChunkPos)) { continue; }
                    if (activeStructureLoadedChunkIndexes.ContainsKey(chunkPos)) { continue; }
                    newMegaChunks[megaChunkPos] = getMegaChunkFromMegaPos(megaChunkPos);
                    megaChunks.Remove(megaChunkPos);
                }
                Dictionary<(int x, int y), bool> chunksToRemove = new Dictionary<(int x, int y), bool>();
                foreach (MegaChunk megaChunk in megaChunks.Values)
                {
                    if (megaChunk.isImmuneToUnloading) // Don't unload the megaChunk if it is immune to unloading
                    {
                        newMegaChunks[megaChunk.pos] = megaChunks[megaChunk.pos];
                        continue;
                    }
                    Structure structure;
                    foreach (int structureId in megaChunk.structures)
                    {
                        if (!activeStructures.ContainsKey(structureId)) { continue; }
                        structure = activeStructures[structureId];
                        if (structure.isImmuneToUnloading)  // Don't unload the megaChunk if it has immune structures in it
                        {
                            newMegaChunks[megaChunk.pos] = megaChunks[megaChunk.pos];
                            continue;
                        }
                    }
                    megaChunk.unloadAllNestsAndStructuresAndChunks(chunksToRemove);
                    saveMegaChunk(megaChunk);
                    actuallyUnloadTheChunks(chunksToRemove);
                }
                megaChunks = newMegaChunks;
                foreach ((int x, int y) pos in megaChunksToSave.Keys)
                {
                    if (megaChunks.ContainsKey(pos)) { saveMegaChunk(megaChunks[pos]); } // Else it has been unloaded and has been saved when being unloaded, so no need to save it.
                }
                megaChunksToSave = new Dictionary<(int x, int y), bool>();
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
                (int x, int y) posToDraw = (position.x - camPos.x, position.y - camPos.y);
                if (posToDraw.x >= 0 && posToDraw.x < receiver.Width && posToDraw.y >= 0 && posToDraw.y < receiver.Height)
                {
                    using (var g = Graphics.FromImage(receiver))
                    {
                        g.FillRectangle(new SolidBrush(color), posToDraw.x * PNGmultiplicator, posToDraw.y * PNGmultiplicator, PNGmultiplicator, PNGmultiplicator);
                    }
                }
            }
            public void pasteImage(Bitmap receiver, Bitmap bitmapToDraw, (int x, int y) position, (int x, int y) camPos, int PNGmultiplicator)
            {
                (int x, int y) posToDraw = (position.x - camPos.x, position.y - camPos.y);
                if (true || posToDraw.x >= 0 && posToDraw.x < receiver.Width && posToDraw.y >= 0 && posToDraw.y < receiver.Height)
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

                (int x, int y) posToDraw = (position.x - camPos.x, position.y - camPos.y);
                if (true || posToDraw.x >= -bitmapToDraw.Width && posToDraw.x < game.effectiveRadius * 32 && posToDraw.y >= -bitmapToDraw.Height && posToDraw.y < game.effectiveRadius * 32)
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
            public Bitmap updateScreen(Bitmap gameBitmap, Bitmap lightBitmap, bool isPngToBeExported = false)
            {
                Graphics gg = Graphics.FromImage(gameBitmap);
                gg.Clear(Color.White);
                gg.Dispose();

                List<(int x, int y, int radius, Color color)> lightPositions = new List<(int x, int y, int radius, Color color)>();

                Chunk chunko;
                int PNGmultiplicator = 4;
                if (isPngToBeExported) { PNGmultiplicator = 1; }
                Player player = game.playerList[0];
                (int x, int y) camPos = (player.camPosX - game.zoomLevel, player.camPosY - game.zoomLevel);

                for (int i = -game.effectiveRadius; i <= game.effectiveRadius; i++)
                {
                    for (int j = -game.effectiveRadius; j <= game.effectiveRadius; j++)
                    {
                        chunko = getChunkFromChunkPos((chunkX + i, chunkY + j), !isPngToBeExported);
                        pasteImage(gameBitmap, chunko.bitmap, (chunko.pos.x * 32, chunko.pos.y * 32), camPos, PNGmultiplicator);
                        //if (debugMode) { drawPixel(Color.Red, (chunko.position.x*32, chunko.position.y*32), PNGmultiplicator); } // if want to show chunk origin
                    }
                }

                foreach (Structure structure in activeStructures.Values)
                {
                    if (structure.bitmap != null) { pasteImage(gameBitmap, structure.bitmap, (structure.pos.x + structure.posOffset[0], structure.pos.y + structure.posOffset[1]), camPos, PNGmultiplicator); }
                    if (structure.type.type == 3)
                    {
                        int frame = ((int)(timeElapsed * 10) + (int)(structure.seed.x) % 100) % 4;
                        pasteImage(gameBitmap, livingPortalAnimation.frames[frame], (structure.pos.x + structure.posOffset[0], structure.pos.y + structure.posOffset[1]), camPos, PNGmultiplicator);
                    }
                }

                foreach (Plant plant in activePlants.Values)
                {
                    pasteImage(gameBitmap, plant.bitmap, (plant.posX + plant.posOffset[0], plant.posY + plant.posOffset[1]), camPos, PNGmultiplicator);
                    if (plant.type.type == 0 && plant.type.subType == 1 && plant.childFlowers.Count > 0)
                    {
                        Flower fireFlower = plant.childFlowers[0];
                        int frame = ((int)(timeElapsed*20) + plant.seed % 100) % 6;
                        pasteImage(gameBitmap, fireAnimation.frames[frame], (plant.posX + fireFlower.pos.x /*!!!!!!!!*/ - 1 /*!!!!!!!*/ + plant.posOffset[0], plant.posY + fireFlower.pos.y + plant.posOffset[1]), camPos, PNGmultiplicator);
                    }
                    if (game.isLight)
                    {
                        int radius = 3;
                        if (plant.type.type == 0 && plant.type.subType == 1) { radius = 5; }
                        else if (plant.type.type == 1 && plant.type.subType == 1) { radius = 11; }

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

                // player
                Color playerColor = player.color;
                if (getChunkFromPixelPos((player.posX, player.posY)).fillStates[PosMod(player.posX), PosMod(player.posY)].type > 0) { playerColor = Color.Red; }
                if (game.isLight) { lightPositions.Add((player.posX, player.posY, 9, player.lightColor)); }
                drawPixel(gameBitmap, playerColor, (player.posX, player.posY), camPos, PNGmultiplicator);

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
                    for (int i = -game.effectiveRadius; i <= game.effectiveRadius; i++)
                    {
                        for (int j = -game.effectiveRadius; j <= game.effectiveRadius; j++)
                        {
                            chunko = getChunkFromChunkPos((chunkX + i, chunkY + j));
                            pasteImage(lightBitmap, chunko.lightBitmap, (chunko.pos.x * 32, chunko.pos.y * 32), camPos, 1);
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

                    Bitmap resizedBitmap = new Bitmap(gameBitmap.Width, gameBitmap.Height);
                    using (Graphics g = Graphics.FromImage(resizedBitmap))
                    {
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        g.DrawImage(lightBitmap, 0, 0, resizedBitmap.Width, resizedBitmap.Height);
                    }
                    pasteLightBitmapOnGameBitmap(gameBitmap, resizedBitmap);
                }

                if (!debugMode) // fog of war
                {
                    for (int i = -game.effectiveRadius; i <= game.effectiveRadius; i++)
                    {
                        for (int j = -game.effectiveRadius; j <= game.effectiveRadius; j++)
                        {
                            chunko = getChunkFromChunkPos((chunkX + i, chunkY + j));
                            if (chunko.explorationLevel == 0)
                            {
                                pasteImage(gameBitmap, black32Bitmap, (chunko.pos.x * 32, chunko.pos.y * 32), camPos, PNGmultiplicator);
                            }
                            else if (chunko.explorationLevel == 1)
                            {                                           // THIS SHOULD NEVER HAPPEN ! Like actually what the fuck ! Due to switching from Debug mode to not debug it seems
                                pasteImage(gameBitmap, chunko.fogBitmap ?? transBlue32Bitmap, (chunko.pos.x * 32, chunko.pos.y * 32), camPos, PNGmultiplicator);
                            }
                        }
                    }
                }

                if (debugMode && !isPngToBeExported) // debug for nests
                {
                    foreach (Structure structure in activeStructures.Values)
                    {
                        Nest nest = structure.getItselfAsNest();
                        if (nest == null) { continue; }
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
                    int xOffset = (game.loadedScreens.Count - 1)*50;
                    foreach (Screen screenToDebug in game.loadedScreens.Values)
                    {
                        Color colorToDraw;
                        (int x, int y) cameraChunkIdx = (ChunkIdx(game.playerList[0].posX), ChunkIdx(game.playerList[0].posY));
                        foreach ((int x, int y) poso in screenToDebug.megaChunks.Keys)
                        {
                            if (player.screen == screenToDebug) { colorToDraw = Color.IndianRed; }
                            else { colorToDraw = Color.Crimson; }
                            drawPixelFixed(gameBitmap, colorToDraw, (300 + poso.x * 16 - cameraChunkIdx.x - xOffset, 300 + poso.y * 16 - cameraChunkIdx.y), 16);
                        }
                        foreach ((int x, int y) poso in screenToDebug.activeStructureLoadedChunkIndexes.Keys)
                        {
                            colorToDraw = Color.FromArgb(100, 255, 255, 100);
                            drawPixelFixed(gameBitmap, colorToDraw, (300 + poso.x - cameraChunkIdx.x - xOffset, 300 + poso.y - cameraChunkIdx.y), 1);
                        }
                        foreach ((int x, int y) poso in screenToDebug.extraLoadedChunks.Keys)
                        {
                            colorToDraw = Color.Purple;
                            drawPixelFixed(gameBitmap, colorToDraw, (300 + poso.x - cameraChunkIdx.x - xOffset, 300 + poso.y - cameraChunkIdx.y), 1);
                        }
                        foreach ((int x, int y) poso in screenToDebug.loadedChunks.Keys)
                        {
                            colorToDraw = Color.FromArgb(150, 0, 128, 0);
                            if (screenToDebug.loadedChunks[poso].unstableLiquidCount > 0) { colorToDraw = Color.DarkBlue; }
                            else if (screenToDebug.activeStructureLoadedChunkIndexes.ContainsKey(poso)) { colorToDraw = Color.Cyan; }
                            else if (screenToDebug.inertStructureLoadedChunkIndexes.ContainsKey(poso)) { colorToDraw = Color.FromArgb(130, 50, 130); }
                            drawPixelFixed(gameBitmap, colorToDraw, (300 + poso.x - cameraChunkIdx.x - xOffset, 300 + poso.y - cameraChunkIdx.y), 1);
                        }
                        drawPixelFixed(gameBitmap, Color.Red, (300 + ChunkIdx(player.posX) - cameraChunkIdx.x - xOffset, 300 + ChunkIdx(player.posY) - cameraChunkIdx.y), 1);

                        foreach (Chunk chunkoko in screenToDebug.loadedChunks.Values)
                        {
                            if (chunkoko.unstableLiquidCount > 0) { pasteImage(gameBitmap, transBlue32Bitmap, (chunkoko.pos.x * 32 - xOffset, chunkoko.pos.y * 32), camPos, PNGmultiplicator); }
                        }
                        xOffset -= 50;
                    }
                }



                gameBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                return gameBitmap;
            }
            public (int type, int subType) getTileContent((int x, int y) posToTest)
            {
                return getChunkFromPixelPos(posToTest).fillStates[PosMod(posToTest.x), PosMod(posToTest.y)];
            }
            public (int type, int subType) setTileContent((int x, int y) posToTest, (int type, int subType) typeToSet)
            {
                return getChunkFromPixelPos(posToTest).tileModification(posToTest.x, posToTest.y, typeToSet);
            }
            public Chunk getChunkFromPixelPos((int x, int y) pos, bool isExtraGetting = true, bool onlyGetIfFullyLoaded = false, Dictionary<(int x, int y), Chunk> extraDict = null)
            {
                return getChunkFromChunkPos(ChunkIdx(pos), isExtraGetting, onlyGetIfFullyLoaded, extraDict);
            }
            public Chunk getChunkFromChunkPos((int x, int y) pos, bool isExtraGetting = true, bool onlyGetIfFullyLoaded = false, Dictionary<(int x, int y), Chunk> extraDict = null)  // Extradict is used
            {
                Chunk chunkToGet;
                if (loadedChunks.ContainsKey(pos))
                {
                    if (extraDict != null) { extraDict[pos] = loadedChunks[pos]; }
                    return loadedChunks[pos];
                }
                else if (onlyGetIfFullyLoaded) { return null; }
                if (extraLoadedChunks.ContainsKey(pos))
                {
                    chunkToGet = extraLoadedChunks[pos];
                    if (isExtraGetting)
                    {
                        chunkToGet.framesSinceLastExtraGetting = 0;
                        if (extraDict != null) { extraDict[pos] = chunkToGet; }
                        return chunkToGet;
                    }
                    chunkToGet.promoteFromExtraToFullyLoaded(getChunkJson(this, pos));  // Upgrade the extraLoaded Chunk to a full Chunk, by loading all its entities, fog, plants... etc
                    loadedChunks[pos] = chunkToGet;
                    extraLoadedMegaChunks.Remove(pos);
                    if (extraDict != null) { extraDict[pos] = chunkToGet; }
                    return chunkToGet;
                }
                chunkToGet = loadChunk(this, pos, isExtraGetting);
                if (extraDict != null) { extraDict[pos] = chunkToGet; }
                return chunkToGet;
            }
            public MegaChunk getMegaChunkFromPixelPos((int x, int y) pos, bool isExtraGetting = true)
            {
                return getMegaChunkFromMegaPos(MegaChunkIdxFromPixelPos(pos), isExtraGetting);
            }
            public MegaChunk getMegaChunkFromChunkPos((int x, int y) pos, bool isExtraGetting = true)
            {
                return getMegaChunkFromMegaPos(MegaChunkIdxFromChunkPos(pos), isExtraGetting);
            }
            public MegaChunk getMegaChunkFromMegaPos((int x, int y) pos, bool isExtraGetting = true)
            {
                if (megaChunks.ContainsKey(pos)) { return megaChunks[pos]; }
                if (extraLoadedMegaChunks.ContainsKey(pos))
                {
                    if (isExtraGetting) { return extraLoadedMegaChunks[pos]; }
                    MegaChunk megaChunkToGet = extraLoadedMegaChunks[pos];
                    megaChunkToGet.loadAllStuffInIt();  // Upgrade the extraLoaded MegaChunk to a full MegaChunk, by loading all its contents and putting it in the other dict
                    megaChunks[pos] = megaChunkToGet;
                    extraLoadedMegaChunks.Remove(pos);
                    return megaChunkToGet;
                }
                return loadMegaChunk(this, pos, isExtraGetting);
            }
        }
    }
}
