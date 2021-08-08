using HarmonyLib;

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
      Harmony.DEBUG = debug;

      Debug.Print("");
      FileLog.Log("");

      Debug.Print("DecideAgentKnockedByBlow ");
      FileLog.Log("DecideAgentKnockedByBlow ");
      Debug.Print("victimAgent.IsMainAgent: " + victim.IsMainAgent);
      FileLog.Log("victimAgent.IsMainAgent: " + victim.IsMainAgent);
      Debug.Print("isInitialBlowShrugOff: " + isInitialBlowShrugOff);
      FileLog.Log("isInitialBlowShrugOff: " + isInitialBlowShrugOff);
      Debug.Print("blow.BlowFlag: " + blow.BlowFlag);
      FileLog.Log("blow.BlowFlag: " + blow.BlowFlag);

      if (victim.IsMainAgent)
      {
        Debug.Print("You blocked the knockback. " + collisionData.DamageType + " " + collisionData.BaseMagnitude);
        FileLog.Log("You blocked the knockback. " + collisionData.DamageType + " " + collisionData.BaseMagnitude);
        blow.BlowFlag &= ~BlowFlags.KnockBack;
        blow.BlowFlag &= ~BlowFlags.KnockDown;
        Debug.Print("blow.BlowFlag: " + blow.BlowFlag);
        FileLog.Log("blow.BlowFlag: " + blow.BlowFlag);
      }

      Harmony.DEBUG = false;
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
      Harmony.DEBUG = debug;

      Debug.Print("");
      FileLog.Log("");

      Debug.Print("DecideMountRearedByBlow ");
      FileLog.Log("DecideMountRearedByBlow ");

      Debug.Print("victimAgent.IsMount: " + victimAgent.IsMount);
      FileLog.Log("victimAgent.IsMount: " + victimAgent.IsMount);
      Debug.Print("victimAgent.IsMine: " + victimAgent.IsMine);
      FileLog.Log("victimAgent.IsMine: " + victimAgent.IsMine);
      Debug.Print("blow.BlowFlag: " + blow.BlowFlag);
      FileLog.Log("blow.BlowFlag: " + blow.BlowFlag);

      if (victimAgent.IsMount)
      {
        Debug.Print("Your mount has shrugged off the blow. " + collisionData.DamageType + " " + collisionData.BaseMagnitude);
        FileLog.Log("Your mount has shrugged off the blow. " + collisionData.DamageType + " " + collisionData.BaseMagnitude);
        // cancel the BlowFlag if there is one
        blow.BlowFlag &= ~BlowFlags.MakesRear;
        Debug.Print("blow.BlowFlag: " + blow.BlowFlag);
        FileLog.Log("blow.BlowFlag: " + blow.BlowFlag);
      }

      Debug.Print("");
      FileLog.Log("");
      Harmony.DEBUG = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Mission), "DecideAgentShrugOffBlow")]
    protected static void DecideAgentShrugOffBlow(Agent victimAgent,
      AttackCollisionData collisionData,
      ref Blow blow, ref bool __result)
    {
      Harmony.DEBUG = debug;

      Debug.Print("");
      FileLog.Log("");

      Debug.Print("DecideAgentShrugOffBlow ");
      FileLog.Log("DecideAgentShrugOffBlow ");
      Debug.Print("Main Agent ");
      FileLog.Log("Main Agent ");
      Debug.Print("victimAgent.IsMainAgent: " + victimAgent.IsMainAgent);
      FileLog.Log("victimAgent.IsMainAgent: " + victimAgent.IsMainAgent);
      Debug.Print("victimAgent.IsMount: " + victimAgent.IsMount);
      FileLog.Log("victimAgent.IsMount: " + victimAgent.IsMount);
      Debug.Print("victimAgent.IsMine: " + victimAgent.IsMine);
      FileLog.Log("victimAgent.IsMine: " + victimAgent.IsMine);
      Debug.Print("blow.BlowFlag: " + blow.BlowFlag);
      FileLog.Log("blow.BlowFlag: " + blow.BlowFlag);

      if (victimAgent.IsMainAgent || (victimAgent.IsMount && victimAgent.IsMine))
      {
        Debug.Print("You've shrugged off the blow. " + collisionData.DamageType + " " + collisionData.BaseMagnitude);
        FileLog.Log("You've shrugged off the blow. " + collisionData.DamageType + " " + collisionData.BaseMagnitude);
        blow.BlowFlag |= BlowFlags.ShrugOff;
        __result = true;
      }

      Debug.Print("");
      FileLog.Log("");
      Harmony.DEBUG = false;
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
      Harmony.DEBUG = debug;
      Debug.Print("");
      FileLog.Log("");

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
        Debug.Print("defender: " + defender.Name);
        FileLog.Log("defender: " + defender.Name);
        Debug.Print("Collision Reaction: cancel stagger");
        FileLog.Log("Collision Reaction: cancel stagger");
        Debug.Print("Collision Reaction: Bounced");
        FileLog.Log("Collision Reaction: Bounced");
        colReaction = MeleeCollisionReaction.Bounced;
      }

      Debug.Print("");
      FileLog.Log("");
      Harmony.DEBUG = false;
    }

    protected override void OnBeforeInitialModuleScreenSetAsRoot()
    {
      base.OnBeforeInitialModuleScreenSetAsRoot();
      var harmony = new Harmony("com.DontInterruptMe.akdombrowski");

      harmony.PatchAll();

      FileLog.Reset();

      InformationManager.DisplayMessage(new InformationMessage("Loaded 'AttackInteruptModifier'.", Color.FromUint(0182599992U)));
    }
  }
}