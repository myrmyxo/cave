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

namespace Cave
{
    public class Structures
    {
        public class Structure
        {
            public int c;   // The "mega" type. If a normal structure, a nest...
            public int id;
            public (int type, int subType, int subSubType) type;
            public (long x, long y) seed;
            public (int x, int y) pos;
            public (int x, int y) size = (0, 0);
            public string name = "";
            public bool isDynamic = false;
            public int state = 0;
            public Structure sisterStructure = null;
            public Dictionary<(int x, int y), (int type, int subType)> structureDict = new Dictionary<(int x, int y), (int type, int subType)>();
            public Dictionary<(int x, int y), bool> chunkPresence = new Dictionary<(int x, int y), bool>();
            public Dictionary<(int x, int y), bool> megaChunkPresence = new Dictionary<(int x, int y), bool>();

            public Bitmap bitmap = null;
            public int[] posOffset = null;

            public float timeAtBirth = -999;

            // loading and unloading management
            public bool isImmuneToUnloading = false;
            public bool isErasedFromTheWorld = false;   // serves both for deletion and when creating the structure if it's not valid and doesn't get added to the world

            public Screens.Screen screen;
            protected Structure() { }   // for inheritance (for Nests)
            public Structure(Game game, StructureJson structureJson)
            {
                setAllStructureJsonVariables(game, structureJson);
                if (isErasedFromTheWorld) { return; }
                findChunkPresence();
                addStructureToTheRightDictInTheScreen();

                if (structureJson.sis != -1)  // important to load here else infinite loop :(
                {
                    sisterStructure = game.getStructure(structureJson.sis);
                }
            }
            public void setAllStructureJsonVariables(Game game, StructureJson structureJson)
            {
                screen = game.getScreen(structureJson.dim);
                id = structureJson.id;
                if (screen.activeStructures.ContainsKey(id) || screen.inertStructures.ContainsKey(id)) { return; }

                type = structureJson.type;
                isDynamic = structureJson.isD;
                isErasedFromTheWorld = structureJson.isE;
                seed = structureJson.seed;
                pos = structureJson.pos;
                size = structureJson.size;
                name = structureJson.name;
                timeAtBirth = structureJson.brth;
                state = structureJson.state;
                structureDict = arrayToFillstates(structureJson.fS);

                makeBitmap();
            }
            public Structure(Screens.Screen screenToPut, (int x, int y) posToPut, (long x, long y) seedToPut, (bool forceType, bool isPlayerGenerated) bools, (int type, int subType, int subSubType) forceType, Dictionary<(int x, int y), (int type, int subType)> forceStructure = null)
            {
                seed = seedToPut;
                screen = screenToPut;
                pos = posToPut;
                id = currentStructureId;

                if (bools.forceType) { type = forceType; }
                else
                {
                    long seedo = (seed.x / 2 + seed.y / 2) % 79461537;
                    if (Abs(seedo) % 200 < 50) // cubeAmalgam
                    {
                        type = (1, 0, 0);
                    }
                    else if (Abs(seedo) % 200 < 150) // circularBlade
                    {
                        type = (2, 0, 0);
                    }
                    else // star 
                    {
                        type = (2, 1, 0);
                    }
                }

                if (bools.isPlayerGenerated && forceStructure != null) { structureDict = forceStructure; }
                if (!bools.isPlayerGenerated) { drawStructure(); } // contains imprintChunks()

                if (isErasedFromTheWorld) { return; }   // if the structure FAILED, don't do anything. Don't add it or anything else
                currentStructureId++;

                findChunkPresence();
                if (bools.isPlayerGenerated) { return; } // if structure is player generated, don't save it and add to dicts YET
                saveStructure();
                addToMegaChunks();
                addStructureToTheRightDictInTheScreen();
            }
            public virtual void saveStructure()
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new JavaScriptDateTimeConverter());
                serializer.NullValueHandling = NullValueHandling.Ignore;

                StructureJson structureJson = new StructureJson(this);

