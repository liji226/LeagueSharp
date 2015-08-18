﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using ShineCommon;
using ShineCommon.Maths;
using SharpDX;

namespace ShineSharp.Champions
{
    public class Diana : BaseChamp
    {
        private Obj_AI_Hero m_target = null;
        private int m_misaya_start_tick = 0, m_moon_start_tick = 0;
        private bool m_moon_r_casted = false;
        public Diana()
            : base ("Diana")
        {
            
        }

        public override void CreateConfigMenu()
        {
            combo = new Menu("Combo", "Combo");
            combo.AddItem(new MenuItem("CUSEQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("CUSEW", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("CUSEE", "Use E").SetValue(true));
            //
            ult = new Menu("R Settings", "rsettings");
            ult.AddItem(new MenuItem("CUSER", "Use R").SetValue(true));
            ult.AddItem(new MenuItem("CUSERMETHOD", "R Method").SetValue<StringList>(new StringList(new string[] { "Use Smart R", "Use Only R To Moonlight Debuffed", "Use R Always" }, 0)));
            //
            combo.AddSubMenu(ult);

            harass = new Menu("Harass", "Harass");
            harass.AddItem(new MenuItem("HUSEQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("HUSEW", "Use W").SetValue(true));
            harass.AddItem(new MenuItem("HUSEE", "Use E").SetValue(false));
            harass.AddItem(new MenuItem("HUSER", "Use R If Moonlight Debuffed").SetValue(true));
            harass.AddItem(new MenuItem("HMANA", "Min. Mana Percent").SetValue(new Slider(50, 100, 0)));

            laneclear = new Menu("LaneClear/JungleClear", "LaneClear");
            laneclear.AddItem(new MenuItem("LUSEQ", "Use Q").SetValue(true));
            laneclear.AddItem(new MenuItem("LUSEW", "Use W").SetValue(true));
            laneclear.AddItem(new MenuItem("LMANA", "Min. Mana Percent").SetValue(new Slider(50, 100, 0)));

            misc = new Menu("Misc", "Misc");
            misc.AddItem(new MenuItem("MMISAYA", "Misaya Combo Key").SetValue<KeyBind>(new KeyBind('T', KeyBindType.Press)))
                    .ValueChanged += (s, ar) =>
                    {
                        if (!ar.GetNewValue<KeyBind>().Active)
                        {
                            m_target = null;
                            m_misaya_start_tick = 0;
                        }
                    };
            misc.AddItem(new MenuItem("MMOON", "Moon Combo Key").SetValue<KeyBind>(new KeyBind('G', KeyBindType.Press)))
                    .ValueChanged += (s, ar) =>
                    {
                        if (!ar.GetNewValue<KeyBind>().Active)
                        {
                            m_target = null;
                            m_moon_start_tick = 0;
                            m_moon_r_casted = false;
                        }
                    };

            misc.AddItem(new MenuItem("MINTERRUPTE", "Use E For Interrupt").SetValue(true));
            misc.AddItem(new MenuItem("MINTERRUPTRE", "Use R->E For Interrupt Important Spells").SetValue(true));
            misc.AddItem(new MenuItem("MGAPCLOSEW", "Use W For Gapcloser").SetValue(true));

            Config.AddSubMenu(combo);
            Config.AddSubMenu(harass);
            Config.AddSubMenu(laneclear);
            Config.AddSubMenu(misc);
            Config.AddToMainMenu();

            BeforeOrbWalking += BeforeOrbwalk;
            BeforeDrawing += BeforeDraw;
            OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.Combo] += Combo;
            OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.Mixed] += Harass;
            OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.LaneClear] += LaneClear;
        }

        public override void SetSpells()
        {
            Spells[Q] = new Spell(SpellSlot.Q, 830f);
            Spells[Q].SetSkillshot(0.5f, 195f, 1600f, false, SkillshotType.SkillshotLine);

            Spells[W] = new Spell(SpellSlot.W, 200f);

            Spells[E] = new Spell(SpellSlot.E, 350f);

            Spells[R] = new Spell(SpellSlot.R, 825f);
        }

        public void BeforeOrbwalk()
        {
            if (Config.Item("MMISAYA").GetValue<KeyBind>().Active)
                Misaya();
            else if (Config.Item("MMOON").GetValue<KeyBind>().Active)
                Moon();
        }

        //r q w r e
        public void Misaya()
        {
            if (m_target == null && ComboReady())
                m_target = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Magical);

            if (m_target != null)
            {
                Orbwalking.Orbwalk(m_target, Game.CursorPos);
                if (m_target.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <= 810f)
                {
                    if (m_misaya_start_tick == 0) //begin combo
                    {
                        m_misaya_start_tick = Environment.TickCount;
                        Spells[R].CastOnUnit(m_target);
                    }
                }

                if (m_misaya_start_tick != 0)
                {
                    Spells[Q].Cast(m_target.ServerPosition);
                    if (!m_target.IsDead)
                        Spells[W].Cast();
                    if (!m_target.IsDead)
                        Spells[R].CastOnUnit(m_target);
                    if (!m_target.IsDead)
                        Spells[E].Cast();

                    m_misaya_start_tick = 0;
                }
            }
            else
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
        }

