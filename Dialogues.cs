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
        public static Dictionary<char, string> lowToUpDict = new Dictionary<char, string> { { 'a', "A" }, { 'b', "B" }, { 'c', "C" }, { 'd', "D" }, { 'e', "E" }, { 'f', "F" }, { 'g', "G" }, { 'h', "H" }, { 'i', "I" }, { 'j', "J" }, { 'k', "K" }, { 'l', "L" }, { 'm', "M" }, { 'n', "N" }, { 'o', "O" }, { 'p', "P" }, { 'q', "Q" }, { 'r', "R" }, { 's', "S" }, { 't', "T" }, { 'u', "U" }, { 'v', "V" }, { 'w', "W" }, { 'x', "X" }, { 'y', "Y" }, { 'z', "Z" } };
        public static Dictionary<char, string> upToLowDict = new Dictionary<char, string> { { 'A', "a" }, { 'B', "b" }, { 'C', "c" }, { 'D', "d" }, { 'E', "e" }, { 'F', "f" }, { 'G', "g" }, { 'H', "h" }, { 'I', "i" }, { 'J', "j" }, { 'K', "k" }, { 'L', "l" }, { 'M', "m" }, { 'N', "n" }, { 'O', "o" }, { 'P', "p" }, { 'Q', "q" }, { 'R', "r" }, { 'S', "s" }, { 'T', "t" }, { 'U', "u" }, { 'V', "v" }, { 'W', "w" }, { 'X', "x" }, { 'Y', "y" }, { 'Z', "z" } };
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

            {"So" , new SP[] { new SP("so", ","), new SP("and", ",") } },
            {"NeedQuestion" , new SP[] { new SP("do you need anything", "?"), new SP("is there anything you need", "?"), new SP("can I help you with some stuff", "?"), new SP("do you need help with something", "?"), new SP("was there anything you needed help with", "?"), new SP("do you want something", "?"), new SP("what can I help you with", "?"), new SP("what can I do for you", "?") } },
            {"WhichBiomeHow" , new SP[] { new SP("are you looking for a specific biome or a biome with a particular characteristic", "?"), new SP("do you want to find a particular biome ? Or do you want to find a biome with a particular characteristic", "?"), new SP("a biome in particular, or a type of biome", "?") } },
            {"WhichBiomeSpecific" , new SP[] { new SP("which biome are you looking for", "?"), new SP("which biome", "?"), new SP("what is the biome you want to find", "?"), new SP("and which biome exactly are you seeking", "?"), new SP("what's the name of the biome you want to reach", "?") } },
            {"WhichBiomeByCharacteristic" , new SP[] { new SP("which type of biome are you looking for", "?"), new SP("which type of biome", "?"), new SP("what is the type of biome you want to find", "?"), new SP("and which type of biome are you seeking", "?"), new SP("what's the type of biome you want to reach", "?") } },
            {"Alright" , new SP[] { new SP("alright"), new SP("alrighty"), new SP("all right"), new SP("okay"), new SP("very well"), new SP("fine"), new SP("got it"), new SP("sure") } },
            {"Goodbye" , new SP[] { new SP("goodbye", "!"), new SP("bye", "!"), new SP("bye bye", "!"), new SP("see ya", "!"), new SP("see you soon", "!"), new SP("see you later", "!"), new SP("take care", "!"), new SP("cheers", "!") } },

            {"ThankYou" , new SP[] { new SP("thanks", "!"), new SP("thanks a lot", "!"), new SP("thank you", "!"), new SP("thanks for your answer", "!"), new SP("thank you very much", "!"), new SP("thank you for your help", "!"), new SP("thank you so much", "!") } },
            {"YoureWelcome" , new SP[] { new SP("you're welcome", "!"), new SP("no problem", "!"), new SP("my pleasure", "!"), new SP("no worries", "!"), new SP("always happy to help", "!"), new SP("glad I could help you", "!"), new SP("anytime", "!") } },

            {"TheClosest" , new SP[] { new SP("the closest"), new SP("the nearest") } },
            {"IsLocated" , new SP[] { new SP("is located"), new SP("is"), new SP("can be found"), new SP("should be") } },

            {"!" , new SP[] { new SP("!"), new SP("!!"), new SP("!!!") } },
            {"?" , new SP[] { new SP("?"), new SP("??"), new SP("???") } },
        };

        public static Dictionary<string, (SP baseSP, (int value, SP sp)[] array)> numberBasedSentenceParts = new Dictionary<string, (SP baseSP, (int value, SP sp)[] array)>()
        {
            {"BiomeSize" , (new SP("tiny"), new (int value, SP sp)[] { (25, new SP("small")), (75, new SP("medium")), (200, new SP("big")), (600, new SP("huge")), (1500, new SP("giant")), (5000, new SP("gargantuan")) }) },
        };

        public static Dictionary<int, (string text, string nextSentenceType)[]> choicesByType = new Dictionary<int, (string text, string nextSentenceType)[]>
        {
            { 0 , new (string text, string nextSentenceType)[] { ("I'm looking for a biome", "BiomeSeekingHow"), ("I'm good", "Goodbye") } },
            { 1 , new (string text, string nextSentenceType)[] { ("A specific biome", "BiomeSeekingSpecific"), ("A type of biome", "BiomeSeekingByCharacteristic") } },
        };
        public static (string text, string nextSentenceType)[] biomeTypeChoices = null;
        public static (string text, string nextSentenceType)[] biomeCharacteristicChoices = null;

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
            public bool hasBeenProceeded;

            public OneSentence() { }
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
                createSentence(dialogue.nextSentences.Count > 0 ? dialogue.nextSentences[dialogue.nextSentences.Count - 1] : dialogue.currentSentence, forceTextAspect);    // yes currentSentence can be null, but that's good !! If trying to update an already existing dialogue by adding new sentences after a choice for example
                findTextWrappingIdx();
                speakingStartTime = timeElapsed;
            }

            public virtual void updateSpeakingStartTimes(float timeToPut) { speakingStartTime = timeToPut; }
            public virtual void createSentence(OneSentence previousSentence, int forceTextAspect)
            {
                if (type == 0) { introductorySentence(previousSentence); }
                else if (type == 1) { introductoryReactionSentence(previousSentence); }
                else if (type == 2) { fuckOffSentence(previousSentence); }
                else if (type == 3) { greetingSentence(previousSentence); }
                else if (type == 4) { greetingFinalResponse(previousSentence); }
                else if (type == 5) { goodbyeSentence(previousSentence); }
                else if (type == 6) { biomeLocalisationSentence(previousSentence); }
                else if (type == 7) { thankYouSentence(previousSentence); }
                else { errorSentence(previousSentence); }
                if (forceTextAspect == 1) { sentence = replaceAllCharactersByDict(sentence, lowToUpDict); }
                else if (forceTextAspect == 2) { sentence = replaceAllCharactersByDict(sentence, upToLowDict); }
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
            public void pickSentencePart(string type, bool[] stateBool, string terminator = "", float c = 1, int? numberValue = null)
            {
                if (c < 1 && (float)rand.NextDouble() > c) { return; }
                SP sp = numberValue is null ? getRandomItem(sentenceParts[type]) : getItemByValueNeeded(numberBasedSentenceParts[type], numberValue.Value);
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
            public void errorSentence(OneSentence previousSentence)
            {
                bool[] S = new bool[2] { true, false };    // 0 : Sentence ended or first sentence, force maj on next.
                writeElement("something went wrong with the dialogues !!! ERROR ERROR !!! BUG !!! BUG BUG !!! DEV please FIX this !!!1!", S, "!");
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
                pickSentencePart("Fine", S);
                if (rand.NextDouble() < 0.65f)
                {
                    writeElement(",", S);
                    pickSentencePart("Thanks", S, ".");
                }
                else { writeElement(".", S); }
            }
            public void goodbyeSentence(OneSentence previousSentence)  // type = 5
            {
                bool[] S = new bool[2] { true, false };    // 0 : Sentence ended or first sentence, force maj on next.
                bool isResponse = previousSentence != null && previousSentence.type == type;
                if (!isResponse) { pickSentencePart("Alright", S, ",", c: 0.45f); }
                pickSentencePart("Goodbye", S);
            }
            public void biomeLocalisationSentence(OneSentence previousSentence)  // type = 6
            {
                bool[] S = new bool[2] { true, false };    // 0 : Sentence ended or first sentence, force maj on next.
                if (previousSentence is null || !(previousSentence is OneChoice)) { writeElement("ERROR ERROR (previous sentence was null of not a choice)", S); return; }
                TextBox chosenTextBox = ((OneChoice)previousSentence).chosenTextBox;
                if (chosenTextBox is null) { writeElement("ERROR ERROR (chosenTextBox was null)", S); return; }
                if (previousSentence.type != 3 && !biomeNamesToTypesDict.ContainsKey(chosenTextBox.text)) { writeElement("I have no idea what this biome is, sorry...", S); return; }

                (BiomeTraits biome, (int x, int y) pos, int biomeSize)? biomeLocation = speaker.screen.findChunkWithBiome(speaker, 10, previousSentence.type == 3 ? (-1, -1) : biomeNamesToTypesDict[chosenTextBox.text], previousSentence.type == 3 ? chosenTextBox.text : "");  // This can crash if biome name doesn't exist (shouldn't).
                if (biomeLocation is null) { writeElement("I don't know where you can find that biome, sorry...", S); return; }
                (int x, int y) diff = (biomeLocation.Value.pos.x * 32 - speaker.posX, biomeLocation.Value.pos.y * 32 - speaker.posY);
                diff = (Abs(diff.x) < 40 ? 0 : AbsFloor(diff.x, 10), Abs(diff.y) < 40 ? 0 : AbsFloor(diff.y, 10));

                pickSentencePart("TheClosest", S);
                writeElement(chosenTextBox.text + " biome", S);
                pickSentencePart("IsLocated", S);
                writeElement("around", S);
                if (diff.x == 0 && diff.y == 0) { writeElement("... here !", S); return; }

                if (diff.x != 0)
                {
                    writeElement(Abs(diff.x).ToString(), S);
                    writeElement("tiles", S);
                    writeElement(diff.x > 0 ? "to the right" : "to the left", S);
                    if (diff.y != 0) { writeElement("and", S); }
                    else { writeElement("of", S); }
                }

                if (diff.y != 0)
                {
                    writeElement(Abs(diff.y).ToString(), S);
                    writeElement("tiles", S);
                    writeElement(diff.y > 0 ? "above" : "under", S);
                }
                writeElement("where we are", S, ".");
                if (previousSentence.type == 3)
                {
                    writeElement("it is a", S);
                    pickSentencePart("BiomeSize", S, numberValue: biomeLocation.Value.biomeSize);
                    writeElement(biomeLocation.Value.biome.name + " biome", S, ".");
                }
                else
                {
                    writeElement("it is", S);
                    pickSentencePart("BiomeSize", S, "!", numberValue: biomeLocation.Value.biomeSize);
                }
            }
            public void thankYouSentence(OneSentence previousSentence)  // type = 7
            {
                bool[] S = new bool[2] { true, false };    // 0 : Sentence ended or first sentence, force maj on next.
                bool isResponse = previousSentence != null && previousSentence.type == type;
                if (isResponse) { pickSentencePart("YoureWelcome", S); }
                else { pickSentencePart("ThankYou", S); }
            }
            public virtual int renderSentence(Bitmap bitmap, (int x, int y) pos)
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
                if (currentSentence is OneChoice && !((OneChoice)currentSentence).choiceDone)   // If current sentence is a non-chosen choice, don't allow it to be skipped, but force all text to appear
                {
                    currentSentence.hasBeenProceeded = true;
                    currentSentence.updateSpeakingStartTimes(currentSentence.hasBeenProceeded ? -999999 : timeElapsed);
                    return false;
                }
                currentSentence.hasBeenProceeded = true;
                previousSentences.Add(currentSentence);
                if (nextSentences.Count == 0) { return stopTalking(); } // Finished talking
                currentSentence = nextSentences[0];
                nextSentences.RemoveAt(0);
                currentSentence.updateSpeakingStartTimes(currentSentence.hasBeenProceeded ? -999999 : timeElapsed);
                return false;   // Not finished talking
            }
            public void goBack()
            {
                if (previousSentences.Count == 0) { return; }
                currentSentence.hasBeenProceeded = true;
                nextSentences.Insert(0, currentSentence);
                currentSentence = previousSentences[previousSentences.Count - 1];
                previousSentences.RemoveAt(previousSentences.Count - 1);
                currentSentence.updateSpeakingStartTimes(-999999);
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
            public void updateDialogue(string dialogueToAdd)
            {
                if (dialogueToAdd == "Goodbye") { goodbyeDialogue(); }
                else if (dialogueToAdd == "BiomeSeekingHow") { biomeSeekingHowChoice(); }
                else if (dialogueToAdd == "BiomeSeekingSpecific") { biomeSeekingSpecificChoice(); }
                else if (dialogueToAdd == "BiomeSeekingByCharacteristic") { biomeSeekingByCharacteristicChoice(); }
                else if (dialogueToAdd == "BiomeSeeking") { biomeSeekingAnswer(); }
                else { errorDialogue(); }
            }
            public void errorDialogue()
            {
                nextSentences.Add(new OneSentence(this, speakers[1], speakers[0], -1));
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
                nextSentences.Add(new OneChoice(this, speakers[1], speakers[0], 0));
            }
            public void goodbyeDialogue()
            {
                nextSentences.Add(new OneSentence(this, speakers[1], speakers[0], 5));
                nextSentences.Add(new OneSentence(this, speakers[0], speakers[1], 5));
            }
            public void biomeSeekingHowChoice()
            {
                nextSentences.Add(new OneChoice(this, speakers[1], speakers[0], 1));
            }
            public void biomeSeekingSpecificChoice()
            {
                nextSentences.Add(new OneMultiChoice(this, speakers[1], speakers[0], 2));
            }
            public void biomeSeekingByCharacteristicChoice()
            {
                nextSentences.Add(new OneMultiChoice(this, speakers[1], speakers[0], 3));
            }
            public void biomeSeekingAnswer()
            {
                nextSentences.Add(new OneSentence(this, speakers[1], speakers[0], 6));
                nextSentences.Add(new OneSentence(this, speakers[0], speakers[1], 7));
                nextSentences.Add(new OneSentence(this, speakers[1], speakers[0], 7));
            }
            public void hateDialogue()
            {
                nextSentences.Add(new OneSentence(this, speakers[1], speakers[0], 2, forceTextAspect:1));
            }
        }

        public class TextBox
        {
            public Game game;
            public (int x, int y) pos;
            public string text;
            public string nextSentenceType;
            public int talkingSpeed = 100;
            public float activationTime;
            public float clickabilityTime;
            public (int x, int y) rectPos;
            public (int x, int y) rectSize;
            public int minX;
            public int maxX;
            public int minY;
            public int maxY;
            public bool hasBeenSelected = false;

            public TextBox() { }
            public TextBox(Game gameToPut, (string text, string nextSentenceType) tupelo, (int x, int y) posToPut)
            {
                game = gameToPut;
                pos = posToPut;
                text = tupelo.text;
                nextSentenceType = tupelo.nextSentenceType;
                findBoundaries();
                activationTime = timeElapsed;
                clickabilityTime = activationTime + (float)text.Length / talkingSpeed;
            }

            public void setHasBeenSelected(bool newValue) { hasBeenSelected = newValue; }
            public virtual void updateActivationTime(float newValue)
            {
                activationTime = newValue;
                clickabilityTime = activationTime + (float)text.Length / talkingSpeed;
            }
            public void findBoundaries()
            {
                rectPos = (pos.x - 16, pos.y - 16);
                rectSize = (text.Length * 7 + 32, 32);
                minX = pos.x - 16;
                maxX = pos.x + text.Length * 7 + 16;
                minY = pos.y - 16;
                maxY = pos.y + 16;
            }
            public virtual bool checkIfInBoundaries((int x, int y) posToCheck)
            {
                if (timeElapsed < clickabilityTime || posToCheck.x < minX || posToCheck.x > maxX || posToCheck.y < minY || posToCheck.y > maxY) { return false; }
                return true;
            }
            public virtual void drawTextBoxBehind(Bitmap bitmap, (int x, int y) mousePos, bool dialogueDone)
            {
                if (timeElapsed < activationTime) { return; }
                Color colorToPaste;
                if (hasBeenSelected || (!dialogueDone && checkIfInBoundaries(mousePos))) { colorToPaste = Color.FromArgb(120, 180, 120); }
                else { colorToPaste = Color.FromArgb(100, 100, 100); }
                drawRectangle(bitmap, colorToPaste, rectPos, rectSize);
            }
            public virtual void drawText(Bitmap bitmap)
            {
                if (timeElapsed < activationTime) { return; }
                string sentenceToPrint = text.Substring(0, Clamp(0, (int)(talkingSpeed * (timeElapsed - activationTime)), text.Length));
                drawString(bitmap, sentenceToPrint, pos, 1, true);
            }
        }

        public class ChoiceListTextBox : TextBox
        {
            public (string text, string nextSentenceType)[] choiceArray;
            public int choiceArrayCursor;
            public TextBox leftArrow;
            public TextBox rightArrow;

            public ChoiceListTextBox(Game gameToPut, (string text, string nextSentenceType)[] choiceArrayToPut, (int x, int y) posToPut)
            {
                game = gameToPut;
                choiceArray = choiceArrayToPut;
                pos = posToPut;
                leftArrow = new TextBox(gameToPut, ("<-", "Irrelevant"), (pos.x, pos.y - 32));
                rightArrow = new TextBox(gameToPut, ("->", "Irrelevant"), (pos.x + 46, pos.y - 32));
                updateCurrentChoice(0);
                activationTime = timeElapsed;
                clickabilityTime = activationTime + (float)text.Length / talkingSpeed;
            }
            public void updateCurrentChoice(int cursorValueChange)
            {
                choiceArrayCursor = PosMod(choiceArrayCursor + cursorValueChange, choiceArray.Length);
                text = choiceArray[choiceArrayCursor].text;
                nextSentenceType = choiceArray[choiceArrayCursor].nextSentenceType;
                findAllBoundaries();
            }
            public override void updateActivationTime(float newValue)
            {
                activationTime = newValue;
                clickabilityTime = activationTime + (float)text.Length / talkingSpeed;
                leftArrow.updateActivationTime(newValue);
                rightArrow.updateActivationTime(newValue);
            }
            public void findAllBoundaries()
            {
                findBoundaries();
                leftArrow.findBoundaries();
                rightArrow.findBoundaries();
            }
            public override bool checkIfInBoundaries((int x, int y) posToCheck)
            {
                if (leftArrow.checkIfInBoundaries(posToCheck)) { if (leftMouseClick && game.mouseInOverlayPictureBox) { updateCurrentChoice(-1); } return false; }
                if (rightArrow.checkIfInBoundaries(posToCheck)) { if (leftMouseClick && game.mouseInOverlayPictureBox) { updateCurrentChoice(1); } return false; }

                if (timeElapsed < clickabilityTime || posToCheck.x < minX || posToCheck.x > maxX || posToCheck.y < minY || posToCheck.y > maxY) { return false; }
                return true;
            }
            public override void drawTextBoxBehind(Bitmap bitmap, (int x, int y) mousePos, bool dialogueDone)
            {
                if (timeElapsed < activationTime) { return; }
                Color colorToPaste;
                if (hasBeenSelected || (!dialogueDone && checkIfInBoundaries(mousePos))) { colorToPaste = Color.FromArgb(120, 180, 120); }
                else { colorToPaste = Color.FromArgb(100, 100, 100); }
                drawRectangle(bitmap, colorToPaste, rectPos, rectSize);
                leftArrow.drawTextBoxBehind(bitmap, mousePos, dialogueDone);
                rightArrow.drawTextBoxBehind(bitmap, mousePos, dialogueDone);
            }
            public override void drawText(Bitmap bitmap)
            {
                if (timeElapsed < activationTime) { return; }
                string sentenceToPrint = text.Substring(0, Clamp(0, (int)(talkingSpeed * (timeElapsed - activationTime)), text.Length));
                drawString(bitmap, sentenceToPrint, pos, 1, true);
                leftArrow.drawText(bitmap);
                rightArrow.drawText(bitmap);
            }
        }

        public class OneChoice : OneSentence
        {
            public TextBox[] choiceTextBoxes;
            public bool choiceDone = false;
            public TextBox chosenTextBox;

            public OneChoice() { }
            public OneChoice(Entity speakerToPut, Entity nonSpeakerToPut, string sentenceToPut)
            {
                speaker = speakerToPut;
                nonSpeaker = nonSpeakerToPut;
                sentence = sentenceToPut;
                speakingStartTime = timeElapsed;
            }
            public OneChoice(Dialogue dialogueToPut, Entity speakerToPut, Entity nonSpeakerToPut, int typeToPut, int forceTextAspect = 0)
            {
                dialogue = dialogueToPut;
                speaker = speakerToPut;
                nonSpeaker = nonSpeakerToPut;
                type = typeToPut;
                sentence = "";
                OneSentence previousSentence = dialogue.nextSentences.Count > 0 ? dialogue.nextSentences[dialogue.nextSentences.Count - 1] : null;
                createSentence(previousSentence, forceTextAspect);
                findTextWrappingIdx();
                speakingStartTime = timeElapsed;

                makeTextBoxes();
            }

            public virtual void setChoiceDone(bool newValue, TextBox chosenTextBoxToPut) { choiceDone = newValue; chosenTextBox = choiceDone ? chosenTextBoxToPut : null; }
            public override void updateSpeakingStartTimes(float timeToPut)
            {
                speakingStartTime = timeToPut;
                float newActivationTime = speakingStartTime + sentence.Length / (float)speaker.GetPersonality().talkingSpeed;
                foreach(TextBox textBox in choiceTextBoxes) { textBox.updateActivationTime(newActivationTime); }
            }
            public override void createSentence(OneSentence previousSentence, int forceTextAspect)
            {
                if (type == 0) { doYouNeedAnythingChoice(previousSentence); }
                else if (type == 1) { biomeSeekingChoice(previousSentence); }
                else if (type == 2) { biomeSeekingChoiceSpecific(previousSentence); }
                else if (type == 3) { biomeSeekingChoiceByCharacteristic(previousSentence); }
                else { errorSentence(previousSentence); }
                if (forceTextAspect == 1) { sentence = replaceAllCharactersByDict(sentence, lowToUpDict); }
                if (forceTextAspect == 2) { sentence = replaceAllCharactersByDict(sentence, upToLowDict); }
            }
            public void doYouNeedAnythingChoice(OneSentence previousSentence)
            {
                bool[] S = new bool[2] { true, false };    // 0 : Sentence ended or first sentence, force maj on next.
                bool isResponse = previousSentence != null && previousSentence.type == type;
                pickSentencePart("So", S, c:0.35f);
                pickSentencePart("NeedQuestion", S);
            }
            public void biomeSeekingChoice(OneSentence previousSentence)
            {
                bool[] S = new bool[2] { true, false };    // 0 : Sentence ended or first sentence, force maj on next.
                bool isResponse = previousSentence != null && previousSentence.type == type;
                pickSentencePart("So", S, c: 0.35f);
                pickSentencePart("WhichBiomeHow", S);
            }
            public void biomeSeekingChoiceSpecific(OneSentence previousSentence)
            {
                bool[] S = new bool[2] { true, false };    // 0 : Sentence ended or first sentence, force maj on next.
                bool isResponse = previousSentence != null && previousSentence.type == type;
                pickSentencePart("So", S, c: 0.25f);
                pickSentencePart("WhichBiomeSpecific", S);
            }
            public void biomeSeekingChoiceByCharacteristic(OneSentence previousSentence)
            {
                bool[] S = new bool[2] { true, false };    // 0 : Sentence ended or first sentence, force maj on next.
                bool isResponse = previousSentence != null && previousSentence.type == type;
                pickSentencePart("So", S, c: 0.25f);
                pickSentencePart("WhichBiomeByCharacteristic", S);
            }
            public virtual void makeTextBoxes()
            {
                List<TextBox> listoTextoBoxo = new List<TextBox>();
                (string text, string nextSentenceType)[] stringoArrayo = choicesByType[type];
                int counto = stringoArrayo.Length;
                int i = 0;
                foreach ((string text, string nextSentenceType) choice in stringoArrayo)
                {
                    if (i == 0) { listoTextoBoxo.Add(new TextBox(dialogue.game, choice, (100, 110))); }
                    else if (i == 1) { listoTextoBoxo.Add(new TextBox(dialogue.game, choice, (300, 110))); }
                    i++;
                }
                choiceTextBoxes = listoTextoBoxo.ToArray();
            }
            public void drawTextBoxesBehind(Bitmap bitmap)
            {
                foreach(TextBox textBox in choiceTextBoxes) { textBox.drawTextBoxBehind(bitmap, dialogue.game.overlayMousePosition, choiceDone); }
            }
            public TextBox returnTextBoxClicked()
            {
                foreach (TextBox textBox in choiceTextBoxes) { if (textBox.checkIfInBoundaries(dialogue.game.overlayMousePosition)) { return textBox; } }
                return null;
            }
            public override int renderSentence(Bitmap bitmap, (int x, int y) pos)
            {
                foreach (TextBox textBox in choiceTextBoxes) { textBox.drawText(bitmap); }

                EntityPersonality personality = speaker.GetPersonality();
                string sentenceToPrint = sentence.Substring(0, Clamp(0, (int)(personality.talkingSpeed * (timeElapsed - speakingStartTime)), sentence.Length));
                return drawString(bitmap, sentenceToPrint, pos, size, true, personality.letterHoverSpeed, this);
            }
        }

        public class OneMultiChoice : OneChoice
        {
            public OneMultiChoice(Entity speakerToPut, Entity nonSpeakerToPut, string sentenceToPut)
            {
                speaker = speakerToPut;
                nonSpeaker = nonSpeakerToPut;
                sentence = sentenceToPut;
                speakingStartTime = timeElapsed;
            }
            public OneMultiChoice(Dialogue dialogueToPut, Entity speakerToPut, Entity nonSpeakerToPut, int typeToPut, int forceTextAspect = 0)
            {
                dialogue = dialogueToPut;
                speaker = speakerToPut;
                nonSpeaker = nonSpeakerToPut;
                type = typeToPut;
                sentence = "";
                OneSentence previousSentence = dialogue.nextSentences.Count > 0 ? dialogue.nextSentences[dialogue.nextSentences.Count - 1] : null;
                createSentence(previousSentence, forceTextAspect);
                findTextWrappingIdx();
                speakingStartTime = timeElapsed;

                makeTextBoxes();
            }

            public override void setChoiceDone(bool newValue, TextBox chosenTextBoxToPut) { choiceDone = newValue; }
            public override void makeTextBoxes()
            {
                if (type == 2) { chosenTextBox = new ChoiceListTextBox(dialogue.game, biomeTypeChoices, (120, 110)); ; }
                else if (type == 3) { chosenTextBox = new ChoiceListTextBox(dialogue.game, biomeCharacteristicChoices, (120, 110)); }
                else { chosenTextBox = new ChoiceListTextBox(dialogue.game, biomeTypeChoices, (120, 110)); }    // This should never happen, default when type not supported
                choiceTextBoxes = new TextBox[] { chosenTextBox };
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
