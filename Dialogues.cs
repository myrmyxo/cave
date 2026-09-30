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
    public partial class Globals
    {
        public static Dictionary<((int type, int subType) type, int state), string> sentences = new Dictionary<((int type, int subType) type, int state), string>()
        {
            { ((0, 0), 0), "Hi !"},
            { ((0, 1), 0), "HII !"},
            { ((0, 2), 0), "H-Hkhkhk-Hi !"},
            { ((0, 3), 0), "Klonkhi !"},
            { ((1, 0), 0), "Ribbit !"},
            { ((1, 1), 0), "Sprotch !"},
            { ((1, 2), 0), "Klank !"},
            { ((2, 0), 0), "Blub !"},
            { ((2, 1), 0), "Blonk !"},
            { ((3, 0), 0), "..."},
            { ((3, 1), 0), "Ab ! Ab !"},
            { ((3, 2), 0), "...?"},
            { ((3, 3), 0), "ZZZZZZZZZ !"},
            { ((4, 0), 0), "Slush !"},
            { ((4, 1), 0), "Slirk !"},
            { ((5, 0), 0), "Snyoooom !"},
            { ((6, 1), 0), "Gobli Goblou !"},
        };

        public static Dictionary<(int entityId1, int entityId2), OneRelationship> relationshipsDict = new Dictionary<(int entityId1, int entityId2), OneRelationship>();
    }
    public class Dialogues
    {
        public class OneSentence
        {
            public Entity speaker;
            public string sentence;
            public Color? color;
            public int font;
            public int size = 1;

            public OneSentence(Entity speakerToPut, string sentenceToPut)
            {
                speaker = speakerToPut;
                sentence = sentenceToPut;
            }
        }

        public class Dialogue
        {
            public Entity[] speakers;
            OneSentence currentSentence;
            List<OneSentence> previousSentences = new List<OneSentence>();
            List<OneSentence> nextSentences = new List<OneSentence>();
            public bool isFinished = false;

            public Dialogue(Entity[] speakersToPut)
            {
                speakers = speakersToPut;

                chooseDialogue();

                if (nextSentences.Count == 0) { stopTalking(); }
                currentSentence = nextSentences[0];
                nextSentences.RemoveAt(0);
            }
            public void renderSpeakerSprite(Bitmap bitmap, (int x, int y) pos)
            {
                if (currentSentence.speaker is Player)
                {
                    Bitmap bitmapo = portraitSprites[currentSentence.speaker.type].bitmap;
                    bitmapo.RotateFlip((RotateFlipType.RotateNoneFlipX));
                    Sprites.drawSpriteOnCanvas(bitmap, bitmapo, pos, 4, true);
                    bitmapo.RotateFlip((RotateFlipType.RotateNoneFlipX));
                }
                else { Sprites.drawSpriteOnCanvas(bitmap, portraitSprites[currentSentence.speaker.type].bitmap, pos, 4, true); }
            }
            public int renderCurrentSentence(Bitmap bitmap, (int x, int y) pos)
            {
                return drawString(bitmap, currentSentence.sentence, pos, currentSentence.size, true);
            }
            public bool proceed()
            {
                previousSentences.Add(currentSentence);
                if (nextSentences.Count == 0) { return stopTalking(); } // Finished talking
                currentSentence = nextSentences[0];
                nextSentences.RemoveAt(0);
                return false;   // Not finished talking
            }
            public void goBack()
            {
                if (previousSentences.Count == 0) { return; }
                nextSentences.Insert(0, currentSentence);
                currentSentence = previousSentences[previousSentences.Count - 1];
                previousSentences.RemoveAt(previousSentences.Count - 1);
            }
            public bool stopTalking()
            {
                isFinished = true;
                return isFinished;
            }

            public void chooseDialogue()
            {
                (Entity one, Entity two) relationshipTuple = getRelationshipTuple(speakers[0], speakers[1]);

                if (!relationshipsDict.ContainsKey((relationshipTuple.one.id, relationshipTuple.two.id))) { introductionDialogue(relationshipTuple); }
                else
                {
                    OneRelationship relationship = relationshipsDict[(relationshipTuple.one.id, relationshipTuple.two.id)];
                    if (speakers[0] == relationshipTuple.one ? (relationship.reputation2 < 0) : (relationship.reputation1 < 0)) { hateDialogue(relationship); }
                    else { randomDialogue(relationship); }
                }
            }
            public void introductionDialogue((Entity one, Entity two) speakerTuple)
            {
                OneRelationship relationship = new OneRelationship(speakerTuple.one, speakerTuple.two);

                nextSentences.Add(new OneSentence(speakers[0], "Hi ! I'm " + speakers[0].traits.name + " !"));
                nextSentences.Add(new OneSentence(speakers[1], "Oh hello ! I'm " + speakers[1].traits.name + " !"));
                nextSentences.Add(new OneSentence(speakers[0], "Nice to meet you " + speakers[1].traits.name + " !"));
                nextSentences.Add(new OneSentence(speakers[1], "Nice to meet you too " + speakers[0].traits.name + " !"));
            }
            public void randomDialogue(OneRelationship relationship)
            {
                nextSentences.Add(new OneSentence(speakers[0], "Skibidi"));
                nextSentences.Add(new OneSentence(speakers[1], "Skibidi Sigma Mewing !"));
                nextSentences.Add(new OneSentence(speakers[0], "Pepega Amogus ?"));
                nextSentences.Add(new OneSentence(speakers[1], "Sadge pepega..."));
                nextSentences.Add(new OneSentence(speakers[0], "Poggito !"));
                nextSentences.Add(new OneSentence(speakers[1], "Poggy pog !"));
            }
            public void hateDialogue(OneRelationship relationship)
            {
                nextSentences.Add(new OneSentence(speakers[1], "FUCK OFF " + speakers[0].traits.name + " YOU FUCKING CUNT"));
            }
        }

        public static (Entity one, Entity two) getRelationshipTuple(Entity entity1, Entity entity2)
        {
            (Entity one, Entity two) relationshipTuple;
            if (entity1.id < entity2.id) { relationshipTuple = (entity1, entity2); }
            else { relationshipTuple = (entity2, entity1); }
            return relationshipTuple;
        }
        public static OneRelationship getOneRelationship(Entity entity1, Entity entity2)
        {
            (int one, int two) relationshipTuple;
            if (entity1.id < entity2.id) { relationshipTuple = (entity1.id, entity2.id); }
            else { relationshipTuple = (entity1.id, entity2.id); }
            if (relationshipsDict.ContainsKey(relationshipTuple)) { return relationshipsDict[relationshipTuple]; }
            return null;
        }

        public class OneRelationship
        {
            public int entityId1;
            public int entityId2;

            public Entity entity1;
            public Entity entity2;

            public int reputation1; // Reputation of entity 1 towards entity 2
            public int reputation2; // Reputation of entity 1 towards entity 2

            public OneRelationship(Entity entityOne, Entity entityTwo)  // It is assumed that the entities are in sorted ID
            {
                entity1 = entityOne;
                entity2 = entityTwo;

                entityId1 = entity1.id;
                entityId2 = entity2.id;

                reputation1 = 1;
                reputation2 = 1;

                if (relationshipsDict.ContainsKey((entityId1, entityId2))) {; }  // This should NEVER happen
                relationshipsDict[(entityId1, entityId2)] = this;
            }
        }
    }
}
