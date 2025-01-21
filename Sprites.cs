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

namespace Cave
{
    public class Sprites
    {
        public static Dictionary<(int, int, int, int), Bitmap> lightBitmaps;

        public static Dictionary<(int, int), OneSprite> tileSprites;
        public static Dictionary<(int, int), OneSprite> entitySprites;
        public static Dictionary<(int, int), OneSprite> plantSprites;
        public static Dictionary<(int, int), OneSprite> materialSprites;
        public static Dictionary<(int, int), OneSprite> toolsSprites;
        public static OneSprite numbersSprite;
        public static Dictionary<int, OneSprite> numberSprites;
        public static OneSprite lettersUpSprite;
        public static Dictionary<int, OneSprite> letterUpSprites;
        public static OneSprite lettersLowSprite;
        public static Dictionary<int, OneSprite> letterLowSprites;
        public static OneSprite arrowsSprite;
        public static Dictionary<int, OneSprite> arrowSprites;
        public static OneSprite operationSignsSprite;
        public static Dictionary<int, OneSprite> operationSignSprites;
        public static OneSprite overlayBackground;

        public static OneAnimation fireAnimation;
        public static OneAnimation livingPortalAnimation;
        public static void loadSpriteDictionaries()
        {
            tileSprites = new Dictionary<(int, int), OneSprite>
            {
                { (-7, 0), new OneSprite("Acid", true)},                     // TO CHANGE
                { (-6, 1), new OneSprite("DeoxygenatedBlood", true)},        // TO CHANGE
                { (-6, 0), new OneSprite("Blood", true)},                    // TO CHANGE
                { (-5, 0), new OneSprite("Honey", false)},                   
                { (-4, 0), new OneSprite("Lava", false)},                    
                { (-3, 0), new OneSprite("FairyLiquid", false)},             
                { (-2, 0), new OneSprite("Water", false)},                   
                { (-1, 0), new OneSprite("Piss", false)},                    
                { (1, 0), new OneSprite("BasicTile", false)},                
                { (1, 1), new OneSprite("DenseRockTile", true)},             // TO CHANGE
                { (2, 0), new OneSprite("BasicTile", false)},                
                { (3, 0), new OneSprite("PlantMatter", false)},              
                { (4, 0), new OneSprite("FleshTile", true)},                 // TO CHANGE
                { (4, 1), new OneSprite("BoneTile", true)},                  // TO CHANGE
                { (5, 0), new OneSprite("MoldTile", true)},                  // TO CHANGE          
                { (6, 0), new OneSprite("BasicTile", false)}                 
            };                                                               
            entitySprites = new Dictionary<(int, int), OneSprite>            
            {                                                                
                { (0, 0), new OneSprite("Fairy", false)},                    
                { (0, 1), new OneSprite("ObsidianFairy", false)},            
                { (0, 2), new OneSprite("FrostFairy", false)},               
                { (0, 3), new OneSprite("SkeletonFairy", true)},             // TO CHANGE
                { (1, 0), new OneSprite("Frog", false)},                     
                { (1, 1), new OneSprite("Carnal", true)},                    // TO CHANGE
                { (1, 2), new OneSprite("Skeletal", true)},                  // TO CHANGE
                { (2, 0), new OneSprite("Fish", false)},                     
                { (2, 1), new OneSprite("SkeletonFish", true)},              // TO CHANGE
                { (3, 0), new OneSprite("HornetEgg", true)},                 // TO CHANGE 
                { (3, 1), new OneSprite("HornetLarva", true)},               // TO CHANGE
                { (3, 2), new OneSprite("HornetCocoon", true)},              // TO CHANGE
                { (3, 3), new OneSprite("Hornet", false)},                   
                { (4, 0), new OneSprite("Worm", true)},                      // TO CHANGE
                { (4, 1), new OneSprite("Nematode", true)},                  // TO CHANGE
                { (5, 0), new OneSprite("WaterSkipper", true)},              // TO CHANGE
            };                                                               
            plantSprites = new Dictionary<(int, int), OneSprite>             
            {                                                                
                { (0, 0), new OneSprite("BasePlant", false)},                
                { (0, 1), new OneSprite("Candle", false)},                   
                { (0, 2), new OneSprite("Tulip", false)},                    
                { (0, 3), new OneSprite("Allium", false)},                   
                { (1, 0), new OneSprite("Tree", false)},                     
                { (1, 1), new OneSprite("ChandelierTree", false)},           
                { (2, 0), new OneSprite("KelpUpwards", false)},              
                { (2, 1), new OneSprite("KelpDownwards", false)},            
                { (3, 0), new OneSprite("ObsidianPlant", false)},
                { (4, 0), new OneSprite("Mushroom", false)},
                { (4, 1), new OneSprite("Mold", true)},                      // TO CHANGE
                { (5, 0), new OneSprite("Vines", false)},
                { (5, 1), new OneSprite("ObsidianVines", false)}
            };
            materialSprites = new Dictionary<(int, int), OneSprite>
            {
                { (1, 0), new OneSprite("PlantMatter", false)},
                { (1, 1), new OneSprite("Wood", false)},
                { (1, 2), new OneSprite("Kelp", false)},
                { (2, 0), new OneSprite("FlowerPetal", false)},
                { (2, 1), new OneSprite("Pollen", false)},
                { (3, 0), new OneSprite("MushroomStem", false)},
                { (3, 1), new OneSprite("MushroomCap", false)},
                { (3, 2), new OneSprite("Mold", true)},                      // TO CHANGE
                { (8, 0), new OneSprite("Flesh", true)},                     // TO CHANGE
                { (8, 1), new OneSprite("Bone", true )},                     // TO CHANGE
                { (10, 0), new OneSprite("MagicRock", true)},                // TO CHANGE
                { (11, 0), new OneSprite("Metal", true )},                   // TO CHANGE
                { (11, 1), new OneSprite("LightBulb", true )},               // TO CHANGE
                { (12, 0), new OneSprite("Wax", true )},                     // TO CHANGE
            };
            toolsSprites = new Dictionary<(int, int), OneSprite>
            {
                { (0, 0), new OneSprite("Sword", true)},                     // TO CHANGE
                { (1, 0), new OneSprite("Pickaxe", true)},                   // TO CHANGE
                { (2, 0), new OneSprite("Scythe", true)},                    // TO CHANGE
                { (3, 0), new OneSprite("MagicWand", true)},                 // TO CHANGE
                { (4, 0), new OneSprite("Axe", true)},                    // TO CHANGE
            };
            overlayBackground = new OneSprite("OverlayBackground", false);

            numbersSprite = new OneSprite("Numbers", false);
            lettersUpSprite = new OneSprite("LettersUp", true);              // TO CHANGE
            lettersLowSprite = new OneSprite("LettersMin", true);            // TO CHANGE       // be careful also it's LettersMin and not LettersLow that are 2 different fonts ! LetterMin look better so i chose it lol
            arrowsSprite = new OneSprite("Arrows", true);                    // TO CHANGE
            operationSignsSprite = new OneSprite("OperationSigns", true);    // TO CHANGE

            fireAnimation = new OneAnimation("Fire", false, 6);
            livingPortalAnimation = new OneAnimation("LivingPortal", true, 4);       // TO CHANGE

            Bitmap[] numberBitmapArray = slice(numbersSprite.bitmap, 11, 1);
            numberSprites = new Dictionary<int, OneSprite>();
            for (int i = 0; i < numberBitmapArray.Count(); i++)
            {
                numberSprites.Add(i, new OneSprite(numberBitmapArray[i]));
            }

            Bitmap[] letterUpBitmapArray = slice(lettersUpSprite.bitmap, 26, 1);
            letterUpSprites = new Dictionary<int, OneSprite>();
            for (int i = 0; i < letterUpBitmapArray.Count(); i++)
            {
                letterUpSprites.Add(i, new OneSprite(letterUpBitmapArray[i]));
            }

            Bitmap[] letterLowBitmapArray = slice(lettersLowSprite.bitmap, 26, 1);
            letterLowSprites = new Dictionary<int, OneSprite>();
            for (int i = 0; i < letterLowBitmapArray.Count(); i++)
            {
                letterLowSprites.Add(i, new OneSprite(letterLowBitmapArray[i]));
            }

            Bitmap[] arrowBitmapArray = slice(arrowsSprite.bitmap, 4, 1);
            arrowSprites = new Dictionary<int, OneSprite>();
            for (int i = 0; i < arrowBitmapArray.Count(); i++)
            {
                arrowSprites.Add(i, new OneSprite(arrowBitmapArray[i]));
            }

            Bitmap[] operationSignBitmapArray = slice(operationSignsSprite.bitmap, 6, 1);
            operationSignSprites = new Dictionary<int, OneSprite>();
            for (int i = 0; i < operationSignBitmapArray.Count(); i++)
            {
                operationSignSprites.Add(i, new OneSprite(operationSignBitmapArray[i]));
            }
        }