        public void Moon()
        {
            if (m_target == null && ComboReady())
                m_target = TargetSelector.GetTarget(2000, TargetSelector.DamageType.Magical);

            if (m_target != null)
            {
                Orbwalking.Orbwalk(m_target, Game.CursorPos);
                var minion = MinionManager.GetMinions(Spells[R].Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None).Where(p => p.Health < Spells[Q].GetDamage(p) && p.HasBuff("dianamoonlight")).OrderByDescending(q => q.ServerPosition.Distance(ObjectManager.Player.ServerPosition)).FirstOrDefault();
                if (minion == null)
                {
                    minion = MinionManager.GetMinions(Spells[Q].Range - 20, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None).Where(p => p.Health > Spells[Q].GetDamage(p)).OrderByDescending(q => q.ServerPosition.Distance(ObjectManager.Player.ServerPosition)).FirstOrDefault();
                    if (minion != null && !m_moon_r_casted)
                    {
                        Spells[Q].Cast(minion.ServerPosition);
                        m_moon_start_tick = Environment.TickCount;
                        m_moon_r_casted = false;
                    }
                }

                if (minion != null)
                {
                    if (minion.ServerPosition.Distance(m_target.ServerPosition) < Spells[E].Range - 10 && Environment.TickCount - m_moon_start_tick > 500)
                    {
                        if (Spells[E].IsReady() && !m_moon_r_casted && Spells[R].IsInRange(minion)) //because r->e combo 
                        {
                            Spells[R].CastOnUnit(minion);
                            m_moon_r_casted = true;
                        }
                    }
                }


                if (m_moon_r_casted)
                {
                    if (Spells[E].IsReady() && !m_target.IsDead)
                        Spells[E].Cast();
                    if (Spells[W].IsReady() && !m_target.IsDead)
                        Spells[W].Cast();

                    if (Spells[Q].IsReady() && !m_target.IsDead)
                    {
                        HitChance hc;
                        Vector2 pos = ShineCommon.Maths.Prediction.GetArcPrediction(m_target, Spells[Q], m_target.GetWaypoints(), m_target.AvgMovChangeTime() + 100, m_target.LastMovChangeTime(), out hc);
                        if (hc >= HitChance.High)
                            Spells[Q].Cast();
                    }

                    if (Spells[R].IsReady() && !m_target.IsDead && HasMoonligh(m_target))
                        Spells[R].Cast();
                    m_moon_start_tick = 0;
                }
            }
            else
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

        }

