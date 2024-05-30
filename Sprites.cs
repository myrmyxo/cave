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
using Newtonsoft.Json.Converters;

using static Cave.Form1;
using static Cave.Form1.Globals;
using static Cave.MathF;
using static Cave.Sprites;
using static Cave.Structures;
using static Cave.Entities;
using static Cave.Files;
using static Cave.Plants;
using static Cave.Screens;
using static Cave.Chunks;
using static Cave.Players;
using System.Drawing.Imaging;
using System.Xml.Linq;

namespace Cave
{
    public class Sprites
    {
        public static Dictionary<int, Bitmap> lightBitmaps;

        public static Dictionary<int, OneSprite> compoundSprites;
        public static Dictionary<(int, int), OneSprite> entitySprites;
        public static Dictionary<(int, int), OneSprite> plantSprites;
        public static Dictionary<(int, int), OneSprite> materialSprites;
        public static OneSprite numbersSprite;
        public static Dictionary<int, OneSprite> numberSprites;
        public static OneSprite overlayBackground;
        public static void loadSpriteDictionaries()
        {
            compoundSprites = new Dictionary<int, OneSprite>
            {
                { -5, new OneSprite("Honey", false)},
                { -4, new OneSprite("Lava", false)},
                { -3, new OneSprite("FairyLiquid", false)},
                { -2, new OneSprite("Water", false)},
                { -1, new OneSprite("Piss", false)},
                { 1, new OneSprite("BasicTile", false)},
                { 2, new OneSprite("BasicTile", false)},
                { 3, new OneSprite("BasicTile", false)},
                { 4, new OneSprite("BasicTile", false)},
                { 5, new OneSprite("BasicTile", false)},
                { 6, new OneSprite("BasicTile", false)}
            };
            entitySprites = new Dictionary<(int, int), OneSprite>
            {
                { (0, 0), new OneSprite("Fairy", false)},
                { (0, 1), new OneSprite("ObsidianFairy", false)},
                { (0, 2), new OneSprite("FrostFairy", false)},
                { (1, 0), new OneSprite("Frog", false)},
                { (2, 0), new OneSprite("Fish", false)},
                { (3, 0), new OneSprite("Hornet", false)},
                { (3, 3), new OneSprite("Hornet", false)}
            };
            plantSprites = new Dictionary<(int, int), OneSprite>
            {
                { (0, 0), new OneSprite("BasePlant", false)},
                { (1, 0), new OneSprite("Tree", false)},
                { (1, 1), new OneSprite("Tree", false)},
                { (2, 0), new OneSprite("KelpUpwards", false)},
                { (2, 1), new OneSprite("KelpDownwards", false)},
                { (3, 0), new OneSprite("ObsidianPlant", false)},
                { (4, 0), new OneSprite("Mushroom", false)},
                { (5, 0), new OneSprite("Vines", false)},
                { (5, 1), new OneSprite("ObsidianVines", false)}
            };
            materialSprites = new Dictionary<(int, int), OneSprite>
            {
                { (1, 0), new OneSprite("PlantMatter", false)},
                { (2, 0), new OneSprite("Wood", false)},
                { (3, 0), new OneSprite("Kelp", false)},
                { (4, 0), new OneSprite("MushroomStem", false)},
                { (5, 0), new OneSprite("MushroomCap", false)},
                { (6, 0), new OneSprite("FlowerPetal", false)},
                { (7, 0), new OneSprite("Pollen", false)},
            };
            overlayBackground = new OneSprite("OverlayBackground", false);
            numbersSprite = new OneSprite("Numbers", false);
            Bitmap[] numberBitmapArray = numbersSprite.slice(11, 1);
            numberSprites = new Dictionary<int, OneSprite>();
            for (int i = 0; i < numberBitmapArray.Count(); i++)
            {
                numberSprites.Add(i, new OneSprite(numberBitmapArray[i]));
            }
        }


