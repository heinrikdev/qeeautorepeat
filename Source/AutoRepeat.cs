using System.Collections.Generic;
using HarmonyLib;
using QEthics;
using RimWorld;
using UnityEngine;
using Verse;

namespace QEEAutoRepeat
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            new Harmony("heinrikdev.qeeautorepeat").PatchAll();
        }
    }

    public class CompProperties_AutoRepeat : CompProperties
    {
        public CompProperties_AutoRepeat()
        {
            compClass = typeof(Comp_AutoRepeat);
        }
    }

    // Guarda o toggle e o ultimo cultivo. Ao ficar ocioso com o toggle ligado,
    // reinicia o mesmo cultivo (cuba de orgaos: receita; cuba de clones: um genoma
    // igual disponivel no mapa).
    public class Comp_AutoRepeat : ThingComp
    {
        public bool autoRepeat;

        // capturado pelos patches de start; nao persistido (re-captura ao iniciar manualmente)
        public GrowerRecipeDef lastRecipe;
        public ThingDef lastGenomeDef;

        private int cooldown;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref autoRepeat, "autoRepeat", defaultValue: false);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Toggle
            {
                defaultLabel = "Auto-repetir cultivo",
                defaultDesc = "Quando ligado, a cuba reinicia sozinha o ultimo cultivo assim que termina e fica ociosa - se houver ingredientes (e, na cuba de clones, um genoma disponivel no mapa).",
                icon = TexCommand.ForbidOff,
                isActive = () => autoRepeat,
                toggleAction = delegate { autoRepeat = !autoRepeat; }
            };
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (!autoRepeat)
            {
                return;
            }
            Building_GrowerBase grower = parent as Building_GrowerBase;
            if (grower == null || !grower.Spawned)
            {
                return;
            }

            // so reinicia quando ocioso
            CrafterStatus status = Traverse.Create(grower).Field("status").GetValue<CrafterStatus>();
            if (status != CrafterStatus.Idle)
            {
                return;
            }
            if (cooldown > 0)
            {
                cooldown--;
                return;
            }

            // cuba de orgaos (gizmo): re-inicia a mesma receita (nao e consumida)
            if (grower is Building_VatGrower && lastRecipe != null)
            {
                Traverse.Create(grower).Method("startCraftingRecipe", new object[] { lastRecipe }).GetValue();
                cooldown = 3;
                return;
            }

            // cuba de clones: procura um genoma igual e re-inicia
            if (grower is Building_PawnVatGrower pawnVat && lastGenomeDef != null)
            {
                GenomeSequence g = FindGenome(pawnVat, lastGenomeDef);
                if (g != null)
                {
                    Traverse.Create(pawnVat).Method("StartCrafting", new object[] { g }).GetValue();
                    cooldown = 3;
                }
            }
        }

        private static GenomeSequence FindGenome(Building vat, ThingDef def)
        {
            Map map = vat.Map;
            if (map == null)
            {
                return null;
            }
            foreach (Thing t in map.listerThings.ThingsOfDef(def))
            {
                if (t is GenomeSequence gs && !t.IsForbidden(Faction.OfPlayer))
                {
                    return gs;
                }
            }
            return null;
        }
    }

    // ---- captura do ultimo cultivo iniciado manualmente ----

    [HarmonyPatch(typeof(Building_VatGrower), "startCraftingRecipe")]
    public static class Patch_VatGrower_Start
    {
        private static void Postfix(Building_VatGrower __instance, GrowerRecipeDef recipeDef)
        {
            Comp_AutoRepeat c = __instance.GetComp<Comp_AutoRepeat>();
            if (c != null)
            {
                c.lastRecipe = recipeDef;
            }
        }
    }

    [HarmonyPatch(typeof(Building_PawnVatGrower), "StartCrafting")]
    public static class Patch_PawnVat_Start
    {
        private static void Postfix(Building_PawnVatGrower __instance, GenomeSequence genome)
        {
            Comp_AutoRepeat c = __instance.GetComp<Comp_AutoRepeat>();
            if (c != null && genome != null)
            {
                c.lastGenomeDef = genome.def;
            }
        }
    }
}
