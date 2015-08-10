using System.IO;

namespace AldursLab.WurmApi.Tests.Builders.WurmClient
{
    class WurmConfig
    {
        readonly DirectoryInfo configDir;

        FileInfo AutorunTxt { get; set; }
        FileInfo GameSettingsTxt { get; set; }

        public WurmConfig(DirectoryInfo configDir, string name)
        {
            Name = name;
            this.configDir = configDir;

            AutorunTxt = new FileInfo(Path.Combine(configDir.FullName, "autorun.txt"));
            Autorun = new Autorun(AutorunTxt);
            GameSettingsTxt = new FileInfo(Path.Combine(configDir.FullName, "gamesettings.txt"));
            GameSettings = new GameSettings(GameSettingsTxt);
        }

        public string Name { get; private set; }
        public string NameNormalized { get { return Name.ToUpperInvariant(); }}

        public Autorun Autorun { get; private set; }
        public GameSettings GameSettings { get; private set; }

        public DirectoryInfo ConfigDir
        {
            get { return configDir; }
        }
    }
}