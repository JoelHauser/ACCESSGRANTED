using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ACCESSGRANTED
{
    /// <summary>Which TerraGroup division the terminal belongs to. Random picks one per launch.</summary>
    public enum ThemeChoice
    {
        Random,
        Corporate,
        Research,
        Security,
        Finance,
        Records,
        Executive,
        InternalAffairs,
        Logistics,
        HumanResources,
        ItDepartment,
        Medical,
        Facilities,
        Legal,
        PublicRelations,
        Holiday
    }

    /// <summary>How often the terminal glitches.</summary>
    public enum GlitchLevel
    {
        Off,
        Subtle,
        Heavy
    }

    // The parts of the screen. Random is for the settings only: it means "let the division
    // choose", and a rolled launch never holds it.
    public enum BackdropKind { Random, Dots, Grid, Hex, Pinstripe, Ruled, Blueprint, Plain }
    public enum FrameKind { Random, Rings, Brackets, Molecule, Dial, Seal, Ornate, Minimal, Squares, Orbit }
    public enum IntroKind { Random, Fade, TitleCard, Wipe, Crt, Sheen, Doors }
    public enum BarKind { Random, Segments, Thin, Solid }
    public enum OutroKind { Random, Zoom, Lift, Sting, Slide, Flash }
    public enum LayoutKind { Random, Centered, Split, SplitMirror, Sidebar, Dock }

    /// <summary>Parts fixed by the settings (or the preview) instead of rolled.</summary>
    internal struct Pins
    {
        internal LayoutKind Layout;
        internal BackdropKind Backdrop;
        internal FrameKind Frame;
        internal IntroKind Intro;
        internal BarKind Bar;
        internal OutroKind Outro;

        internal static Pins FromConfig() => new Pins
        {
            Layout = AccessGrantedPlugin.Layout.Value,
            Backdrop = AccessGrantedPlugin.Backdrop.Value,
            Frame = AccessGrantedPlugin.Frame.Value,
            Intro = AccessGrantedPlugin.Intro.Value,
            Bar = AccessGrantedPlugin.Bar.Value,
            Outro = AccessGrantedPlugin.Ending.Value
        };
    }

    /// <summary>
    /// One division's look and voice: palette, the choices for each of the five parts of the
    /// screen (backdrop, emblem frame, intro, progress bar, ending), and the words it uses. A
    /// launch picks one of each from the division's lists.
    /// </summary>
    internal sealed class Theme
    {
        internal readonly ThemeChoice Id;
        internal readonly Color32 Ink;
        internal readonly Color32 Glow;
        internal readonly Color32 Primary;
        internal readonly Color32 Mid;
        internal readonly Color32 Accent;
        internal readonly Color32 Text;
        internal readonly Color32 Dim;
        internal readonly Color32 Faint;

        /// <summary>Slow steps and ACCESS DENIED.</summary>
        internal readonly Color32 Warn;

        /// <summary>The second colour of the RGB split, opposite the accent.</summary>
        internal readonly Color32 Split;

        internal readonly string AccentHex;
        internal readonly string WarnHex;

        internal BackdropKind[] Backdrops;
        internal FrameKind[] Frames;
        internal IntroKind[] Intros;
        internal BarKind[] Bars;
        internal OutroKind[] Outros;
        internal bool Scanlines;
        internal bool Watermark;
        internal bool Chart;

        internal string Division;
        internal string TitleCard;
        internal string LogTag = "OK";
        internal string SlowNote = "THANK YOU FOR YOUR PATIENCE";
        internal string[] Headers;
        internal string[] Grants;
        internal string[] Chatter;
        internal string[] Headlines;

        /// <summary>The division's own internal memos; may hold Lore placeholders like {nick} or {boss}.</summary>
        internal string[] Memos;

        private Theme(ThemeChoice id, string ink, string glow, string primary, string mid,
            string accent, string text, string dim, string warn, string split)
        {
            Id = id;
            Ink = Hex(ink);
            Glow = Hex(glow);
            Primary = Hex(primary);
            Mid = Hex(mid);
            Accent = Hex(accent);
            Text = Hex(text);
            Dim = Hex(dim);
            Faint = WithAlpha(Dim, 60);
            Warn = Hex(warn);
            Split = Hex(split);
            AccentHex = "#" + accent;
            WarnHex = "#" + warn;
        }

        internal static readonly Theme[] All =
        {
            new Theme(ThemeChoice.Corporate, "05070F", "1A2A6A", "3150C8", "2A3F9A", "8FB4FF", "E8EEFF", "7184B0", "FFAA33", "FF3B5C")
            {
                Backdrops = new[] { BackdropKind.Dots, BackdropKind.Blueprint, BackdropKind.Plain }, Frames = new[] { FrameKind.Rings, FrameKind.Minimal, FrameKind.Orbit },
                Intros = new[] { IntroKind.TitleCard, IntroKind.Doors, IntroKind.Fade }, Bars = new[] { BarKind.Segments, BarKind.Solid }, Outros = new[] { OutroKind.Zoom, OutroKind.Sting, OutroKind.Slide },
                Watermark = true,
                Division = "CORPORATE HEADQUARTERS", TitleCard = "VIRTUS IN SCIENTIA",
                Headers = new[] { "TERRAGROUP INTRANET  //  EMPLOYEE PORTAL", "TERRAGROUP  //  CORPORATE HEADQUARTERS", "TERRAGROUP  //  NORVINSK REGIONAL OFFICE" },
                Grants = new[] { "WELCOME TO TERRAGROUP", "SIGN-IN SUCCESSFUL" },
                Chatter = new[] { "MISSION STATEMENT  ACKNOWLEDGED", "ORG CHART  {n} POSITIONS VACANT", "TOWN HALL RECORDING  AVAILABLE", "BRAND GUIDELINES  REV {n}  APPLIED" },
                Headlines = new[] { "TERRAGROUP NAMED REGION'S MOST TRUSTED EMPLOYER", "CEO ADDRESSES STAFF: 'THE FUTURE IS BRIGHT'" },
                Memos = new[] { "PER POLICY TG-COM-01, STAFF MAY NOT DISCUSS THE EVENTS OF 2017 WITH ANYONE OUTSIDE THE COMPANY, OR INSIDE IT.", "THE SITUATION IN NORVINSK IS TO BE DESCRIBED AS A 'TEMPORARY OPERATIONAL DISRUPTION' IN ALL CORRESPONDENCE.", "THE Q4 TOWN HALL IS POSTPONED UNTIL THE CAR PARK IS SECURE. A RECORDING WILL BE CIRCULATED.", "VISITING UN PERSONNEL ARE TO BE MET AT RECEPTION AND ESCORTED AT ALL TIMES. DO NOT ANSWER QUESTIONS.", "EFFECTIVE IMMEDIATELY, ALL TRAVEL OUT OF THE REGION REQUIRES BOARD APPROVAL. NO REQUESTS ARE BEING APPROVED.", "{faction} CONTRACTORS ARE NOT EMPLOYEES OF TERRAGROUP AND ARE NOT COVERED BY COMPANY POLICY. OR PROTECTION." }
            },
            new Theme(ThemeChoice.Research, "0C0305", "6A0A14", "D9283A", "A01E2C", "FF8A8A", "FFE8E8", "B47078", "FFC83C", "3CDCFF")
            {
                Backdrops = new[] { BackdropKind.Hex, BackdropKind.Blueprint }, Frames = new[] { FrameKind.Molecule, FrameKind.Orbit },
                Intros = new[] { IntroKind.Wipe, IntroKind.Fade, IntroKind.Doors }, Bars = new[] { BarKind.Segments, BarKind.Thin }, Outros = new[] { OutroKind.Sting, OutroKind.Zoom },
                Division = "RESEARCH DIVISION", TitleCard = "TERRAGROUP LABS", LogTag = "CLEARED",
                SlowNote = "RESULTS PENDING",
                Headers = new[] { "TERRAGROUP LABS  //  RESEARCH DIVISION", "LABS SUBNET B-14  //  RESTRICTED", "TERRAGROUP LABS  //  CLINICAL TRIALS OFFICE" },
                Grants = new[] { "CLEARANCE CONFIRMED", "LAB ACCESS APPROVED" },
                Chatter = new[] { "SPECIMEN REGISTRY  {n} ENTRIES", "CLEAN ROOM 4  PRESSURE NOMINAL", "TRIAL PROTOCOL 6-HELIX  ON SCHEDULE", "ETHICS REVIEW  WAIVED", "COLD STORAGE  -80C  STABLE", "SAMPLE CHAIN OF CUSTODY  SIGNED", "INCIDENT REPORT #{n}  REDACTED PER LEGAL" },
                Headlines = new[] { "LABS EXPANSION AHEAD OF SCHEDULE", "TERRAGROUP TRIALS 'SHOWING PROMISE', SAYS TERRAGROUP" },
                Memos = new[] { "SAMPLE BATCH 6-HELIX-B HAS NOT BEEN ACCOUNTED FOR. ANY STAFF WITH INFORMATION SHOULD REPORT TO LAB SECURITY.", "LEDX DEVICES ARE AUDITED DAILY UNDER POLICY TG-LAB-12. DISCREPANCIES WILL BE DEDUCTED FROM SALARY.", "FORMER STAFF MEMBER A. SANITAR IS NOT AUTHORISED TO ENTER ANY TERRAGROUP FACILITY. DO NOT ACCEPT HIS SAMPLES.", "TRIAL PARTICIPANTS ARE TO BE REFERRED TO BY CASE NUMBER ONLY. PERSONAL ATTACHMENT IS DISCOURAGED.", "LEVEL 2 AIR FILTRATION IS OVERDUE FOR SERVICE. RESPIRATORS ARE AVAILABLE FROM STORES ON REQUEST.", "THE ARENA FACILITY IS A CONTROLLED TESTING ENVIRONMENT. ATTENDANCE AS A SPECTATOR IS NOT PERMITTED." }
            },
            new Theme(ThemeChoice.Security, "0A0500", "6E3400", "FF8C14", "C8640A", "FFBE5A", "FFE2B4", "B0783C", "FF4632", "3CC8FF")
            {
                Backdrops = new[] { BackdropKind.Grid, BackdropKind.Blueprint }, Frames = new[] { FrameKind.Brackets, FrameKind.Squares },
                Intros = new[] { IntroKind.Crt, IntroKind.Doors }, Bars = new[] { BarKind.Thin, BarKind.Segments }, Outros = new[] { OutroKind.Lift, OutroKind.Slide },
                Scanlines = true,
                Division = "SECURITY DEPARTMENT", TitleCard = "AUTHORIZED PERSONNEL ONLY", LogTag = "CLEAR",
                SlowNote = "PLEASE REMAIN AT THE CHECKPOINT",
                Headers = new[] { "TERRAGROUP SECURITY  //  CHECKPOINT 3", "TERRAGROUP SECURITY  //  BADGE CONTROL", "PHYSICAL SECURITY  //  CCTV OPERATIONS" },
                Grants = new[] { "BADGE ACCEPTED", "CHECKPOINT CLEARED" },
                Chatter = new[] { "BADGE READER  GATE {n}  ONLINE", "CCTV  {n} CAMERAS RECORDING", "VISITOR LOG  {n} ENTRIES", "PATROL ROUTE B  CONFIRMED", "PERIMETER FENCE  NO ALERTS", "KEYCARD REGISTRY  {n} ACTIVE", "ARMORY INVENTORY  RECONCILED" },
                Headlines = new[] { "SECURITY REMINDS STAFF: TAILGATING IS A FIREABLE OFFENSE", "NEW BADGE READERS INSTALLED AT ALL GATES" },
                Memos = new[] { "RAIDER UNITS OPERATE UNDER A SEPARATE CHAIN OF COMMAND. DO NOT ENGAGE, OBSTRUCT OR QUESTION THEM.", "GATE 3 LOGGED {nick} ENTERING TWICE AND LEAVING ONCE. PLEASE BADGE OUT AT EVERY EXIT.", "{boss} HAS BEEN REPORTED NEAR {bossplace}. ALL PATROL ROUTES THROUGH {bossmap} ARE SUSPENDED UNTIL FURTHER NOTICE.", "RED KEYCARD PRIVILEGES ARE UNDER REVIEW. HOLDERS SHOULD NOT ATTEMPT TO USE THEM UNTIL CONTACTED.", "STAFF FOUND SUPPLYING KEYCARDS TO THIRD PARTIES, INCLUDING FENCE, WILL BE REFERRED TO INTERNAL AFFAIRS.", "REMINDER: ALL AREAS OF THE LABORATORY ARE MONITORED. THIS INCLUDES THE AREAS STAFF BELIEVE ARE NOT." }
            },
            new Theme(ThemeChoice.Finance, "020805", "0A4A28", "2FB86A", "23905A", "8CF0B0", "DFFFEA", "6FA088", "FFB43C", "FF3CA0")
            {
                Backdrops = new[] { BackdropKind.Dots, BackdropKind.Grid }, Frames = new[] { FrameKind.Dial, FrameKind.Minimal },
                Intros = new[] { IntroKind.Wipe, IntroKind.Fade }, Bars = new[] { BarKind.Solid, BarKind.Thin }, Outros = new[] { OutroKind.Zoom, OutroKind.Slide },
                Chart = true,
                Division = "INVESTOR RELATIONS", TitleCard = "TGRP  //  NORVINSK EXCHANGE", LogTag = "POSTED",
                SlowNote = "MARKET VOLATILITY",
                Headers = new[] { "TERRAGROUP FINANCE  //  INVESTOR RELATIONS", "TERRAGROUP TREASURY  //  LEDGER ACCESS", "TERRAGROUP  //  QUARTERLY REPORTING" },
                Grants = new[] { "ACCOUNT VERIFIED", "TRANSACTION APPROVED" },
                Chatter = new[] { "LEDGER CLOSE  Q4  IN PROGRESS", "ESCROW  FLEA MARKET  SETTLED", "INSURANCE CLAIMS  {n} PENDING", "RUB/USD FEED  SYNCED", "AUDIT TRAIL  {n} TRANSACTIONS", "DIVIDEND SCHEDULE  POSTPONED", "OFFSHORE TRANSFER  APPROVED" },
                Headlines = new[] { "TGRP HITS ALL-TIME HIGH ON LABS NEWS", "ANALYSTS: 'BUY', 'STRONG BUY', 'DO NOT ASK'" },
                Memos = new[] { "THE SERVER ROOM 2 BITCOIN FARM IS A COMPANY ASSET. PERSONAL DEVICES MUST NOT BE CONNECTED TO IT.", "FLEA MARKET TRANSACTIONS ARE NOT REIMBURSABLE AND MUST NOT BE MADE ON COMPANY ACCOUNTS.", "EXPENSE CLAIMS LISTING 'AMMUNITION, PERSONAL' NOW REQUIRE A SIGNED INCIDENT REPORT.", "PAYMENTS TO THE CONTRACTOR KNOWN AS PEACEKEEPER ARE TO BE SETTLED IN US DOLLARS ONLY, AS PER HIS TERMS.", "THE ROUBLE IS STABLE. STAFF ARE ASKED NOT TO CONVERT SALARIES INTO PHYSICAL GOODS.", "Q4 FIGURES ARE FINAL. ANY LOSSES IN THE NORVINSK REGION ARE TO BE BOOKED AS 'WEATHER'." }
            },
            new Theme(ThemeChoice.Records, "060607", "3C3C40", "AAAAAF", "78787D", "E1E1E6", "F0F0F0", "828287", "D23C3C", "3CA0FF")
            {
                Backdrops = new[] { BackdropKind.Ruled, BackdropKind.Plain }, Frames = new[] { FrameKind.Seal, FrameKind.Minimal },
                Intros = new[] { IntroKind.Fade, IntroKind.TitleCard }, Bars = new[] { BarKind.Thin }, Outros = new[] { OutroKind.Sting, OutroKind.Lift },
                Division = "RECORDS MANAGEMENT", TitleCard = "CONFIDENTIAL", LogTag = "FILED",
                SlowNote = "RETRIEVING FROM OFF-SITE STORAGE",
                Headers = new[] { "TERRAGROUP RECORDS  //  ARCHIVE ACCESS", "RECORDS MANAGEMENT  //  BASEMENT LEVEL 2", "TERRAGROUP  //  DOCUMENT CONTROL" },
                Grants = new[] { "FILE OPENED", "RECORD RETRIEVED" },
                Chatter = new[] { "ARCHIVE BOX #{n}  RETRIEVED", "MICROFILM INDEX  REBUILT", "DOCUMENT 0x{hex}  CLASSIFIED", "RETENTION POLICY  APPLIED", "SHREDDING QUEUE  {n} PAGES", "CROSS-REFERENCING PERSONNEL FILES" },
                Headlines = new[] { "RECORDS: ALL 2017 FILES 'LOST IN A FLOOD'", "DOCUMENT RETENTION POLICY SHORTENED TO 0 DAYS" },
                Memos = new[] { "ALL DOCUMENTS REFERRING TO THE 2017 INCIDENT ARE TO BE RECLASSIFIED UNDER RETENTION CODE R-0. DO NOT READ THEM.", "INTELLIGENCE FOLDERS ARE COMPANY RECORDS. THEIR SALE TO TRADERS IS A BREACH OF POLICY TG-REC-04.", "THE ARCHIVE HAS BEEN RELOCATED. STAFF WITH A NEED TO KNOW THE LOCATION WILL BE INFORMED.", "THE PERSONNEL FILE FOR {nick} IS INCOMPLETE. PLEASE SUBMIT AN IDENTIFICATION PHOTO WITHOUT FACE COVERING.", "BOX 2017-C IS MISSING FROM BASEMENT LEVEL 2. IF LOCATED, SEAL IT AND CONTACT RECORDS. DO NOT OPEN IT.", "SHREDDING REQUESTS NOW REQUIRE TWO SIGNATURES. SHREDDING WITHOUT A REQUEST REQUIRES NONE." }
            },
            new Theme(ThemeChoice.Executive, "080602", "6E500A", "DCAA28", "AA7D19", "FFD76E", "FFF0C8", "AA915A", "FF5A3C", "5A8CFF")
            {
                Backdrops = new[] { BackdropKind.Pinstripe }, Frames = new[] { FrameKind.Ornate, FrameKind.Squares },
                Intros = new[] { IntroKind.Sheen, IntroKind.Doors }, Bars = new[] { BarKind.Solid }, Outros = new[] { OutroKind.Flash, OutroKind.Sting },
                Division = "EXECUTIVE BOARD", TitleCard = "EXECUTIVE SUITE", LogTag = "SIGNED",
                SlowNote = "YOUR DRIVER IS ON THE WAY",
                Headers = new[] { "TERRAGROUP  //  EXECUTIVE BOARD", "TERRAGROUP  //  OFFICE OF THE CHAIRMAN" },
                Grants = new[] { "WELCOME TO THE BOARD", "EXECUTIVE ACCESS GRANTED" },
                Chatter = new[] { "BOARD MINUTES  SEALED", "GOLDEN PARACHUTE  FUNDED", "PRIVATE JET  FUELED", "SHAREHOLDER LETTER  APPROVED", "KAPPA CONTAINER  REGISTERED", "COLLECTOR ITEMS  {n} CATALOGUED" },
                Headlines = new[] { "BOARD APPROVES ITS OWN BONUSES", "CHAIRMAN'S YACHT 'NOT A COMPANY ASSET'" },
                Memos = new[] { "THE BOARD WILL CONVENE AT THE SHORELINE RESORT ON THURSDAY. SECURITY HAS DECLARED THE RESORT SAFE.", "EXECUTIVE EVACUATION ROUTES HAVE BEEN UPDATED. STAFF EVACUATION ROUTES ARE UNDER REVIEW.", "KAPPA-CLASS SECURE CONTAINERS ARE ISSUED BY INVITATION. THE COLLECTOR HAS ASKED TO BE REMEMBERED TO THE BOARD.", "THE BOARD HAS RESOLVED THAT TERRAGROUP BEARS NO RESPONSIBILITY FOR EVENTS IN NORVINSK. MOTION CARRIED UNANIMOUSLY.", "THE ANNUAL BONUS POOL HAS BEEN ALLOCATED IN FULL TO RETENTION OF THE BOARD.", "THE CHAIRMAN'S OFFICE IS CLOSED TO ALL STAFF. THIS INCLUDES THE CHAIRMAN'S STAFF." }
            },
            new Theme(ThemeChoice.InternalAffairs, "06020C", "461478", "9646FF", "6E32C8", "E678FF", "F0DCFF", "9678BE", "FFBE3C", "3CFFC8")
            {
                Backdrops = new[] { BackdropKind.Grid, BackdropKind.Plain }, Frames = new[] { FrameKind.Brackets, FrameKind.Orbit },
                Intros = new[] { IntroKind.TitleCard }, Bars = new[] { BarKind.Segments }, Outros = new[] { OutroKind.Sting, OutroKind.Zoom },
                Division = "INTERNAL AFFAIRS", TitleCard = "THIS SESSION IS BEING REVIEWED", LogTag = "NOTED",
                SlowNote = "PLEASE WAIT. YOU ARE NOT IN TROUBLE",
                Headers = new[] { "TERRAGROUP  //  INTERNAL AFFAIRS", "OFFICE OF COMPLIANCE  //  REVIEW BOARD" },
                Grants = new[] { "ACCESS GRANTED... [MONITORED]", "SESSION LOGGED" },
                Chatter = new[] { "SESSION FLAGGED FOR REVIEW", "KEYSTROKES  LOGGED", "INTERVIEW ROOM 2  RESERVED", "PERSONNEL FILE {emp}  OPENED", "ANOMALY SCORE  {pct}", "WITNESS STATEMENT #{n}  FILED" },
                Headlines = new[] { "INTERNAL AFFAIRS: 'WE ARE ALWAYS LISTENING'", "WHISTLEBLOWER HOTLINE TEMPORARILY DISCONNECTED" },
                Memos = new[] { "{nick} WAS OBSERVED TRADING WITH THE INDIVIDUAL KNOWN AS FENCE. THIS HAS BEEN ADDED TO YOUR FILE.", "ALL {faction} PERSONNEL ARE REMINDED THAT FRIENDLY FIRE INCIDENTS ARE RECORDED AND REVIEWED.", "LEDX DEVICES CONTINUE TO GO MISSING FROM THE MEDICAL WING. WE KNOW WHO IS RESPONSIBLE. WE ARE GIVING YOU A CHANCE.", "AN ANONYMOUS REPORT PLACES THE MARKED ROOM KEY IN YOUR LOCKER. PLEASE REMAIN AT YOUR DESK.", "THIS DEPARTMENT'S HOTLINE IS CONFIDENTIAL. YOUR NAME IS ONLY RECORDED FOR QUALITY PURPOSES.", "STAFF UNDER REVIEW ARE ENTITLED TO REPRESENTATION. NONE IS CURRENTLY AVAILABLE." }
            },
            new Theme(ThemeChoice.Logistics, "020A0C", "0A4650", "1FA5B5", "17808C", "7FE3EE", "DDFAFF", "64A0A8", "FFB43C", "FF4A7A")
            {
                Backdrops = new[] { BackdropKind.Grid, BackdropKind.Blueprint }, Frames = new[] { FrameKind.Dial, FrameKind.Orbit },
                Intros = new[] { IntroKind.Wipe, IntroKind.Doors }, Bars = new[] { BarKind.Segments, BarKind.Thin }, Outros = new[] { OutroKind.Slide, OutroKind.Lift },
                Division = "LOGISTICS AND SUPPLY", TitleCard = "MOVING WHAT MATTERS", LogTag = "SHIPPED",
                SlowNote = "DELAYED AT CUSTOMS",
                Headers = new[] { "TERRAGROUP LOGISTICS  //  DISPATCH", "TERRAGROUP LOGISTICS  //  PORT OPERATIONS", "SUPPLY CHAIN  //  NORVINSK DEPOT" },
                Grants = new[] { "DELIVERY CONFIRMED", "MANIFEST CLEARED" },
                Chatter = new[] { "CONTAINER TG-{n}  SEALED", "MANIFEST #{n}  RECONCILED", "TRUCK CONVOY  DEPARTED", "WAREHOUSE B  {pct}% CAPACITY", "CUSTOMS FORMS  {n} PENDING", "FORKLIFT CERT  EXPIRED", "PALLETS  {n} UNACCOUNTED FOR" },
                Headlines = new[] { "PORT TERMINAL BACKLOG 'SHRINKING'", "LOGISTICS: ALL SHIPMENTS ARRIVED, MOSTLY" },
                Memos = new[] { "THE AMMUNITION SHIPMENT FOR THE TRADER PRAPOR IS DELAYED AT CUSTOMS. HE HAS BEEN INFORMED AND IS UNHAPPY.", "A PEACEKEEPER CONVOY WILL PASS THROUGH CUSTOMS AT 12:00. THE ROAD IS TO BE KEPT CLEAR.", "THE BTR OPERATING IN THE CITY IS NOT A COMPANY VEHICLE. MILEAGE CLAIMS FOR IT WILL BE REJECTED.", "SEALED CONTAINERS AT THE PORT HAVE BEEN MOVED WITHOUT A WORK ORDER. DO NOT OPEN THEM. DO NOT FOLLOW THEM.", "AIRDROP SCHEDULES ARE CLASSIFIED. STAFF ARE ASKED NOT TO STAND IN OPEN GROUND AT PREDICTABLE TIMES.", "CARGO BELONGING TO THE CONTRACTOR JAEGER WAS DELIVERED TO THE WRONG DEPOT. RETURN IT BEFORE HE COLLECTS IT." }
            },
            new Theme(ThemeChoice.HumanResources, "0C0508", "5A2236", "E0607E", "B04A62", "FFB2C0", "FFEFF2", "B88894", "FFC850", "50D2FF")
            {
                Backdrops = new[] { BackdropKind.Dots, BackdropKind.Plain }, Frames = new[] { FrameKind.Minimal, FrameKind.Seal },
                Intros = new[] { IntroKind.Fade, IntroKind.TitleCard }, Bars = new[] { BarKind.Solid }, Outros = new[] { OutroKind.Sting, OutroKind.Zoom },
                Division = "HUMAN RESOURCES", TitleCard = "OUR PEOPLE ARE OUR GREATEST ASSET", LogTag = "DONE",
                SlowNote = "YOUR WELLBEING IS IMPORTANT TO US",
                Headers = new[] { "TERRAGROUP HR  //  EMPLOYEE SERVICES", "HUMAN RESOURCES  //  ONBOARDING", "TERRAGROUP  //  PEOPLE AND CULTURE" },
                Grants = new[] { "WELCOME TO THE TEAM", "ONBOARDING COMPLETE" },
                Chatter = new[] { "WELLNESS SURVEY  {pct}% POSITIVE", "TRAINING MODULE {n}  ASSIGNED", "MANDATORY FUN  SCHEDULED", "BENEFITS ENROLLMENT  CLOSED", "EXIT INTERVIEWS  {n} PENDING", "TEAM LUNCH  POSTPONED AGAIN", "PERFORMANCE REVIEW  DUE" },
                Headlines = new[] { "STAFF RETENTION 'STABLE', SAYS HR", "NEW OPEN-PLAN OFFICES 'BOOST MORALE'" },
                Memos = new[] { "WELCOME TO TERRAGROUP, {nick}. YOUR ONBOARDING IS COMPLETE. PLEASE COMPLETE YOUR NEXT-OF-KIN FORM TODAY.", "{faction} CONTRACTORS ARE NOT ELIGIBLE FOR THE COMPANY HEALTH PLAN, PENSION OR REPATRIATION.", "EXIT INTERVIEWS ARE MANDATORY FOR ALL DEPARTING STAFF. STAFF WISHING TO DEPART SHOULD SEE SECURITY FIRST.", "THE TEAM-BUILDING DAY HAS MOVED TO INTERCHANGE. BODY ARMOUR WILL BE PROVIDED. ATTENDANCE IS MANDATORY.", "THIS MONTH'S STAFF SURVEY RECORDED A 2% RISE IN MORALE. HR IS LOOKING INTO THE CAUSE.", "ANNUAL LEAVE IS SUSPENDED FOR ALL NORVINSK STAFF. THE ROAD OUT IS ALSO SUSPENDED." }
            },
            new Theme(ThemeChoice.ItDepartment, "04060A", "22304A", "5A7DB4", "46628E", "A8C4F0", "E6EEFA", "7888A4", "FF8C3C", "3CFFB4")
            {
                Backdrops = new[] { BackdropKind.Blueprint, BackdropKind.Grid }, Frames = new[] { FrameKind.Squares, FrameKind.Minimal },
                Intros = new[] { IntroKind.Crt, IntroKind.Wipe }, Bars = new[] { BarKind.Thin, BarKind.Segments }, Outros = new[] { OutroKind.Slide, OutroKind.Zoom },
                Division = "IT SERVICE DESK", TitleCard = "HAVE YOU TRIED TURNING IT OFF AND ON AGAIN", LogTag = "RESOLVED",
                SlowNote = "YOUR CALL IS IMPORTANT TO US",
                Headers = new[] { "TERRAGROUP IT  //  SERVICE DESK", "TERRAGROUP IT  //  ENDPOINT MANAGEMENT", "IT DEPARTMENT  //  SERVER ROOM 2" },
                Grants = new[] { "TICKET RESOLVED", "LOGIN SUCCESSFUL" },
                Chatter = new[] { "TICKET #{n}  ESCALATED", "PASSWORD RESET  {n} REQUESTS", "PATCH TUESDAY  APPLIED", "BACKUP JOB  {pct}% COMPLETE", "PRINTER DRIVER  REINSTALLED", "DISK QUOTA  EXCEEDED", "PHISHING TEST  {n} CLICKED" },
                Headlines = new[] { "IT: 'PLEASE STOP PLUGGING IN FOUND USB DRIVES'", "EMAIL OUTAGE BLAMED ON 'SOLAR ACTIVITY'" },
                Memos = new[] { "DO NOT CONNECT FOUND USB DEVICES TO COMPANY MACHINES. THIS INCLUDES DEVICES FOUND IN THE LAB.", "SERVER ROOM 2 IS RUNNING AT 41 DEGREES. STAFF ARE ASKED NOT TO PROP THE DOOR OPEN WITH FIRE EXTINGUISHERS.", "{pct}% OF STAFF CLICKED THIS MONTH'S PHISHING TEST. MANDATORY RETRAINING HAS BEEN SCHEDULED FOR THEM.", "TETRIZ CONSOLES ARE NOT APPROVED HARDWARE AND WILL BE CONFISCATED FROM DESKS.", "THE GUEST WIFI HAS BEEN SHUT DOWN AFTER IT WAS USED TO COORDINATE A RAID ON THE WAREHOUSE.", "PLANNED MAINTENANCE TONIGHT WILL TAKE THE LAB'S DOOR CONTROLS OFFLINE. PLEASE LEAVE BEFORE 22:00." }
            },
            new Theme(ThemeChoice.Medical, "020809", "0E4A4A", "2EC4B6", "22968C", "A0F5EC", "F0FFFD", "6EA8A2", "FF6A5A", "FF5AC8")
            {
                Backdrops = new[] { BackdropKind.Hex, BackdropKind.Plain }, Frames = new[] { FrameKind.Molecule, FrameKind.Minimal },
                Intros = new[] { IntroKind.Fade, IntroKind.Doors }, Bars = new[] { BarKind.Thin }, Outros = new[] { OutroKind.Sting, OutroKind.Zoom },
                Division = "TERRAGROUP HEALTH", TitleCard = "YOUR HEALTH. OUR SCIENCE.", LogTag = "CHECKED",
                SlowNote = "THE DOCTOR WILL SEE YOU SHORTLY",
                Headers = new[] { "TERRAGROUP HEALTH  //  OCCUPATIONAL MEDICINE", "TERRAGROUP PHARMA  //  DISPENSARY", "STAFF CLINIC  //  NORVINSK CAMPUS" },
                Grants = new[] { "FIT FOR DUTY", "HEALTH CHECK PASSED" },
                Chatter = new[] { "VACCINATION RECORD  UPDATED", "BLOOD PANEL  {n} SAMPLES", "DISPENSARY STOCK  {pct}%", "SICK DAYS  {n} DENIED", "MEDKIT AUDIT  PASSED", "STIMULANT LOG  RECONCILED", "SIDE EFFECTS  WITHIN TOLERANCE" },
                Headlines = new[] { "TERRAGROUP PHARMA LAUNCHES NEW 'WELLNESS' STIMULANT", "CLINIC HOURS EXTENDED FOR STAFF" },
                Memos = new[] { "THE TRADER KNOWN AS THERAPIST HAS ORDERED A FURTHER 40 SURGICAL KITS. THIS ORDER IS NOT TO BE FULFILLED.", "COMBAT STIMULANTS ARE PRESCRIPTION ONLY AND MAY NOT BE TAKEN TO MEET PERFORMANCE TARGETS.", "STAFF TREATED BY THE INDIVIDUAL KNOWN AS SANITAR MUST REPORT TO THE CLINIC FOR TESTING WITHIN 24 HOURS.", "SICK LEAVE IS SUSPENDED UNTIL FURTHER NOTICE. UNWELL STAFF SHOULD REPORT TO THEIR DESK AS NORMAL.", "PAINKILLER STOCKS ARE NOW ISSUED BY SIGNATURE ONLY, AFTER LAST MONTH'S AUDIT.", "THE BLOOD DRIVE CONTINUES ON THURSDAY. PARTICIPATION IS VOLUNTARY AND WILL BE RECORDED." }
            },
            new Theme(ThemeChoice.Facilities, "0A0903", "54460A", "F2C230", "BE961E", "FFE184", "FFF6D8", "AC9C64", "FF5A32", "4A8CFF")
            {
                Backdrops = new[] { BackdropKind.Pinstripe, BackdropKind.Grid }, Frames = new[] { FrameKind.Brackets, FrameKind.Dial },
                Intros = new[] { IntroKind.Doors, IntroKind.Crt }, Bars = new[] { BarKind.Segments }, Outros = new[] { OutroKind.Lift, OutroKind.Slide },
                Division = "FACILITIES MANAGEMENT", TitleCard = "SAFETY FIRST", LogTag = "FIXED",
                SlowNote = "MAINTENANCE IS ON ITS WAY",
                Headers = new[] { "TERRAGROUP FACILITIES  //  BUILDING SERVICES", "FACILITIES  //  PLANT ROOM", "TERRAGROUP  //  HEALTH AND SAFETY" },
                Grants = new[] { "ALL SYSTEMS NOMINAL", "BUILDING OPEN" },
                Chatter = new[] { "ELEVATOR 2  OUT OF SERVICE", "HVAC ZONE {n}  BALANCED", "FIRE DRILL  {n}:00", "KEY REQUEST #{n}  APPROVED", "GENERATOR TEST  PASSED", "WET FLOOR SIGNS  {n} DEPLOYED", "DAYS WITHOUT INCIDENT  0" },
                Headlines = new[] { "FACILITIES: EAST WING PLUMBING 'MOSTLY FIXED'", "NEW SAFETY SIGNAGE INSTALLED CAMPUS-WIDE" },
                Memos = new[] { "LIFT B REMAINS OUT OF SERVICE. A REPAIR DATE WILL BE CONFIRMED WHEN THE ENGINEER IS LOCATED.", "FUEL CANS STORED IN STAIRWELLS ARE A FIRE HAZARD AND WILL BE REMOVED. THEIR CONTENTS WILL BE REUSED.", "THE BUNKER GENERATOR IS FOR EMERGENCY USE ONLY AND MUST NOT BE CONNECTED TO PRIVATE PREMISES.", "DAMAGE TO THE LOBBY GLAZING IS SCHEDULED FOR REPAIR FOR THE FOURTH TIME THIS QUARTER.", "TAP WATER IN BUILDING C HAS PASSED ITS LATEST TEST. RESULTS FOR THE PREVIOUS TEST ARE NOT AVAILABLE.", "CEILING VOIDS ARE NOT STORAGE. ITEMS FOUND THERE WILL BE CONFISCATED BY FACILITIES." }
            },
            new Theme(ThemeChoice.Legal, "0C0405", "50141E", "B4323F", "8C2632", "E8C9A0", "FAF0E6", "A88C84", "FFB43C", "3CB4FF")
            {
                Backdrops = new[] { BackdropKind.Ruled, BackdropKind.Pinstripe }, Frames = new[] { FrameKind.Seal, FrameKind.Ornate },
                Intros = new[] { IntroKind.TitleCard, IntroKind.Fade }, Bars = new[] { BarKind.Thin }, Outros = new[] { OutroKind.Sting },
                Division = "LEGAL DEPARTMENT", TitleCard = "PRIVILEGED AND CONFIDENTIAL", LogTag = "NOTED",
                SlowNote = "PENDING REVIEW BY COUNSEL",
                Headers = new[] { "TERRAGROUP LEGAL  //  OFFICE OF GENERAL COUNSEL", "TERRAGROUP LEGAL  //  CONTRACTS", "LEGAL  //  LITIGATION SUPPORT" },
                Grants = new[] { "TERMS ACCEPTED", "AGREEMENT EXECUTED" },
                Chatter = new[] { "NDA #{n}  COUNTERSIGNED", "LITIGATION HOLD  EXTENDED", "SETTLEMENT  UNDISCLOSED", "CLAUSE {n}  AMENDED", "WAIVER  ON FILE", "DISCOVERY REQUEST  CONTESTED", "STATUTE OF LIMITATIONS  CHECKED" },
                Headlines = new[] { "TERRAGROUP SETTLES CASE, ADMITS NOTHING", "LEGAL: 'THE INCIDENT' IS NOT TO BE DISCUSSED" },
                Memos = new[] { "TERRAGROUP HAS NO KNOWLEDGE OF THE EVENTS DESCRIBED IN RECENT PRESS REPORTS. THIS STATEMENT IS FINAL.", "THIS COMMUNICATION IS COVERED BY YOUR NON-DISCLOSURE AGREEMENT, AS IS ITS EXISTENCE.", "THE CONTRACT WITH THE INDIVIDUAL KNOWN AS PEACEKEEPER IS UNDER RENEGOTIATION. HE HAS REQUESTED BETTER TERMS.", "STAFF CALLED TO GIVE EVIDENCE SHOULD STATE THAT THEY DO NOT RECALL. LEGAL WILL SUPPLY THE WORDING.", "ALL INSURANCE CLAIMS PLACED THROUGH THE TRADER PRAPOR ARE FINAL AND MAY NOT BE CONTESTED.", "STAFF ARE ASKED NOT TO CONTACT LEGAL TO ASK WHETHER A COURSE OF ACTION IS LEGAL." }
            },
            new Theme(ThemeChoice.PublicRelations, "08040C", "4A1A5A", "C83CC8", "9A2E9A", "F7A8F0", "FFEFFF", "A884A8", "FFC03C", "3CFFD2")
            {
                Backdrops = new[] { BackdropKind.Dots, BackdropKind.Plain }, Frames = new[] { FrameKind.Rings, FrameKind.Orbit },
                Intros = new[] { IntroKind.TitleCard, IntroKind.Sheen }, Bars = new[] { BarKind.Solid }, Outros = new[] { OutroKind.Flash, OutroKind.Slide },
                Division = "COMMUNICATIONS", TitleCard = "BUILDING A BETTER TOMORROW", LogTag = "RELEASED",
                SlowNote = "STATEMENT TO FOLLOW",
                Headers = new[] { "TERRAGROUP COMMUNICATIONS  //  PRESS OFFICE", "TERRAGROUP  //  BRAND AND REPUTATION", "PUBLIC RELATIONS  //  MEDIA DESK" },
                Grants = new[] { "MESSAGE APPROVED", "ON BRAND" },
                Chatter = new[] { "PRESS RELEASE #{n}  DRAFTED", "SENTIMENT SCORE  {pct}", "JOURNALIST CALLS  {n} DECLINED", "TALKING POINTS  DISTRIBUTED", "SOCIAL POST  SCHEDULED", "NO COMMENT  ISSUED", "BRAND AUDIT  PASSED" },
                Headlines = new[] { "TERRAGROUP WINS AWARD FOR TRANSPARENCY", "'WE CARE ABOUT NORVINSK', SAYS SPOKESPERSON" },
                Memos = new[] { "THE APPROVED TERM FOR CURRENT EVENTS IN NORVINSK IS 'A LOCAL SECURITY MATTER'. PLEASE USE NO OTHER.", "JOURNALISTS ARE NOT TO BE ADMITTED TO ANY FACILITY. ANY JOURNALIST FOUND ON SITE SHOULD BE REPORTED TO SECURITY.", "IMAGES FROM THE ULTRA MALL INCIDENT ARE BEING REMOVED FROM SOCIAL MEDIA. DO NOT SHARE THEM, EVEN INTERNALLY.", "ARMED PERSONNEL IN THE LOBBY ARE TO BE DESCRIBED TO VISITORS AS 'CONSULTANTS'.", "OUR NEW CAMPAIGN, 'BUILDING A BETTER TOMORROW', HAS BEEN WITHDRAWN IN THE NORVINSK REGION.", "TERRAGROUP HAS MADE A DONATION TO THE CITY OF NORVINSK. THE AMOUNT AND RECIPIENT WILL NOT BE DISCLOSED." }
            },
            new Theme(ThemeChoice.Holiday, "03070C", "1A3A5A", "4FA0E0", "3A7FB8", "CDEBFF", "FFFFFF", "8AA8C0", "FF5A5A", "FF4A6A")
            {
                Backdrops = new[] { BackdropKind.Pinstripe, BackdropKind.Dots }, Frames = new[] { FrameKind.Ornate, FrameKind.Seal },
                Intros = new[] { IntroKind.TitleCard, IntroKind.Sheen }, Bars = new[] { BarKind.Solid }, Outros = new[] { OutroKind.Flash, OutroKind.Zoom },
                Division = "HOLIDAY SCHEDULE", TitleCard = "SEASON'S GREETINGS FROM TERRAGROUP", LogTag = "WRAPPED",
                SlowNote = "SNOW ON THE LINE",
                Headers = new[] { "TERRAGROUP SOCIAL COMMITTEE  //  HOLIDAYS", "TERRAGROUP  //  YEAR-END CLOSE" },
                Grants = new[] { "HAPPY HOLIDAYS, OPERATOR", "SEASON'S GREETINGS" },
                Chatter = new[] { "HOLIDAY ROSTER  POSTED", "GIFT EXCHANGE  {n} PARTICIPANTS", "BONUSES  UNDER REVIEW", "SNOW PLOW  DISPATCHED", "TREE IN LOBBY  INSPECTED", "OFFICE CLOSED  31 DEC - 1 JAN" },
                Headlines = new[] { "HOLIDAY PARTY CANCELLED. ATTENDANCE REMAINS MANDATORY", "YEAR-END BONUSES PAID IN COMPANY STOCK" },
                Memos = new[] { "THE HOLIDAY PARTY IS CANCELLED FOR SECURITY REASONS. ATTENDANCE REMAINS MANDATORY.", "SECRET SANTA GIFTS ARE LIMITED TO A VALUE OF ONE GRAPHICS CARD. GRENADES ARE NOT AN ACCEPTABLE GIFT.", "THE LOBBY TREE HAS BEEN DECORATED BY THE SECURITY TEAM. PLEASE DO NOT TOUCH THE DECORATIONS.", "A FIGURE IN A RED SUIT HAS BEEN SIGHTED ON LIGHTHOUSE. STAFF ARE ADVISED NOT TO APPROACH.", "THE OFFICE WILL CLOSE ON 25 DECEMBER. STAFF ON SITE WILL REMAIN ON SITE.", "THIS YEAR'S HOLIDAY BONUS HAS BEEN ISSUED AS A BOX OF MATCHES. THANK YOU FOR YOUR SERVICE." }
            }
        };

        internal static Theme Get(ThemeChoice id)
        {
            foreach (Theme theme in All)
            {
                if (theme.Id == id)
                {
                    return theme;
                }
            }
            return All[0];
        }

        /// <summary>The seasonal theme for today's date, or null.</summary>
        internal static Theme InSeason(DateTime now)
        {
            // Christmas Day only.
            if (now.Month == 12 && now.Day == 25)
            {
                return Get(ThemeChoice.Holiday);
            }
            return null;
        }

        internal static Color32 WithAlpha(Color32 color, byte alpha) => new Color32(color.r, color.g, color.b, alpha);

        private static Color32 Hex(string hex) => new Color32(
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16),
            255);
    }

    internal enum NoticeKind
    {
        Memo,
        Intel,
        Note
    }

    /// <summary>One notice that slides in during the load. Its body may hold Lore placeholders, filled as it shows.</summary>
    internal sealed class Notice
    {
        internal float At;
        internal NoticeKind Kind;
        internal string From;
        internal string Body;
        /// <summary>For a memo: who signed it and its reference, shown under the body.</summary>
        internal string Signature;
    }

    /// <summary>
    /// Everything about one launch's terminal that is left to chance: division, ring layout,
    /// header, badge, the closing words, the news ticker, and the rare surprises (a fake denial,
    /// a memo). Rolled once when the terminal starts, so nothing here costs anything per frame.
    /// </summary>
    internal sealed class Variant
    {
        internal Theme Theme;
        internal GlitchLevel Glitches;

        internal BackdropKind Backdrop;
        internal FrameKind Frame;
        internal IntroKind Intro;
        internal BarKind Bar;
        internal OutroKind Outro;

        /// <summary>How the brand and the readout are arranged on the screen.</summary>
        internal LayoutKind Layout;

        internal int DashCount;
        internal int DashMajorEvery;
        internal float DashSpin;
        internal int ArcCount;
        internal float ArcSweep;
        internal float ArcStart;
        internal float ArcSpin;
        internal int ChartSeed;

        internal string Header;
        internal string TopRight;
        internal string GrantText;
        internal bool Greet;
        internal bool FakeDenial;
        internal bool Milestone;
        internal string Ticker;

        /// <summary>The notices that slide in during the load, in order: memos, intel, the odd note.</summary>
        internal Notice[] Notices;

        private static readonly string[] Stocks = { "TGRP", "TG-LABS", "NVSK IDX", "BTC", "RUB/USD", "USEC LTD", "TG-PHARMA" };

        /// <param name="preview">
        /// A preview (or a roll never shown): shows three notices, is more likely to fake a
        /// denial, and does not mark what it picks as seen.
        /// </param>
        internal static Variant Roll(System.Random random, ThemeChoice choice, GlitchLevel glitches, int launches, Pins pins, bool preview = false)
        {
            var v = new Variant { Glitches = glitches };
            // What this launch shows is picked from what the player has not seen lately; a
            // preview picks the same way but leaves the record alone.
            Archive.NewRun();
            Archive.Recording = !preview;
            v.Milestone = launches > 0 && launches % 100 == 0;

            DateTime now = DateTime.Now;
            Theme season = Theme.InSeason(now);
            if (choice != ThemeChoice.Random)
            {
                v.Theme = Theme.Get(choice);
            }
            else if (v.Milestone)
            {
                v.Theme = Theme.Get(ThemeChoice.Executive);
            }
            else if (season != null && random.NextDouble() < 0.6)
            {
                v.Theme = season;
            }
            else
            {
                // Every division equally likely, but the ones shown in the last few launches sit
                // out, so the same screen does not come round again soon.
                var divisions = new List<Theme>();
                var names = new List<string>();
                foreach (Theme theme in Theme.All)
                {
                    if (theme.Id != ThemeChoice.Holiday)
                    {
                        divisions.Add(theme);
                        names.Add(theme.Id.ToString());
                    }
                }
                v.Theme = divisions[Archive.PickShuffled("division", names, random)];
            }
            Theme t = v.Theme;
            v.Backdrop = PickPart(random, t, "backdrop", t.Backdrops);
            v.Frame = PickPart(random, t, "frame", t.Frames);
            v.Intro = PickPart(random, t, "intro", t.Intros);
            v.Bar = PickPart(random, t, "bar", t.Bars);
            v.Outro = PickPart(random, t, "outro", t.Outros);
            v.Layout = PickLayout(random);
            if (pins.Layout != LayoutKind.Random) v.Layout = pins.Layout;
            if (pins.Backdrop != BackdropKind.Random) v.Backdrop = pins.Backdrop;
            if (pins.Frame != FrameKind.Random) v.Frame = pins.Frame;
            if (pins.Intro != IntroKind.Random) v.Intro = pins.Intro;
            if (pins.Bar != BarKind.Random) v.Bar = pins.Bar;
            if (pins.Outro != OutroKind.Random) v.Outro = pins.Outro;

            int[] dashCounts = { 36, 48, 60, 72 };
            v.DashCount = dashCounts[random.Next(dashCounts.Length)];
            v.DashMajorEvery = random.Next(2) == 0 ? 4 : 6;
            v.DashSpin = Range(random, 6f, 16f) * Sign(random);
            v.ArcCount = random.Next(2, 5);
            v.ArcSweep = 360f / v.ArcCount * Range(random, 0.35f, 0.7f);
            v.ArcStart = Range(random, 0f, 360f);
            v.ArcSpin = Range(random, 16f, 36f) * Sign(random);
            v.ChartSeed = random.Next();

            v.Header = t.Headers[random.Next(t.Headers.Length)];
            string employee = $"{random.Next(1, 100):00}-{random.Next(1000, 10000)}";
            string clearance = "ABCD"[random.Next(4)].ToString();
            string session = launches > 0 ? $"  //  SESSION {launches:0000}" : string.Empty;
            v.TopRight = $"EMPLOYEE {employee}  //  CLEARANCE {clearance}{session}{(v.Milestone ? "  //  MILESTONE" : string.Empty)}";

            // The mod is ACCESS GRANTED: that stays the most common ending.
            v.GrantText = random.Next(100) < 55 ? "ACCESS GRANTED" : t.Grants[random.Next(t.Grants.Length)];
            v.Greet = random.Next(100) < 30;
            float denial = glitches == GlitchLevel.Off ? 0f : preview ? 1f / 3f : glitches == GlitchLevel.Heavy ? 1f / 8f : 1f / 25f;
            v.FakeDenial = random.NextDouble() < denial;

            v.Ticker = BuildTicker(random, t);

            // Two or three notices a launch, spread over it: the division's memos, memos from
            // elsewhere in the company, regional intel, and now and then something nobody admits
            // to sending.
            int count = preview ? 3 : random.Next(2, 4);
            float[] at = { 0.12f, 0.4f, 0.68f };
            v.Notices = new Notice[count];
            List<string> divisionMemos = Correspondence.MemosFor(t);
            var commonBodies = new List<string>();
            foreach (string[] memo in Correspondence.CommonMemos)
            {
                commonBodies.Add(memo[1]);
            }
            for (int i = 0; i < count; i++)
            {
                var notice = new Notice { At = at[i] + Range(random, 0f, 0.08f) };
                int roll = random.Next(100);
                if (roll < 10)
                {
                    notice.Kind = NoticeKind.Note;
                    notice.From = "UNKNOWN SENDER";
                    notice.Body = Lore.EasterEgg(random);
                }
                else if (roll < 45)
                {
                    notice.Kind = NoticeKind.Intel;
                    notice.From = Lore.IntelSender(random);
                    notice.Body = Lore.IntelTemplate(random);
                }
                else if (random.Next(10) < 7)
                {
                    notice.Kind = NoticeKind.Memo;
                    notice.From = t.Division;
                    notice.Body = Archive.PickLine("memo", divisionMemos, random);
                    notice.Signature = Correspondence.Signature(random, t.Id);
                }
                else
                {
                    int pick = Archive.Pick("memo", commonBodies, random);
                    string sender = Correspondence.CommonMemos[pick][0];
                    notice.Kind = NoticeKind.Memo;
                    notice.From = sender;
                    notice.Body = commonBodies[pick];
                    notice.Signature = Correspondence.Signature(random, t.Id, sender);
                }
                v.Notices[i] = notice;
            }
            Archive.Save();
            return v;
        }

        /// <summary>
        /// One loop of the news ticker: share prices between headlines. The headlines are the
        /// division's and the company's, the ones the player has not seen lately, with a few
        /// lines of regional intel among them.
        /// </summary>
        private static string BuildTicker(System.Random random, Theme theme)
        {
            const int Count = 8;
            List<string> pool = Correspondence.HeadlinesFor(theme);
            var headlines = new List<string>();
            int intel = random.Next(2, 4);
            for (int i = 0; i < Count - intel; i++)
            {
                headlines.Add(Lore.Fill(Archive.PickLine("headline", pool, random), random));
            }
            headlines.AddRange(Lore.Headlines(random, intel));
            for (int i = headlines.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (headlines[i], headlines[j]) = (headlines[j], headlines[i]);
            }

            var builder = new StringBuilder();
            int stocksPerHeadline = theme.Id == ThemeChoice.Finance ? 3 : 1;
            int stock = random.Next(Stocks.Length);
            for (int h = 0; h < headlines.Count; h++)
            {
                for (int s = 0; s < stocksPerHeadline; s++)
                {
                    float change = Range(random, -3.5f, 4.5f);
                    string colour = change >= 0f ? theme.AccentHex : theme.WarnHex;
                    builder.Append(Stocks[stock++ % Stocks.Length]).Append("  ")
                        .Append(Range(random, 40f, 900f).ToString("0.00"))
                        .Append("  <color=").Append(colour).Append('>')
                        .Append(change >= 0f ? "+" : string.Empty).Append(change.ToString("0.00")).Append("%</color>")
                        .Append("     |     ");
                }
                builder.Append(headlines[h]).Append("     |     ");
            }
            return builder.ToString();
        }

        /// <summary>One of the division's choices for a slot, avoiding the ones it showed last time.</summary>
        private static T PickPart<T>(System.Random random, Theme theme, string slot, T[] choices)
        {
            var names = new List<string>();
            foreach (T choice in choices)
            {
                names.Add(choice.ToString());
            }
            return choices[Archive.PickShuffled(slot + "." + theme.Id, names, random)];
        }

        private static LayoutKind PickLayout(System.Random random)
        {
            // Any layout, avoiding the ones shown last time, like the divisions.
            var layouts = new List<LayoutKind>();
            var names = new List<string>();
            foreach (LayoutKind layout in Enum.GetValues(typeof(LayoutKind)))
            {
                if (layout != LayoutKind.Random)
                {
                    layouts.Add(layout);
                    names.Add(layout.ToString());
                }
            }
            return layouts[Archive.PickShuffled("layout", names, random)];
        }

        private static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        private static float Sign(System.Random random) => random.Next(2) == 0 ? -1f : 1f;
    }

    /// <summary>
    /// Made-up office chatter for the line under the log: the division's own lines and shared
    /// ones, picked from what the player has not seen lately (see Archive). Never mixed into the
    /// real steps, so the log and its times stay true.
    /// </summary>
    internal sealed class Chatter
    {
        private readonly System.Random _random;
        private readonly List<string> _division;
        private readonly string[] _common;

        internal Chatter(System.Random random, Theme theme)
        {
            _random = random;
            _division = Correspondence.ChatterFor(theme);
            _common = Correspondence.CommonChatter;
        }

        internal string Next()
        {
            // TerraGroup watches the whole region: about a third of the chatter is intel. (The
            // unsigned notes are too long for this one line; they arrive as notices instead.)
            int roll = _random.Next(100);
            if (roll < 35)
            {
                return ":: " + Lore.IntelLine(_random);
            }
            // The division's own lines come up more often than the shared ones.
            string line = roll < 75 && _division.Count > 0
                ? Archive.PickLine("chatter", _division, _random)
                : Archive.PickLine("chatter", _common, _random);
            if (line.IndexOf('{') < 0)
            {
                return ":: " + line;
            }
            return ":: " + line
                .Replace("{hex}", _random.Next(0x1000, 0x10000).ToString("X4"))
                .Replace("{mods}", BepInEx.Bootstrap.Chainloader.PluginInfos.Count.ToString())
                .Replace("{emp}", $"{_random.Next(1, 100):00}-{_random.Next(1000, 10000)}")
                .Replace("{pct}", _random.Next(40, 100).ToString())
                .Replace("{n}", _random.Next(2, 999).ToString());
        }
    }
}
