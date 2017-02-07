using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloey.Helpers
{
    internal class ModifierData
    {
        #region Category : Disables (Stuns, Shackles)

        // stun modifiers that prevent movement
        internal static readonly HashSet<string> VulnerableStunModifiers = new HashSet<string>
        {
            "modifier_bash", // abyssal blade
            "modifier_bashed", // skull basher
            "modifier_stun", // basic stun
            "modifier_stunned", // basic stun
            "modifier_ancientapparition_coldfeet_freeze", // ancient apparition cold feet
            "modifier_bane_fiends_grip", // bane - fiend's grip
            "modifier_earthshaker_fissure_stun", // earthshaker - fissure
            "modifier_faceless_void_timelock_freeze", // faceless void - time lock
            "modifier_invoker_cold_snap_freeze", // invoker - cold snap
            "modifier_enigma_malefice", // enigma - malefice
            "modifier_jakiro_ice_path_stun", // jakiro - ice path
            "modifier_lion_impale", // lion - earth spike
            "modifier_medusa_stone_gaze_stone", // medusa - stone gaze
            "modifier_monkey_king_boundless_strike_stun", // monkeyking - boundless strike
            "modifier_monkey_king_unperched_stunned", // monkeyking - tree fall
            "modifier_morphling_adaptive_strike", // morphling - adaptive strike
            "modifier_necrolyte_reapers_scythe", // necrophos - reaper's scythe
            "modifier_nyx_assassin_impale", // nyx - impale
            "modifier_pudge_disember", // pudge - disember
            "modifier_sandking_impale", // sandking - burrowstrike
            "modifier_shadow_shaman_shackles", // shadow shaman - shackles
            "modifier_tidehunter_ravage", // tide hunter - ravage
            "modifier_tiny_avalanche_stun", // tiny - avalanche
            "modifier_tusk_walrus_punch_air_time", // tusk - walrus punch
            "modifier_windrunner_shackle_shot", // windrunner - shackleshot extended
            "modifier_winter_wyvern_cold_embrace", // winter wyvern - cold embrace
            "modifier_techies_stasis_trap_stunned", // techies - stasis trap
        };

        #endregion

        #region Category : Disables (Sleep, Cyclone)

        // sleep and cyclone modifiers that prevent movement
        internal static readonly HashSet<string> InvulnerableStunModifiers = new HashSet<string>
        {
            "modifier_bane_nightmare_invulberable", // bane - nightmare
            // "modifier_naga_siren_song_of_the_siren_aura",
            "modifier_elder_titan_echo_stomp", // elder titan - echo stomp
            "modifier_eul_cyclone", // item - eul's scpeter of divinity
            "modifier_invoker_tornado" // invoker - tornado
        };

        #endregion


        #region Category : Disables (Roots, Frozen)

        // root modifiers that prevent movement
        internal static readonly HashSet<string> RootModifiers = new HashSet<string>
        {
            "modifier_crystal_maiden_frostbite", // crystal maiden - frostbite
            "modifier_ember_spirit_searing_chains", // ember spirit - searing chains
            "modifier_naga_siren_ensare", // naga sire - ensnare
            "modifier_meepo_earthbind", // meepo - earthbind
            "modifier_rod_of_atos_debuff", // item - rod of atos
            "modifier_treant_overgrowth", // treant protector - overgrowth
            "modifier_underlord_pit_of_malice_ensare", // underlord = pit of malice
        };

        #endregion
    }
}
