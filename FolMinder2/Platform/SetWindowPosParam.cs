namespace FolMinder2.Platform
{
    public record struct SetWindowPosParam(
        bool ChangeSize = false,
        bool ChangePosition = false,
        ZOrderOption ZOrder = ZOrderOption.Unchanged,
        bool Activate = false,
        WindowVisibility Visibility = WindowVisibility.Unchanged,
        double Left = 0,
        double Top = 0,
        double Width = 0,
        double Height = 0);
}
