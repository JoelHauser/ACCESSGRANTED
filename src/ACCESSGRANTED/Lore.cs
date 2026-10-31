using System.Collections.Generic;

namespace ACCESSGRANTED
{
    /// <summary>
    /// What TerraGroup knows about the region: where the bosses were last seen, which PMC
    /// squads are moving where, what the traders are buying, what is selling on the flea. Fills
    /// the chatter, the bulletin board, the ticker and the priority intel notices, so the
    /// terminal reads like the company watching Tarkov while you wait. Plus the odd easter egg.
    /// </summary>
    internal static class Lore
    {
        private struct Spot
        {
            internal string Map;
            internal string[] Places;
        }

        private static readonly Spot[] Maps =
        {
            new Spot { Map = "CUSTOMS", Places = new[] { "DORMS", "BIG RED", "THE FORTRESS", "NEW GAS", "CONSTRUCTION" } },
            new Spot { Map = "FACTORY", Places = new[] { "OFFICE BLOCK", "GATE 3", "PUMPING STATION", "THE TUNNELS" } },
            new Spot { Map = "WOODS", Places = new[] { "SAWMILL", "USEC CAMP", "SCAV BUNKER", "THE CRASH SITE" } },
            new Spot { Map = "SHORELINE", Places = new[] { "THE RESORT", "THE PIER", "POWER STATION", "WEATHER STATION" } },
            new Spot { Map = "INTERCHANGE", Places = new[] { "ULTRA MALL", "IDEA", "GOSHAN", "KIBA ARMS" } },
            new Spot { Map = "RESERVE", Places = new[] { "THE BUNKERS", "BLACK PAWN", "WHITE KING", "THE DRAINS" } },
            new Spot { Map = "LIGHTHOUSE", Places = new[] { "WATER TREATMENT", "THE CHALETS", "ROGUE CAMP", "THE ISLAND" } },
            new Spot { Map = "STREETS", Places = new[] { "LEXOS", "CONCORDIA", "PINEWOOD HOTEL", "CHEKANNAYA 15" } },
            new Spot { Map = "THE LAB", Places = new[] { "THE SERVER ROOM", "THE MANAGER'S OFFICE", "THE ARENA", "BLUE LAB" } },
            new Spot { Map = "GROUND ZERO", Places = new[] { "TERRAGROUP HQ", "EMERCOM", "THE UNDERPASS", "NAKATANI" } }
        };

        private static readonly string[][] Bosses =
        {
            new[] { "RESHALA", "CUSTOMS", "DORMS" },
            new[] { "KILLA", "INTERCHANGE", "ULTRA MALL" },
            new[] { "SHTURMAN", "WOODS", "SAWMILL" },
            new[] { "GLUKHAR", "RESERVE", "THE BARRACKS" },
            new[] { "SANITAR", "SHORELINE", "THE RESORT" },
            new[] { "TAGILLA", "FACTORY", "THE FORGE" },
            new[] { "KABAN", "STREETS", "THE CAR DEALERSHIP" },
            new[] { "KOLLONTAY", "STREETS", "THE POLICE ACADEMY" },
            new[] { "ZRYACHIY", "LIGHTHOUSE", "THE ISLAND" },
            new[] { "THE GOONS", "LIGHTHOUSE", "THE CHALETS" },
            new[] { "PARTISAN", "WOODS", "THE TREELINE" },
            new[] { "CULTISTS", "CUSTOMS", "THE GAS STATION" }
        };

        private static readonly string[] Traders = { "PRAPOR", "THERAPIST", "FENCE", "SKIER", "PEACEKEEPER", "MECHANIC", "RAGMAN", "JAEGER", "REF" };

        private static readonly string[] Items =
        {
            "LEDX", "GRAPHICS CARDS", "PHYSICAL BITCOIN", "LABS KEYCARDS", "MOONSHINE", "INTELLIGENCE FOLDERS",
            "TETRIZ CONSOLES", "THERMAL SCOPES", "GOLDEN ROOSTERS", "RED KEYCARDS", "SALEWA KITS", "GAS ANALYZERS"
        };

