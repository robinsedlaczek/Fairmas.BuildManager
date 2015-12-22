using System;
using System.Collections.Generic;

namespace Fairmas.BuildManager.Backend.Models
{
    public class BuildConfiguration
    {
        #region Public Properties

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public IList<BuildConfigurationParameter<string>> Parameters { get; set; }

        #endregion

        #region Internal Properties

        internal string PathToBatch { get; set; }

        internal bool IsRunning { get; set; }

        #endregion

    }
}
