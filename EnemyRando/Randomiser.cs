using System;
using System.Collections;
using System.Linq;
using Architect.Api;
using Architect.Behaviour.Fixers;
using Architect.Behaviour.Utility;
using Architect.Content.Preloads;
using Architect.Objects.Groups;
using Architect.Utils;
using GlobalEnums;
using GlobalSettings;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace EnemyRando;

public static class Randomiser
{
    private const string ON_RANDO_DEATH = "OnRandoDeath";
    
    public static void Init()
    {
        typeof(SendMessage).Hook(nameof(SendMessage.OnEnter),
            (Action<SendMessage> orig, SendMessage self) =>
            {
                if (self.functionCall.FunctionName.Contains("FreezeMoment") &&
                    self.fsmComponent.GetComponent<BlockAudio>())
                {
                    self.Finish();
                }
                else orig(self);
            });
        
        typeof(CreateObject).Hook(nameof(CreateObject.OnEnter),
            (Action<CreateObject> orig, CreateObject self) =>
            {
                var go = self.gameObject.value;
                if (go && go.GetComponent<FreezeMomentOnEnable>() &&
                    self.fsmComponent.GetComponent<BlockAudio>()) self.Finish();
                else orig(self);
            });
        
        typeof(ApplyMusicCue).Hook(nameof(ApplyMusicCue.OnEnter),
            (Action<ApplyMusicCue> orig, ApplyMusicCue self) =>
            {
                if (self.fsmComponent.GetComponent<BlockAudio>())
                {
                    self.Finish();
                } else orig(self);
            });
        
        typeof(TransitionToAudioSnapshot).Hook(nameof(TransitionToAudioSnapshot.OnEnter),
            (Action<TransitionToAudioSnapshot> orig, TransitionToAudioSnapshot self) =>
            {
                if (self.fsmComponent.GetComponent<BlockAudio>())
                {
                    self.Finish();
                } else orig(self);
            });
        
        typeof(StartRoarEmitter).Hook(nameof(StartRoarEmitter.OnEnter),
            (Action<StartRoarEmitter> _, StartRoarEmitter self) =>
            {
                self.Finish();
            });
        
        typeof(HealthManager).Hook(nameof(HealthManager.OnEnable),
            (Action<HealthManager> orig, HealthManager healthManager) =>
            {
                orig(healthManager);
                
                if (healthManager.isDead) return;
                if (!PreloadManager.HasPreloaded) return;
                if (healthManager.GetComponent<ReplacementEnemy>()) return;
                if (healthManager.GetComponent<ReplacedEnemy>()) return;

                if (healthManager.transform.parent)
                {
                    if (healthManager.transform.parent.name.Contains("Intro Minions")) return;
                }

                if (healthManager.name.Contains("Music Box Bell")) return;

                var type = EnemyChooser.GetEnemyType(healthManager).GetRandoType();
                if (type == Settings.RandoType.Disabled) return;

                EnemyRandoPlugin.Instance.StartCoroutine(Rando(healthManager, type));
            });

        EventHooks.OnEvent += (o, s) =>
        {
            if (s != ON_RANDO_DEATH) return;

            var rando = o.GetComponent<ReplacementEnemy>();
            if (!rando) return;

            if (!rando.target) return;

            var replaced = rando.target.gameObject.GetComponent<ReplacedEnemy>();
            if (!replaced) return;
            replaced.replacements.Remove(rando);
            if (replaced.replacements.Count == 0)
            {
                foreach (var dh in replaced.GetComponentsInChildren<DamageHero>()) dh.enabled = false;
                if (!rando.target.hasSpecialDeath || rando.target.name.Contains("Spine Floater")) 
                    rando.target.transform.position = rando.transform.position;
                Object.Destroy(replaced);
                rando.target.gameObject.SetActive(true);
                EnemyRandoPlugin.Instance.StartCoroutine(ZeroHp(rando));
            }
        };
    }