        public void Combo()
        {
            if (m_target != null && m_target.IsDead)
                m_target = null;

            if (Spells[Q].IsReady() && Config.Item("CUSEQ").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(Spells[Q].Range, TargetSelector.DamageType.Magical);
                if (t != null)
                {
                    HitChance hc;
                    Vector2 pos = ShineCommon.Maths.Prediction.GetArcPrediction(t, Spells[Q], t.GetWaypoints(), t.AvgMovChangeTime(), t.LastMovChangeTime(), out hc);
                    if (hc >= HitChance.VeryHigh)
                        Spells[Q].Cast(pos);
                }
            }

            if (m_target == null)
                m_target = HeroManager.Enemies.Where(p => HasMoonligh(p) && p.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <= 900).OrderByDescending(q => TargetSelector.GetPriority(q)).FirstOrDefault();

            if (Config.Item("CUSER").GetValue<bool>())
            {
                if (m_target != null && Config.Item("CUSERMETHOD").GetValue<StringList>().SelectedIndex != 2 && Spells[R].IsReady())
                {
                    if (Spells[R].IsInRange(m_target))
                    {
                        Spells[R].CastOnUnit(m_target);
                        if (!m_target.IsDead && Spells[W].IsReady() && Config.Item("CUSEW").GetValue<bool>()) //overkill check
                            Spells[W].Cast();

                        if (!m_target.IsDead) //overkill check
                        {
                            if (Config.Item("CUSERMETHOD").GetValue<StringList>().SelectedIndex != 1)
                            {
                                if (Spells[R].IsReady())
                                {
                                    Spells[R].CastOnUnit(m_target);
                                    m_target = null;
                                }
                            }
                            if(Spells[E].IsReady() && Config.Item("CUSEE").GetValue<bool>())
                                Spells[E].Cast();
                        }
                        else
                            m_target = null;
                        return;
                    }
                    m_target = null;
                }

                if (m_target == null && (Config.Item("CUSERMETHOD").GetValue<StringList>().SelectedIndex == 2 || Config.Item("CUSERMETHOD").GetValue<StringList>().SelectedIndex == 0))
                {
                    var t = TargetSelector.GetTarget(Spells[R].Range, TargetSelector.DamageType.Magical);
                    if (t != null)
                    {
                        if (Config.Item("CUSERMETHOD").GetValue<StringList>().SelectedIndex == 2 || (Config.Item("CUSERMETHOD").GetValue<StringList>().SelectedIndex == 0 && CalculateDamageR(t) >= t.Health))
                        {
                            m_target = t;
                            Spells[R].CastOnUnit(t);
                        }
                    }
                }
            }

            {
                var t = m_target == null ? TargetSelector.GetTarget(Spells[E].Range, TargetSelector.DamageType.Magical) : m_target;
                if (t != null)
                {
                    if (!t.IsDead)
                    {
                        if (Spells[W].IsReady() && !Spells[E].IsReady() && Spells[W].IsInRange(t) && Config.Item("CUSEW").GetValue<bool>())
                            Spells[W].Cast();

                        if (!t.IsDead && Spells[E].IsReady() && Spells[W].IsReady() && Spells[E].IsInRange(t))
                        {
                            if (Config.Item("CUSEW").GetValue<bool>())
                                Spells[W].Cast();
                            if (Config.Item("CUSEE").GetValue<bool>())
                                Spells[E].Cast();
                        }
                    }
                    m_target = null;
                }
            }
        }

        public void Harass()
        {
            if (ObjectManager.Player.ManaPercent < Config.Item("HMANA").GetValue<Slider>().Value)
                return;

            if (Spells[Q].IsReady() && Config.Item("HUSEQ").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(Spells[Q].Range, TargetSelector.DamageType.Magical);
                if (t != null)
                {
                    HitChance hc;
                    Vector2 pos = ShineCommon.Maths.Prediction.GetArcPrediction(t, Spells[Q], t.GetWaypoints(), t.AvgMovChangeTime() + 100, t.LastMovChangeTime(), out hc);
                    if (hc >= HitChance.VeryHigh)
                        Spells[Q].Cast(pos);
                }
            }

            if (m_target == null)
                m_target = HeroManager.Enemies.Where(p => HasMoonligh(p) && p.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <= 900).OrderByDescending(q => TargetSelector.GetPriority(q)).FirstOrDefault();

            if (Config.Item("HUSER").GetValue<bool>())
            {
                if (m_target != null)
                {
                    if (m_target.ServerPosition.CountEnemiesInRange(600) == 1 && Spells[R].IsInRange(m_target) && HasMoonligh(m_target))
                    {
                        Spells[R].CastOnUnit(m_target);

                        if (!m_target.IsDead && Spells[W].IsReady() && Config.Item("HUSEW").GetValue<bool>()) //overkill check
                            Spells[W].Cast();

                        if (!m_target.IsDead && Spells[E].IsReady() && Config.Item("HUSEE").GetValue<bool>()) //overkill check
                            Spells[E].Cast();

                        m_target = null;
                        return;
                    }
                    m_target = null;
                }
            }

            if (m_target == null)
            {
                var t = TargetSelector.GetTarget(Spells[E].Range, TargetSelector.DamageType.Magical);
                if (t != null)
                {
                    if (Spells[W].IsReady() && !Spells[E].IsReady() && Spells[W].IsInRange(t) && Config.Item("HUSEW").GetValue<bool>())
                        Spells[W].Cast();

                    if (!t.IsDead && Spells[E].IsReady() && Spells[W].IsReady() && Spells[E].IsInRange(t))
                    {
                        if (Config.Item("HUSEW").GetValue<bool>())
                            Spells[W].Cast();
                        if (Config.Item("HUSEE").GetValue<bool>())
                            Spells[E].Cast();
                    }
                }
            }
        }

