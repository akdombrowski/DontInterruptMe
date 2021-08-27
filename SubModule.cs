using HarmonyLib;

using System.Collections.Generic;

using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DontInterruptMe
{
    [HarmonyPatch]
    public class SubModule : MBSubModuleBase
    {
        private static bool debug = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mission), "DecideAgentKnockedByBlow")]
        public static void DecideAgentKnockedByBlow(
            Agent attacker,
            Agent victim,
            in AttackCollisionData collisionData,
            WeaponComponentData attackerWeapon,
            bool isInitialBlowShrugOff,
            ref Blow blow)
        {

            if (victim.IsMainAgent)
            {
                Harmony.DEBUG = debug;
                List<string> buffer = FileLog.GetBuffer(true);
                buffer.Add("");
                buffer.Add("");

                buffer.Add("DecideAgentKnockedByBlow ");
                buffer.Add("victimAgent.IsMainAgent: " + victim.IsMainAgent);
                buffer.Add("isInitialBlowShrugOff: " + isInitialBlowShrugOff);
                buffer.Add("blow.BlowFlag: " + blow.BlowFlag);

                buffer.Add("You blocked the knockback. " + collisionData.DamageType + " " + collisionData.BaseMagnitude);

                blow.BlowFlag &= ~BlowFlags.KnockBack;
                blow.BlowFlag &= ~BlowFlags.KnockDown;

                buffer.Add("blow.BlowFlag: " + blow.BlowFlag);


                FileLog.LogBuffered(buffer);
                FileLog.FlushBuffer(); 
                
                Harmony.DEBUG = false;
            }

        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mission), "DecideMountRearedByBlow")]
        public static void DecideMountRearedByBlow(Agent attackerAgent,
            Agent victimAgent,
            in AttackCollisionData collisionData,
            WeaponComponentData attackerWeapon,
            float rearDamageThresholdMultiplier,
            Vec3 blowDirection,
            ref Blow blow)
        {

            if (victimAgent.IsMount)
            {
                Harmony.DEBUG = debug;

                List<string> buffer = FileLog.GetBuffer(true);

                buffer.Add("");
                buffer.Add("");
                buffer.Add("DecideMountRearedByBlow");
                buffer.Add("blow.BlowFlag: " + blow.BlowFlag);

                buffer.Add("Your mount has shrugged off the blow. " + collisionData.DamageType + " " + collisionData.BaseMagnitude);
                // cancel the BlowFlag if there is one
                blow.BlowFlag &= ~BlowFlags.MakesRear;

                buffer.Add("blow.BlowFlag: " + blow.BlowFlag);
                buffer.Add("");
                buffer.Add("");

                FileLog.LogBuffered(buffer);
                FileLog.FlushBuffer();

                Harmony.DEBUG = false;
            }

        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mission), "DecideAgentShrugOffBlow")]
        protected static void DecideAgentShrugOffBlow(Agent victimAgent,
            AttackCollisionData collisionData,
            ref Blow blow, ref bool __result)
        {

            if (victimAgent.IsMainAgent || (victimAgent.IsMount && victimAgent.IsMine))
            {
                Harmony.DEBUG = debug;
                List<string> buffer = FileLog.GetBuffer(true);

                buffer.Add("");
                buffer.Add("");
                buffer.Add("DecideAgentShrugOffBlow ");
                buffer.Add("victimAgent.IsMainAgent: " + victimAgent.IsMainAgent);
                buffer.Add("victimAgent.IsMount: " + victimAgent.IsMount);
                buffer.Add("victimAgent.IsMine: " + victimAgent.IsMine);
                buffer.Add("blow.BlowFlag: " + blow.BlowFlag);

                buffer.Add("You've shrugged off the blow. " + collisionData.DamageType + " " + collisionData.BaseMagnitude);

                blow.BlowFlag |= BlowFlags.ShrugOff;

                __result = true;

                FileLog.LogBuffered(buffer);
                FileLog.FlushBuffer();
                Harmony.DEBUG = false;
            }

        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mission), "DecideWeaponCollisionReaction")]
        protected static void DecideWeaponCollisionReaction(
          Blow registeredBlow,
          in AttackCollisionData collisionData,
          Agent attacker,
          Agent defender,
          in MissionWeapon attackerWeapon,
          bool isFatalHit,
          bool isShruggedOff,
          out MeleeCollisionReaction colReaction, Mission __instance)
        {

            // original code
            if (collisionData.IsColliderAgent && collisionData.StrikeType == 1 && collisionData.CollisionHitResultFlags.HasAnyFlag<CombatHitResultFlags>(CombatHitResultFlags.HitWithStartOfTheAnimation))
                colReaction = MeleeCollisionReaction.Staggered;
            else if (!collisionData.IsColliderAgent && collisionData.PhysicsMaterialIndex != -1 && PhysicsMaterial.GetFromIndex(collisionData.PhysicsMaterialIndex).GetFlags().HasAnyFlag<PhysicsMaterialFlags>(PhysicsMaterialFlags.AttacksCanPassThrough))
                colReaction = MeleeCollisionReaction.SlicedThrough;
            else if (!collisionData.IsColliderAgent || registeredBlow.InflictedDamage <= 0)
                colReaction = MeleeCollisionReaction.Bounced;
            else if (collisionData.StrikeType == 1 && attacker.IsDoingPassiveAttack)
                colReaction = MissionGameModels.Current.AgentApplyDamageModel.DecidePassiveAttackCollisionReaction(attacker, defender, isFatalHit);
            else
            {
                WeaponClass weaponClass = !attackerWeapon.IsEmpty ? attackerWeapon.CurrentUsageItem.WeaponClass : WeaponClass.Undefined;
                int num1 = attackerWeapon.IsEmpty ? 0 : (!isFatalHit ? 1 : 0);
                int num2 = isShruggedOff ? 1 : 0;
                colReaction = (num1 & num2) != 0 || attackerWeapon.IsEmpty && defender != null && defender.IsHuman && !collisionData.IsAlternativeAttack && (collisionData.VictimHitBodyPart == BoneBodyPartType.Chest || collisionData.VictimHitBodyPart == BoneBodyPartType.ShoulderLeft || (collisionData.VictimHitBodyPart == BoneBodyPartType.ShoulderRight || collisionData.VictimHitBodyPart == BoneBodyPartType.Abdomen) || collisionData.VictimHitBodyPart == BoneBodyPartType.Legs) ? MeleeCollisionReaction.Bounced : ((weaponClass == WeaponClass.OneHandedAxe || weaponClass == WeaponClass.TwoHandedAxe) && (!isFatalHit && (double)collisionData.InflictedDamage < (double)defender.HealthLimit * 0.5) || attackerWeapon.IsEmpty && !collisionData.IsAlternativeAttack && collisionData.AttackDirection == Agent.UsageDirection.AttackUp || collisionData.ThrustTipHit && (sbyte)collisionData.DamageType == (sbyte)1 && (!attackerWeapon.IsEmpty && defender.CanThrustAttackStickToBone(collisionData.CollisionBoneIndex)) ? MeleeCollisionReaction.Stuck : MeleeCollisionReaction.SlicedThrough);
                if (!collisionData.AttackBlockedWithShield && !collisionData.CollidedWithShieldOnBack || colReaction != MeleeCollisionReaction.SlicedThrough)
                    return;
                colReaction = MeleeCollisionReaction.Bounced;
            }
            // original code

            if (defender != null && defender.IsMine && colReaction == MeleeCollisionReaction.Staggered)
            {
                Harmony.DEBUG = debug;

                List<string> buffer = FileLog.GetBuffer(true);

                buffer.Add("");
                buffer.Add("");
                buffer.Add("DecideWeaponCollisionReaction");
                buffer.Add("defender: " + defender.Name);
                buffer.Add("Collision Reaction: cancel stagger");
                colReaction = MeleeCollisionReaction.Bounced;
                buffer.Add("colReaction: " + colReaction);
                buffer.Add("");
                buffer.Add("");
                FileLog.LogBuffered(buffer);
                FileLog.FlushBuffer();
                Harmony.DEBUG = false;
            }

        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            var harmony = new Harmony("com.DontInterruptMe.akdombrowski");

            harmony.PatchAll();

            InformationManager.DisplayMessage(new InformationMessage("Loaded 'DontInterruptMe'.", Color.FromUint(0182599992U)));
        }
    }
}