    private static IEnumerator ZeroHp(ReplacementEnemy rando)
    {
        yield return new WaitForSeconds(0.1f);
        if (!rando.target) yield break;

        var fsms = rando.target.GetComponents<PlayMakerFSM>();
        var die = true;
        var canRerando = true;
        
        if (rando.target.name.Contains("Flower Queen")) rando.target.hasSpecialDeath = false;
        
        if (rando.target.name.Contains("Dock Guard"))
        {
            rando.target.gameObject.LocateMyFSM("Control").SendEvent("ZERO HP");
            yield break;
        }
        
        foreach (var fsm in fsms)
        {
            if (fsm.FsmGlobalTransitions.FirstOrDefault(ev => ev.EventName == "ZERO HP") != null)
            {
                fsm.SendEvent("ZERO HP");
                if (rando.target.hasSpecialDeath) die = false;
            }
            
            if (fsm.name == "Conductor Boss")
            {
                fsm.SendEvent("DIE");
            }

            if (fsm.FsmGlobalTransitions.FirstOrDefault(ev => ev.EventName == "FINAL BLOCK") != null)
            {
                fsm.SendEvent("FINAL BLOCK");
                var fail = fsm.GetState("Fail Hit");
                if (fail.actions.Length == 15) fail.AddAction(() => { fsm.gameObject.transform.Translate(0, 4, 0); });
                die = false;
            }

            var cc = fsm.FsmVariables.FindFsmGameObject("Centipede Control");
            cc?.Value.GetComponent<PlayMakerFSM>().SetState("Bellbeast Jumps In");

            if (rando.target.name.Contains("Spine Floater"))
            {
                var idle = fsm.GetState("Idle");
                idle?.AddAction(() => fsm.SendEvent("FINAL"), 0, true);
                die = false;
                canRerando = false;
            }

            if (fsm.name.Contains("Driller ") && fsm.FsmName.Equals("Death Checker"))
            {
                fsm.SetState("Notify Parent");
                var par = rando.target.transform.parent;

                if (par)
                {
                    foreach (var rep in par.GetComponentsInChildren<ReplacedEnemy>(true))
                    {
                        foreach (var replacement in rep.replacements.Where(replacement => replacement))
                        {
                            replacement.gameObject.SetActive(false);
                        }
                    }

                    par.gameObject.SetActive(false);
                }

                canRerando = false;

                rando.target.transform.parent = null;
                rando.target.hasSpecialDeath = false;
            }
        }

        if (rando.target.name.Contains("Lost Lace"))
        {
            rando.target.hasSpecialDeath = false;
        }
        
        var b3 = rando.target.name.Contains("Conductor");
        if (rando.target.name.Contains("Slasher") || rando.target.name.Contains("Slammer") || b3)
        {
            var bs = GameObject.Find("Boss Scene");
            if (bs)
            {
                rando.target.gameObject.SetActive(false);
                rando.target.transform.parent = bs.transform.Find("Husks");
                
                var ctrl = bs.LocateMyFSM("Control");
                if (ctrl)
                {
                    if (b3)
                    {
                        ctrl.SetState("Death Explode");
                    } else ctrl.SendEvent("HUSK KILLED");
                }

                canRerando = false;
                die = false;
            }
        }

        if (die) rando.target.Die(0, AttackTypes.Generic, true);

        var phaseControl = rando.target.gameObject.LocateMyFSM("Phase Control");
        if (phaseControl) phaseControl.SetState("Death Hit");

        if (canRerando) EnemyRandoPlugin.Instance.StartCoroutine(WaitForRando(rando.target, rando.randoType));
        
        if (rando && rando.gameObject) Object.Destroy(rando.gameObject);
    }

    private static IEnumerator WaitForRando(HealthManager healthManager, Settings.RandoType type)
    {
        yield return new WaitUntil(() => !healthManager || !healthManager.isDead);
        yield return Rando(healthManager, type);
    }

    private static readonly int EnemiesLayer = LayerMask.NameToLayer("Enemies");
    private static readonly int InteractiveLayer = LayerMask.NameToLayer("Interactive Object");
    