        public class OneSprite
        {
            public Bitmap bitmap;
            public OneSprite(Bitmap bitmapToPut)
            {
                bitmap = bitmapToPut;
                (int, int) dimensions = (bitmapToPut.Width, bitmapToPut.Height);
                List<Color> paletteList = new List<Color>();
                for (int j = 0; j < dimensions.Item2; j++)
                {
                    for (int i = 0; i < dimensions.Item1; i++)
                    {
                        Color pixelColor = bitmapToPut.GetPixel(i, j);
                        for (int k = 0; k < paletteList.Count(); k++)
                        {
                            if (pixelColor == paletteList[k])
                            {
                                goto afterTest;
                            }
                        }
                        paletteList.Add(pixelColor);
                    afterTest:;
                    }
                }
                Color[] palette = new Color[paletteList.Count];
                for (int i = 0; i < paletteList.Count; i++)
                {
                    palette[i] = paletteList[i];
                }
            }
            public OneSprite(string contentString, bool isFileName)
            {
                if (isFileName)
                {
                    contentString = findSpritesPath() + $"\\{contentString}.txt";
                    using (StreamReader f = new StreamReader(contentString))
                    {
                        contentString = f.ReadToEnd();
                    }
                }
                else { contentString = SpriteStrings.spriteStringsDict[contentString]; }
                bitmap = makeBitmapFromContentString(contentString);
            }
        }
        public class OneAnimation
        {
            public Bitmap[] frames;
            public int frameCount;
            public OneAnimation(Bitmap[] framesToPut)
            {
                frames = framesToPut;
                //(int, int) dimensions = (frames[0].Width, frames[0].Height);
                frameCount = frames.Length;
            }
            public OneAnimation(string contentString, bool isFileName, int amoountOfFrames)
            {
                frameCount = amoountOfFrames;
                if (isFileName)
                {
                    contentString = findSpritesPath() + $"\\{contentString}.txt";
                    using (StreamReader f = new StreamReader(contentString))
                    {
                        contentString = f.ReadToEnd();
                    }
                }
                else { contentString = SpriteStrings.spriteStringsDict[contentString]; }

                Bitmap motherBitmap = makeBitmapFromContentString(contentString);
                frames = slice(motherBitmap, frameCount, 1);

                foreach (Bitmap bitmap in frames)
                {
                    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
            }
        }


