using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LoLCumulativeStats
{
    class RiotData
    {
        public static RiotData Instance;

        public RiotData(string name, string tag, string region)
        {
            Instance = this;
            this.name = name;
            this.tag = tag;
            this.region = region;
        }

        public string name { get; }
        public string tag { get; }
        public string region { get; }
        public string puuid { get; set; }
    }

    class Program
    {
        List<JObject> playerResults = new List<JObject>();
        public static StreamWriter[] writer = new StreamWriter[5];
        public static string rgapiKey;

        static void Main(string[] args)
        {
            Program program = new Program();
            program.Initialize().Wait();
        }

        public void ChangeAPIKey()
        {
            Console.WriteLine("Insert your API key here...");
            Console.WriteLine("(To get an API key from Riot, go to https://developer.riotgames.com and apply for one there)");

            string input = Console.ReadLine();

            using StreamWriter sw = new StreamWriter("API-Key.txt");
            sw.WriteLine(input);
            sw.Close();

            Console.WriteLine("API Key successfully changed.");
        }

        public async Task Initialize()
        {
            if (!File.Exists("API-Key.txt"))
            {
                Console.WriteLine("No API key detected, creating...");

                File.Create("API-Key.txt").Close();

                ChangeAPIKey();
            }

            using (StreamReader sr = new StreamReader("API-Key.txt"))
            {
                rgapiKey = sr.ReadLine();
                sr.Close();
            }

            if (!File.Exists("PlayerInfo.txt"))
            {
                Console.WriteLine("Player info not detected, creating...");

                File.Create("PlayerInfo.txt").Close();
                ChangePlayerInfo();
            }

            TrySetData();

            if (!File.Exists("Kills.txt"))
            {
                Console.WriteLine("Kills file not detected, creating...");
                File.Create("Kills.txt").Close();
            }
            if (!File.Exists("Deaths.txt"))
            {
                Console.WriteLine("Deaths file not detected, creating...");
                File.Create("Deaths.txt").Close();
            }
            if (!File.Exists("Assists.txt"))
            {
                Console.WriteLine("Assists file not detected, creating...");
                File.Create("Assists.txt").Close();
            }
            if (!File.Exists("TowerDmg.txt"))
            {
                Console.WriteLine("TowerDmg file not detected, creating...");
                File.Create("TowerDmg.txt").Close();
            }
            if (!File.Exists("KDA.txt"))
            {
                Console.WriteLine("KDA file not detected, creating...");
                File.Create("KDA.txt").Close();
            }

            Console.WriteLine("Connecting to Riot API...\n");
            UpdateTotal();

            await UpdateTimer();

            while (true)
            {
                Run();
            }
        }

        public void Run()
        {
            DisplayMenu();

            while (true)
            {
                string input = Console.ReadLine();

                int buffer;
                if (int.TryParse(input, out buffer))
                {
                    switch (buffer)
                    {
                        case 1:
                            ChangePlayerInfo();
                            Console.WriteLine("Player data has been set successfully. The app will now close to facilitate a restart.");
                            Environment.Exit(1);
                            break;

                        case 2:
                            Environment.Exit(1);
                            break;

                        default:
                            Console.WriteLine("Unacceptable answer, please reinput..");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Answer submitted is non-integer, please reinput..");
                }

            }
        }

        public void ChangePlayerInfo()
        {
            using (StreamWriter sw = new StreamWriter("PlayerInfo.txt"))
            {
                Console.WriteLine("Please insert your full player name, including your tag (e.g. Cxlys#EUW1)");
                sw.WriteLine(Console.ReadLine());

                Console.WriteLine("What region are you from?\n1. EUW\n2. NA\n3. ASIA\n4. SEA\nRespond using the number next to your region.");

                bool buffer = false;
                while (!buffer)
                {
                    string input = Console.ReadLine();

                    switch (int.Parse(input))
                    {
                        case 1:
                            buffer = true;
                            sw.WriteLine("europe");
                            break;

                        case 2:
                            buffer = true;
                            sw.WriteLine("americas");
                            break;

                        case 3:
                            buffer = true;
                            sw.WriteLine("asia");
                            break;

                        case 4:
                            buffer = true;
                            sw.WriteLine("sea");
                            break;

                        default:
                            Console.WriteLine("Answer not accepted, please retry: \n");
                            break;
                    }
                }
                sw.Close();
            }
        }

        public void DisplayMenu()
        {
            Console.WriteLine("\n1. Change targeted player");
            Console.WriteLine("2. Exit");
        }

        public async Task UpdateTimer(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                await Task.Delay(30 * 60000, cancellationToken);
                UpdateTotal();
            }
        }

        public void TrySetData()
        {
            using (StreamReader sr = new StreamReader("PlayerInfo.txt"))
            {
                var foo = sr.ReadLine();
                string[] nameAndTag = foo.Split('#');

                Console.WriteLine(nameAndTag[1]);
                var name = nameAndTag[0];
                var tag = nameAndTag[1];
                var region = sr.ReadLine();

                if (name is null || region is null)
                {
                    Console.WriteLine("Fatal error detected, exiting...");
                    File.Delete("PlayerInfo.txt");
                    Environment.Exit(1);
                }
                else
                {
                    new RiotData(name, tag, region);
                }
            }
        }

        public void UpdateTotal()
        {
            InitializeWriters();

            HttpClient riotClient = new HttpClient();
            playerResults.Clear();

            bool foo = false;
            HttpResponseMessage puuRes = default;
            while (!foo)
            {
                riotClient.DefaultRequestHeaders.Clear();
                riotClient.DefaultRequestHeaders.Add("X-Riot-Token", rgapiKey);

                Console.WriteLine("https://" + RiotData.Instance.region + ".api.riotgames.com/riot/account/v1/accounts/by-riot-id/" + RiotData.Instance.name + "/" + RiotData.Instance.tag);
                puuRes = riotClient.GetAsync("https://" + RiotData.Instance.region + ".api.riotgames.com/riot/account/v1/accounts/by-riot-id/" + RiotData.Instance.name + "/" + RiotData.Instance.tag).Result;

                if (!puuRes.IsSuccessStatusCode)
                {
                    Console.WriteLine("\nError with getting puuid, likely issue with API key. Preparing to change API key...");
                    ChangeAPIKey();
                }
                else
                {
                    foo = true;
                }
            }

            JObject puuInfo = JObject.Parse(puuRes.Content.ReadAsStringAsync().Result);
            RiotData.Instance.puuid = puuInfo["puuid"].ToString();

            DateTimeOffset today = new DateTime(year: DateTime.Now.Year, month: DateTime.Now.Month, day: DateTime.Now.Day, hour: 0, minute: 0, second: 0);
            long epochStart = today.ToUnixTimeSeconds();
            long epochEnd = epochStart + 86400;

            HttpResponseMessage res = riotClient.GetAsync(@"https://" + RiotData.Instance.region + ".api.riotgames.com/lol/match/v5/matches/by-puuid/" + RiotData.Instance.puuid + @"/ids?startTime=" + epochStart + "&endTime=" + epochEnd + @"&type=ranked&start=0&count=100").Result;

            JArray array = JArray.Parse(res.Content.ReadAsStringAsync().Result);
            foreach (string matchId in array)
            {
                HttpResponseMessage matchRes = riotClient.GetAsync(@"https://" + RiotData.Instance.region + ".api.riotgames.com/lol/match/v5/matches/" + matchId).Result;

                JObject matchInfo = JObject.Parse(matchRes.Content.ReadAsStringAsync().Result);

                foreach (JObject par in matchInfo["info"]["participants"])
                {
                    if (par["puuid"].ToString() == RiotData.Instance.puuid)
                    {
                        playerResults.Add(par);
                    }
                }
            }

            string kills = GetTotalKills().ToString();
            string deaths = GetTotalDeaths().ToString();
            string assists = GetTotalAssists().ToString();

            Console.WriteLine("\nTotal kills: " + GetTotalKills());
            Console.WriteLine("Total deaths: " + GetTotalDeaths());
            Console.WriteLine("Total assists: " + GetTotalAssists());
            Console.WriteLine("Total turret damage: " + GetTotalTurretDamage());
            Console.WriteLine("KDA: " + kills + "/" + deaths + "/" + assists);
            Console.WriteLine("All information above has been written to local .txt files.");

            writer[0].Write(kills);
            writer[1].Write(deaths);
            writer[2].Write(assists);
            writer[3].Write(GetTotalTurretDamage().ToString());
            writer[4].Write(kills + "/" + deaths + "/" + assists);

            KillWriters();
        }

        public void InitializeWriters()
        {
            writer[0] = new StreamWriter("Kills.txt");
            writer[1] = new StreamWriter("Deaths.txt");
            writer[2] = new StreamWriter("Assists.txt");
            writer[3] = new StreamWriter("TowerDmg.txt");
            writer[4] = new StreamWriter("KDA.txt");
        }

        public void KillWriters()
        {
            writer[0].Close();
            writer[1].Close();
            writer[2].Close();
            writer[3].Close();
            writer[4].Close();
        }

        public int GetTotalKills()
        {
            var total = 0;
            foreach (JObject obj in playerResults)
            {
                total += int.Parse(obj["kills"].ToString());
            }

            return total;
        }

        public int GetTotalDeaths()
        {
            var total = 0;
            foreach (JObject obj in playerResults)
            {
                total += int.Parse(obj["deaths"].ToString());
            }

            return total;
        }

        public int GetTotalAssists()
        {
            var total = 0;
            foreach (JObject obj in playerResults)
            {
                total += int.Parse(obj["assists"].ToString());
            }

            return total;
        }

        public int GetTotalTurretDamage()
        {
            var total = 0;
            foreach (JObject obj in playerResults)
            {
                total += int.Parse(obj["damageDealtToTurrets"].ToString());
            }

            return total;
        }
    }


}
