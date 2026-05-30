using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Lab2
{
    internal class Points
    {
        private Queue<Point> points = new Queue<Point>();
        private int width;
        private int height;
        public Points(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public int count()
        {
            return points.Count;
        }

        public void add(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            points.Enqueue(new Point(x, y));
        }
        public void add(Point point)
        {
            if (point.X < 0 || point.Y < 0 || point.X >= width || point.Y >= height) return;
            points.Enqueue(point);
        }

        public Point get()
        {
            return points.Dequeue();
        }
    }
}
