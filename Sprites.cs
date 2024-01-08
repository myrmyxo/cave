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

namespace Cave
{
    public class Sprites
    {
        public static Dictionary<int, OneSprite> compoundSprites;
        public static Dictionary<(int, int), OneSprite> entitySprites;
        public static Dictionary<(int, int), OneSprite> plantSprites;
        public static OneSprite numbersSprite;
        public static Dictionary<int, OneSprite> numberSprites;
        public static OneSprite overlayBackground = new OneSprite("OverlayBackground", true);
        public static void loadSpriteDictionaries()
        {
            compoundSprites = new Dictionary<int, OneSprite>
            {
                { -5, new OneSprite("Honey", true)},
                { -4, new OneSprite("Lava", true)},
                { -3, new OneSprite("FairyLiquid", true)},
                { -2, new OneSprite("Water", true)},
                { -1, new OneSprite("Piss", true)},
                { 1, new OneSprite("BasicTile", true)},
                { 2, new OneSprite("BasicTile", true)},
            };
            entitySprites = new Dictionary<(int, int), OneSprite>
            {
                { (0, 0), new OneSprite("Fairy", true)},
                { (0, 1), new OneSprite("ObsidianFairy", true)},
                { (0, 2), new OneSprite("FrostFairy", true)},
                { (1, 0), new OneSprite("Frog", true)},
                { (2, 0), new OneSprite("Fish", true)},
                { (3, 0), new OneSprite("Hornet", true)}
            };
            plantSprites = new Dictionary<(int, int), OneSprite>
            {
                { (0, 0), new OneSprite("PlantMatter", true)},
                { (1, 0), new OneSprite("Wood", true)},
                { (2, 0), new OneSprite("Kelp", true)},
                { (2, 1), new OneSprite("Kelp", true)},
                { (3, 0), new OneSprite("ObsidianFairy", true)},
                { (4, 0), new OneSprite("Fairy", true)},
                { (5, 0), new OneSprite("FlowerPetal", true)},
                { (5, 1), new OneSprite("ObsidianFairy", true)}
            };
            overlayBackground = new OneSprite("OverlayBackground", true);
            numbersSprite = new OneSprite("Numbers", true);
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
                    contentString = (findSpritesPath() + $"\\{contentString}.txt");
                    using (StreamReader f = new StreamReader(contentString))
                    {
                        contentString = f.ReadToEnd();
                    }
                }
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
                        bitmap.SetPixel(i, j, palette[thirdLineInts[i+j*dimensions.Item1]]);
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
                        bitmap.SetPixel(i, j, Color.FromArgb(palette[colors[i, j]].Item1, palette[colors[i, j]].Item2, palette[colors[i, j]].Item3, palette[colors[i, j]].Item4));
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
    }
}
