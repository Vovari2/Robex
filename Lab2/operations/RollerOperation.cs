using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lab2.operations
{
    internal class RollerOperation : Operation
    {
        private readonly List<PointF> points = new List<PointF>();
        private readonly Pen pen;

        private static int MAX_SIZE = 20;
        private readonly Queue<PointF> drawingPoints = new Queue<PointF>();
        internal RollerOperation(Graphics graphics, Bitmap bitmap, Brush color, float width) : base(Type.ROLLER, graphics, bitmap)
        {
            this.pen = new Pen(color, width);
        }


        internal override void move(PointF point)
        {
            if (points.Count() >= 1 && distance(point, points[points.Count() - 1]) <= 5D)
                return;

            points.Add(point);

            if (drawingPoints.Count >= MAX_SIZE)
                drawingPoints.Dequeue();
            drawingPoints.Enqueue(point);

            if (drawingPoints.Count >= 2)
                graphics.DrawLines(pen, drawingPoints.ToArray());
        }
        internal override void down(PointF point)
        {
            points.Add(point);
            drawingPoints.Enqueue(point);
        }
        internal override void up(PointF point)
        {

        }
        private double distance(PointF a, PointF b)
        {
            float x = a.X - b.X;
            float y = a.Y - b.Y;
            return Math.Sqrt(x*x + y*y );
        }
    }
}
