using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MondaySharp.NET.Domain.ColumnTypes
{
    public record ColumnRating : ColumnBaseType
    {
        public Rating? Rating { get; set; }

        public ColumnRating() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="rating"></param>
        public ColumnRating(string? id, Rating? rating)
        {
            this.Id = id;
            this.Rating = rating;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString() => string.Format("\"{0}\" : {{\"rating\" : {1}}}", this.Id, (int?)this.Rating ?? 0);
    }

    public enum Rating
    {
        None = 0,
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5
    }
}