                using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\CaveData\\{screen.game.seed}\\StructureData\\{id}.json"))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, structureJson);
                }
            }
            public virtual int setClassTypeInJson() { return 0; }
            public void addStructureToTheRightDictInTheScreen()
            {
                Chunk newChunk;
                if (isDynamic)
                {
                    foreach ((int, int) chunkPos in chunkPresence.Keys)
                    {
                        if (!screen.loadedChunks.ContainsKey(chunkPos))
                        {
                            newChunk = new Chunk(chunkPos, false, screen); // this is needed cause uhh yeah idk sometimes loadedChunks is FUCKING ADDED IN AGAIN ???
                            if (!screen.loadedChunks.ContainsKey(chunkPos)) { screen.loadedChunks[chunkPos] = newChunk; }
                        }
                    }
                    screen.activeStructures[id] = this;
                    isImmuneToUnloading = false;
                }
                else { screen.inertStructures[id] = this; }
            }
            public void findChunkPresence()
            {
                foreach ((int x, int y) posToTest in structureDict.Keys)
                {
                    chunkPresence[ChunkIdx(posToTest)] = true;
                }
                foreach ((int x, int y) poso in chunkPresence.Keys)
                {
                    megaChunkPresence[MegaChunkIdxFromChunkPos(poso)] = true;
                }
            }
            public bool drawLakeNew() // thank you papa still for base code <3
            {
                (int x, int y) posToTest;
                (int type, int subType) material = screen.getTileContent(pos);
                if (material.type != 0) { return false; } // if start tile isn't empty, fail

                (int type, int subType) forceType = (0, 0);

                int modY = 1;
                int modX = 0;
                int count = 0;
                while (true) // go down (can flow left/right) until finding a solid tile.
                {
                    if (count > 96) { return false; } // If moved more than 96 tiles, fail
                    material = screen.getTileContent((pos.x + modX, pos.y - modY));
                    if (material.type < 0) { forceType = material; return false; } // for now if it bumps into already present liquid, do not try to extend the lake... might change in ze futur
                    if (material.type == 0) { modY++; count++; }
                    else if (screen.getTileContent((pos.x + modX - 1, pos.y - modY)).type <= 0 && screen.getTileContent((pos.x + modX - 1, pos.y - modY + 1)).type == 0) { modX--; count++; }
                    else if (screen.getTileContent((pos.x + modX + 1, pos.y - modY)).type <= 0 && screen.getTileContent((pos.x + modX + 1, pos.y - modY + 1)).type == 0) { modX++; count++; }
                    else { break; }
                }
                posToTest = (pos.x + modX, pos.y - modY + 1); // because uh this one was solid lol so need to fill the one ABOVE it
                int currentY = posToTest.y;

                long seedo = (seed.x / 2 + seed.y / 2) % 79461537;
                int megaLake = 0;
                if (seedo % 100 == 0) { megaLake = 10000; }
                else if (seedo % 10 == 0) { megaLake = 2500; }

                int[] tilesFilled = new int[] { 0, 1 + Min((int)(seedo % 1009), (int)(seedo % 1277)) + megaLake}; // just a way to update the amount of tiles filled recursively not to go too high lolol. 2nd is maximum not to go over.
                Dictionary<(int x, int y), Chunk> chunkDict = new Dictionary<(int x, int y), Chunk>();
                Dictionary<(int x, int y), bool> tilesToFill = new Dictionary<(int x, int y), bool>(); // All tiles added there WILL be filled after no matter what
                Dictionary<(int x, int y), bool> newTilesToFill = new Dictionary<(int x, int y), bool> { { posToTest, false } }; // New layer of tiles to fill tested this iteration. True means will be added to tilesToFill if the layer is valid, False means it will be tested next row (y was too high)
                Dictionary<(int x, int y), bool> babyNewTilesToFill; // baby dict. Will become the new newTilesToFill after. Goo goo ga ga
                bool proceed = true;
                while (proceed && newTilesToFill.Count > 0) // until he gets a STOP by a floodPixel that bumped into a liquid tile or he filled too much, he continues filling new rows
                {
                    babyNewTilesToFill = new Dictionary<(int x, int y), bool>();
                    foreach ((int x, int y) key in newTilesToFill.Keys)
                    {
                        if (newTilesToFill[key]) { tilesToFill[key] = true; }
                        else { proceed = proceed && floodPixel(key, currentY, tilesToFill, babyNewTilesToFill, chunkDict, tilesFilled); }
                    }
                    newTilesToFill = babyNewTilesToFill;
                    currentY++;
                }
                if (tilesToFill.Count == 0) { return false; } // No laketches ?


                material = (-2, 0); // material is now type to fill with
                Chunk chunkToTest = chunkDict[ChunkIdx(posToTest)];
                (int type, int subType) biome = chunkToTest.biomeIndex[PosMod(posToTest.x), PosMod(posToTest.y)][0].Item1;

                if (biome == (5, 0)) { material = (-3, 0); } // if fairy biome : put fairy liquid
                else if (biome == (2, 0)) // if hot biome : put lava
                {
                    material = (-4, 0);
                    /*if (THIS WAS PUT THERE TO ADD MORE LAVA LAKES THE HIGHER THE TEMPERATURE !!!But fuck it myb i'll use the mean or center tile saved this costs loads of memory    chunkToTest.secondaryBiomeValues[testPos32.Item1, testPos32.Item2, 0] + chunkToTest.secondaryBigBiomeValues[testPos32.Item1, testPos32.Item2, 0] - 128 + rand.Next(200) - 200 > 100)
                    {
                        liquidTypeToFill = -4;
                    }*/
                }
                else if (biome == (10, 1) || biome == (10, 2) || biome == (10, 3)) { material = (-6, 0); }// if bone or flesh and bone or blood ocean : put blood
                else if (biome == (10, 0) || biome == (10, 4)) { material = (-7, 0); } // if flesh or acid ocean : put acid
                
                seedo = LCGyNeg(LCGxNeg(seedo));

                if (seedo % 1000 == 0) { material = (-1, 0); }
                else if (seedo % 1000 < 5) { material = (-3, 0); }

                foreach ((int x, int y) poso in tilesToFill.Keys)
                {
                    structureDict[poso] = material;
                }

                name = "";
                int syllables = 2 + Min((int)(seedo % 13), (int)(seedo % 3));
                for (int i = 0; i < syllables; i++)
                {
                    name += nameArray[seedo % nameArray.Length];
                    seedo = LCGz(seedo);
                }

                return true;
            }
            bool floodPixel((int x, int y) pos, int maxY, Dictionary<(int x, int y), bool> tilesToFill, Dictionary<(int x, int y), bool> newTilesToFill, Dictionary<(int x, int y), Chunk> chunkDict, int[] tilesFilled)
            {
                if (tilesToFill.ContainsKey(pos) || newTilesToFill.ContainsKey(pos)) { return true; } // already tried to filled this one, don't try to fill it but continue the fill
                if (tilesFilled[0] > 2000) { return false; } // lake tooo biiig, ABORT ABORT

                (int x, int y) chunkPos = ChunkIdx(pos);
                Chunk chunkToTest = screen.getChunkEvenIfNotLoaded(chunkPos, chunkDict);
                chunkDict[chunkPos] = chunkToTest;
                (int type, int subType) material = chunkToTest.fillStates[PosMod(pos.x), PosMod(pos.y)];

                if (material.type < 0) { return false; } // bumped on a liquid tile, ABORT ABORT
                if (material.type == 0)
                {
                    if (pos.y <= maxY) { newTilesToFill[pos] = true; }
                    else { newTilesToFill[pos] = false; return true; } // if too high, keep it as a test for later but don't fill it and try neighbours YET
                    tilesFilled[0]++;
                    return 
                    floodPixel((pos.x - 1, pos.y), maxY, tilesToFill, newTilesToFill, chunkDict, tilesFilled) &&
                    floodPixel((pos.x + 1, pos.y), maxY, tilesToFill, newTilesToFill, chunkDict, tilesFilled) &&
                    floodPixel((pos.x, pos.y - 1), maxY, tilesToFill, newTilesToFill, chunkDict, tilesFilled) &&
                    floodPixel((pos.x, pos.y + 1), maxY, tilesToFill, newTilesToFill, chunkDict, tilesFilled);
                }
                return true; // return true even if fill not worked, just stop if tile is liquid or if filled too much
            }
            public void drawStructure()
            {
                long seedo = (seed.x / 2 + seed.y / 2) % 79461537;
                name = "";
                int syllables = 2 + Min((int)(seedo % 13), (int)(seedo % 3));
                for (int i = 0; i < syllables; i++)
                {
                    name += nameArray[seedo % nameArray.Length];
                    seedo = LCGz(seedo);
                }

                bool success;
                if (type.type == 0) { success = drawLakeNew(); }
                else if (type == (1, 0, 0)) { success = cubeAmalgam(); }
                else if (type == (2, 0, 0)) { success = sawBlade(); }
                else if (type == (2, 1, 0)) { success = star(); }
                else { success = false; }   // if not in these structure is not validated

                if (success) { imprintChunks(); }
                else { isErasedFromTheWorld = true; }
            }
            public void initAfterStructureValidated()
            {
                if (type.type == 3) { portal(); }
                timeAtBirth = timeElapsed;

                imprintChunks();

                findChunkPresence();
                saveStructure();
                addToMegaChunks();
                addStructureToTheRightDictInTheScreen();
            }
            public void addToMegaChunks()
            {
                MegaChunk megaChunk;
                foreach ((int x, int y) pos in megaChunkPresence.Keys)
                {
                    megaChunk = screen.getMegaChunkFromMegaPos(pos, true);
                    if (!megaChunk.structures.Contains(id)) // should always be the case but whatever
                    {
                        megaChunk.structures.Add(id);
                        screen.megaChunksToSave[pos] = true;
                        (int dim, int x, int y) location = (megaChunk.screen.id, pos.x, pos.y);
                        if (!screen.game.structureGenerationLogsStructureUpdateCount.ContainsKey(location)) { screen.game.structureGenerationLogsStructureUpdateCount[location] = 0; }
                        screen.game.structureGenerationLogsStructureUpdateCount[location] += 1;
                    }
                }
            }
            public bool cubeAmalgam()
            {
                size = ((int)(seed.x % 5) + 1, (int)(seed.y % 5) + 1);
                int squaresToDig = (int)(seed.x % (10 + (size.Item1 * size.Item2))) + (int)(size.Item1 * size.Item2 * 0.2f) + 1;
                long seedoX = seed.x;
                long seedoY = seed.y;

                (int x, int y) posToTest;
                for (int gu = 0; gu < squaresToDig; gu++)
                {
                    seedoX = LCGxNeg(seedoX);
                    seedoY = LCGyNeg(seedoY);
                    int sizo = (int)((LCGxNeg(seedoY)) % 7 + 7) % 7 + 1;
                    int centerX = (int)(pos.x + sizo + seedoX % (size.Item1 * 32 - 2 * sizo));
                    int centerY = (int)(pos.x + sizo + seedoY % (size.Item2 * 32 - 2 * sizo));
                    for (int i = -sizo; i <= sizo; i++)
                    {
                        for (int j = -sizo; j <= sizo; j++)
                        {
                            posToTest = (centerX + i, centerY + j);
                            if (Abs(i) == sizo || Abs(j) == sizo) { structureDict[posToTest] = (1, 0); }
                            else { structureDict[posToTest] = (0, 0); }
                        }
                    }
                }
                return true;
            }
            public bool sawBlade()
            {
                int sizeX = (int)(seed.x % 5) + 1;
                size = (sizeX, sizeX);
                long seedoX = seed.x;
                long seedoY = seed.y;

                int angleOfShape = (int)LCGz(seedoX + seedoY) % 360;
                (int x, int y) posToTest;

                for (int i = -size.Item1 * 16; i < size.Item1 * 16; i++)
                {
                    for (int j = -size.Item2 * 16; j < size.Item2 * 16; j++)
                    {
                        int angleMod = (int)(Math.Atan2(i, j) * 180 / Math.PI);
                        int angle = (3600 + angleOfShape - angleMod) % 360;
                        float distance = (float)Math.Sqrt(i * i + j * j);

                        float sizo = (size.Item1 * (8 - sawBladeSeesaw(angle, 72) * 0.1f));

                        if (distance < sizo)
                        {
                            structureDict[(pos.x + i, pos.y + j)] = (0, 0);
                            //outline
                            foreach ((int x, int y) mod in neighbourArray)
                            {
                                posToTest = (pos.x + i + mod.x, pos.y + j + mod.y);
                                if (!structureDict.ContainsKey(posToTest))
                                {
                                    structureDict[posToTest] = (1, 0);
                                }
                            }
                        }
                    }
                }
                // lil X thingy in the middle
                structureDict[(pos.x, pos.y)] = (1, 0);
                foreach ((int x, int y) mod in diagArray)
                {
                    structureDict[(pos.x + mod.x, pos.y + mod.y)] = (2, 0);
                }
                return true;
            }
            public bool star()
            {
                int sizeX = (int)(seed.x % 5) + 1;
                size = (sizeX, sizeX);
                long seedoX = seed.x;
                long seedoY = seed.y;

                int angleOfShape = (int)LCGz(seedoX + seedoY) % 360;
                (int x, int y) posToTest;

                for (int i = -size.Item1 * 16; i < size.Item1 * 16; i++)
                {
                    for (int j = -size.Item2 * 16; j < size.Item2 * 16; j++)
                    {
                        int angleMod = (int)(Math.Atan2(i, j) * 180 / Math.PI);
                        int angle = (3600 + angleOfShape - angleMod) % 360;
                        float distance = (float)Math.Sqrt(i * i + j * j);

                        float sizo = (size.Item1 * (8 - Seesaw(angle, 72) * 0.1f));

                        if (distance < sizo)
                        {
                            structureDict[(pos.x + i, pos.y + j)] = (0, 0);
                            //outline
                            foreach ((int x, int y) mod in neighbourArray)
                            {
                                posToTest = (pos.x + i + mod.x, pos.y + j + mod.y);
                                if (!structureDict.ContainsKey(posToTest))
                                {
                                    structureDict[posToTest] = (1, 0);
                                }
                            }
                        }
                    }
                }
                return true;
            }
            public void portal()
            {
                isDynamic = true;
                if (type.subSubType == 0)
                {
                    for (int i = -2; i <= 2; i++)
                    {
                        for (int j = -2; j <= 2; j++)
                        {
                            if (Abs(i) == Abs(j) && Abs(i) == 2) { continue; }
                            structureDict[(pos.x + i, pos.y + j)] = (0, 0);
                        }
                    }
                    new Particle(screen, pos, pos, (1, 4, 0));
                    Screens.Screen livingDimensionScreen;
                    if (screen.game.livingDimensionId == -1) { livingDimensionScreen = screen.game.loadDimension(currentDimensionId, false, false, 2, 0); }
                    else { livingDimensionScreen = screen.game.loadDimension(screen.game.livingDimensionId); }
                    Structure sister = new Structure(livingDimensionScreen, pos, seed, (true, true), (3, 0, 1));
                    screen.game.structuresToAdd[sister.id] = sister;
                    sisterStructure = sister;
                    sister.sisterStructure = this;
                }
                else
                {
                    for (int i = -4; i <= 4; i++)
                    {
                        for (int j = -5; j <= 3; j++)
                        {
                            float dist = Distance((i, j), (0, -1));
                            if (dist > 4.5f) { continue; }
                            if (dist > 3.5f) { structureDict[(pos.x + i, pos.y + j)] = (4, 0); }
                            else { structureDict[(pos.x + i, pos.y + j)] = (0, 0); }
                        }
                    }
                    imprintChunks();
                    structureDict = new Dictionary<(int x, int y), (int type, int subType)>();
                    for (int i = -2; i <= 2; i++)
                    {
                        for (int j = -2; j <= 2; j++)
                        {
                            if (Abs(i) == Abs(j) && Abs(i) == 2) { continue; }
                            structureDict[(pos.x + i, pos.y + j)] = (0, 0);
                        }
                    }
                }
                makeBitmap();
            }
            public virtual void moveStructure() // it's not actually moving but whatever lmfao
            {
                if (type.type == 3)
                {
                    if (state == 0)
                    {
                        if (rand.Next(3) == 0 || timeElapsed > 3 + timeAtBirth)
                        {
                            ((int x, int y) pos, bool success) result = tryRandomConversion((-6, 0), (-6, 1));
                            if (result.success) { new Particle(screen, result.pos, this.pos, (2, -6, 0)); }
                        }
                        if (!structureDict.Values.Contains((-6, 0))) { state = 1; }
                    }
                    else
                    {
                        (int x, int y) pos = structureDict.Keys.ToArray()[rand.Next(structureDict.Count)];
                        (int type, int subType) material = structureDict[pos];
                        if (material.type == -6)
                        {
                            new Particle(screen, pos, this.pos, (2, material.type, material.subType));
                        }
                    }
                    tryTeleportation();
                }
                // add time at birth for structs, for portal, make it so when it's created tiles around the portal disappear in like 2-3 seconds,
                // then blood particles start moving from the blood, going to the center position, agregate into a portal. The blood particles come continiously from the portal
                // even after it's been created, but there's a lot more at the start. After the portal is done it can be used.
            }
            public virtual void addEntityToStructure(Entity entity)
            {
                return; // lol
            }
            public virtual Nest getItselfAsNest() { return null; } // not perfect but ehhhhh... for debug
            public void tryTeleportation()
            {
                int idToTeleportTo = sisterStructure.screen.id;
                foreach (Entity entity in screen.activeEntities.Values)
                {
                    int diffX = Abs(entity.posX - pos.x);
                    int diffY = Abs(entity.posY - pos.y);
                    if (diffX <= 2 && diffY <= 2 && structureDict.ContainsKey((entity.posX, entity.posY))) // if inside the portal.
                    {
                        if (entity.timeAtLastTeleportation + 1 > timeElapsed) // do not TP if last TP was less than 1 second ago
                        {
                            if (entity.timeAtLastTeleportation + 0.5f > timeElapsed) { entity.timeAtLastTeleportation = timeElapsed; } // if last TP was less than 1/2 second ago, reinitialize timer (so entity needs to stay out of the portal for 0.5s to be able to teleport again, to not get stuck in a loop)
                            continue;
                        }
                        entity.teleport((entity.posX, entity.posY), idToTeleportTo);
                    }
                }
                foreach (Player player in screen.game.playerList)
                {
                    if (player.screen != screen) { continue; }
                    int diffX = Abs(player.posX - pos.x);
                    int diffY = Abs(player.posY - pos.y);
                    if (diffX <= 2 && diffY <= 2 && structureDict.ContainsKey((player.posX, player.posY))) // if inside the portal.
                    {
                        if (player.timeAtLastTeleportation + 1 > timeElapsed) // do not TP if last TP was less than 1 second ago
                        {
                            if (player.timeAtLastTeleportation + 0.5f > timeElapsed) { player.timeAtLastTeleportation = timeElapsed; } // if last TP was less than 1/2 second ago, reinitialize timer (so entity needs to stay out of the portal for 0.5s to be able to teleport again, to not get stuck in a loop)
                            continue;
                        }
                        player.teleport((player.posX, player.posY), idToTeleportTo);
                    }
                }

                screen.addRemoveEntities();
            }
            public ((int x, int y) pos, bool success) tryRandomConversion((int type, int subType) typeToConvert, (int type, int subType) newType)
            {
                (int x, int y) pos = structureDict.Keys.ToArray()[rand.Next(structureDict.Count)];
                (int type, int subType) material = structureDict[pos];
                if (material == typeToConvert)
                {
                    structureDict[pos] = newType;
                    screen.setTileContent(pos, newType);
                    return ((pos.x, pos.y), true);
                }
                return ((0, 0), false);
            }
            public void imprintChunks()
            {
                Dictionary<(int x, int y), Chunk> chunkDict = new Dictionary<(int x, int y), Chunk>();
                (int x, int y) chunkPos;
                Chunk chunkToTest;

                foreach ((int x, int y) posToTest in structureDict.Keys)
                {
                    chunkPos = ChunkIdx(posToTest);
                    chunkToTest = screen.getChunkEvenIfNotLoaded(chunkPos, chunkDict);
                    chunkToTest.fillStates[PosMod(posToTest.x), PosMod(posToTest.y)] = structureDict[posToTest];
                    chunkToTest.modificationCount = 1;
                    chunkToTest.findTileColor(PosMod(posToTest.x), PosMod(posToTest.y));
                }

                foreach (Chunk chunk in chunkDict.Values)
                {
                    saveChunk(chunk);
                }
            }
            public void EraseFromTheWorld()
            {
                if (isErasedFromTheWorld) { return; }
                foreach ((int x, int y) pos in megaChunkPresence.Keys)
                {
                    MegaChunk megaChunk = screen.getMegaChunkFromMegaPos(pos, true);
                    megaChunk.structures.Remove(id);
                    saveMegaChunk(megaChunk);
                }
                if (screen.activeStructures.ContainsKey(id)) { screen.activeStructures.Remove(id); }
                isErasedFromTheWorld = true;
                saveStructure();
                if (sisterStructure != null) { sisterStructure.EraseFromTheWorld(); }
            }
            public void makeBitmap()
            {
                if (type.type == 3)
                {
                    posOffset = new int[] { -2, -2 };
                }
                else { bitmap = null; posOffset = null; }
            }
        }
        public static bool testForBloodAltar(Screens.Screen screen, (int x, int y) startPos)
        {
            Dictionary<(int x, int y), (int type, int subType)> dicto = new Dictionary<(int x, int y), (int type, int subType)>();

            (int x, int y) posToTest;
            (int type, int subType) material = screen.getTileContent(startPos);
            if (material != (4, 0)) { return false; } // if start tile isn't fleshTile, fail
            dicto[startPos] = (0, 0);

            (int x, int y) chunkPos;
            Chunk chunkToTest;
            foreach ((int x, int y) mod in directionPositionArray)
            {
                chunkPos = ChunkIdx(startPos.x + mod.x, startPos.y + mod.y);
                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    chunkToTest = screen.loadedChunks[chunkPos];
                    if (chunkToTest.fillStates[PosMod(startPos.x + mod.x), PosMod(startPos.y + mod.y)].type != 0) { return false; }
                }
                else { return false; } // if chunks loaded DO NOT make altar lololol
            }

            int count = 1;
            while (true) // go down until finding a blood tile.
            {
                if (count > 12) { return false; } // If went down more than 5 tiles, fail
                posToTest = (startPos.x, startPos.y - count);
                material = screen.getTileContent(posToTest);
                if (material != (0, 0))
                {
                    if (material == (-6, 0)) // if bumps on blood tile, proceed
                    {
                        dicto[posToTest] = material;
                        break;
                    }
                    else { return false; } // if bumps on a tile other than air or blood, fail
                }
                count++;
            }
            if (count < 5) { return false; }

            (bool left, bool right) validity = (false, false);
            count = 1; // Length of the blood pool. 1 at first because blood tile it bumped on needs to be counted
            int currentX = 1;

            while (validity != (true, true) && count <= 15)
            {
                if (!validity.left)
                {
                    material = screen.getTileContent((posToTest.x - currentX, posToTest.y));
                    if (material == (-6, 0)) // if blood :
                    {
                        if (screen.getTileContent((posToTest.x - currentX, posToTest.y - 1)) != (1, 1)) { return false; } // test if tile under it is denseRock (if not fail)
                        if (screen.getTileContent((posToTest.x - currentX, posToTest.y + 1)).type != 0) { return false; } // test if tile over it is air (if not fail)
                        dicto.Add((posToTest.x - currentX, posToTest.y - 1), (1, 1));
                        dicto.Add((posToTest.x - currentX, posToTest.y + 1), (0, 0));
                        count++;
                    }
                    else if (material == (1, 1)) { validity = (true, validity.right); } // if dense rock, continue and stop testing on the left (no blood so don't count it)
                    else { return false; } // if other than dense rock or blood, fail
                    dicto.Add((posToTest.x - currentX, posToTest.y), material);
                }
                if (!validity.right)
                {
                    material = screen.getTileContent((posToTest.x + currentX, posToTest.y));
                    if (material == (-6, 0)) // if blood :
                    {
                        if (screen.getTileContent((posToTest.x + currentX, posToTest.y - 1)) != (1, 1)) { return false; } // test if tile under it is denseRock (if not fail)
                        if (screen.getTileContent((posToTest.x + currentX, posToTest.y + 1)).type != 0) { return false; } // test if tile over it is air (if not fail)
                        dicto.Add((posToTest.x + currentX, posToTest.y - 1), (1, 1));
                        dicto.Add((posToTest.x + currentX, posToTest.y + 1), (0, 0));
                        count++;
                    }
                    else if (material == (1, 1)) { validity = (validity.left, true); } // if dense rock, continue and stop testing on the right (no blood so don't count it)
                    else { return false; } // if other than dense rock or blood, fail
                    dicto.Add((posToTest.x + currentX, posToTest.y), material);
                }
                currentX++;
            }
            if (count < 3 || count > 15) { return false; } // if blood pool is longer than 15 tiles, fail... bro it's too long lol stop trolling

            // AFTER THIS, THE BLOOD ALTAR IS CONSIDERED VALID AND THE STRUCTURE WILL BE GENERATED

            Structure altar = new Structure(screen, startPos, (0, 0), (true, true), (3, 0, 0), dicto);
            screen.game.structuresToAdd[altar.id] = altar;

            return true; // blood altar is valid ! yay ! that was suprisingly easy to do. now test if bugs (i hope not i hate bunny (it's a joke i love bunnies yay !))
        }
    }
}