        public static Bitmap makeBitmapFromContentString(string contentString) // read string content, and put it in the variables I guess
        {
            // contenu du contentString :
            // ligne 1 dimensions, x puis y séparés par un *.
            // ligne 2 palette, ints chacun suivis d'un ; ( se finit par x; ), format RGBA, 4 à la suite = 1 couleur.
            // ligne 3 chaque case, int tous suivis d'un ; ( se finit par x; ), avec int = l'emplacement dans la palette.

            int currentIdx = 0;
            int startIdx = 0;
            int lengthOfSub = 0;
            (int, int) dimensions = (1, 1); ;
            List<string> subStrings = new List<string>();
            while (currentIdx < contentString.Length)
            {
                if (contentString[currentIdx] == '\n')
                {
                    subStrings.Add(contentString.Substring(startIdx, lengthOfSub));
                    startIdx = currentIdx + 1; // current idx not yet +1ed
                    lengthOfSub = -1; // it will get +1ed after
                }
                currentIdx++;
                lengthOfSub++;
            }
            subStrings.Add(contentString.Substring(startIdx));
            string firstLine = subStrings[0];
            string secondLine = subStrings[1];
            string thirdLine = subStrings[2];

            currentIdx = 0;
            while (currentIdx < firstLine.Length)
            {
                if (firstLine[currentIdx] == '*')
                {
                    dimensions = (int.Parse(firstLine.Substring(0, currentIdx)), int.Parse(firstLine.Substring(currentIdx + 1)));
                    break;
                }
                currentIdx++;
            }

            currentIdx = 0;
            startIdx = 0;
            lengthOfSub = 0;
            List<int> secondLineInts = new List<int>();
            while (currentIdx < secondLine.Length)
            {
                if (secondLine[currentIdx] == ';')
                {
                    secondLineInts.Add(int.Parse(secondLine.Substring(startIdx, lengthOfSub)));
                    startIdx = currentIdx + 1; // current idx not yet +1ed
                    lengthOfSub = -1; // it will get +1ed after
                }
                currentIdx++;
                lengthOfSub++;
            }

            currentIdx = 0;
            startIdx = 0;
            lengthOfSub = 0;
            int currentArrayIdx = 0;
            int[] thirdLineInts = new int[dimensions.Item1 * dimensions.Item2];
            while (currentIdx < thirdLine.Length)
            {
                if (thirdLine[currentIdx] == ';')
                {
                    thirdLineInts[currentArrayIdx] = int.Parse(thirdLine.Substring(startIdx, lengthOfSub));
                    startIdx = currentIdx + 1; // current idx not yet +1ed
                    lengthOfSub = -1; // it will get +1ed after
                    currentArrayIdx++;
                }
                currentIdx++;
                lengthOfSub++;
            }


            Color[] palette = new Color[secondLineInts.Count / 4];
            for (int i = 0; i < secondLineInts.Count / 4; i++)
            {
                palette[i] = Color.FromArgb(secondLineInts[i * 4 + 3], secondLineInts[i * 4], secondLineInts[i * 4 + 1], secondLineInts[i * 4 + 2]);
            }

            Bitmap bitmapToReturn = new Bitmap(dimensions.Item1, dimensions.Item2);
            for (int i = 0; i < dimensions.Item1; i++)
            {
                for (int j = 0; j < dimensions.Item2; j++)
                {
                    setPixelButFaster(bitmapToReturn, (i, j), palette[thirdLineInts[i + j * dimensions.Item1]]);
                }
            }
            return bitmapToReturn;
        }
        public static Bitmap[] slice(Bitmap bitmap, int columnAmount, int lineAmount)
        {
            (int x, int y) dimensionsChild = (bitmap.Width / columnAmount, bitmap.Height / lineAmount);
            Bitmap[] tempArray = new Bitmap[columnAmount * lineAmount];
            for (int j = 0; j < lineAmount; j++)
            {
                for (int i = 0; i < columnAmount; i++)
                {
                    tempArray[i + columnAmount * j] = new Bitmap(dimensionsChild.x, dimensionsChild.y);
                    Bitmap Child = tempArray[i + columnAmount * j];
                    // remplace les valeurs du sprite
                    using (Graphics grD = Graphics.FromImage(Child)) //thanks Amen Ayach on stack overfloww w w
                    {
                        Rectangle srcRegion = new Rectangle(dimensionsChild.x * i, dimensionsChild.y * j, dimensionsChild.x, dimensionsChild.y);
                        Rectangle destRegion = new Rectangle(0, 0, dimensionsChild.x, dimensionsChild.y);
                        grD.DrawImage(bitmap, destRegion, srcRegion, GraphicsUnit.Pixel);
                    }
                }
            }
            return tempArray;
        }
        public static void drawSpriteOnCanvas(Bitmap bigBitmap, Bitmap smallBitmap, (int, int) posToDraw, int scaleFactor, bool centeredDraw)
        {
            if (scaleFactor <= 0) { return; }
            int[] drawRange = new int[4]; //OF THE SMALL SPRITE, startX (if cropped) (0-length), stopX (0-length), startY, stopY. Example, if it is [0,8,4,8], it will go from beginning on sprite to 8th pixel of sprite not included, and from 4th pixel of sprite (vertical) to 8th pixel of sprite not included
                                          // test les positions si ça dépasse pas
            if (centeredDraw)
            {
                posToDraw = ((int)(posToDraw.Item1 - (smallBitmap.Width * scaleFactor * 0.5f)), (int)(posToDraw.Item2 - (smallBitmap.Height * scaleFactor * 0.5f)));
            }
            drawRange[0] = -Min(posToDraw.Item1, 0);
            drawRange[1] = Min(bigBitmap.Width - posToDraw.Item1, smallBitmap.Width * scaleFactor);
            drawRange[2] = -Min(posToDraw.Item2, 0); ;
            drawRange[3] = Min(bigBitmap.Height - posToDraw.Item2, smallBitmap.Height * scaleFactor);
            if (drawRange[0] >= drawRange[1] || drawRange[2] >= drawRange[3])
            {
                return;
            }

            // Resizing
            Bitmap resizedBitmap;
            if (scaleFactor == 1)
            {
                resizedBitmap = smallBitmap;
            }
            else
            {
                resizedBitmap = new Bitmap(smallBitmap.Width * scaleFactor, smallBitmap.Height * scaleFactor);
                using (Graphics g = Graphics.FromImage(resizedBitmap))
                {
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.DrawImage(smallBitmap, 0, 0, resizedBitmap.Width, resizedBitmap.Height);
                }
            }

            // remplace les valeurs du sprite
            using (Graphics grD = Graphics.FromImage(bigBitmap)) //thanks Amen Ayach on stack overfloww w w
            {
                Rectangle srcRegion = new Rectangle(drawRange[0], drawRange[2], drawRange[1] - drawRange[0], drawRange[3] - drawRange[2]);
                Rectangle destRegion = new Rectangle(posToDraw.Item1 + drawRange[0], posToDraw.Item2 + drawRange[2], drawRange[1] - drawRange[0], drawRange[3] - drawRange[2]);
                grD.DrawImage(resizedBitmap, destRegion, srcRegion, GraphicsUnit.Pixel);
            }
        }

