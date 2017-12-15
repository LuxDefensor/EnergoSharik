using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Energosphere
{

    [Serializable]
    public class PointNotFoundException : Exception
    {
        public PointNotFoundException()
        {
        }
        public PointNotFoundException(string message) : base(message) { }
        public PointNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected PointNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
