using System;
using System.Collections.Generic;
using System.Linq;

namespace RemoteTerminal.Screens
{
    /// <summary>
    /// The virtual in-memory representation of the screen scrollback buffer.
    /// </summary>
    /// <remarks>
    /// The scrollback buffer is organized in multiple "partitions". Their purpose is to reduce the performance impact
    /// associated with the continuous adding and removal of lines to the buffer (which happens as soon as the scrollback
    /// buffer has reached its maximum size).
    /// </remarks>
    class ScreenScrollbackBuffer : IEnumerable<ScreenLine>
    {
        /// <summary>
        /// The maximum number of lines in each partition.
        /// </summary>
        private const int PartitionSize = 100;

        /// <summary>
        /// The maximum number of lines in this scrollback buffer.
        /// </summary>
        private readonly int maximumCount;

        /// <summary>
        /// The list of partitions.
        /// </summary>
        private readonly List<List<ScreenLine>> partitions = new List<List<ScreenLine>>(new[] { new List<ScreenLine>(PartitionSize) });

        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenScrollbackBuffer"/> class with the specified maximum number of lines.
        /// </summary>
        /// <param name="maximumCount">The maximum number of lines.</param>
        public ScreenScrollbackBuffer(int maximumCount)
        {
            this.maximumCount = maximumCount;
        }

        /// <summary>
        /// Appends a single line to the scrollback buffer.
        /// </summary>
        /// <param name="screenLine">The line to append.</param>
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

        /// <summary>
        /// The number of lines contained in this scrollback buffer.
        /// </summary>
        public int Count
        {
            get { return Math.Min(this.partitions.Sum(p => p.Count), this.maximumCount); }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the lines in the scrollback buffer.
        /// </summary>
        /// <returns>A <see cref="IEnumerator<T>"/> for the lines in the scrollback buffer.</returns>
        public IEnumerator<ScreenLine> GetEnumerator()
        {
            int actualCount = this.partitions.Sum(p => p.Count);
            int skip = Math.Max(0, actualCount - this.maximumCount);

            var partitions = ((IEnumerable<List<ScreenLine>>)this.partitions).Reverse();

            return partitions.First().Skip(skip).Concat(partitions.Skip(1).SelectMany(p => p)).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the lines in the scrollback buffer.
        /// </summary>
        /// <returns>A <see cref="IEnumerator<T>"/> for the lines in the scrollback buffer.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