        public void LaneClear()
        {
            if (ObjectManager.Player.ManaPercent < Config.Item("LMANA").GetValue<Slider>().Value)
                return;

            if (Spells[Q].IsReady() && Config.Item("LUSEQ").GetValue<bool>())
            {
                var farm = MinionManager.GetBestCircularFarmLocation(MinionManager.GetMinions(Spells[Q].Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None).Select(p => p.ServerPosition.To2D()).ToList(), Spells[Q].Width, Spells[Q].Range);
                if (farm.MinionsHit > 0)
                    Spells[Q].Cast(farm.Position);

                var jungle_minion = MinionManager.GetMinions(Spells[Q].Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();
                if (jungle_minion != null)
                    Spells[Q].Cast(jungle_minion.ServerPosition);
            }

            if (Spells[W].IsReady() && Config.Item("LUSEW").GetValue<bool>())
            {
                if (MinionManager.GetMinions(Spells[W].Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None).Count >= 3)
                    Spells[W].Cast();

                var jungle_minion = MinionManager.GetMinions(Spells[W].Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();
                if (jungle_minion != null)
                    Spells[W].Cast();
            }
        }

        public override void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("MGAPCLOSEW").GetValue<bool>() && gapcloser.End.Distance(ObjectManager.Player.ServerPosition) <= 300)
                if (Spells[W].IsReady())
                    Spells[W].Cast();
        }

        public override void Interrupter_OnPossibleToInterrupt(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Config.Item("MINTERRUPTE").GetValue<bool>() && Spells[E].IsInRange(sender))
                Spells[E].Cast();

            if (Config.Item("MINTERRUPTRE").GetValue<bool>() && Spells[R].IsInRange(sender) && sender.IsChannelingImportantSpell())
            {
                if (Spells[R].IsReady() && Spells[E].IsReady())
                {
                    Spells[R].CastOnUnit(sender);
                    Spells[E].Cast();
                }
            }
        }

        public void BeforeDraw()
        {
            if (m_target != null)
            {
                if (Config.Item("MMISAYA").GetValue<KeyBind>().Active)
                    Text.DrawText(null, "Misaya Combo Target", (int)(m_target.HPBarPosition.X + m_target.BoundingRadius / 2 - 10), (int)(m_target.HPBarPosition.Y - 20), SharpDX.Color.Yellow);
                else if(Config.Item("MMOON").GetValue<KeyBind>().Active)
                    Text.DrawText(null, "Moon Combo Target", (int)(m_target.HPBarPosition.X + m_target.BoundingRadius / 2 - 10), (int)(m_target.HPBarPosition.Y - 20), SharpDX.Color.Yellow);
            }

            foreach (var enemy in HeroManager.Enemies)
            {
                if (enemy.Health < CalculateComboDamage(enemy) + (Spells[R].IsReady() ? CalculateDamageR(enemy) : 0))
                {
                    var killable_pos = Drawing.WorldToScreen(enemy.Position);
                    Drawing.DrawText((int)killable_pos.X - 20, (int)killable_pos.Y + 35, System.Drawing.Color.Red, "Killable");
                }
            }
        }

        public bool HasMoonligh(Obj_AI_Hero t)
        {
            return t.HasBuff("dianamoonlight");
        }
    }
}