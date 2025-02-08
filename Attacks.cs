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
    public class Attacks
    {
        public class Attack
        {
            public int state = -1;
            public (int x, int y) direction = (0, 0);
            public (int type, int subType, int typeOfElement) type = (-1, -1, -1);
            public AttackTraits traits;
            public Dictionary<int, bool> entitiesAlreadyHitByCurrentAttack = new Dictionary<int, bool>();
            public (int x, int y) startPos;
            public (int x, int y) pos;
            public Entity motherEntity;
            public bool isDone = false;
            public bool digSuccess = false;
            public (int type, int subType) dugTile = (0, 0);
            public Attack(Entity motherEntityToPut, (int type, int subType, int typeOfElement) typeToPut, (int x, int y) posToPut, (int x, int y) directionToPut)
            {
                motherEntity = motherEntityToPut;
                type = typeToPut;
                traits = attackTraitsDict.ContainsKey(type) ? attackTraitsDict[type] : attackTraitsDict[(-1, 0, 0)];
                state = -1;
                isDone = false;
                pos = posToPut;
                startPos = pos;
                direction = directionToPut;
                motherEntity.screen.activeAttacks.Add(this);
            }
            public void updateAttack()
            {
                List<((int x, int y), Color color)> posToDrawList = motherEntity.screen.attacksToDraw;
                List<((int x, int y) pos, Attack attack)> posToAttackList = motherEntity.screen.attacksToDo;

                state++;
                pos = (motherEntity.posX, motherEntity.posY);
                if (type == (0, 0, 4)) // if sword attack
                {
                    int sign = 1;
                    if (direction.x > 0) { sign = -1; }
                    (int x, int y) attackDirection = directionPositionArray[PosMod(directionPositionDictionary[direction] + (sign * (state - 2)), 8)];

                    // draw 2 pixels, at attack direction, attack direction*2
                    posToDrawList.Add(((pos.x + attackDirection.x, pos.y + attackDirection.y), Color.White));
                    posToDrawList.Add(((pos.x + 2 * attackDirection.x, pos.y + 2 * attackDirection.y), Color.White));

                    if (attackDirection.x != 0 && attackDirection.y != 0) // diagonal, add 4 attack pixels (sword + sides)
                    {
                        posToAttackList.Add(((pos.x + attackDirection.x, pos.y + attackDirection.y), this));
                        posToAttackList.Add(((pos.x + 2 * attackDirection.x, pos.y + attackDirection.y), this));
                        posToAttackList.Add(((pos.x + attackDirection.x, pos.y + 2 * +attackDirection.y), this));
                        posToAttackList.Add(((pos.x + 2 * attackDirection.x, pos.y + 2 * attackDirection.y), this));

                    }
                    else if (attackDirection.x != 0) // not diagonal, add attack pixels (sword + sides)
                    {
                        posToAttackList.Add(((pos.x + attackDirection.x, pos.y - 1), this));
                        posToAttackList.Add(((pos.x + attackDirection.x, pos.y), this));
                        posToAttackList.Add(((pos.x + attackDirection.x, pos.y + 1), this));
                        posToAttackList.Add(((pos.x + 2 * attackDirection.x, pos.y - 1), this));
                        posToAttackList.Add(((pos.x + 2 * attackDirection.x, pos.y), this));
                        posToAttackList.Add(((pos.x + 2 * attackDirection.x, pos.y + 1), this));
                    }
                    else
                    {
                        posToAttackList.Add(((pos.x - 1, pos.y + attackDirection.y), this));
                        posToAttackList.Add(((pos.x, pos.y + attackDirection.y), this));
                        posToAttackList.Add(((pos.x + 1, pos.y + attackDirection.y), this));
                        posToAttackList.Add(((pos.x - 1, pos.y + 2 * attackDirection.y), this));
                        posToAttackList.Add(((pos.x, pos.y + 2 * attackDirection.y), this));
                        posToAttackList.Add(((pos.x + 1, pos.y + 2 * attackDirection.y), this));
                    }

                    if (state >= 4) { finishAttack(); }
                }
                else if (type == (1, 0, 4) || type == (4, 0, 4) || type == (6, 0, 5)) // if pickaxe, axe, goblin hand
                {
                    (int x, int y) attackPos = (pos.x + motherEntity.direction.x, pos.y + motherEntity.direction.y);
                    if (state == 0) { posToAttackList.Add((attackPos, this)); }
                    posToDrawList.Add((attackPos, Color.White));
                    if (devMode || state >= 3) { finishAttack(); }
                }
                else if (type == (3, 1, 5)) // hornet mandible attack
                {
                    if (state == 0) { posToAttackList.Add((startPos, this)); }
                    posToDrawList.Add((startPos, Color.White));
                    if (state >= 3) { finishAttack(); }
                }
                else if (type == (2, 0, 4)) // if scythe attack
                {
                    (int x, int y) attackPos = (0, 0);
                    int sign = 1;
                    if (direction.x > 0) { sign = -1; }

                    if (state == 0) { attackPos = (pos.x + sign, pos.y + 1); }
                    else if (state == 1) { attackPos = (pos.x - sign, pos.y + 1); }
                    else if (state == 2) { attackPos = (pos.x - 2 * sign, pos.y); }
                    else if (state == 3) { attackPos = (pos.x - sign, pos.y - 1); }
                    else if (state == 4) { attackPos = (pos.x, pos.y - 1); }

                    posToDrawList.Add(((attackPos.x, attackPos.y), Color.White));
                    posToDrawList.Add(((attackPos.x - sign, attackPos.y), Color.White));
                    for (int j = -1; j <= 1; j += 1)
                    {
                        posToAttackList.Add(((attackPos.x, attackPos.y + j), this));
                        posToAttackList.Add(((attackPos.x - sign, attackPos.y + j), this));
                    }

                    if (state >= 4) { finishAttack(); }
                }
                else if (type == (3, 0, 4))   // If magic wand attack
                {
                    int sign = -1;
                    if (direction.x > 0) { sign = 1; }

                    // draw the wooden staff, 1 pixel, starts on top of player, then diag, then in front. If in front of player, add the "magic pixel" (lmao) that's purple
                    (int x, int y) mod;
                    if (state < 1) { mod = (0, 1); }
                    else if (state == 1) { mod = (1, 1); }
                    else
                    {
                        if (state == 2) { pos = (motherEntity.posX, motherEntity.posY); }
                        mod = (1, 0);
                        int posoX = startPos.x + state * sign;
                        posToDrawList.Add(((posoX, startPos.y), Color.BlueViolet));
                        posToAttackList.Add(((posoX, startPos.y), this));
                    }
                    posToDrawList.Add(((pos.x + mod.x * sign, pos.y + mod.y), Color.FromArgb(140, 140, 50)));


                    if (state >= 10) { finishAttack(); }
                }
                else { finishAttack(); }
            }
            public void sendAttack((int x, int y) attackPos)
            {
                Chunk chunkToTest = motherEntity.screen.getChunkFromPixelPos(attackPos);
                int abort = 0;
                if (traits.isTerrainDigging)
                {
                    ((int type, int subType) dugTile, bool success) returnTuple = motherEntity.TerrainDig(attackPos);
                    if (!returnTuple.success) { abort++; }
                    digSuccess = returnTuple.success;
                    dugTile = returnTuple.dugTile;
                }
                if (traits.isPlantDigging)
                {
                    digSuccess = motherEntity.PlantDig(attackPos, (type.type, type.subType, 4), chunkToTest);
                    if (!digSuccess) { abort++; }
                }
                if (type == (3, 0, 4)) { if (motherEntity.screen.type.type != 2) testForBloodAltar(motherEntity.screen, attackPos); }

                if (!traits.isHitting) { return; }
                foreach (Entity entity in chunkToTest.entityList)
                {
                    if (entity.type != motherEntity.type && (entity.posX, entity.posY) == attackPos && !entitiesAlreadyHitByCurrentAttack.ContainsKey(entity.id))
                    {
                        entity.hp -= traits.damage;
                        entitiesAlreadyHitByCurrentAttack[entity.id] = true;
                        entity.timeAtLastGottenHit = timeElapsed;
                        if (entity.hp <= 0)
                        {
                            entity.dieAndDrop(motherEntity);
                        }
                    }
                }
                if (traits.isAbortable && (abort >= 1 + (traits.isPlantDigging && traits.isPlantDigging ? 1 : 0))) { finishAttack(); }
            }
            public void finishAttack()
            {
                isDone = true;
                motherEntity.screen.attacksToRemove[this] = true;
            }
        }
    }
}