        private static readonly string[] Intel =
        {
            "{boss} SIGHTED  //  {bossmap}  //  {bossplace}",
            "{boss} CONFIRMED ACTIVE  //  {bossmap}  //  AVOID {bossplace}",
            "BEAR SQUAD ({n4}) MOVING THROUGH {place}, {map}",
            "USEC PATROL ENGAGED SCAVS NEAR {place}, {map}",
            "SCAV ACTIVITY UP {pct}% AT {place}, {map}",
            "{trader} RECEIVED A SHIPMENT OF {item}",
            "{trader} BUYING {item} ABOVE MARKET",
            "FENCE OFFERING {item} AT A {pct}% MARKUP",
            "FLEA MARKET: {item} UP {pct}% SINCE MORNING",
            "FLEA MARKET: {item} DOWN {pct}% ON NEWS FROM {map}",
            "AIRDROP INBOUND  //  {map}",
            "BTR ROUTE CHANGED  //  {map}",
            "EXFIL AT {place} REPORTED COMPROMISED",
            "PMC CASUALTIES TODAY: {n3}. SCAV CASUALTIES: UNCOUNTED",
            "{faction} OPERATOR {nick} LOGGED ENTERING THE REGION",
            "SIGNAL INTERCEPTED FROM {place}, {map}. DECRYPTING",
            "LIGHTKEEPER: NO CONTACT FOR {n2} DAYS",
            "DRONE FOOTAGE OF {place} UNDER REVIEW",
            "UNIDENTIFIED SNIPER AT {place}, {map}",
            "RAIDERS DEPLOYED TO {place}, {map}",
            "{boss} NOT SEEN AT {bossplace} FOR {n2} HOURS. LOCATION UNKNOWN",
            "{boss} GUARDS COUNTED: {n4}. WELL EQUIPPED",
            "HEAVY GUNFIRE REPORTED AT {place}, {map}. NO TERRAGROUP STAFF IN AREA",
            "SMOKE OBSERVED OVER {place}, {map}",
            "{faction} SQUAD EXTRACTED VIA {place}, {map} WITH HEAVY LOAD",
            "ROGUE PATROL EXPANDED PAST {place}, LIGHTHOUSE",
            "CULTIST ACTIVITY AFTER DARK AT {place}, {map}",
            "MINEFIELD MARKERS MOVED NEAR {place}, {map}",
            "{trader} HAS STOPPED TAKING ORDERS FOR {item}",
            "{trader} IS ASKING ABOUT A MISSING SHIPMENT. AVOID",
            "{trader} PRICES UP {pct}% OVERNIGHT",
            "FENCE BOUGHT {n3} ITEMS FROM SCAVS TODAY. NO QUESTIONS ASKED",
            "RAGMAN RESTOCKED BODY ARMOUR. QUEUE FORMING",
            "MECHANIC SEEN AT THE FACTORY. MOOD: POOR",
            "PEACEKEEPER RAISED HIS RATES. AGAIN",
            "JAEGER POSTED A NEW TASK NEAR {place}, {map}",
            "SKIER'S MEN ARE COLLECTING DEBTS IN {map}",
            "THERAPIST REQUESTING SURGICAL SUPPLIES. PAYING WELL",
            "REF HAS OPENED THE ARENA FOR ANOTHER ROUND",
            "FLEA MARKET LISTING OF {item} REMOVED AFTER {n2} MINUTES",
            "FLEA MARKET FEES UP {pct}%. TRADERS UNHAPPY",
            "PRICE OF {item} DOUBLED AFTER {boss} SIGHTING",
            "AIRDROP AT {place}, {map} CONTESTED BY {n4} GROUPS",
            "BTR SEEN IDLING AT {place}, STREETS",
            "EXFIL CAR AT {place}, {map} DEMANDING HIGHER FARE",
            "POWER RESTORED TO {place}, {map}. EXTRACTS MAY BE LIT",
            "CO-OP EXTRACT AT {place} ACTIVE. SCAVS COOPERATIVE, FOR NOW",
            "BEAR CASUALTIES IN {map}: {n3}. USEC CASUALTIES: {n3}",
            "USEC CONVOY SPOTTED LEAVING {place}, {map}",
            "BEAR CHECKPOINT SET UP NEAR {place}, {map}",
            "{n2} PMC OPERATORS CONFIRMED MISSING IN {map}",
            "SCAV BOSS CHANGE AT {bossplace}. NEW LEADER UNKNOWN",
            "SIGNAL JAMMING DETECTED OVER {map}",
            "LABS KEYCARD SOLD FOR {n3}K ROUBLES",
            "ARMOURED VEHICLE ABANDONED AT {place}, {map}",
            "FLARE SEEN OVER {place}, {map} AT DUSK",
            "THE GOONS LAST SEEN HEADING TOWARDS {map}",
            "PARTISAN TRAPS FOUND NEAR {place}, {map}",
            "SANITAR SEEN TRADING STIMULANTS NEAR {place}, SHORELINE",
            "WEATHER WORSENING OVER {map}. VISIBILITY POOR",
            "UNKNOWN AIRCRAFT OVER {map}. NOT OURS",
            "GROUND ZERO PERIMETER BREACHED AT {place}"
        };

