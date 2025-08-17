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
    public class Screens
    {
        public class Game
        {
            public List<string> structureGenerationLogs = new List<string>();
            public Dictionary<(int dim, int x, int y), int> structureGenerationLogsStructureUpdateCount = new Dictionary<(int dim, int x, int y), int>();

            public List<((int x, int y) pos, Color col)> miscDebugList = new List<((int x, int y) pos, Color col)>();

            public Dictionary<int, Screen> loadedScreens = new Dictionary<int, Screen>();
            public List<Player> playerList = new List<Player>();

            public Dictionary<int, Structure> structuresToAdd = new Dictionary<int, Structure>();
            public Dictionary<int, Structure> structuresToRemove = new Dictionary<int, Structure>();

            public Bitmap overlayBitmap;
            public long seed;
            public int livingDimensionId = -1;

            public int zoomLevel;
            public float realZoomLevel;
            public int effectiveRadius;
            public int PNGmultiplicator;
            public float zoomingSpeed = 0;

            public Bitmap gameBitmap;
            public Bitmap lightBitmap;
            public Bitmap finalBitmap;
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
                realZoomLevel = zoomLevel;
                effectiveRadius = chunkLoadMininumRadius;

                bool isMonoeBiomeToPut = false;
                bool isPngToExport = false;

                loadStructuresYesOrNo = true;
                spawnNests = false;
                spawnEntitiesBool = true;
                spawnPlants = true;
                bool spawnNOTHING = false;
                bool spawnEVERYTHING = false;
                if (spawnNOTHING) { loadStructuresYesOrNo = false; spawnEntitiesBool = false; spawnPlants = false; }
                if (spawnEVERYTHING) { loadStructuresYesOrNo = true; spawnEntitiesBool = true; spawnPlants = true; }

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
                    makeBiomeDiagram((0, 0), (a, b), (c, d), "-");
                }
                int counti = 0;
                for (int i = -512; i < -1000/*1536*/; i += 64)
                {
                    makeBiomeDiagram((0, 0), (0, 1), (i, 512), counti.ToString());
                    counti++;
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
                    makeGameBitmaps();
                    loadedScreens[idToPut].updateScreen(true).Save($"{currentDirectory}\\caveMap.png");
                    makeGameBitmaps();
                    chunkLoadMininumRadius = oldChunkLength;
                    findEffectiveChunkLoadingRadius();
                }
                else
                {
                    makeGameBitmaps();
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
                    screen.attacksToDo = new List<((int x, int y) pos, Attack attack)>();
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
                zoomUpdate();
                



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

                    foreach ((int x, int y) pos in screen.chunksToMature.Keys)
                    {
                        if (screen.loadedChunks.ContainsKey(pos)) { screen.loadedChunks[pos].matureChunk(); }
                    }
                    screen.chunksToMature = new Dictionary<(int x, int y), bool>();

                    foreach (Plant plant in screen.activePlants.Values) { plant.testPlantGrowth(false); }
                    screen.putPlantsInChunks();

                    foreach (Entity entity in screen.activeEntities.Values) { entity.moveEntity(); }
                    screen.addRemoveEntities();

                    screen.attacksToRemove = new Dictionary<Attack, bool>();
                    for (int i = 0; i < screen.activeAttacks.Count; i++) { screen.activeAttacks[i].updateAttack(); }
                    foreach (Attack attack in screen.attacksToRemove.Keys) { screen.activeAttacks.Remove(attack); }

                    foreach (Particle particle in screen.activeParticles) { particle.moveParticle(); }
                    foreach (Structure structure in screen.activeStructures.Values) { structure.moveStructure(); }
                    screen.addRemoveEntities();
                    foreach ((int x, int y) pos in screen.loadedChunks.Keys)
                    {
                        if (rand.Next(200) == 0) { screen.loadedChunks[(pos.x, pos.y)].unstableLiquidCount++; }
                        screen.loadedChunks[(pos.x, pos.y)].moveLiquids();
                    }

                    screen.makeBitmapsOfPlants();
                    screen.putEntitiesInChunks();
                    screen.attacksToRemove = new Dictionary<Attack, bool>();
                    foreach (((int x, int y) pos, Attack attack) attack in screen.attacksToDo) { attack.attack.sendAttack(attack.pos); }
                    foreach (Attack attack in screen.attacksToRemove.Keys) { screen.activeAttacks.Remove(attack); }
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
                gamePictureBox.Image = playerScreen.updateScreen();
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
            public void zoomUpdate()
            {
                if (zoomPress[0] == zoomPress[1])
                {
                    zoomingSpeed = Sign(zoomingSpeed) * Max(0.7f * Abs(zoomingSpeed) - 1, 0);
                }
                else
                {
                    float factor = realZoomLevel / 50;
                    float limit = Max(1, factor * 3);
                    int sign = zoomPress[0] ? 1 : -1;
                    zoomingSpeed = Clamp(-limit, zoomingSpeed, limit) + sign * factor;
                }
                if (zoomingSpeed != 0)
                {
                    realZoomLevel = Max(2, realZoomLevel + zoomingSpeed);
                    zoomLevel = (int)Floor(realZoomLevel, 1);
                    findEffectiveChunkLoadingRadius();
                    makeGameBitmaps();
                }
            }
            public void findEffectiveChunkLoadingRadius() { effectiveRadius = Max(chunkLoadMininumRadius, RoundUp(zoomLevel, 32) / 32); }
            public void makeGameBitmaps()
            {
                PNGmultiplicator = Max(1, 320 / zoomLevel);
                gameBitmap = new Bitmap(2 * zoomLevel + 1, 2 * zoomLevel + 1);
                lightBitmap = new Bitmap(2 * zoomLevel + 1, 2 * zoomLevel + 1);
                finalBitmap = PNGmultiplicator == 1 ? gameBitmap : new Bitmap(2 * PNGmultiplicator * zoomLevel + 1, 2 * PNGmultiplicator * zoomLevel + 1); ;
            }
            public void setPlayerDimension(Player player, int targetDimension)
            {
                if (targetDimension > currentDimensionId) { targetDimension = currentDimensionId; currentTargetDimension = currentDimensionId; }
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

            public Dictionary<(int x, int y), bool> chunksToMature = new Dictionary<(int x, int y), bool>();
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
            public List<Attack> activeAttacks = new List<Attack>();
            public Dictionary<Attack, bool> attacksToRemove = new Dictionary<Attack, bool>();

            public Dictionary<(int x, int y), bool> megaChunksToSave = new Dictionary<(int x, int y), bool>();

            public List<((int x, int y) pos, Attack attack)> attacksToDo = new List<((int x, int y) pos, Attack attack)>();
            public List<((int x, int y) pos, Color color)> attacksToDraw = new List<((int x, int y), Color color)>();

            public Dictionary<(int, int), List<Plant>> outOfBoundsPlants = new Dictionary<(int, int), List<Plant>>(); // not used as of now but in some functions so can't remove LMAO

            public Dictionary<(int, int), bool> liquidsThatCantGoLeft = new Dictionary<(int, int), bool>();
            public Dictionary<(int, int), bool> liquidsThatCantGoRight = new Dictionary<(int, int), bool>();

            public HashSet<(int x, int y)> climbablePositions = new HashSet<(int x, int y)>();

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
                            type = biomeTraitsDict.Keys.ToArray()[rand.Next(biomeTraitsDict.Count)];
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
                if (player.dimension == id) { forceLoadChunksForAllPoints(); }

                foreach ((int x, int y) pos in chunksToMature.Keys)
                {
                    if (loadedChunks.ContainsKey(pos)) { loadedChunks[pos].matureChunk(); }
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
                climbablePositions = new HashSet<(int x, int y)>();

                Plant[] plantArray = activePlants.Values.ToArray();
                Plant plant;
                for (int i = 0; i < plantArray.Length; i++)
                {
                    plant = plantArray[i];
                    foreach ((int x, int y) chunkPos in plant.chunkPresence.Keys) { getChunkFromChunkPos(chunkPos).plants[plant.id] = activePlants[plant.id]; }
                    if (plant.traits.isClimbable)
                    {
                        foreach (PlantElement plantElement in plant.returnAllPlantElements())
                        if (plantElement.traits.isClimbable)
                        {
                            foreach ((int x, int y) pos in plantElement.fillStates.Keys)
                            {
                                (int x, int y) pososo = plantElement.getWorldPos(pos);
                                climbablePositions.Add(pososo);
                                foreach ((int x, int y) mod in neighbourArray) { climbablePositions.Add((pososo.x + mod.x, pososo.y + mod.y)); }
                            }
                        }
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
                    else { if (activeEntities.ContainsKey(id)) { cringeEntities.Add(activeEntities[id]); } }    // Cuz sometimes they disappear ?????????? what ???
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
            public void drawPixelFixed(Bitmap receiver, Color color, (int x, int y) posToDraw, (int x, int y) scaledOffset, int scale, bool fromTopRightCorner = false)
            {
                using (var g = Graphics.FromImage(receiver))
                {
                    g.FillRectangle(new SolidBrush(color), fromTopRightCorner ? receiver.Width - (1 + posToDraw.x + scaledOffset.x * scale) : posToDraw.x + scaledOffset.x * scale, fromTopRightCorner ? receiver.Width - (1 + posToDraw.y + scaledOffset.y * scale) : posToDraw.y + scaledOffset.y * scale, scale, scale);
                }
            }
            public void drawPixel(Bitmap receiver, Color color, (int x, int y) position, (int x, int y) camPos, int PNGmultiplicator = 1)
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
            public bool pasteImage(Bitmap receiver, Bitmap bitmapToDraw, (int x, int y) position, (int x, int y) camPos, int PNGmultiplicator = 1)
            {
                (int x, int y) posToDraw = (position.x - camPos.x, position.y - camPos.y);
                if (PNGmultiplicator != 1 || posToDraw.x + bitmapToDraw.Width >= 0 && posToDraw.x - bitmapToDraw.Width < receiver.Width && posToDraw.y + bitmapToDraw.Height >= 0 && posToDraw.y - bitmapToDraw.Height < receiver.Height)
                {
                    using (Graphics g = Graphics.FromImage(receiver))
                    {
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        g.DrawImage(bitmapToDraw, posToDraw.x * PNGmultiplicator, posToDraw.y * PNGmultiplicator, bitmapToDraw.Width * PNGmultiplicator, bitmapToDraw.Height * PNGmultiplicator);
                    }
                }
                return true;
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
            public Bitmap updateScreen(bool isPngToBeExported = false)
            {
                Bitmap gameBitmap = game.gameBitmap;
                Bitmap lightBitmap = game.lightBitmap;

                Graphics gg = Graphics.FromImage(gameBitmap);
                gg.Clear(Color.White);
                gg.Dispose();

                List<(int x, int y, int radius, Color color)> lightPositions = new List<(int x, int y, int radius, Color color)>();

                Player player = game.playerList[0];
                (int x, int y) camPos = (player.camPosX - game.zoomLevel, player.camPosY - game.zoomLevel);

                drawChunksOnScreen(gameBitmap, camPos, isPngToBeExported);

                foreach (Structure structure in activeStructures.Values) { drawStructureOnScreen(gameBitmap, camPos, lightPositions, structure); }

                foreach (Plant plant in activePlants.Values) { drawPlantOnScreen(gameBitmap, camPos, lightPositions, plant); }
                foreach (Entity entity in activeEntities.Values) { drawEntityOnScreen(gameBitmap, camPos, lightPositions, entity); }
                foreach (Particle particle in activeParticles) { drawParticleOnScreen(gameBitmap, camPos, lightPositions, particle); }
                drawPlayerOnScreen(gameBitmap, camPos, lightPositions, player);

                drawAttacksOnScreen(gameBitmap, camPos, isPngToBeExported);

                drawLightOnScreen(gameBitmap, lightBitmap, camPos, lightPositions);

                drawFogOfWarOnScreen(gameBitmap, camPos);                

                if (debugMode && !isPngToBeExported && false) { drawNestDebugOnScreen(gameBitmap, camPos); } // debug for nests
                if (debugMode && !isPngToBeExported && true) { drawEntityPathDebugOnScreen(gameBitmap, camPos, player); } // debug for paths
                if (debugMode && !isPngToBeExported && false) { drawMiscDebugOnScreen(gameBitmap, camPos); } // debug misc
                if (true) { game.miscDebugList = new List<((int x, int y) pos, Color col)>(); }  // For memory leak ig blah blah

                // Upscale 1*1 bitmap into the pngmult*pngmult bitmap !
                Bitmap finalBitmap = game.finalBitmap;
                if (game.PNGmultiplicator > 1) { pasteImage(finalBitmap, gameBitmap, (0, 0), (0, 0), game.PNGmultiplicator); }

                if (debugMode && !isPngToBeExported) { drawChunksAndMegachunksDebugOnScreen(finalBitmap, player); } // debug shit for chunks and megachunks

                finalBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                return finalBitmap;
            }
            public void drawChunksOnScreen(Bitmap gameBitmap, (int x, int y) camPos, bool isPngToBeExported)
            {
                Chunk chunko;
                for (int i = -game.effectiveRadius; i <= game.effectiveRadius; i++)
                {
                    for (int j = -game.effectiveRadius; j <= game.effectiveRadius; j++)
                    {
                        chunko = getChunkFromChunkPos((chunkX + i, chunkY + j), !isPngToBeExported);
                        pasteImage(gameBitmap, chunko.bitmap, (chunko.pos.x * 32, chunko.pos.y * 32), camPos);
                        //if (debugMode) { drawPixel(Color.Red, (chunko.position.x*32, chunko.position.y*32)); } // if want to show chunk origin
                    }
                }
            }
            public void drawStructureOnScreen(Bitmap gameBitmap, (int x, int y) camPos, List<(int x, int y, int radius, Color color)> lightPositions, Structure structure)
            {
                // NOT USED AND WAS NEVER USED but keep in case idk // if (structure.bitmap != null) { pasteImage(gameBitmap, structure.bitmap, (structure.pos.x + structure.posOffset.x, structure.pos.y + structure.posOffset.y), camPos); }
                if (structure.animation != null)
                {
                    int frame = ((int)(timeElapsed * 10) + (int)(structure.seed.x) % 100) % 4;
                    pasteImage(gameBitmap, structure.animation.frames[frame], (structure.pos.x + structure.animation.offset.x, structure.pos.y + structure.animation.offset.y), camPos);
                }
            }
            public void drawPlantOnScreen(Bitmap gameBitmap, (int x, int y) camPos, List<(int x, int y, int radius, Color color)> lightPositions, Plant plant)
            {
                if (plant.bounds.x.max + plant.outOfBoundsVisibility < camPos.x || plant.bounds.x.min - plant.outOfBoundsVisibility > camPos.x + game.zoomLevel * 2 + 1 || plant.bounds.y.max + plant.outOfBoundsVisibility < camPos.y || plant.bounds.y.min - plant.outOfBoundsVisibility > camPos.y + game.zoomLevel * 2 + 1) { return; }

                pasteImage(gameBitmap, plant.bitmap, (plant.posX + plant.posOffset.x, plant.posY + plant.posOffset.y), camPos);
                if (plant.animatedPlantElements != null && !debugMode)
                {
                    foreach (PlantElement plantElement in plant.animatedPlantElements)
                    {
                        int frame = ((int)(timeElapsed * 20) + plantElement.seed % 100) % 6;
                        (int x, int y) posToDraw = plantElement.motherPlant.getRealPos(plantElement.pos);
                        pasteImage(gameBitmap, plantElement.traits.animation.frames[frame], (posToDraw.x + plantElement.traits.animation.offset.x, posToDraw.y + plantElement.traits.animation.offset.y), camPos);
                    }
                }
                if (game.isLight)
                {
                    if (plant.lightPositions != null) { foreach ((int x, int y, int radius, Color color) item in plant.lightPositions) { lightPositions.Add(item); } }
                }
            }
            public (int x, int y) findPreviousPastPosition(Entity entity, int segment, (int x, int y) segmentPos, (int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color) item)
            {
                if (segment == 1) {; }
                if (entity.pastPositions.Count < 2) { return (entity.posX, entity.posY); }
                (int x, int y) tempPos = segmentPos;
                if (segment <= 0)
                {
                    for (int i = Clamp(0, segment + 1, entity.pastPositions.Count); i < entity.pastPositions.Count && i >= 0; i++)
                    {
                        tempPos = entity.pastPositions[i];
                        if (tempPos != segmentPos) { return (entity.posX - (tempPos.x - entity.posX), entity.posY - (tempPos.y - entity.posY)); }
                    }
                }
                else
                {
                    for (int i = Clamp(0, segment - 1, entity.pastPositions.Count); i < entity.pastPositions.Count && i >= 0; i--)
                    {
                        tempPos = entity.pastPositions[i];
                        if (tempPos != segmentPos) { return tempPos; }
                    }
                }
                return tempPos;
            }
            public void drawEntityOnScreen(Bitmap gameBitmap, (int x, int y) camPos, List<(int x, int y, int radius, Color color)> lightPositions, Entity entity, int forceLightRadius = 0)
            {
                if (entity.posX + entity.length < camPos.x || entity.posX - entity.length > camPos.x + game.zoomLevel * 2 + 1 || entity.posY + entity.length < camPos.y || entity.posY - entity.length > camPos.y + game.zoomLevel * 2 + 1) { return; }

                Color color = entity.color;
                if (entity.timeAtLastGottenHit > timeElapsed - 0.5f)
                {
                    float redMult = Min(1, (entity.timeAtLastGottenHit - timeElapsed + 0.5f) * 3);
                    float entityMult = 1 - redMult;
                    color = Color.FromArgb((int)(entityMult * color.R + redMult * 255), (int)(entityMult * color.G), (int)(entityMult * color.B));
                }
                if (game.isLight && (entity.traits.lightRadius != null || forceLightRadius > 0)) { lightPositions.Add((entity.posX, entity.posY, entity.traits.lightRadius is null ? forceLightRadius : entity.traits.lightRadius.Value, entity.lightColor)); }
                if (!entity.traits.transparentTail) { drawPixel(gameBitmap, color, (entity.posX, entity.posY), camPos); }
                if (entity.length > 0 || entity.traits.tailMap != null)
                {
                    if (!entity.traits.transparentTail)
                    {
                        int county = entity.pastPositions.Count;
                        for (int i = 0; i < entity.length - 1; i++)
                        {
                            if (i >= county) { break; }
                            drawPixel(gameBitmap, color, entity.pastPositions[i], camPos);
                        }
                    }

                    if (entity.traits.tailMap != null)
                    {
                        foreach ((int segment, bool fromEnd, bool oriented, int angleMod, (int x, int y) pos, (bool isVariation, int? lightRadius, (int a, int r, int g, int b) value)? color) tupelo in entity.traits.tailMap)
                        {
                            int segment = tupelo.fromEnd ? entity.pastPositions.Count - 1 - tupelo.segment : tupelo.segment - 1;
                            if (segment < -1 || entity.pastPositions.Count <= segment) { continue; }

                            (int x, int y) segmentPos = segment <= -1 ? (entity.posX, entity.posY) : entity.pastPositions[segment];
                            (int x, int y) previousSegmentPos = findPreviousPastPosition(entity, segment, segmentPos, tupelo);
                            int signX = tupelo.oriented ? 1 : SignZero(previousSegmentPos.x - segmentPos.x);
                            int angleModo = tupelo.oriented ? directionPositionDictionary[(SignZero(previousSegmentPos.x - segmentPos.x), SignZero(previousSegmentPos.y - segmentPos.y))] : 0;
                            (int x, int y) poso = rotate8(tupelo.pos, tupelo.angleMod + angleModo);

                            Color tailColor = color;
                            if (tupelo.color.HasValue)
                            {
                                if (tupelo.color.Value.isVariation == true) { tailColor = Color.FromArgb(ColorClamp(color.A + tupelo.color.Value.value.a), ColorClamp(color.R + tupelo.color.Value.value.r), ColorClamp(color.G + tupelo.color.Value.value.g), ColorClamp(color.B + tupelo.color.Value.value.b)); }
                                else { tailColor = Color.FromArgb(ColorClamp(tupelo.color.Value.value.a), ColorClamp(tupelo.color.Value.value.r), ColorClamp(tupelo.color.Value.value.g), ColorClamp(tupelo.color.Value.value.b)); }
                                if (tupelo.color.Value.lightRadius != null) { lightPositions.Add((segmentPos.x + signX * poso.x, segmentPos.y + poso.y, tupelo.color.Value.lightRadius.Value, entity.getLightColorFromColor(tailColor))); }
                            }

                            drawPixel(gameBitmap, tailColor, (segmentPos.x + signX * poso.x, segmentPos.y + poso.y), camPos);
                        }
                    }
                }
                if (entity.traits.wingTraits != null)
                {
                    Color wingColor;
                    if (entity.traits.wingTraits.Value.color.isVariation == true) { wingColor = Color.FromArgb(ColorClamp(color.A + entity.traits.wingTraits.Value.color.value.a), ColorClamp(color.R + entity.traits.wingTraits.Value.color.value.r), ColorClamp(color.G + entity.traits.wingTraits.Value.color.value.g), ColorClamp(color.B + entity.traits.wingTraits.Value.color.value.b)); }
                    else { wingColor = Color.FromArgb(ColorClamp(entity.traits.wingTraits.Value.color.value.a), ColorClamp(entity.traits.wingTraits.Value.color.value.r), ColorClamp(entity.traits.wingTraits.Value.color.value.g), ColorClamp(entity.traits.wingTraits.Value.color.value.b)); }
                    (int x, int y) wingPos = wingPosArray[(int)(entity.wingTimer / entity.traits.wingTraits.Value.period) % 8];
                    if (entity.traits.wingTraits.Value.type == 0)
                    {
                        int forceX = 0;
                        if (entity.speedX >= entity.traits.wingTraits.Value.turningSpeed * 2) { forceX = -1; }
                        else if (entity.speedX <= -entity.traits.wingTraits.Value.turningSpeed * 2) { forceX = 1; }
                        drawPixel(gameBitmap, wingColor, (entity.posX + (forceX == 0 ? (entity.speedX >= -entity.traits.wingTraits.Value.turningSpeed ? wingPos.x : 0) : forceX), entity.posY + wingPos.y), camPos);
                        drawPixel(gameBitmap, wingColor, (entity.posX + (forceX == 0 ? (entity.speedX <= entity.traits.wingTraits.Value.turningSpeed ? -wingPos.x : 0) : forceX), entity.posY + wingPos.y), camPos);
                    }
                    else if (entity.traits.wingTraits.Value.type == 1)
                    {
                        if (Abs(entity.speedX) > entity.traits.wingTraits.Value.turningSpeed || Abs(entity.speedY) > entity.traits.wingTraits.Value.turningSpeed)
                        {
                            wingPos = rotate8((1, 0), wingPos.y + directionPositionDictionary[(Abs(entity.speedY) >= 2 * Abs(entity.speedX) ? 0 : SignZero(entity.speedX), Abs(entity.speedX) >= 2 * Abs(entity.speedY) ? 0 : SignZero(entity.speedY))]);   // else if would never go orthoganal (even if speedX = 3 and speedY = 0.05 for example)
                            drawPixel(gameBitmap, wingColor, (entity.posX + wingPos.x, entity.posY + wingPos.y), camPos);
                        }
                    }
                }
            }
            public void drawPlayerOnScreen(Bitmap gameBitmap, (int x, int y) camPos, List<(int x, int y, int radius, Color color)> lightPositions, Player player)
            {
                drawEntityOnScreen(gameBitmap, camPos, lightPositions, player, 7);
                Color playerColor = player.color;
                if (!player.traits.isDigging && getChunkFromPixelPos((player.posX, player.posY)).fillStates[PosMod(player.posX), PosMod(player.posY)].isSolid) { drawPixel(gameBitmap, Color.Red, (player.posX, player.posY), camPos); }
                if (debugMode)
                {
                    Color directionColor = Color.FromArgb(100, playerColor.R, playerColor.G, playerColor.B);
                    for (int i = 1; i <= 3; i++) { drawPixel(gameBitmap, directionColor, (player.posX + player.direction.x * i, player.posY + player.direction.y * i), camPos); }
                }
            }
            public void drawParticleOnScreen(Bitmap gameBitmap, (int x, int y) camPos, List<(int x, int y, int radius, Color color)> lightPositions, Particle particle, int forceLightRadius = 0)
            {
                if (particle.posX < camPos.x || particle.posX > camPos.x + game.zoomLevel * 2 + 1 || particle.posY < camPos.y || particle.posY > camPos.y + game.zoomLevel * 2 + 1) { return; }

                Color color = particle.color;
                //if (game.isLight && entity.type == 0) { lightPositions.Add((particle.posX, particle.posY, 7, particle.lightColor)); }
                drawPixel(gameBitmap, color, (particle.posX, particle.posY), camPos);
            }
            public void drawAttacksOnScreen(Bitmap gameBitmap, (int x, int y) camPos, bool isPngToBeExported)
            {
                foreach (((int x, int y) pos, Color color) item in attacksToDraw) { drawPixel(gameBitmap, item.color, item.pos, camPos); }
                if (debugMode && !isPngToBeExported)
                {
                    foreach (((int x, int y) pos, Attack attack) attack in attacksToDo)
                    {
                        drawPixel(gameBitmap, Color.IndianRed, attack.pos, camPos);
                    }
                }
            }
            public void drawLightOnScreen(Bitmap gameBitmap, Bitmap lightBitmap, (int x, int y) camPos, List<(int x, int y, int radius, Color color)> lightPositions)
            {
                Chunk chunko;
                if (game.isLight && !debugMode) // light shit
                {
                    for (int i = -game.effectiveRadius; i <= game.effectiveRadius; i++)
                    {
                        for (int j = -game.effectiveRadius; j <= game.effectiveRadius; j++)
                        {
                            chunko = getChunkFromChunkPos((chunkX + i, chunkY + j));
                            pasteImage(lightBitmap, chunko.lightBitmap, (chunko.pos.x * 32, chunko.pos.y * 32), camPos, 1);
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
            }
            public void drawFogOfWarOnScreen(Bitmap gameBitmap, (int x, int y) camPos)
            {
                Chunk chunko;
                if (!debugMode) // fog of war
                {
                    for (int i = -game.effectiveRadius; i <= game.effectiveRadius; i++)
                    {
                        for (int j = -game.effectiveRadius; j <= game.effectiveRadius; j++)
                        {
                            chunko = getChunkFromChunkPos((chunkX + i, chunkY + j));
                            if (chunko.explorationLevel == 0) { pasteImage(gameBitmap, black32Bitmap, (chunko.pos.x * 32, chunko.pos.y * 32), camPos); }
                            else if (chunko.explorationLevel == 1)
                            {                                           //    \/ \/ \/ THIS SHOULD NEVER HAPPEN ! Like actually what the fuck ! Due to switching from Debug mode to not debug it seems
                                pasteImage(gameBitmap, chunko.fogBitmap ?? transBlue32Bitmap, (chunko.pos.x * 32, chunko.pos.y * 32), camPos);
                            }
                        }
                    }
                }
            }
            public void drawNestDebugOnScreen(Bitmap gameBitmap, (int x, int y) camPos)
            {
                foreach (Structure structure in activeStructures.Values)
                {
                    Nest nest = structure.getItselfAsNest();
                    if (nest == null) { continue; }
                    foreach ((int x, int y) posToDrawAt in nest.digErrands)
                    {
                        drawPixel(gameBitmap, Color.FromArgb(100, 255, 0, 0), posToDrawAt, camPos);
                    }
                    if (nest.rooms.ContainsKey(1))
                    {
                        foreach ((int x, int y) posToDrawAt in nest.rooms[1].tiles) { drawPixel(gameBitmap, Color.FromArgb(100, 120, 0, 100), posToDrawAt, camPos); }
                    }
                    foreach (Room room in nest.rooms.Values)
                    {
                        if (room.type == 2)
                        {
                            foreach ((int x, int y) posToDrawAt in room.dropPositions) { drawPixel(gameBitmap, Color.FromArgb(100, 0, 0, 255), posToDrawAt, camPos); }
                        }
                        else if (room.type == 3)
                        {
                            foreach ((int x, int y) posToDrawAt in room.dropPositions) { drawPixel(gameBitmap, Color.FromArgb(100, 220, 255, 150), posToDrawAt, camPos); }
                        }
                    }
                }
            }
            public void drawEntityPathDebugOnScreen(Bitmap gameBitmap, (int x, int y) camPos, Player player)
            {
                Color simplifiedPathColor = Color.FromArgb(100, 200, 0, 100);
                Color collisionColor = Color.FromArgb(255 - (int)(510 * Seesaw(timeElapsed * 2.724f, 1)), 200, 100 + (int)(200 * Seesaw(timeElapsed * 2.724f, 1)), 100 + (int)(200 * Seesaw(timeElapsed * 2.724f, 1)));
                foreach (Entity entity in activeEntities.Values)
                {
                    foreach ((int x, int y) posToDrawAt in entity.pathToTarget) { drawPixel(gameBitmap, Color.FromArgb(100, entity.color.R, entity.color.G, entity.color.B), posToDrawAt, camPos); }
                    foreach ((int x, int y) posToDrawAt in entity.simplifiedPathToTarget) { drawPixel(gameBitmap, simplifiedPathColor, posToDrawAt, camPos); }
                    //foreach ((int x, int y) posToDrawAt in entity.traits.collisionPoints) { drawPixel(gameBitmap, collisionColor, (entity.posX + posToDrawAt.x, entity.posY + posToDrawAt.y), camPos); }
                    drawPixel(gameBitmap, Color.DarkBlue, entity.targetPos, camPos);
                }

                foreach ((int x, int y) posToDrawAt in player.pathToTarget) { drawPixel(gameBitmap, Color.FromArgb(100, player.color.R, player.color.G, player.color.B), posToDrawAt, camPos); }
                foreach ((int x, int y) posToDrawAt in player.simplifiedPathToTarget) { drawPixel(gameBitmap, simplifiedPathColor, posToDrawAt, camPos); }

                int signX = Sign(player.speedX);
                if (timeElapsed % 0.5f < 0.333f)
                {
                    foreach ((int x, int y) posToDrawAt in player.traits.collisionPoints.down) { drawPixel(gameBitmap, Color.Red, (player.posX + posToDrawAt.x * signX, player.posY + posToDrawAt.y), camPos); }
                }
                if (timeElapsed % 0.5f > 0.1666f)
                {
                    foreach ((int x, int y) posToDrawAt in player.traits.collisionPoints.side) { drawPixel(gameBitmap, Color.Blue, (player.posX + posToDrawAt.x * signX, player.posY + posToDrawAt.y), camPos); }
                }
                if (timeElapsed % 0.5f < 0.1666f || timeElapsed % 0.5f > 0.333)
                {
                    foreach ((int x, int y) posToDrawAt in player.traits.collisionPoints.up) { drawPixel(gameBitmap, Color.Green, (player.posX + posToDrawAt.x * signX, player.posY + posToDrawAt.y), camPos); }
                }
            }
            public void drawMiscDebugOnScreen(Bitmap gameBitmap, (int x, int y) camPos)
            {
                if (false)  // Debug for plants exact Plant and PlantElement's positions
                {
                    List<PlantElement> listo = new List<PlantElement>();
                    foreach (Plant plant in activePlants.Values)
                    {
                        listo.Add(plant.plantElement);
                        if (rand.Next(5) == 0) { game.miscDebugList.Add(((plant.posX, plant.posY), Color.Crimson)); }
                    }
                    while (listo.Count > 0)
                    {
                        PlantElement current = listo[0];
                        listo.RemoveAt(0);
                        foreach (PlantElement baby in current.childPlantElements) { listo.Add(baby); }
                        if (rand.Next(5) != 0) { game.miscDebugList.Insert(0, (current.motherPlant.getRealPos(current.pos), Color.MediumPurple)); }
                    }
                }
                foreach (Chunk chunkoko in loadedChunks.Values) { if (chunkoko.unstableLiquidCount > 0) { pasteImage(gameBitmap, transBlue32Bitmap, (chunkoko.pos.x * 32, chunkoko.pos.y * 32), camPos); } }
                foreach (((int x, int y) pos, Color col) item in game.miscDebugList) { drawPixel(gameBitmap, item.col, item.pos, camPos); }
                game.miscDebugList = new List<((int x, int y) pos, Color col)>();
            }
            public void drawChunksAndMegachunksDebugOnScreen(Bitmap finalBitmap, Player player)
            {
                int xOffset = (game.loadedScreens.Count - 1) * 100;
                int scaleFactor = Max(1, 30 / (game.zoomLevel + 10));
                Color colorToDraw;
                (int x, int y) playerChunkPos = (ChunkIdx(player.posX), ChunkIdx(player.posY));
                foreach (Screen screenToDebug in game.loadedScreens.Values)
                {
                    foreach ((int x, int y) poso in screenToDebug.megaChunks.Keys)
                    {
                        if (player.screen == screenToDebug) { colorToDraw = Color.IndianRed; }
                        else { colorToDraw = Color.Crimson; }
                        drawPixelFixed(finalBitmap, colorToDraw, (100 + xOffset + playerChunkPos.x * 2, 100 + playerChunkPos.y * 2), (-poso.x, -poso.y), 32 * scaleFactor, true);
                    }
                    foreach ((int x, int y) poso in screenToDebug.activeStructureLoadedChunkIndexes.Keys)
                    {
                        colorToDraw = Color.FromArgb(100, 255, 255, 100);
                        drawPixelFixed(finalBitmap, colorToDraw, (100 + xOffset, 100), (-poso.x + playerChunkPos.x, -poso.y + playerChunkPos.y), 2 * scaleFactor, true);
                    }
                    foreach ((int x, int y) poso in screenToDebug.extraLoadedChunks.Keys)
                    {
                        colorToDraw = Color.Purple;
                        drawPixelFixed(finalBitmap, colorToDraw, (100 + xOffset, 100), (-poso.x + playerChunkPos.x, -poso.y + playerChunkPos.y), 2 * scaleFactor, true);
                    }
                    foreach ((int x, int y) poso in screenToDebug.loadedChunks.Keys)
                    {
                        colorToDraw = Color.FromArgb(150, 0, 128, 0);
                        if (screenToDebug.loadedChunks[poso].unstableLiquidCount > 0) { colorToDraw = Color.DarkBlue; }
                        else if (screenToDebug.activeStructureLoadedChunkIndexes.ContainsKey(poso)) { colorToDraw = Color.Cyan; }
                        else if (screenToDebug.inertStructureLoadedChunkIndexes.ContainsKey(poso)) { colorToDraw = Color.FromArgb(130, 50, 130); }
                        drawPixelFixed(finalBitmap, colorToDraw, (100 + xOffset, 100), (-poso.x + playerChunkPos.x, -poso.y + playerChunkPos.y), 2 * scaleFactor, true);
                    }
                    drawPixelFixed(finalBitmap, Color.Red, (100 + xOffset, 100), (0, 0), 2 * scaleFactor, true);
                    xOffset -= 120;
                }
            }
            public TileTraits getTileContent((int x, int y) posToTest)
            {
                return getChunkFromPixelPos(posToTest).fillStates[PosMod(posToTest.x), PosMod(posToTest.y)];
            }
            public TileTraits setTileContent((int x, int y) posToTest, (int type, int subType) typeToSet)
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