    private static IEnumerator Rando(HealthManager healthManager, Settings.RandoType type)
    {
        yield return new WaitForSeconds(0.02f);
        if (!healthManager) yield break;
        var fsm = healthManager.GetComponent<PlayMakerFSM>();
        var arena = (bool)healthManager.GetComponentInParent<BattleWave>() || 
                    (healthManager.transform.parent && healthManager.transform.parent.name.Contains("Key Control"));
        if (healthManager.hp > 100000) yield break;
        healthManager.hp = 10000;
        if (arena) yield return new WaitForSeconds(0.75f);
        yield return new WaitUntil(() =>
        {
            // If dead then cancel
            if (!healthManager || healthManager.isDead) return true;
            
            // Ensure interactive
            var interactive = healthManager.gameObject.layer == InteractiveLayer;
            if (healthManager.gameObject.layer != EnemiesLayer && !interactive) return false;
            
            // Wait until can take damage
            if (healthManager is { IsInvincible: true, InvincibleFromDirection: 0 }) return false;
            
            // Wait until visible
            var renderer = healthManager.GetComponent<Renderer>();
            if (renderer && !renderer.enabled) return false;

            if (fsm) {
                // Ensure FSM is ready
                if (fsm.ActiveStateName.Contains("Intro") || 
                    fsm.ActiveStateName.Contains("Zoom") ||
                    fsm.ActiveStateName.Contains("Roar") ||
                    fsm.ActiveStateName.Contains("Drop") ||
                    fsm.ActiveStateName.Contains("Pause")) return false;

                // Clawmaidens
                if (healthManager.name.Contains("Handmaiden") && fsm.ActiveStateName != "Tele In End") return false;
                
                // Nyleth
                if (healthManager.name.Contains("Flower Queen") && fsm.ActiveStateName != "Idle") return false;
            }
            
            // Ensure collider is ready
            var col = healthManager.GetComponent<Collider2D>();
            if ((!col || !col.isActiveAndEnabled) && 
                healthManager.name != "Splinter Queen" && 
                !healthManager.name.Contains("Coral Swimmer Small") && 
                !healthManager.name.Contains("Swamp Drifter") && 
                !interactive)
                return false;
            
            // If arena do raytrace
            if (!arena) return true;

            var orig = healthManager.transform.position;
            var targ = HeroController.instance.transform.position;
            
            var dist = targ - orig;
            return !Physics2D.Raycast(orig, dist.normalized, dist.magnitude, TerrainMask).collider;
        });
        if (!healthManager || healthManager.isDead) yield break;
        yield return new WaitForSeconds(arena ? 0.75f : 0.02f);
        if (healthManager.hp > 100000) yield break;

        var noControl = HeroController.instance.controlReqlinquished;

        healthManager.largeGeoDrops = 0;
        healthManager.mediumGeoDrops = 0;
        healthManager.smallGeoDrops = 0;
        healthManager.shellShardDrops = 0;

        if (!healthManager) yield break;
        if (!SpawnRandomEnemy(healthManager, type, arena, healthManager.initHp)) yield break;

        var de = healthManager.enemyDeathEffects;
        if (de)
        {
            var corpse = de.GetInstantiatedCorpse(AttackTypes.Generic);
            if (corpse)
            {
                foreach (var o in corpse.GetComponentsInChildren<tk2dSprite>())
                {
                    o.color = Color.clear;
                }

                foreach (var o in corpse.GetComponentsInChildren<SpriteRenderer>())
                {
                    o.color = Color.clear;
                }

                corpse.AddComponent<MiscFixers.ColorLock>();
            }
        }

        if (noControl)
        {
            yield return new WaitForSeconds(0.2f);
            
            if (HeroController.instance.controlReqlinquished && 
                !HeroController.instance.cState.isBinding &&
                HeroController.instance.transitionState == HeroTransitionState.WAITING_TO_TRANSITION
                && !HeroController.instance.cState.dashing
                && !PlayerData.instance.atBench
                && !HeroController.instance.sprintFSM.ActiveStateName.Contains("Sprint")
                && !HeroController.instance.umbrellaFSM.ActiveStateName.Contains("Inflate")
                && !HeroController.instance.isUmbrellaActive.Value
                && !HeroController.instance.cState.isSprinting
                && !HeroController.instance.cState.jumping
                && !HeroController.instance.cState.floating)
            {
                HeroController.instance.RegainControl();
                HeroController.instance.StartAnimationControl();
            }
        }
    }

    private static readonly LayerMask TerrainMask = LayerMask.GetMask("Terrain");

