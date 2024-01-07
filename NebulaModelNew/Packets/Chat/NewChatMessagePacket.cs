﻿#region

using System;
using NebulaModel.DataStructures.Chat;

#endregion

namespace NebulaModel.Packets.Chat;

public class NewChatMessagePacket
{
    public NewChatMessagePacket() { }

    public NewChatMessagePacket(ChatMessageType messageType, string messageText, DateTime sentAt, string userName)
    {
        MessageType = messageType;
        MessageText = messageText;
        SentAt = sentAt.ToBinary();
        UserName = userName;
    }

    public ChatMessageType MessageType { get; set; }
    public string MessageText { get; set; }
    public long SentAt { get; set; }
    public string UserName { get; set; }
}
