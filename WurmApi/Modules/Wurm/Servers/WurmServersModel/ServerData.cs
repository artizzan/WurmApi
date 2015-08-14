﻿using System;
using System.Collections.Generic;
using AldursLab.WurmApi.PersistentObjects;
using Newtonsoft.Json;

namespace AldursLab.WurmApi.Modules.Wurm.Servers.WurmServersModel
{
    [JsonObject(MemberSerialization.Fields)]
    class ServersData : Entity
    {
        private readonly Dictionary<ServerName, ServerData> serverDatas;
        private DateTimeOffset lastScanDate;

        public Dictionary<ServerName, ServerData> ServerDatas
        {
            get { return serverDatas; }
        }

        public ServersData()
        {
            serverDatas = new Dictionary<ServerName, ServerData>();
            lastScanDate = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero); //something that wont on break on +/- adjustments
        }

        public DateTimeOffset LastScanDate
        {
            get { return lastScanDate; }
            set { lastScanDate = value; }
        }
    }

    public class ServerData
    {
        public ServerData()
        {
            LogHistory = new TimeDetails();
        }

        public TimeDetails LogHistory { get; set; }
    }
}
