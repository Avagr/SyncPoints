using GraphX.PCL.Common.Models;
using System.Xml.Serialization;

namespace SyncPointsLib
{
    /// <summary>
    /// Weighted directed edge
    /// </summary>
    /// <typeparam name="TVertex"> Type of vertex</typeparam>
    public class WeightedEdge : EdgeBase<SyncVertex>
    {
        public WeightedEdge() : base(null, null, 1)
        {
        }

        [XmlAttribute("EdgeData")]
        public string EdgeData
        {
            get => Weight + "æ" + (IsStarting ? 1 : 0);
            set
            {
                Weight = double.Parse(value.Split('æ')[0]);
                IsStarting = value.Split('æ')[1] == "1";
            }
        }

        public bool IsStarting { get; set; }

        public WeightedEdge(SyncVertex source, SyncVertex target, double weight = 1) : base(source, target, weight) { }

        public override string ToString()
        {
            return Weight.ToString();
        }
    }
}
