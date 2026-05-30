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
    internal class EraserOperation : Operation
    {
        private readonly List<PointF> points = new List<PointF>();
        private readonly float size;

        internal EraserOperation(Graphics graphics, Bitmap bitmap, float size) : base(Type.ERASER, graphics, bitmap)
        {
            this.size = size;
        }

        internal override void move(PointF point)
        {
            points.Add(point);
            graphics.FillEllipse(Brushes.White, point.X - size / 2, point.Y - size / 2, size, size);
        }
        internal override void down(PointF point)
        {
            points.Add(point);
            graphics.FillEllipse(Brushes.White, point.X - size / 2, point.Y - size / 2, size, size);
        }
        internal override void up(PointF point)
        {
        }
    }
}
