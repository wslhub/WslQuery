using System.Collections.Generic;

namespace WslQuery
{
    public sealed class DistroQueryResult
    {
        public bool Succeed { get; set; }
        public List<DistroInfo> Distros { get; set; } = new List<DistroInfo>();
        public string Error { get; set; }
    }
}
