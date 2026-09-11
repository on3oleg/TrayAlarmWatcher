using System.Drawing.Drawing2D;

namespace TrayAlarmWatcher;

internal static class TrayIconFactory
{
    private const int Size = 32;

    public static Icon CreateAlarmIcon() => CreateCircleIcon(Color.FromArgb(220, 38, 38), null);

    public static Icon CreateCalmIcon() => CreateCircleIcon(Color.FromArgb(46, 160, 67), null);

    public static Icon CreateUnknownIcon() => CreateCircleIcon(Color.FromArgb(130, 130, 130), "?");

    private static Icon CreateCircleIcon(Color fillColor, string? mark)
    {
        using var bitmap = new Bitmap(Size, Size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var brush = new SolidBrush(fillColor);
            g.FillEllipse(brush, 2, 2, Size - 4, Size - 4);

            using var pen = new Pen(Color.FromArgb(90, Color.Black), 1.5f);
            g.DrawEllipse(pen, 2, 2, Size - 4, Size - 4);

            if (mark is not null)
            {
                using var font = new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Pixel);
                var textSize = g.MeasureString(mark, font);
                using var textBrush = new SolidBrush(Color.White);
                g.DrawString(
                    mark,
                    font,
                    textBrush,
                    (Size - textSize.Width) / 2f,
                    (Size - textSize.Height) / 2f - 1f);
            }
        }

        // Іконка створюється один раз при старті й живе весь час роботи процесу,
        // тому HICON навмисно не звільняється через DestroyIcon — ОС прибирає його при завершенні процесу.
        return Icon.FromHandle(bitmap.GetHicon());
    }
}
