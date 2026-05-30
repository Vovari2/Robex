using Lab2.operations;
using System.Drawing;
using System.Windows.Forms;

namespace Lab2
{
    internal abstract class Operation
    {
        protected Type type;
        protected Graphics graphics;
        protected Bitmap bitmap;
        protected Operation(Type type, Graphics graphics, Bitmap bitmap)
        {
            this.type = type;
            this.graphics = graphics;
            this.bitmap = bitmap;
        }
        internal Type getType()
        {
            return type;
        }

        internal abstract void move(PointF point);
        internal abstract void down(PointF point);
        internal abstract void up(PointF point);

        public static Operation of(Type type, Graphics g, Bitmap b, Brush mainColor, Brush secondColor, float width)
        {
            switch (type)
            {
                case Type.ROLLER: return new RollerOperation(g, b, mainColor, width);
                case Type.ERASER: return new EraserOperation(g, b, width);
                default: return null;
            }
        }

        public enum Type
        {
            NONE,
            ROLLER,
            ERASER,
        }
    }
}
