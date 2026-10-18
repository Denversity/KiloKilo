using KiloKilo.Helpers;
using KiloKilo.Models;

namespace KiloKilo.Controls;

/// <summary>Courbe d'évolution du prix d'un produit (dessinée avec Microsoft.Maui.Graphics).</summary>
public class PrixChartDrawable : IDrawable
{
    private readonly IReadOnlyList<PointPrix> _points;
    private readonly bool _sombre;

    public PrixChartDrawable(IReadOnlyList<PointPrix> points, bool sombre)
    {
        _points = points;
        _sombre = sombre;
    }

    public void Draw(ICanvas canvas, RectF rect)
    {
        if (_points.Count == 0) return;

        var texte = Color.FromArgb(_sombre ? "#9AA59C" : "#6B7468");
        var grille = Color.FromArgb(_sombre ? "#2A332D" : "#E6E0D2");
        var courbe = Color.FromArgb(_sombre ? "#7FBF93" : "#4A7C59");

        const float gauche = 46, droite = 14, haut = 14, bas = 28;
        var w = rect.Width - gauche - droite;
        var h = rect.Height - haut - bas;

        var min = _points.Min(p => p.Prix);
        var max = _points.Max(p => p.Prix);
        if (max == min) { min *= 0.9m; max = max * 1.1m + 1; }
        else { var marge = (max - min) * 0.15m; min -= marge; max += marge; }

        float X(int i) => _points.Count == 1 ? gauche + w / 2 : gauche + w * i / (_points.Count - 1);
        float Y(decimal v) => haut + h * (float)((max - v) / (max - min));

        // Grille et repères de prix
        canvas.StrokeSize = 1;
        canvas.StrokeColor = grille;
        canvas.FontColor = texte;
        canvas.FontSize = 10;
        for (var i = 0; i < 3; i++)
        {
            var v = max - (max - min) * i / 2;
            var y = Y(v);
            canvas.DrawLine(gauche, y, gauche + w, y);
            canvas.DrawString(Fcfa.Number(Math.Round(v)), 0, y - 8, gauche - 6, 16,
                HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        // Courbe
        if (_points.Count > 1)
        {
            var path = new PathF();
            for (var i = 0; i < _points.Count; i++)
            {
                if (i == 0) path.MoveTo(X(i), Y(_points[i].Prix));
                else path.LineTo(X(i), Y(_points[i].Prix));
            }
            canvas.StrokeColor = courbe;
            canvas.StrokeSize = 3;
            canvas.StrokeLineJoin = LineJoin.Round;
            canvas.DrawPath(path);
        }

        // Points (le dernier relevé est mis en avant)
        canvas.FillColor = courbe;
        for (var i = 0; i < _points.Count; i++)
            canvas.FillCircle(X(i), Y(_points[i].Prix), i == _points.Count - 1 ? 6 : 4);

        // Dates de début et de fin
        canvas.FontColor = texte;
        canvas.DrawString(_points[0].Date.ToString("dd/MM"), X(0) - 24, rect.Height - bas + 8, 48, 16,
            HorizontalAlignment.Center, VerticalAlignment.Center);
        if (_points.Count > 1)
            canvas.DrawString(_points[^1].Date.ToString("dd/MM"), X(_points.Count - 1) - 24, rect.Height - bas + 8, 48, 16,
                HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
