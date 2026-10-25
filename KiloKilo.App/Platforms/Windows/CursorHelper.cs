using System.Reflection;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;

namespace KiloKilo.WinUI;

/// <summary>
/// Affiche le curseur « main » au survol de tout élément cliquable (boutons, interrupteurs, cases,
/// listes déroulantes et vues portant un TapGestureRecognizer). Sans effet sur Android / iOS.
/// </summary>
public static class CursorHelper
{
    private static readonly PropertyInfo? ProtectedCursor =
        typeof(UIElement).GetProperty("ProtectedCursor", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void Register()
    {
        ViewHandler.ViewMapper.AppendToMapping("KiloKiloHandCursor", (handler, view) =>
        {
            if (handler.PlatformView is not UIElement element || !EstCliquable(view)) return;
            ProtectedCursor?.SetValue(element, InputSystemCursor.Create(InputSystemCursorShape.Hand));
        });
    }

    private static bool EstCliquable(IView view) =>
        view is Microsoft.Maui.Controls.Button
            or Microsoft.Maui.Controls.ImageButton
            or Microsoft.Maui.Controls.Switch
            or Microsoft.Maui.Controls.CheckBox
            or Microsoft.Maui.Controls.Picker
            or Microsoft.Maui.Controls.Slider
        || (view is Microsoft.Maui.Controls.View v && v.GestureRecognizers.OfType<TapGestureRecognizer>().Any());
}