        private static readonly string[] EasterEggs =
        {
            "A REQUEST FROM TAGILLA FOR A REPLACEMENT HAMMER HANDLE HAS BEEN DECLINED FOR THE FOURTH TIME. PLEASE DO NOT FORWARD IT AGAIN.",
            "STAFF ARE ASKED TO REFER TO THE BTR DRIVER BY HIS CALL SIGN ONLY, NOT AS 'UNCLE'.",
            "A PARACORD LISTING ON THE FLEA MARKET AT 1 ROUBLE IS NOT A COMPANY OFFER. DO NOT BUY IT.",
            "MECHANIC HAS CONFIRMED GUNSMITH ORDER 16 REMAINS OPEN. HE HAS DECLINED TO GIVE A COMPLETION DATE.",
            "THE COMPOSITION OF SANITAR'S STIMULANTS IS CLASSIFIED. STAFF SHOULD NOT REQUEST SAMPLES FROM HIM.",
            "THE LIGHTKEEPER IS NOT ACCEPTING VISITORS. THIS POSITION IS NOT EXPECTED TO CHANGE.",
            "PROCUREMENT IS REVIEWING A REQUEST FROM KILLA FOR A LARGER HELMET. NO TIMESCALE HAS BEEN AGREED.",
            "STAFF HEARING THE PHRASE 'CHEEKI BREEKI' NEARBY SHOULD TAKE COVER AND REPORT IT AFTERWARDS.",
            "THERE IS NO SCHEDULED WIPE. STAFF SHOULD NOT PLAN AROUND ONE.",
            "THE GOLDEN ROOSTER IN STORAGE B IS NOT FOR SALE TO STAFF OR TRADERS.",
            "PRAPOR'S AMMUNITION COUNT DOES NOT MATCH OUR RECORDS. FINANCE HAS STOPPED ASKING.",
            "THE MARKED ROOM IS MARKED FOR A REASON. ENTRY IS AT THE STAFF MEMBER'S OWN RISK.",
            "THE CAT IN THE HIDEOUT IS NOT A COMPANY ASSET. STAFF MAY CONTINUE TO FEED IT AT THEIR OWN EXPENSE.",
            "SIGHTINGS OF A FIGURE IN THE LAB ARENA HAVE BEEN ATTRIBUTED TO THE LIGHTING. THE LIGHTING IS BEING REVIEWED.",
            "INJURIES SUSTAINED AFTER DARK IN THE WOODS ARE NOT COVERED BY THE COMPANY INSURANCE POLICY.",
            "A ROUND HAS BEEN FOUND CHAMBERED IN EVERY RIFLE IN THE ARMOURY. WHOEVER IS DOING THIS SHOULD STOP.",
            "THE FLOOR 3 OFFICE SMELLS OF MOONSHINE AGAIN. THE CONTRACTOR JAEGER DENIES ALL INVOLVEMENT.",
            "THE SECURE CONTAINER ISSUED TO YOU IS NOT TO BE USED FOR PERSONAL ITEMS. WE ARE AWARE THAT IT IS.",
            "IF YOU ARE READING THIS, THE FILTER HAS FAILED AGAIN. PLEASE DELETE IT AND FORGET THE WORD 'TERMINUS'.",
            "SOMEONE HAS BEEN LEAVING TUSHONKA ON THE CEO'S DESK. IT IS NOT FUNNY. IT IS, A LITTLE.",
            "THE PERSON WHO KEEPS RENAMING THE SHARED DRIVE TO 'EXTRACT_HERE' HAS BEEN IDENTIFIED. HELLO.",
            "DO NOT ANSWER THE PHONE IN THE EMPTY OFFICE ON FLOOR 6. NOBODY WORKS ON FLOOR 6.",
            "THE STAFF CAT HAS BEEN SEEN IN THE BLUE LAB. NOBODY KNOWS HOW. NOBODY IS GOING IN TO GET IT.",
            "THE 'LOOT GOBLIN' AWARD IS NOT AN OFFICIAL COMPANY AWARD. PLEASE STOP ENGRAVING THE TROPHY.",
            "WHOEVER LEFT A BACKPACK FULL OF BOLTS IN THE LOBBY: MECHANIC WANTS THEM. MECHANIC ALWAYS WANTS THEM.",
            "THE LIGHTHOUSE KEEPER HAS SENT A CHRISTMAS CARD. IT ARRIVED IN JUNE. IT WAS UNSIGNED. IT KNEW OUR ADDRESS.",
            "SOMEONE IS DRAWING SMILEY FACES ON THE LAB'S OBSERVATION WINDOWS. FROM THE INSIDE.",
            "THE GRAPHICS CARD IN THE BOARDROOM PROJECTOR HAS BEEN REMOVED. THE PROJECTOR NOW SHOWS ONLY THE COMPANY LOGO.",
            "A STAFF MEMBER CLAIMS TO HAVE BEEN SHOT BY A SCAV THROUGH TWO WALLS. THE WALLS HAVE BEEN SENT FOR TESTING.",
            "THE MARKED KEY IS NOT A STATUS SYMBOL. PLEASE STOP WEARING IT ON YOUR LANYARD.",
            "THE CEO'S PASSWORD WAS 'KAPPA'. IT HAS BEEN CHANGED. IT IS NOW 'KAPPA2'.",
            "RUMOURS THAT THE HIDEOUT CAN BE UPGRADED WITH COMPANY FUNDS ARE FALSE. THE BITCOIN FARM WAS A ONE-OFF.",
            "IF FOUND, PLEASE RETURN TO: [THE REST OF THIS MESSAGE HAS BEEN REMOVED]",
            "WE KNOW ABOUT THE WIGGLE. EVERYONE KNOWS ABOUT THE WIGGLE.",
            "A NOTE FOUND IN A SECURE CONTAINER READS 'GRENADE FOR LATER'. SECURITY WOULD LIKE TO KNOW WHEN LATER IS.",
            "THE REQUEST FOR A STAFF DISCOUNT AT THE FLEA MARKET HAS BEEN FORWARDED TO NOBODY.",
            "THERE IS NO ARMOURED TRAIN. STOP ASKING ABOUT THE ARMOURED TRAIN.",
            "THE PORTRAIT OF THE FOUNDER IN THE LOBBY HAS BEEN TURNED TO FACE THE WALL. AGAIN. BY WHOM?",
            "PLEASE STOP CALLING THE CANTEEN 'THE HIDEOUT'. IT CONFUSES THE NEW STARTERS.",
            "THE PHRASE 'IT'S A PVE SERVER' HAS BEEN OVERHEARD IN THE BREAK ROOM. LEGAL WOULD LIKE A WORD.",
            "WHOEVER KEEPS HIDING IN THE BUSHES OUTSIDE GATE 2: WE CAN SEE YOU. SO CAN EVERYONE ELSE.",
            "THE OFFICE PLANT ON FLOOR 3 HAS BEEN WATERED WITH MOONSHINE FOR A MONTH. IT IS THRIVING. JAEGER IS PROUD.",
            "A REQUEST TO REPLACE THE COMPANY CAR WITH A BTR HAS BEEN DECLINED, NARROWLY.",
            "THE VOICES IN THE VENTILATION ON FLOOR 4 ARE THE HVAC. FACILITIES IS ALMOST SURE.",
            "WE ARE AWARE THAT THE CLOCK IN RECEPTION RUNS SEVEN TIMES FAST. IT IS SET TO TARKOV TIME.",
            "SOMEONE HAS BEEN SELLING THE CANTEEN'S SALT ON THE FLEA MARKET. THE PRICE IS, FRANKLY, IMPRESSIVE.",
            "THE COLLECTOR HAS WRITTEN TO ASK FOR THE OFFICE CAT. THE ANSWER IS NO. THE ANSWER WILL ALWAYS BE NO."
        };

