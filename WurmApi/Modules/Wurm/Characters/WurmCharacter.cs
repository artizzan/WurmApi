using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AldursLab.WurmApi.JobRunning;
using AldursLab.WurmApi.Modules.Events.Internal;
using AldursLab.WurmApi.Modules.Events.Internal.Messages;
using AldursLab.WurmApi.Modules.Events.Public;
using AldursLab.WurmApi.Utility;
using JetBrains.Annotations;

namespace AldursLab.WurmApi.Modules.Wurm.Characters
{
    class WurmCharacter : IWurmCharacter, IDisposable, IHandle<YouAreOnEventDetectedOnLiveLogs>
    {
        readonly IWurmConfigs wurmConfigs;
        readonly IWurmServers wurmServers;
        readonly IWurmServerHistory wurmServerHistory;
        readonly IWurmApiLogger logger;
        readonly TaskManager taskManager;
        readonly IWurmLogsMonitor logsMonitor;
        readonly IPublicEventInvoker publicEventInvoker;
        readonly InternalEventAggregator internalEventAggregator;

        readonly FileSystemWatcher configFileWatcher;
        readonly string configDefiningFileFullPath;
        string currentConfigName = string.Empty;

        private const string ConfigDefinerFileName = "config.txt";

        readonly TaskHandle configUpdateTask;

        public WurmCharacter([NotNull] CharacterName name, [NotNull] string playerDirectoryFullPath,
            [NotNull] IWurmConfigs wurmConfigs, [NotNull] IWurmServers wurmServers,
            [NotNull] IWurmServerHistory wurmServerHistory,
            [NotNull] IWurmApiLogger logger, 
            [NotNull] TaskManager taskManager, [NotNull] IWurmLogsMonitor logsMonitor,
            [NotNull] IPublicEventInvoker publicEventInvoker, [NotNull] InternalEventAggregator internalEventAggregator)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (playerDirectoryFullPath == null) throw new ArgumentNullException("playerDirectoryFullPath");
            if (wurmConfigs == null) throw new ArgumentNullException("wurmConfigs");
            if (wurmServers == null) throw new ArgumentNullException("wurmServers");
            if (wurmServerHistory == null) throw new ArgumentNullException("wurmServerHistory");
            if (logger == null) throw new ArgumentNullException("logger");
            if (taskManager == null) throw new ArgumentNullException("taskManager");
            if (logsMonitor == null) throw new ArgumentNullException("logsMonitor");
            if (publicEventInvoker == null) throw new ArgumentNullException("publicEventInvoker");
            if (internalEventAggregator == null) throw new ArgumentNullException("internalEventAggregator");

            this.wurmConfigs = wurmConfigs;
            this.wurmServers = wurmServers;
            this.wurmServerHistory = wurmServerHistory;
            this.logger = logger;
            this.taskManager = taskManager;
            this.logsMonitor = logsMonitor;
            this.publicEventInvoker = publicEventInvoker;
            this.internalEventAggregator = internalEventAggregator;

            internalEventAggregator.Subscribe(this);

            Name = name;
            configDefiningFileFullPath = Path.Combine(playerDirectoryFullPath, ConfigDefinerFileName);

            RefreshCurrentConfig();

            configUpdateTask = new TaskHandle(RefreshCurrentConfig, "Current config update for player " + Name);
            taskManager.Add(configUpdateTask);

            configFileWatcher = new FileSystemWatcher(playerDirectoryFullPath)
            {
                Filter = ConfigDefinerFileName
            };
            configFileWatcher.Changed += ConfigFileWatcherOnChanged;
            configFileWatcher.Created += ConfigFileWatcherOnChanged;
            configFileWatcher.Deleted += ConfigFileWatcherOnChanged;
            configFileWatcher.Renamed += ConfigFileWatcherOnChanged;
            configFileWatcher.EnableRaisingEvents = true;
            
            configUpdateTask.Trigger();

            try
            {
                wurmServerHistory.BeginTracking(this.Name);
            }
            catch (Exception exception)
            {
                logger.Log(LogLevel.Error,
                    string.Format("Failed to initiate tracking of server history for character {0}", name),
                    this,
                    exception);
            }
        }

