using System.Collections.Generic;

namespace Fairmas.BuildManager.Backend.Models
{
    public class BuildConfigurationParameter<T> : BuildConfigurationParameterBase
    {
        public IList<T> SourceValues
        {
            get;
            set;
        }

        public IList<T> SelectedValues
        {
            get;
            set;
        }
    }
}
