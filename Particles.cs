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
using static Cave.Traits;
using static Cave.Attacks;
using static Cave.Files;
using static Cave.Plants;
using static Cave.Screens;
using static Cave.Chunks;
using static Cave.Players;
using static Cave.Particles;
using static Cave.Dialogues;

namespace Cave
{
    public class Particles
    {
        public class Particle
        {
            public Screens.Screen screen;

            public int seed;
            public (int type, int subType, int subSubType) type;
            public int state = 0;
            public int refId = -1;
            public float realPosX;
            public float realPosY;
            public int posX;
            public int posY;
            public float speedX = 0; // if angle is used, the speed used as the actual speed is speedX;
            public float speedY = 0;
            public float angle = 0;
            public bool isAngular = false;
            public (int x, int y) targetPos;
            public Color color;
            public Color lightColor;

            public float timeAtBirth;
            public float lifeExpectancy;
            public Particle(Screens.Screen screenToPut, (int x, int y) positionToPut, (int x, int y) targetPosToPut, (int type, int subType, int subSubType) typeToPut, int idToPut = -1)
            {
                screen = screenToPut;
                refId = idToPut;
                posX = positionToPut.Item1;
                realPosX = posX;
                posY = positionToPut.Item2;
                realPosY = posY;
                targetPos = targetPosToPut;
                seed = rand.Next(1000000000); //                              TO CHANGE FALSE RANDOM NOT SEEDED ARGHHEHEEEE
                timeAtBirth = timeElapsed;
                transformParticle(typeToPut);
                findLightColor();

                screen.particlesToAdd.Add(this);
            }
            public void transformParticle(int typeToPut, int subTypeToPut, int subSubTypeToPut)
            {
                type = (typeToPut, subTypeToPut, subSubTypeToPut);
                findEverything();
            }
            public void transformParticle((int type, int subType, int subSubType) typeToPut)
            {
                type = typeToPut;
                findEverything();
            }
            public void findEverything()
            {
                color = findColor();
                findLightColor();
                findLifeExpectancy();
                findAngulosity();
            }
            public Color findColor()
            {
                if (type.type == 1) // fading away
                {
                    (int r, int g, int b, float mult) materialColor = tileColors[(type.subType, type.subSubType)];
                    return Color.FromArgb(Max(0, 255 - (int)(100 * (timeElapsed - timeAtBirth))), materialColor.r, materialColor.g, materialColor.b);
                }
                if (type.type == 2) // color of tile
                {
                    (int r, int g, int b, float mult) materialColor = tileColors[(type.subType, type.subSubType)];
                    return Color.FromArgb(100, materialColor.r, materialColor.g, materialColor.b);
                }
                if (type.type == 3) // circular motion
                {
                    int shadeMod = rand.Next(35);
                    return Color.FromArgb(100, 255 - shadeMod, 255 - shadeMod, 255 - shadeMod);
                }
                if (type.type == 4) // circular motion
                {
                    int shadeMod = rand.Next(35);
                    return Color.FromArgb(100, 255 - shadeMod, 255 - shadeMod, 255 - shadeMod);
                }
                if (type.type == 5) // plant particle
                {
                    if (screen.activePlants.ContainsKey(refId))
                    {
                        Plant motherPlant = screen.activePlants[refId];
                        return motherPlant.colorDict[(type.subType, type.subSubType)];
                    }
                }
                return Color.FromArgb(100, rand.Next(256), rand.Next(256), rand.Next(256));
            }
            public void findLightColor()
            {
                lightColor = Color.FromArgb(255, (color.R + 255) / 2, (color.G + 255) / 2, (color.B + 255) / 2);
            }
            public void findLifeExpectancy()
            {
                if (type.type == 1) { lifeExpectancy = 3; }
                else { lifeExpectancy = 3 + (float)(rand.NextDouble()) * 7; }
            }
            public void findAngulosity()
            {
                if (type.type == 3 || type.type == 5)
                {
                    isAngular = true;
                    angle = (float)rand.NextDouble() * 6.28f;
                }
                else { isAngular = false; }
            }
            public bool testDeath()
            {
                (int x, int y) chunkPos = ChunkIdx(posX, posY);
                if (!screen.loadedChunks.ContainsKey(chunkPos)) { screen.particlesToRemove[this] = true; return true; }
                if (timeElapsed > timeAtBirth + lifeExpectancy) { screen.particlesToRemove[this] = true; return true; }
                return false;
            }
            public void changeSpeedRandom(float range)
            {
                speedX += range - (float)(rand.NextDouble()) * range * 2;
                speedY += range - (float)(rand.NextDouble()) * range * 2;
            }
            public void ariGeoSlowDown(float ari, float geo)
            {
                speedX = Sign(speedX) * Max(0, Abs(speedX) * geo - ari);
                speedY = Sign(speedY) * Max(0, Abs(speedY) * geo - ari);
            }
            public void moveParticle()
            {
                int diffX;
                int diffY;
                if (type.type == 0)
                {
                    if (state == 0) // moving away from center
                    {
                        diffX = posX - targetPos.x;
                        if (Abs(diffX) < 3) { speedX = speedX + Sign(diffX) * 0.3f; }
                        diffY = posY - targetPos.y;
                        if (Abs(diffY) < 3) { speedY = speedY + Sign(diffY) * 0.3f; }
                        if (diffX * diffX + diffY * diffY > 24) { state = 1; }
                    }
                    else if (state == 1) // moving to center
                    {
                        diffX = posX - targetPos.x;
                        diffY = posY - targetPos.y;
                        if (Abs(diffX) > 0) { speedX -= Sign(diffX) * 0.45f; }
                        if (Abs(diffY) > 0) { speedY -= Sign(diffY) * 0.45f; }
                        if (Abs(diffX) + Abs(diffY) < 2) { state = 0; }
                    }

                    changeSpeedRandom(0.1f);
                    ariGeoSlowDown(0.1f, 0.9f);
                }
                else if (type.type == 2)
                {
                    if (state == 0)
                    {
                        speedY = 0.3f + 0.5f * (float)(rand.NextDouble());
                        state = 1;
                    }
                    else if (state == 1)
                    {
                        ariGeoSlowDown(0.08f, 0.9f);
                        if (speedY < 0.03f) { state = 2; }
                    }
                    else if (state == 2)
                    {
                        diffX = posX - targetPos.x;
                        diffY = posY - targetPos.y;
                        if (Abs(diffX) > 0) { speedX -= diffX * 0.05f; }
                        if (Abs(diffY) > 0) { speedY -= diffY * 0.05f; }
                        if (Abs(diffX) + Abs(diffY) < 2)
                        {
                            if (rand.Next(10) == 0) { transformParticle(3, type.subType, type.subSubType);}
                            else { Destroy(); }
                        }
                        ariGeoSlowDown(0.03f, 0.9f);
                    }

                    changeSpeedRandom(0.05f);
                }
                else if (type.type == 3)
                {
                    speedX = speedX * 0.9f + (float)(rand.NextDouble()) * 0.2f;
                    angle = (float)Math.Atan2(targetPos.y - posY, targetPos.x - posX) + (float)Math.PI*(0.37f + 0.16f*(float)(rand.NextDouble()));
                }
                else if (type.type == 5)
                {
                    speedX = speedX * 0.9f + (float)(rand.NextDouble()) * 0.1f;
                    angle += -(((float)rand.NextDouble()-0.5f) * 1f);
                }

                if (isAngular)
                {
                    realPosX += speedX * (float)Math.Cos(angle);
                    realPosY += speedX * (float)Math.Sin(angle);
                    posX = (int)(Floor(realPosX, 1));
                    posY = (int)(Floor(realPosY, 1));
                }
                else
                {
                    realPosX += speedX;
                    realPosY += speedY;
                    posX = (int)(Floor(realPosX, 1));
                    posY = (int)(Floor(realPosY, 1));
                }

                if (!testDeath())
                {
                    if (type.type == 1) { color = findColor(); }
                }
            }
            public void Destroy() // lol, lmao even
            {
                lifeExpectancy = -999;
            }
        }
    }
}
