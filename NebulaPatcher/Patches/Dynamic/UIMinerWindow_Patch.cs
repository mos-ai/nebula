﻿using HarmonyLib;
using NebulaModel.Packets.Factory.Miner;
using NebulaWorld;

namespace NebulaPatcher.Patches.Dynamic
{
    [HarmonyPatch(typeof(UIMinerWindow))]
    class UIMinerWindow_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIMinerWindow.OnProductIconClick))]
        public static void OnProductIconClick_Prefix(UIMinerWindow __instance)
        {
            if (Multiplayer.IsActive)
            {
                LocalPlayer.SendPacketToLocalStar(new MinerStoragePickupPacket(__instance.minerId, GameMain.localPlanet?.id ?? -1));
            }
        }
    }
}
