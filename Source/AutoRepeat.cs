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
        public GenomeSequence lastGenome;

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

        // As cubas do QEE ticam em modo Normal, entao usamos CompTick (CompTickRare
        // nunca seria chamado). Checamos ~1x por segundo.
        private static bool Dbg => Prefs.DevMode;

        // As cubas do QEE ticam em modo Normal, entao usamos CompTick (CompTickRare
        // nunca seria chamado). Checamos ~1x por segundo.
        public override void CompTick()
        {
            base.CompTick();
            if (!autoRepeat)
            {
                return;
            }
            if (!parent.IsHashIntervalTick(60))
            {
                return;
            }
            Building_GrowerBase grower = parent as Building_GrowerBase;
            if (grower == null || !grower.Spawned)
            {
                if (Dbg) Log.Warning("[QEEAutoRepeat] tick: parent nao e Building_GrowerBase ou nao spawnado.");
                return;
            }

            CrafterStatus status;
            try
            {
                status = Traverse.Create(grower).Field("status").GetValue<CrafterStatus>();
            }
            catch (System.Exception e)
            {
                Log.Error("[QEEAutoRepeat] falha ao ler 'status': " + e.Message);
                return;
            }

            if (status != CrafterStatus.Idle)
            {
                return; // ainda cultivando/enchendo
            }

            // ocioso: tenta reiniciar
            try
            {
                // cuba de orgaos (gizmo): re-inicia a mesma receita (nao e consumida)
                if (grower is Building_VatGrower)
                {
                    if (lastRecipe == null)
                    {
                        if (Dbg) Log.Message("[QEEAutoRepeat] cuba de orgaos ociosa mas sem receita memorizada (inicie um cultivo 1x).");
                        return;
                    }
                    if (Dbg) Log.Message("[QEEAutoRepeat] reiniciando cuba de orgaos: " + lastRecipe.defName);
                    Traverse.Create(grower).Method("startCraftingRecipe", new object[] { lastRecipe }).GetValue();
                    return;
                }

                // cuba de clones: reusa o mesmo genoma (o QEE nao consome; a cuba guarda em 'genome')
                if (grower is Building_PawnVatGrower pawnVat)
                {
                    GenomeSequence g = (lastGenome != null && !lastGenome.Destroyed)
                        ? lastGenome
                        : Traverse.Create(pawnVat).Field("genome").GetValue<GenomeSequence>();

                    if (g == null || g.Destroyed)
                    {
                        if (Dbg) Log.Message("[QEEAutoRepeat] cuba de clones ociosa mas sem genoma (lastGenome e campo 'genome' nulos). Inicie uma clonagem 1x.");
                        return;
                    }
                    if (Dbg) Log.Message("[QEEAutoRepeat] reiniciando cuba de clones com genoma: " + g.LabelCap);
                    Traverse.Create(pawnVat).Method("StartCrafting", new object[] { g }).GetValue();
                }
            }
            catch (System.Exception e)
            {
                Log.Error("[QEEAutoRepeat] erro ao reiniciar cultivo: " + e);
            }
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
                if (Prefs.DevMode) Log.Message("[QEEAutoRepeat] capturou receita da cuba de orgaos: " + recipeDef?.defName);
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
                c.lastGenome = genome;
                if (Prefs.DevMode) Log.Message("[QEEAutoRepeat] capturou genoma da cuba de clones: " + genome.LabelCap);
            }
        }
    }

    // Log de inicializacao pra confirmar que o mod carregou e patcheou.
    [StaticConstructorOnStartup]
    public static class LoadNotice
    {
        static LoadNotice()
        {
            Log.Message("[QEEAutoRepeat] carregado e patches aplicados.");
        }
    }
}