        public static void drawElement(Bitmap bigBitmap, (int type, int subType, int megaType) element, (int, int) posToDraw, int scaleFactor, bool centeredDraw)
        {
            if (element.megaType == 0)
            {
                Sprites.drawSpriteOnCanvas(bigBitmap, tileSprites[(element.type, element.subType)].bitmap, posToDraw, scaleFactor, centeredDraw);
            }
            else if (element.megaType == 1)
            {
                Sprites.drawSpriteOnCanvas(bigBitmap, entitySprites[(element.type, element.subType)].bitmap, posToDraw, scaleFactor, centeredDraw);
            }
            else if (element.megaType == 2)
            {
                Sprites.drawSpriteOnCanvas(bigBitmap, plantSprites[(element.type, element.subType)].bitmap, posToDraw, scaleFactor, centeredDraw);
            }
            else if (element.megaType == 3)
            {
                Sprites.drawSpriteOnCanvas(bigBitmap, materialSprites[(element.type, element.subType)].bitmap, posToDraw, scaleFactor, centeredDraw);
            }
            else if (element.megaType == 4)
            {
                Sprites.drawSpriteOnCanvas(bigBitmap, toolsSprites[(element.type, element.subType)].bitmap, posToDraw, scaleFactor, centeredDraw);
            }
        }

