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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;

namespace Cave
{
    public class Plants
    {
        public class Plant
        {
            public Screens.Screen screen;
            public PlantElement plantElement;
            public PlantTraits traits;

            public int id;
            public (int type, int subType) type;
            public int seed;
            public int randValue;
            public int growthLevel;
            public int posX = 0;
            public int posY = 0;
            public int attachPoint; // 0 ground, 1 leftWall, 2 rightWall, 3 ceiling
            public Dictionary<(int type, int subType), Color> colorDict = new Dictionary<(int type, int subType), Color>();

            public float timeAtLastGrowth = timeElapsed;

            public Bitmap bitmap = new Bitmap(1, 1);
            public Bitmap secondaryBitmap = new Bitmap(1, 1);
            public List<(int x, int y)> lightPositions = new List<(int x, int y)>();
            public Color lightColor = Color.Black;
            public (int type, int subType) lightMaterial = (0, 0);
            public int[] posOffset = new int[3];
            public int[] bounds = new int[4];

            public Dictionary<(int x, int y), bool> chunkPresence = new Dictionary<(int x, int y), bool>();

            public bool isDeadAndShouldDisappear = false;
            public bool isStable = false;
            public Plant(Screens.Screen screenToPut, PlantJson plantJson)
            {
                screen = screenToPut;
                posX = plantJson.pos.Item1;
                posY = plantJson.pos.Item2;
                seed = plantJson.seed;
                randValue = plantJson.rand;
                id = plantJson.id;
                growthLevel = plantJson.grLvl;
                timeAtLastGrowth = plantJson.lastGr;
                transformPlant(plantJson.type);
                plantElement = new PlantElement(this, plantJson.pE);
                makeBitmap();
            }
            public Plant(Chunk chunkToPut, (int x, int y) posToPut, (int type, int subType) typeToPut)
            {
                posX = posToPut.x;
                posY = posToPut.y;
                screen = chunkToPut.screen;
                seed = LCGint1(Abs((int)chunkToPut.chunkSeed));
                seed = Abs(seed + rand.Next(100000)); // TO CHANGE TOCHANGE cuz false randommmm
                randValue = seed;
                id = currentPlantId;
                growthLevel = 0;
                transformPlant(typeToPut);
                plantElement = new PlantElement(this, (0, 0), traits.plantElementType, seed);
                if (isDeadAndShouldDisappear) { return; }
                tryGrowToMaximum();
                makeBitmap();
                timeAtLastGrowth = timeElapsed;

                currentPlantId++;
            }
            public Plant(Screens.Screen screenToPut, (int, int) posToPut, (int type, int subType) typeToPut)
            {
                screen = screenToPut;
                posX = posToPut.Item1;
                posY = posToPut.Item2;
                seed = rand.Next(1000000000); //                               FALSE RANDOM NOT SEEDED ARGHHEHEEEE
                randValue = seed;
                id = currentPlantId;
                growthLevel = 0;
                transformPlant(typeToPut);
                testPlantPosition();
                if (isDeadAndShouldDisappear) { return; }
                plantElement = new PlantElement(this, (0, 0), traits.plantElementType, seed);
                testPlantGrowth(true);
                makeBitmap();
                timeAtLastGrowth = timeElapsed;

                currentPlantId++;
            }
            public int getplantRandValue(int mod = -1)
            {
                randValue = (int)cashInt((seed, 7, 13), 0);
                if (mod == 0) { return 0; }
                return mod > 0 ? randValue % mod : randValue;
            }
            public (int x, int y) getRealPos((int x, int y) pos) { return (posX + pos.x, posY + pos.y); }
            public (int x, int y) getRelativePos((int x, int y) pos) { return (pos.x - posX, pos.y - posY); }
            public void tryAddMaterialColor((int type, int subType) materialToAdd, ColorRange forceColorRange = null)
            {
                if (colorDict.ContainsKey(materialToAdd)) { return; }
                ColorRange c = forceColorRange ?? getMaterialTraits(materialToAdd).colorRange;
                float hueVar = (float)((seed % 11) * 0.2f - 1);
                float shadeVar = (float)((LCGz(seed) % 11) * 0.2f - 1);
                colorDict[materialToAdd] = Color.FromArgb(
                    ColorClamp(c.r.v + (int)(hueVar * c.r.h) + (int)(shadeVar * c.r.s)),
                    ColorClamp(c.g.v + (int)(hueVar * c.g.h) + (int)(shadeVar * c.g.s)),
                    ColorClamp(c.b.v + (int)(hueVar * c.b.h) + (int)(shadeVar * c.b.s))
                );
            }
            public void makeBitmap()
            {
                List<PlantElement> plantElements = returnAllPlantElements();
                Dictionary<(int x, int y), (int type, int subType)> fillDict = returnFullPlantFillDict(plantElements);

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
                findLightPositions(plantElements);

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
            public void findLightPositions(List<PlantElement> plantElements)
            {
                lightPositions = new List<(int x, int y)>();

                if (traits.lightElements is null || traits.lightElements.Length == 0) { lightMaterial = (0, 0); return; }
                else { lightMaterial = traits.lightElements[0]; }

                foreach (PlantElement element in plantElements)
                {
                    if (element.traits.forceLightAtPos) { lightPositions.Add((element.pos.x + posX, element.pos.y + posY)); }
                    foreach ((int x, int y) keyo in element.fillStates.Keys)
                    {
                        if (element.fillStates[keyo] == lightMaterial)
                        {
                            lightPositions.Add((keyo.x + element.pos.x + posX, keyo.y + element.pos.y + posY));
                        }
                    }
                }

                if (colorDict.ContainsKey(lightMaterial))
                {
                    Color col = colorDict[lightMaterial];
                    lightColor = Color.FromArgb(255, (col.R + 255) / 2, (col.G + 255) / 2, (col.B + 255) / 2);
                }
                else { lightColor = Color.Black; }
            }
            public void tryGrowToMaximum()
            {
                isStable = false;
                int maxIterations = -1;
                if (plantElement.traits.plantGrowthRules != null && plantElement.traits.plantGrowthRules.isMold) { maxIterations = 2 + rand.Next(100); }
                int i = 0;
                while (!isStable && (maxIterations == -1 || i < maxIterations))
                {
                    isStable = !testPlantGrowth(true);
                    i++;
                }
                if (growthLevel < traits.minGrowthForValidity) { isDeadAndShouldDisappear = true; }
            }
            public bool testPlantGrowth(bool forceGrowth)
            {
                // plantElement.tryToMakeParticle();
                if (forceGrowth || (!isStable && timeElapsed >= 0.2f + timeAtLastGrowth))
                {
                    int growthIncreaseInt = 0;

                    List<PlantElement> plantElementsToGrow = new List<PlantElement> { plantElement };
                    for (int i = 0; i < plantElementsToGrow.Count; i++)
                    {
                        PlantElement currentPlant = plantElementsToGrow[i];
                        growthIncreaseInt = Max(growthIncreaseInt, currentPlant.tryGrowth());
                        foreach (PlantElement plantElementToAdd in currentPlant.childPlantElements)
                        {
                            plantElementsToGrow.Add(plantElementToAdd);
                        }
                    }
                    if (!forceGrowth) { makeBitmap(); }   // not to make bitmap when it's not needed (growing to max)
                    timeAtLastGrowth = timeElapsed;

                    if (growthIncreaseInt == 0) { isStable = true; return false; }
                    if (growthIncreaseInt == 1) { growthLevel++; isStable = true; return false; }
                    if (growthIncreaseInt == 2) { growthLevel++; isStable = false; return true; }
                }
                return false;
            }
            public bool testIfPositionEmpty((int x, int y) mod) // Improve for water (returns true for water, should only do that for plants)
            {
                (int x, int y) pixelPos = getRealPos(mod);
                (int x, int y) pixelTileIndex = PosMod(pixelPos);

                Chunk chunkToTest = screen.getChunkFromPixelPos(pixelPos);
                if (chunkToTest.fillStates[pixelTileIndex.x, pixelTileIndex.y].isSolid) { return false; }
                return true;
            }
            public void testPlantPosition()
            {
                if (!screen.getChunkFromPixelPos((posX, posY)).fillStates[PosMod(posX), PosMod(posY)].isAir) { goto Fail; } // Full tile -> Fail
                int mod = traits.isCeiling ? 1 : -1;
                if (!screen.getChunkFromPixelPos((posX, posY + mod)).fillStates[PosMod(posX), PosMod(posY + mod)].isAir) { return; } // tile under/over full -> Success
            Fail:;
                isDeadAndShouldDisappear = true;
            }
            public Dictionary<(int x, int y), (int type, int subType)> returnFullPlantFillDict(List<PlantElement> plantElements = null)
            {
                if (plantElements is null) { plantElements = returnAllPlantElements(); }
                Dictionary<(int x, int y), (int type, int subType)> fillDict = new Dictionary<(int x, int y), (int type, int subType)>();
                foreach (PlantElement element in plantElements)
                {
                    foreach ((int x, int y) keyo in element.fillStates.Keys) { fillDict[(keyo.x + element.pos.x, keyo.y + element.pos.y)] = element.fillStates[keyo]; }
                }
                return fillDict;
            }
            public List<PlantElement> returnAllPlantElements()
            {
                List<PlantElement> plantElements = new List<PlantElement> { plantElement };
                for (int i = 0; i < plantElements.Count; i++)
                {
                    foreach (PlantElement plantElement in plantElements[i].childPlantElements) { plantElements.Add(plantElement); }
                }
                return plantElements;
            }
            public (int type, int subType) tryDig((int x, int y) posToDig, (int type, int subType, int typeOfElement)? currentItem = null, (int type, int subType)? targetMaterialNullable = null, bool toolRestrictions = true)
            {
                List<PlantElement> plantElements = returnAllPlantElements();

                (int x, int y) posToTest;
                foreach (PlantElement element in plantElements)
                {
                    posToTest = (posToDig.x - posX - element.pos.x, posToDig.y - posY - element.pos.y);
                    if (element.fillStates.TryGetValue((posToTest.x, posToTest.y), out (int type, int subType) value))
                    {
                        MaterialTraits traits = getMaterialTraits(value);
                        if ((targetMaterialNullable is null ? true : targetMaterialNullable == value) && (!toolRestrictions || traits.toolGatheringRequirement == null || traits.toolGatheringRequirement.Value == currentItem))
                        {
                            element.fillStates.Remove((posToTest.x, posToTest.y));
                            screen.plantsToMakeBitmapsOf[id] = this;
                            return value;
                        }
                    }
                }
                return (0, 0);
            }
            public (bool found, int x, int y) findPointOfInterestInPlant((int type, int subType) elementOfInterest)
            {
                List<PlantElement> plantElements = returnAllPlantElements();

                foreach (PlantElement element in plantElements)
                {
                    if (element.fillStates.ContainsValue(elementOfInterest))
                    {
                        foreach ((int x, int y) pos in element.fillStates.Keys)
                        {
                            if (element.fillStates[pos] == elementOfInterest)
                            {
                                return (true, posX + element.pos.x + pos.x, posY + element.pos.y + pos.y);
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
                if (traits.colorOverrideArray != null)
                {
                    foreach (((int type, int subType) type, ColorRange colorRange) item in traits.colorOverrideArray)
                    {
                        tryAddMaterialColor(item.type, item.colorRange);
                    }
                }
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




        public class PlantElement
        {
            public Plant motherPlant;
            public PlantElementTraits traits;

            public List<PlantElement> childPlantElements = new List<PlantElement>();

            public int seed;
            public (int type, int subType, int subSubType) type;
            
            public int maxGrowthLevel;
            
            public (int x, int y) pos;

            public int growthLevel;
            public (int x, int y) lastDrawPos = (0, 0);
            public (int x, int y) growthDirection = (0, 0);
            public (int x, int y) baseDirection = (0, 0);

            public int currentFrameArrayIdx = 0;
            public int frameArrayOffset = 0;

            public int currentChildArrayIdx = -1;
            public int childArrayOffset;
            public int currentDirectionArrayIdx = -1;
            public int directionArrayOffset;
            public int currentModArrayIdx = -1;
            public int modArrayOffset;

            public Dictionary<(int type, int subType), Color> colorOverrideDict = null;
            public Dictionary<(int x, int y), (int type, int subType)> fillStates = new Dictionary<(int x, int y), (int type, int subType)>();
            public PlantElement(Plant motherPlantToPut, PlantElementJson plantElementJson)
            {
                motherPlant = motherPlantToPut;
                pos = plantElementJson.pos;
                lastDrawPos = plantElementJson.lstGrPos;
                type = plantElementJson.type;
                baseDirection = plantElementJson.bD;
                getTraitAndAddColors();
                seed = plantElementJson.seed;
                fillStates = arrayToFillstates(plantElementJson.fS);
                foreach (PlantElementJson baby in plantElementJson.pEs) { childPlantElements.Add(new PlantElement(motherPlant, baby)); }
                growthLevel = plantElementJson.grLvl;
            }
            public PlantElement(Plant motherPlantToPut, (int x, int y) posToPut, (int type, int subType, int subSubType) typeToPut, int seedToPut, (int x, int y)? forceDirection = null)
            {
                motherPlant = motherPlantToPut;
                pos = posToPut;
                type = typeToPut;
                seed = seedToPut;
                getTraitAndAddColors();
                fillStates = new Dictionary<(int x, int y), (int type, int subType)>();
                growthLevel = -1;
                init(forceDirection);
            }
            public int getRandValue(int valueModifier, int modulo = -1)
            {
                int randy = (int)cashInt((valueModifier, 7, 13), seed);
                if (modulo == 0) { return 0; }
                return modulo > 0 ? randy % modulo : randy;
            }
            public void getTraitAndAddColors()
            {
                traits = getPlantElementTraits(type);
                maxGrowthLevel = traits.maxGrowth.maxLevel + seed % (traits.maxGrowth.range + 1);  // will put variation in growth levels here
                foreach ((int type, int subType) material in traits.materialsPresent)
                {
                    motherPlant.tryAddMaterialColor(material);
                }
            }
            public (int x, int y) absolutePos((int x, int y) position) {
                return (pos.x + position.x, pos.y + position.y);
            }
            public bool tryFill((int x, int y) testPos, (int type, int subType) typeToFill)
            {
                if (motherPlant.testIfPositionEmpty(absolutePos(testPos))) { fillStates[testPos] = typeToFill; return true; }
                return false;
            }
            public void makeBaby(((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType) item, int seedMod, int offset)
            {
                (int x, int y) babyPos = item.dirType != 0 ? absolutePos(lastDrawPos) : absolutePos((lastDrawPos.x + item.mod.x, lastDrawPos.y + item.mod.y));
                (int x, int y)? babyDirection = null;
                if (item.dirType == 1) { babyDirection = item.mod; }
                else if (item.dirType == 2) { babyDirection = baseDirection; }
                PlantElement baby = new PlantElement(motherPlant, babyPos, item.child, getRandValue(seed + 923147 * seedMod + offset * 10000), babyDirection);
                childPlantElements.Add(baby);
            }
            public void makeBaby(((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, (int frame, int range) birthFrame) item, int seedMod)
            {
                (int x, int y) babyPos = item.dirType != 0 ? absolutePos(lastDrawPos) : absolutePos((lastDrawPos.x + item.mod.x, lastDrawPos.y + item.mod.y));
                (int x, int y)? babyDirection = null;
                if (item.dirType == 1) { babyDirection = item.mod; }
                else if (item.dirType == 2) { babyDirection = baseDirection; }
                PlantElement baby = new PlantElement(motherPlant, babyPos, item.child, getRandValue(seed + 3 * seedMod), babyDirection);
                childPlantElements.Add(baby);
            }
            public int init((int x, int y)? forceDirection = null)
            {
                childArrayOffset = 1;
                directionArrayOffset = 1;
                modArrayOffset = 1;

                fillStates = new Dictionary<(int x, int y), (int type, int subType)>();
                if (traits.plantGrowthRules != null)
                {
                    childArrayOffset += traits.plantGrowthRules.childOffset;
                    directionArrayOffset += traits.plantGrowthRules.dGOffset;
                    modArrayOffset += traits.plantGrowthRules.pMOffset;

                    if (traits.plantGrowthRules.startDirection != null)
                    {
                        ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped) dir = traits.plantGrowthRules.startDirection.Value;
                        baseDirection = (dir.direction.x * (dir.canBeFlipped.x ? (rand.Next(2) * 2 - 1) : 1), dir.direction.y * (dir.canBeFlipped.y ? (rand.Next(2) * 2 - 1) : 1));

                        if (forceDirection != null) { baseDirection = (baseDirection.x == 0 ? forceDirection.Value.x : baseDirection.x, baseDirection.y == 0 ? forceDirection.Value.y : baseDirection.y); }
                    }
                    else if (forceDirection != null) { baseDirection = forceDirection.Value; }
                    else if (type == (4, 1, 0)) { baseDirection = (0, 0); }  // temp, so mold doesn't get moved lol
                    else if (motherPlant.traits.isCeiling) { lastDrawPos = (0, 1); baseDirection = (0, -1); }
                    else { lastDrawPos = (0, -1); baseDirection = (0, 1); }

                    growthDirection = baseDirection;
                    baseDirection = (baseDirection.x == 0 ? getRandValue(seed + 7, 2) * 2 - 1 : baseDirection.x, baseDirection.y == 0 ? getRandValue(seed + 13, 2) * 2 - 1 : baseDirection.y);

                    if (traits.plantGrowthRules.childrenOnGrowthStart != null && traits.plantGrowthRules.childrenOnGrowthStart.Length > 0)
                    {
                        int offset = 0;
                        foreach (((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType) item in traits.plantGrowthRules.childrenOnGrowthStart)
                        {
                            makeBaby(item, 0, offset);
                            offset += 1;
                        }
                    }
                }

                growthLevel = 0;
                return 2;
            }
            public int tryGrowth()  // 0 -> Growth Failed and ABORT, 1 -> Final growth, won't grow anymore after but SUCCESS, 2 -> Normal growth, SUCCESS and continue after
            {
                if (growthLevel == -1)
                {
                    return init();
                }

                if (traits.frames != null)  // for traits, first growth level at which the plantElement start to develop is at 0. for plantGrowthRules, it is at 1. Be careful
                {
                    if (growthLevel > maxGrowthLevel) { return 0; }    // Fail if already at max growth
                    growthLevel++;
                    if (traits.frames != null && traits.frames.Length != 0 && (/*!traits.frames.loopFrames*/true && currentFrameArrayIdx < traits.frames.Length))
                    {
                        ((int frame, int range) changeFrame, PlantStructureFrame frame) frame = traits.frames[(currentFrameArrayIdx) % traits.frames.Length];

                        int cost = frame.changeFrame.frame + rand.Next(frame.changeFrame.range);
                        if (growthLevel - frameArrayOffset >= cost)
                        {
                            frameArrayOffset += cost;
                            currentFrameArrayIdx++;

                            fillStates = new Dictionary<(int x, int y), (int type, int subType)>();
                            foreach ((int x, int y) pos in frame.frame.elementDict.Keys) { tryFill(pos, frame.frame.elementDict[pos]); }
                            // maybe make it so the growth only succceeds if > half of the newly filled fillStates have been successfully filled ? If not it cancels the growth, keep old fillStates, and doesn't increase growthLevel
                        }
                    }
                    if (growthLevel > maxGrowthLevel) { return 1; }    // If was final growth
                    return 2;
                }
                else if (traits.plantGrowthRules != null)
                {
                    (int x, int y) drawPos = lastDrawPos;
                    int growthLevelToTest = growthLevel + 1;
                    if (growthLevelToTest > maxGrowthLevel + (traits.plantGrowthRules.childrenOnGrowthEnd is null ? 0 : 1)) { goto SuccessButStop; }

                    if (traits.plantGrowthRules.directionGrowthArray != null && traits.plantGrowthRules.directionGrowthArray.Length > 0 && (traits.plantGrowthRules.loopDG || currentDirectionArrayIdx + 1 < traits.plantGrowthRules.directionGrowthArray.Length))
                    {
                        ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame) item = traits.plantGrowthRules.directionGrowthArray[(currentDirectionArrayIdx + 1) % traits.plantGrowthRules.directionGrowthArray.Length];

                        int cost = item.changeFrame.frame + getRandValue(directionArrayOffset + 10000, item.changeFrame.range + 1);
                        if (growthLevelToTest - directionArrayOffset >= cost)
                        {
                            directionArrayOffset += cost;
                            currentDirectionArrayIdx++;

                            growthDirection = (item.direction.x * (item.canBeFlipped.x ? (item.canBeFlipped.independant ? (rand.Next(2) * 2 - 1) : baseDirection.x) : 1), item.direction.y * (item.canBeFlipped.y ? (item.canBeFlipped.independant ? (rand.Next(2) * 2 - 1) : baseDirection.y) : 1));
                        }
                    }   

                    drawPos = (lastDrawPos.x + growthDirection.x, lastDrawPos.y + growthDirection.y);

                    if (traits.plantGrowthRules.growthPosModArray != null && traits.plantGrowthRules.growthPosModArray.Length > 0 && (traits.plantGrowthRules.loopPM || currentModArrayIdx + 1 < traits.plantGrowthRules.growthPosModArray.Length))
                    {
                        ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame) item = traits.plantGrowthRules.growthPosModArray[(currentModArrayIdx + 1) % traits.plantGrowthRules.growthPosModArray.Length];

                        int cost = item.changeFrame.frame + getRandValue(modArrayOffset + 20000, item.changeFrame.range + 1);
                        if (growthLevelToTest - modArrayOffset >= cost)
                        {
                            modArrayOffset += cost;
                            currentModArrayIdx++;

                            drawPos = (drawPos.x + item.mod.x * (item.canBeFlipped.x ? (item.canBeFlipped.independant ? (rand.Next(2) * 2 - 1) : baseDirection.x) : 1), drawPos.y + item.mod.y * (item.canBeFlipped.y ? (item.canBeFlipped.independant ? (rand.Next(2) * 2 - 1) : baseDirection.y) : 1));
                        }
                    }

                    if (traits.plantGrowthRules.childArray != null && traits.plantGrowthRules.childArray.Length > 0 && (traits.plantGrowthRules.loopChild || currentChildArrayIdx + 1 < traits.plantGrowthRules.childArray.Length))
                    {
                        ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, (int frame, int range) birthFrame) item = traits.plantGrowthRules.childArray[(currentChildArrayIdx + 1) % traits.plantGrowthRules.childArray.Length];

                        int cost = item.birthFrame.frame + getRandValue(childArrayOffset + 30000, item.birthFrame.range + 1);
                        if (growthLevelToTest - childArrayOffset >= cost)
                        {
                            childArrayOffset += cost;
                            currentChildArrayIdx++;
                            makeBaby(item, growthLevelToTest);
                        }
                    }
                    // prolly gonna have to move this down
                    foreach (PlantElement child in childPlantElements) { if (child.traits.stickToLastDrawPosOfParent) { child.pos = absolutePos(drawPos); } }

                    if (growthLevelToTest == maxGrowthLevel + 1) // Should only happen when plants has childrenOnGrowthEnd and has done its last growth already
                    {
                        if (traits.plantGrowthRules.childrenOnGrowthEnd != null && traits.plantGrowthRules.childrenOnGrowthEnd.Length > 0 && growthLevelToTest == maxGrowthLevel + 1)
                        {
                            drawPos = lastDrawPos;
                            int offset = 0;
                            foreach (((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType) item in traits.plantGrowthRules.childrenOnGrowthEnd)
                            {
                                makeBaby(item, 1, offset);
                                offset += 1;
                            }
                        }
                        goto SuccessButStop;  // Necessary else might make children again when getting loaded
                    }

                    if (traits.plantGrowthRules.isMold)
                    {
                        if (growthLevel >= maxGrowthLevel) { goto Fail; }    // If overgrown

                        for (int i = 0; i < 1 + (int)(fillStates.Count * 0.2f); i++)
                        {
                            if (fillStates.Count == 0) { drawPos = lastDrawPos; }
                            else
                            {
                                drawPos = getRandomItem(fillStates.Keys.ToList());
                                int rando = rand.Next(5);
                                if (rando != 4) { drawPos = (drawPos.x + neighbourArray[rando].Item1, drawPos.y + neighbourArray[rando].Item2); }
                            }
                            if (tryMoldConversion(drawPos)) { goto Success; }
                            if (!fillStates.ContainsKey(drawPos) && tryFill(drawPos, traits.plantGrowthRules.materalToFillWith)) { goto Success; }
                        }
                        goto FailButContinue;
                    }

                    if (tryFill(drawPos, traits.plantGrowthRules.materalToFillWith))
                    {
                        if (growthLevelToTest >= maxGrowthLevel + (traits.plantGrowthRules.childrenOnGrowthEnd is null ? 0 : 1)) { goto SuccessButStop; }
                        goto Success;
                    }
                    goto Fail;

                Fail:;
                    return 0;

                SuccessButStop:;
                    lastDrawPos = drawPos;
                    growthLevel = growthLevelToTest;
                    return 1;

                Success:;
                    lastDrawPos = drawPos;
                    growthLevel = growthLevelToTest;
                FailButContinue:;
                    return 2;
                }
                return 0;
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
                    if (motherPlant.screen.getTileContent(motherPlant.getRealPos(posToTest)).isSolid) { fullTiles += 1; }
                    if (fillStates.ContainsKey(posToTest)) { moldyTiles += 1; }
                }
                if (moldyTiles + fullTiles >= 4)
                {
                    motherPlant.screen.setTileContent(motherPlant.getRealPos(pos), (5, 0));
                    fillStates.Remove(pos);
                    return true;
                }
                return false;
            }
            public bool tryToMakeParticle()
            {
                if ((type == (4, 1, 0) || true) && fillStates.Count > 0)
                {
                    if (rand.Next(10) == 0)
                    {
                        (int x, int y) pos = motherPlant.getRealPos(fillStates.Keys.ToArray()[rand.Next(fillStates.Count)]);
                        new Particle(motherPlant.screen, pos, pos, (5, 3, 2), motherPlant.id);  // mold particle
                        return true;
                    }
                }
                return false;
            }
            public void updatePos((int x, int y) mod)
            {
                foreach (PlantElement plantElement in childPlantElements)
                {
                    plantElement.pos = (plantElement.pos.x + mod.x, plantElement.pos.y + mod.y);
                    plantElement.updatePos(mod);
                }
                pos = (pos.x + mod.x, pos.y + mod.y);
            }
            public void updatePosSet((int x, int y) posToSet)
            {
                (int x, int y) mod = (posToSet.x - pos.x, posToSet.y - pos.y);
                foreach (PlantElement plantElement in childPlantElements)
                {
                    plantElement.pos = (plantElement.pos.x + mod.x, plantElement.pos.y + mod.y);
                    plantElement.updatePos(mod);
                }
                pos = posToSet;
            }
        }
    }
}
