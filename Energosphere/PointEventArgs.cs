using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Energosphere
{
    public class PointEventArgs:EventArgs
    {
        public List<Point> Points;
        public Point ParentPoint;
    }
}
