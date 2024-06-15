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
using System.Linq.Expressions;

namespace Cave
{
    public class Plants
    {
        public static Dictionary<(int type, int subType), bool> upsideDownPlants = new Dictionary<(int type, int subType), bool>
        {
            { (2, 1), true },
            { (5, 0), true },
            { (5, 1), true },
            { (6, 0), true }
        };
        public class Branch
        {
            public Plant motherPlant;
            public int seed;
            public int type;
            public int growthLevel;
            public int maxGrowthLevel;
            public (int x, int y) pos;
            public (int x, int y) lastDrawPos = (0, 0);
            public Dictionary<(int x, int y), int> fillStates;

            public List<Branch> childBranches = new List<Branch>();
            public List<Flower> childFlowers = new List<Flower>();
            public Branch(Plant motherPlantToPut, BranchJson branchJson)
            {
                motherPlant = motherPlantToPut;
                pos = branchJson.pos;
                lastDrawPos = branchJson.lstGrPos;
                type = branchJson.type;
                seed = branchJson.seed;
                fillStates = arrayToFillstates(branchJson.fillStates);
                foreach (BranchJson baby in branchJson.branches)
                {
                    childBranches.Add(new Branch(motherPlant, baby));
                }
                foreach (FlowerJson flowerJson in branchJson.flowers)
                {
                    childFlowers.Add(new Flower(motherPlant, flowerJson));
                }
                growthLevel = branchJson.grLvl;
            }
            public Branch(Plant motherPlantToPut, (int x, int y) posToPut, int typeToPut, int seedToPut)
            {
                motherPlant = motherPlantToPut;
                pos = posToPut;
                type = typeToPut;
                seed = seedToPut;
                fillStates = new Dictionary<(int x, int y), int>();
                growthLevel = -1;
                tryGrowth();
            }
            public void updatePos((int x, int y) mod)
            {
                foreach (Branch branch in childBranches)
                {
                    branch.pos = (branch.pos.x + mod.x, branch.pos.y + mod.y);
                    branch.updatePos(mod);
                }
                foreach (Flower flower in childFlowers)
                {
                    flower.pos = (flower.pos.x + mod.x, flower.pos.y + mod.y);
                }
                pos = (pos.x + mod.x, pos.y + mod.y);
            }
            public void updatePosSet((int x, int y) posToSet)
            {
                (int x, int y) mod = (posToSet.x - pos.x, posToSet.y - pos.y);
                foreach (Branch branch in childBranches)
                {
                    branch.pos = (branch.pos.x + mod.x, branch.pos.y + mod.y);
                    branch.updatePos(mod);
                }
                foreach (Flower flower in childFlowers)
                {
                    flower.pos = (flower.pos.x + mod.x, flower.pos.y + mod.y);
                }
                pos = posToSet;
            }
            public (int x, int y) absolutePos((int x, int y) position)
            {
                return (pos.x + position.x, pos.y + position.y);
            }
            public bool tryFill((int x, int y) testPos, int typeToFill)
            {
                if (motherPlant.testIfPositionEmpty(absolutePos(testPos))) { fillStates[testPos] = typeToFill; return true; }
                return false;
            }
            public bool tryGrowth() // 0 = nothing, 1 = plantMatter, 2 = wood, 3 = aquaticPlantMatter, 4 = mushroomStem, 5 = mushroomCap, 6 = petal, 7 = flowerPollen
            {
                (int x, int y) drawPos = lastDrawPos;
                int growthLevelToTest = growthLevel + 1;
                if (growthLevelToTest == 0)
                {
                    goto Success;
                }

                if (motherPlant.type == 1) // woody tree
                {
                    if (motherPlant.subType == 0)
                    {
                        maxGrowthLevel = Min(motherPlant.maxGrowthLevel - pos.y, seed % motherPlant.maxGrowthLevel);
                        if (growthLevelToTest > maxGrowthLevel) { goto Fail; }

                        int direction; // -1 = left, 1 = right
                        direction = (seed % 2) * 2 - 1;

                        drawPos = (lastDrawPos.x, lastDrawPos.y + 1);

                        if (LCGint1(seed + growthLevelToTest) % Max(1, (int)(growthLevelToTest * 0.5f)) == 0 || growthLevelToTest < 3)
                        {
                            drawPos = (drawPos.x + direction, drawPos.y);
                        }

                        if (tryFill(drawPos, 2))
                        {
                            if (growthLevel == 1 + seed % 2)
                            {
                                Flower baby = new Flower(motherPlant, absolutePos(drawPos), 0, LCGint1(seed + 3 * growthLevelToTest));
                                childFlowers.Add(baby);
                            }
                            else
                            {
                                foreach (Flower flower in childFlowers)
                                {
                                    flower.pos = absolutePos(drawPos);
                                }
                            }
                            goto Success;
                        }
                        goto Fail;
                    }
                    else if (motherPlant.subType == 1)
                    {
                        drawPos = lastDrawPos;

                        int direction; // -1 = left, 1 = right
                        direction = (seed % 2) * 2 - 1;

                        int turningAge = 2 + seed % 10;
                        int diagoLengto = 1 + seed % 2;

                        maxGrowthLevel = Min(turningAge + diagoLengto + motherPlant.maxGrowthLevel - pos.y, turningAge + diagoLengto + seed % motherPlant.maxGrowthLevel);
                        if (growthLevelToTest > maxGrowthLevel) { goto Fail; }


                        if (growthLevelToTest >= turningAge)
                        {
                            if (growthLevelToTest >= turningAge + diagoLengto)
                            {
                                drawPos = (drawPos.x, drawPos.y + 1);
                            }
                            else
                            {
                                drawPos = (drawPos.x + direction, drawPos.y + 1);
                            }
                        }
                        else
                        {
                            drawPos = (drawPos.x + direction, drawPos.y);
                        }

                        if (tryFill(drawPos, 2))
                        {
                            if (growthLevel == 1 + seed % 2)
                            {
                                Flower baby = new Flower(motherPlant, absolutePos(drawPos), 0, LCGint1(seed + 3 * growthLevelToTest));
                                childFlowers.Add(baby);
                            }
                            else
                            {
                                foreach (Flower flower in childFlowers)
                                {
                                    flower.pos = absolutePos(drawPos);
                                }
                            }
                            goto Success;
                        }
                        goto Fail;
                    }
                }


            SuccessButStop:;
                lastDrawPos = drawPos;
                growthLevel = growthLevelToTest;

            Fail:;
                return false;

            Success:;
                lastDrawPos = drawPos;
                growthLevel = growthLevelToTest;
                return true;
            }
        }
        public class Flower
        {
            public Plant motherPlant;
            public int seed;
            public int type;
            public int growthLevel;
            public int maxGrowthLevel;
            public (int x, int y) pos;
            public (int x, int y) lastDrawPos = (0, 0);
            public Dictionary<(int x, int y), int> fillStates;
            public Flower(Plant motherPlantToPut, FlowerJson flowerJson)
            {
                motherPlant = motherPlantToPut;
                pos = flowerJson.pos;
                lastDrawPos = flowerJson.lstGrPos;
                type = flowerJson.type;
                seed = flowerJson.seed;
                fillStates = arrayToFillstates(flowerJson.fillStates);
                growthLevel = flowerJson.grLvl;
            }
            public Flower(Plant motherPlantToPut, (int x, int y) posToPut, int typeToPut, int seedToPut)
            {
                motherPlant = motherPlantToPut;
                pos = posToPut;
                type = typeToPut;
                seed = seedToPut;
                fillStates = new Dictionary<(int x, int y), int>();
                growthLevel = -1;
            }
            public (int x, int y) absolutePos((int x, int y) position)
            {
                return (pos.x + position.x, pos.y + position.y);
            }
            public bool tryFill((int x, int y) testPos, int typeToFill)
            {
                if (motherPlant.testIfPositionEmpty(absolutePos(testPos))) { fillStates[testPos] = typeToFill; return true; }
                return false;
            }
            public bool tryGrowth() // 0 = nothing, 1 = plantMatter, 2 = wood, 3 = aquaticPlantMatter, 4 = mushroomStem, 5 = mushroomCap, 6 = petal, 7 = flowerPollen
            {
                int growthLevelToTest = growthLevel + 1;
                if (growthLevelToTest == 0)
                {
                    goto Success;
                }

                if (motherPlant.type == 0)
                {
                    fillStates = new Dictionary<(int x, int y), int>();
                    if (motherPlant.subType == 2)                                   // Tulip
                    {
                        maxGrowthLevel = 4;

                        if (growthLevelToTest == 1)
                        {
                            tryFill((0, 0), 6);
                        }
                        else if (growthLevelToTest == 2)
                        {
                            tryFill((0, 0), 6);
                            tryFill((0, 1), 6);
                        }
                        else if (growthLevelToTest == 3)
                        {
                            tryFill((0, 0), 6);
                            tryFill((0, 1), 6);
                            tryFill((0, 2), 6);
                        }
                        else if (growthLevelToTest == 4)
                        {
                            tryFill((0, 0), 6);
                            tryFill((-1, 1), 6);
                            tryFill((0, 1), 6);
                            tryFill((1, 1), 6);
                            tryFill((0, 2), 6);
                        }
                        else
                        {
                            tryFill((0, 0), 6);
                            tryFill((-1, 1), 6);
                            tryFill((0, 1), 6);
                            tryFill((1, 1), 6);
                            tryFill((-1, 2), 6);
                            tryFill((1, 2), 6);
                            goto SuccessButStop;
                        }
                        goto Success;
                    }
                    if (motherPlant.subType == 3)                                   // Allium
                    {
                        maxGrowthLevel = 3;

                        if (growthLevelToTest == 1)
                        {
                            tryFill((0, 0), 6);
                        }
                        else if (growthLevelToTest == 2)
                        {
                            tryFill((-1, 0), 6);
                            tryFill((0, 0), 6);
                            tryFill((1, 0), 6);
                            tryFill((-1, 1), 6);
                            tryFill((0, 1), 6);
                            tryFill((1, 1), 6);
                        }
                        else if (growthLevelToTest == 3)
                        {
                            tryFill((-1, 0), 6);
                            tryFill((0, 0), 6);
                            tryFill((1, 0), 6);
                            tryFill((-1, 1), 6);
                            tryFill((0, 1), 6);
                            tryFill((1, 1), 6);
                            tryFill((-1, 2), 6);
                            tryFill((0, 2), 6);
                            tryFill((1, 2), 6);
                        }
                        else
                        {
                            tryFill((-1, 0), 6);
                            tryFill((0, 0), 6);
                            tryFill((1, 0), 6);
                            tryFill((-2, 1), 6);
                            tryFill((-1, 1), 6);
                            tryFill((0, 1), 6);
                            tryFill((1, 1), 6);
                            tryFill((2, 1), 6);
                            tryFill((-2, 2), 6);
                            tryFill((-1, 2), 6);
                            tryFill((0, 2), 6);
                            tryFill((1, 2), 6);
                            tryFill((2, 2), 6);
                            tryFill((-1, 3), 6);
                            tryFill((0, 3), 6);
                            tryFill((1, 3), 6);
                            goto SuccessButStop;
                        }
                        goto Success;
                    }

                    // else, case of candle ig
                    maxGrowthLevel = 1;
                    goto Fail;
                }
                if (motherPlant.type == 1) // tree
                {
                    if (motherPlant.subType == 1)
                    {
                        fillStates = new Dictionary<(int x, int y), int>();
                        if (growthLevelToTest == 1)
                        {
                            tryFill((0, 0), 1);
                        }
                        else if (growthLevelToTest == 2)
                        {
                            tryFill((0, 0), 1);
                            tryFill((0, 1), 2);
                        }
                        else if (growthLevelToTest == 3)
                        {
                            tryFill((0, 0), 1);
                            tryFill((0, 1), 2);
                            tryFill((-1, 1), 2);
                            tryFill((1, 1), 2);
                        }
                        else if (growthLevelToTest == 4)
                        {
                            tryFill((0, 0), 1);
                            tryFill((0, 1), 2);
                            tryFill((-1, 1), 2);
                            tryFill((1, 1), 2);
                            tryFill((0, 2), 2);
                        }
                        else if (growthLevelToTest >= 5)
                        {
                            tryFill((-1, -1), 2);
                            tryFill((0, -1), 2);
                            tryFill((1, -1), 2);
                            tryFill((0, 0), 1);
                            tryFill((-2, 1), 2);
                            tryFill((-1, 1), 2);
                            tryFill((0, 1), 2);
                            tryFill((1, 1), 2);
                            tryFill((2, 1), 2);
                            tryFill((0, 2), 2);
                            goto SuccessButStop;
                        }
                        goto Success;
                    }
                    else
                    {
                        fillStates = new Dictionary<(int x, int y), int>();

                        (int x, int y) drawPos = (0, 0);

                        if (growthLevelToTest == 1) // test so chandelier is one pixel big
                        {
                            tryFill(drawPos, 1);
                        }
                        else if (growthLevelToTest == 2)
                        {
                            for (int i = -1; i < 2; i++)
                            {
                                for (int j = -1; j < 2; j++)
                                {
                                    drawPos = (i, j);

                                    if (!(Abs(i) == 1 && Abs(j) == 1))
                                    {
                                        tryFill(drawPos, 1);
                                    }
                                }
                            }
                        }
                        else if (growthLevelToTest == 3)
                        {
                            for (int i = -1; i < 2; i++)
                            {
                                for (int j = -1; j < 2; j++)
                                {
                                    drawPos = (i, j);


                                    tryFill(drawPos, 1);
                                }
                            }
                        }
                        else if (growthLevelToTest >= 4)
                        {
                            for (int i = -2; i < 3; i++)
                            {
                                for (int j = -2; j < 3; j++)
                                {
                                    drawPos = (i, j);

                                    if (!(Abs(i) == 2 && Abs(j) == 2))
                                    {
                                        tryFill(drawPos, 1);
                                    }
                                }
                            }
                            goto SuccessButStop;
                        }
                        goto Success;
                    }
                }
                else if (motherPlant.type == 4) // mushy
                {
                    maxGrowthLevel = motherPlant.maxGrowthLevel;
                    if (growthLevelToTest > maxGrowthLevel) { goto Fail; }

                    fillStates = new Dictionary<(int x, int y), int>();

                    tryFill((-1, 0), 5);
                    tryFill((0, 0), 5);
                    tryFill((1, 0), 5);
                    if (growthLevelToTest > seed % 8 + 2)
                    {
                        tryFill((-2, 0), 5);
                        tryFill((2, 0), 5);
                        if (growthLevelToTest > seed % 8 + 3 + seed % 2)
                        {
                            tryFill((-1, 1), 5);
                            tryFill((0, 1), 5);
                            tryFill((1, 1), 5);
                        }
                    }
                    goto Success;
                }
                else if (motherPlant.type == 5) // vine
                {
                    maxGrowthLevel = 3;
                    fillStates = new Dictionary<(int x, int y), int>();
                    if (type == 0)
                    {
                        if (growthLevelToTest == 1)
                        {
                            tryFill((0, 0), 6);
                        }
                        else if (growthLevelToTest == 2)
                        {
                            tryFill((0, 0), 6);
                            tryFill((1, 0), 6);
                            tryFill((-1, 0), 6);
                            tryFill((0, 1), 6);
                            tryFill((0, -1), 6);
                        }
                        else if (growthLevelToTest >= 3)
                        {
                            tryFill((0, 0), 7);
                            tryFill((1, 0), 6);
                            tryFill((-1, 0), 6);
                            tryFill((0, 1), 6);
                            tryFill((0, -1), 6);
                            goto SuccessButStop;
                        }
                        goto Success;
                    }
                    else
                    {
                        if (growthLevelToTest == 1)
                        {
                            tryFill((0, 0), 6);
                        }
                        else if (growthLevelToTest == 2)
                        {
                            tryFill((0, 0), 6);
                            tryFill((1, 1), 6);
                            tryFill((-1, 1), 6);
                            tryFill((1, -1), 6);
                            tryFill((-1, -1), 6);
                        }
                        else if (growthLevelToTest >= 3)
                        {
                            tryFill((0, 0), 7);
                            tryFill((1, 1), 6);
                            tryFill((-1, 1), 6);
                            tryFill((1, -1), 6);
                            tryFill((-1, -1), 6);
                            goto SuccessButStop;
                        }
                        goto Success;
                    }
                }

            SuccessButStop:;
                growthLevel = growthLevelToTest;

            Fail:;
                return false;

            Success:;
                growthLevel = growthLevelToTest;
                return true;
            }
        }
        public class Plant
        {
            public Screens.Screen screen;

