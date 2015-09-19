using System;
using System.Collections.Generic;
using System.Linq;

namespace RemoteTerminal.Screens
{
    class ScreenScrollbackBuffer : IEnumerable<ScreenLine>
    {
        private const int PartitionSize = 100;

        private readonly int maximumCount;

        private readonly List<List<ScreenLine>> partitions = new List<List<ScreenLine>>(new[] { new List<ScreenLine>(PartitionSize) });

        public ScreenScrollbackBuffer(int maximumCount)
        {
            this.maximumCount = maximumCount;
        }

        public void Append(ScreenLine screenLine)
        {
            List<ScreenLine> currentPartition = this.partitions[0];
            currentPartition.Add(screenLine);

            if (currentPartition.Count >= currentPartition.Capacity)
            {
                if ((this.partitions.Count - 1) * PartitionSize >= this.maximumCount)
                {
                    var oldPartition = this.partitions[this.partitions.Count - 1];
                    this.partitions.RemoveAt(this.partitions.Count - 1);
                    foreach (var line in oldPartition)
                    {
                        ScreenCell.RecycleCells(line);
                    }
                }

                this.partitions.Insert(0, new List<ScreenLine>(PartitionSize));
            }
        }

        public int Count
        {
            get { return Math.Min(this.partitions.Sum(p => p.Count), this.maximumCount); }
        }

        public IEnumerator<ScreenLine> GetEnumerator()
        {
            int actualCount = this.partitions.Sum(p => p.Count);
            int skip = Math.Max(0, actualCount - this.maximumCount);

            var partitions = ((IEnumerable<List<ScreenLine>>)this.partitions).Reverse();

            return partitions.First().Skip(skip).Concat(partitions.Skip(1).SelectMany(p => p)).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