        public static void drawInventory(Game game, Dictionary<(int index, int subType, int typeOfElement), int> inventoryQuantities, List<(int index, int subType, int typeOfElement)> inventoryElements, int inventoryCursor)
        {                         
            if (inventoryElements.Count > 0)
            {
                (int type, int subType, int megaType) element = inventoryElements[inventoryCursor];
                drawElement(game.overlayBitmap, element, (360, 64), 4, true);

                int quantity = inventoryQuantities[element];
                if (quantity == -999)
                {
                    Sprites.drawSpriteOnCanvas(game.overlayBitmap, numberSprites[10].bitmap, (420, 64), 4, true);
                }
                else
                {
                    int scaleFactor = 4;
                    if (quantity >= 10000) { scaleFactor = 2; }
                    else if (quantity >= 1000) { scaleFactor = 3; }
                    drawNumber(game.overlayBitmap, quantity, (420, 64), scaleFactor, true);
                }
            }

            Screens.Screen screen = game.playerList[0].screen;
            if (debugMode) { drawNumber(game.overlayBitmap, screen.activeStructureLoadedChunkIndexes.Count, (420, 120), 1, true); }
        }
        public static void drawCraftRecipe(Game game, ((int type, int subType, int megaType) material, int count)[] Recipe)
        {
            List<((int type, int subType, int megaType) material, int count)> reactives = new List<((int type, int subType, int megaType) material, int count)>();
            List<((int type, int subType, int megaType) material, int count)> products = new List<((int type, int subType, int megaType) material, int count)>();

            int scaleCount = 1;

            foreach (((int type, int subType, int megaType) material, int count) tupel in Recipe)
            {
                if (tupel.count < 0) { reactives.Add(tupel); }
                else if (tupel.count > 0) { products.Add(tupel); }
                else { continue; }
                scaleCount++;
            }

            int scaleFactor = 4;
            if (scaleCount > 6) { scaleFactor = 1; }
            else if (scaleCount > 4) { scaleFactor = 2; }
            else if (scaleCount > 3) { scaleFactor = 3; }

            int posX = 5 * scaleFactor;
            for( int i = 0; i < reactives.Count; i++)
            {
                ((int type, int subType, int megaType) element, int count) tupel = reactives[i];
                posX += drawNumber(game.overlayBitmap, -tupel.count, (posX, 64), scaleFactor, true);         // -tuple.count cause it's negative it's not work lol if not minus
                drawElement(game.overlayBitmap, tupel.element, (posX + 4 * scaleFactor, 64), scaleFactor, true);
                posX += 16 * scaleFactor;
                if (i < reactives.Count - 1)
                {
                    posX += 2 * scaleFactor;
                    drawSpriteOnCanvas(game.overlayBitmap, operationSignSprites[0].bitmap, (posX, 64), scaleFactor, true);
                    posX += 10 * scaleFactor;
                }
            }

            posX += 2 * scaleFactor;
            drawSpriteOnCanvas(game.overlayBitmap, arrowSprites[1].bitmap, (posX, 64), scaleFactor, true);
            posX += 10 * scaleFactor;

            for (int i = 0; i < products.Count; i++)
            {
                ((int type, int subType, int megaType) element, int count) tupel = products[i];
                posX += drawNumber(game.overlayBitmap, tupel.count, (posX, 64), scaleFactor, true);
                drawElement(game.overlayBitmap, tupel.element, (posX + 4 * scaleFactor, 64), scaleFactor, true);
                posX += 16 * scaleFactor;
                if (i < products.Count - 1)
                {
                    posX += 2 * scaleFactor;
                    drawSpriteOnCanvas(game.overlayBitmap, operationSignSprites[0].bitmap, (posX, 64), scaleFactor, true);
                    posX += 10 * scaleFactor;
                }
            }

        }
        public static int drawNumber(Bitmap bitmap, int number, (int x, int y) pos, int scaleFactor, bool centeredDraw)
        {
            int counto = 0;
            List<int> numberList = new List<int>();
            if (number == 0) { numberList.Add(0); }
            for (int i = 0; number > 0; i++)
            {
                numberList.Insert(0, number % 10);
                number = number / 10;
            }
            for (int i = 0; i < numberList.Count; i++)
            {
                Sprites.drawSpriteOnCanvas(bitmap, numberSprites[numberList[i]].bitmap, (pos.x + i * 8 * scaleFactor, pos.y), scaleFactor, centeredDraw);
                counto += 8 * scaleFactor;
            }

            return counto;
        }
        public static string findSpritesPath()
        {
            string filepath = currentDirectory;
            int foundo = 0;
            int idx = filepath.Length - 1;
            while (foundo < 2)
            {
                if (filepath[idx] == '\\')
                {
                    filepath = filepath.Substring(0, idx);
                    foundo++;
                }
                idx--;
            }
            return (filepath + $"\\Sprites");
        }
        public static void turnPngIntoString(string filename)
        {
            string filepath = findSpritesPath() + $"\\{filename}";
            turnPngIntoStringFromFilepath(filepath);
        }
        public static void turnPngIntoStringFromFilepath(string filepath)
        {
            if (!System.IO.File.Exists(filepath + ".png"))
            {
                return;
            }
            Bitmap bitmap = new Bitmap(filepath + ".png");
            (int, int) dimensions = (bitmap.Width, bitmap.Height);
            string firstLine = dimensions.Item1.ToString() + "*" + dimensions.Item2.ToString();
            List<Color> palette = new List<Color>();
            string thirdLine = "";
            for (int j = 0; j < dimensions.Item2; j++)
            {
                for (int i = 0; i < dimensions.Item1; i++)
                {
                    int colorIdx = -1;
                    Color pixelColor = bitmap.GetPixel(i, j);
                    for (int k = 0; k < palette.Count(); k++)
                    {
                        if (pixelColor == palette[k])
                        {
                            colorIdx = k;
                            goto afterTest;
                        }
                    }
                    colorIdx = palette.Count();
                    palette.Add(pixelColor);
                afterTest:;
                    thirdLine = thirdLine + colorIdx.ToString() + ";";
                }
            }
            string secondLine = "";
            for (int i = 0; i < palette.Count; i++)
            {
                secondLine = secondLine + palette[i].R.ToString() + ";";
                secondLine = secondLine + palette[i].G.ToString() + ";";
                secondLine = secondLine + palette[i].B.ToString() + ";";
                secondLine = secondLine + palette[i].A.ToString() + ";";
            }
            using (StreamWriter f = new StreamWriter(filepath + ".txt", false))
            {
                f.Write(firstLine + "\n" + secondLine + "\n" + thirdLine);
            }
        }




