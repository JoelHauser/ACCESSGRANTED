using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace StartupProgress
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
        NightShift,
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
            Layout = StartupProgressPlugin.Layout.Value,
            Backdrop = StartupProgressPlugin.Backdrop.Value,
            Frame = StartupProgressPlugin.Frame.Value,
            Intro = StartupProgressPlugin.Intro.Value,
            Bar = StartupProgressPlugin.Bar.Value,
            Outro = StartupProgressPlugin.Ending.Value
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

        /// <summary>Share of a random roll, by day and by night. Seasonal themes are 0 and come by date.</summary>
        internal float DayWeight;
        internal float NightWeight;

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
                DayWeight = 34, NightWeight = 28,
                Backdrops = new[] { BackdropKind.Dots, BackdropKind.Blueprint, BackdropKind.Plain }, Frames = new[] { FrameKind.Rings, FrameKind.Minimal, FrameKind.Orbit },
                Intros = new[] { IntroKind.TitleCard, IntroKind.Doors, IntroKind.Fade }, Bars = new[] { BarKind.Segments, BarKind.Solid }, Outros = new[] { OutroKind.Zoom, OutroKind.Sting, OutroKind.Slide },
                Watermark = true,
                Division = "CORPORATE HEADQUARTERS", TitleCard = "VIRTUS IN SCIENTIA",
                Headers = new[] { "TERRAGROUP INTRANET  //  EMPLOYEE PORTAL", "TERRAGROUP  //  CORPORATE HEADQUARTERS", "TERRAGROUP  //  NORVINSK REGIONAL OFFICE" },
                Grants = new[] { "WELCOME TO TERRAGROUP", "SIGN-IN SUCCESSFUL" },
                Chatter = new[] { "MISSION STATEMENT  ACKNOWLEDGED", "ORG CHART  {n} POSITIONS VACANT", "TOWN HALL RECORDING  AVAILABLE", "BRAND GUIDELINES  REV {n}  APPLIED" },
                Headlines = new[] { "TERRAGROUP NAMED REGION'S MOST TRUSTED EMPLOYER", "CEO ADDRESSES STAFF: 'THE FUTURE IS BRIGHT'" }
            },
            new Theme(ThemeChoice.Research, "0C0305", "6A0A14", "D9283A", "A01E2C", "FF8A8A", "FFE8E8", "B47078", "FFC83C", "3CDCFF")
            {
                DayWeight = 14, NightWeight = 14,
                Backdrops = new[] { BackdropKind.Hex, BackdropKind.Blueprint }, Frames = new[] { FrameKind.Molecule, FrameKind.Orbit },
                Intros = new[] { IntroKind.Wipe, IntroKind.Fade, IntroKind.Doors }, Bars = new[] { BarKind.Segments, BarKind.Thin }, Outros = new[] { OutroKind.Sting, OutroKind.Zoom },
                Division = "RESEARCH DIVISION", TitleCard = "TERRAGROUP LABS", LogTag = "CLEARED",
                SlowNote = "RESULTS PENDING",
                Headers = new[] { "TERRAGROUP LABS  //  RESEARCH DIVISION", "LABS SUBNET B-14  //  RESTRICTED", "TERRAGROUP LABS  //  CLINICAL TRIALS OFFICE" },
                Grants = new[] { "CLEARANCE CONFIRMED", "LAB ACCESS APPROVED" },
                Chatter = new[] { "SPECIMEN REGISTRY  {n} ENTRIES", "CLEAN ROOM 4  PRESSURE NOMINAL", "TRIAL PROTOCOL 6-HELIX  ON SCHEDULE", "ETHICS REVIEW  WAIVED", "COLD STORAGE  -80C  STABLE", "SAMPLE CHAIN OF CUSTODY  SIGNED", "INCIDENT REPORT #{n}  REDACTED PER LEGAL" },
                Headlines = new[] { "LABS EXPANSION AHEAD OF SCHEDULE", "TERRAGROUP TRIALS 'SHOWING PROMISE', SAYS TERRAGROUP" }
            },
            new Theme(ThemeChoice.Security, "0A0500", "6E3400", "FF8C14", "C8640A", "FFBE5A", "FFE2B4", "B0783C", "FF4632", "3CC8FF")
            {
                DayWeight = 12, NightWeight = 18,
                Backdrops = new[] { BackdropKind.Grid, BackdropKind.Blueprint }, Frames = new[] { FrameKind.Brackets, FrameKind.Squares },
                Intros = new[] { IntroKind.Crt, IntroKind.Doors }, Bars = new[] { BarKind.Thin, BarKind.Segments }, Outros = new[] { OutroKind.Lift, OutroKind.Slide },
                Scanlines = true,
                Division = "SECURITY DEPARTMENT", TitleCard = "AUTHORIZED PERSONNEL ONLY", LogTag = "CLEAR",
                SlowNote = "PLEASE REMAIN AT THE CHECKPOINT",
                Headers = new[] { "TERRAGROUP SECURITY  //  CHECKPOINT 3", "TERRAGROUP SECURITY  //  BADGE CONTROL", "PHYSICAL SECURITY  //  CCTV OPERATIONS" },
                Grants = new[] { "BADGE ACCEPTED", "CHECKPOINT CLEARED" },
                Chatter = new[] { "BADGE READER  GATE {n}  ONLINE", "CCTV  {n} CAMERAS RECORDING", "VISITOR LOG  {n} ENTRIES", "PATROL ROUTE B  CONFIRMED", "PERIMETER FENCE  NO ALERTS", "KEYCARD REGISTRY  {n} ACTIVE", "ARMORY INVENTORY  RECONCILED" },
                Headlines = new[] { "SECURITY REMINDS STAFF: TAILGATING IS A FIREABLE OFFENSE", "NEW BADGE READERS INSTALLED AT ALL GATES" }
            },
            new Theme(ThemeChoice.Finance, "020805", "0A4A28", "2FB86A", "23905A", "8CF0B0", "DFFFEA", "6FA088", "FFB43C", "FF3CA0")
            {
                DayWeight = 13, NightWeight = 10,
                Backdrops = new[] { BackdropKind.Dots, BackdropKind.Grid }, Frames = new[] { FrameKind.Dial, FrameKind.Minimal },
                Intros = new[] { IntroKind.Wipe, IntroKind.Fade }, Bars = new[] { BarKind.Solid, BarKind.Thin }, Outros = new[] { OutroKind.Zoom, OutroKind.Slide },
                Chart = true,
                Division = "INVESTOR RELATIONS", TitleCard = "TGRP  //  NORVINSK EXCHANGE", LogTag = "POSTED",
                SlowNote = "MARKET VOLATILITY",
                Headers = new[] { "TERRAGROUP FINANCE  //  INVESTOR RELATIONS", "TERRAGROUP TREASURY  //  LEDGER ACCESS", "TERRAGROUP  //  QUARTERLY REPORTING" },
                Grants = new[] { "ACCOUNT VERIFIED", "TRANSACTION APPROVED" },
                Chatter = new[] { "LEDGER CLOSE  Q4  IN PROGRESS", "ESCROW  FLEA MARKET  SETTLED", "INSURANCE CLAIMS  {n} PENDING", "RUB/USD FEED  SYNCED", "AUDIT TRAIL  {n} TRANSACTIONS", "DIVIDEND SCHEDULE  POSTPONED", "OFFSHORE TRANSFER  APPROVED" },
                Headlines = new[] { "TGRP HITS ALL-TIME HIGH ON LABS NEWS", "ANALYSTS: 'BUY', 'STRONG BUY', 'DO NOT ASK'" }
            },
            new Theme(ThemeChoice.Records, "060607", "3C3C40", "AAAAAF", "78787D", "E1E1E6", "F0F0F0", "828287", "D23C3C", "3CA0FF")
            {
                DayWeight = 13, NightWeight = 12,
                Backdrops = new[] { BackdropKind.Ruled, BackdropKind.Plain }, Frames = new[] { FrameKind.Seal, FrameKind.Minimal },
                Intros = new[] { IntroKind.Fade, IntroKind.TitleCard }, Bars = new[] { BarKind.Thin }, Outros = new[] { OutroKind.Sting, OutroKind.Lift },
                Division = "RECORDS MANAGEMENT", TitleCard = "CONFIDENTIAL", LogTag = "FILED",
                SlowNote = "RETRIEVING FROM OFF-SITE STORAGE",
                Headers = new[] { "TERRAGROUP RECORDS  //  ARCHIVE ACCESS", "RECORDS MANAGEMENT  //  BASEMENT LEVEL 2", "TERRAGROUP  //  DOCUMENT CONTROL" },
                Grants = new[] { "FILE OPENED", "RECORD RETRIEVED" },
                Chatter = new[] { "ARCHIVE BOX #{n}  RETRIEVED", "MICROFILM INDEX  REBUILT", "DOCUMENT 0x{hex}  CLASSIFIED", "RETENTION POLICY  APPLIED", "SHREDDING QUEUE  {n} PAGES", "CROSS-REFERENCING PERSONNEL FILES" },
                Headlines = new[] { "RECORDS: ALL 2017 FILES 'LOST IN A FLOOD'", "DOCUMENT RETENTION POLICY SHORTENED TO 0 DAYS" }
            },
            new Theme(ThemeChoice.Executive, "080602", "6E500A", "DCAA28", "AA7D19", "FFD76E", "FFF0C8", "AA915A", "FF5A3C", "5A8CFF")
            {
                DayWeight = 3, NightWeight = 3,
                Backdrops = new[] { BackdropKind.Pinstripe }, Frames = new[] { FrameKind.Ornate, FrameKind.Squares },
                Intros = new[] { IntroKind.Sheen, IntroKind.Doors }, Bars = new[] { BarKind.Solid }, Outros = new[] { OutroKind.Flash, OutroKind.Sting },
                Division = "EXECUTIVE BOARD", TitleCard = "EXECUTIVE SUITE", LogTag = "SIGNED",
                SlowNote = "YOUR DRIVER IS ON THE WAY",
                Headers = new[] { "TERRAGROUP  //  EXECUTIVE BOARD", "TERRAGROUP  //  OFFICE OF THE CHAIRMAN" },
                Grants = new[] { "WELCOME TO THE BOARD", "EXECUTIVE ACCESS GRANTED" },
                Chatter = new[] { "BOARD MINUTES  SEALED", "GOLDEN PARACHUTE  FUNDED", "PRIVATE JET  FUELED", "SHAREHOLDER LETTER  APPROVED", "KAPPA CONTAINER  REGISTERED", "COLLECTOR ITEMS  {n} CATALOGUED" },
                Headlines = new[] { "BOARD APPROVES ITS OWN BONUSES", "CHAIRMAN'S YACHT 'NOT A COMPANY ASSET'" }
            },
            new Theme(ThemeChoice.InternalAffairs, "06020C", "461478", "9646FF", "6E32C8", "E678FF", "F0DCFF", "9678BE", "FFBE3C", "3CFFC8")
            {
                DayWeight = 3, NightWeight = 3,
                Backdrops = new[] { BackdropKind.Grid, BackdropKind.Plain }, Frames = new[] { FrameKind.Brackets, FrameKind.Orbit },
                Intros = new[] { IntroKind.TitleCard }, Bars = new[] { BarKind.Segments }, Outros = new[] { OutroKind.Sting, OutroKind.Zoom },
                Division = "INTERNAL AFFAIRS", TitleCard = "THIS SESSION IS BEING REVIEWED", LogTag = "NOTED",
                SlowNote = "PLEASE WAIT. YOU ARE NOT IN TROUBLE",
                Headers = new[] { "TERRAGROUP  //  INTERNAL AFFAIRS", "OFFICE OF COMPLIANCE  //  REVIEW BOARD" },
                Grants = new[] { "ACCESS GRANTED... [MONITORED]", "SESSION LOGGED" },
                Chatter = new[] { "SESSION FLAGGED FOR REVIEW", "KEYSTROKES  LOGGED", "INTERVIEW ROOM 2  RESERVED", "PERSONNEL FILE {emp}  OPENED", "ANOMALY SCORE  {pct}", "WITNESS STATEMENT #{n}  FILED" },
                Headlines = new[] { "INTERNAL AFFAIRS: 'WE ARE ALWAYS LISTENING'", "WHISTLEBLOWER HOTLINE TEMPORARILY DISCONNECTED" }
            },
            new Theme(ThemeChoice.Logistics, "020A0C", "0A4650", "1FA5B5", "17808C", "7FE3EE", "DDFAFF", "64A0A8", "FFB43C", "FF4A7A")
            {
                DayWeight = 10, NightWeight = 10,
                Backdrops = new[] { BackdropKind.Grid, BackdropKind.Blueprint }, Frames = new[] { FrameKind.Dial, FrameKind.Orbit },
                Intros = new[] { IntroKind.Wipe, IntroKind.Doors }, Bars = new[] { BarKind.Segments, BarKind.Thin }, Outros = new[] { OutroKind.Slide, OutroKind.Lift },
                Division = "LOGISTICS AND SUPPLY", TitleCard = "MOVING WHAT MATTERS", LogTag = "SHIPPED",
                SlowNote = "DELAYED AT CUSTOMS",
                Headers = new[] { "TERRAGROUP LOGISTICS  //  DISPATCH", "TERRAGROUP LOGISTICS  //  PORT OPERATIONS", "SUPPLY CHAIN  //  NORVINSK DEPOT" },
                Grants = new[] { "DELIVERY CONFIRMED", "MANIFEST CLEARED" },
                Chatter = new[] { "CONTAINER TG-{n}  SEALED", "MANIFEST #{n}  RECONCILED", "TRUCK CONVOY  DEPARTED", "WAREHOUSE B  {pct}% CAPACITY", "CUSTOMS FORMS  {n} PENDING", "FORKLIFT CERT  EXPIRED", "PALLETS  {n} UNACCOUNTED FOR" },
                Headlines = new[] { "PORT TERMINAL BACKLOG 'SHRINKING'", "LOGISTICS: ALL SHIPMENTS ARRIVED, MOSTLY" }
            },
            new Theme(ThemeChoice.HumanResources, "0C0508", "5A2236", "E0607E", "B04A62", "FFB2C0", "FFEFF2", "B88894", "FFC850", "50D2FF")
            {
                DayWeight = 9, NightWeight = 7,
                Backdrops = new[] { BackdropKind.Dots, BackdropKind.Plain }, Frames = new[] { FrameKind.Minimal, FrameKind.Seal },
                Intros = new[] { IntroKind.Fade, IntroKind.TitleCard }, Bars = new[] { BarKind.Solid }, Outros = new[] { OutroKind.Sting, OutroKind.Zoom },
                Division = "HUMAN RESOURCES", TitleCard = "OUR PEOPLE ARE OUR GREATEST ASSET", LogTag = "DONE",
                SlowNote = "YOUR WELLBEING IS IMPORTANT TO US",
                Headers = new[] { "TERRAGROUP HR  //  EMPLOYEE SERVICES", "HUMAN RESOURCES  //  ONBOARDING", "TERRAGROUP  //  PEOPLE AND CULTURE" },
                Grants = new[] { "WELCOME TO THE TEAM", "ONBOARDING COMPLETE" },
                Chatter = new[] { "WELLNESS SURVEY  {pct}% POSITIVE", "TRAINING MODULE {n}  ASSIGNED", "MANDATORY FUN  SCHEDULED", "BENEFITS ENROLLMENT  CLOSED", "EXIT INTERVIEWS  {n} PENDING", "TEAM LUNCH  POSTPONED AGAIN", "PERFORMANCE REVIEW  DUE" },
                Headlines = new[] { "STAFF RETENTION 'STABLE', SAYS HR", "NEW OPEN-PLAN OFFICES 'BOOST MORALE'" }
            },
            new Theme(ThemeChoice.ItDepartment, "04060A", "22304A", "5A7DB4", "46628E", "A8C4F0", "E6EEFA", "7888A4", "FF8C3C", "3CFFB4")
            {
                DayWeight = 9, NightWeight = 10,
                Backdrops = new[] { BackdropKind.Blueprint, BackdropKind.Grid }, Frames = new[] { FrameKind.Squares, FrameKind.Minimal },
                Intros = new[] { IntroKind.Crt, IntroKind.Wipe }, Bars = new[] { BarKind.Thin, BarKind.Segments }, Outros = new[] { OutroKind.Slide, OutroKind.Zoom },
                Division = "IT SERVICE DESK", TitleCard = "HAVE YOU TRIED TURNING IT OFF AND ON AGAIN", LogTag = "RESOLVED",
                SlowNote = "YOUR CALL IS IMPORTANT TO US",
                Headers = new[] { "TERRAGROUP IT  //  SERVICE DESK", "TERRAGROUP IT  //  ENDPOINT MANAGEMENT", "IT DEPARTMENT  //  SERVER ROOM 2" },
                Grants = new[] { "TICKET RESOLVED", "LOGIN SUCCESSFUL" },
                Chatter = new[] { "TICKET #{n}  ESCALATED", "PASSWORD RESET  {n} REQUESTS", "PATCH TUESDAY  APPLIED", "BACKUP JOB  {pct}% COMPLETE", "PRINTER DRIVER  REINSTALLED", "DISK QUOTA  EXCEEDED", "PHISHING TEST  {n} CLICKED" },
                Headlines = new[] { "IT: 'PLEASE STOP PLUGGING IN FOUND USB DRIVES'", "EMAIL OUTAGE BLAMED ON 'SOLAR ACTIVITY'" }
            },
            new Theme(ThemeChoice.Medical, "020809", "0E4A4A", "2EC4B6", "22968C", "A0F5EC", "F0FFFD", "6EA8A2", "FF6A5A", "FF5AC8")
            {
                DayWeight = 9, NightWeight = 8,
                Backdrops = new[] { BackdropKind.Hex, BackdropKind.Plain }, Frames = new[] { FrameKind.Molecule, FrameKind.Minimal },
                Intros = new[] { IntroKind.Fade, IntroKind.Doors }, Bars = new[] { BarKind.Thin }, Outros = new[] { OutroKind.Sting, OutroKind.Zoom },
                Division = "TERRAGROUP HEALTH", TitleCard = "YOUR HEALTH. OUR SCIENCE.", LogTag = "CHECKED",
                SlowNote = "THE DOCTOR WILL SEE YOU SHORTLY",
                Headers = new[] { "TERRAGROUP HEALTH  //  OCCUPATIONAL MEDICINE", "TERRAGROUP PHARMA  //  DISPENSARY", "STAFF CLINIC  //  NORVINSK CAMPUS" },
                Grants = new[] { "FIT FOR DUTY", "HEALTH CHECK PASSED" },
                Chatter = new[] { "VACCINATION RECORD  UPDATED", "BLOOD PANEL  {n} SAMPLES", "DISPENSARY STOCK  {pct}%", "SICK DAYS  {n} DENIED", "MEDKIT AUDIT  PASSED", "STIMULANT LOG  RECONCILED", "SIDE EFFECTS  WITHIN TOLERANCE" },
                Headlines = new[] { "TERRAGROUP PHARMA LAUNCHES NEW 'WELLNESS' STIMULANT", "CLINIC HOURS EXTENDED FOR STAFF" }
            },
            new Theme(ThemeChoice.Facilities, "0A0903", "54460A", "F2C230", "BE961E", "FFE184", "FFF6D8", "AC9C64", "FF5A32", "4A8CFF")
            {
                DayWeight = 8, NightWeight = 9,
                Backdrops = new[] { BackdropKind.Pinstripe, BackdropKind.Grid }, Frames = new[] { FrameKind.Brackets, FrameKind.Dial },
                Intros = new[] { IntroKind.Doors, IntroKind.Crt }, Bars = new[] { BarKind.Segments }, Outros = new[] { OutroKind.Lift, OutroKind.Slide },
                Division = "FACILITIES MANAGEMENT", TitleCard = "SAFETY FIRST", LogTag = "FIXED",
                SlowNote = "MAINTENANCE IS ON ITS WAY",
                Headers = new[] { "TERRAGROUP FACILITIES  //  BUILDING SERVICES", "FACILITIES  //  PLANT ROOM", "TERRAGROUP  //  HEALTH AND SAFETY" },
                Grants = new[] { "ALL SYSTEMS NOMINAL", "BUILDING OPEN" },
                Chatter = new[] { "ELEVATOR 2  OUT OF SERVICE", "HVAC ZONE {n}  BALANCED", "FIRE DRILL  {n}:00", "KEY REQUEST #{n}  APPROVED", "GENERATOR TEST  PASSED", "WET FLOOR SIGNS  {n} DEPLOYED", "DAYS WITHOUT INCIDENT  0" },
                Headlines = new[] { "FACILITIES: EAST WING PLUMBING 'MOSTLY FIXED'", "NEW SAFETY SIGNAGE INSTALLED CAMPUS-WIDE" }
            },
            new Theme(ThemeChoice.Legal, "0C0405", "50141E", "B4323F", "8C2632", "E8C9A0", "FAF0E6", "A88C84", "FFB43C", "3CB4FF")
            {
                DayWeight = 7, NightWeight = 7,
                Backdrops = new[] { BackdropKind.Ruled, BackdropKind.Pinstripe }, Frames = new[] { FrameKind.Seal, FrameKind.Ornate },
                Intros = new[] { IntroKind.TitleCard, IntroKind.Fade }, Bars = new[] { BarKind.Thin }, Outros = new[] { OutroKind.Sting },
                Division = "LEGAL DEPARTMENT", TitleCard = "PRIVILEGED AND CONFIDENTIAL", LogTag = "NOTED",
                SlowNote = "PENDING REVIEW BY COUNSEL",
                Headers = new[] { "TERRAGROUP LEGAL  //  OFFICE OF GENERAL COUNSEL", "TERRAGROUP LEGAL  //  CONTRACTS", "LEGAL  //  LITIGATION SUPPORT" },
                Grants = new[] { "TERMS ACCEPTED", "AGREEMENT EXECUTED" },
                Chatter = new[] { "NDA #{n}  COUNTERSIGNED", "LITIGATION HOLD  EXTENDED", "SETTLEMENT  UNDISCLOSED", "CLAUSE {n}  AMENDED", "WAIVER  ON FILE", "DISCOVERY REQUEST  CONTESTED", "STATUTE OF LIMITATIONS  CHECKED" },
                Headlines = new[] { "TERRAGROUP SETTLES CASE, ADMITS NOTHING", "LEGAL: 'THE INCIDENT' IS NOT TO BE DISCUSSED" }
            },
            new Theme(ThemeChoice.PublicRelations, "08040C", "4A1A5A", "C83CC8", "9A2E9A", "F7A8F0", "FFEFFF", "A884A8", "FFC03C", "3CFFD2")
            {
                DayWeight = 8, NightWeight = 6,
                Backdrops = new[] { BackdropKind.Dots, BackdropKind.Plain }, Frames = new[] { FrameKind.Rings, FrameKind.Orbit },
                Intros = new[] { IntroKind.TitleCard, IntroKind.Sheen }, Bars = new[] { BarKind.Solid }, Outros = new[] { OutroKind.Flash, OutroKind.Slide },
                Division = "COMMUNICATIONS", TitleCard = "BUILDING A BETTER TOMORROW", LogTag = "RELEASED",
                SlowNote = "STATEMENT TO FOLLOW",
                Headers = new[] { "TERRAGROUP COMMUNICATIONS  //  PRESS OFFICE", "TERRAGROUP  //  BRAND AND REPUTATION", "PUBLIC RELATIONS  //  MEDIA DESK" },
                Grants = new[] { "MESSAGE APPROVED", "ON BRAND" },
                Chatter = new[] { "PRESS RELEASE #{n}  DRAFTED", "SENTIMENT SCORE  {pct}", "JOURNALIST CALLS  {n} DECLINED", "TALKING POINTS  DISTRIBUTED", "SOCIAL POST  SCHEDULED", "NO COMMENT  ISSUED", "BRAND AUDIT  PASSED" },
                Headlines = new[] { "TERRAGROUP WINS AWARD FOR TRANSPARENCY", "'WE CARE ABOUT NORVINSK', SAYS SPOKESPERSON" }
            },
            new Theme(ThemeChoice.NightShift, "020309", "141E46", "4652A0", "343E7A", "9AA6E6", "D6DCF6", "5E6694", "FFAA40", "FF4A7A")
            {
                DayWeight = 0, NightWeight = 12,
                Backdrops = new[] { BackdropKind.Plain, BackdropKind.Dots }, Frames = new[] { FrameKind.Minimal, FrameKind.Orbit },
                Intros = new[] { IntroKind.Fade }, Bars = new[] { BarKind.Thin }, Outros = new[] { OutroKind.Sting },
                Division = "NIGHT SHIFT", TitleCard = "AFTER HOURS ACCESS", LogTag = "OK",
                SlowNote = "THE BUILDING IS QUIET",
                Headers = new[] { "TERRAGROUP  //  AFTER HOURS", "NIGHT SHIFT  //  SKELETON CREW" },
                Grants = new[] { "WORKING LATE, OPERATOR?", "AFTER HOURS ACCESS GRANTED" },
                Chatter = new[] { "LIGHTS  FLOOR {n}  OFF", "CLEANING CREW  ON SITE", "SECURITY ROUND  {n}  COMPLETE", "OVERTIME  LOGGED", "VENDING MACHINE  RESTOCKED", "PARKING LOT  {n} VEHICLES" },
                Headlines = new[] { "OVERNIGHT: NOTHING TO REPORT", "NIGHT SHIFT ALLOWANCE UNDER REVIEW" }
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
                Headlines = new[] { "HOLIDAY PARTY CANCELLED. ATTENDANCE REMAINS MANDATORY", "YEAR-END BONUSES PAID IN COMPANY STOCK" }
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

        /// <summary>A memo slides in once progress passes this, or never if negative.</summary>
        internal float MemoAt = -1f;
        internal string MemoFrom;
        internal string MemoBody;

        private static readonly string[] CommonHeadlines =
        {
            "TERRAGROUP REPORTS RECORD QUARTER",
            "NORVINSK SPECIAL ECONOMIC ZONE EXPANDS",
            "TERRAGROUP DENIES ALL ALLEGATIONS",
            "HR: BADGES MUST BE VISIBLE AT ALL TIMES",
            "CAFETERIA B CLOSED FOR SANITATION",
            "PORT AUTHORITY REOPENING DATE TBD",
            "UNTAR CHECKPOINTS NOW OPERATIONAL",
            "SPOKESPERSON: 'NOTHING HAPPENED IN TARKOV'",
            "PARKING LOT C REASSIGNED TO SECURITY CONTRACTORS"
        };

        private static readonly string[] Stocks = { "TGRP", "TG-LABS", "NVSK IDX", "BTC", "RUB/USD", "USEC LTD", "TG-PHARMA" };

        private static readonly string[] Memos =
        {
            "THE EAST WING IS CLOSED UNTIL FURTHER NOTICE. PLEASE DO NOT ASK WHY.",
            "UNAUTHORIZED SAMPLES MAY NOT LEAVE THE LABORATORY. THIS INCLUDES LUNCH.",
            "THE EVACUATION PLAN HAS BEEN UPDATED. PLEASE DO NOT USE THE OLD ONE.",
            "PAYROLL IS DELAYED DUE TO CIRCUMSTANCES. THANK YOU FOR YOUR LOYALTY.",
            "LOST: ONE BLUE KEYCARD. IF FOUND, DO NOT USE IT.",
            "THE PORT IS CLOSED. THE PORT WILL REOPEN SOON. THE PORT IS FINE.",
            "PLEASE STOP FEEDING THE SCAVS.",
            "TEAM-BUILDING EXERCISE MOVED TO RESERVE BASE. BRING ARMOR.",
            "A REMINDER THAT 'PASSWORD' IS NOT AN ACCEPTABLE PASSWORD.",
            "EMPLOYEE OF THE MONTH: {nick}. CONGRATULATIONS.",
            "COFFEE MACHINE ON FLOOR 3 IS FOR EXECUTIVES. YOU KNOW WHO YOU ARE."
        };

        private static readonly string[] MemoSenders = { "HUMAN RESOURCES", "FACILITIES", "LEGAL", "OFFICE OF THE CEO", "IT DEPARTMENT", "COMPLIANCE" };

        /// <param name="preview">A preview always shows a memo and is more likely to fake a denial.</param>
        internal static Variant Roll(System.Random random, ThemeChoice choice, GlitchLevel glitches, int launches, Pins pins, bool preview = false)
        {
            var v = new Variant { Glitches = glitches };
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
                bool night = now.Hour >= 21 || now.Hour < 6;
                float total = 0f;
                foreach (Theme theme in Theme.All)
                {
                    total += night ? theme.NightWeight : theme.DayWeight;
                }
                float pick = (float)random.NextDouble() * total;
                v.Theme = Theme.All[0];
                foreach (Theme theme in Theme.All)
                {
                    float weight = night ? theme.NightWeight : theme.DayWeight;
                    if (weight > 0f && (pick -= weight) < 0f)
                    {
                        v.Theme = theme;
                        break;
                    }
                }
            }
            Theme t = v.Theme;
            v.Backdrop = t.Backdrops[random.Next(t.Backdrops.Length)];
            v.Frame = t.Frames[random.Next(t.Frames.Length)];
            v.Intro = t.Intros[random.Next(t.Intros.Length)];
            v.Bar = t.Bars[random.Next(t.Bars.Length)];
            v.Outro = t.Outros[random.Next(t.Outros.Length)];
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

            if (preview || random.Next(4) == 0)
            {
                v.MemoAt = Range(random, 0.25f, 0.65f);
                v.MemoFrom = MemoSenders[random.Next(MemoSenders.Length)];
                v.MemoBody = t.Id == ThemeChoice.Holiday && random.Next(2) == 0
                    ? "THE HOLIDAY PARTY IS CANCELLED. ATTENDANCE REMAINS MANDATORY."
                    : Memos[random.Next(Memos.Length)];
            }
            return v;
        }

        /// <summary>One loop of the news ticker: share prices between headlines.</summary>
        private static string BuildTicker(System.Random random, Theme theme)
        {
            var headlines = new List<string>(theme.Headlines);
            headlines.AddRange(CommonHeadlines);
            for (int i = headlines.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (headlines[i], headlines[j]) = (headlines[j], headlines[i]);
            }

            var builder = new StringBuilder();
            int stocksPerHeadline = theme.Id == ThemeChoice.Finance ? 3 : 1;
            int stock = random.Next(Stocks.Length);
            for (int h = 0; h < 6; h++)
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
                builder.Append(headlines[h % headlines.Count]).Append("     |     ");
            }
            return builder.ToString();
        }

        private static LayoutKind PickLayout(System.Random random)
        {
            int roll = random.Next(100);
            if (roll < 28) return LayoutKind.Centered;
            if (roll < 46) return LayoutKind.Split;
            if (roll < 64) return LayoutKind.SplitMirror;
            if (roll < 82) return LayoutKind.Sidebar;
            return LayoutKind.Dock;
        }

        private static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        private static float Sign(System.Random random) => random.Next(2) == 0 ? -1f : 1f;
    }

    /// <summary>
    /// Made-up office chatter for the line under the log: the division's own lines and shared
    /// ones. Never mixed into the real steps, so the log and its times stay true. Drawn from a
    /// shuffled bag, so a line does not repeat until the rest have shown.
    /// </summary>
    internal sealed class Chatter
    {
        private static readonly string[] Common =
        {
            "SYNCING PAYROLL LEDGER",
            "COMPLIANCE AUDIT Q4  PASSED",
            "NDA ON FILE  ACKNOWLEDGED",
            "BADGE 0x{hex}  VERIFIED",
            "LOADING {mods} APPROVED VENDOR PLUGINS",
            "SHAREHOLDER BRIEFING  CACHED",
            "EXPENSE REPORT #{n}  REJECTED",
            "TIMESHEET  {n} HOURS LOGGED",
            "HR TICKET #{n}  CLOSED WITHOUT ACTION",
            "QUARTERLY TARGETS  {pct}% MET",
            "VPN  NORVINSK OFFICE  CONNECTED",
            "PRINTER 3B  STILL OFFLINE",
            "SECURE MAIL  {n} UNREAD",
            "LEGAL HOLD  APPLIED"
        };

        private readonly System.Random _random;
        private readonly string[] _lines;
        private readonly int[] _order;
        private int _next;

        internal Chatter(System.Random random, Theme theme)
        {
            _random = random;
            var lines = new List<string>(theme.Chatter);
            // The division's own lines twice, so they come up more than the shared ones.
            lines.AddRange(theme.Chatter);
            lines.AddRange(Common);
            _lines = lines.ToArray();
            _order = new int[_lines.Length];
            for (int i = 0; i < _order.Length; i++)
            {
                _order[i] = i;
            }
            _next = _order.Length;
        }

        internal string Next()
        {
            if (_next >= _order.Length)
            {
                for (int i = _order.Length - 1; i > 0; i--)
                {
                    int j = _random.Next(i + 1);
                    (_order[i], _order[j]) = (_order[j], _order[i]);
                }
                _next = 0;
            }
            string line = _lines[_order[_next++]];
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
