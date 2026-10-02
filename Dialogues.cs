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
        public static Dictionary<char, string> lowToUpDict = new Dictionary<char, string> { {'a', "A" }, {'b', "B" }, {'c', "C" }, {'d', "D" }, {'e', "E" }, {'f', "F" }, {'g', "G" }, {'h', "H" }, {'i', "I" }, {'j', "J" }, {'k', "K" }, {'l', "L" }, {'m', "M" }, {'n', "N" }, {'o', "O" }, {'p', "P" }, {'q', "Q" }, {'r', "R" }, {'s', "S" }, {'t', "T" }, {'u', "U" }, {'v', "V" }, {'w', "W" }, {'x', "X" }, {'y', "Y" }, {'z', "Z" } };
        public static Dictionary<char, string> upToLowDict = new Dictionary<char, string> { {'A', "a" }, {'B', "b" }, {'C', "c" }, {'D', "d" }, {'E', "e" }, {'F', "f" }, {'G', "g" }, {'H', "h" }, {'I', "i" }, {'J', "j" }, {'K', "k" }, {'L', "l" }, {'M', "m" }, {'N', "n" }, {'O', "o" }, {'P', "p" }, {'Q', "q" }, {'R', "r" }, {'S', "s" }, {'T', "t" }, {'U', "u" }, {'V', "v" }, {'W', "w" }, {'X', "x" }, {'Y', "y" }, {'Z', "z" } };
        public static HashSet<string> trimmerCharacters = new HashSet<string> { ".", ",", "!", "?", ":", ";" };

        public static Dictionary<string, SP[]> sentenceParts = new Dictionary<string, SP[]>
        {
            {"BaseGreeting" , new SP[] { new SP("hi"), new SP("hello"), new SP("hey"), new SP("hey there"), new SP("hi there") } },
            {"Greeting" , new SP[] { new SP("hi"), new SP("hello"), new SP("hey"), new SP("what's up", "?"), new SP("hey there"), new SP("hi there") } },
            {"Introduction" , new SP[] { new SP("I'm"), new SP("I am"), new SP("my name is"), new SP("my name's") } },
            {"IntroductionReaction" , new SP[] { new SP("nice to meet you"), new SP("glad to meet you"), new SP("happy to meet you") } },
            {"NameQuestion" , new SP[] { new SP("who are you", "?"), new SP("what's your name", "?") } },

            {"HowAreYou" , new SP[] { new SP("how are you", "?"), new SP("how are you doing", "?"), new SP("how's it going", "?"), new SP("what's up", "?"), new SP("how are things", "?"), new SP("how have you been", "?"), new SP("what are you up to", "?") } },
            {"Fine" , new SP[] { new SP("I'm doing good"), new SP("I'm good"), new SP("I'm doing fine"), new SP("I'm fine"), new SP("things are going pretty well"), new SP("I'm doing great"), new SP("I'm not doing too bad") } },
            {"Thanks" , new SP[] { new SP("thanks"), new SP("thanks for asking") } },
            {"ReturnQuestion" , new SP[] { new SP("and you", "?"), new SP("what about you", "?") } },

            {"Interjection" , new SP[] { new SP("ah"), new SP("oh") } },
            {"Exaggeration" , new SP[] { new SP("very"), new SP("so"), new SP("extremely"), new SP("super"), new SP("really") } },
            {"Diminution" , new SP[] { new SP("not very"), new SP("not so"), new SP("not super"), new SP("not really") } },
            {"Too" , new SP[] { new SP("too"), new SP("as well") } },

            {"FuckOff" , new SP[] { new SP("fuck you"), new SP("get lost"), new SP("go die"), new SP("get away from me"), new SP("get the fuck away from me"), new SP("fuck off"), new SP("piss off"), new SP("go to hell"), new SP("go fuck yourself"), new SP("fuck you"), new SP("buzz off") } },
            {"Insult" , new SP[] { new SP("cunt"), new SP("piece of shit"), new SP("asshole"), new SP("dickhead"), new SP("moron") , new SP("idiot") , new SP("dumbass") , new SP("loser"), new SP("jackass"), new SP("jerk"), new SP("goon"), new SP("cretin"), new SP("imbecile"), new SP("donkey"), new SP("creep"), new SP("dick"), new SP("scum"), new SP("shitbag"), new SP("shithead"), new SP("fucker"), new SP("pig"), new SP("bastard"), new SP("motherfucker"), new SP("clown"), new SP("dipshit"), new SP("chud") } },
            {"InsultAdjective" , new SP[] { new SP("fucking"), new SP("fuckass"), new SP("freaking"), new SP("damn"), new SP("damned"), new SP("stupid"), new SP("effing"), new SP("miserable"), new SP("complete"), new SP("despicable"), new SP("filthy"), new SP("disgusting"), new SP("stinky"), new SP("smelly"), new SP("vile"), new SP("pathetic"), new SP("useless"), new SP("worthless"), new SP("lame"), new SP("degenerate"), new SP("shitty") } },

            {"!" , new SP[] { new SP("!"), new SP("!!"), new SP("!!!") } },
            {"?" , new SP[] { new SP("?"), new SP("??"), new SP("???") } },
        };

        public static Dictionary<(int entityId1, int entityId2), OneRelationship> relationshipsDict = new Dictionary<(int entityId1, int entityId2), OneRelationship>();
    }
    public class Dialogues
    {
        public class SP     // for sentence part
        {
            public string part;
            public string baseTerminator;
            public bool isSentenceEnd;

            public SP(string partToPut, string bT = "", bool iSE = false)
            {
                part = partToPut;
                baseTerminator = bT;
                isSentenceEnd = iSE || (baseTerminator != "," && baseTerminator != "");
            }
        }



        public class OneSentence
        {
            public Dialogue dialogue;
            public Entity speaker;
            public Entity nonSpeaker;
            public int type = -1;
            public string sentence;
            public int[] textWrappingIdx;
            public Color? color;
            public int font;
            public int size = 1;
            public float speakingStartTime;

            public OneSentence(Entity speakerToPut, Entity nonSpeakerToPut, string sentenceToPut)
            {
                speaker = speakerToPut;
                nonSpeaker = nonSpeakerToPut;
                sentence = sentenceToPut;
                speakingStartTime = timeElapsed;
            }
            public OneSentence(Dialogue dialogueToPut, Entity speakerToPut, Entity nonSpeakerToPut, int typeToPut, int forceTextAspect = 0)
            {
                dialogue = dialogueToPut;
                speaker = speakerToPut;
                nonSpeaker = nonSpeakerToPut;
                type = typeToPut;
                sentence = "";
                OneSentence previousSentence = dialogue.nextSentences.Count > 0 ? dialogue.nextSentences[dialogue.nextSentences.Count - 1] : null;
                if (type == 0) { introductorySentence(previousSentence); }
                if (type == 1) { introductoryReactionSentence(previousSentence); }
                if (type == 2) { fuckOffSentence(previousSentence); }
                if (type == 3) { greetingSentence(previousSentence); }
                if (type == 4) { greetingFinalResponse(previousSentence); }
                if (forceTextAspect == 1) { sentence = replaceAllCharactersByDict(sentence, lowToUpDict); }
                if (forceTextAspect == 2) { sentence = replaceAllCharactersByDict(sentence, upToLowDict); }
                findTextWrappingIdx();
                speakingStartTime = timeElapsed;
            }

            public void findTextWrappingIdx()
            {
                List<int> listo = new List<int>();
                int lastWordStartIdx = 0;
                int j = 0;
                for (int i = 0; i < sentence.Length; i++)
                {
                    if (sentence[i] == ' ') { lastWordStartIdx = i + 1; }
                    if (j >= 39)
                    {
                        j = i - lastWordStartIdx;
                        listo.Add(lastWordStartIdx);
                    }
                    j++;
                }
                textWrappingIdx = listo.ToArray();
            }
            public void pickSentencePart(string type, bool[] stateBool, string terminator = "", float c = 1)
            {
                if (c < 1 && (float)rand.NextDouble() > c) { return; }
                SP sp = getRandomItem(sentenceParts[type]);
                string tempPart = sp.part;
                if (sentence.Length > 0 && tempPart.Length > 0 && trimmerCharacters.Contains(tempPart.Substring(0, 1)) && sentence[sentence.Length - 1] == ' ') { sentence = sentence.Substring(0, sentence.Length - 1); } // If there's a space before AND putting a punctuation mark as first of SP, remove the space not to have a weird space.
                if (stateBool[0])   // If previous sentence was end or is first sentence, force first character to be uppercase
                {
                    tempPart = replaceCharacterAtPosByDict(tempPart, 0, lowToUpDict);
                    stateBool[0] = false;
                }
                if (sp.isSentenceEnd || (terminator != "" && terminator != ","))   // If this is an end sentence
                {
                    stateBool[0] = true;
                }
                sentence += tempPart + (terminator == "" ? sp.baseTerminator : terminator) + " ";
            }
            public void writeElement(string element, bool[] stateBool, string terminator = "", float c = 1)
            {
                if (c < 1 && (float)rand.NextDouble() > c) { return; }
                if (sentence.Length > 0 && trimmerCharacters.Contains(element) && sentence[sentence.Length - 1] == ' ') { sentence = sentence.Substring(0, sentence.Length - 1); } // If there's a space before AND putting a punctuation mark, remove the space not to have a weird space.
                string tempPart = element;
                if (stateBool[0])   // If previous sentence was end or is first sentence, force first character to be uppercase
                {
                    tempPart = replaceCharacterAtPosByDict(tempPart, 0, lowToUpDict);
                    stateBool[0] = false;
                }
                if (terminator != "" && terminator != ",")  // If this is an end sentence
                {
                    stateBool[0] = true;
                }
                sentence += tempPart + terminator + " ";
            }
            public void introductorySentence(OneSentence previousSentence)  // type = 0
            {
                bool[] S = new bool[2] { true, false };    // 0 : Sentence ended or first sentence, force maj on next.
                bool isResponse = previousSentence != null && previousSentence.type == type;
                if (isResponse)
                {
                    pickSentencePart("Interjection", S, c:0.5f);
                    pickSentencePart("Greeting", S, "!");
                }
                else { pickSentencePart("Greeting", S, ","); }
                pickSentencePart("Introduction", S);
                writeElement(speaker.traits.name, S, "!");
                if (!isResponse) { pickSentencePart("NameQuestion", S, c:0.5f); }
            }
            public void introductoryReactionSentence(OneSentence previousSentence)  // type = 1
            {
                bool[] S = new bool[2] { true, false };    // 0 : Sentence ended or first sentence, force maj on next.
                bool isResponse = previousSentence != null && previousSentence.type == type;
                pickSentencePart("IntroductionReaction", S);
                if (isResponse) { pickSentencePart("Too", S); }
                writeElement(nonSpeaker.traits.name, S, "!");
            }
            public void fuckOffSentence(OneSentence previousSentence)   // type = 2
            {
                bool[] S = new bool[2] { true, false };    // 0 : Sentence ended or first sentence, force maj on next.
                pickSentencePart("FuckOff", S);
                writeElement(nonSpeaker.traits.name, S, c:0.7f);
                writeElement("you", S);
                pickSentencePart("InsultAdjective", S, c: 0.6f);
                pickSentencePart("Insult", S);
                if (rand.NextDouble() < 0.5f)
                {
                    pickSentencePart("!", S);
                    pickSentencePart("FuckOff", S);
                    int amount = rand.Next(50) == 0 ? rand.Next(15) + 5 : 0;
                    for (int i = 0; i < amount; i++)
                    {
                        pickSentencePart("!", S);
                        pickSentencePart("FuckOff", S);
                    }
                }
                pickSentencePart("!", S);
            }
            public void greetingSentence(OneSentence previousSentence)  // type = 3
            {
                bool[] S = new bool[2] { true, false };    // 0 : Sentence ended or first sentence, force maj on next.
                bool isResponse = previousSentence != null && previousSentence.type == type;
                if (isResponse) { pickSentencePart("Interjection", S, c: 0.5f); }
                pickSentencePart("BaseGreeting", S);
                writeElement(nonSpeaker.traits.name, S, ",");
                if (isResponse)
                {
                    pickSentencePart("Fine", S, ",");
                    pickSentencePart("Thanks", S, ".", c: 0.35f);
                    if (rand.NextDouble() < 0.4f) { pickSentencePart("ReturnQuestion", S); }
                    else { pickSentencePart("HowAreYou", S); }
                }
                else { pickSentencePart("HowAreYou", S); }
            }
            public void greetingFinalResponse(OneSentence previousSentence)  // type = 4
            {
                bool[] S = new bool[2] { true, false };    // 0 : Sentence ended or first sentence, force maj on next.
                bool isResponse = previousSentence != null && previousSentence.type == type;
                pickSentencePart("Fine", S);
                if (rand.NextDouble() < 0.65f)
                {
                    writeElement(",", S);
                    pickSentencePart("Thanks", S, ".");
                }
                else { writeElement(".", S); }
            }
            public int renderSentence(Bitmap bitmap, (int x, int y) pos)
            {
                EntityPersonality personality = speaker.GetPersonality();
                string sentenceToPrint = sentence.Substring(0, Clamp(0, (int)(personality.talkingSpeed * (timeElapsed - speakingStartTime)), sentence.Length));
                return drawString(bitmap, sentenceToPrint, pos, size, true, personality.letterHoverSpeed, this);
            }
        }

        public class Dialogue
        {
            public Game game;
            public Entity[] speakers;
            public OneSentence currentSentence;
            public List<OneSentence> previousSentences = new List<OneSentence>();
            public List<OneSentence> nextSentences = new List<OneSentence>();
            public bool isFinished = false;

            public Dialogue(Game gameToPut, Entity[] speakersToPut)
            {
                game = gameToPut;
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
            public bool proceed()
            {
                previousSentences.Add(currentSentence);
                if (nextSentences.Count == 0) { return stopTalking(); } // Finished talking
                currentSentence = nextSentences[0];
                nextSentences.RemoveAt(0);
                currentSentence.speakingStartTime = timeElapsed;
                return false;   // Not finished talking
            }
            public void goBack()
            {
                if (previousSentences.Count == 0) { return; }
                nextSentences.Insert(0, currentSentence);
                currentSentence = previousSentences[previousSentences.Count - 1];
                previousSentences.RemoveAt(previousSentences.Count - 1);
                currentSentence.speakingStartTime = timeElapsed;
            }
            public bool stopTalking()
            {
                isFinished = true;
                return isFinished;
            }

            public void chooseDialogue()
            {
                (Entity one, Entity two) relationshipTuple = getRelationshipTuple(speakers[0], speakers[1]);
                OneRelationship relationship = getOneRelationship(game, relationshipTuple.one, relationshipTuple.two);

                if (relationship is null) { introductionDialogue(relationshipTuple); }
                else
                {
                    if (speakers[0] == relationshipTuple.one ? (relationship.reputation2 < 0) : (relationship.reputation1 < 0)) { hateDialogue(); }
                    else { greetingDialogue(); }
                }
            }
            public void introductionDialogue((Entity one, Entity two) speakerTuple)
            {
                new OneRelationship(speakerTuple.one, speakerTuple.two);

                nextSentences.Add(new OneSentence(this, speakers[0], speakers[1], 0));
                nextSentences.Add(new OneSentence(this, speakers[1], speakers[0], 0));
                nextSentences.Add(new OneSentence(this, speakers[0], speakers[1], 1));
                nextSentences.Add(new OneSentence(this, speakers[1], speakers[0], 1));
            }
            public void greetingDialogue()
            {
                nextSentences.Add(new OneSentence(this, speakers[0], speakers[1], 3));
                nextSentences.Add(new OneSentence(this, speakers[1], speakers[0], 3));
                nextSentences.Add(new OneSentence(this, speakers[0], speakers[1], 4));
            }
            public void hateDialogue()
            {
                nextSentences.Add(new OneSentence(this, speakers[1], speakers[0], 2, forceTextAspect:1));
            }
        }

        public class OneRelationship
        {
            public Game game;

            public int entityId1;
            public int entityId2;

            public Entity entity1;
            public Entity entity2;

            public int reputation1; // Reputation of entity 1 towards entity 2
            public int reputation2; // Reputation of entity 1 towards entity 2

            public OneRelationship(OneRelationshipJson oneRelationshipJson, Entity entityOne, Entity entityTwo)  // It is assumed that the entities are in sorted ID
            {
                game = entityOne.screen.game;

                entity1 = entityOne;
                entity2 = entityTwo;

                entityId1 = entity1.id;
                entityId2 = entity2.id;

                reputation1 = oneRelationshipJson.V[0];
                reputation2 = oneRelationshipJson.V[1];

                if (relationshipsDict.ContainsKey((entityId1, entityId2))) { return; }  // This should NEVER happen
                relationshipsDict[(entityId1, entityId2)] = this;
            }
            public OneRelationship(Entity entityOne, Entity entityTwo)  // It is assumed that the entities are in sorted ID
            {
                game = entityOne.screen.game;

                entity1 = entityOne;
                entity2 = entityTwo;

                entityId1 = entity1.id;
                entityId2 = entity2.id;

                setRelationshipScoreTo(entityId1, 1, false);
                setRelationshipScoreTo(entityId2, 1, false);

                if (relationshipsDict.ContainsKey((entityId1, entityId2))) { return; }  // This should NEVER happen
                relationshipsDict[(entityId1, entityId2)] = this;

                saveOneRelationship(game, this);
            }
            public void setRelationshipScoreTo(int entityId, int scoreToPut, bool save = true)
            {
                if (entityId1 == entityId) { reputation1 = scoreToPut; }
                else { reputation2 = scoreToPut; }
                if (save) { saveOneRelationship(game, this); }
            }
        }

        public static (Entity one, Entity two) getRelationshipTuple(Entity entity1, Entity entity2)
        {
            (Entity one, Entity two) relationshipTuple;
            if (entity1.id < entity2.id) { relationshipTuple = (entity1, entity2); }
            else { relationshipTuple = (entity2, entity1); }
            return relationshipTuple;
        }
        public static OneRelationship getOneRelationship(Game game, Entity entity1, Entity entity2)
        {
            (int one, int two) relationshipTuple;
            if (entity1.id < entity2.id) { relationshipTuple = (entity1.id, entity2.id); }
            else { relationshipTuple = (entity1.id, entity2.id); }
            if (relationshipsDict.ContainsKey(relationshipTuple)) { return relationshipsDict[relationshipTuple]; }
            OneRelationshipJson json = tryLoadOneRelationship(game, entity1.id, entity2.id);
            if (json is null) { return null; }
            return new OneRelationship(json, entity1, entity2);
        }

        public static string replaceCharacterAtPosByDict(string sentence, int pos, Dictionary<char, string> dict)
        {
            if (sentence.Count() <= pos || !dict.ContainsKey(sentence[pos])) { return sentence; }
            return sentence.Substring(0, pos) + dict[sentence[pos]] + (sentence.Count() <= pos + 1 ? "" : sentence.Substring(pos + 1));
        }
        public static string replaceAllCharactersByDict(string sentence, Dictionary<char, string> dict)
        {
            string newSentence = "";
            foreach (char c in sentence)
            {
                if (dict.ContainsKey(c)) { newSentence += dict[c]; }
                else { newSentence += c; }
            }
            return newSentence;
        }
    }
}
