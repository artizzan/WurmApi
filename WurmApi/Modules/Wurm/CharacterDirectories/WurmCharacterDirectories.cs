﻿using System;
using System.Collections.Generic;
using System.Linq;
using AldursLab.WurmApi.JobRunning;
using AldursLab.WurmApi.Modules.Events.Internal;
using AldursLab.WurmApi.Modules.Events.Internal.Messages;
using AldursLab.WurmApi.Utility;
using JetBrains.Annotations;

namespace AldursLab.WurmApi.Modules.Wurm.CharacterDirectories
{
    /// <summary>
    /// Manages directory information about wurm character folders
    /// </summary>
    class WurmCharacterDirectories : WurmSubdirsMonitor, IWurmCharacterDirectories
    {
        readonly IInternalEventAggregator eventAggregator;

        public WurmCharacterDirectories(IWurmPaths wurmPaths, [NotNull] IInternalEventAggregator eventAggregator, TaskManager taskManager)
            : base(wurmPaths.CharactersDirFullPath, taskManager, () => eventAggregator.Send(new CharacterDirectoriesChanged()))
        {
            if (eventAggregator == null) throw new ArgumentNullException("eventAggregator");
            this.eventAggregator = eventAggregator;
        }

        public string GetFullDirPathForCharacter([NotNull] CharacterName characterName)
        {
            if (characterName == null) throw new ArgumentNullException("characterName");
            return GetFullPathForDirName(characterName.Normalized);
        }

        public IEnumerable<CharacterName> GetAllCharacters()
        {
            return base.AllDirectoryNamesNormalized.Select(s => new CharacterName(s)).ToArray();
        }

        public bool Exists(CharacterName characterName)
        {
            return base.AllDirectoryNamesNormalized.Any(s => new CharacterName(s) == characterName);
        }
    }
}