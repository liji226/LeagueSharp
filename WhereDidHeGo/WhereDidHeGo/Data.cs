﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace WhereDidHeGo
{
    public class Data
    {
        public static List<Tuple<int, Vector3>> StealthPoses = new List<Tuple<int, Vector3>>();
        public static List<Vector3> PingPoses = new List<Vector3>();
        public static List<Tuple<float, List<Vector2>>> StealthPaths = new List<Tuple<float, List<Vector2>>>();
        public static List<Tuple<int, string>> StealthSpells = new List<Tuple<int, string>>();
        public static List<_sdata> AntiStealthSpells = new List<_sdata>();
        public static Menu Config;

        public static void Initialize()
        {
            #region Spell Databases
            StealthSpells.Add(new Tuple<int, string>(3, "akalismokebomb"));
            StealthSpells.Add(new Tuple<int, string>(1, "khazixr"));
            StealthSpells.Add(new Tuple<int, string>(1, "khazixrlong"));
            StealthSpells.Add(new Tuple<int, string>(3, "talonshadowassault"));
            StealthSpells.Add(new Tuple<int, string>(1, "monkeykingdecoy"));
            StealthSpells.Add(new Tuple<int, string>(1, "hideinshadows"));

            AntiStealthSpells.Add(new _sdata { ChampionName = "caitlyn", Spell = new Spell(SpellSlot.W, 800), StealthDetectionLevel = 1 });
            AntiStealthSpells.Add(new _sdata { ChampionName = "kogmaw", Spell = new Spell(SpellSlot.R, 1200), StealthDetectionLevel = 1 }); //range + level * 300
            AntiStealthSpells.Add(new _sdata { ChampionName = "leesin", Spell = new Spell(SpellSlot.Q, 1100), StealthDetectionLevel = 1 });
            AntiStealthSpells.Add(new _sdata { ChampionName = "nidalee", Spell = new Spell(SpellSlot.W, 900), StealthDetectionLevel = 1 });
            AntiStealthSpells.Add(new _sdata { ChampionName = "nocturne", Spell = new Spell(SpellSlot.Q, 1200), StealthDetectionLevel = 1 });
            AntiStealthSpells.Add(new _sdata { ChampionName = "twistedfate", Spell = new Spell(SpellSlot.R, 4000), StealthDetectionLevel = 3, SelfCast = true });
            AntiStealthSpells.Add(new _sdata { ChampionName = "fizz", Spell = new Spell(SpellSlot.W, 1275), StealthDetectionLevel = 2 });
            #endregion
            #region Config Menu
            Config = new Menu("Where Did He Go", "wheredidhego", true);
            Config.AddItem(new MenuItem("DRAWCIRCLE", "Show Where did enemy go").SetValue(true));
            Config.AddItem(new MenuItem("PINGMODE", "Ping Enemy Missing").SetValue(new StringList(new string[] { "Only local", "Send to team", "Disabled" }, 0)));
            Config.AddItem(new MenuItem("PINGCOUNT", "Ping X Times").SetValue(new Slider(1, 1, 100)));
            Config.AddItem(new MenuItem("DRAWWAPOINTS", "Draw Enemy's Last Waypoint").SetValue(true));
            Config.AddItem(new MenuItem("ENABLEWDHG", "Enabled").SetValue(true));

            Menu antiStealth = new Menu("Anti-Stealth", "wdhgantistealth");
            antiStealth.AddItem(new MenuItem("USEVISIONWARD", "Use Vision Ward").SetValue(true));
            antiStealth.AddItem(new MenuItem("USEORACLESLENS", "Use Oracle's Lens").SetValue(true));
            Config.AddSubMenu(antiStealth);

            var spells = AntiStealthSpells.Where(p => p.ChampionName == ObjectManager.Player.ChampionName.ToLower());
            if (spells != null)
            {
                Menu antiStealthSpells = new Menu("Reveal Spells", "wdhgrevealspells");
                foreach (var spell in spells)
                {
                    Menu antiStealthSpell = new Menu(String.Format("{0} ({1})", ObjectManager.Player.Spellbook.GetSpell(spell.Spell.Slot).Name, spell.Spell.Slot), "wdhg" + spell.Spell.Slot.ToString());
                    antiStealthSpell.AddItem(new MenuItem(String.Format("DETECT{0}", spell.Spell.Slot.ToString()), "Detection Level").SetValue(new Slider(spell.StealthDetectionLevel, 1, 3)));
                    antiStealthSpell.AddItem(new MenuItem(String.Format("USE{0}", spell.Spell.Slot.ToString()), "Enabled").SetValue(true));
                    antiStealthSpells.AddSubMenu(antiStealthSpell);
                }
                Config.AddSubMenu(antiStealthSpells);
            }

            Config.AddToMainMenu();
            #endregion
        }

        public struct _sdata
        {
            public string ChampionName;
            public Spell Spell;
            public int StealthDetectionLevel;
            public bool SelfCast;
        }
    }
}