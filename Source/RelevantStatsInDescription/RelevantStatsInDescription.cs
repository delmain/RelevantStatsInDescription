﻿using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RelevantStatsInDescription;

[StaticConstructorOnStartup]
public class RelevantStatsInDescription
{
    private static Dictionary<string, string> cachedDescriptions;

    static RelevantStatsInDescription()
    {
        cachedDescriptions = new Dictionary<string, string>();
        var harmony = new Harmony("Mlie.RelevantStatsInDescription");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    public static void ClearCache()
    {
        cachedDescriptions = new Dictionary<string, string>();
    }

    public static string GetUpdatedDescription(BuildableDef def, ThingDef stuff)
    {
        var descriptionKey = $"{def.defName}|{stuff?.defName}";
        if (cachedDescriptions.ContainsKey(descriptionKey))
        {
            return cachedDescriptions[descriptionKey] + def.description;
        }

        var arrayToAdd = new List<string>();

        if (def is TerrainDef floorDef)
        {
            if (RelevantStatsInDescriptionMod.instance.RelevantStatsInDescriptionSettings.ShowAffordance)
            {
                if (floorDef.affordances?.Any() == true)
                {
                    if (floorDef.affordances.Contains(TerrainAffordanceDefOf.Heavy))
                    {
                        arrayToAdd.Add("RSID_MaxAffordance".Translate(TerrainAffordanceDefOf.Heavy.LabelCap));
                    }
                    else if (floorDef.affordances.Contains(TerrainAffordanceDefOf.Medium))
                    {
                        arrayToAdd.Add("RSID_MaxAffordance".Translate(TerrainAffordanceDefOf.Medium.LabelCap));
                    }
                    else if (floorDef.affordances.Contains(TerrainAffordanceDefOf.Light))
                    {
                        arrayToAdd.Add("RSID_MaxAffordance".Translate(TerrainAffordanceDefOf.Light.LabelCap));
                    }
                    else
                    {
                        arrayToAdd.Add("RSID_MaxAffordance".Translate("RSID_Undefined".Translate()));
                    }
                }
                else
                {
                    arrayToAdd.Add("RSID_MaxAffordance".Translate("RSID_Undefined".Translate()));
                }

                arrayToAdd.Add(" - - - \n");
                cachedDescriptions[descriptionKey] = string.Join("\n", arrayToAdd);
            }
            else
            {
                cachedDescriptions[descriptionKey] = string.Empty;
            }

            return cachedDescriptions[descriptionKey] + floorDef.description;
        }

        if (def is not ThingDef buildableThing)
        {
            return def.description;
        }

        if (stuff == null && def.MadeFromStuff)
        {
            stuff = GenStuff.DefaultStuffFor(def);
        }

        var thing = new Thing { def = buildableThing };
        if (stuff != null)
        {
            thing.SetStuffDirect(stuff);
        }

        // Structural building
        if (buildableThing.graphicData?.linkFlags != null &&
            ((buildableThing.graphicData.linkFlags & LinkFlags.Wall) != 0 ||
             (buildableThing.graphicData.linkFlags & LinkFlags.Fences) != 0 ||
             (buildableThing.graphicData.linkFlags & LinkFlags.Barricades) != 0 ||
             (buildableThing.graphicData.linkFlags & LinkFlags.Sandbags) != 0) ||
            buildableThing.IsDoor)
        {
            if (RelevantStatsInDescriptionMod.instance.RelevantStatsInDescriptionSettings.ShowHP)
            {
                arrayToAdd.Add("RSID_MaxHP".Translate(thing.MaxHitPoints));
            }

            if (RelevantStatsInDescriptionMod.instance.RelevantStatsInDescriptionSettings.ShowCover &&
                buildableThing.fillPercent < 1)
            {
                arrayToAdd.Add("RSID_Cover".Translate(buildableThing.fillPercent.ToStringPercent()));
            }
        }

        // Comfort
        if (RelevantStatsInDescriptionMod.instance.RelevantStatsInDescriptionSettings.ShowComfort)
        {
            var comfort = thing.GetStatValue(StatDefOf.Comfort);

            if (comfort > 0)
            {
                arrayToAdd.Add(
                    "RSID_Comfort".Translate(comfort.ToStringPercent()));
            }
        }

        // Bed
        if (buildableThing.IsBed)
        {
            if (RelevantStatsInDescriptionMod.instance.RelevantStatsInDescriptionSettings.ShowBedRest &&
                buildableThing.StatBaseDefined(StatDefOf.BedRestEffectiveness))
            {
                arrayToAdd.Add(
                    "RSID_BedRestEffectiveness".Translate(
                        thing.GetStatValue(StatDefOf.BedRestEffectiveness).ToStringPercent()));
            }

            if (buildableThing.building.bed_defaultMedical)
            {
                if (RelevantStatsInDescriptionMod.instance.RelevantStatsInDescriptionSettings.ShowMedicalTendQuality &&
                    buildableThing.StatBaseDefined(StatDefOf.MedicalTendQuality))
                {
                    arrayToAdd.Add("RSID_MedicalTendQuality".Translate(
                        thing.GetStatValue(StatDefOf.MedicalTendQuality).ToStringPercent()));
                }

                if (RelevantStatsInDescriptionMod.instance.RelevantStatsInDescriptionSettings.ShowImmunityGainSpeed &&
                    buildableThing.StatBaseDefined(StatDefOf.ImmunityGainSpeedFactor))
                {
                    arrayToAdd.Add("RSID_ImmunityGainSpeedFactor".Translate(
                        thing.GetStatValue(StatDefOf.ImmunityGainSpeedFactor).ToStringPercent()));
                }

                if (RelevantStatsInDescriptionMod.instance.RelevantStatsInDescriptionSettings
                        .ShowSurgerySuccessChance &&
                    buildableThing.StatBaseDefined(StatDefOf.SurgerySuccessChanceFactor))
                {
                    arrayToAdd.Add("RSID_SurgerySuccessChanceFactor".Translate(
                        thing.GetStatValue(StatDefOf.SurgerySuccessChanceFactor)
                            .ToStringPercent()));
                }
            }
        }

        //// Turrets
        //if (buildableThing.building?.turretGunDef != null)
        //{
        //    var turretGunDef = buildableThing.building.turretGunDef;
        //    var verb = turretGunDef.Verbs?.First();
        //    if (verb != null)
        //    {
        //        var damage = verb.defaultProjectile?.projectile?.GetDamageAmount(turretGunDef, stuff);
        //        if (damage > 0)
        //        {
        //            var cooldownTime = buildableThing.building.turretBurstCooldownTime +
        //                               buildableThing.building.turretBurstWarmupTime;
        //            if (verb.burstShotCount > 1)
        //            {
        //                cooldownTime += verb.burstShotCount * verb.ticksBetweenBurstShots;
        //                damage *= verb.burstShotCount;
        //            }

        //            var dpm = damage / cooldownTime * 42;
        //            arrayToAdd.Add("RSID_DPM".Translate(dpm.ToString()));
        //        }
        //    }
        //}

        // Poweruse
        var consumption = buildableThing.GetCompProperties<CompProperties_Power>()?.basePowerConsumption;
        if (consumption != null)
        {
            if (RelevantStatsInDescriptionMod.instance.RelevantStatsInDescriptionSettings.ShowPowerConsumer &&
                consumption > 0)
            {
                arrayToAdd.Add("RSID_PowerUser".Translate(consumption.ToString()));
            }

            if (RelevantStatsInDescriptionMod.instance.RelevantStatsInDescriptionSettings.ShowPowerProducer &&
                consumption < 0)
            {
                arrayToAdd.Add("RSID_PowerProducer".Translate((consumption * -1).ToString()));
            }
        }

        // Beauty
        if (RelevantStatsInDescriptionMod.instance.RelevantStatsInDescriptionSettings.ShowBeauty)
        {
            var beauty = thing.GetStatValue(StatDefOf.Beauty);
            if (beauty != 0)
            {
                arrayToAdd.Add("RSID_Beauty".Translate(beauty));
            }
        }

        // Joy
        if (RelevantStatsInDescriptionMod.instance.RelevantStatsInDescriptionSettings.ShowJoy &&
            buildableThing.StatBaseDefined(StatDefOf.JoyGainFactor))
        {
            var joy = thing.GetStatValue(StatDefOf.JoyGainFactor);
            if (joy != 0)
            {
                arrayToAdd.Add("RSID_Joy".Translate(joy.ToStringPercent()));
            }
        }

        // Affordance requirement
        if (RelevantStatsInDescriptionMod.instance.RelevantStatsInDescriptionSettings.ShowAffordanceRequirement &&
            buildableThing.terrainAffordanceNeeded != null &&
            buildableThing.terrainAffordanceNeeded != TerrainAffordanceDefOf.Light)
        {
            arrayToAdd.Add("RSID_AffordanceRequirement".Translate(buildableThing.terrainAffordanceNeeded.LabelCap));
        }

        if (arrayToAdd.Any())
        {
            arrayToAdd.Add(" - - - \n");
            cachedDescriptions[descriptionKey] = string.Join("\n", arrayToAdd);
        }
        else
        {
            cachedDescriptions[descriptionKey] = string.Empty;
        }

        return cachedDescriptions[descriptionKey] + buildableThing.description;
    }
}