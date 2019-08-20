using System.Collections.Generic;
using System.Drawing;

namespace Shape
{
    public abstract class Shapes
    {
        [Serialize] public Color Color { get; set; }
    }

    public class Circle : Shapes
    {
        [Serialize] public Point Center { get; set; }
        [Serialize] public double Radius { get; set; }
    }

    public abstract class Polygon : Shapes
    {
    }

    public class Rectangle : Polygon
    {
        [Serialize] public Point BottomLeft { get; set; }
        [Serialize] public double Height { get; set; }
        [Serialize] public double Width { get; set; }

        [Serialize]
        public Point TopRight =>
            new Point
            {
                X = BottomLeft.X + Width,
                Y = BottomLeft.Y + Height
            };
    }

    public class Triangle : Polygon
    {
        [Serialize] public HashSet<Point> Vertexes { get; set; }
    }
}