    private static bool SpawnRandomEnemy(HealthManager source, Settings.RandoType type, bool arena, int hp)
    {
        if (source.gameObject.GetComponent<ReplacedEnemy>()) return false;
        var replaced = source.gameObject.AddComponent<ReplacedEnemy>();
        source.gameObject.SetActive(false);
        
        var (o, blackThreaded, plasmified) = EnemyChooser.GetRandomEnemy(source, type);
        
        var rot = source.gameObject.transform.rotation;
        var rg = o.GetRotationGroup();
        if (rg is RotationGroup.None or RotationGroup.Vertical) rot = o.Prefab.transform.rotation;

        var pos = source.gameObject.transform.position;
        pos.z = o.ZPosition;

        var ogCol = source.GetComponent<Collider2D>();
        for (var i = 0; i < Math.Max(1, Settings.Multiplier.Value); i++)
        {
            var enemy = Object.Instantiate(
                o.Prefab,
                pos,
                rot);

            enemy.name = $"[Enemy Rando] {o.GetName()} ({source.name})";
            
            o.PostSpawnAction?.Invoke(enemy);

            foreach (var conf in o.ConfigGroup)
            {
                var val = conf.GetDefaultValue();
                if (val == null) continue;

                if (val.GetPriority() < 0) val.Setup(enemy);
            }

            enemy.RemoveComponent<PersistentBoolItem>();
            enemy.RemoveComponent<TestGameObjectActivator>();
            enemy.RemoveComponent<DeactivateIfPlayerdataFalse>();
            enemy.RemoveComponent<DeactivateIfPlayerdataFalseDelayed>();
            enemy.RemoveComponent<DeactivateIfPlayerdataTrue>();
            enemy.RemoveComponent<ConstrainPosition>();

            var hm = enemy.GetComponent<HealthManager>();
            if (Settings.MaintainHp.Value) hm.hp = hp;
            if (blackThreaded) hm.hp *= 2;
            hm.bigEnemyDeath = source.bigEnemyDeath;

            var re = enemy.AddComponent<ReplacementEnemy>();
            re.target = source;
            re.randoType = type;
            EventHooks.AddEvent(enemy, "OnDeath", ON_RANDO_DEATH);

            enemy.SetActive(true);

            foreach (var conf in o.ConfigGroup)
            {
                var val = conf.GetDefaultValue();
                if (val == null) continue;

                if (val.GetPriority() >= 0) val.Setup(enemy);
            }

            if (plasmified) enemy.AddComponent<LifebloodState>();
            if (blackThreaded)
            {
                var bts = enemy.AddComponent<BlackThreader.CustomBlackThreadState>();

                bts.customAttack = Effects.BlackThreadAttacksDefault[Random.RandomRangeInt(0, 4)];
        
                bts.extraSpriteRenderers = enemy.GetComponentsInChildren<SpriteRenderer>(true);
                bts.extraMeshRenderers = enemy.GetComponentsInChildren<MeshRenderer>(true);

                bts.useCustomHPMultiplier = true;
                bts.customHPMultiplier = 1;
        
                hm.blackThreadState = bts;
                hm.hasBlackThreadState = true;
            }
            
            replaced.replacements.Add(re);
            
            EnemyRandoPlugin.Instance.StartCoroutine(FinishSpawn(hm, ogCol, arena));
        }

        return true;
    }

    private static IEnumerator FinishSpawn(HealthManager hm, Collider2D ogCol, bool arena)
    {
        yield return null;

        var de = hm.enemyDeathEffects;
        if (!de) yield break;

        de.SkipKillFreeze = true;

        var corpse = de.GetInstantiatedCorpse(AttackTypes.Generic);
        if (corpse) corpse.AddComponent<BlockAudio>();

        if (!ogCol) yield break;

        var col = hm.GetComponent<Collider2D>();
        if (!col) yield break;

        var ogBounds = ogCol.bounds;
        var bounds = col.bounds;

        var yShift = (ogBounds.max.y - bounds.min.y) * 1.1f;
        var touch = Physics2D.Raycast(ogBounds.max, new Vector2(0, -1), ogBounds.max.y - bounds.min.y, TerrainMask).collider;
        if (!touch) yield break;
        hm.transform.Translate(0, yShift, 0);
    }
}