        public static Color ColorFromHSV(double hue, double saturation, double value) // thanks Greg from stackoverflow
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }



        public static Bitmap makeLightBitmap(int smallRadius, (int radius, int r, int g, int b) col)
        {
            int radiusSq = (int)((smallRadius + 0.5f) * (smallRadius + 0.5f));
            int bigRadius2 = col.radius * 2;
            int bigRadiusSq = (int)((col.radius + 0.5f) * (col.radius + 0.5f));
            Bitmap bitmap = new Bitmap(bigRadius2 + 1, bigRadius2 + 1/*, PixelFormat.Format8bppIndexed*/); // size 0 : 1*1 bitmap
            int length2;
            Color color;
            Color semiTransparent = Color.FromArgb(128, col.r, col.g, col.b);
            Color transparent = Color.FromArgb(255, col.r, col.g, col.b);
            for (int i = 0; i < bigRadius2 + 1; i++)
            {
                for (int j = 0; j < bigRadius2 + 1; j++)
                {
                    length2 = (i - col.radius) * (i - col.radius) + (j - col.radius) * (j - col.radius);
                    if (length2 <= bigRadiusSq)
                    {
                        if (length2 <= radiusSq)
                        {
                            color = transparent;
                        }
                        else { color = semiTransparent; }
                        setPixelButFaster(bitmap, (i, j), color);
                    }
                }
            }
            return bitmap;
        }



