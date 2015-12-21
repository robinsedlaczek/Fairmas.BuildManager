using System;

namespace Fairmas.BuildManager.Backend.Models
{
    public class InstallerBuildConfiguration
    {
        #region Public Properties

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        #endregion

        #region Internal Properties

        internal string PathToBatch { get; set; }

        internal bool IsRunning { get; set; }

        #endregion

    }
}