        void ConfigFileWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            configUpdateTask.Trigger();
        }

        private void RefreshCurrentConfig()
        {
            if (!File.Exists(configDefiningFileFullPath))
            {
                throw new WurmApiException(
                    string.Format("{0} is missing for character {1}, cannot obtain config!",
                        ConfigDefinerFileName,
                        Name));
            }

            currentConfigName = File.ReadAllText(configDefiningFileFullPath).Trim();
            if (string.IsNullOrWhiteSpace(currentConfigName))
            {
                throw new WurmApiException(
                    string.Format("Could not read config for character {0}, because {1} contains no config name!",
                        Name,
                        ConfigDefinerFileName));
            }

            try
            {
                CurrentConfig = wurmConfigs.GetConfig(currentConfigName);
            }
            catch (Exception exception)
            {
                throw new WurmApiException(
                    string.Format("Could not read config {0} for player {1}. See inner exception for details.",
                        currentConfigName,
                        Name),
                    exception);
            }
        }

        public CharacterName Name { get; private set; }

        public IWurmConfig CurrentConfig { get; private set; }

        #region GetHistoricServerAtLogStamp

        public async Task<IWurmServer> GetHistoricServerAtLogStampAsync(DateTime stamp)
        {
            return await GetHistoricServerAtLogStampAsync(stamp, CancellationToken.None).ConfigureAwait(false);
        }

        public IWurmServer GetHistoricServerAtLogStamp(DateTime stamp)
        {
            return GetHistoricServerAtLogStamp(stamp, CancellationToken.None);
        }

        public async Task<IWurmServer> GetHistoricServerAtLogStampAsync(DateTime stamp, CancellationToken cancellationToken)
        {
            var serverName = await wurmServerHistory.GetServerAsync(this.Name, stamp, cancellationToken).ConfigureAwait(false);
            var server = wurmServers.GetByName(serverName);
            return server;
        }

        public IWurmServer GetHistoricServerAtLogStamp(DateTime stamp, CancellationToken cancellationToken)
        {
            return TaskHelper.UnwrapSingularAggegateException(() => GetHistoricServerAtLogStampAsync(stamp, cancellationToken).Result);
        }

        #endregion

        #region GetCurrentServer

        public async Task<IWurmServer> GetCurrentServerAsync()
        {
            return await GetCurrentServerAsync(CancellationToken.None);
        }

        public IWurmServer GetCurrentServer()
        {
            return GetCurrentServer(CancellationToken.None);
        }

        public async Task<IWurmServer> GetCurrentServerAsync(CancellationToken cancellationToken)
        {
            var serverName = await wurmServerHistory.GetCurrentServerAsync(this.Name, cancellationToken).ConfigureAwait(false);
            var server = wurmServers.GetByName(serverName);
            return server;
        }

        public IWurmServer GetCurrentServer(CancellationToken cancellationToken)
        {
            return TaskHelper.UnwrapSingularAggegateException(() => GetCurrentServerAsync(cancellationToken).Result);
        }

        public event EventHandler<PotentialServerChangeEventArgs> LogInOrCurrentServerPotentiallyChanged;

        #endregion

        public void Dispose()
        {
            configFileWatcher.EnableRaisingEvents = false;
            configFileWatcher.Dispose();
        }

        public void Handle(YouAreOnEventDetectedOnLiveLogs message)
        {
            if (message.CharacterName == Name)
            {
                publicEventInvoker.TriggerInstantly(LogInOrCurrentServerPotentiallyChanged,
                    this,
                    new PotentialServerChangeEventArgs(message.ServerName, message.CurrentServerNameChanged));
            }
        }

        public override string ToString()
        {
            return this.Name.ToString();
        }
    }

    public class PotentialServerChangeEventArgs : EventArgs
    {
        public PotentialServerChangeEventArgs(ServerName serverName, bool serverChanged)
        {
            ServerName = serverName;
            ServerChanged = serverChanged;
        }

        /// <summary>
        /// Parsed server name
        /// </summary>
        public ServerName ServerName { get; private set; }

        /// <summary>
        /// Indicates, if detected server is different from last detected server.
        /// Note, that this may be a false positive. This would happen, when WurmApi hadn't known previous server for this character. 
        /// To use this property reliably, first do a GetCurrentServer() and assuming it has returned a server (and thus WurmApi knows it now), 
        /// this property will give accurate information on successive invocations.
        /// </summary>
        public bool ServerChanged { get; private set; }
    }
}