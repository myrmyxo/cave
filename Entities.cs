using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

using static Cave.Form1;
using static Cave.Form1.Globals;
using static Cave.MathF;
using static Cave.Sprites;
using static Cave.Structures;
using static Cave.Entities;
using static Cave.Files;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;

namespace Cave
{
    public class Entities
    {
        public class Entity
        {
            public Form1.Screen screen;

            public int seed;
            public int type; // 0 = fairy , 1 = frog , 2 = fish, 3 = hornet
            public int subType;
            public int state; // 0 = idle I guess idk
            public float realPosX = 0;
            public float realPosY = 0;
            public int posX = 0;
            public int posY = 0;
            public float speedX = 0;
            public float speedY = 0;
            public Color color;

            public Dictionary<(int index, int subType, int typeOfElement), int> inventoryQuantities;
            public List<(int index, int subType, int typeOfElement)> inventoryElements;
            public int elementsPossessed = 0;
            public int inventoryCursor = 0;

            public float timeAtLastDig = -9999;
            public float timeAtLastPlace = -9999;

            public bool isDeadAndShouldDisappear = false;

            public Entity(Chunk chunk, EntityJson entityJson)
            {
                screen = chunk.screen;
                realPosX = entityJson.pos.Item1;
                posX = (int)realPosX;
                realPosY = entityJson.pos.Item2;
                posY = (int)realPosY;
                type = entityJson.type.Item1;
                subType = entityJson.type.Item2;
                seed = entityJson.seed;
                inventoryElements = entityJson.invE;
                inventoryQuantities = entityJson.invQ;
                state = entityJson.state;
                color = findColor();
            }
            public Entity(Chunk chunk)
            {
                screen = chunk.screen;
                seed = LCGint1(Abs((int)chunk.chunkSeed));
                placeEntity(chunk);
                findType(chunk);
                color = findColor();
                if (type == 3) { initializeInventory(); }
            }
            public Entity(Chunk chunk, (int, int) positionToPut, int typeToPut, int subTypeToPut)
            {
                screen = chunk.screen;
                posX = positionToPut.Item1;
                realPosX = posX;
                posY = positionToPut.Item2;
                realPosY = posY;
                type = typeToPut;
                subType = subTypeToPut;
                seed = rand.Next(1000000000); //                               FALSE RANDOM NOT SEEDED ARGHHEHEEEE
                if (type == 3) { initializeInventory(); }
                color = findColor();
            }
            public void findType(Chunk chunk)
            {
                int biome = chunk.biomeIndex[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16][0].Item1;
                if (chunk.fillStates[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16] < 0)
                {
                    type = 2;
                    subType = 0;
                }
                else if (biome == 5)
                {
                    type = 0;
                    subType = 0;
                }
                else if (biome == 6)
                {
                    type = 0;
                    subType = 1;
                }
                else if (biome == 7)
                {
                    type = 0;
                    subType = 2;
                }
                else
                {
                    type = 1;
                    subType = 0;
                }
            }
            public Color findColor()
            {
                int hueVar = seed%101 - 50;
                int shadeVar = seed%61 - 30;
                if (type == 0)
                {
                    if (subType == 1)
                    {
                        return Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar);
                    }
                    if (subType == 2)
                    {
                        hueVar = Abs((int)(hueVar * 0.4f));
                        shadeVar = Abs(shadeVar);
                        return Color.FromArgb(255 - hueVar - shadeVar, 255 - hueVar - shadeVar, 255 - shadeVar);
                    }
                    return Color.FromArgb(130 + hueVar + shadeVar, 130 - hueVar + shadeVar, 210 + shadeVar);
                }
                if (type == 1)
                {
                    return Color.FromArgb(90 + hueVar + shadeVar, 210 + shadeVar, 110 - hueVar + shadeVar);
                }
                if (type == 2)
                {
                    return Color.FromArgb(190 + shadeVar, 80 - hueVar + shadeVar, 80 + hueVar + shadeVar);
                }
                if (type == 3)
                {
                    return Color.FromArgb(190 + (int)(hueVar * 0.2f) + shadeVar, 190 - (int)(hueVar * 0.2f) + shadeVar, 80 + shadeVar);
                }
                return Color.Red;
            }
            public (int, int) findIntPos(float positionX, float positionY)
            {
                return ((int)Floor(positionX, 1), (int)Floor(positionY, 1));
            }
            public void placeEntity(Chunk chunk)
            {
                int counto = 0;
                while (counto < 10000)
                {
                    int randX = rand.Next(16);
                    int randY = rand.Next(16);
                    if (chunk.fillStates[randX, randY] <= 0)
                    {
                        posX = (int)chunk.position.Item1 * 16 + randX;
                        realPosX = posX;
                        posY = (int)chunk.position.Item2 * 16 + randY;
                        realPosY = posY;
                        return;
                    }
                    counto += 1;
                }
                isDeadAndShouldDisappear = true;
            }
            public void testDigPlace()
            {
                if (type == 3)
                {
                    if (timeAtLastDig + 0.5f + rand.NextDouble() * 3 < timeElapsed)
                    {
                        int direction = rand.Next(4);
                        int digorplace = elementsPossessed + rand.Next(5) - 5;
                        if (digorplace < 0)
                        {
                            Dig(posX + directionArray[direction].Item1, posY + directionArray[direction].Item2);
                        }
                        else
                        {
                            Place(posX + directionArray[direction].Item1, posY + directionArray[direction].Item2);
                        }
                    }

                    timeAtLastPlace = timeAtLastDig;
                }
            }
            public void moveEntity()
            {
                (int, int) chunkPos;
                if (type == 0 || type == 3)
                {
                    if (state == 0) // idle
                    {
                        if (Abs(speedX) > 0)
                        {
                            if (Abs(speedX) > 0.5f)
                            {
                                speedX = speedX - Sign(speedX) * 0.5f;
                            }
                            else
                            {
                                speedX = 0;
                            }
                        }
                        if (Abs(speedY) > 0)
                        {
                            if (Abs(speedY) > 0.5f)
                            {
                                speedY = speedY - Sign(speedY) * 0.5f;
                            }
                            else
                            {
                                speedY = 0;
                            }
                        }
                        if (rand.Next(100) == 0)
                        {
                            state = 1;
                        }
                    }
                    else if (state == 1) // moving in the air
                    {
                        speedX += (float)(rand.NextDouble()) - 0.5f;
                        speedY += (float)(rand.NextDouble()) - 0.5f;
                        if (rand.Next(1000) < 3)
                        {
                            state = 0;
                        }
                    }
                    else if (state == 2) // moving in the water
                    {
                        if (Abs(speedX) > 0)
                        {
                            if (Abs(speedX) > 0.5f)
                            {
                                speedX = speedX - Sign(speedX) * 0.5f;
                            }
                            else
                            {
                                speedX = 0;
                            }
                        }
                        if (Abs(speedY) > 0)
                        {
                            if (Abs(speedY) > 0.5f)
                            {
                                speedY = speedY - Sign(speedY) * 0.5f;
                            }
                            else
                            {
                                speedY = 0;
                            }
                        }
                        speedX += (float)(rand.NextDouble()) - 0.5f;
                        speedY += (float)(rand.NextDouble());
                    }
                    else { state = 0; }
                }
                else if (type == 1)
                {
                    speedY -= 0.5f;
                    chunkPos = screen.findChunkAbsoluteIndex(posX, posY - 1); // +1 cause coordinates are inverted lol
                    if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                    {
                        if (chunkToTest.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 31) % 16] > 0)
                        {
                            speedX = Sign(speedX) * (Max(0, Abs(speedX) * (0.85f) - 0.2f));
                            if (rand.NextDouble() > 0.05f)
                            {
                                speedX += (float)(rand.NextDouble()) * 5 - 2.5f;
                                speedY += (float)(rand.NextDouble()) * 2.5f;
                            }
                        }
                    }
                }
                else if (type == 2) // fish
                {
                    chunkPos = screen.findChunkAbsoluteIndex(posX, posY); // +1 cause coordinates are inverted lol


                    if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTesta))
                    {
                        if (chunkToTesta.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 32) % 16] < 0)
                        {
                            if (state >= 2)
                            {
                                state = 1;
                            }
                        }
                        else
                        {
                            state = 2;
                        }
                    }

                    if (state == 0) // idle
                    {
                        if (Abs(speedX) > 0)
                        {
                            if (Abs(speedX) > 0.5f)
                            {
                                speedX = speedX - Sign(speedX) * 0.5f;
                            }
                            else
                            {
                                speedX = 0;
                            }
                        }
                        if (Abs(speedY) > 0)
                        {
                            if (Abs(speedY) > 0.5f)
                            {
                                speedY = speedY - Sign(speedY) * 0.5f;
                            }
                            else
                            {
                                speedY = 0;
                            }
                        }
                        if (rand.Next(50) == 0)
                        {
                            state = 1;
                        }
                    }
                    else if (state == 1) // moving in water
                    {
                        speedX = speedX * 0.9f - Sign(speedX) * 0.12f;
                        speedY = speedY * 0.9f - Sign(speedY) * 0.12f;

                        speedX += (float)(rand.NextDouble()) - 0.5f;
                        speedY += (float)(rand.NextDouble()) - 0.5f;
                        if (rand.Next(100) == 0)
                        {
                            speedX = (float)(rand.NextDouble()) * 3 - 1.5f;
                            speedY = (float)(rand.NextDouble()) * 3 - 1.5f;
                        }
                        else if (rand.Next(1000) == 0)
                        {
                            state = 0;
                        }
                    }
                    else if (state == 2) // outside water
                    {
                        speedY -= 0.5f;
                        chunkPos = screen.findChunkAbsoluteIndex(posX, posY - 1); // +1 cause coordinates are inverted lol (no)

                        if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                        {
                            if (chunkToTest.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 31) % 16] > 0)
                            {
                                speedX = speedX * 0.9f - Sign(speedX) * 0.12f;
                                if (rand.NextDouble() > 0.05f)
                                {
                                    speedX += (float)(rand.NextDouble()) * 2 - 1;
                                    speedY = (float)(rand.NextDouble()) * 1.5f - 0.5f;
                                }
                            }
                        }
                    }
                }
                if (type != 2)
                {
                    chunkPos = screen.findChunkAbsoluteIndex(posX, posY);
                    if (screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                    {
                        if (chunkToTest.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 31) % 16] < 0)
                        {
                            speedX = speedX * 0.85f - Sign(speedX) * 0.15f;
                            speedY = speedY * 0.85f - Sign(speedY) * 0.15f;
                        }
                    }
                }
                float toMoveX = speedX;
                float toMoveY = speedY;

                while (Abs(toMoveY) > 0)
                {
                    chunkPos = screen.findChunkAbsoluteIndex(posX, posY + (int)Sign(toMoveY));
                    if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                    {
                        posY += (int)Sign(toMoveY);
                        saveEntity(this);
                        screen.entitesToRemove.Add(this);
                        return;
                    }
                    if (chunkToTest.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 32 + (int)Sign(toMoveY)) % 16] <= 0)
                    {
                        if (Abs(toMoveY) >= 1)
                        {
                            posY += (int)Sign(toMoveY);
                            realPosY += Sign(toMoveY);
                            toMoveY = Sign(toMoveY) * (Abs(toMoveY) - 1);
                        }
                        else
                        {
                            realPosY += toMoveY;
                            posY = (int)Floor(realPosY, 1);
                            toMoveY = 0;
                        }
                    }
                    else
                    {
                        speedY = 0;
                        toMoveY = 0;
                        break;
                    }
                }
                while (Abs(toMoveX) > 0)
                {
                    chunkPos = screen.findChunkAbsoluteIndex(posX + (int)Sign(toMoveX), posY);
                    if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                    {
                        posX += (int)Sign(toMoveX);
                        saveEntity(this);
                        screen.entitesToRemove.Add(this);
                        return;
                    }
                    if (chunkToTest.fillStates[(posX % 16 + 32 + (int)Sign(toMoveX)) % 16, (posY % 16 + 32) % 16] <= 0)
                    {
                        if (Abs(toMoveX) >= 1)
                        {
                            posX += (int)Sign(toMoveX);
                            realPosX += Sign(toMoveX);
                            toMoveX = Sign(toMoveX) * (Abs(toMoveX) - 1);
                        }
                        else
                        {
                            realPosX += toMoveX;
                            posX = (int)Floor(realPosX, 1);
                            toMoveX = 0;
                        }
                    }
                    else
                    {
                        speedX = 0;
                        toMoveX = 0;
                        break;
                    }
                }

                // test what happens if in special liquids (fairy lake, lava...)

                {
                    chunkPos = screen.findChunkAbsoluteIndex(posX, posY);
                    if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest))
                    {
                        return;
                    }
                    if (type != 0 && chunkToTest.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 32) % 16] == -3)
                    {
                        if (rand.Next(10) == 0)
                        {
                            type = 0;
                            this.color = Color.Purple;
                        }
                    }
                    if (type != 2 && chunkToTest.fillStates[(posX % 16 + 32) % 16, (posY % 16 + 32) % 16] == -4)
                    {
                        if (rand.Next(10) == 0)
                        {
                            screen.entitesToRemove.Add(this);
                        }
                    }
                }
            }
            public void initializeInventory()
            {
                inventoryQuantities = new Dictionary<(int index, int subType, int typeOfElement), int>
                {

                };
                inventoryElements = new List<(int index, int subType, int typeOfElement)>
                {

                };
            }
            public void moveInventoryCursor(int value)
            {
                int counto = inventoryElements.Count;
                if (counto == 0) { inventoryCursor = 0; return; }
                inventoryCursor = ((inventoryCursor + value) % counto + counto) % counto;
            }
            public void Dig(int posToDigX, int posToDigY)
            {
                (int, int) chunkPos = screen.findChunkAbsoluteIndex(posToDigX, posToDigY);
                if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest)) { return; }
                int tileContent = chunkToTest.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16];
                if (tileContent != 0)
                {
                    (int index, int subType, int typeOfElement)[] inventoryKeys = inventoryQuantities.Keys.ToArray();
                    for (int i = 0; i < inventoryKeys.Length; i++)
                    {
                        if (inventoryKeys[i].index == tileContent && inventoryKeys[i].typeOfElement == 0)
                        {
                            if (inventoryQuantities[(tileContent, 0, 0)] != -999)
                            {
                                inventoryQuantities[(tileContent, 0, 0)]++;
                            }
                            goto AfterTest;
                        }
                    }
                    // there was none of the thing present in the inventory already so gotta create it
                    inventoryQuantities.Add((tileContent, 0, 0), 1);
                    inventoryElements.Add((tileContent, 0, 0));
                AfterTest:;
                    chunkToTest.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16] = 0;
                    chunkToTest.findTileColor((posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16);
                    chunkToTest.testLiquidUnstableAir(posToDigX, posToDigY);
                    chunkToTest.modificationCount += 1;
                    elementsPossessed++;
                    timeAtLastDig = timeElapsed;
                }
            }
            public void Place(int posToDigX, int posToDigY)
            {
                (int, int) chunkPos = screen.findChunkAbsoluteIndex(posToDigX, posToDigY);
                if (!screen.loadedChunks.TryGetValue(chunkPos, out Chunk chunkToTest)) { return; }
                (int index, int subType, int typeOfElement) tileContent = inventoryElements[inventoryCursor];
                int tileState = chunkToTest.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16];
                if (tileState == 0 || tileState < 0 && tileContent.typeOfElement == 1)
                {
                    if (inventoryQuantities[tileContent] != -999)
                    {
                        inventoryQuantities[tileContent]--;
                        if (inventoryQuantities[tileContent] <= 0)
                        {
                            inventoryQuantities.Remove(tileContent);
                            inventoryElements.Remove(tileContent);
                            moveInventoryCursor(0);
                        }
                    }
                    if (tileContent.typeOfElement == 1)
                    {
                        Entity newEntity = new Entity(chunkToTest, (posToDigX, posToDigY), tileContent.index, tileContent.subType);
                        screen.activeEntities.Add(newEntity);
                        timeAtLastPlace = timeElapsed;
                    }
                    else if (tileContent.typeOfElement == 2)
                    {
                        Plant newPlant = new Plant(chunkToTest, (posToDigX, posToDigY), tileContent.index, tileContent.subType);
                        if (!newPlant.isDeadAndShouldDisappear) { screen.activePlants.Add(newPlant); }
                        timeAtLastPlace = timeElapsed;
                    }
                    else
                    {
                        chunkToTest.fillStates[(posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16] = tileContent.index;
                        chunkToTest.findTileColor((posToDigX % 16 + 32) % 16, (posToDigY % 16 + 32) % 16);
                        chunkToTest.testLiquidUnstableLiquid(posToDigX, posToDigY);
                        chunkToTest.modificationCount += 1;
                        timeAtLastPlace = timeElapsed;
                    }
                    elementsPossessed--;
                }
            }
        }
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
                fillStates = arrayToDict(branchJson.fillStates);
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
                int growthLevelToTest = growthLevel + 1;
                if (growthLevelToTest == 0)
                {
                    growthLevel = growthLevelToTest;
                    return true;
                }


                if (motherPlant.type == 1) // woody tree
                {
                    maxGrowthLevel = Min(motherPlant.maxGrowthLevel - pos.y, seed%motherPlant.maxGrowthLevel);
                    if (growthLevelToTest > maxGrowthLevel)
                    {
                        return false;
                    }

                    int direction; // -1 = left, 1 = right
                    direction = (seed % 2) * 2 - 1;

                    (int x, int y) drawPos = (lastDrawPos.x, lastDrawPos.y + 1);

                    if (LCGint1(seed + growthLevelToTest) % Max(1, (int)(growthLevelToTest*0.5f)) == 0 || growthLevelToTest < 3)
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
                        lastDrawPos = drawPos;
                        growthLevel = growthLevelToTest;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }



                // IF THE ranbrach IS NOT A KNOWN INDEX IT WEIRD.... so just bye bye branchds I guess
                return false;
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
                fillStates = arrayToDict(flowerJson.fillStates);
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
                    growthLevel = growthLevelToTest;
                    return true;
                }

                if (motherPlant.type == 1) // tree
                {
                    maxGrowthLevel = 5;

                    (int x, int y) drawPos = (0, 0);
                    if (growthLevelToTest == 1)
                    {
                        fillStates = new Dictionary<(int x, int y), int>();
                        tryFill(drawPos, 1);
                    }
                    else if (growthLevelToTest == 2)
                    {
                        fillStates = new Dictionary<(int x, int y), int>();
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
                        fillStates = new Dictionary<(int x, int y), int>();
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
                        fillStates = new Dictionary<(int x, int y), int>();
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
                    }
                    if (growthLevelToTest > maxGrowthLevel) { return false; }
                    growthLevel = growthLevelToTest;
                    return true;
                }
                else if (motherPlant.type == 4) // mushy
                {
                    maxGrowthLevel = motherPlant.maxGrowthLevel;

                    fillStates = new Dictionary<(int x, int y), int>();

                    tryFill((-1, 0), 5);
                    tryFill((0, 0), 5);
                    tryFill((1, 0), 5);
                    if (growthLevel > seed % 8 + 2)
                    {
                        tryFill((-2, 0), 5);
                        tryFill((2, 0), 5);
                        if (growthLevel > seed % 8 + 3 + seed % 2)
                        {
                            tryFill((-1, 1), 5);
                            tryFill((0, 1), 5);
                            tryFill((1, 1), 5);
                        }
                    }

                    if (growthLevelToTest > maxGrowthLevel) { return false; }
                    growthLevel = growthLevelToTest;
                    return true;
                }
                else if (motherPlant.type == 5) // vine
                {
                    if (type == 0)
                    {
                        maxGrowthLevel = 3;

                        if (growthLevelToTest == 1)
                        {
                            fillStates = new Dictionary<(int x, int y), int>();
                            tryFill((0, 0), 6);
                            growthLevel = growthLevelToTest;
                            return true;
                        }
                        else if (growthLevelToTest == 2)
                        {
                            fillStates = new Dictionary<(int x, int y), int>();
                            tryFill((0, 0), 6);
                            tryFill((1, 0), 6);
                            tryFill((-1, 0), 6);
                            tryFill((0, 1), 6);
                            tryFill((0, -1), 6);
                            growthLevel = growthLevelToTest;
                            return true;
                        }
                        else if (growthLevelToTest == 3)
                        {
                            fillStates = new Dictionary<(int x, int y), int>();
                            tryFill((0, 0), 7);
                            tryFill((1, 0), 6);
                            tryFill((-1, 0), 6);
                            tryFill((0, 1), 6);
                            tryFill((0, -1), 6);
                            growthLevel = growthLevelToTest;
                            return true;
                        }
                        else if (growthLevelToTest > maxGrowthLevel) { return false; }
                    }
                    else if (type == 1)
                    {
                        int maxGrowthLevel = 3;

                        if (growthLevelToTest == 1)
                        {
                            fillStates = new Dictionary<(int x, int y), int>();
                            tryFill((0, 0), 6);
                            growthLevel = growthLevelToTest;
                            return true;
                        }
                        else if (growthLevelToTest == 2)
                        {
                            fillStates = new Dictionary<(int x, int y), int>();
                            tryFill((0, 0), 6);
                            tryFill((1, 1), 6);
                            tryFill((-1, 1), 6);
                            tryFill((1, -1), 6);
                            tryFill((-1, -1), 6);
                            growthLevel = growthLevelToTest;
                            return true;
                        }
                        else if (growthLevelToTest == 3)
                        {
                            fillStates = new Dictionary<(int x, int y), int>();
                            tryFill((0, 0), 7);
                            tryFill((1, 1), 6);
                            tryFill((-1, 1), 6);
                            tryFill((1, -1), 6);
                            tryFill((-1, -1), 6);
                            growthLevel = growthLevelToTest;
                            return true;
                        }
                        else if (growthLevelToTest > maxGrowthLevel) { return false; }
                    }
                }




                // IF THE fklower IS NOT A KNOWN INDEX IT WEIRD.... so just bye bye folwer I guess
                return false;
            }
        }
        public class Plant
        {
            public Form1.Screen screen;
            public Chunk chunk;

            public int seed;
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

            public Bitmap bitmap;
            public Dictionary<(int x, int y), int> fillStates = new Dictionary<(int x, int y), int>();
            public int[] posOffset = new int[3];
            public int[] bounds = new int[4];
            public (int x, int y) lastDrawPos = (0, 0);

            public List<Branch> childBranches = new List<Branch>();
            public List<Flower> childFlowers = new List<Flower>();

            public bool isDeadAndShouldDisappear = false;
            public bool isStable = false;
            public Plant(Chunk chunkToPut, PlantJson plantJson)
            {
                chunk = chunkToPut;
                screen = chunk.screen;
                posX = plantJson.pos.Item1;
                posY = plantJson.pos.Item2;
                lastDrawPos = plantJson.lstGrPos;
                type = plantJson.type.Item1;
                subType = plantJson.type.Item2;
                seed = plantJson.seed;
                growthLevel = plantJson.grLvl;
                timeAtLastGrowth = plantJson.lastGr;
                fillStates = arrayToDict(plantJson.fillStates);
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
            public Plant(Chunk chunkToPut)
            {
                chunk = chunkToPut;
                screen = chunk.screen;
                seed = LCGint1(Abs((int)chunk.chunkSeed));
                growthLevel = -1;
                placePlant();
                findType();
                findColors();
                tryGrowth();
                makeBitmap();
                timeAtLastGrowth = timeElapsed;
            }
            public Plant(Chunk chunkToPut, (int, int) positionToPut, int typeToPut, int subTypeToPut)
            {
                chunk = chunkToPut;
                screen = chunk.screen;
                posX = positionToPut.Item1;
                posY = positionToPut.Item2;
                type = typeToPut;
                subType = subTypeToPut;;
                seed = rand.Next(1000000000); //                               FALSE RANDOM NOT SEEDED ARGHHEHEEEE
                growthLevel = -1;
                testPlantPosition();
                findColors();
                tryGrowth();
                makeBitmap();
                timeAtLastGrowth = timeElapsed;
            }
            public bool tryFill((int x, int y) testPos, int typeToFill)
            {
                if (testIfPositionEmpty(testPos)) { fillStates[testPos] = typeToFill; return true; }
                return false;
            }
            public void findType()
            {
                int biome = chunk.biomeIndex[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16][0].Item1;
                if (attachPoint == 0)
                {
                    if (chunk.fillStates[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16] < 0)
                    {
                        type = 2;
                        subType = 0;
                    }
                    else if (biome == 3) // forest
                    {
                        if (rand.Next(2) == 0)
                        {
                            type = 0;
                            subType = 0;
                        }
                        else
                        {
                            type = 1;
                            subType = 0;
                        }
                    }
                    else if (biome == 5) // fairy
                    {
                        type = 4;
                        subType = 0;
                    }
                    else if (biome == 6) // obsidian
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
                    else
                    {
                        type = 0;
                        subType = 0;
                    }
                }

                else if (attachPoint == 3)
                {
                    if (chunk.fillStates[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16] < 0)
                    {
                        type = 2;
                        subType = 1;
                    }
                    else if (biome == 6) // obsidian
                    {
                        type = 5;
                        subType = 1;
                    }
                    else
                    {
                        if (seed % 7 == 0)
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
                int hueVar = (int)(seedo%101) - 50;
                seedo = LCGint1(seed);
                int shadeVar = (int)(seedo%61) - 30;
                //int biome = chunk.biomeIndex[(posX % 16 + 16) % 16, (posY % 16 + 16) % 16][0].Item1;
                if (type == 0) // normal
                {
                    colorDict.Add(1, Color.FromArgb(50 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                    return;
                }
                if (type == 1) // woody
                {
                    colorDict.Add(1, Color.FromArgb(50 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                    colorDict.Add(2, Color.FromArgb(140 + (int)(hueVar*0.3f) - shadeVar, 140 - (int)(hueVar * 0.3f) - shadeVar, 50 - shadeVar));
                    colorDict.Add(6, Color.FromArgb(170 - shadeVar, 120 - hueVar - shadeVar, 150 - shadeVar));
                    colorDict.Add(7, Color.FromArgb(170 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                    return;
                }
                if (type == 2) // kelp
                {
                    colorDict.Add(3, Color.FromArgb(30 - shadeVar, 90 - shadeVar + hueVar, 140 - shadeVar - hueVar));
                    return;
                }
                if (type == 3) // obsidian
                {
                    colorDict.Add(1, Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar));
                    return;
                }
                if (type == 4) // mushroom
                {
                    colorDict.Add(4, Color.FromArgb(180 + shadeVar, 160 + shadeVar, 165 + shadeVar));
                    colorDict.Add(5, Color.FromArgb(140 - shadeVar, 120 + hueVar, 170 - hueVar));
                    return;
                }
                if (type == 5) // vine
                {
                    if (subType == 0)
                    {
                        colorDict.Add(1, Color.FromArgb(50 - shadeVar, 120 - hueVar - shadeVar, 50 - shadeVar));
                        colorDict.Add(6, Color.FromArgb(170 - shadeVar, 120 - hueVar - shadeVar, 150 - shadeVar));
                        colorDict.Add(7, Color.FromArgb(170 - shadeVar, 170 - hueVar - shadeVar, 50 - shadeVar));
                        return;
                    }
                    else if (subType == 1)
                    {
                        colorDict.Add(1, Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar));
                        colorDict.Add(6, Color.FromArgb(30 + shadeVar, 30 + shadeVar, 30 + shadeVar));
                        colorDict.Add(7, Color.FromArgb(220 + shadeVar, 220 + shadeVar, 220 + shadeVar));
                        return;
                    }
                }
            }
            public bool testIfPositionEmpty((int x, int y) mod)
            {
                (int x, int y) pixelPos = (posX + mod.x, posY + mod.y);
                (int x, int y) chunkPos = (Floor(pixelPos.x, 16) / 16, Floor(pixelPos.y, 16) / 16);

                if (screen.loadedChunks.ContainsKey(chunkPos))
                {
                    if (screen.loadedChunks[chunkPos].fillStates[(pixelPos.x % 16 + 16) % 16, (pixelPos.y % 16 + 16) % 16] > 0)
                    {
                        return false;
                    }
                    return true;
                }

                if (!screen.extraLoadedChunks.ContainsKey(chunkPos)) { screen.extraLoadedChunks.Add(chunkPos, new Chunk(chunkPos, true, screen)); }
                if (screen.extraLoadedChunks[chunkPos].fillStates[(pixelPos.x % 16 + 16) % 16, (pixelPos.y % 16 + 16) % 16] > 0)
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
                bitmap = new Bitmap(width, height);

                foreach ((int x, int y) drawPos in fillDict.Keys)
                {
                    bitmap.SetPixel(drawPos.x - minX, drawPos.y - minY, colorDict[fillDict[drawPos]]);
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
            }
            public bool tryGrowth() // 0 = nothing, 1 = plantMatter, 2 = wood, 3 = aquaticPlantMatter, 4 = mushroomStem, 5 = mushroomCap, 6 = petal, 7 = flowerPollen
            {
                int seedo = seed;

                int growthLevelToTest = growthLevel+1;
                if (growthLevelToTest == 0)
                {
                    if (type == 0 || type == 1 || (type == 2 && subType == 0) || type == 3 || type == 4)
                    {
                        lastDrawPos = (0, -1);
                    }
                    else
                    {
                        lastDrawPos = (0, 1);
                    }
                    growthLevel = growthLevelToTest;
                    return true;
                }

                if (type == 0) // normal plant
                {
                    maxGrowthLevel = 1 + seed % 5;
                    if (growthLevelToTest > maxGrowthLevel) { return false; }

                    (int x, int y) drawPos = (lastDrawPos.x, lastDrawPos.y + 1);

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
                        lastDrawPos = drawPos;
                        growthLevel = growthLevelToTest;
                        return true;
                    }
                    return false;
                }
                else if (type == 1) // tree
                {
                    maxGrowthLevel = 10 + seed % 40;
                    if (growthLevelToTest > maxGrowthLevel) { return false; }
                    if (growthLevelToTest == 0) { }

                    (int x, int y) drawPos = (lastDrawPos.x, lastDrawPos.y + 1);

                    seedo = LCGint1(seed + growthLevelToTest * (seed % 7 + 1));
                    int resulto = seedo % 3;
                    if (resulto == 0 && growthLevel == 2)
                    {
                        drawPos = (drawPos.x - 1, drawPos.y);
                    }
                    else if (resulto == 2 && growthLevel == 2)
                    {
                        drawPos = (drawPos.x + 1, drawPos.y);
                    }

                    if (tryFill(drawPos, 2))
                    {
                        lastDrawPos = drawPos;

                        int spacing = 3 + seedo % 3;
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
                        lastDrawPos = drawPos;
                        growthLevel = growthLevelToTest;
                        return true;
                    }
                    return false;
                }
                else if (type == 2) // kelp
                {
                    if (subType == 0)
                    {
                        maxGrowthLevel = 1 + seed % 10;
                        if (growthLevelToTest > maxGrowthLevel) { return false; }

                        int multo = 2 * (seed % 2) - 1;
                        (int x, int y) drawPos = (((growthLevelToTest - 1) % 2) * multo, growthLevelToTest - 1);

                        if (tryFill(drawPos, 3))
                        {
                            lastDrawPos = drawPos;
                            growthLevel = growthLevelToTest;
                            return true;
                        }
                        return false;
                    }
                    else if (subType == 1)
                    {
                        maxGrowthLevel = 1 + seed % 10;
                        if (growthLevelToTest > maxGrowthLevel) { return false; }

                        int multo = 2 * (seed % 2) - 1;
                        (int x, int y) drawPos = (((growthLevelToTest - 1) % 2) * multo, -(growthLevelToTest - 1));

                        if (tryFill(drawPos, 3))
                        {
                            lastDrawPos = drawPos;
                            growthLevel = growthLevelToTest;
                            return true;
                        }
                        return false;
                    }
                }
                else if (type == 3) // obsidian plant
                {
                    maxGrowthLevel = 1 + seed % 3;
                    if (growthLevelToTest > maxGrowthLevel) { return false; }

                    (int x, int y) drawPos = (lastDrawPos.x, lastDrawPos.y + 1);

                    if (tryFill(drawPos, 1))
                    {
                        lastDrawPos = drawPos;
                        growthLevel = growthLevelToTest;
                        return true;
                    }
                    return false;
                }
                else if (type == 4) // mushroom
                {
                    maxGrowthLevel = 1 + seed % 7;
                    if (growthLevelToTest > maxGrowthLevel) { return false; }

                    (int x, int y) drawPos = (lastDrawPos.x, lastDrawPos.y + 1);

                    if (tryFill(drawPos, 4))
                    {
                        lastDrawPos = drawPos;

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
                        growthLevel = growthLevelToTest;
                        return true;
                    }
                    return false;
                }
                else if (type == 5) // vine
                {
                    maxGrowthLevel = 4 + seed % 60;
                    if (growthLevelToTest > maxGrowthLevel) { return false; }

                    int multo = 2 * (seed % 2) - 1;
                    (int x, int y) drawPos = (((int)((growthLevelToTest - 1) * 0.5f) % 2) * multo, -(growthLevelToTest - 1));

                    if (tryFill(drawPos, 1))
                    {
                        lastDrawPos = drawPos;
                        growthLevel = growthLevelToTest;

                        int spacingOfFlowers = 4 + seedo % 4;
                        if (growthLevel % spacingOfFlowers == 2)
                        {
                            int typet = (LCGint2(seed + growthLevel) % 2 + 2) % 2;
                            Flower flower = new Flower(this, drawPos, typet, seed + growthLevel);
                            childFlowers.Add(flower);
                        }

                        return true;
                    }
                }
                return false;
            }
            public void testPlantGrowth()
            {
                if (!isStable && timeElapsed >= 0.2f + timeAtLastGrowth)
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
                    makeBitmap();
                    timeAtLastGrowth = timeElapsed;
                }
            }
            public void addPlantInChunks()
            {
                (int, int) chunkPos;
                Chunk chunkToTest;

                for (int i = bounds[0]; i <= bounds[1];i++)
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
                }
            }
            public void placePlant()
            {
                int counto = 0;
                while (counto < 10000)
                {
                    int randX = rand.Next(16);
                    int randY = rand.Next(14)+1;
                    if (chunk.fillStates[randX, randY] <= 0)
                    {
                        if (chunk.fillStates[randX, randY - 1] > 0)
                        {
                            posX = chunk.position.Item1 * 16 + randX;
                            posY = chunk.position.Item2 * 16 + randY;
                            attachPoint = 0;
                            return;
                        }
                        else if (chunk.fillStates[randX, randY + 1] > 0)
                        {
                            posX = chunk.position.Item1 * 16 + randX;
                            posY = chunk.position.Item2 * 16 + randY;
                            attachPoint = 3;
                            return;
                        }
                    }
                    counto += 1;
                }
                isDeadAndShouldDisappear = true;
            }
            public void testPlantPosition()
            {
                int randX = (posX%16+16)%16;
                int randY = (posY%16+16)%16;
                if (chunk.fillStates[randX, randY] <= 0)
                {
                    if (randY < 15 && chunk.fillStates[randX, randY + 1] > 0)
                    {
                        return;
                    }
                    else if (randY > 0 && chunk.fillStates[randX, randY - 1] > 0)
                    {
                        return;
                    }
                }
                isDeadAndShouldDisappear = true;
            }
        }
    }
}
