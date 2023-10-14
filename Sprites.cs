using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cave
{
    internal class Sprites
    {
        //
        public class OneSprite
        {
            public (int, int, int, int)[] palette;
            public int[,] colors;
            public (int, int) dimensions;
            public Bitmap bitmap;

            OneSprite(string contentString) // read string content, and put it in the variables I guess
            {
                // contentString, ligne 1 dimensions, x puis y séparés par un point-virgule.
                // ligne 2 palette, int tous séparés par un point-virgule, format RGBA, 4 à la suite = 1 couleur.
                // ligne 3 chaque case, int tous séparés par un point-virgule, avec int = l'emplacement dans la palette.
            }
            public void drawBitmap()
            {
                bitmap = new Bitmap(dimensions.Item1, dimensions.Item2);
                for (int i = 0; i < dimensions.Item1; i++)
                {
                    for (int j = 0; j < dimensions.Item2; j++)
                    {
                        bitmap.SetPixel(i, j, Color.FromArgb(palette[colors[i, j]].Item1, palette[colors[i, j]].Item2, palette[colors[i, j]].Item3, palette[colors[i, j]].Item4));
                    }
                }
            }
            public void drawSpriteOnCanvas(Bitmap bigBitmap, (int, int) posToDraw, int scaleFactor, bool centeredDraw)
            {
                // test les positions si ça dépasse pas

                // remplace les valeurs du sprite

            }
        }

    }
}