        private static readonly string[] Senders =
        {
            "REGIONAL INTELLIGENCE", "SIGNALS DESK", "FIELD OBSERVATION", "OVERWATCH", "THREAT ASSESSMENT",
            "SITUATION ROOM", "INTERCEPT ANALYSIS", "MARKET WATCH", "PERIMETER OBSERVATION", "DRONE OPERATIONS"
        };

        /// <summary>A line of regional intel, filled in.</summary>
        internal static string IntelLine(System.Random random) => Fill(IntelTemplate(random), random);

        /// <summary>A real place in the region, for labelling a camera, a destination, a dateline.</summary>
        internal static void Location(System.Random random, out string map, out string place)
        {
            Spot spot = Maps[random.Next(Maps.Length)];
            map = spot.Map;
            place = spot.Places[random.Next(spot.Places.Length)];
        }

        /// <summary>An intel line still holding its placeholders, for filling in later; one the player has not seen lately.</summary>
        internal static string IntelTemplate(System.Random random) => Archive.PickLine("intel", Intel, random);

        /// <summary>A note nobody admits to sending; one the player has not seen lately.</summary>
        internal static string EasterEgg(System.Random random) => Archive.PickLine("note", EasterEggs, random);

        internal static string IntelSender(System.Random random) => Senders[random.Next(Senders.Length)];

