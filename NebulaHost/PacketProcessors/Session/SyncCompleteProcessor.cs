﻿using NebulaModel.Attributes;
using NebulaModel.Networking;
using NebulaModel.Packets.Processors;
using NebulaModel.Packets.Session;
using NebulaWorld;

namespace NebulaHost.PacketProcessors.Session
{
    [RegisterPacketProcessor]
    public class SyncCompleteProcessor : IPacketProcessor<SyncComplete>
    {
        private PlayerManager playerManager;

        public SyncCompleteProcessor()
        {
            playerManager = MultiplayerHostSession.Instance.PlayerManager;
        }

        public void ProcessPacket(SyncComplete packet, NebulaConnection conn)
        {
            Player player = playerManager.GetSyncingPlayer(conn);
            playerManager.SyncingPlayers.Remove(player.Connection);
            playerManager.ConnectedPlayers.Add(player.Connection, player);

            if (playerManager.SyncingPlayers.Count == 0)
            {
                playerManager.SendPacketToOtherPlayers(new SyncComplete(), player);
            }

            // Unpause the game
            InGamePopup.FadeOut();
            GameMain.Resume();
        }
    }
}