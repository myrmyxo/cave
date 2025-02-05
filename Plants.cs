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
using static Cave.Traits;

namespace Cave
{
    public class Plants
    {
        public class Branch
        {
            public Plant motherPlant;
            public int seed;
            public int type;
            public int growthLevel;
            public int maxGrowthLevel;
            public (int x, int y) pos;
            public (int x, int y) lastDrawPos = (0, 0);
            public Dictionary<(int x, int y), (int type, int subType)> fillStates;

            public List<Branch> childBranches = new List<Branch>();
            public List<Flower> childFlowers = new List<Flower>();
            public Branch(Plant motherPlantToPut, BranchJson branchJson)
            {
                motherPlant = motherPlantToPut;
                pos = branchJson.pos;
                lastDrawPos = branchJson.lstGrPos;
                type = branchJson.type;
                seed = branchJson.seed;
                fillStates = arrayToFillstates(branchJson.fS);
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
                fillStates = new Dictionary<(int x, int y), (int type, int subType)>();
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
            public bool tryFill((int x, int y) testPos, (int type, int subType) typeToFill)
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

                if (motherPlant.type.type == 1) // woody tree
                {
                    if (motherPlant.type.subType == 0)
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

                        if (tryFill(drawPos, (1, 1)))
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
                    else if (motherPlant.type.subType == 1)
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

                        if (tryFill(drawPos, (11, 0)))
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
            public Dictionary<(int x, int y), (int type, int subType)> fillStates;
            public Flower(Plant motherPlantToPut, FlowerJson flowerJson)
            {
                motherPlant = motherPlantToPut;
                pos = flowerJson.pos;
                lastDrawPos = flowerJson.lstGrPos;
                type = flowerJson.type;
                seed = flowerJson.seed;
                fillStates = arrayToFillstates(flowerJson.fS);
                growthLevel = flowerJson.grLvl;
            }
            public Flower(Plant motherPlantToPut, (int x, int y) posToPut, int typeToPut, int seedToPut)
            {
                motherPlant = motherPlantToPut;
                pos = posToPut;
                type = typeToPut;
                seed = seedToPut;
                fillStates = new Dictionary<(int x, int y), (int type, int subType)>();
                growthLevel = -1;
            }
            public (int x, int y) absolutePos((int x, int y) position)
            {
                return (pos.x + position.x, pos.y + position.y);
            }
            public bool tryFill((int x, int y) testPos, (int type, int subType) typeToFill)
            {
                if (motherPlant.testIfPositionEmpty(absolutePos(testPos))) { fillStates[testPos] = typeToFill; return true; }
                return false;
            }
            public bool setMaxGrowthAndTestIfOverAndReinitFillStates(int growthLevelToTest, int maxGrowthToSet)
            {
                maxGrowthLevel = maxGrowthToSet;
                if (growthLevelToTest > maxGrowthLevel) { return true; }
                fillStates = new Dictionary<(int x, int y), (int type, int subType)>();
                return false;
            }
            public bool tryGrowth() // 0 = nothing, 1 = plantMatter, 2 = wood, 3 = aquaticPlantMatter, 4 = mushroomStem, 5 = mushroomCap, 6 = petal, 7 = flowerPollen
            {
                int growthLevelToTest = growthLevel + 1;
                if (growthLevelToTest == 0)
                {
                    goto Success;
                }

                if (motherPlant.type.type == 0)
                {
                    if (motherPlant.type.subType == 2)  // Tulip
                    {
                        if (setMaxGrowthAndTestIfOverAndReinitFillStates(growthLevelToTest, 5)) { goto Fail; }
                        if (growthLevelToTest == 1)
                        {
                            tryFill((0, 0), (2, 0));
                        }
                        else if (growthLevelToTest == 2)
                        {
                            tryFill((0, 0), (2, 0));
                            tryFill((0, 1), (2, 0));
                        }
                        else if (growthLevelToTest == 3)
                        {
                            tryFill((0, 0), (2, 0));
                            tryFill((0, 1), (2, 0));
                            tryFill((0, 2), (2, 0));
                        }
                        else if (growthLevelToTest == 4)
                        {
                            tryFill((0, 0), (2, 0));
                            tryFill((-1, 1), (2, 0));
                            tryFill((0, 1), (2, 0));
                            tryFill((1, 1), (2, 0));
                            tryFill((0, 2), (2, 0));
                        }
                        else
                        {
                            tryFill((0, 0), (2, 0));
                            tryFill((-1, 1), (2, 0));
                            tryFill((0, 1), (2, 0));
                            tryFill((1, 1), (2, 0));
                            tryFill((-1, 2), (2, 0));
                            tryFill((1, 2), (2, 0));
                            goto SuccessButStop;
                        }
                        goto Success;
                    }
                    if (motherPlant.type.subType == 3)                                   // Allium
                    {
                        if (setMaxGrowthAndTestIfOverAndReinitFillStates(growthLevelToTest, 4)) { goto Fail; }
                        if (growthLevelToTest == 1)
                        {
                            tryFill((0, 0), (2, 0));
                        }
                        else if (growthLevelToTest == 2)
                        {
                            tryFill((-1, 0), (2, 0));
                            tryFill((0, 0), (2, 0));
                            tryFill((1, 0), (2, 0));
                            tryFill((-1, 1), (2, 0));
                            tryFill((0, 1), (2, 0));
                            tryFill((1, 1), (2, 0));
                        }
                        else if (growthLevelToTest == 3)
                        {
                            tryFill((-1, 0), (2, 0));
                            tryFill((0, 0), (2, 0));
                            tryFill((1, 0), (2, 0));
                            tryFill((-1, 1), (2, 0));
                            tryFill((0, 1), (2, 0));
                            tryFill((1, 1), (2, 0));
                            tryFill((-1, 2), (2, 0));
                            tryFill((0, 2), (2, 0));
                            tryFill((1, 2), (2, 0));
                        }
                        else
                        {
                            tryFill((-1, 0), (2, 0));
                            tryFill((0, 0), (2, 0));
                            tryFill((1, 0), (2, 0));
                            tryFill((-2, 1), (2, 0));
                            tryFill((-1, 1), (2, 0));
                            tryFill((0, 1), (2, 0));
                            tryFill((1, 1), (2, 0));
                            tryFill((2, 1), (2, 0));
                            tryFill((-2, 2), (2, 0));
                            tryFill((-1, 2), (2, 0));
                            tryFill((0, 2), (2, 0));
                            tryFill((1, 2), (2, 0));
                            tryFill((2, 2), (2, 0));
                            tryFill((-1, 3), (2, 0));
                            tryFill((0, 3), (2, 0));
                            tryFill((1, 3), (2, 0));
                            goto SuccessButStop;
                        }
                        goto Success;
                    }

                    // else, case of candle ig
                    maxGrowthLevel = 1;
                    goto Fail;
                }
                if (motherPlant.type.type == 1) // tree
                {
                    if (motherPlant.type.subType == 1) // chandelierTree
                    {
                        if (setMaxGrowthAndTestIfOverAndReinitFillStates(growthLevelToTest, 5)) { goto Fail; }
                        if (growthLevelToTest == 1)
                        {
                            tryFill((0, 0), (11, 1)); // ...here... duh
                        }
                        else if (growthLevelToTest == 2)
                        {
                            tryFill((0, 0), (11, 1)); // ...here
                            tryFill((0, 1), (11, 0));
                        }
                        else if (growthLevelToTest == 3)
                        {
                            tryFill((0, 0), (11, 1)); // here
                            tryFill((0, 1), (11, 0));
                            tryFill((-1, 1), (11, 0));
                            tryFill((1, 1), (11, 0));
                        }
                        else if (growthLevelToTest == 4)
                        {
                            tryFill((0, 0), (11, 1)); // here
                            tryFill((0, 1), (11, 0));
                            tryFill((-1, 1), (11, 0));
                            tryFill((1, 1), (11, 0));
                            tryFill((0, 2), (11, 0));
                        }
                        else
                        {
                            tryFill((-1, -1), (11, 0));
                            tryFill((0, -1), (11, 0));
                            tryFill((1, -1), (11, 0));
                            tryFill((0, 0), (11, 1)); // here
                            tryFill((-2, 1), (11, 0));
                            tryFill((-1, 1), (11, 0));
                            tryFill((0, 1), (11, 0));
                            tryFill((1, 1), (11, 0));
                            tryFill((2, 1), (11, 0));
                            tryFill((0, 2), (11, 0));
                            goto SuccessButStop;
                        }
                        goto Success;
                    }
                    else
                    {
                        if (setMaxGrowthAndTestIfOverAndReinitFillStates(growthLevelToTest, 4)) { goto Fail; }

                        (int x, int y) drawPos = (0, 0);

                        if (growthLevelToTest == 1) // test so chandelier is one pixel big
                        {
                            tryFill(drawPos, (1, 0));
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
                                        tryFill(drawPos, (1, 0));
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


                                    tryFill(drawPos, (1, 0));
                                }
                            }
                        }
                        else
                        {
                            for (int i = -2; i < 3; i++)
                            {
                                for (int j = -2; j < 3; j++)
                                {
                                    drawPos = (i, j);

                                    if (!(Abs(i) == 2 && Abs(j) == 2))
                                    {
                                        tryFill(drawPos, (1, 0));
                                    }
                                }
                            }
                            goto SuccessButStop;
                        }
                        goto Success;
                    }
                }
                else if (motherPlant.type.type == 4) // mushy
                {
                    if (setMaxGrowthAndTestIfOverAndReinitFillStates(growthLevelToTest, motherPlant.maxGrowthLevel)) { goto Fail; }

                    tryFill((-1, 0), (3, 1));
                    tryFill((0, 0), (3, 1));
                    tryFill((1, 0), (3, 1));
                    if (growthLevelToTest > seed % 8 + 2)
                    {
                        tryFill((-2, 0), (3, 1));
                        tryFill((2, 0), (3, 1));
                        if (growthLevelToTest > seed % 8 + 3 + seed % 2)
                        {
                            tryFill((-1, 1), (3, 1));
                            tryFill((0, 1), (3, 1));
                            tryFill((1, 1), (3, 1));
                        }
                    }
                    goto Success;
                }
                else if (motherPlant.type.type == 5) // vine
                {
                    setMaxGrowthAndTestIfOverAndReinitFillStates(growthLevelToTest, 3);
                    if (type == 0)
                    {
                        if (growthLevelToTest == 1)
                        {
                            tryFill((0, 0), (2, 0));
                        }
                        else if (growthLevelToTest == 2)
                        {
                            tryFill((0, 0), (2, 0));
                            tryFill((1, 0), (2, 0));
                            tryFill((-1, 0), (2, 0));
                            tryFill((0, 1), (2, 0));
                            tryFill((0, -1), (2, 0));
                        }
                        else
                        {
                            tryFill((0, 0), (2, 1));
                            tryFill((1, 0), (2, 0));
                            tryFill((-1, 0), (2, 0));
                            tryFill((0, 1), (2, 0));
                            tryFill((0, -1), (2, 0));
                            goto SuccessButStop;
                        }
                        goto Success;
                    }
                    else
                    {
                        if (growthLevelToTest == 1)
                        {
                            tryFill((0, 0), (2, 0));
                        }
                        else if (growthLevelToTest == 2)
                        {
                            tryFill((0, 0), (2, 0));
                            tryFill((1, 1), (2, 0));
                            tryFill((-1, 1), (2, 0));
                            tryFill((1, -1), (2, 0));
                            tryFill((-1, -1), (2, 0));
                        }
                        else
                        {
                            tryFill((0, 0), (2, 1));
                            tryFill((1, 1), (2, 0));
                            tryFill((-1, 1), (2, 0));
                            tryFill((1, -1), (2, 0));
                            tryFill((-1, -1), (2, 0));
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
            public (int type, int subType) type;
            public PlantTraits traits;
            public int state;
            public int growthLevel;
            public int maxGrowthLevel;
            public int posX = 0;
            public int posY = 0;
            public int attachPoint; // 0 ground, 1 leftWall, 2 rightWall, 3 ceiling
            public Dictionary<(int type, int subType), Color> colorDict;

            public float timeAtLastGrowth = timeElapsed;

            public Bitmap bitmap = new Bitmap(1, 1);
            public Bitmap secondaryBitmap = new Bitmap(1, 1);
            public Dictionary<(int x, int y), (int type, int subType)> fillStates = new Dictionary<(int x, int y), (int type, int subType)>();
            public List<(int x, int y)> lightPositions = new List<(int x, int y)>();
            public Color lightColor = Color.Black;
            public (int type, int subType) lightMaterial = (0, 0);
            public int[] posOffset = new int[3];
            public int[] bounds = new int[4];
            public (int x, int y) lastDrawPos = (0, 0);

            public List<Branch> childBranches = new List<Branch>();
            public List<Flower> childFlowers = new List<Flower>();

            public Dictionary<(int x, int y), bool> chunkPresence = new Dictionary<(int x, int y), bool>();

            public bool isDeadAndShouldDisappear = false;
            public bool isStable = false;
            public Plant(Screens.Screen screenToPut, PlantJson plantJson)
            {
                screen = screenToPut;
                posX = plantJson.pos.Item1;
                posY = plantJson.pos.Item2;
                lastDrawPos = plantJson.lstGrPos;
                transformPlant(plantJson.type);
                seed = plantJson.seed;
                id = plantJson.id;
                growthLevel = plantJson.grLvl;
                timeAtLastGrowth = plantJson.lastGr;
                fillStates = arrayToFillstates(plantJson.fS);
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
            public Plant(Chunk chunkToPut, (int x, int y) posToPut, (int type, int subType) typeToPut)
            {
                posX = posToPut.x;
                posY = posToPut.y;
                screen = chunkToPut.screen;
                seed = LCGint1(Abs((int)chunkToPut.chunkSeed));
                seed = Abs(seed + rand.Next(100000)); // TO CHANGE TOCHANGE cuz false randommmm
                transformPlant(typeToPut);
                id = currentPlantId;
                growthLevel = -1;
                if (isDeadAndShouldDisappear) { return; }
                findColors();
                tryGrowToMaximum();
                makeBitmap();
                timeAtLastGrowth = timeElapsed;

                currentPlantId++;
            }
            public Plant(Screens.Screen screenToPut, (int, int) positionToPut, (int type, int subType) typeToPut)
            {
                screen = screenToPut;
                posX = positionToPut.Item1;
                posY = positionToPut.Item2;
                transformPlant(typeToPut);
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
            public bool tryFill((int x, int y) testPos, (int type, int subType) typeToFill)
            {
                if (testIfPositionEmpty(testPos)) { fillStates[testPos] = typeToFill; return true; }
                return false;
            }
            public void findColors()
            {
                colorDict = new Dictionary<(int type, int subType), Color>();
                int seedo = LCGint1(seed);
                int hueVar = (int)(seedo % 101) - 50;
                seedo = LCGint1(seed);
                int shadeVar = (int)(seedo % 61) - 30;
                if (type.type == 0) // normal
                {
                    if (type.subType == 1)
                    {
                        shadeVar = (int)(shadeVar * 0.3f);
                        colorDict.Add((12, 0), Color.FromArgb(210 - shadeVar, 210 - shadeVar, 200 - shadeVar)); // wax
                        colorDict.Add((11, 1), Color.FromArgb(200 - shadeVar, 120 - shadeVar, 40 - shadeVar)); // lightBulb (used for the color of the light only)
                    }
                    else if (type.subType == 2)
                    {
                        colorDict.Add((1, 0), Color.FromArgb(50 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                        colorDict.Add((2, 0), Color.FromArgb(220 - shadeVar, 110 - hueVar - shadeVar, 130 + hueVar - shadeVar));
                    }
                    else if (type.subType == 3)
                    {
                        colorDict.Add((1, 0), Color.FromArgb(50 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                        colorDict.Add((2, 0), Color.FromArgb(140 - shadeVar, 80 - hueVar - shadeVar, 220 - shadeVar));
                    }
                    else
                    {
                        colorDict.Add((1, 0), Color.FromArgb(50 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                    }
                    return;
                }
                else if (type.type == 1) // woody
                {
                    if (type.subType == 1) // chandelier
                    {
                        shadeVar = (int)(shadeVar*0.3f);
                        colorDict.Add((11, 0), Color.FromArgb(40 - shadeVar, 40 - shadeVar, 60 - shadeVar));
                        colorDict.Add((11, 1), Color.FromArgb(230 - shadeVar, 230 - shadeVar, 120 - shadeVar));
                    }
                    else // normal
                    {
                        colorDict.Add((1, 0), Color.FromArgb(50 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                        colorDict.Add((1, 1), Color.FromArgb(140 + (int)(hueVar * 0.3f) - shadeVar, 140 - (int)(hueVar * 0.3f) - shadeVar, 50 - shadeVar));
                        colorDict.Add((2, 0), Color.FromArgb(170 - shadeVar, 120 - hueVar - shadeVar, 150 - shadeVar));
                        colorDict.Add((2, 1), Color.FromArgb(170 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                    }
                }
                else if (type.type == 2) // kelp
                {
                    colorDict.Add((1, 2), Color.FromArgb(30 - shadeVar, 90 - shadeVar + hueVar, 140 - shadeVar - hueVar));
                }
                else if (type.type == 3) // obsidian
                {
                    colorDict.Add((1, 0), Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar));
                }
                else if (type.type == 4) // mushroom
                {
                    if (type.subType == 0)
                    {
                        colorDict.Add((3, 0), Color.FromArgb(180 + shadeVar, 160 + shadeVar, 165 + shadeVar));
                        colorDict.Add((3, 1), Color.FromArgb(140 - shadeVar, 120 + hueVar, 170 - hueVar));
                    }
                    else if (type.subType == 1)
                    {
                        hueVar = (int)(hueVar * 0.3f);
                        colorDict.Add((3, 2), Color.FromArgb(50 - shadeVar, 50 - shadeVar, 100 - shadeVar));
                    }
                }
                else if (type.type == 5) // vine
                {
                    if (type.subType == 0)
                    {
                        colorDict.Add((1, 0), Color.FromArgb(50 - shadeVar, 120 - hueVar - shadeVar, 50 - shadeVar));
                        colorDict.Add((2, 0), Color.FromArgb(170 - shadeVar, 120 - hueVar - shadeVar, 150 - shadeVar));
                        colorDict.Add((2, 1), Color.FromArgb(170 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                    }
                    else if (type.subType == 1)
                    {
                        colorDict.Add((1, 0), Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar));
                        colorDict.Add((2, 0), Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar));
                        colorDict.Add((2, 1), Color.FromArgb(220 + shadeVar, 220 + shadeVar, 220 + shadeVar));
                    }
                }
                else if (type.type == 2) // kelp
                {
                    colorDict.Add((1, 2), Color.FromArgb(30 - shadeVar, 90 - shadeVar + hueVar, 140 - shadeVar - hueVar));
                }
            }
            public (int x, int y) getRealPos((int x, int y) pos) { return (posX + pos.x, posY + pos.y); }
            public (int x, int y) getRelativePos((int x, int y) pos) { return (pos.x - posX, pos.y - posY); }
            public bool testIfPositionEmpty((int x, int y) mod) // Improve for water (returns true for water, should only do that for plants)
            {
                (int x, int y) pixelPos = getRealPos(mod);
                (int x, int y) pixelTileIndex = PosMod(pixelPos);

                Chunk chunkToTest = screen.getChunkFromPixelPos(pixelPos);
                if (chunkToTest.fillStates[pixelTileIndex.x, pixelTileIndex.y].type > 0) { return false; }
                return true;
            }
            public void makeBitmap()
            {
                (List<Branch>, List<Flower>) returnedTuple = returnAllBranchesAndFlowers();
                List<Branch> branchList = returnedTuple.Item1;
                List<Flower> flowerList = returnedTuple.Item2;
                Dictionary<(int x, int y), (int type, int subType)> fillDict = returnFullPlantFillDict(branchList, flowerList);

                int minX = 0;
                int maxX = 0;
                int minY = 0;
                int maxY = 0;

                foreach ((int x, int y) drawPos in fillDict.Keys)
                {
                    if (drawPos.x < minX) { minX = drawPos.x; }
                    else if (drawPos.x > maxX) { maxX = drawPos.x; }
                    if (drawPos.y < minY) { minY = drawPos.y; }
                    else if (drawPos.y > maxY) { maxY = drawPos.y; }
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

                (int, int) tupelo = ChunkIdx(one, three);
                (int, int) tupela = ChunkIdx(two, four);

                bounds[0] = tupelo.Item1;
                bounds[1] = tupelo.Item2;
                bounds[2] = tupela.Item1;
                bounds[3] = tupela.Item2;

                findChunkPresence(fillDict);
                findLightPositions(branchList, flowerList);

                testDeath(fillDict);
            }
            public void findChunkPresence(Dictionary<(int x, int y), (int type, int subType)> fillDict)
            {
                chunkPresence = new Dictionary<(int x, int y), bool>();
                if (fillDict.Count == 0) { chunkPresence[ChunkIdx(posX, posY)] = true; }    // Needed if plant at stage 0 so it doesn't disappear forever lmfao !
                foreach ((int x, int y) posToTest in fillDict.Keys)
                {
                    chunkPresence[ChunkIdx(getRealPos(posToTest))] = true;
                }
            }
            public void findLightPositions(List<Branch> branchList, List<Flower> flowerList)
            {
                lightPositions = new List<(int x, int y)>();

                if (type == (0, 1))
                {
                    lightMaterial = (11, 1);
                    if (childFlowers.Count > 0)
                    {
                        Flower fireFlower = childFlowers[0];
                        lightPositions.Add((fireFlower.pos.x + posX, fireFlower.pos.y + posY + 2)); // !!!!!!!!!! the +1 !!!
                    }
                }
                else if (type == (1, 1)) { lightMaterial = (11, 1); }
                else { lightMaterial = (0, 0); }

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
            public void tryGrowToMaximum()
            {
                isStable = false;
                int maxIterations = -1;
                if (type == (4, 1)) { maxIterations = 2 + rand.Next(100); }
                int i = 0;
                while (!isStable && (maxIterations == -1 || i < maxIterations))
                {
                    isStable = !testPlantGrowth(true);
                    i++;
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
            public bool tryGrowth() // 0 = nothing, 1 = plantMatter, 2 = wood, 3 = aquaticPlantMatter, 4 = mushroomStem, 5 = mushroomCap, 6 = petal, 7 = flowerPollen... NO LOL ITS NOT
            {
                int seedo = seed;
                int growthLevelToTest = growthLevel + 1;
                (int x, int y) drawPos = lastDrawPos;

                if (growthLevelToTest == 0)
                {
                    if (type == (4, 1))
                    {

                    }
                    else if (type.type == 5 || type == (3, 1))
                    {
                        drawPos = (0, 1);
                    }
                    else
                    {
                        drawPos = (0, -1);
                    }
                    goto Success;
                }

                if (type.type == 0) // normal plant
                {
                    if (!maxGrowthDict.ContainsKey(type)) { goto Fail; }
                    (int min, int range) maxGrowthTuple = maxGrowthDict[type];
                    if (growthLevelToTest > maxGrowthTuple.min + seed % maxGrowthTuple.range) { goto Fail; }

                    drawPos = (lastDrawPos.x, lastDrawPos.y + 1);
                    if (type.subType == 1 || type.subType == 2 || type.subType == 3) // straight growing flowers
                    {
                        if (testIfPositionEmpty((drawPos.x, drawPos.y+2)))
                        {
                            (int type, int subType) typeToFill = (1, 0);
                            if (type.subType == 1) { typeToFill = (12, 0); }

                            if (tryFill(drawPos, typeToFill))
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

                        if (tryFill(drawPos, (1, 0)))
                        {
                            goto Success;
                        }
                    }
                }
                else if (type.type == 1) // tree
                {
                    maxGrowthLevel = 10 + seed % 40;
                    if (growthLevelToTest > maxGrowthLevel) { goto Fail; }
                    if (growthLevelToTest == 0) { }

                    drawPos = (lastDrawPos.x, lastDrawPos.y + 1);
                    (int type, int subType) typeToFill = (1, 1);

                    int spacing = 3 + seed % 3;
                    seedo = LCGint1(seed + growthLevelToTest * (seed % 7 + 1));
                    if (type.subType != 1)
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
                    else { spacing += 2; typeToFill = (11, 0); }

                    if (tryFill(drawPos, typeToFill))
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
                else if (type.type == 2) // kelp
                {
                    if (type.subType == 0)
                    {
                        maxGrowthLevel = 1 + seed % 10;
                        if (growthLevelToTest > maxGrowthLevel) { goto Fail; }

                        int multo = 2 * (seed % 2) - 1;
                        drawPos = (((growthLevelToTest - 1) % 2) * multo, growthLevelToTest - 1);

                        if (tryFill(drawPos, (1, 2)))
                        {
                            goto Success;
                        }
                    }
                    else if (type.subType == 1)
                    {
                        maxGrowthLevel = 1 + seed % 10;
                        if (growthLevelToTest > maxGrowthLevel) { goto Fail; }

                        int multo = 2 * (seed % 2) - 1;
                        drawPos = (((growthLevelToTest - 1) % 2) * multo, -(growthLevelToTest - 1));

                        if (tryFill(drawPos, (1, 2)))
                        {
                            goto Success;
                        }
                    }
                }
                else if (type.type == 3) // obsidian plant
                {
                    maxGrowthLevel = 1 + seed % 3;
                    if (growthLevelToTest > maxGrowthLevel) { goto Fail; }

                    drawPos = (lastDrawPos.x, lastDrawPos.y + 1);

                    if (tryFill(drawPos, (1, 0)))
                    {
                        goto Success;
                    }
                }
                else if (type.type == 4) // Fungi
                {
                    if (type.subType == 0) // Mushroom
                    {
                        maxGrowthLevel = 1 + seed % 7;
                        if (growthLevelToTest > maxGrowthLevel) { goto Fail; }

                        drawPos = (lastDrawPos.x, lastDrawPos.y + 1);

                        if (tryFill(drawPos, (3, 0)))
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
                    else if (type.subType == 1) // Mold
                    {
                        bool success = false;
                        int maxRep = 1 + (int)(fillStates.Count * 0.2f);
                        int count = 0;
                        while (count < maxRep)
                        {
                            if (fillStates.Count == 0) { drawPos = lastDrawPos; }
                            else
                            {
                                drawPos = fillStates.Keys.ToArray()[rand.Next(fillStates.Count)];
                                int rando = rand.Next(5);
                                if (rando < 4) { drawPos = (drawPos.x + neighbourArray[rando].Item1, drawPos.y + neighbourArray[rando].Item2); }
                            }
                            if (tryMoldConversion(drawPos)) { success = true; }
                            if (!fillStates.ContainsKey(drawPos) && tryFill(drawPos, (3, 2))) { success = true; }
                            count++;
                        }
                        if (success) { goto Success; }
                        goto FailButContinue;
                    }
                }
                else if (type.type == 5) // vine
                {
                    maxGrowthLevel = 4 + seed % 60;
                    if (growthLevelToTest > maxGrowthLevel) { goto Fail; }

                    int multo = 2 * (seed % 2) - 1;
                    drawPos = (((int)((growthLevelToTest - 1) * 0.5f) % 2) * multo, -(growthLevelToTest - 1));

                    if (tryFill(drawPos, (1, 0)))
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
            FailButContinue:;
                return true;
            }
            public bool tryMoldConversion((int x, int y) pos)
            {
                if (!fillStates.ContainsKey(pos)) { return false; }
                int moldyTiles = 0;
                int fullTiles = 0;
                (int x, int y) posToTest;
                foreach ((int x, int y) mod in neighbourArray)
                {
                    posToTest = (pos.x + mod.x, pos.y + mod.y);
                    if (screen.getTileContent(getRealPos(posToTest)).type != 0) { fullTiles += 1; }
                    if (fillStates.ContainsKey(posToTest)) { moldyTiles += 1; }
                }
                if (moldyTiles + fullTiles >= 4)
                {
                    screen.setTileContent(getRealPos(pos), (5, 0));
                    fillStates.Remove(pos);
                    return true;
                }
                return false;
            }
            public bool tryToMakeParticle()
            {
                if (type == (4, 1) && fillStates.Count > 0)
                {
                    if (rand.Next(10) == 0)
                    {
                        (int x, int y) pos = getRealPos(fillStates.Keys.ToArray()[rand.Next(fillStates.Count)]);
                        new Particle(screen, pos, pos, (5, 3, 2), id);  // mold particle
                        return true;
                    }
                }
                return false;
            }
            public bool testPlantGrowth(bool forceGrowth)
            {
                tryToMakeParticle();
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
            public void testPlantPosition()
            {
                if (screen.getChunkFromPixelPos((posX, posY)).fillStates[PosMod(posX), PosMod(posY)].type > 0) { goto Fail; } // Full tile -> Fail
                int mod = traits.isCeiling ? 1 : -1;
                if (screen.getChunkFromPixelPos((posX, posY + mod)).fillStates[PosMod(posX), PosMod(posY + mod)].type > 0) { return; } // tile under full -> Success
            Fail:;
                isDeadAndShouldDisappear = true;
            }
            public Dictionary<(int x, int y), (int type, int subType)> returnFullPlantFillDict(List<Branch> branchList = null, List<Flower> flowerList = null)
            {
                if (branchList == null)
                {
                    (List<Branch>, List<Flower>) returnedTuple = returnAllBranchesAndFlowers();
                    branchList = returnedTuple.Item1;
                    flowerList = returnedTuple.Item2;
                }
                Dictionary<(int x, int y), (int type, int subType)> fillDict = new Dictionary<(int x, int y), (int type, int subType)>(fillStates);
                foreach (Branch branch in branchList)
                {
                    foreach ((int x, int y) keyo in branch.fillStates.Keys) { fillDict[(keyo.x + branch.pos.x, keyo.y + branch.pos.y)] = branch.fillStates[keyo]; }
                }
                foreach (Flower flower in flowerList)
                {
                    foreach ((int x, int y) keyo in flower.fillStates.Keys) { fillDict[(keyo.x + flower.pos.x, keyo.y + flower.pos.y)] = flower.fillStates[keyo]; }
                }
                return fillDict;
            }
            public (List<Branch>, List<Flower>) returnAllBranchesAndFlowers()
            {
                List<Branch> branchesToTest = new List<Branch>();
                List<Flower> flowersToTest = new List<Flower>();
                foreach (Branch branch in childBranches) { branchesToTest.Add(branch); }
                foreach (Flower flower in childFlowers) { flowersToTest.Add(flower); }
                for (int i = 0; i < branchesToTest.Count; i++)
                {
                    foreach (Branch branch in branchesToTest[i].childBranches) { branchesToTest.Add(branch); }
                    foreach (Flower flower in branchesToTest[i].childFlowers) { flowersToTest.Add(flower); }
                }
                return (branchesToTest, flowersToTest);
            }
            public (int type, int subType) tryDig((int x, int y) posToDig, (int type, int subType, int typeOfElement)? currentItem = null, (int type, int subType)? targetMaterialNullable = null, bool toolRestrictions = true)
            {
                (List<Branch>, List<Flower>) returnedTuple = returnAllBranchesAndFlowers();
                List<Branch> branchesToTest = returnedTuple.Item1;
                List<Flower> flowersToTest = returnedTuple.Item2;

                (int x, int y) posToTest;
                foreach (Flower flower in flowersToTest)
                {
                    posToTest = (posToDig.x - posX - flower.pos.x, posToDig.y - posY - flower.pos.y);
                    if (flower.fillStates.TryGetValue((posToTest.x, posToTest.y), out (int type, int subType) value))
                    {
                        if ((targetMaterialNullable is null ? true : targetMaterialNullable == value) && (!toolRestrictions || !materialGatheringToolRequirement.ContainsKey(value) || materialGatheringToolRequirement[value] == currentItem))
                        {
                            flower.fillStates.Remove((posToTest.x, posToTest.y));
                            screen.plantsToMakeBitmapsOf[id] = this;
                            return value;
                        }
                    }
                }

                foreach (Branch branch in branchesToTest)
                {
                    posToTest = (posToDig.x - posX - branch.pos.x, posToDig.y - posY - branch.pos.y);
                    if (branch.fillStates.TryGetValue((posToTest.x, posToTest.y), out (int type, int subType) value))
                    {
                        if ((targetMaterialNullable is null ? true : targetMaterialNullable == value) && (!toolRestrictions || !materialGatheringToolRequirement.ContainsKey(value) || materialGatheringToolRequirement[value] == currentItem))
                        {
                            branch.fillStates.Remove((posToTest.x, posToTest.y));
                            screen.plantsToMakeBitmapsOf[id] = this;
                            return value;
                        }
                    }
                }

                posToTest = (posToDig.x - posX, posToDig.y - posY);
                if (fillStates.TryGetValue((posToTest.x, posToTest.y), out (int type, int subType) value2))
                {
                    if ((targetMaterialNullable is null ? true : targetMaterialNullable == value2) && (!toolRestrictions || !materialGatheringToolRequirement.ContainsKey(value2) || materialGatheringToolRequirement[value2] == currentItem))
                    {
                        fillStates.Remove((posToTest.x, posToTest.y));
                        screen.plantsToMakeBitmapsOf[id] = this;
                        return value2;
                    }
                }
                
                return (0, 0);
            }
            public (bool found, int x, int y) findPointOfInterestInPlant((int type, int subType) elementOfInterest)
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

            public void transformPlant((int type, int subType) newType)
            {
                type = newType;
                traits = plantTraitsDict.ContainsKey(type) ? plantTraitsDict[type] : plantTraitsDict[(-1, 0)];
                if (traits.isCeiling) { attachPoint = 1; }

                findColors();
            }
            public void testDeath(Dictionary<(int x, int y), (int type, int subType)> fillDict)
            {
                if (growthLevel > 0 && fillDict.Count == 0)
                {
                    dieAndDrop();
                }
            }

            public void dieAndDrop()
            {
                screen.plantsToRemove[id] = this;
            }
        }
    }
}