        // the unsafe shit is NOT MY CODE !!! Thank you davidtbernal (i think)     I mean I modifier it a lot so it now is but idk yeah idk
        public static unsafe Bitmap replaceColor(Bitmap bitmap, Color colorToReplace, Color replacement)    // only to set transparency !
        {
            BitmapData bData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

            byte bitsPerPixel = (byte)System.Drawing.Image.GetPixelFormatSize(bData.PixelFormat);
            byte* scan0 = (byte*)bData.Scan0.ToPointer();

            byte* data;
            for (int i = 0; i < bData.Height; ++i)
            {
                for (int j = 0; j < bData.Width; ++j)
                {
                    data = scan0 + i * bData.Stride + j * bitsPerPixel / 8;
                    if (data[0] == colorToReplace.B && data[1] == colorToReplace.G && data[2] == colorToReplace.R && data[3] == colorToReplace.A)
                    {
                        data[0] = replacement.B;
                        data[1] = replacement.G;
                        data[2] = replacement.R;
                        data[3] = replacement.A;
                    }
                }
            }

            bitmap.UnlockBits(bData);

            return bitmap;
        }
        public static unsafe Bitmap pasteBitmapTransparenciesOnBitmap(Bitmap receiver, List<(int x, int y, int radius, Color color)> bitmapPos, (int x, int y) camPos)    // only to set transparency !
        {
            BitmapData bData1 = receiver.LockBits(new Rectangle(0, 0, receiver.Width, receiver.Height), ImageLockMode.ReadWrite, receiver.PixelFormat);

            byte bitsPerPixel1 = (byte)System.Drawing.Image.GetPixelFormatSize(bData1.PixelFormat);
            byte* scan01 = (byte*)bData1.Scan0.ToPointer();

            byte* data1;
            byte* data2;
            (int x, int y) pos;
            Bitmap bitmapToPaste;
            foreach ((int x, int y, int radius, Color color) posot in bitmapPos)
            {
                bitmapToPaste = getLightBitmap(posot.color, posot.radius);

                BitmapData bData2 = bitmapToPaste.LockBits(new Rectangle(0, 0, bitmapToPaste.Width, bitmapToPaste.Height), ImageLockMode.ReadWrite, bitmapToPaste.PixelFormat);
                byte bitsPerPixel2 = (byte)System.Drawing.Image.GetPixelFormatSize(bData2.PixelFormat);
                byte* scan02 = (byte*)bData2.Scan0.ToPointer();

                (int x, int y) poso = (posot.x - posot.radius - camPos.x - UnloadedChunksAmount * 32, posot.y - posot.radius - camPos.y - UnloadedChunksAmount * 32);
                for (int i = 0; i < bData2.Height; ++i)
                {
                    for (int j = 0; j < bData2.Width; ++j)
                    {
                        pos = (poso.x + j, poso.y + i);
                        if (pos.x < 0 || pos.y < 0 || receiver.Width <= pos.x || receiver.Height <= pos.y) { continue; }
                        data1 = scan01 + pos.y * bData1.Stride + pos.x * bitsPerPixel1 / 8;
                        data2 = scan02 + i * bData2.Stride + j * bitsPerPixel2 / 8;
                        for (int k = 0; k < 3; k++)
                        {
                            data1[k] = Max(data1[k], (byte)(data2[3]*data2[k]*_1On255));
                        }
                        data1[3] = 255;
                    }
                }

                bitmapToPaste.UnlockBits(bData2);
            }

            receiver.UnlockBits(bData1);

            return receiver;
        }
        public static unsafe Bitmap pasteLightBitmapOnGameBitmap(Bitmap receiver, Bitmap bitmapToPaste)
        {
            BitmapData bData1 = receiver.LockBits(new Rectangle(0, 0, receiver.Width, receiver.Height), ImageLockMode.ReadWrite, receiver.PixelFormat);
            BitmapData bData2 = bitmapToPaste.LockBits(new Rectangle(0, 0, bitmapToPaste.Width, bitmapToPaste.Height), ImageLockMode.ReadWrite, bitmapToPaste.PixelFormat);

            byte bitsPerPixel1 = (byte)System.Drawing.Image.GetPixelFormatSize(bData1.PixelFormat);
            byte bitsPerPixel2 = (byte)System.Drawing.Image.GetPixelFormatSize(bData2.PixelFormat);
            byte* scan01 = (byte*)bData1.Scan0.ToPointer();
            byte* scan02 = (byte*)bData2.Scan0.ToPointer();

            byte* data1;
            byte* data2;
            for (int i = 0; i < bData2.Height; ++i)
            {
                for (int j = 0; j < bData2.Width; ++j)
                {
                    data1 = scan01 + i * bData1.Stride + j * bitsPerPixel1 / 8;
                    data2 = scan02 + i * bData2.Stride + j * bitsPerPixel2 / 8;
                    for (int k = 0; k < 3; k++)
                    {
                        //data1[k] = (byte)(100 + k*50);
                        data1[k] = (byte)(Min((int)data2[3], 1)*data1[k]*data2[k]*_1On255);
                    }
                }
            }

            receiver.UnlockBits(bData1);
            bitmapToPaste.UnlockBits(bData2);

            return receiver;
        }
        public static unsafe Bitmap setPixelButFaster(Bitmap bitmap, (int x, int y) pos, Color colorToDraw)
        {
            BitmapData bData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

            byte bitsPerPixel = (byte)System.Drawing.Image.GetPixelFormatSize(bData.PixelFormat);
            byte* scan0 = (byte*)bData.Scan0.ToPointer();

            byte* data;
            data = scan0 + pos.y * bData.Stride + pos.x * bitsPerPixel / 8;
            //data[0] = Max(colorToDraw.B, data[0]);   // idk why this was there
            //data[1] = Max(colorToDraw.G, data[1]);
            //data[2] = Max(colorToDraw.R, data[2]);
            //data[3] = Max(colorToDraw.A, data[3]);
            data[0] = colorToDraw.B;
            data[1] = colorToDraw.G;
            data[2] = colorToDraw.R;
            data[3] = colorToDraw.A;

            bitmap.UnlockBits(bData);

            return bitmap;
        }