        public class OneSprite
        {
            public Color[] palette;
            public (int, int) dimensions;
            public Bitmap bitmap;
            public OneSprite(Bitmap bitmapToPut)
            {
                bitmap = bitmapToPut;
                dimensions = (bitmapToPut.Width, bitmapToPut.Height);
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
                palette = new Color[paletteList.Count];
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
                makeBitmapFromContentString(contentString);
            }
            public void makeBitmapFromContentString(string contentString) // read string content, and put it in the variables I guess
            {
                // contenu du contentString :
                // ligne 1 dimensions, x puis y séparés par un *.
                // ligne 2 palette, ints chacun suivis d'un ; ( se finit par x; ), format RGBA, 4 à la suite = 1 couleur.
                // ligne 3 chaque case, int tous suivis d'un ; ( se finit par x; ), avec int = l'emplacement dans la palette.

                int currentIdx = 0;
                int startIdx = 0;
                int lengthOfSub = 0;
                List<string> subStrings = new List<string>();
                while (currentIdx < contentString.Length)
                {
                    if (contentString[currentIdx] == '\n')
                    {
                        subStrings.Add(contentString.Substring(startIdx, lengthOfSub));
                        startIdx = currentIdx+1; // current idx not yet +1ed
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
                        dimensions = (int.Parse(firstLine.Substring(0, currentIdx)), int.Parse(firstLine.Substring(currentIdx+1)));
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
                int[] thirdLineInts = new int[dimensions.Item1*dimensions.Item2];
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

                palette = new Color[secondLineInts.Count/4];
                for (int i = 0; i < secondLineInts.Count/4; i++)
                {
                    palette[i] = Color.FromArgb(secondLineInts[i*4 + 3], secondLineInts[i*4], secondLineInts[i*4 + 1], secondLineInts[i*4 + 2]);
                }

                bitmap = new Bitmap(dimensions.Item1, dimensions.Item2);
                for (int i = 0; i < dimensions.Item1; i ++)
                {
                    for (int j = 0; j < dimensions.Item2; j++)
                    {
                        setPixelButFaster(bitmap, (i, j), palette[thirdLineInts[i+j*dimensions.Item1]]);
                    }
                }
            }
            /*public void drawBitmap() // not use anymore but I'll keed it cuz idk
            {
                bitmap = new Bitmap(dimensions.Item1, dimensions.Item2);
                for (int i = 0; i < dimensions.Item1; i++)
                {
                    for (int j = 0; j < dimensions.Item2; j++)
                    {
                        bitmap.setPixelButFaster(i, j, Color.FromArgb(palette[colors[i, j]].Item1, palette[colors[i, j]].Item2, palette[colors[i, j]].Item3, palette[colors[i, j]].Item4));
                    }
                }
            }*/
            public Bitmap[] slice(int columnAmount, int lineAmount)
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

            // NEED TO SCALE BEFOOORE I mean i'll just rectangle fill manuatlly instead of thing below it'll be easier
            /*int pixelPosX;
            int pixelPosY;
            for (int i = drawRange[0]; i < (int)(((drawRange[1]-drawRange[0])/scaleFactor)+0.99f); i++)
            {
                for (int j = drawRange[2]; j < (int)(((drawRange[3]-drawRange[2])/scaleFactor)+0.99f); j++)
                {
                    pixelPosX = posToDraw.Item1+i*scaleFactor;
                    pixelPosY = posToDraw.Item2+j*scaleFactor;

                    if (pixelPosX < 0 || pixelPosX >= bigBitmap.Width || pixelPosY < 0 || pixelPosY >= bigBitmap.Height)
                    {
                        continue;
                    }

                    Color color = smallBitmap.GetPixel(i, j);

                    using (var g = Graphics.FromImage(bigBitmap))
                    {
                        g.FillRectangle(new SolidBrush(color), pixelPosX, pixelPosY, scaleFactor, scaleFactor);
                    }
                }
            }*/

            // Resizing
            Bitmap resizedBitmap;
            if (scaleFactor == 1)
            {
                resizedBitmap = smallBitmap;
            }
            else
            {
                resizedBitmap = new Bitmap(smallBitmap.Width*scaleFactor, smallBitmap.Height*scaleFactor);
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
            if (!System.IO.File.Exists(filepath+".png"))
            {
                return;
            }
            Bitmap bitmap = new Bitmap(filepath+".png");
            (int, int) dimensions = (bitmap.Width, bitmap.Height);
            string firstLine = dimensions.Item1.ToString()+"*"+dimensions.Item2.ToString();
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
            using (StreamWriter f = new StreamWriter(filepath+".txt", false))
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



        public static Bitmap makeLightBitmap(int radius, int bigRadius)
        {
            int radiusSq = (int)((radius+0.5f)*(radius+0.5f));
            int bigRadius2 = bigRadius*2;
            int bigRadiusSq = (int)((bigRadius+0.5f)*(bigRadius+0.5f));
            Bitmap bitmap = new Bitmap(bigRadius2 + 1, bigRadius2 + 1); // size 0 : 1*1 bitmap
            int length2;
            Color color;
            Color semiTransparent = Color.FromArgb(128,255,255,255);
            Color transparent = Color.FromArgb(255,255,255,255);
            for (int i = 0; i < bigRadius2 + 1; i++)
            {
                for (int j = 0; j < bigRadius2 + 1; j++)
                {   
                    length2 = (i - bigRadius) * (i - bigRadius) + (j - bigRadius) * (j - bigRadius);
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



        // the unsafe shit is NOT MY CODE !!! Thank you davidtbernal (i think)
        public static unsafe Bitmap replaceLight(Bitmap bitmap)    // only to set transparency !
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
                    data[3] = (byte)(255 - data[3]);
                    data[0] = 0;
                    data[1] = 0;
                    data[2] = 0;
                }
            }

            bitmap.UnlockBits(bData);

            return bitmap;
        }
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
        public static unsafe Bitmap pasteBitmapOnBitmap(Bitmap receiver, Bitmap bitmapToPaste, (int x, int y) posToPaste)    // only to set transparency !
        {
            BitmapData bData1 = receiver.LockBits(new Rectangle(0, 0, receiver.Width, receiver.Height), ImageLockMode.ReadWrite, receiver.PixelFormat);
            BitmapData bData2 = bitmapToPaste.LockBits(new Rectangle(0, 0, bitmapToPaste.Width, bitmapToPaste.Height), ImageLockMode.ReadWrite, bitmapToPaste.PixelFormat);

            byte bitsPerPixel1 = (byte)System.Drawing.Image.GetPixelFormatSize(bData1.PixelFormat);
            byte bitsPerPixel2 = (byte)System.Drawing.Image.GetPixelFormatSize(bData2.PixelFormat);
            byte* scan01 = (byte*)bData1.Scan0.ToPointer();
            byte* scan02 = (byte*)bData2.Scan0.ToPointer();

            byte* data1;
            byte* data2;
            (int x, int y) pos;
            for (int i = 0; i < bData2.Height; ++i)
            {
                for (int j = 0; j < bData2.Width; ++j)
                {
                    pos = (posToPaste.x + i, posToPaste.y + j);
                    data1 = scan01 + pos.x * bData1.Stride + pos.y * bitsPerPixel1 / 8;
                    data2 = scan02 + i * bData2.Stride + j * bitsPerPixel2 / 8;
                    data1 = data2;
                    /*if (data[0] == colorToReplace.B && data[1] == colorToReplace.G && data[2] == colorToReplace.R && data[3] == colorToReplace.A)
                    {
                        data[0] = replacement.B;
                        data[1] = replacement.G;
                        data[2] = replacement.R;
                        data[3] = replacement.A;
                    }*/
                }
            }

            receiver.UnlockBits(bData1);
            bitmapToPaste.UnlockBits(bData2);

            return receiver;
        }
        public static unsafe Bitmap pasteBitmapTransparencyOnBitmap(Bitmap receiver, Bitmap bitmapToPaste, (int x, int y) posToPaste)    // only to set transparency !
        {
            BitmapData bData1 = receiver.LockBits(new Rectangle(0, 0, receiver.Width, receiver.Height), ImageLockMode.ReadWrite, receiver.PixelFormat);
            BitmapData bData2 = bitmapToPaste.LockBits(new Rectangle(0, 0, bitmapToPaste.Width, bitmapToPaste.Height), ImageLockMode.ReadWrite, bitmapToPaste.PixelFormat);

            byte bitsPerPixel1 = (byte)System.Drawing.Image.GetPixelFormatSize(bData1.PixelFormat);
            byte bitsPerPixel2 = (byte)System.Drawing.Image.GetPixelFormatSize(bData2.PixelFormat);
            byte* scan01 = (byte*)bData1.Scan0.ToPointer();
            byte* scan02 = (byte*)bData2.Scan0.ToPointer();

            byte* data1;
            byte* data2;
            (int x, int y) pos;
            for (int i = 0; i < bData2.Height; ++i)
            {
                for (int j = 0; j < bData2.Width; ++j)
                {
                    pos = (posToPaste.x + j, posToPaste.y + i);
                    if (pos.x < 0 || pos.y < 0 || receiver.Width <= pos.x || receiver.Height <= pos.y) { continue; }
                    data1 = scan01 + pos.y * bData1.Stride + pos.x * bitsPerPixel1 / 8;
                    data2 = scan02 + i * bData2.Stride + j * bitsPerPixel2 / 8;
                    data1[3] = 0;
                    //data1[3] = Min(data1[3], data2[3]);
                }
            }

            receiver.UnlockBits(bData1);
            bitmapToPaste.UnlockBits(bData2);

            return receiver;
        }
        /*public static unsafe Bitmap pasteBitmapTransparenciesOnBitmap(Bitmap receiver, List<(int x, int y)> bitmapPos, (int x, int y) camPos)    // only to set transparency !
        {
            BitmapData bData1 = receiver.LockBits(new Rectangle(0, 0, receiver.Width, receiver.Height), ImageLockMode.ReadWrite, receiver.PixelFormat);
                BitmapData bData2 = lightBitmap3.LockBits(new Rectangle(0, 0, lightBitmap3.Width, lightBitmap3.Height), ImageLockMode.ReadWrite, lightBitmap3.PixelFormat);

            byte bitsPerPixel1 = (byte)System.Drawing.Image.GetPixelFormatSize(bData1.PixelFormat);
                byte bitsPerPixel2 = (byte)System.Drawing.Image.GetPixelFormatSize(bData2.PixelFormat);
            byte* scan01 = (byte*)bData1.Scan0.ToPointer();
                byte* scan02 = (byte*)bData2.Scan0.ToPointer();

            byte* data1;
            byte* data2;
            (int x, int y) pos;
            foreach ((int x, int y) posot in bitmapPos)
            {
                (int x, int y) poso = (posot.x - camPos.x - UnloadedChunksAmount * 32, posot.y - camPos.y - UnloadedChunksAmount * 32);
                for (int i = 0; i < bData2.Height; ++i)
                {
                    for (int j = 0; j < bData2.Width; ++j)
                    {
                        pos = (poso.x + j, poso.y + i);
                        if (pos.x < 0 || pos.y < 0 || receiver.Width <= pos.x || receiver.Height <= pos.y) { continue; }
                        data1 = scan01 + pos.y * bData1.Stride + pos.x * bitsPerPixel1 / 8;
                        data2 = scan02 + i * bData2.Stride + j * bitsPerPixel2 / 8;
                        data1[3] = 0;
                        //data1[3] = Min(data1[3], data2[3]);
                    }
                }
            }

            receiver.UnlockBits(bData1);
                lightBitmap3.UnlockBits(bData2);

            return receiver;
        }*/
        public static unsafe Bitmap setPixelButFaster(Bitmap bitmap, (int x, int y) pos, Color colorToDraw)
        {
            BitmapData bData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

            byte bitsPerPixel = (byte)System.Drawing.Image.GetPixelFormatSize(bData.PixelFormat);
            byte* scan0 = (byte*)bData.Scan0.ToPointer();

            byte* data;
            data = scan0 + pos.y * bData.Stride + pos.x * bitsPerPixel / 8;
            data[0] = Max(colorToDraw.B, data[0]);
            data[1] = Max(colorToDraw.G, data[1]);
            data[2] = Max(colorToDraw.R, data[2]);
            data[3] = Max(colorToDraw.A, data[3]);

            bitmap.UnlockBits(bData);

            return bitmap;
        }
    }
}
