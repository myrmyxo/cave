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

            public PlantElement[] animatedPlantElements = null;
            public List<(int x, int y, int radius, Color color)> lightPositions = null;
            public int outOfBoundsVisibility = 0;

            public Bitmap bitmap = new Bitmap(1, 1);
            public Bitmap secondaryBitmap = new Bitmap(1, 1);

            public (int x, int y) posOffset = (0, 0);
            public ((int min, int max) x, (int min, int max) y) bounds = ((0, 0), (0, 0));

            public Dictionary<(int x, int y), bool> chunkPresence = new Dictionary<(int x, int y), bool>();

            public bool isDeadAndShouldDisappear = false;
            public bool isStable = false;
            public bool hasBeenModified = false;
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
                plantElement = new PlantElement(this, (0, 0), traits.plantElementType, seed, null, true);
                if (isDeadAndShouldDisappear) { return; }
                tryGrowToMaximum();
                if (isDeadAndShouldDisappear) { return; }
                makeBitmap();
                timeAtLastGrowth = timeElapsed;

                currentPlantId++;
                hasBeenModified = true;
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
                if (!testIfPlantPositionValid()) { isDeadAndShouldDisappear = true; return; }
                plantElement = new PlantElement(this, (0, 0), traits.plantElementType, seed, null, true);
                if (isDeadAndShouldDisappear) { return; }
                testPlantGrowth(true);
                if (isDeadAndShouldDisappear) { return; }
                makeBitmap();
                timeAtLastGrowth = timeElapsed;

                currentPlantId++;
                hasBeenModified = true;
            }
            public int getplantRandValue(int mod = -1)
            {
                randValue = cashInt((seed, 7, 13), 0);
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
                Dictionary<(int x, int y), Color> fillDict = returnFullPlantFillDict(plantElements);
                animatedPlantElements = returnAnimatedPlantElements(plantElements);

                outOfBoundsVisibility = 0;
                if (animatedPlantElements != null) { foreach (PlantElement plantElement in animatedPlantElements) { outOfBoundsVisibility = Max(outOfBoundsVisibility, plantElement.traits.animation.frames[0].Width, plantElement.traits.animation.frames[0].Height); } }

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

                bounds = ((posX + minX, posX + maxX), (posY + minY, posY + maxY));

                Bitmap bitmapToMake;
                bitmap = new Bitmap(width, height);
                bitmapToMake = bitmap;

                foreach ((int x, int y) drawPos in fillDict.Keys)
                {
                    bitmapToMake.SetPixel(drawPos.x - minX, drawPos.y - minY, fillDict[drawPos]);
                }

                posOffset = (minX, minY);

                findChunkPresence(fillDict);
                findLightPositions(plantElements);
                if (lightPositions != null) { foreach((int x, int y, int radius, Color color) item in lightPositions) { outOfBoundsVisibility = Max(outOfBoundsVisibility, item.radius); } }

                testDeath(fillDict);
            }
            public void findChunkPresence(Dictionary<(int x, int y), Color> fillDict)
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
                if (!traits.isLuminous) { lightPositions = null; return; }

                lightPositions = new List<(int x, int y, int radius, Color color)>();

                foreach (PlantElement element in plantElements)
                {
                    if (element.traits.lightRadius == 0 || !colorDict.ContainsKey(element.traits.lightElement)) { continue; }

                    Color col = colorDict[element.traits.lightElement];
                    Color lightColor = Color.FromArgb(255, (col.R + 255) / 2, (col.G + 255) / 2, (col.B + 255) / 2);

                    if (element.traits.forceLightAtPos) { lightPositions.Add((element.pos.x + posX, element.pos.y + posY, element.traits.lightRadius, lightColor)); }
                    foreach ((int x, int y) keyo in element.fillStates.Keys)
                    {
                        if (element.fillStates[keyo] == element.traits.lightElement)
                        {
                            lightPositions.Add((element.pos.x + posX, element.pos.y + posY, element.traits.lightRadius, lightColor));
                        }
                    }
                }
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
                hasBeenModified = true; // Might cause bug l8r lol but prolly not
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
                    if (growthIncreaseInt == 1) { isStable = true; growthLevel++; hasBeenModified = true; return false; }
                    if (growthIncreaseInt == 2) { isStable = false; growthLevel++; hasBeenModified = true; return true; }
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
            public bool testIfPlantPositionValid()
            {
                if (screen.getChunkFromPixelPos((posX, posY)).fillStates[PosMod(posX), PosMod(posY)].isSolid) { return false; } // Full tile -> Fail
                if (traits.isEveryAttach)
                {
                    foreach ((int x, int y) mod in neighbourArray) { if (screen.getTileContent((posX + mod.x, posY + mod.y)).isSolid) { return true; } }
                    return false;
                }
                else if (traits.isSide) { if (!screen.getChunkFromPixelPos((posX - 1, posY)).fillStates[PosMod(posX - 1), PosMod(posY)].isAir || !screen.getChunkFromPixelPos((posX + 1, posY)).fillStates[PosMod(posX + 1), PosMod(posY)].isAir) { return true; } }   // tile left XOR right full -> Success
                else if (screen.getChunkFromPixelPos((posX, posY + (traits.isCeiling ? 1 : -1))).fillStates[PosMod(posX), PosMod(posY + (traits.isCeiling ? 1 : -1))].isSolid) { return true; }    // tile under/over full -> Success
                return false;
            }
            public PlantElement[] returnAnimatedPlantElements(List<PlantElement> plantElements = null)
            {
                if (plantElements is null) { plantElements = returnAllPlantElements(); }
                foreach (PlantElement element in plantElements) { if (element.traits.animation != null) { goto animatedPEFound; } }
                return null;
            animatedPEFound:;
                List<PlantElement> animatedPlantElementList = new List<PlantElement>();
                foreach (PlantElement element in plantElements) { if (element.traits.animation != null) { animatedPlantElementList.Add(element); } }
                return animatedPlantElementList.ToArray();
            }
            public Dictionary<(int x, int y), Color> returnFullPlantFillDict(List<PlantElement> plantElements = null)
            {
                if (plantElements is null) { plantElements = returnAllPlantElements(); }
                Dictionary<(int x, int y), Color> fillDict = new Dictionary<(int x, int y), Color>();
                foreach (PlantElement element in plantElements)
                {
                    foreach ((int x, int y) keyo in element.fillStates.Keys)
                    {
                        (int type, int subType) material = element.fillStates[keyo];
                        if (element.colorOverrideDict != null && element.colorOverrideDict.ContainsKey(material)) { fillDict[(keyo.x + element.pos.x, keyo.y + element.pos.y)] = element.colorOverrideDict[material]; }
                        else { fillDict[(keyo.x + element.pos.x, keyo.y + element.pos.y)] = colorDict[material]; }
                    }
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
                            hasBeenModified = true;
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
                    foreach (((int type, int subType) type, ColorRange colorRange) item in traits.colorOverrideArray) { tryAddMaterialColor(item.type, item.colorRange); }
                }
            }
            public void testDeath(Dictionary<(int x, int y), Color> fillDict)
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
            
            public float maxGrowthLevel;
            public int growthLevel;
            public float growthSpeedVariation;

            public (int x, int y) pos;

            public (int x, int y) lastDrawPos = (0, 0);
            public (int x, int y) growthDirection = (0, 0);
            public (int x, int y) baseDirection = (0, 0);

            public int currentFrameArrayIdx = 0;
            public int frameArrayOffset = 0;

            public int currentChildArrayIdx = -1;
            public int childArrayOffset = 1;
            public int currentDirectionArrayIdx = -1;
            public int directionArrayOffset = 1;
            public int currentModArrayIdx = -1;
            public int modArrayOffset = 1;

            public Dictionary<(int type, int subType), Color> colorOverrideDict = null;
            public Dictionary<(int x, int y), (int type, int subType)> fillStates = new Dictionary<(int x, int y), (int type, int subType)>();

            public bool isDeadAndShouldDisappear = false;
            public PlantElement(Plant motherPlantToPut, PlantElementJson plantElementJson)
            {
                motherPlant = motherPlantToPut;

                seed = plantElementJson.s;
                type = plantElementJson.t;

                getTraitAndAddColors();

                maxGrowthLevel = plantElementJson.mG;
                growthLevel = plantElementJson.gR;

                pos = plantElementJson.pos;

                lastDrawPos = plantElementJson.lGP;
                growthDirection = plantElementJson.gD;
                baseDirection = plantElementJson.bD;

                currentFrameArrayIdx = plantElementJson.oIA[0];
                frameArrayOffset = plantElementJson.oIA[1];
                currentChildArrayIdx = plantElementJson.oIA[2];
                childArrayOffset = plantElementJson.oIA[3];
                currentDirectionArrayIdx = plantElementJson.oIA[4];
                directionArrayOffset = plantElementJson.oIA[5];
                currentModArrayIdx = plantElementJson.oIA[6];
                modArrayOffset = plantElementJson.oIA[7];

                fillStates = arrayToFillstates(plantElementJson.fS);

                foreach (PlantElementJson baby in plantElementJson.pEs) { childPlantElements.Add(new PlantElement(motherPlant, baby)); }
            }
            public PlantElement(Plant motherPlantToPut, (int x, int y) posToPut, (int type, int subType, int subSubType) typeToPut, int seedToPut, PlantElement motherPlantElement, bool isMainPlantElement = false, (int x, int y)? forceDirection = null)
            {
                motherPlant = motherPlantToPut;
                pos = posToPut;
                type = typeToPut;
                seed = seedToPut;
                getTraitAndAddColorsAndFindMaxGrowth(motherPlantElement);
                fillStates = new Dictionary<(int x, int y), (int type, int subType)>();
                growthLevel = -1;
                isDeadAndShouldDisappear = init(isMainPlantElement, forceDirection) == 0;
                motherPlant.isDeadAndShouldDisappear = isMainPlantElement ? isDeadAndShouldDisappear : motherPlant.isDeadAndShouldDisappear;
            }
            public int getRandValue(int valueModifier, int modulo = -1)
            {
                int randy = cashInt((valueModifier, 7, 13), seed);
                if (modulo == 0) { return 0; }
                return modulo > 0 ? randy % modulo : randy;
            }
            public void transitionToOtherPlantElement((int type, int subType, int subSubType) newType)  // if new plant element doesn't contains a material that the old one did, it might cause bug on reload !
            {   // CAREFUL when transitioning, it will NOT be able to add the maxGrowthParentRelatedVariation related to MotherPlantElement cuz it's not passed in the function !!!
                seed = Abs((int)LCGyNeg(seed));

                currentFrameArrayIdx = 0;
                frameArrayOffset = 0;

                currentChildArrayIdx = -1;
                childArrayOffset = 1;
                currentDirectionArrayIdx = -1;
                directionArrayOffset = 1;
                currentModArrayIdx = -1;
                modArrayOffset = 1;

                growthLevel = -1;    // not -1 cuz it's uhhh yeah !!! It's gotten init()ed before so it's 0 actually !!!!!!!!

                type = newType;
                getTraitAndAddColorsAndFindMaxGrowth();
                init(false, null, true);
            }
            public void getTraitAndAddColors()
            {
                traits = getPlantElementTraits(type);
                foreach ((int type, int subType) material in traits.materialsPresent) { motherPlant.tryAddMaterialColor(material); }
                if (traits.lightRadius > 0) { motherPlant.tryAddMaterialColor(traits.lightElement); }
                makeColorOverrideDict();
            }
            public void getTraitAndAddColorsAndFindMaxGrowth(PlantElement motherPlantElement = null)
            {
                traits = getPlantElementTraits(type);
                int maxGrowthLevelVariation = seed % (traits.maxGrowth.range + 1);
                maxGrowthLevel = traits.maxGrowth.maxLevel + maxGrowthLevelVariation;
                if (motherPlantElement != null && traits.maxGrowthParentRelatedVariation != null)
                {
                    if (traits.maxGrowthParentRelatedVariation.Value.fromEnd) { maxGrowthLevel += traits.maxGrowthParentRelatedVariation.Value.step * (motherPlantElement.maxGrowthLevel - motherPlantElement.growthLevel); }
                    else { maxGrowthLevel += traits.maxGrowthParentRelatedVariation.Value.step * motherPlantElement.growthLevel; }
                }
                growthSpeedVariation = traits.plantGrowthRules is null ? 1 : traits.plantGrowthRules.growthSpeedVariationFactor.baseValue + (float)(rand.NextDouble() * traits.plantGrowthRules.growthSpeedVariationFactor.variation);
                if (traits.plantGrowthRules != null && traits.plantGrowthRules.offsetMaxGrowthVariation)
                {
                    directionArrayOffset += (int)(growthSpeedVariation * maxGrowthLevelVariation);
                    childArrayOffset += (int)(growthSpeedVariation * maxGrowthLevelVariation);
                    modArrayOffset += (int)(growthSpeedVariation * maxGrowthLevelVariation);
                    frameArrayOffset += (int)(growthSpeedVariation * maxGrowthLevelVariation);
                }
                foreach ((int type, int subType) material in traits.materialsPresent) { motherPlant.tryAddMaterialColor(material); }
                if (traits.lightRadius > 0) { motherPlant.tryAddMaterialColor(traits.lightElement); }
                makeColorOverrideDict();
            }
            public void makeColorOverrideDict()
            {
                if (traits.colorOverrideArray is null) { return; }
                colorOverrideDict = new Dictionary<(int type, int subType), Color>();
                foreach (((int type, int subType) type, ColorRange colorRange) tuple in traits.colorOverrideArray)
                {
                    ColorRange c = tuple.colorRange;
                    if (c is null) { c = colorOverrideOfTypeIfPresentInMotherPlant(tuple.type) ?? getMaterialTraits(tuple.type).colorRange; }
                    float hueVar = (float)((seed % 11) * 0.2f - 1);
                    float shadeVar = (float)((LCGz(seed) % 11) * 0.2f - 1);
                    colorOverrideDict[tuple.type] = Color.FromArgb(
                        ColorClamp(c.r.v + (int)(hueVar * c.r.h) + (int)(shadeVar * c.r.s)),
                        ColorClamp(c.g.v + (int)(hueVar * c.g.h) + (int)(shadeVar * c.g.s)),
                        ColorClamp(c.b.v + (int)(hueVar * c.b.h) + (int)(shadeVar * c.b.s))
                    );
                }
            }
            public ColorRange colorOverrideOfTypeIfPresentInMotherPlant((int type, int subType) type)
            {
                foreach (((int type, int subType) type, ColorRange colorRange) tuple in motherPlant.traits.colorOverrideArray)
                {
                    if (tuple.type == type) { return tuple.colorRange; }
                }
                return null;
            }
            public (int x, int y) absolutePos((int x, int y) position)
            {
                return (pos.x + position.x, pos.y + position.y);
            }
            public (int x, int y) getWorldPos((int x, int y) position)
            {
                return (pos.x + position.x + motherPlant.posX, pos.y + position.y + motherPlant.posY);
            }
            public TileTraits getTileFromRelPos((int x, int y) testPos)
            {
                return motherPlant.screen.getTileContent(getWorldPos(testPos));
            }
            public bool testPositionEmpty((int x, int y) testPos)
            {
                if (motherPlant.testIfPositionEmpty(absolutePos(testPos))) { return true; }
                return false;
            }
            public bool tryFill((int x, int y) testPos, (int type, int subType) typeToFill)
            {
                if (motherPlant.testIfPositionEmpty(absolutePos(testPos))) { fillStates[testPos] = typeToFill; return true; }
                return false;
            }
            public bool makeBaby(((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance) item, int seedMod, int offset)
            {
                (int x, int y) babyPos = item.dirType != 0 ? absolutePos(lastDrawPos) : absolutePos((lastDrawPos.x + item.mod.x, lastDrawPos.y + item.mod.y));
                (int x, int y)? babyDirection = null;
                if (item.dirType == 1) { babyDirection = item.mod; }
                else if (item.dirType == 2) { babyDirection = baseDirection; }
                PlantElement baby = new PlantElement(motherPlant, babyPos, item.child, getRandValue(seed + 923147 * seedMod + offset * 10000), this, false, babyDirection);
                if (baby.isDeadAndShouldDisappear)
                {
                    maxGrowthLevel += item.failMGIncrease;
                    return false;
                }
                childPlantElements.Add(baby);
                return true;
            }
            public bool makeBaby(((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance) item, int seedMod)
            {
                (int x, int y) babyPos = item.dirType != 0 ? absolutePos(lastDrawPos) : absolutePos((lastDrawPos.x + item.mod.x, lastDrawPos.y + item.mod.y));
                (int x, int y)? babyDirection = null;
                if (item.dirType == 1) { babyDirection = item.mod; }
                else if (item.dirType == 2) { babyDirection = baseDirection; }
                PlantElement baby = new PlantElement(motherPlant, babyPos, item.child, getRandValue(seed + 3 * seedMod), this, false, babyDirection);
                if (baby.isDeadAndShouldDisappear)
                {
                    maxGrowthLevel += item.failMGIncrease;
                    return false;
                }
                childPlantElements.Add(baby);
                return true;
            }
            public int findBaseDirection((int x, int y)? forceDirection)
            {
                if (traits.plantGrowthRules != null && traits.plantGrowthRules.startDirection != null)
                {
                    ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped) dir = traits.plantGrowthRules.startDirection.Value;
                    baseDirection = (dir.direction.x * (dir.canBeFlipped.x ? (rand.Next(2) * 2 - 1) : 1), dir.direction.y * (dir.canBeFlipped.y ? (rand.Next(2) * 2 - 1) : 1));

                    if (forceDirection != null) { baseDirection = (baseDirection.x == 0 ? forceDirection.Value.x : baseDirection.x, baseDirection.y == 0 ? forceDirection.Value.y : baseDirection.y); }
                }
                else if (forceDirection != null) { baseDirection = forceDirection.Value; }
                else if (motherPlant.traits.isEveryAttach)
                {
                    if (!testPositionEmpty((0, -1))) { baseDirection = (baseDirection.x, 1); }
                    else if (!testPositionEmpty((0, 1))) { baseDirection = (baseDirection.x, -1); }
                    if (!testPositionEmpty((-1, 0))) { baseDirection = (1, baseDirection.y); }
                    else if (!testPositionEmpty((1, 0))) { baseDirection = (-1, baseDirection.y); }
                    if (baseDirection == (0, 0)) { return 0; }  // Plant was floating. Wadafak.
                }
                else if (type == (4, 1, 0)) { baseDirection = (0, 0); }  // temp, so mold doesn't get moved lol
                else if (motherPlant.traits.isCeiling) { baseDirection = (0, -1); }
                else if (motherPlant.traits.isSide)
                {
                    if (testPositionEmpty((-1, 0)))
                    {
                        if (testPositionEmpty((1, 0))) { return 0; }    // Both sides were empty, should not happen but in case plant die
                        baseDirection = (-1, 0);
                    }
                    else if (testPositionEmpty((1, 0))) { baseDirection = (1, 0); }
                    else { return 0; }  // Both sides were not empty, plant sandwiched lol, for now plant die but later might change idk
                }
                else { baseDirection = (0, 1); }

                growthDirection = baseDirection;
                baseDirection = (baseDirection.x == 0 ? getRandValue(seed + 7, 2) * 2 - 1 : baseDirection.x, baseDirection.y == 0 ? getRandValue(seed + 13, 2) * 2 - 1 : baseDirection.y);

                return -1;  // Return -1 to say everything went good lol
            }
            public int init(bool isMainPlantElement = false, (int x, int y)? forceDirection = null, bool isTransition = false)
            {
                if (!isTransition) { fillStates = new Dictionary<(int x, int y), (int type, int subType)>(); }
                if (findBaseDirection(forceDirection) == 0) { return 0; }
                if (traits.plantGrowthRules != null)
                {
                    childArrayOffset += traits.plantGrowthRules.childOffset;
                    directionArrayOffset += traits.plantGrowthRules.dGOffset;
                    modArrayOffset += traits.plantGrowthRules.pMOffset;

                    if (!isTransition)
                    {
                        if (!isMainPlantElement) { updatePos(growthDirection); } // if the first plantElement created by the plant (like the trunk of a tree, stem of a flower...)
                        lastDrawPos = (-growthDirection.x, -growthDirection.y);
                    }

                    if (traits.plantGrowthRules.childrenOnGrowthStart != null && traits.plantGrowthRules.childrenOnGrowthStart.Length > 0)
                    {
                        int offset = 0;
                        foreach (((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance) item in traits.plantGrowthRules.childrenOnGrowthStart)
                        {
                            if (item.chance != 100 && getRandValue((int)maxGrowthLevel + childArrayOffset + 20000, 100) > item.chance) { continue; }
                            makeBaby(item, 0, traits.plantGrowthRules.mirrorTwinChildren ? 0 : offset);
                            offset += 1;
                        }
                    }
                }

                if (traits.requiredEmptyTiles != null)
                {
                    if (traits.plantGrowthRules is null || traits.plantGrowthRules.tileContentNeededToGrow is null)
                    {
                        foreach (((int x, int y) pos, (bool x, bool y) baseDirectionFlip) item in traits.requiredEmptyTiles)
                        {
                            if (!testPositionEmpty((item.pos.x * (item.baseDirectionFlip.x && baseDirection.x < 0 ? -1 : 1), item.pos.y * (item.baseDirectionFlip.y && baseDirection.y < 0 ? -1 : 1))))
                            {
                                growthLevel = -1;
                                return 0;
                            }
                        }
                    }
                    else
                    {
                        foreach (((int x, int y) pos, (bool x, bool y) baseDirectionFlip) item in traits.requiredEmptyTiles)
                        {
                            if (getTileFromRelPos((item.pos.x * (item.baseDirectionFlip.x && baseDirection.x < 0 ? -1 : 1), item.pos.y * (item.baseDirectionFlip.y && baseDirection.y < 0 ? -1 : 1))).type != traits.plantGrowthRules.tileContentNeededToGrow.Value)
                            {
                                growthLevel = -1;
                                return 0;
                            }
                        }
                    }
                }
                if (traits.specificRequiredEmptyTiles != null)
                {
                    foreach (((int x, int y) pos, (int type, int subType) type, (bool x, bool y) baseDirectionFlip) item in traits.specificRequiredEmptyTiles)
                    {
                        if (getTileFromRelPos((item.pos.x * (item.baseDirectionFlip.x && baseDirection.x < 0 ? -1 : 1), item.pos.y * (item.baseDirectionFlip.y && baseDirection.y < 0 ? -1 : 1))).type != item.type)
                        {
                            growthLevel = -1;
                            return 0;
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
                    growthLevel = 0;
                    return 2;  // should NEVER happen ??
                }

                if (traits.frames != null)  // for traits, first growth level at which the plantElement start to develop is at 0. for plantGrowthRules, it is at 1. Be careful
                {
                    if (growthLevel > maxGrowthLevel) { return 0; }    // Fail if already at max growth
                    growthLevel++;
                    if (traits.frames != null && traits.frames.Length != 0 && (/*!traits.frames.loopFrames*/true && currentFrameArrayIdx < traits.frames.Length))
                    {
                        ((int frame, int range) changeFrame, PlantStructureFrame frame) frame = traits.frames[(currentFrameArrayIdx) % traits.frames.Length];

                        int cost = (int)(growthSpeedVariation * (frame.changeFrame.frame + rand.Next(frame.changeFrame.range)));
                        if (growthLevel - frameArrayOffset >= cost)
                        {
                            frameArrayOffset += cost;
                            currentFrameArrayIdx++;

                            fillStates = new Dictionary<(int x, int y), (int type, int subType)>();
                            foreach ((int x, int y) pos in frame.frame.elementDict.Keys) { tryFill((pos.x * (frame.frame.directionalFlip.x ? baseDirection.x : 1), pos.y * (frame.frame.directionalFlip.y ? baseDirection.y : 1)), frame.frame.elementDict[pos]); }
                            // maybe make it so the growth only succceeds if > half of the newly filled fillStates have been successfully filled ? If not it cancels the growth, keep old fillStates, and doesn't increase growthLevel
                        }
                    }
                    if (growthLevel > maxGrowthLevel)   // If was final growth
                    {
                        if (traits.deathChild != null && (traits.deathChild.Value.chance == 100 || getRandValue((int)maxGrowthLevel + childArrayOffset + 20000, 100) <= traits.deathChild.Value.chance)) { makeBaby((traits.deathChild.Value.plantElement, traits.deathChild.Value.offset, 0, 0, 100), growthLevel, 1); }
                        if (traits.transitionToOtherPlantElementOnGrowthEnd != null)
                        {
                            transitionToOtherPlantElement(traits.transitionToOtherPlantElementOnGrowthEnd.Value);
                            return 2;
                        }
                        return 1;
                    }
                    return 2;
                }
                else if (traits.plantGrowthRules != null)
                {
                    (int x, int y) drawPos = lastDrawPos;
                    int growthLevelToTest = growthLevel + 1;
                    if (growthLevelToTest > maxGrowthLevel + (traits.plantGrowthRules.childrenOnGrowthEnd is null ? 0 : 1)) { goto SuccessButStop; }

                    if (traits.plantGrowthRules.directionGrowthArray != null && traits.plantGrowthRules.directionGrowthArray.Length > 0 && (traits.plantGrowthRules.loopDG || currentDirectionArrayIdx + 1 < traits.plantGrowthRules.directionGrowthArray.Length))
                    {
                        ((int x, int y) direction, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance) item = traits.plantGrowthRules.directionGrowthArray[(currentDirectionArrayIdx + 1) % traits.plantGrowthRules.directionGrowthArray.Length];

                        int cost = (int)(growthSpeedVariation * (item.changeFrame.frame + getRandValue(directionArrayOffset + 10000, item.changeFrame.range + 1)));
                        if (growthLevelToTest - directionArrayOffset >= cost)
                        {
                            directionArrayOffset += cost;
                            currentDirectionArrayIdx++;

                            if (item.chance == 100 || getRandValue(growthLevelToTest + directionArrayOffset + 20000, 100) <= item.chance)
                            {
                                if (traits.plantGrowthRules.rotationalDG) { growthDirection = directionPositionArray[PosMod(directionPositionDictionary[growthDirection] + item.direction.x * (item.canBeFlipped.x ? baseDirection.x : 1), 8)]; }
                                else { growthDirection = (item.direction.x * (item.canBeFlipped.x ? (item.canBeFlipped.independant ? (rand.Next(2) * 2 - 1) : baseDirection.x) : 1), item.direction.y * (item.canBeFlipped.y ? (item.canBeFlipped.independant ? (rand.Next(2) * 2 - 1) : baseDirection.y) : 1)); }
                            }
                        }
                    }   

                    drawPos = (lastDrawPos.x + growthDirection.x, lastDrawPos.y + growthDirection.y);

                    if (traits.plantGrowthRules.growthPosModArray != null && traits.plantGrowthRules.growthPosModArray.Length > 0 && (traits.plantGrowthRules.loopPM || currentModArrayIdx + 1 < traits.plantGrowthRules.growthPosModArray.Length))
                    {
                        ((int x, int y) mod, (bool x, bool y, bool independant) canBeFlipped, (int frame, int range) changeFrame, int chance) item = traits.plantGrowthRules.growthPosModArray[(currentModArrayIdx + 1) % traits.plantGrowthRules.growthPosModArray.Length];

                        int cost = (int)(growthSpeedVariation * (item.changeFrame.frame + getRandValue(modArrayOffset + 20000, item.changeFrame.range + 1)));
                        if (growthLevelToTest - modArrayOffset >= cost)
                        {
                            modArrayOffset += cost;
                            currentModArrayIdx++;

                            if (item.chance == 100 || getRandValue(growthLevelToTest + modArrayOffset + 20000, 100) <= item.chance) { drawPos = (drawPos.x + item.mod.x * (item.canBeFlipped.x ? (item.canBeFlipped.independant ? (rand.Next(2) * 2 - 1) : baseDirection.x) : 1), drawPos.y + item.mod.y * (item.canBeFlipped.y ? (item.canBeFlipped.independant ? (rand.Next(2) * 2 - 1) : baseDirection.y) : 1)); }
                        }
                    }

                    if (traits.plantGrowthRules.preventGaps)
                    {
                        if (Abs(drawPos.x - lastDrawPos.x) > 1) { drawPos = (lastDrawPos.x + Sign(drawPos.x - lastDrawPos.x), drawPos.y); }
                        if (Abs(drawPos.y - lastDrawPos.y) > 1) { drawPos = (drawPos.x, lastDrawPos.y + Sign(drawPos.y - lastDrawPos.y)); }
                    }

                    if (traits.plantGrowthRules.hindrancePreventionPositions != null)
                    {
                        HashSet<(int x, int y)> posToRemove = new HashSet<(int x, int y)>();
                        (int x, int y) worldPos = getWorldPos(drawPos);
                        motherPlant.screen.game.miscDebugList.Add((worldPos, Color.PeachPuff));
                        if (drawPos.x < lastDrawPos.x && traits.plantGrowthRules.hindrancePreventionPositions.Value.left != null)
                        {
                            foreach ((int x, int y, bool stopGrowth) poso in traits.plantGrowthRules.hindrancePreventionPositions.Value.left)
                            {
                                motherPlant.screen.game.miscDebugList.Add(((worldPos.x + poso.x, worldPos.y + poso.y), Color.Red));
                                if (motherPlant.screen.getTileContent((worldPos.x + poso.x, worldPos.y + poso.y)).isSolid)
                                {
                                    if (poso.stopGrowth) { goto Fail; }
                                    else { posToRemove.Add((poso.x, poso.y)); }
                                }
                            }
                        }
                        else if (drawPos.x > lastDrawPos.x && traits.plantGrowthRules.hindrancePreventionPositions.Value.right != null)
                        {
                            foreach ((int x, int y, bool stopGrowth) poso in traits.plantGrowthRules.hindrancePreventionPositions.Value.right)
                            {
                                motherPlant.screen.game.miscDebugList.Add(((worldPos.x + poso.x, worldPos.y + poso.y), Color.Red));
                                if (motherPlant.screen.getTileContent((worldPos.x + poso.x, worldPos.y + poso.y)).isSolid)
                                {
                                    if (poso.stopGrowth) { goto Fail; }
                                    else { posToRemove.Add((poso.x, poso.y)); }
                                }
                            }
                        }
                        if (drawPos.y < lastDrawPos.y && traits.plantGrowthRules.hindrancePreventionPositions.Value.down != null)
                        {
                            foreach ((int x, int y, bool stopGrowth) poso in traits.plantGrowthRules.hindrancePreventionPositions.Value.down)
                            {
                                motherPlant.screen.game.miscDebugList.Add(((worldPos.x + poso.x, worldPos.y + poso.y), Color.Red));
                                if (motherPlant.screen.getTileContent((worldPos.x + poso.x, worldPos.y + poso.y)).isSolid)
                                {
                                    if (poso.stopGrowth) { goto Fail; }
                                    else { posToRemove.Add((poso.x, poso.y)); }
                                }
                            }
                        }
                        else if (drawPos.y > lastDrawPos.y && traits.plantGrowthRules.hindrancePreventionPositions.Value.up != null)
                        {
                            foreach ((int x, int y, bool stopGrowth) poso in traits.plantGrowthRules.hindrancePreventionPositions.Value.up)
                            {
                                motherPlant.screen.game.miscDebugList.Add(((worldPos.x + poso.x, worldPos.y + poso.y), Color.Red));
                                if (motherPlant.screen.getTileContent((worldPos.x + poso.x, worldPos.y + poso.y)).isSolid)
                                {
                                    if (poso.stopGrowth) { goto Fail; }
                                    else { posToRemove.Add((poso.x, poso.y)); }
                                }
                            }
                        }
                        foreach ((int x, int y) pos in posToRemove) { foreach (PlantElement child in childPlantElements) { if (child.traits.isSticky != null) { child.fillStates.Remove(pos); } } } // Crucial
                    }


                    if (traits.plantGrowthRules.childArray != null && traits.plantGrowthRules.childArray.Length > 0 && (traits.plantGrowthRules.loopChild || currentChildArrayIdx + 1 < traits.plantGrowthRules.childArray.Length))
                    {
                        ((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, (int frame, int range) birthFrame, int chance) item = traits.plantGrowthRules.childArray[(currentChildArrayIdx + 1) % traits.plantGrowthRules.childArray.Length];

                        int cost = (int)(growthSpeedVariation * (item.birthFrame.frame + getRandValue(childArrayOffset + 30000, item.birthFrame.range + 1)));
                        if (growthLevelToTest - childArrayOffset >= cost)
                        {   // This part here will make it so it the baby chance is not 100 AND fails it still proceeds in the array even without spawning the bebe
                            if ((item.chance < 100 && getRandValue(growthLevelToTest + childArrayOffset + 20000, 100) > item.chance) || makeBaby(item, growthLevelToTest))  // Only increase cost and position when the growth of the child succeeds. Might add that as a parameter idk to make some kids skippable
                            {
                                currentChildArrayIdx++;
                                childArrayOffset += cost;
                            }
                        }
                    }


                    if (growthLevelToTest == maxGrowthLevel + 1) // Should only happen when plants has childrenOnGrowthEnd and has done its last growth already
                    {
                        if (traits.plantGrowthRules.childrenOnGrowthEnd != null && traits.plantGrowthRules.childrenOnGrowthEnd.Length > 0 && growthLevelToTest == maxGrowthLevel + 1)
                        {
                            drawPos = lastDrawPos;
                            int offset = 0;
                            foreach (((int type, int subType, int subSubType) child, (int x, int y) mod, int dirType, float failMGIncrease, int chance) item in traits.plantGrowthRules.childrenOnGrowthEnd)
                            {
                                if (item.chance != 100 && getRandValue((int)maxGrowthLevel + childArrayOffset + 20000, 100) > item.chance) { continue; }
                                makeBaby(item, 1, traits.plantGrowthRules.mirrorTwinChildren ? 0 : offset);
                                offset += 1;
                            }
                        }
                        if (traits.transitionToOtherPlantElementOnGrowthEnd != null)
                        {
                            lastDrawPos = drawPos;
                            transitionToOtherPlantElement(traits.transitionToOtherPlantElementOnGrowthEnd.Value);
                            return 2;   // return not goto !! improtant ig ?
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

                    if ((traits.plantGrowthRules.tileContentNeededToGrow is null || traits.plantGrowthRules.tileContentNeededToGrow.Value == motherPlant.screen.getTileContent(motherPlant.getRealPos(drawPos)).type) && tryFill(drawPos, traits.plantGrowthRules.materalToFillWith))
                    {
                        updateStickyChildren(drawPos);
                        if (growthLevelToTest >= maxGrowthLevel + (traits.plantGrowthRules.childrenOnGrowthEnd is null ? 0 : 1))
                        {
                            if (traits.transitionToOtherPlantElementOnGrowthEnd != null)
                            {
                                lastDrawPos = drawPos;
                                transitionToOtherPlantElement(traits.transitionToOtherPlantElementOnGrowthEnd.Value);
                                return 2;   // return not goto !! improtant ig ?
                            }
                            goto SuccessButStop;
                        }
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
            public void updateStickyChildren((int x, int y) newPos)
            {
                foreach (PlantElement child in childPlantElements)
                {
                    if (child.traits.isSticky != null)
                    {
                        child.pos = absolutePos(newPos);
                        child.pos = (child.pos.x + child.traits.isSticky.Value.pos.x * (child.traits.isSticky.Value.flip.x && baseDirection.x < 0 ? -1 : 1), child.pos.y + child.traits.isSticky.Value.pos.y * (child.traits.isSticky.Value.flip.y && baseDirection.y < 0 ? -1 : 1));
                        child.updateStickyChildren(child.lastDrawPos);
                    }
                }
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