        public static Bitmap getLightBitmap(Color color, int radius)
        {
            (int radius, int r, int g, int b) col = (radius, (int)((color.R * _1On17) * 17), (int)((color.G * _1On17) * 17), (int)((color.B * _1On17) * 17));
            if (lightBitmaps.ContainsKey(col))
            {
                return lightBitmaps[col];
            }
            else
            {
                lightBitmaps[col] = makeLightBitmap((int)(radius * 0.5f + 0.6f), col);
                return lightBitmaps[col];
            }
        }
        public static void makeLightBitmaps()
        {
            lightBitmaps = new Dictionary<(int, int, int, int), Bitmap>();
            /*for (int i = 0; i <= 20; i++)
            {
                lightBitmaps[i] = makeLightBitmap((int)(i*0.5f+0.6f), i);
            }*/
        }
        public static void makeBlackBitmap()
        {
            black32Bitmap = new Bitmap(32, 32);
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    Sprites.setPixelButFaster(black32Bitmap, (i, j), Color.Black);
                }
            }
            Color colortoSet = Color.FromArgb(128, 0, 80, 255);
            transBlue32Bitmap = new Bitmap(32, 32);
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    Sprites.setPixelButFaster(transBlue32Bitmap, (i, j), colortoSet);
                }
            }
        }
    }
}
