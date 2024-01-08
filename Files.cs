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

namespace Cave
{
    public class Files
    {
        public class ChunkJson
        {
            public long chunkSeed;
            public (int, int) position;
            public int[,] fillStates;
            public bool enPlSpawned;
            public List<EntityJson> entityList;
            public List<PlantJson> plantList;
            public ChunkJson(Chunk chunk)
            {
                chunkSeed = chunk.chunkSeed;
                position = chunk.position;
                fillStates = chunk.fillStates;
                enPlSpawned = chunk.entitiesAndPlantsSpawned;
                entityList = new List<EntityJson>();
                foreach (Entity entity in chunk.entityList)
                {
                    entityList.Add(new EntityJson(entity));
                }
                plantList = new List<PlantJson>();
                foreach (Plant plant in chunk.plantList)
                {
                    plantList.Add(new PlantJson(plant));
                }
            }
            public ChunkJson()
            {

            }
        }
        public class EntityJson
        {
            public int seed;
            public (int, int) type;
            public int state;
            public (float, float) pos;
            public (float, float) speed;

            public Dictionary<(int index, int subType, int typeOfElement), int> invQ;
            public List<(int index, int subType, int typeOfElement)> invE;

            public (float, float) lastDP;
            public EntityJson(Entity entity)
            {
                seed = entity.seed;
                type = (entity.type, entity.subType);
                state = entity.state;
                pos = (entity.realPosX, entity.realPosY);
                speed = (entity.speedX, entity.speedY);
                invQ = entity.inventoryQuantities;
                invE = entity.inventoryElements;
                lastDP = (entity.timeAtLastDig, entity.timeAtLastPlace);
            }
            public EntityJson()
            {

            }
        }
        public class PlantJson
        {
            public int seed;
            public (int, int) type;
            public (int, int) pos;
            public (int, int) lstGrPos;
            public int grLvl;
            public float lastGr;

            public int[,] fillStates;

            public List<BranchJson> branches;
            public List<FlowerJson> flowers;
            public PlantJson(Plant plant)
            {
                seed = plant.seed;
                type = (plant.type, plant.subType);
                pos = (plant.posX, plant.posY);
                lstGrPos = plant.lastDrawPos;
                grLvl = plant.growthLevel;
                lastGr = plant.timeAtLastGrowth;
                fillStates = dictToArray(plant.fillStates);
                branches = new List<BranchJson>();
                foreach (Branch childBranch in plant.childBranches)
                {
                    branches.Add(new BranchJson(childBranch));
                }
                flowers = new List<FlowerJson>();
                foreach (Flower flower in plant.childFlowers)
                {
                    flowers.Add(new FlowerJson(flower));
                }
            }
            public PlantJson()
            {

            }
        }
        public class BranchJson
        {
            public int seed;
            public (int, int) pos;
            public (int, int) lstGrPos;
            public int type;
            public int grLvl;

            public int[,] fillStates;

            public List<BranchJson> branches;
            public List<FlowerJson> flowers;
            public BranchJson(Branch branch)
            {
                seed = branch.seed;
                type = branch.type;
                pos = branch.pos;
                lstGrPos = branch.lastDrawPos;
                grLvl = branch.growthLevel;
                fillStates = dictToArray(branch.fillStates);
                branches = new List<BranchJson>();
                foreach (Branch childBranch in branch.childBranches)
                {
                    branches.Add(new BranchJson(childBranch));
                }
                flowers = new List<FlowerJson>();
                foreach (Flower flower in branch.childFlowers)
                {
                    flowers.Add(new FlowerJson(flower));
                }
            }
            public BranchJson()
            {

            }
        }
        public class FlowerJson
        {
            public int seed;
            public (int, int) pos;
            public (int, int) lstGrPos;
            public int type;
            public int grLvl;

            public int[,] fillStates;
            public FlowerJson(Flower flower)
            {
                seed = flower.seed;
                type = flower.type;
                pos = flower.pos;
                lstGrPos = flower.lastDrawPos;
                grLvl = flower.growthLevel;
                fillStates = dictToArray(flower.fillStates);
            }
            public FlowerJson()
            {

            }
        }
        public static int[,] dictToArray(Dictionary<(int x, int y), int> dicto)
        {
            int[,] arrayo = new int[dicto.Count, 3];
            (int x, int y)[] keyo = dicto.Keys.ToArray();
            for (int i = 0; i < keyo.Length; i++)
            {
                arrayo[i, 0] = keyo[i].x;
                arrayo[i, 1] = keyo[i].y;
                arrayo[i, 2] = dicto[keyo[i]];
            }
            return arrayo;
        }
        public static Dictionary<(int x, int y), int> arrayToDict(int[,] arrayo)
        {
            Dictionary<(int x, int y), int> dicto = new Dictionary<(int x, int y), int>();
            for (int i = 0; i < arrayo.GetLength(0); i++)
            {
                dicto[(arrayo[i, 0], arrayo[i, 1])] = arrayo[i, 2];
            }
            return dicto;
        }
        public static void saveChunk(Chunk chunk, bool creaturesSpawned)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\ChunkData\\{chunk.screen.seed}\\{chunk.position.Item1}.{chunk.position.Item2}.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                ChunkJson baby = new ChunkJson(chunk);
                serializer.Serialize(writer, baby);
            }
        }
        public static void loadChunk(Chunk chunk, bool loadEntitiesAndPlants)
        {
            bool willSpawnEntities;
            using (StreamReader f = new StreamReader($"{currentDirectory}\\ChunkData\\{chunk.screen.seed}\\{chunk.position.Item1}.{chunk.position.Item2}.json"))
            {
                string content = f.ReadToEnd();
                ChunkJson chunkJson = JsonConvert.DeserializeObject<ChunkJson>(content);

                chunk.chunkSeed = chunkJson.chunkSeed;
                chunk.position = chunkJson.position;
                chunk.fillStates = chunkJson.fillStates;
                chunk.entitiesAndPlantsSpawned = chunkJson.enPlSpawned;

                if(loadEntitiesAndPlants)
                {
                    chunk.entityList = new List<Entity>();
                    foreach (EntityJson entityJson in chunkJson.entityList)
                    {
                        chunk.entityList.Add(new Entity(chunk, entityJson));
                    }
                    chunk.plantList = new List<Plant>();
                    foreach (PlantJson plantJson in chunkJson.plantList)
                    {
                        chunk.plantList.Add(new Plant(chunk, plantJson));
                    }
                }
            }
        }
        public static void saveEntity(Entity entity)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            ChunkJson chunkJson;
            (int, int) position = (Floor(entity.posX, 16) / 16, Floor(entity.posY, 16) / 16);

            if (System.IO.File.Exists($"{currentDirectory}\\ChunkData\\{entity.screen.seed}\\{position.Item1}.{position.Item2}.json"))
            {
                using (StreamReader f = new StreamReader($"{currentDirectory}\\ChunkData\\{entity.screen.seed}\\{position.Item1}.{position.Item2}.json"))
                {
                    string content = f.ReadToEnd();
                    chunkJson = JsonConvert.DeserializeObject<ChunkJson>(content);
                    chunkJson.entityList.Add(new EntityJson(entity));
                }
                using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\ChunkData\\{entity.screen.seed}\\{position.Item1}.{position.Item2}.json"))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, chunkJson);
                }
            }
            else
            {
                chunkJson = new ChunkJson(new Chunk(position, true, entity.screen));
                using (StreamWriter sw = new StreamWriter($"{currentDirectory}\\ChunkData\\{entity.screen.seed}\\{position.Item1}.{position.Item2}.json"))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, chunkJson);
                }
            }
        }
    }
}