        /// <summary>
        /// Fills the placeholders: {boss}{bossmap}{bossplace} are one consistent sighting,
        /// {map}{place} one consistent location; {trader} {item} {faction} {nick} {n2} {n3}
        /// {n4} {pct} speak for themselves.
        /// </summary>
        internal static string Fill(string template, System.Random random)
        {
            if (template.IndexOf('{') < 0)
            {
                return template;
            }
            string[] boss = Bosses[random.Next(Bosses.Length)];
            Spot spot = Maps[random.Next(Maps.Length)];
            string nick = StartupTracker.Nickname;
            string side = StartupTracker.Side;
            return template
                .Replace("{bossmap}", boss[1])
                .Replace("{bossplace}", boss[2])
                .Replace("{boss}", boss[0])
                .Replace("{map}", spot.Map)
                .Replace("{place}", spot.Places[random.Next(spot.Places.Length)])
                .Replace("{trader}", Traders[random.Next(Traders.Length)])
                .Replace("{item}", Items[random.Next(Items.Length)])
                .Replace("{faction}", string.IsNullOrEmpty(side) ? (random.Next(2) == 0 ? "USEC" : "BEAR") : side)
                .Replace("{nick}", string.IsNullOrEmpty(nick) ? "[REDACTED]" : nick.ToUpperInvariant())
                .Replace("{n2}", random.Next(2, 60).ToString())
                .Replace("{n3}", random.Next(40, 400).ToString())
                .Replace("{n4}", random.Next(3, 6).ToString())
                .Replace("{pct}", random.Next(8, 140).ToString());
        }

        /// <summary>A few intel headlines for the ticker.</summary>
        internal static IEnumerable<string> Headlines(System.Random random, int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return IntelLine(random);
            }
        }
    }
}
