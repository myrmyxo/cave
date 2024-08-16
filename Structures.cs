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
    public class Structures
    {
        public class Structure
        {
            public string name = "";
            public (int, int) structChunkPosition;
            public int type;
            public int id;
            public Screens.Screen screen;
            public long seedX;
            public long seedY;
            public int posX;
            public int posY;
            public (int x, int y) size;
            public Dictionary<(int x, int y), (int type, int subType)> structureDict = new Dictionary<(int x, int y), (int type, int subType)>();

            public Dictionary<(int x, int y), bool> chunkPresence = new Dictionary<(int x, int y), bool>();
            public Structure(int posXToPut, int posYToPut, long seedXToPut, long seedYToPut, bool isLake, (int, int) structChunkPositionToPut, Screens.Screen screenToPut)
            {
                seedX = seedXToPut;
                seedY = seedYToPut;
                screen = screenToPut;
                posX = posXToPut;
                posY = posYToPut;
                structChunkPosition = structChunkPositionToPut;

                id = currentStructureId;
                currentStructureId++;

                if (isLake)
                {
                    // waterLake
                    {
                        type = 3;
                        int sizeX = 0;
                        int sizeY = 0;
                        size = (sizeX, sizeY);
                    }
                }
                else
                {
                    long seedo = (seedX / 2 + seedY / 2) % 79461537;
                    if (Abs(seedo) % 200 < 50) // cubeAmalgam
                    {
                        type = 0;
                        int sizeX = (int)(seedX % 5) + 1;
                        int sizeY = (int)(seedY % 5) + 1;
                        size = (sizeX, sizeY);
                    }
                    else if (Abs(seedo) % 200 < 150)// circularBlade
                    {
                        type = 1;
                        int sizeX = (int)(seedX % 5) + 1;
                        size = (sizeX, sizeX);
                    }
                    else // star 
                    {
                        type = 2;
                        int sizeX = (int)(seedX % 5) + 1;
                        size = (sizeX, sizeX);
                    }
                }
            }
            public bool drawLakeNew() // thank you papa still for base code <3
            {
                (int x, int y) posToTest;
                (int type, int subType) material = screen.getTileContent((posX, posY));
                if (material.type != 0) { return false; } // if start tile isn't empty, fail

                (int type, int subType) forceType = (0, 0);

                int modY = 1;
                int modX = 0;
                int count = 0;
                while (true) // go down (can flow left/right) until finding a solid tile.
                {
                    if (count > 96) { return false; } // If moved more than 96 tiles, fail
                    material = screen.getTileContent((posX + modX, posY - modY));
                    if (material.type < 0) { forceType = material; return false; } // for now if it bumps into already present liquid, do not try to extend the lake... might change in ze futur
                    if (material.type == 0) { modY++; count++; }
                    else if (screen.getTileContent((posX + modX - 1, posY - modY)).type <= 0 && screen.getTileContent((posX + modX - 1, posY - modY + 1)).type == 0) { modX--; count++; }
                    else if (screen.getTileContent((posX + modX + 1, posY - modY)).type <= 0 && screen.getTileContent((posX + modX + 1, posY - modY + 1)).type == 0) { modX++; count++; }
                    else { break; }
                }
                posToTest = (posX + modX, posY - modY + 1); // because uh this one was solid lol so need to fill the one ABOVE it
                int currentY = posToTest.y;

                long seedo = (seedX / 2 + seedY / 2) % 79461537;
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
                if (tilesToFill.Count < 0) { return false; } // No laketches ?


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
                    chunkDict[ChunkIdx(poso)].fillStates[PosMod(poso.x), PosMod(poso.y)] = material;
                    chunkDict[ChunkIdx(poso)].modificationCount = 1;
                }

                foreach (Chunk chunk in chunkDict.Values)
                {
                    Files.saveChunk(chunk);
                    chunkPresence[chunk.position] = true;
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
                if (type == 0) { cubeAmalgam(); }
                else if (type == 1) { sawBlade(); }
                else if (type == 2) { star(); }

                long seedo = (seedX / 2 + seedY / 2) % 79461537;
                name = "";
                int syllables = 2 + Min((int)(seedo % 13), (int)(seedo % 3));
                for (int i = 0; i < syllables; i++)
                {
                    name += nameArray[seedo % nameArray.Length];
                    seedo = LCGz(seedo);
                }
            }
            public void cubeAmalgam()
            {
                int squaresToDig = (int)(seedX % (10 + (size.Item1 * size.Item2))) + (int)(size.Item1 * size.Item2 * 0.2f) + 1;
                long seedoX = seedX;
                long seedoY = seedY;

                (int x, int y) posToTest;
                for (int gu = 0; gu < squaresToDig; gu++)
                {
                    seedoX = LCGxNeg(seedoX);
                    seedoY = LCGyNeg(seedoY);
                    int sizo = (int)((LCGxNeg(seedoY)) % 7 + 7) % 7 + 1;
                    int centerX = (int)(posX + sizo + seedoX % (size.Item1 * 32 - 2 * sizo));
                    int centerY = (int)(posX + sizo + seedoY % (size.Item2 * 32 - 2 * sizo));
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
            }
            public void sawBlade()
            {
                long seedoX = seedX;
                long seedoY = seedY;

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
                            structureDict[(posX + i, posY + j)] = (0, 0);
                            //outline
                            foreach ((int x, int y) mod in neighbourArray)
                            {
                                posToTest = (posX + i + mod.x, posY + j + mod.y);
                                if (!structureDict.ContainsKey(posToTest))
                                {
                                    structureDict[posToTest] = (1, 0);
                                }
                            }
                        }
                    }
                }
                // lil X thingy in the middle
                structureDict[(posX, posY)] = (1, 0);
                foreach ((int x, int y) mod in diagArray)
                {
                    structureDict[(posX + mod.x, posY + mod.y)] = (2, 0);
                }
            }
            public void star()
            {
                long seedoX = seedX;
                long seedoY = seedY;

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
                            structureDict[(posX + i, posY + j)] = (0, 0);
                            //outline
                            foreach ((int x, int y) mod in neighbourArray)
                            {
                                posToTest = (posX + i + mod.x, posY + j + mod.y);
                                if (!structureDict.ContainsKey(posToTest))
                                {
                                    structureDict[posToTest] = (1, 0);
                                }
                            }
                        }
                    }
                }
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
            public void saveInFile()
            {
                string savename = "";
                if (type == 3)
                {
                    savename = $"lake {name}";
                }
                else
                {
                    savename = $"{name} {structureNames[type]}";
                }
                using (StreamWriter f = new StreamWriter($"{currentDirectory}\\CaveData\\{screen.game.seed}\\StructureData\\{structChunkPosition.Item1}.{structChunkPosition.Item2}.{savename}.txt", false))
                {
                    string stringo = $"Welcome to structure {name}'s file !";
                    stringo += $"{name} is a {structureNames[type]}.";
                    f.Write(stringo);
                }
            }
        }
        public static bool testForBloodAltar(Screens.Screen screen, (int x, int y) startPos)
        {
            (int x, int y) posToTest;
            (int type, int subType) material = screen.getTileContent(startPos);
            if (material != (4, 0)) { return false; } // if start tile isn't fleshTIle, fail

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
                if (count > 5) { return false; } // If went down more than 5 tiles, fail
                posToTest = (startPos.x, startPos.y - count); 
                material = screen.getTileContent(posToTest);
                if (material != (0, 0))
                {
                    if (material == (-6, 0)) { break; } // if bumps on blood tile, proceed
                    else { return false; } // if bumps on a tile other than air or blood, fail
                }
                count++;
            }

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
                        count++;
                    }
                    else if (material == (1, 1)) { validity = (true, validity.right); } // if dense rock, continue and stop testing on the left (no blood so don't count it)
                    else { return false; } // if other than dense rock or blood, fail
                }
                if (!validity.right)
                {
                    material = screen.getTileContent((posToTest.x + currentX, posToTest.y));
                    if (material == (-6, 0)) // if blood :
                    {
                        if (screen.getTileContent((posToTest.x + currentX, posToTest.y - 1)) != (1, 1)) { return false; } // test if tile under it is denseRock (if not fail)
                        if (screen.getTileContent((posToTest.x + currentX, posToTest.y + 1)).type != 0) { return false; } // test if tile over it is air (if not fail)
                        count++;
                    }
                    else if (material == (1, 1)) { validity = (validity.left, true); } // if dense rock, continue and stop testing on the right (no blood so don't count it)
                    else { return false; } // if other than dense rock or blood, fail
                }
                currentX++;
            }
            if (count < 3 || count > 15) { return false; } // if blood pool is longer than 15 tiles, fail... bro it's too long lol stop trolling
            return true; // else blood altar is valid ! yay ! that was suprisingly easy to do. now test if bugs (i hope not i hate bunny (it's a joke i love bunnies yay !))
        }
    }
}
