﻿using HarmonyLib;
using NebulaModel.Packets.Factory.Inserter;
using NebulaWorld;

namespace NebulaPatcher.Patches.Dynamic
{
    [HarmonyPatch(typeof(UIInserterWindow))]
    class UIInserterWindow_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIInserterWindow.OnResetFilterButtonClick))]
        public static void OnResetFilterButtonClick_Prefix(UIInserterWindow __instance)
        {
            //Notify about reseting inserter's filter
            if (Multiplayer.IsActive)
            {
                LocalPlayer.SendPacketToLocalStar(new InserterFilterUpdatePacket(__instance.inserterId, 0, GameMain.localPlanet?.id ?? -1));
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIInserterWindow.OnItemPickerReturn))]
        public static void OnItemPickerReturn_Prefix(UIInserterWindow __instance, ItemProto item)
        {
            //Notify about changing filter item
            if (Multiplayer.IsActive)
            {
                LocalPlayer.SendPacketToLocalStar(new InserterFilterUpdatePacket(__instance.inserterId, (item != null) ? item.ID : 0, GameMain.localPlanet?.id ?? -1));
            }
        }
    }
}