            public int seed;
            public int id;
            public int type;
            public int subType;
            public int state;
            public int growthLevel;
            public int maxGrowthLevel;
            public int posX = 0;
            public int posY = 0;
            public int attachPoint; // 0 ground, 1 leftWall, 2 rightWall, 3 ceiling
            public Dictionary<int, Color> colorDict;

            public float timeAtLastGrowth = timeElapsed;

            public Bitmap bitmap = new Bitmap(1, 1);
            public Bitmap secondaryBitmap = new Bitmap(1, 1);
            public Dictionary<(int x, int y), int> fillStates = new Dictionary<(int x, int y), int>();
            public List<(int x, int y)> lightPositions = new List<(int x, int y)>();
            public Color lightColor = Color.Black;
            public int lightMaterial = 0;
            public int[] posOffset = new int[3];
            public int[] bounds = new int[4];
            public (int x, int y) lastDrawPos = (0, 0);

            public List<Branch> childBranches = new List<Branch>();
            public List<Flower> childFlowers = new List<Flower>();

            public bool isDeadAndShouldDisappear = false;
            public bool isStable = false;
            public Plant(Screens.Screen screenToPut, PlantJson plantJson)
            {
                screen = screenToPut;
                posX = plantJson.pos.Item1;
                posY = plantJson.pos.Item2;
                lastDrawPos = plantJson.lstGrPos;
                type = plantJson.type.Item1;
                subType = plantJson.type.Item2;
                seed = plantJson.seed;
                id = plantJson.id;
                growthLevel = plantJson.grLvl;
                timeAtLastGrowth = plantJson.lastGr;
                fillStates = arrayToFillstates(plantJson.fillStates);
                foreach (BranchJson branchJson in plantJson.branches)
                {
                    childBranches.Add(new Branch(this, branchJson));
                }
                foreach (FlowerJson flowerJson in plantJson.flowers)
                {
                    childFlowers.Add(new Flower(this, flowerJson));
                }
                findColors();
                makeBitmap();
            }
            public Plant(Chunk chunkToPut, int groupOfPlant)
            {
                posX = chunkToPut.position.x*32;
                posY = chunkToPut.position.y*32;
                screen = chunkToPut.screen;
                seed = LCGint1(Abs((int)chunkToPut.chunkSeed));
                seed = Abs(seed + rand.Next(100000)); // TO CHANGE TOCHANGE cuz false randommmm
                id = currentPlantId;
                growthLevel = -1;
                placePlant();
                findType(groupOfPlant);
                if (isDeadAndShouldDisappear) { return; }
                findColors();
                growToMaximum();
                makeBitmap();
                timeAtLastGrowth = timeElapsed;

                currentPlantId++;
            }
            public Plant(Screens.Screen screenToPut, (int, int) positionToPut, int typeToPut, int subTypeToPut)
            {
                screen = screenToPut;
                posX = positionToPut.Item1;
                posY = positionToPut.Item2;
                type = typeToPut;
                subType = subTypeToPut;
                if (upsideDownPlants.ContainsKey((typeToPut, subTypeToPut))) { attachPoint = 3; }
                seed = rand.Next(1000000000); //                               FALSE RANDOM NOT SEEDED ARGHHEHEEEE
                id = currentPlantId;
                growthLevel = -1;
                testPlantPosition();
                if (isDeadAndShouldDisappear) { return; }
                findColors();
                tryGrowth();
                makeBitmap();
                timeAtLastGrowth = timeElapsed;

                currentPlantId++;
            }
            public bool tryFill((int x, int y) testPos, int typeToFill)
            {
                if (testIfPositionEmpty(testPos)) { fillStates[testPos] = typeToFill; return true; }
                return false;
            }
            public void findType(int group) // 0 = small, 1 = tree
            {
                (int x, int y) tileIndex = GetChunkIndexFromTile(posX, posY);

                (int x, int y) chunkPos = screen.findChunkAbsoluteIndex(posX, posY);
                if (!screen.loadedChunks.ContainsKey(chunkPos))
                {
                    return;
                }
                Chunk chunkToTest = screen.loadedChunks[chunkPos];

                (int biome, int subBiome) biome = chunkToTest.biomeIndex[tileIndex.x, tileIndex.y][0].Item1;
                if (attachPoint == 0)
                {
                    if (chunkToTest.fillStates[tileIndex.x, tileIndex.y] < 0)
                    {
                        type = 2;
                        subType = 0;
                    }
                    else if (biome == (3, 0)) // forest
                    {
                        if (group == 1)
                        {
                            type = 1;
                            subType = 0;
                        }
                        else
                        {
                            int rando = rand.Next(101);
                            if (rando < 40)
                            {
                                type = 0;
                                subType = 0;
                            }
                            else if (rando > 80)
                            {
                                type = 0;
                                subType = 3;
                            }
                            else
                            {
                                type = 0;
                                subType = 2;
                            }
                        }
                    }
                    else if (biome == (5, 0)) // fairy
                    {
                        type = 4;
                        subType = 0;
                    }
                    else if (biome == (6, 0)) // obsidian
                    {
                        if (rand.Next(100) == 0)
                        {
                            type = 1;
                            subType = 0;
                        }
                        else
                        {
                            type = 3;
                            subType = 0;
                        }
                    }
                    else if (biome == (9, 0)) // chandelier
                    {
                        if (group == 0)
                        {
                            type = 0;
                            subType = 1;
                        }
                        else if (group == 1)
                        {
                            type = 1;
                            subType = 1;
                        }
                    }
                    else
                    {
                        type = 0;
                        subType = 0;
                    }
                }

                else if (attachPoint == 3)
                {
                    if (chunkToTest.fillStates[tileIndex.x, tileIndex.y] < 0)
                    {
                        type = 2;
                        subType = 1;
                    }
                    else if (biome == (6, 0)) // obsidian
                    {
                        type = 5;
                        subType = 1;
                    }
                    else if (biome == (9, 0))
                    {
                        isDeadAndShouldDisappear = true;
                    }
                    else
                    {
                        if (seed % 7 == 0 && false) // the fuck is this for ? I don't even recall ????      probably upside down trees
                        {
                            type = 6;
                            subType = 0;
                        }
                        else
                        {
                            type = 5;
                            subType = 0;
                        }
                    }
                }
            }
            public void findColors()
            {
                colorDict = new Dictionary<int, Color>();
                int seedo = LCGint1(seed);
                int hueVar = (int)(seedo % 101) - 50;
                seedo = LCGint1(seed);
                int shadeVar = (int)(seedo % 61) - 30;
                //(int x, int y) tileIndex = GetChunkIndexFromTile(posX, posY, 32);
                //int biome = chunk.biomeIndex[tileIndex.x, tileIndex.y][0].Item1;
                if (type == 0) // normal
                {
                    if (subType == 1)
                    {
                        shadeVar = (int)(shadeVar * 0.3f);
                        colorDict.Add(1, Color.FromArgb(210 - shadeVar, 210 - shadeVar, 200 - shadeVar));
                        colorDict.Add(2, Color.FromArgb(40 - shadeVar, 30 - shadeVar, 10 - shadeVar));
                        colorDict.Add(6, Color.FromArgb(220 - shadeVar, 200 - shadeVar, 80 - shadeVar));
                        colorDict.Add(7, Color.FromArgb(200 - shadeVar, 120 - shadeVar, 40 - shadeVar));
                    }
                    else if (subType == 2)
                    {
                        colorDict.Add(1, Color.FromArgb(50 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                        colorDict.Add(6, Color.FromArgb(220 - shadeVar, 110 - hueVar - shadeVar, 130 + hueVar - shadeVar));
                    }
                    else if (subType == 3)
                    {
                        colorDict.Add(1, Color.FromArgb(50 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                        colorDict.Add(6, Color.FromArgb(140 - shadeVar, 80 - hueVar - shadeVar, 220 - shadeVar));
                    }
                    else
                    {
                        colorDict.Add(1, Color.FromArgb(50 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                    }
                    return;
                }
                else if (type == 1) // woody
                {
                    if (subType == 1)
                    {
                        shadeVar = (int)(shadeVar*0.3f);
                        colorDict.Add(1, Color.FromArgb(230 - shadeVar, 230 - shadeVar, 120 - shadeVar));
                        colorDict.Add(2, Color.FromArgb(40 - shadeVar, 40 - shadeVar, 60 - shadeVar));
                        colorDict.Add(6, Color.FromArgb(170 - shadeVar, 120 - hueVar - shadeVar, 150 - shadeVar));
                        colorDict.Add(7, Color.FromArgb(170 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                    }
                    else
                    {
                        colorDict.Add(1, Color.FromArgb(50 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                        colorDict.Add(2, Color.FromArgb(140 + (int)(hueVar * 0.3f) - shadeVar, 140 - (int)(hueVar * 0.3f) - shadeVar, 50 - shadeVar));
                        colorDict.Add(6, Color.FromArgb(170 - shadeVar, 120 - hueVar - shadeVar, 150 - shadeVar));
                        colorDict.Add(7, Color.FromArgb(170 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                    }
                }
                else if (type == 2) // kelp
                {
                    colorDict.Add(3, Color.FromArgb(30 - shadeVar, 90 - shadeVar + hueVar, 140 - shadeVar - hueVar));
                }
                else if (type == 3) // obsidian
                {
                    colorDict.Add(1, Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar));
                }
                else if (type == 4) // mushroom
                {
                    colorDict.Add(4, Color.FromArgb(180 + shadeVar, 160 + shadeVar, 165 + shadeVar));
                    colorDict.Add(5, Color.FromArgb(140 - shadeVar, 120 + hueVar, 170 - hueVar));
                }
                else if (type == 5) // vine
                {
                    if (subType == 0)
                    {
                        colorDict.Add(1, Color.FromArgb(50 - shadeVar, 120 - hueVar - shadeVar, 50 - shadeVar));
                        colorDict.Add(6, Color.FromArgb(170 - shadeVar, 120 - hueVar - shadeVar, 150 - shadeVar));
                        colorDict.Add(7, Color.FromArgb(170 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                    }
                    else if (subType == 1)
                    {
                        colorDict.Add(1, Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar));
                        colorDict.Add(6, Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar));
                        colorDict.Add(7, Color.FromArgb(220 + shadeVar, 220 + shadeVar, 220 + shadeVar));
                    }
                }
            }
            public bool testIfPositionEmpty((int x, int y) mod)
            {
                (int x, int y) pixelPos = (posX + mod.x, posY + mod.y);
                (int x, int y) pixelTileIndex = GetChunkIndexFromTile(pixelPos.x, pixelPos.y);
                (int x, int y) chunkPos = (Floor(pixelPos.x, 32) / 32, Floor(pixelPos.y, 32) / 32);

                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    if (screen.loadedChunks[chunkPos].fillStates[pixelTileIndex.x, pixelTileIndex.y] > 0)
                    {
                        return false;
                    }
                    return true;
                }

                if (!screen.extraLoadedChunks.ContainsKey(chunkPos)) { screen.extraLoadedChunks.Add(chunkPos, new Chunk(chunkPos, true, screen)); }
                if (screen.extraLoadedChunks[chunkPos].fillStates[pixelTileIndex.x, pixelTileIndex.y] > 0)
                {
                    return false;
                }
                return true;
            }
            public void makeBitmap()
            {
                List<Branch> branchList = new List<Branch>(childBranches);
                List<Flower> flowerList = new List<Flower>(childFlowers);

                Branch currentBranch;
                for (int i = 0; i < branchList.Count; i++)
                {
                    currentBranch = branchList[i];
                    foreach (Branch branch in currentBranch.childBranches)
                    {
                        branchList.Add(branch);
                    }
                    foreach (Flower flower in currentBranch.childFlowers)
                    {
                        flowerList.Add(flower);
                    }
                }

                Dictionary<(int x, int y), int> fillDict = new Dictionary<(int x, int y), int>(fillStates);

                foreach (Branch branch in branchList)
                {
                    foreach ((int x, int y) keyo in branch.fillStates.Keys)
                    {
                        fillDict[(keyo.x + branch.pos.x, keyo.y + branch.pos.y)] = branch.fillStates[keyo];
                    }
                }
                foreach (Flower flower in flowerList)
                {
                    foreach ((int x, int y) keyo in flower.fillStates.Keys)
                    {
                        fillDict[(keyo.x + flower.pos.x, keyo.y + flower.pos.y)] = flower.fillStates[keyo];
                    }
                }

                int minX = 0;
                int maxX = 0;
                int minY = 0;
                int maxY = 0;

                foreach ((int x, int y) drawPos in fillDict.Keys)
                {
                    if (drawPos.x < minX)
                    {
                        minX = drawPos.x;
                    }
                    else if (drawPos.x > maxX)
                    {
                        maxX = drawPos.x;
                    }
                    if (drawPos.y < minY)
                    {
                        minY = drawPos.y;
                    }
                    else if (drawPos.y > maxY)
                    {
                        maxY = drawPos.y;
                    }
                }

                int width = maxX - minX + 1;
                int height = maxY - minY + 1;

                Bitmap bitmapToMake;
                bitmap = new Bitmap(width, height);
                bitmapToMake = bitmap;

                foreach ((int x, int y) drawPos in fillDict.Keys)
                {
                    setPixelButFaster(bitmapToMake, (drawPos.x - minX, drawPos.y - minY), colorDict[fillDict[drawPos]]);
                }

                posOffset[0] = minX;
                posOffset[1] = minY;

                int one = posX - posOffset[0];
                int two = posX + width - posOffset[0] - 1;
                int three = posY - posOffset[1];
                int four = posY + height - posOffset[1] - 1;

                (int, int) tupelo = screen.findChunkAbsoluteIndex(one, three);
                (int, int) tupela = screen.findChunkAbsoluteIndex(two, four);

                bounds[0] = tupelo.Item1;
                bounds[1] = tupelo.Item2;
                bounds[2] = tupela.Item1;
                bounds[3] = tupela.Item2;

                //addPlantInChunks();
                findLightPositions(branchList, flowerList);
            }
            public void findLightPositions(List<Branch> branchList, List<Flower> flowerList)
            {
                lightPositions = new List<(int x, int y)>();

                if (type == 0 && subType == 1)
                {
                    lightMaterial = 7;
                    if (childFlowers.Count > 0)
                    {
                        Flower fireFlower = childFlowers[0];
                        lightPositions.Add((fireFlower.pos.x + posX, fireFlower.pos.y + posY + 2)); // !!!!!!!!!! the +1 !!!
                    }
                }
                else if (type == 1 && subType == 1) { lightMaterial = 1; }
                else { lightMaterial = 0; }

                foreach((int x, int y) pos in fillStates.Keys)
                {
                    if (fillStates[pos] == lightMaterial)
                    {
                        lightPositions.Add((pos.x + posX, pos.y + posY));
                    }
                }
                foreach (Branch branch in branchList)
                {
                    foreach ((int x, int y) keyo in branch.fillStates.Keys)
                    {
                        if (branch.fillStates[keyo] == lightMaterial)
                        {
                            lightPositions.Add((keyo.x + branch.pos.x + posX, keyo.y + branch.pos.y + posY));
                        }
                    }
                }
                foreach (Flower flower in flowerList)
                {
                    foreach ((int x, int y) keyo in flower.fillStates.Keys)
                    {
                        if (flower.fillStates[keyo] == lightMaterial)
                        {
                            lightPositions.Add((keyo.x + flower.pos.x + posX, keyo.y + flower.pos.y + posY));
                        }
                    }
                }

                if (colorDict.ContainsKey(lightMaterial))
                {
                    Color col = colorDict[lightMaterial];
                    lightColor = Color.FromArgb(255, (col.R+255)/2,(col.G+255)/2, (col.B+255)/2);
                }
                else { lightColor = Color.Black; }
            }
            public void growToMaximum()
            {
                isStable = false;
                while (!isStable)
                {
                    isStable = !testPlantGrowth(true);
                }
            }
            public Dictionary<(int type, int subType), (int min, int range)> maxGrowthDict = new Dictionary<(int type, int subType), (int min, int range)>
            {
                { (0, 0), (1, 5) }, // grass
                { (0, 1), (1, 5) }, // candle
                { (0, 2), (2, 4) }, // tulip
                { (0, 3), (4, 3) }, // allum
                { (0, 4), (1, 5) },
            };
            public bool tryGrowth() // 0 = nothing, 1 = plantMatter, 2 = wood, 3 = aquaticPlantMatter, 4 = mushroomStem, 5 = mushroomCap, 6 = petal, 7 = flowerPollen
            {
                int seedo = seed;
                int growthLevelToTest = growthLevel + 1;
                (int x, int y) drawPos = lastDrawPos;

                if (growthLevelToTest == 0)
                {
                    if (type == 5 || (type == 3 && subType == 1))
                    {
                        drawPos = (0, 1);
                    }
                    else
                    {
                        drawPos = (0, -1);
                    }
                    goto Success;
                }

                if (type == 0) // normal plant
                {
                    if (!maxGrowthDict.ContainsKey((type, subType))) { goto Fail; }
                    (int min, int range) maxGrowthTuple = maxGrowthDict[(type, subType)];
                    if (growthLevelToTest > maxGrowthTuple.min + seed % maxGrowthTuple.range) { goto Fail; }

                    drawPos = (lastDrawPos.x, lastDrawPos.y + 1);
                    if (subType == 1 || subType == 2 || subType == 3) // straight growing flowers
                    {
                        if (testIfPositionEmpty((drawPos.x, drawPos.y+2)))
                        {
                            if (tryFill(drawPos, 1))
                            {
                                if (growthLevel == seed % 2)
                                {
                                    Flower baby = new Flower(this, drawPos, 0, LCGint1(seed + 3 * growthLevelToTest));
                                    childFlowers.Add(baby);
                                }
                                foreach (Flower flower in childFlowers)
                                {
                                    flower.pos = (drawPos.x, drawPos.y+1);
                                }
                                goto Success;
                            }
                        }
                    }
                    else
                    {
                        seedo = LCGint1(seed + growthLevelToTest * (seed % 7 + 1));
                        int resulto = seedo % 3;
                        if (resulto == 0 && growthLevel % 2 == 1)
                        {
                            drawPos = (drawPos.x - 1, drawPos.y);
                        }
                        else if (resulto == 2 && growthLevel % 2 == 1)
                        {
                            drawPos = (drawPos.x + 1, drawPos.y);
                        }

                        if (tryFill(drawPos, 1))
                        {
                            goto Success;
                        }
                    }
                }
                else if (type == 1) // tree
                {
                    maxGrowthLevel = 10 + seed % 40;
                    if (growthLevelToTest > maxGrowthLevel) { goto Fail; }
                    if (growthLevelToTest == 0) { }

                    drawPos = (lastDrawPos.x, lastDrawPos.y + 1);

                    int spacing = 3 + seed % 3;
                    seedo = LCGint1(seed + growthLevelToTest * (seed % 7 + 1));
                    if (subType != 1)
                    {
                        int resulto = seedo % 3;
                        if (resulto == 0 && growthLevel == 2)
                        {
                            drawPos = (drawPos.x - 1, drawPos.y);
                        }
                        else if (resulto == 2 && growthLevel == 2)
                        {
                            drawPos = (drawPos.x + 1, drawPos.y);
                        }
                    }
                    else { spacing += 2; }

                    if (tryFill(drawPos, 2))
                    {
                        lastDrawPos = drawPos;

                        if (growthLevelToTest % spacing == 2)
                        {
                            Branch branch = new Branch(this, drawPos, 0, LCGint2(seed + growthLevelToTest));
                            childBranches.Add(branch);
                        }

                        if (growthLevel == 1 + seed % 2)
                        {
                            Flower baby = new Flower(this, drawPos, 0, LCGint1(seed + 3 * growthLevelToTest));
                            childFlowers.Add(baby);
                        }
                        else
                        {
                            foreach (Flower flower in childFlowers)
                            {
                                flower.pos = drawPos;
                            }
                        }
                        goto Success;
                    }
                }
                else if (type == 2) // kelp
                {
                    if (subType == 0)
                    {
                        maxGrowthLevel = 1 + seed % 10;
                        if (growthLevelToTest > maxGrowthLevel) { goto Fail; }

                        int multo = 2 * (seed % 2) - 1;
                        drawPos = (((growthLevelToTest - 1) % 2) * multo, growthLevelToTest - 1);

                        if (tryFill(drawPos, 3))
                        {
                            goto Success;
                        }
                    }
                    else if (subType == 1)
                    {
                        maxGrowthLevel = 1 + seed % 10;
                        if (growthLevelToTest > maxGrowthLevel) { goto Fail; }

                        int multo = 2 * (seed % 2) - 1;
                        drawPos = (((growthLevelToTest - 1) % 2) * multo, -(growthLevelToTest - 1));

                        if (tryFill(drawPos, 3))
                        {
                            goto Success;
                        }
                    }
                }
                else if (type == 3) // obsidian plant
                {
                    maxGrowthLevel = 1 + seed % 3;
                    if (growthLevelToTest > maxGrowthLevel) { goto Fail; }

                    drawPos = (lastDrawPos.x, lastDrawPos.y + 1);

                    if (tryFill(drawPos, 1))
                    {
                        goto Success;
                    }
                }
                else if (type == 4) // mushroom
                {
                    maxGrowthLevel = 1 + seed % 7;
                    if (growthLevelToTest > maxGrowthLevel) { goto Fail; }

                    drawPos = (lastDrawPos.x, lastDrawPos.y + 1);

                    if (tryFill(drawPos, 4))
                    {
                        if (growthLevel == 1)
                        {
                            Flower baby = new Flower(this, drawPos, 0, LCGint1(seed + 3 * growthLevelToTest));
                            childFlowers.Add(baby);
                        }
                        else
                        {
                            foreach (Flower flower in childFlowers)
                            {
                                flower.pos = drawPos;
                            }
                        }
                        goto Success;
                    }
                }
                else if (type == 5) // vine
                {
                    maxGrowthLevel = 4 + seed % 60;
                    if (growthLevelToTest > maxGrowthLevel) { goto Fail; }

                    int multo = 2 * (seed % 2) - 1;
                    drawPos = (((int)((growthLevelToTest - 1) * 0.5f) % 2) * multo, -(growthLevelToTest - 1));

                    if (tryFill(drawPos, 1))
                    {
                        int spacingOfFlowers = 4 + seedo % 4;
                        if (growthLevel % spacingOfFlowers == 2)
                        {
                            int typet = (LCGint2(seed + growthLevel) % 2 + 2) % 2;
                            Flower flower = new Flower(this, drawPos, typet, seed + growthLevel);
                            childFlowers.Add(flower);
                        }
                        goto Success;
                    }
                }

            SuccessButStop:;
                lastDrawPos = drawPos;
                growthLevel = growthLevelToTest;

            Fail:;
                return false;

            Success:;
                growthLevel = growthLevelToTest;
                lastDrawPos = drawPos;
                return true;
            }
            public bool testPlantGrowth(bool forceGrowth)
            {
                if (forceGrowth || (!isStable && timeElapsed >= 0.2f + timeAtLastGrowth))
                {
                    isStable = true;

                    if (tryGrowth()) { isStable = false; }

                    List<Branch> branchesToGrow = new List<Branch>(childBranches);
                    for (int i = 0; i < branchesToGrow.Count; i++)
                    {
                        if (branchesToGrow[i].tryGrowth()) { isStable = false; }
                        foreach (Branch branch in branchesToGrow[i].childBranches)
                        {
                            branchesToGrow.Add(branch);
                        }
                        foreach (Flower flower in branchesToGrow[i].childFlowers)
                        {
                            if (flower.tryGrowth()) { isStable = false; }
                        }
                    }
                    foreach (Flower flower in childFlowers)
                    {
                        if (flower.tryGrowth()) { isStable = false; }
                    }
                    if (!forceGrowth) { makeBitmap(); }   // not to make bitmap when it's not needed (growing to max)
                    timeAtLastGrowth = timeElapsed;

                    return !isStable;
                }
                return false;
            }
            public void addPlantInChunks()
            {/*
                (int, int) chunkPos;
                Chunk chunkToTest;

                for (int i = bounds[0]; i <= bounds[1]; i++)
                {
                    for (int j = bounds[2]; j <= bounds[3]; j++)
                    {
                        if (i < screen.chunkX || i >= screen.chunkX + screen.chunkResolution || j < screen.chunkY || j >= screen.chunkY + screen.chunkResolution)
                        {
                            if (i == chunk.position.Item1 && j == chunk.position.Item2)
                            {
                                chunkPos = screen.findChunkAbsoluteIndex(i, j);
                                chunkToTest = screen.loadedChunks[chunkPos];
                                chunkToTest.plantList.Add(this);
                            }
                            else
                            {
                                chunkPos = screen.findChunkAbsoluteIndex(i, j);
                                chunkToTest = screen.loadedChunks[chunkPos];
                                chunkToTest.exteriorPlantList.Add(this);
                            }
                        }
                        else
                        {
                            if (screen.outOfBoundsPlants.ContainsKey((i, j)))
                            {
                                screen.outOfBoundsPlants[(i, j)].Add(this);
                            }
                            else
                            {
                                screen.outOfBoundsPlants.Add((i, j), new List<Plant> { this });
                            }
                        }
                    }
                }*/
            }
            public void placePlant()
            {
                int counto = 0;
                while (counto < 10000)
                {
                    int randX = rand.Next(32);
                    int randY = rand.Next(32);
                    posX += randX;
                    posY += randY;
                    (int x, int y) chunkPos = screen.findChunkAbsoluteIndex(posX, posY);
                    if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                    {
                        (int x, int y) tileIndex = GetChunkIndexFromTile(posX, posY);
                        if (chunkToTest.fillStates[tileIndex.x, tileIndex.y] <= 0)
                        {
                            chunkPos = screen.findChunkAbsoluteIndex(posX, posY - 1);
                            if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTesta))
                            {
                                tileIndex = GetChunkIndexFromTile(posX, posY - 1);
                                if (chunkToTesta.fillStates[tileIndex.x, tileIndex.y] > 0)
                                {
                                    attachPoint = 0;
                                    return;
                                }
                            }
                            chunkPos = screen.findChunkAbsoluteIndex(posX, posY + 1);
                            if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTesto))
                            {
                                tileIndex = GetChunkIndexFromTile(posX, posY + 1);
                                if (chunkToTesto.fillStates[tileIndex.x, tileIndex.y] > 0)
                                {
                                    attachPoint = 3;
                                    return;
                                }
                            }
                        }
                    }
                    posX -= randX;
                    posY -= randY;
                    counto += 1;
                }
                isDeadAndShouldDisappear = true;
            }
            public void testPlantPosition()
            {
                (int x, int y) chunkPos = screen.findChunkAbsoluteIndex(posX, posY);
                if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                {
                    (int x, int y) tileIndex = GetChunkIndexFromTile(posX, posY);
                    if (chunkToTest.fillStates[tileIndex.x, tileIndex.y] <= 0)
                    {
                        chunkPos = screen.findChunkAbsoluteIndex(posX, posY - 1);
                        if (attachPoint == 0 && screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTesto))
                        {
                            tileIndex = GetChunkIndexFromTile(posX, posY - 1);
                            if (chunkToTesto.fillStates[tileIndex.x, tileIndex.y] > 0)
                            {
                                return;
                            }
                        }
                        chunkPos = screen.findChunkAbsoluteIndex(posX, posY + 1);
                        if (attachPoint == 3 && screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTesta))
                        {
                            tileIndex = GetChunkIndexFromTile(posX, posY + 1);
                            if (chunkToTesta.fillStates[tileIndex.x, tileIndex.y] > 0)
                            {
                                return;
                            }
                        }
                    }
                }
                isDeadAndShouldDisappear = true;
            }
            public (List<Branch>, List<Flower>) returnAllBranchesAndFlowers()
            {
                List<Branch> branchesToTest = new List<Branch>();
                List<Flower> flowersToTest = new List<Flower>();
                foreach (Branch branch in childBranches)
                {
                    branchesToTest.Add(branch);
                }
                foreach (Flower flower in childFlowers)
                {
                    flowersToTest.Add(flower);
                }
                for (int i = 0; i < branchesToTest.Count; i++)
                {
                    foreach (Branch branch in branchesToTest[i].childBranches)
                    {
                        branchesToTest.Add(branch);
                    }
                    foreach (Flower flower in branchesToTest[i].childFlowers)
                    {
                        flowersToTest.Add(flower);
                    }
                }
                return (branchesToTest, flowersToTest);
            }
            public int testDig(int posToDigX, int posToDigY)
            {
                (List<Branch>, List<Flower>) returnedTuple = returnAllBranchesAndFlowers();
                List<Branch> branchesToTest = returnedTuple.Item1;
                List<Flower> flowersToTest = returnedTuple.Item2;

                (int x, int y) posToTest;
                foreach (Flower flower in flowersToTest)
                {
                    posToTest = (posToDigX - posX - flower.pos.x, posToDigY - posY - flower.pos.y);
                    if (flower.fillStates.TryGetValue((posToTest.x, posToTest.y), out int value))
                    {
                        if (value != 0)
                        {
                            flower.fillStates.Remove((posToTest.x, posToTest.y));
                            makeBitmap();
                            return value;
                        }
                    }
                }

                foreach (Branch branch in branchesToTest)
                {
                    posToTest = (posToDigX - posX - branch.pos.x, posToDigY - posY - branch.pos.y);
                    if (branch.fillStates.TryGetValue((posToTest.x, posToTest.y), out int value))
                    {
                        if (value != 0)
                        {
                            branch.fillStates.Remove((posToTest.x, posToTest.y));
                            makeBitmap();
                            return value;
                        }
                    }
                }

                {
                    posToTest = (posToDigX - posX, posToDigY - posY);
                    if (fillStates.TryGetValue((posToTest.x, posToTest.y), out int value))
                    {
                        if (value != 0)
                        {
                            fillStates.Remove((posToTest.x, posToTest.y));
                            makeBitmap();
                            return value;
                        }
                    }
                }

                return 0;
            }
            public (bool found, int x, int y) findPointOfInterestInPlant(int elementOfInterest)
            {
                (List<Branch>, List<Flower>) returnedTuple = returnAllBranchesAndFlowers();
                List<Branch> branchesToTest = shuffleList(returnedTuple.Item1);
                List<Flower> flowersToTest = shuffleList(returnedTuple.Item2);

                foreach (Flower flower in flowersToTest)
                {
                    if (flower.fillStates.ContainsValue(elementOfInterest))
                    {
                        foreach ((int x, int y) pos in flower.fillStates.Keys)
                        {
                            if (flower.fillStates[pos] == elementOfInterest)
                            {
                                return (true, posX + flower.pos.x + pos.x, posY + flower.pos.y + pos.y);
                            }
                        }
                    }
                }

                foreach (Branch branch in branchesToTest)
                {
                    if (branch.fillStates.ContainsValue(elementOfInterest))
                    {
                        foreach ((int x, int y) pos in branch.fillStates.Keys)
                        {
                            if (branch.fillStates[pos] == elementOfInterest)
                            {
                                return (true, posX + branch.pos.x + pos.x, posY + branch.pos.y + pos.y);
                            }
                        }
                    }
                }

                {
                    if (fillStates.ContainsValue(elementOfInterest))
                    {
                        foreach ((int x, int y) pos in fillStates.Keys)
                        {
                            if (fillStates[pos] == elementOfInterest)
                            {
                                return (true, posX + pos.x, posY + pos.y);
                            }
                        }
                    }
                }

                return (false, 0, 0);
            }
        }
    }
}
