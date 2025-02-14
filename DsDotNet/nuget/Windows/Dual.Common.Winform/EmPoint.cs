using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dual.Common.Winform
{
    public static class EmPoint
    {
        /// <summary>
        /// base point 기준으로 ref point 가 얼마나 offset 되었는지를 반환
        /// </summary>
        public static Point GetOffset(this Point refPoint, Point basePoint)
        {
            var x = refPoint.X - basePoint.X;
            var y = refPoint.Y - basePoint.Y;
            return new Point(x, y);
        }

        public static Rectangle GetRectangle(this Control control, bool startAtOrigin=false)
        {
            (var w, var h) = (control.Width, control.Height);
            if (startAtOrigin)
                return new Rectangle(0, 0, w, h);

            var l = control.Location;
            return new Rectangle(l.X, l.Y, w, h);
        }
    }
}
